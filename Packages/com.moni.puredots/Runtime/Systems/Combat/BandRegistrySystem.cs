using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Maintains registry entries for band/squad entities.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup), OrderFirst = true)]
    public partial struct BandRegistrySystem : ISystem
    {
        private EntityQuery _bandQuery;
        private ComponentLookup<SpatialGridResidency> _residencyLookup;
        private ComponentLookup<BandStats> _statsLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bandQuery = SystemAPI.QueryBuilder()
                .WithAll<BandId, BandStats, LocalTransform>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<BandRegistry>();

            _residencyLookup = state.GetComponentLookup<SpatialGridResidency>(isReadOnly: true);
            _statsLookup = state.GetComponentLookup<BandStats>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var registryEntity = SystemAPI.GetSingletonEntity<BandRegistry>();
            var registry = SystemAPI.GetComponentRW<BandRegistry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;
            var entries = state.EntityManager.GetBuffer<BandRegistryEntry>(registryEntity);

            var expectedCount = math.max(16, _bandQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<BandRegistryEntry>(expectedCount, Allocator.Temp);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig gridConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState gridState);
            var hasSpatial = hasSpatialConfig && hasSpatialState && gridConfig.CellCount > 0 && gridConfig.CellSize > 0f;

            var hasSyncState = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState syncState);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSyncState && syncState.HasSpatialData;
            var spatialVersion = hasSpatial ? gridState.Version : (requireSpatialSync ? syncState.SpatialVersion : 0u);

            _residencyLookup.Update(ref state);
            _statsLookup.Update(ref state);

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;
            var totalMembers = 0;

            foreach (var (bandId, transform, entity) in SystemAPI.Query<RefRO<BandId>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                var stats = _statsLookup.HasComponent(entity)
                    ? _statsLookup[entity]
                    : new BandStats { MemberCount = 0, Morale = 1f, Flags = 0 };

                int cellId = -1;
                var usedResidency = false;

                if (hasSpatial && _residencyLookup.HasComponent(entity))
                {
                    var residency = _residencyLookup[entity];
                    if ((uint)residency.CellId < (uint)gridConfig.CellCount)
                    {
                        cellId = residency.CellId;
                        resolvedCount++;
                        usedResidency = true;
                    }
                }

                if (!usedResidency && hasSpatial)
                {
                    SpatialHash.Quantize(transform.ValueRO.Position, gridConfig, out var coords);
                    var flattened = SpatialHash.Flatten(in coords, in gridConfig);
                    if ((uint)flattened < (uint)gridConfig.CellCount)
                    {
                        cellId = flattened;
                        fallbackCount++;
                    }
                    else
                    {
                        unmappedCount++;
                    }
                }

                builder.Add(new BandRegistryEntry
                {
                    BandEntity = entity,
                    BandId = bandId.ValueRO.Value,
                    Position = transform.ValueRO.Position,
                    MemberCount = stats.MemberCount,
                    Morale = stats.Morale,
                    Flags = stats.Flags,
                    CellId = cellId,
                    SpatialVersion = spatialVersion
                });

                totalMembers += stats.MemberCount;
            }

            var continuity = hasSpatial
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersion, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref entries, ref metadata, timeState.Tick, continuity);

            registry.ValueRW = new BandRegistry
            {
                TotalBands = entries.Length,
                TotalMembers = totalMembers,
                LastUpdateTick = timeState.Tick,
                LastSpatialVersion = spatialVersion,
                SpatialResolvedCount = resolvedCount,
                SpatialFallbackCount = fallbackCount,
                SpatialUnmappedCount = unmappedCount
            };
        }
    }
}

