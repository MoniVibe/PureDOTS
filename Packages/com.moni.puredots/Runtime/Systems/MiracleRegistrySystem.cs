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
    /// Builds the miracle registry for quick lookup by hand, AI, and presentation systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct MiracleRegistrySystem : ISystem
    {
        private EntityQuery _miracleQuery;
        private ComponentLookup<MiracleTarget> _targetLookup;
        private ComponentLookup<MiracleCaster> _casterLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _miracleQuery = SystemAPI.QueryBuilder()
                .WithAll<MiracleDefinition, MiracleRuntimeState>()
                .WithNone<PlaybackGuardTag>()
                .Build();

            _targetLookup = state.GetComponentLookup<MiracleTarget>(true);
            _casterLookup = state.GetComponentLookup<MiracleCaster>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);

            state.RequireForUpdate<MiracleRegistry>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
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

            _targetLookup.Update(ref state);
            _casterLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var registryEntity = SystemAPI.GetSingletonEntity<MiracleRegistry>();
            var registry = SystemAPI.GetComponentRW<MiracleRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<MiracleRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig
                                 && hasSpatialState
                                 && spatialConfig.CellCount > 0
                                 && spatialConfig.CellSize > 0f;

            var hasSpatialSync = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);
            var spatialVersionSource = hasSpatialGrid
                ? spatialState.Version
                : (hasSpatialSync && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSync && spatialSyncState.HasSpatialData;

            var expectedCount = math.max(8, _miracleQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<MiracleRegistryEntry>(expectedCount, Allocator.Temp);

            var totalMiracles = 0;
            var activeMiracles = 0;
            var sustainedMiracles = 0;
            var coolingMiracles = 0;
            var totalEnergyCost = 0f;
            var totalCooldownSeconds = 0f;

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            foreach (var (definition, runtime, entity) in SystemAPI.Query<RefRO<MiracleDefinition>, RefRO<MiracleRuntimeState>>()
                         .WithNone<PlaybackGuardTag>()
                         .WithEntityAccess())
            {
                var targetPosition = float3.zero;
                if (_targetLookup.HasComponent(entity))
                {
                    targetPosition = _targetLookup[entity].TargetPosition;
                }
                else if (_transformLookup.HasComponent(entity))
                {
                    targetPosition = _transformLookup[entity].Position;
                }

                var targetCellId = -1;
                var entrySpatialVersion = spatialVersionSource;

                if (hasSpatialGrid)
                {
                    entrySpatialVersion = spatialState.Version;
                    targetCellId = ClassifyPosition(targetPosition, in spatialConfig, spatialState.Version, ref resolvedCount, ref fallbackCount, ref unmappedCount);
                }

                var flags = MiracleRegistryFlags.None;
                if (runtime.ValueRO.Lifecycle == MiracleLifecycleState.Active)
                {
                    flags |= MiracleRegistryFlags.Active;
                }

                if (definition.ValueRO.CastingMode == MiracleCastingMode.Sustained)
                {
                    flags |= MiracleRegistryFlags.Sustained;
                }

                if (runtime.ValueRO.Lifecycle == MiracleLifecycleState.CoolingDown)
                {
                    flags |= MiracleRegistryFlags.CoolingDown;
                }

                var casterEntity = _casterLookup.HasComponent(entity)
                    ? _casterLookup[entity].CasterEntity
                    : Entity.Null;

                builder.Add(new MiracleRegistryEntry
                {
                    MiracleEntity = entity,
                    CasterEntity = casterEntity,
                    Type = definition.ValueRO.Type,
                    CastingMode = definition.ValueRO.CastingMode,
                    Lifecycle = runtime.ValueRO.Lifecycle,
                    Flags = flags,
                    TargetPosition = targetPosition,
                    TargetCellId = targetCellId,
                    SpatialVersion = entrySpatialVersion,
                    ChargePercent = runtime.ValueRO.ChargePercent,
                    CurrentRadius = runtime.ValueRO.CurrentRadius,
                    CurrentIntensity = runtime.ValueRO.CurrentIntensity,
                    CooldownSecondsRemaining = runtime.ValueRO.CooldownSecondsRemaining,
                    EnergyCostThisCast = definition.ValueRO.BaseCost,
                    LastCastTick = runtime.ValueRO.LastCastTick
                });

                totalMiracles++;
                if ((flags & MiracleRegistryFlags.Active) != 0)
                {
                    activeMiracles++;
                    totalEnergyCost += definition.ValueRO.BaseCost;
                }

                if ((flags & MiracleRegistryFlags.Sustained) != 0 && (flags & MiracleRegistryFlags.Active) != 0)
                {
                    sustainedMiracles++;
                    totalEnergyCost += definition.ValueRO.SustainedCostPerSecond;
                }

                if ((flags & MiracleRegistryFlags.CoolingDown) != 0)
                {
                    coolingMiracles++;
                }

                totalCooldownSeconds += runtime.ValueRO.CooldownSecondsRemaining;
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref entries, ref metadata, timeState.Tick, continuity);

            registry.ValueRW = new MiracleRegistry
            {
                TotalMiracles = totalMiracles,
                ActiveMiracles = activeMiracles,
                SustainedMiracles = sustainedMiracles,
                CoolingMiracles = coolingMiracles,
                TotalEnergyCost = totalEnergyCost,
                TotalCooldownSeconds = totalCooldownSeconds,
                LastUpdateTick = timeState.Tick,
                LastSpatialVersion = spatialVersionSource,
                SpatialResolvedCount = resolvedCount,
                SpatialFallbackCount = fallbackCount,
                SpatialUnmappedCount = unmappedCount
            };
        }

        private static int ClassifyPosition(float3 position, in SpatialGridConfig config, uint gridVersion, ref int resolved, ref int fallback, ref int unmapped)
        {
            SpatialHash.Quantize(position, config, out var coords);
            var cellId = SpatialHash.Flatten(in coords, in config);
            if ((uint)cellId < (uint)config.CellCount)
            {
                fallback++;
                return cellId;
            }

            unmapped++;
            return -1;
        }
    }
}
