using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Registry
{
    /// <summary>
    /// Bridges Godgame authored entities into the shared PureDOTS villager and storehouse registries.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RegistrySpatialSyncSystem))]
    public partial struct GodgameRegistryBridgeSystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private EntityQuery _storehouseQuery;
        private EntityQuery _resourceNodeQuery;
        private EntityQuery _spawnerQuery;
        private EntityQuery _bandQuery;
        private EntityQuery _miracleQuery;
        private Entity _snapshotEntity;
        private ComponentLookup<SpatialGridResidency> _spatialResidencyLookup;
        private ComponentLookup<MiracleTarget> _miracleTargetLookup;
        private ComponentLookup<MiracleCaster> _miracleCasterLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RegistryDirectory>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<VillagerRegistry>();
            state.RequireForUpdate<StorehouseRegistry>();
            state.RequireForUpdate<ResourceRegistry>();
            state.RequireForUpdate<SpawnerRegistry>();
            state.RequireForUpdate<BandRegistry>();
            state.RequireForUpdate<MiracleRegistry>();

            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<GodgameVillager, LocalTransform>()
                .Build();

            _storehouseQuery = SystemAPI.QueryBuilder()
                .WithAll<GodgameStorehouse, LocalTransform>()
                .Build();

            _resourceNodeQuery = SystemAPI.QueryBuilder()
                .WithAll<GodgameResourceNodeMirror, LocalTransform>()
                .Build();

            _spawnerQuery = SystemAPI.QueryBuilder()
                .WithAll<GodgameSpawnerMirror, LocalTransform>()
                .Build();

            _bandQuery = SystemAPI.QueryBuilder()
                .WithAll<GodgameBand, LocalTransform>()
                .Build();

            _spatialResidencyLookup = state.GetComponentLookup<SpatialGridResidency>(isReadOnly: true);
            _miracleTargetLookup = state.GetComponentLookup<MiracleTarget>(isReadOnly: true);
            _miracleCasterLookup = state.GetComponentLookup<MiracleCaster>(isReadOnly: true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);

            _miracleQuery = SystemAPI.QueryBuilder()
                .WithAll<MiracleDefinition, MiracleRuntimeState>()
                .WithNone<PlaybackGuardTag>()
                .Build();

            using var snapshotQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GodgameRegistrySnapshot>());
            if (snapshotQuery.IsEmptyIgnoreFilter)
            {
                _snapshotEntity = state.EntityManager.CreateEntity(typeof(GodgameRegistrySnapshot));
            }
            else
            {
                _snapshotEntity = snapshotQuery.GetSingletonEntity();
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _spatialResidencyLookup.Update(ref state);
            _miracleTargetLookup.Update(ref state);
            _miracleCasterLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            var tick = SystemAPI.GetSingleton<TimeState>().Tick;
            var summary = new BridgeSummary(tick);

            UpdateVillagerRegistry(ref state, ref summary);
            UpdateStorehouseRegistry(ref state, ref summary);
            UpdateResourceRegistry(ref state, ref summary);
            UpdateSpawnerRegistry(ref state, ref summary);
            UpdateBandRegistry(ref state, ref summary);
            UpdateMiracleRegistry(ref state, ref summary);

            ref var snapshot = ref SystemAPI.GetComponentRW<GodgameRegistrySnapshot>(_snapshotEntity).ValueRW;
            snapshot.VillagerCount = summary.VillagerCount;
            snapshot.AvailableVillagers = summary.AvailableVillagers;
            snapshot.IdleVillagers = summary.IdleVillagers;
            snapshot.ReservedVillagers = summary.ReservedVillagers;
            snapshot.CombatReadyVillagers = summary.CombatReadyVillagers;
            snapshot.AverageVillagerHealth = summary.AverageHealth;
            snapshot.AverageVillagerMorale = summary.AverageMorale;
            snapshot.AverageVillagerEnergy = summary.AverageEnergy;
            snapshot.StorehouseCount = summary.StorehouseCount;
            snapshot.TotalStorehouseCapacity = summary.TotalStorehouseCapacity;
            snapshot.TotalStorehouseStored = summary.TotalStorehouseStored;
            snapshot.TotalStorehouseReserved = summary.TotalStorehouseReserved;
            snapshot.ResourceNodeCount = summary.ResourceNodeCount;
            snapshot.ActiveResourceNodes = summary.ActiveResourceNodes;
            snapshot.TotalResourceUnitsRemaining = summary.TotalResourceUnitsRemaining;
            snapshot.SpawnerCount = summary.SpawnerCount;
            snapshot.ActiveSpawnerCount = summary.ActiveSpawnerCount;
            snapshot.PendingSpawnerCount = summary.PendingSpawnerCount;
            snapshot.BandCount = summary.BandCount;
            snapshot.BandMemberCount = summary.BandMemberCount;
            snapshot.AverageBandMorale = summary.BandCount > 0 ? summary.BandMoraleSum / summary.BandCount : 0f;
            snapshot.AverageBandCohesion = summary.BandCount > 0 ? summary.BandCohesionSum / summary.BandCount : 0f;
            snapshot.AverageBandDiscipline = summary.BandCount > 0 ? summary.BandDisciplineSum / summary.BandCount : 0f;
            snapshot.MiracleCount = summary.MiracleCount;
            snapshot.ActiveMiracles = summary.ActiveMiracles;
            snapshot.SustainedMiracles = summary.SustainedMiracles;
            snapshot.CoolingMiracles = summary.CoolingMiracles;
            snapshot.TotalMiracleEnergyCost = summary.TotalMiracleEnergyCost;
            snapshot.TotalMiracleCooldownSeconds = summary.TotalMiracleCooldownSeconds;
            snapshot.LastRegistryTick = summary.Tick;
        }

        private void UpdateVillagerRegistry(ref SystemState state, ref BridgeSummary summary)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<VillagerRegistry>();
            var buffer = state.EntityManager.GetBuffer<VillagerRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;
            ref var registry = ref SystemAPI.GetComponentRW<VillagerRegistry>(registryEntity).ValueRW;

            if (metadata.ArchetypeId == 0)
            {
                metadata.ArchetypeId = GodgameRegistryIds.VillagerArchetype;
            }

            var expectedCount = math.max(4, _villagerQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<VillagerRegistryEntry>(expectedCount, Allocator.Temp);

            float healthSum = 0f;
            float moraleSum = 0f;
            float energySum = 0f;

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig && hasSpatialState && spatialConfig.CellCount > 0 && spatialConfig.CellSize > 0f;
            var hasSpatialSyncState = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);
            var spatialVersionSource = hasSpatialGrid ? spatialState.Version : (hasSpatialSyncState && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSyncState && spatialSyncState.HasSpatialData;

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            foreach (var (villager, transform, entity) in SystemAPI.Query<RefRO<GodgameVillager>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                var data = villager.ValueRO;
                var position = transform.ValueRO.Position;

                var availabilityFlags = BuildAvailabilityFlags(data.IsAvailable, data.IsReserved);

                var cellId = -1;
                var entrySpatialVersion = 0u;

                if (hasSpatialGrid)
                {
                    var resolved = false;
                    var fallback = false;

                    if (_spatialResidencyLookup.HasComponent(entity))
                    {
                        var residency = _spatialResidencyLookup[entity];
                        if ((uint)residency.CellId < (uint)spatialConfig.CellCount && residency.Version == spatialState.Version)
                        {
                            cellId = residency.CellId;
                            entrySpatialVersion = residency.Version;
                            resolved = true;
                        }
                    }

                    if (!resolved)
                    {
                        SpatialHash.Quantize(position, spatialConfig, out var coords);
                        var computedCell = SpatialHash.Flatten(in coords, in spatialConfig);
                        if ((uint)computedCell < (uint)spatialConfig.CellCount)
                        {
                            cellId = computedCell;
                            entrySpatialVersion = spatialState.Version;
                            fallback = true;
                        }
                        else
                        {
                            cellId = -1;
                            entrySpatialVersion = 0;
                            unmappedCount++;
                        }
                    }

                    if (resolved)
                    {
                        resolvedCount++;
                    }
                    else if (fallback)
                    {
                        fallbackCount++;
                    }
                }

                builder.Add(new VillagerRegistryEntry
                {
                    VillagerEntity = entity,
                    VillagerId = data.VillagerId,
                    FactionId = data.FactionId,
                    Position = position,
                    CellId = cellId,
                    SpatialVersion = entrySpatialVersion,
                    JobType = data.JobType,
                    JobPhase = data.JobPhase,
                    ActiveTicketId = data.ActiveTicketId,
                    CurrentResourceTypeIndex = data.CurrentResourceTypeIndex,
                    AvailabilityFlags = availabilityFlags,
                    Discipline = (byte)data.Discipline,
                    HealthPercent = (byte)math.clamp(math.round(data.HealthPercent), 0f, 100f),
                    MoralePercent = (byte)math.clamp(math.round(data.MoralePercent), 0f, 100f),
                    EnergyPercent = (byte)math.clamp(math.round(data.EnergyPercent), 0f, 100f),
                    AIState = (byte)data.AIState,
                    AIGoal = (byte)data.AIGoal,
                    CurrentTarget = data.CurrentTarget,
                    Productivity = data.Productivity
                });

                summary.VillagerCount++;

                if (data.IsAvailable != 0)
                {
                    summary.AvailableVillagers++;
                }

                if (data.IsAvailable != 0 && data.IsReserved == 0 && data.JobPhase == VillagerJob.JobPhase.Idle)
                {
                    summary.IdleVillagers++;
                }

                if (data.IsReserved != 0)
                {
                    summary.ReservedVillagers++;
                }

                if (data.IsCombatReady != 0)
                {
                    summary.CombatReadyVillagers++;
                }

                healthSum += math.clamp(data.HealthPercent, 0f, 100f);
                moraleSum += math.clamp(data.MoralePercent, 0f, 100f);
                energySum += math.clamp(data.EnergyPercent, 0f, 100f);
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref buffer, ref metadata, summary.Tick, continuity);

            var averageHealth = summary.VillagerCount > 0 ? healthSum / summary.VillagerCount : 0f;
            var averageMorale = summary.VillagerCount > 0 ? moraleSum / summary.VillagerCount : 0f;
            var averageEnergy = summary.VillagerCount > 0 ? energySum / summary.VillagerCount : 0f;

            registry.TotalVillagers = summary.VillagerCount;
            registry.AvailableVillagers = summary.AvailableVillagers;
            registry.IdleVillagers = summary.IdleVillagers;
            registry.ReservedVillagers = summary.ReservedVillagers;
            registry.CombatReadyVillagers = summary.CombatReadyVillagers;
            registry.AverageHealthPercent = averageHealth;
            registry.AverageMoralePercent = averageMorale;
            registry.AverageEnergyPercent = averageEnergy;
            registry.LastUpdateTick = summary.Tick;
            registry.LastSpatialVersion = spatialVersionSource;
            registry.SpatialResolvedCount = resolvedCount;
            registry.SpatialFallbackCount = fallbackCount;
            registry.SpatialUnmappedCount = unmappedCount;

            summary.AverageHealth = averageHealth;
            summary.AverageMorale = averageMorale;
            summary.AverageEnergy = averageEnergy;
        }

        private void UpdateStorehouseRegistry(ref SystemState state, ref BridgeSummary summary)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<StorehouseRegistry>();
            var buffer = state.EntityManager.GetBuffer<StorehouseRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;
            ref var registry = ref SystemAPI.GetComponentRW<StorehouseRegistry>(registryEntity).ValueRW;

            if (metadata.ArchetypeId == 0)
            {
                metadata.ArchetypeId = GodgameRegistryIds.StorehouseArchetype;
            }

            var expectedCount = math.max(2, _storehouseQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<StorehouseRegistryEntry>(expectedCount, Allocator.Temp);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig && hasSpatialState && spatialConfig.CellCount > 0 && spatialConfig.CellSize > 0f;
            var hasSpatialSyncState = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);
            var spatialVersionSource = hasSpatialGrid ? spatialState.Version : (hasSpatialSyncState && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSyncState && spatialSyncState.HasSpatialData;

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            foreach (var (storehouse, transform, entity) in SystemAPI.Query<RefRO<GodgameStorehouse>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                var data = storehouse.ValueRO;
                var position = transform.ValueRO.Position;

                var cellId = -1;
                var entrySpatialVersion = 0u;

                if (hasSpatialGrid)
                {
                    var resolved = false;
                    var fallback = false;

                    if (_spatialResidencyLookup.HasComponent(entity))
                    {
                        var residency = _spatialResidencyLookup[entity];
                        if ((uint)residency.CellId < (uint)spatialConfig.CellCount && residency.Version == spatialState.Version)
                        {
                            cellId = residency.CellId;
                            entrySpatialVersion = residency.Version;
                            resolved = true;
                        }
                    }

                    if (!resolved)
                    {
                        SpatialHash.Quantize(position, spatialConfig, out var coords);
                        var computedCell = SpatialHash.Flatten(in coords, in spatialConfig);
                        if ((uint)computedCell < (uint)spatialConfig.CellCount)
                        {
                            cellId = computedCell;
                            entrySpatialVersion = spatialState.Version;
                            fallback = true;
                        }
                        else
                        {
                            cellId = -1;
                            entrySpatialVersion = 0;
                            unmappedCount++;
                        }
                    }

                    if (resolved)
                    {
                        resolvedCount++;
                    }
                    else if (fallback)
                    {
                        fallbackCount++;
                    }
                }

                var summaries = new FixedList32Bytes<StorehouseRegistryCapacitySummary>();
                if (data.ResourceSummaries.Length > 0)
                {
                    for (var i = 0; i < data.ResourceSummaries.Length; i++)
                    {
                        var resourceSummary = data.ResourceSummaries[i];
                        summaries.Add(new StorehouseRegistryCapacitySummary
                        {
                            ResourceTypeIndex = resourceSummary.ResourceTypeIndex,
                            Capacity = resourceSummary.Capacity,
                            Stored = resourceSummary.Stored,
                            Reserved = resourceSummary.Reserved
                        });
                    }
                }
                else if (data.TotalCapacity > 0f || data.TotalStored > 0f || data.TotalReserved > 0f)
                {
                    summaries.Add(new StorehouseRegistryCapacitySummary
                    {
                        ResourceTypeIndex = data.PrimaryResourceTypeIndex,
                        Capacity = data.TotalCapacity,
                        Stored = data.TotalStored,
                        Reserved = data.TotalReserved
                    });
                }

                builder.Add(new StorehouseRegistryEntry
                {
                    StorehouseEntity = entity,
                    Position = position,
                    TotalCapacity = data.TotalCapacity,
                    TotalStored = data.TotalStored,
                    TypeSummaries = summaries,
                    LastMutationTick = data.LastMutationTick != 0 ? data.LastMutationTick : summary.Tick,
                    CellId = cellId,
                    SpatialVersion = entrySpatialVersion
                });

                summary.StorehouseCount++;
                summary.TotalStorehouseCapacity += math.max(0f, data.TotalCapacity);
                summary.TotalStorehouseStored += math.max(0f, data.TotalStored);
                summary.TotalStorehouseReserved += math.max(0f, data.TotalReserved);
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref buffer, ref metadata, summary.Tick, continuity);

            registry.TotalStorehouses = summary.StorehouseCount;
            registry.TotalCapacity = summary.TotalStorehouseCapacity;
            registry.TotalStored = summary.TotalStorehouseStored;
            registry.LastUpdateTick = summary.Tick;
            registry.LastSpatialVersion = spatialVersionSource;
            registry.SpatialResolvedCount = resolvedCount;
            registry.SpatialFallbackCount = fallbackCount;
            registry.SpatialUnmappedCount = unmappedCount;
        }

        private void UpdateMiracleRegistry(ref SystemState state, ref BridgeSummary summary)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<MiracleRegistry>();
            var buffer = state.EntityManager.GetBuffer<MiracleRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;
            ref var registry = ref SystemAPI.GetComponentRW<MiracleRegistry>(registryEntity).ValueRW;

            if (metadata.ArchetypeId == 0)
            {
                metadata.ArchetypeId = GodgameRegistryIds.MiracleArchetype;
            }

            var expectedCount = math.max(2, _miracleQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<MiracleRegistryEntry>(expectedCount, Allocator.Temp);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig && hasSpatialState && spatialConfig.CellCount > 0 && spatialConfig.CellSize > 0f;
            var hasSpatialSyncState = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);
            var spatialVersionSource = hasSpatialGrid ? spatialState.Version : (hasSpatialSyncState && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSyncState && spatialSyncState.HasSpatialData;

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            foreach (var (definition, runtime, entity) in SystemAPI
                         .Query<RefRO<MiracleDefinition>, RefRO<MiracleRuntimeState>>()
                         .WithNone<PlaybackGuardTag>()
                         .WithEntityAccess())
            {
                var targetPosition = float3.zero;
                if (_miracleTargetLookup.HasComponent(entity))
                {
                    targetPosition = _miracleTargetLookup[entity].TargetPosition;
                }
                else if (_localTransformLookup.HasComponent(entity))
                {
                    targetPosition = _localTransformLookup[entity].Position;
                }

                var cellId = -1;
                var entrySpatialVersion = spatialVersionSource;

                if (hasSpatialGrid)
                {
                    SpatialHash.Quantize(targetPosition, spatialConfig, out var coords);
                    var computedCell = SpatialHash.Flatten(in coords, in spatialConfig);
                    if ((uint)computedCell < (uint)spatialConfig.CellCount)
                    {
                        cellId = computedCell;
                        entrySpatialVersion = spatialState.Version;
                        fallbackCount++;
                    }
                    else
                    {
                        cellId = -1;
                        entrySpatialVersion = 0;
                        unmappedCount++;
                    }
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

                var casterEntity = _miracleCasterLookup.HasComponent(entity)
                    ? _miracleCasterLookup[entity].CasterEntity
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
                    TargetCellId = cellId,
                    SpatialVersion = entrySpatialVersion,
                    ChargePercent = runtime.ValueRO.ChargePercent,
                    CurrentRadius = runtime.ValueRO.CurrentRadius,
                    CurrentIntensity = runtime.ValueRO.CurrentIntensity,
                    CooldownSecondsRemaining = runtime.ValueRO.CooldownSecondsRemaining,
                    EnergyCostThisCast = definition.ValueRO.BaseCost,
                    LastCastTick = runtime.ValueRO.LastCastTick
                });

                summary.MiracleCount++;
                if ((flags & MiracleRegistryFlags.Active) != 0)
                {
                    summary.ActiveMiracles++;
                    summary.TotalMiracleEnergyCost += definition.ValueRO.BaseCost;
                }

                if ((flags & MiracleRegistryFlags.Sustained) != 0 && (flags & MiracleRegistryFlags.Active) != 0)
                {
                    summary.SustainedMiracles++;
                    summary.TotalMiracleEnergyCost += definition.ValueRO.SustainedCostPerSecond;
                }

                if ((flags & MiracleRegistryFlags.CoolingDown) != 0)
                {
                    summary.CoolingMiracles++;
                }

                summary.TotalMiracleCooldownSeconds += math.max(0f, runtime.ValueRO.CooldownSecondsRemaining);
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref buffer, ref metadata, summary.Tick, continuity);

            registry.TotalMiracles = summary.MiracleCount;
            registry.ActiveMiracles = summary.ActiveMiracles;
            registry.SustainedMiracles = summary.SustainedMiracles;
            registry.CoolingMiracles = summary.CoolingMiracles;
            registry.TotalEnergyCost = summary.TotalMiracleEnergyCost;
            registry.TotalCooldownSeconds = summary.TotalMiracleCooldownSeconds;
            registry.LastUpdateTick = summary.Tick;
            registry.LastSpatialVersion = spatialVersionSource;
            registry.SpatialResolvedCount = resolvedCount;
            registry.SpatialFallbackCount = fallbackCount;
            registry.SpatialUnmappedCount = unmappedCount;
        }

        private void UpdateResourceRegistry(ref SystemState state, ref BridgeSummary summary)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
            var buffer = state.EntityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;
            ref var registry = ref SystemAPI.GetComponentRW<ResourceRegistry>(registryEntity).ValueRW;

            if (metadata.ArchetypeId == 0)
            {
                metadata.ArchetypeId = GodgameRegistryIds.ResourceNodeArchetype;
            }

            var expectedCount = math.max(2, _resourceNodeQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<ResourceRegistryEntry>(expectedCount, Allocator.Temp);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig && hasSpatialState && spatialConfig.CellCount > 0 && spatialConfig.CellSize > 0f;
            var hasSpatialSyncState = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);
            var spatialVersionSource = hasSpatialGrid ? spatialState.Version : (hasSpatialSyncState && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSyncState && spatialSyncState.HasSpatialData;

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            foreach (var (resourceNode, transform, entity) in SystemAPI
                         .Query<RefRO<GodgameResourceNodeMirror>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var data = resourceNode.ValueRO;
                var position = transform.ValueRO.Position;

                var cellId = -1;
                var entrySpatialVersion = 0u;

                if (hasSpatialGrid)
                {
                    var resolved = false;
                    var fallback = false;

                    if (_spatialResidencyLookup.HasComponent(entity))
                    {
                        var residency = _spatialResidencyLookup[entity];
                        if ((uint)residency.CellId < (uint)spatialConfig.CellCount && residency.Version == spatialState.Version)
                        {
                            cellId = residency.CellId;
                            entrySpatialVersion = residency.Version;
                            resolved = true;
                        }
                    }

                    if (!resolved)
                    {
                        SpatialHash.Quantize(position, spatialConfig, out var coords);
                        var computedCell = SpatialHash.Flatten(in coords, in spatialConfig);
                        if ((uint)computedCell < (uint)spatialConfig.CellCount)
                        {
                            cellId = computedCell;
                            entrySpatialVersion = spatialState.Version;
                            fallback = true;
                        }
                        else
                        {
                            cellId = -1;
                            entrySpatialVersion = 0;
                            unmappedCount++;
                        }
                    }

                    if (resolved)
                    {
                        resolvedCount++;
                    }
                    else if (fallback)
                    {
                        fallbackCount++;
                    }
                }

                builder.Add(new ResourceRegistryEntry
                {
                    ResourceTypeIndex = data.ResourceTypeIndex,
                    SourceEntity = entity,
                    Position = position,
                    UnitsRemaining = data.RemainingAmount,
                    ActiveTickets = 0,
                    ClaimFlags = 0,
                    LastMutationTick = data.LastMutationTick != 0 ? data.LastMutationTick : summary.Tick,
                    CellId = cellId,
                    SpatialVersion = entrySpatialVersion,
                    FamilyIndex = 0,
                    Tier = ResourceTier.Raw
                });

                summary.ResourceNodeCount++;
                if (data.IsDepleted == 0)
                {
                    summary.ActiveResourceNodes++;
                }

                summary.TotalResourceUnitsRemaining += data.RemainingAmount;
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref buffer, ref metadata, summary.Tick, continuity);

            registry.TotalResources = summary.ResourceNodeCount;
            registry.TotalActiveResources = summary.ActiveResourceNodes;
            registry.LastUpdateTick = summary.Tick;
            registry.LastSpatialVersion = spatialVersionSource;
            registry.SpatialResolvedCount = resolvedCount;
            registry.SpatialFallbackCount = fallbackCount;
            registry.SpatialUnmappedCount = unmappedCount;
        }

        private static byte BuildAvailabilityFlags(byte available, byte reserved)
        {
            byte flags = 0;
            if (available != 0)
            {
                flags |= VillagerAvailabilityFlags.Available;
            }

            if (reserved != 0)
            {
                flags |= VillagerAvailabilityFlags.Reserved;
            }

            return flags;
        }

        private void UpdateSpawnerRegistry(ref SystemState state, ref BridgeSummary summary)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<SpawnerRegistry>();
            var buffer = state.EntityManager.GetBuffer<SpawnerRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;
            ref var registry = ref SystemAPI.GetComponentRW<SpawnerRegistry>(registryEntity).ValueRW;

            if (metadata.ArchetypeId == 0)
            {
                metadata.ArchetypeId = GodgameRegistryIds.SpawnerArchetype;
            }

            var expectedCount = math.max(2, _spawnerQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<SpawnerRegistryEntry>(expectedCount, Allocator.Temp);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig && hasSpatialState && spatialConfig.CellCount > 0 && spatialConfig.CellSize > 0f;
            var hasSpatialSyncState = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);
            var spatialVersionSource = hasSpatialGrid ? spatialState.Version : (hasSpatialSyncState && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSyncState && spatialSyncState.HasSpatialData;

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            foreach (var (spawner, transform, entity) in SystemAPI
                         .Query<RefRO<GodgameSpawnerMirror>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var data = spawner.ValueRO;
                var position = transform.ValueRO.Position;

                var cellId = -1;
                var entrySpatialVersion = 0u;

                if (hasSpatialGrid)
                {
                    var resolved = false;
                    var fallback = false;

                    if (_spatialResidencyLookup.HasComponent(entity))
                    {
                        var residency = _spatialResidencyLookup[entity];
                        if ((uint)residency.CellId < (uint)spatialConfig.CellCount && residency.Version == spatialState.Version)
                        {
                            cellId = residency.CellId;
                            entrySpatialVersion = residency.Version;
                            resolved = true;
                        }
                    }

                    if (!resolved)
                    {
                        SpatialHash.Quantize(position, spatialConfig, out var coords);
                        var computedCell = SpatialHash.Flatten(in coords, in spatialConfig);
                        if ((uint)computedCell < (uint)spatialConfig.CellCount)
                        {
                            cellId = computedCell;
                            entrySpatialVersion = spatialState.Version;
                            fallback = true;
                        }
                        else
                        {
                            cellId = -1;
                            entrySpatialVersion = 0;
                            unmappedCount++;
                        }
                    }

                    if (resolved)
                    {
                        resolvedCount++;
                    }
                    else if (fallback)
                    {
                        fallbackCount++;
                    }
                }

                var flags = data.IsActive != 0 ? SpawnerStatusFlags.Active : SpawnerStatusFlags.Disabled;
                builder.Add(new SpawnerRegistryEntry
                {
                    SpawnerEntity = entity,
                    SpawnerTypeId = data.SpawnerTypeId,
                    OwnerFaction = Entity.Null,
                    ActiveSpawnCount = data.PendingSpawnCount,
                    Capacity = data.TotalCapacity,
                    CooldownSeconds = 0f,
                    RemainingCooldown = 0f,
                    Flags = flags,
                    Position = position,
                    CellId = cellId,
                    SpatialVersion = entrySpatialVersion
                });

                summary.SpawnerCount++;
                if (data.IsActive != 0)
                {
                    summary.ActiveSpawnerCount++;
                }

                summary.PendingSpawnerCount += data.PendingSpawnCount;
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref buffer, ref metadata, summary.Tick, continuity);

            registry.TotalSpawners = summary.SpawnerCount;
            registry.ActiveSpawnerCount = summary.ActiveSpawnerCount;
            registry.LastUpdateTick = summary.Tick;
            registry.LastSpatialVersion = spatialVersionSource;
            registry.SpatialResolvedCount = resolvedCount;
            registry.SpatialFallbackCount = fallbackCount;
            registry.SpatialUnmappedCount = unmappedCount;
        }

        private void UpdateBandRegistry(ref SystemState state, ref BridgeSummary summary)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<BandRegistry>();
            var buffer = state.EntityManager.GetBuffer<BandRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;
            ref var registry = ref SystemAPI.GetComponentRW<BandRegistry>(registryEntity).ValueRW;

            if (metadata.ArchetypeId == 0)
            {
                metadata.ArchetypeId = GodgameRegistryIds.BandArchetype;
            }

            var expectedCount = math.max(1, _bandQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<BandRegistryEntry>(expectedCount, Allocator.Temp);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig && hasSpatialState && spatialConfig.CellCount > 0 && spatialConfig.CellSize > 0f;
            var hasSpatialSyncState = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);
            var spatialVersionSource = hasSpatialGrid ? spatialState.Version : (hasSpatialSyncState && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSyncState && spatialSyncState.HasSpatialData;

            var resolvedCount = 0;
            var fallbackCount = 0;
            var unmappedCount = 0;

            foreach (var (band, transform, entity) in SystemAPI
                         .Query<RefRO<GodgameBand>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var data = band.ValueRO;
                var position = transform.ValueRO.Position;

                var cellId = -1;
                var entrySpatialVersion = 0u;

                if (hasSpatialGrid)
                {
                    var resolved = false;
                    var fallback = false;

                    if (_spatialResidencyLookup.HasComponent(entity))
                    {
                        var residency = _spatialResidencyLookup[entity];
                        if ((uint)residency.CellId < (uint)spatialConfig.CellCount && residency.Version == spatialState.Version)
                        {
                            cellId = residency.CellId;
                            entrySpatialVersion = residency.Version;
                            resolved = true;
                        }
                    }

                    if (!resolved)
                    {
                        SpatialHash.Quantize(position, spatialConfig, out var coords);
                        var computedCell = SpatialHash.Flatten(in coords, in spatialConfig);
                        if ((uint)computedCell < (uint)spatialConfig.CellCount)
                        {
                            cellId = computedCell;
                            entrySpatialVersion = spatialState.Version;
                            fallback = true;
                        }
                        else
                        {
                            cellId = -1;
                            entrySpatialVersion = 0;
                            unmappedCount++;
                        }
                    }

                    if (resolved)
                    {
                        resolvedCount++;
                    }
                    else if (fallback)
                    {
                        fallbackCount++;
                    }
                }

                builder.Add(new BandRegistryEntry
                {
                    BandEntity = entity,
                    BandId = data.BandId,
                    Position = position,
                    MemberCount = data.MemberCount,
                    Morale = data.Morale,
                    Cohesion = data.Cohesion,
                    AverageDiscipline = data.AverageDiscipline,
                    Flags = data.StatusFlags,
                    CellId = cellId,
                    SpatialVersion = entrySpatialVersion
                });

                summary.BandCount++;
                summary.BandMemberCount += data.MemberCount;
                summary.BandMoraleSum += data.Morale;
                summary.BandCohesionSum += data.Cohesion;
                summary.BandDisciplineSum += data.AverageDiscipline;
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolvedCount, fallbackCount, unmappedCount, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref buffer, ref metadata, summary.Tick, continuity);

            registry.TotalBands = summary.BandCount;
            registry.TotalMembers = summary.BandMemberCount;
            registry.AverageMorale = summary.BandCount > 0 ? summary.BandMoraleSum / summary.BandCount : 0f;
            registry.AverageCohesion = summary.BandCount > 0 ? summary.BandCohesionSum / summary.BandCount : 0f;
            registry.AverageDiscipline = summary.BandCount > 0 ? summary.BandDisciplineSum / summary.BandCount : 0f;
            registry.LastUpdateTick = summary.Tick;
            registry.LastSpatialVersion = spatialVersionSource;
            registry.SpatialResolvedCount = resolvedCount;
            registry.SpatialFallbackCount = fallbackCount;
            registry.SpatialUnmappedCount = unmappedCount;
        }

        private struct BridgeSummary
        {
            public uint Tick;
            public int VillagerCount;
            public int AvailableVillagers;
            public int IdleVillagers;
            public int ReservedVillagers;
            public int CombatReadyVillagers;
            public int StorehouseCount;
            public float TotalStorehouseCapacity;
            public float TotalStorehouseStored;
            public float TotalStorehouseReserved;
            public float AverageHealth;
            public float AverageMorale;
            public float AverageEnergy;
            public int ResourceNodeCount;
            public int ActiveResourceNodes;
            public float TotalResourceUnitsRemaining;
            public int SpawnerCount;
            public int ActiveSpawnerCount;
            public int PendingSpawnerCount;
            public int BandCount;
            public int BandMemberCount;
            public float BandMoraleSum;
            public float BandCohesionSum;
            public float BandDisciplineSum;
            public int MiracleCount;
            public int ActiveMiracles;
            public int SustainedMiracles;
            public int CoolingMiracles;
            public float TotalMiracleEnergyCost;
            public float TotalMiracleCooldownSeconds;

            public BridgeSummary(uint tick)
            {
                Tick = tick;
                VillagerCount = 0;
                AvailableVillagers = 0;
                IdleVillagers = 0;
                ReservedVillagers = 0;
                CombatReadyVillagers = 0;
                StorehouseCount = 0;
                TotalStorehouseCapacity = 0f;
                TotalStorehouseStored = 0f;
                TotalStorehouseReserved = 0f;
                AverageHealth = 0f;
                AverageMorale = 0f;
                AverageEnergy = 0f;
                ResourceNodeCount = 0;
                ActiveResourceNodes = 0;
                TotalResourceUnitsRemaining = 0f;
                SpawnerCount = 0;
                ActiveSpawnerCount = 0;
                PendingSpawnerCount = 0;
                BandCount = 0;
                BandMemberCount = 0;
                BandMoraleSum = 0f;
                BandCohesionSum = 0f;
                BandDisciplineSum = 0f;
                MiracleCount = 0;
                ActiveMiracles = 0;
                SustainedMiracles = 0;
                CoolingMiracles = 0;
                TotalMiracleEnergyCost = 0f;
                TotalMiracleCooldownSeconds = 0f;
            }
        }
    }

    /// <summary>
    /// Publishes Godgame registry metrics into the shared telemetry stream after debug data is assembled.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(DebugDisplaySystem))]
    public partial struct GodgameRegistryTelemetrySystem : ISystem
    {
        private static readonly FixedString64Bytes MetricVillagers = new FixedString64Bytes("godgame.registry.villagers");
        private static readonly FixedString64Bytes MetricVillagersAvailable = new FixedString64Bytes("godgame.registry.villagers.available");
        private static readonly FixedString64Bytes MetricVillagersIdle = new FixedString64Bytes("godgame.registry.villagers.idle");
        private static readonly FixedString64Bytes MetricVillagersReserved = new FixedString64Bytes("godgame.registry.villagers.reserved");
        private static readonly FixedString64Bytes MetricVillagersCombatReady = new FixedString64Bytes("godgame.registry.villagers.combatready");
        private static readonly FixedString64Bytes MetricVillagersHealth = new FixedString64Bytes("godgame.registry.villagers.health.avg");
        private static readonly FixedString64Bytes MetricVillagersMorale = new FixedString64Bytes("godgame.registry.villagers.morale.avg");
        private static readonly FixedString64Bytes MetricVillagersEnergy = new FixedString64Bytes("godgame.registry.villagers.energy.avg");
        private static readonly FixedString64Bytes MetricStorehouses = new FixedString64Bytes("godgame.registry.storehouses");
        private static readonly FixedString64Bytes MetricStorehousesCapacity = new FixedString64Bytes("godgame.registry.storehouses.capacity");
        private static readonly FixedString64Bytes MetricStorehousesStored = new FixedString64Bytes("godgame.registry.storehouses.stored");
        private static readonly FixedString64Bytes MetricStorehousesReserved = new FixedString64Bytes("godgame.registry.storehouses.reserved");
        private static readonly FixedString64Bytes MetricResourceNodes = new FixedString64Bytes("godgame.registry.resources.nodes");
        private static readonly FixedString64Bytes MetricResourceNodesActive = new FixedString64Bytes("godgame.registry.resources.nodes.active");
        private static readonly FixedString64Bytes MetricResourceUnits = new FixedString64Bytes("godgame.registry.resources.units");
        private static readonly FixedString64Bytes MetricSpawners = new FixedString64Bytes("godgame.registry.spawners");
        private static readonly FixedString64Bytes MetricSpawnersActive = new FixedString64Bytes("godgame.registry.spawners.active");
        private static readonly FixedString64Bytes MetricSpawnersPending = new FixedString64Bytes("godgame.registry.spawners.pending");
        private static readonly FixedString64Bytes MetricBands = new FixedString64Bytes("godgame.registry.bands");
        private static readonly FixedString64Bytes MetricBandMembers = new FixedString64Bytes("godgame.registry.bands.members");
        private static readonly FixedString64Bytes MetricBandMorale = new FixedString64Bytes("godgame.registry.bands.morale.avg");
        private static readonly FixedString64Bytes MetricBandCohesion = new FixedString64Bytes("godgame.registry.bands.cohesion.avg");
        private static readonly FixedString64Bytes MetricBandDiscipline = new FixedString64Bytes("godgame.registry.bands.discipline.avg");
        private static readonly FixedString64Bytes MetricMiracles = new FixedString64Bytes("godgame.registry.miracles");
        private static readonly FixedString64Bytes MetricMiraclesActive = new FixedString64Bytes("godgame.registry.miracles.active");
        private static readonly FixedString64Bytes MetricMiraclesSustained = new FixedString64Bytes("godgame.registry.miracles.sustained");
        private static readonly FixedString64Bytes MetricMiraclesCooling = new FixedString64Bytes("godgame.registry.miracles.cooling");
        private static readonly FixedString64Bytes MetricMiracleEnergy = new FixedString64Bytes("godgame.registry.miracles.energy");
        private static readonly FixedString64Bytes MetricMiracleCooldown = new FixedString64Bytes("godgame.registry.miracles.cooldown");
        private static readonly FixedString64Bytes MetricTick = new FixedString64Bytes("godgame.registry.tick");

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GodgameRegistrySnapshot>();
            state.RequireForUpdate<TelemetryStream>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var snapshot = SystemAPI.GetSingleton<GodgameRegistrySnapshot>();
            var buffer = SystemAPI.GetSingletonBuffer<TelemetryMetric>();

            buffer.Add(new TelemetryMetric { Key = MetricVillagers, Value = snapshot.VillagerCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricVillagersAvailable, Value = snapshot.AvailableVillagers, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricVillagersIdle, Value = snapshot.IdleVillagers, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricVillagersReserved, Value = snapshot.ReservedVillagers, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricVillagersCombatReady, Value = snapshot.CombatReadyVillagers, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricVillagersHealth, Value = snapshot.AverageVillagerHealth, Unit = TelemetryMetricUnit.Ratio });
            buffer.Add(new TelemetryMetric { Key = MetricVillagersMorale, Value = snapshot.AverageVillagerMorale, Unit = TelemetryMetricUnit.Ratio });
            buffer.Add(new TelemetryMetric { Key = MetricVillagersEnergy, Value = snapshot.AverageVillagerEnergy, Unit = TelemetryMetricUnit.Ratio });
            buffer.Add(new TelemetryMetric { Key = MetricStorehouses, Value = snapshot.StorehouseCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricStorehousesCapacity, Value = snapshot.TotalStorehouseCapacity, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricStorehousesStored, Value = snapshot.TotalStorehouseStored, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricStorehousesReserved, Value = snapshot.TotalStorehouseReserved, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricResourceNodes, Value = snapshot.ResourceNodeCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricResourceNodesActive, Value = snapshot.ActiveResourceNodes, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricResourceUnits, Value = snapshot.TotalResourceUnitsRemaining, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricSpawners, Value = snapshot.SpawnerCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricSpawnersActive, Value = snapshot.ActiveSpawnerCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricSpawnersPending, Value = snapshot.PendingSpawnerCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricBands, Value = snapshot.BandCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricBandMembers, Value = snapshot.BandMemberCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricBandMorale, Value = snapshot.AverageBandMorale, Unit = TelemetryMetricUnit.Ratio });
            buffer.Add(new TelemetryMetric { Key = MetricBandCohesion, Value = snapshot.AverageBandCohesion, Unit = TelemetryMetricUnit.Ratio });
            buffer.Add(new TelemetryMetric { Key = MetricBandDiscipline, Value = snapshot.AverageBandDiscipline, Unit = TelemetryMetricUnit.Ratio });
            buffer.Add(new TelemetryMetric { Key = MetricMiracles, Value = snapshot.MiracleCount, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricMiraclesActive, Value = snapshot.ActiveMiracles, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricMiraclesSustained, Value = snapshot.SustainedMiracles, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricMiraclesCooling, Value = snapshot.CoolingMiracles, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricMiracleEnergy, Value = snapshot.TotalMiracleEnergyCost, Unit = TelemetryMetricUnit.Count });
            buffer.Add(new TelemetryMetric { Key = MetricMiracleCooldown, Value = snapshot.TotalMiracleCooldownSeconds, Unit = TelemetryMetricUnit.Time });
            buffer.Add(new TelemetryMetric { Key = MetricTick, Value = snapshot.LastRegistryTick, Unit = TelemetryMetricUnit.Count });
        }
    }
}
