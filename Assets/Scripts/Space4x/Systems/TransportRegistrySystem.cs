using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using Space4X.Runtime.Transport;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Systems
{
    /// <summary>
    /// Populates transport registries (miner vessels, haulers, freighters, wagons) with spatial metadata and availability summaries.
    /// Mirrors the villager/resource registry pattern so downstream systems can resolve grid cells deterministically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PureDOTS.Systems.ResourceSystemGroup))]
    public partial struct TransportRegistrySystem : ISystem
    {
        private ComponentLookup<SpatialGridResidency> _residencyLookup;
        private EntityQuery _minerQuery;
        private EntityQuery _haulerQuery;
        private EntityQuery _freighterQuery;
        private EntityQuery _wagonQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _residencyLookup = state.GetComponentLookup<SpatialGridResidency>(true);

            _minerQuery = SystemAPI.QueryBuilder().WithAll<MinerVessel, LocalTransform>().WithNone<PlaybackGuardTag>().Build();
            _haulerQuery = SystemAPI.QueryBuilder().WithAll<Hauler, LocalTransform>().WithNone<PlaybackGuardTag>().Build();
            _freighterQuery = SystemAPI.QueryBuilder().WithAll<Freighter, LocalTransform>().WithNone<PlaybackGuardTag>().Build();
            _wagonQuery = SystemAPI.QueryBuilder().WithAll<Wagon, LocalTransform>().WithNone<PlaybackGuardTag>().Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<MinerVesselRegistry>();
            state.RequireForUpdate<HaulerRegistry>();
            state.RequireForUpdate<FreighterRegistry>();
            state.RequireForUpdate<WagonRegistry>();
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

            state.EntityManager.CompleteDependencyBeforeRO<SpatialGridResidency>();
            _residencyLookup.Update(ref state);

            var hasSpatialConfig = SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig);
            var hasSpatialState = SystemAPI.TryGetSingleton(out SpatialGridState spatialState);
            var hasSpatialGrid = hasSpatialConfig
                                 && hasSpatialState
                                 && spatialConfig.CellCount > 0
                                 && spatialConfig.CellSize > 0f;

            var hasSpatialSync = SystemAPI.TryGetSingleton(out RegistrySpatialSyncState spatialSyncState);

            UpdateMinerVesselRegistry(ref state, in timeState, hasSpatialGrid, hasSpatialSync, in spatialConfig, in spatialState, in spatialSyncState);
            UpdateHaulerRegistry(ref state, in timeState, hasSpatialGrid, hasSpatialSync, in spatialConfig, in spatialState, in spatialSyncState);
            UpdateFreighterRegistry(ref state, in timeState, hasSpatialGrid, hasSpatialSync, in spatialConfig, in spatialState, in spatialSyncState);
            UpdateWagonRegistry(ref state, in timeState, hasSpatialGrid, hasSpatialSync, in spatialConfig, in spatialState, in spatialSyncState);
        }

        private void UpdateMinerVesselRegistry(ref SystemState state, in TimeState timeState, bool hasSpatialGrid, bool hasSpatialSync, in SpatialGridConfig spatialConfig, in SpatialGridState spatialState, in RegistrySpatialSyncState spatialSyncState)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<MinerVesselRegistry>();
            var registry = SystemAPI.GetComponentRW<MinerVesselRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<MinerVesselRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;

            var expected = math.max(8, _minerQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<MinerVesselRegistryEntry>(expected, Allocator.Temp);

            var spatialVersionSource = hasSpatialGrid
                ? spatialState.Version
                : (hasSpatialSync && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSync && spatialSyncState.HasSpatialData;

            var total = 0;
            var available = 0;
            var idle = 0;
            var busy = 0;
            var totalCapacity = 0f;
            var totalLoad = 0f;
            var resolved = 0;
            var fallback = 0;
            var unmapped = 0;

            foreach (var (vessel, transform, entity) in SystemAPI.Query<RefRO<MinerVessel>, RefRO<LocalTransform>>().WithNone<PlaybackGuardTag>().WithEntityAccess())
            {
                var position = transform.ValueRO.Position;
                var cellId = -1;
                uint spatialVersion = spatialVersionSource;

                if (hasSpatialGrid)
                {
                    ClassifySpatialData(entity, position, true, in spatialConfig, spatialState.Version, ref cellId, ref spatialVersion, ref resolved, ref fallback, ref unmapped);
                }

                var entry = new MinerVesselRegistryEntry
                {
                    VesselEntity = entity,
                    Position = position,
                    CellId = cellId,
                    SpatialVersion = spatialVersion,
                    ResourceTypeIndex = vessel.ValueRO.ResourceTypeIndex,
                    Capacity = vessel.ValueRO.Capacity,
                    Load = vessel.ValueRO.Load,
                    Flags = vessel.ValueRO.Flags,
                    LastCommandTick = vessel.ValueRO.LastCommandTick
                };

                builder.Add(entry);

                total++;
                totalCapacity += vessel.ValueRO.Capacity;
                totalLoad += vessel.ValueRO.Load;
                if ((vessel.ValueRO.Flags & TransportUnitFlags.Idle) != 0)
                {
                    available++;
                    idle++;
                }
                else
                {
                    busy++;
                }
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolved, fallback, unmapped, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref entries, ref metadata, timeState.Tick, continuity);

            registry.ValueRW = new MinerVesselRegistry
            {
                TotalVessels = total,
                AvailableVessels = available,
                IdleVessels = idle,
                BusyVessels = busy,
                TotalCapacity = totalCapacity,
                TotalLoad = totalLoad,
                LastUpdateTick = timeState.Tick,
                LastSpatialVersion = spatialVersionSource,
                SpatialResolvedCount = resolved,
                SpatialFallbackCount = fallback,
                SpatialUnmappedCount = unmapped
            };
        }

        private void UpdateHaulerRegistry(ref SystemState state, in TimeState timeState, bool hasSpatialGrid, bool hasSpatialSync, in SpatialGridConfig spatialConfig, in SpatialGridState spatialState, in RegistrySpatialSyncState spatialSyncState)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<HaulerRegistry>();
            var registry = SystemAPI.GetComponentRW<HaulerRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<HaulerRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;

            var expected = math.max(8, _haulerQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<HaulerRegistryEntry>(expected, Allocator.Temp);

            var total = 0;
            var idle = 0;
            var assigned = 0;
            var totalReserved = 0f;
            var travelAccumulator = 0f;
            var travelSamples = 0;
            var resolved = 0;
            var fallback = 0;
            var unmapped = 0;

            var spatialVersionSource = hasSpatialGrid
                ? spatialState.Version
                : (hasSpatialSync && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSync && spatialSyncState.HasSpatialData;

            foreach (var (hauler, transform, entity) in SystemAPI.Query<RefRO<Hauler>, RefRO<LocalTransform>>().WithNone<PlaybackGuardTag>().WithEntityAccess())
            {
                var position = transform.ValueRO.Position;
                var cellId = -1;
                uint spatialVersion = spatialVersionSource;
                if (hasSpatialGrid)
                {
                    ClassifySpatialData(entity, position, true, in spatialConfig, spatialState.Version, ref cellId, ref spatialVersion, ref resolved, ref fallback, ref unmapped);
                }

                builder.Add(new HaulerRegistryEntry
                {
                    HaulerEntity = entity,
                    Position = position,
                    CellId = cellId,
                    SpatialVersion = spatialVersion,
                    CargoTypeIndex = hauler.ValueRO.CargoTypeIndex,
                    ReservedCapacity = hauler.ValueRO.ReservedCapacity,
                    EstimatedTravelTime = hauler.ValueRO.EstimatedTravelTime,
                    RouteId = hauler.ValueRO.RouteId,
                    Flags = hauler.ValueRO.Flags
                });

                total++;
                if ((hauler.ValueRO.Flags & TransportUnitFlags.Idle) != 0)
                {
                    idle++;
                }
                else if ((hauler.ValueRO.Flags & (TransportUnitFlags.Assigned | TransportUnitFlags.Carrying)) != 0)
                {
                    assigned++;
                }

                totalReserved += hauler.ValueRO.ReservedCapacity;
                travelAccumulator += hauler.ValueRO.EstimatedTravelTime;
                travelSamples++;
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolved, fallback, unmapped, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref entries, ref metadata, timeState.Tick, continuity);

            registry.ValueRW = new HaulerRegistry
            {
                TotalHaulers = total,
                IdleHaulers = idle,
                AssignedHaulers = assigned,
                TotalReservedCapacity = totalReserved,
                AverageTravelTime = travelSamples > 0 ? travelAccumulator / travelSamples : 0f,
                LastUpdateTick = timeState.Tick,
                LastSpatialVersion = spatialVersionSource,
                SpatialResolvedCount = resolved,
                SpatialFallbackCount = fallback,
                SpatialUnmappedCount = unmapped
            };
        }

        private void UpdateFreighterRegistry(ref SystemState state, in TimeState timeState, bool hasSpatialGrid, bool hasSpatialSync, in SpatialGridConfig spatialConfig, in SpatialGridState spatialState, in RegistrySpatialSyncState spatialSyncState)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<FreighterRegistry>();
            var registry = SystemAPI.GetComponentRW<FreighterRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<FreighterRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;

            var expected = math.max(4, _freighterQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<FreighterRegistryEntry>(expected, Allocator.Temp);

            var total = 0;
            var active = 0;
            var idle = 0;
            var totalCapacity = 0f;
            var totalPayload = 0f;
            var resolved = 0;
            var fallback = 0;
            var unmapped = 0;

            var spatialVersionSource = hasSpatialGrid
                ? spatialState.Version
                : (hasSpatialSync && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSync && spatialSyncState.HasSpatialData;

            foreach (var (freighter, transform, entity) in SystemAPI.Query<RefRO<Freighter>, RefRO<LocalTransform>>().WithNone<PlaybackGuardTag>().WithEntityAccess())
            {
                var position = transform.ValueRO.Position;
                var cellId = -1;
                uint spatialVersion = spatialVersionSource;
                if (hasSpatialGrid)
                {
                    ClassifySpatialData(entity, position, true, in spatialConfig, spatialState.Version, ref cellId, ref spatialVersion, ref resolved, ref fallback, ref unmapped);
                }

                builder.Add(new FreighterRegistryEntry
                {
                    FreighterEntity = entity,
                    Position = position,
                    CellId = cellId,
                    SpatialVersion = spatialVersion,
                    Destination = freighter.ValueRO.Destination,
                    ManifestId = freighter.ValueRO.ManifestId,
                    PayloadCapacity = freighter.ValueRO.PayloadCapacity,
                    PayloadLoaded = freighter.ValueRO.PayloadLoaded,
                    Flags = freighter.ValueRO.Flags
                });

                total++;
                if ((freighter.ValueRO.Flags & (TransportUnitFlags.Assigned | TransportUnitFlags.Carrying)) != 0)
                {
                    active++;
                }
                else
                {
                    idle++;
                }

                totalCapacity += freighter.ValueRO.PayloadCapacity;
                totalPayload += freighter.ValueRO.PayloadLoaded;
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolved, fallback, unmapped, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref entries, ref metadata, timeState.Tick, continuity);

            registry.ValueRW = new FreighterRegistry
            {
                TotalFreighters = total,
                ActiveFreighters = active,
                IdleFreighters = idle,
                TotalPayloadCapacity = totalCapacity,
                TotalPayloadLoaded = totalPayload,
                LastUpdateTick = timeState.Tick,
                LastSpatialVersion = spatialVersionSource,
                SpatialResolvedCount = resolved,
                SpatialFallbackCount = fallback,
                SpatialUnmappedCount = unmapped
            };
        }

        private void UpdateWagonRegistry(ref SystemState state, in TimeState timeState, bool hasSpatialGrid, bool hasSpatialSync, in SpatialGridConfig spatialConfig, in SpatialGridState spatialState, in RegistrySpatialSyncState spatialSyncState)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<WagonRegistry>();
            var registry = SystemAPI.GetComponentRW<WagonRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<WagonRegistryEntry>(registryEntity);
            ref var metadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;

            var expected = math.max(4, _wagonQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<WagonRegistryEntry>(expected, Allocator.Temp);

            var total = 0;
            var available = 0;
            var assigned = 0;
            var totalCapacity = 0f;
            var totalReserved = 0f;
            var resolved = 0;
            var fallback = 0;
            var unmapped = 0;

            var spatialVersionSource = hasSpatialGrid
                ? spatialState.Version
                : (hasSpatialSync && spatialSyncState.HasSpatialData ? spatialSyncState.SpatialVersion : 0u);
            var requireSpatialSync = metadata.SupportsSpatialQueries && hasSpatialSync && spatialSyncState.HasSpatialData;

            foreach (var (wagon, transform, entity) in SystemAPI.Query<RefRO<Wagon>, RefRO<LocalTransform>>().WithNone<PlaybackGuardTag>().WithEntityAccess())
            {
                var position = transform.ValueRO.Position;
                var cellId = -1;
                uint spatialVersion = spatialVersionSource;
                if (hasSpatialGrid)
                {
                    ClassifySpatialData(entity, position, true, in spatialConfig, spatialState.Version, ref cellId, ref spatialVersion, ref resolved, ref fallback, ref unmapped);
                }

                builder.Add(new WagonRegistryEntry
                {
                    WagonEntity = entity,
                    AssignedVillager = wagon.ValueRO.AssignedVillager,
                    Position = position,
                    CellId = cellId,
                    SpatialVersion = spatialVersion,
                    CargoCapacity = wagon.ValueRO.CargoCapacity,
                    CargoReserved = wagon.ValueRO.CargoReserved,
                    Flags = wagon.ValueRO.Flags
                });

                total++;
                totalCapacity += wagon.ValueRO.CargoCapacity;
                totalReserved += wagon.ValueRO.CargoReserved;
                if ((wagon.ValueRO.Flags & TransportUnitFlags.Idle) != 0)
                {
                    available++;
                }
                else if ((wagon.ValueRO.Flags & (TransportUnitFlags.Assigned | TransportUnitFlags.Carrying)) != 0)
                {
                    assigned++;
                }
            }

            var continuity = hasSpatialGrid
                ? RegistryContinuitySnapshot.WithSpatialData(spatialVersionSource, resolved, fallback, unmapped, requireSpatialSync)
                : RegistryContinuitySnapshot.WithoutSpatialData(requireSpatialSync);

            builder.ApplyTo(ref entries, ref metadata, timeState.Tick, continuity);

            registry.ValueRW = new WagonRegistry
            {
                TotalWagons = total,
                AvailableWagons = available,
                AssignedWagons = assigned,
                TotalCargoCapacity = totalCapacity,
                TotalCargoReserved = totalReserved,
                LastUpdateTick = timeState.Tick,
                LastSpatialVersion = spatialVersionSource,
                SpatialResolvedCount = resolved,
                SpatialFallbackCount = fallback,
                SpatialUnmappedCount = unmapped
            };
        }

        private void ClassifySpatialData(Entity entity, float3 position, bool hasSpatialGrid, in SpatialGridConfig config, uint gridVersion, ref int cellId, ref uint spatialVersion, ref int resolved, ref int fallback, ref int unmapped)
        {
            if (!hasSpatialGrid)
            {
                cellId = -1;
                spatialVersion = 0;
                return;
            }

            if (_residencyLookup.HasComponent(entity))
            {
                var residency = _residencyLookup[entity];
                if ((uint)residency.CellId < (uint)config.CellCount && residency.Version == gridVersion)
                {
                    cellId = residency.CellId;
                    spatialVersion = residency.Version;
                    resolved++;
                    return;
                }
            }

            SpatialHash.Quantize(position, config, out var coords);
            var computedCell = SpatialHash.Flatten(in coords, in config);
            if ((uint)computedCell < (uint)config.CellCount)
            {
                cellId = computedCell;
                spatialVersion = gridVersion;
                fallback++;
            }
            else
            {
                cellId = -1;
                spatialVersion = 0;
                unmapped++;
            }
        }
    }
}



