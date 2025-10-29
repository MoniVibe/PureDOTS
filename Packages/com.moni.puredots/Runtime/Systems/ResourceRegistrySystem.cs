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
    /// Maintains a registry of all resource sources indexed by type for efficient queries.
    /// Updates singleton component and buffer with current resource state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ResourceSystemGroup), OrderFirst = true)]
    public partial struct ResourceRegistrySystem : ISystem
    {
        private EntityQuery _resourceQuery;
        private ComponentLookup<ResourceJobReservation> _reservationLookup;
        private ComponentLookup<SpatialGridResidency> _residencyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _resourceQuery = SystemAPI.QueryBuilder()
                .WithAll<ResourceSourceConfig, ResourceTypeId>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ResourceTypeIndex>();
            state.RequireForUpdate<ResourceRegistry>();

            _reservationLookup = state.GetComponentLookup<ResourceJobReservation>(true);
            _residencyLookup = state.GetComponentLookup<SpatialGridResidency>(true);
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

            var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
            var registry = SystemAPI.GetComponentRW<ResourceRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            ref var registryMetadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;

            var totalResources = 0;
            var totalActiveResources = 0;

            // Get catalog for type lookups
            var catalog = SystemAPI.GetSingleton<ResourceTypeIndex>().Catalog;
            if (!catalog.IsCreated)
            {
                return;
            }

            _reservationLookup.Update(ref state);
            _residencyLookup.Update(ref state);

            var expectedCount = math.max(16, _resourceQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<ResourceRegistryEntry>(expectedCount, Allocator.Temp);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig gridConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState gridState);
            var hasSpatial = hasSpatialConfig
                             && hasSpatialState
                             && gridConfig.CellCount > 0
                             && gridConfig.CellSize > 0f;

            RegistrySpatialSyncState syncState = default;
            var hasSyncState = SystemAPI.TryGetSingleton(out syncState);
            var requireSpatialSync = registryMetadata.SupportsSpatialQueries && hasSyncState && syncState.HasSpatialData;
            var spatialVersion = hasSpatial
                ? gridState.Version
                : (requireSpatialSync ? syncState.SpatialVersion : 0u);

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            // Query all resource sources
            foreach (var (sourceState, resourceTypeId, transform, entity) in SystemAPI.Query<RefRO<ResourceSourceState>, RefRO<ResourceTypeId>, RefRO<LocalTransform>>()
                .WithAll<ResourceSourceConfig>()
                .WithEntityAccess())
            {
                // Lookup type index
                var typeIndex = catalog.Value.LookupIndex(resourceTypeId.ValueRO.Value);
                if (typeIndex < 0)
                {
                    continue; // Skip unknown types
                }

                var reservation = _reservationLookup.HasComponent(entity)
                    ? _reservationLookup[entity]
                    : default;

                var position = transform.ValueRO.Position;
                var cellId = -1;
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
                    SpatialHash.Quantize(position, gridConfig, out var coords);
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

                builder.Add(new ResourceRegistryEntry
                {
                    ResourceTypeIndex = (ushort)typeIndex,
                    SourceEntity = entity,
                    Position = position,
                    UnitsRemaining = sourceState.ValueRO.UnitsRemaining,
                    ActiveTickets = reservation.ActiveTickets,
                    ClaimFlags = reservation.ClaimFlags,
                    LastMutationTick = reservation.LastMutationTick,
                    CellId = cellId,
                    SpatialVersion = spatialVersion
                });

                totalResources++;
                if (sourceState.ValueRO.UnitsRemaining > 0f)
                {
                totalActiveResources++;
                }
            }

            var continuity = hasSpatial
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersion, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref entries, ref registryMetadata, timeState.Tick, continuity);

            registry.ValueRW = new ResourceRegistry
            {
                TotalResources = totalResources,
                TotalActiveResources = totalActiveResources,
                LastUpdateTick = timeState.Tick,
                LastSpatialVersion = spatialVersion,
                SpatialResolvedCount = resolvedCount,
                SpatialFallbackCount = fallbackCount,
                SpatialUnmappedCount = unmappedCount
            };
        }
    }
}
