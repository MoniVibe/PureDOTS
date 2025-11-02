using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Transport;
using PureDOTS.Runtime.Villager;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Ensures the core deterministic singletons exist even without authoring data.
    /// Runs once at startup so downstream systems can safely require these components.
    /// </summary>
    [UpdateInGroup(typeof(TimeSystemGroup), OrderFirst = true)]
    public partial struct CoreSingletonBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            EnsureSingletons(state.EntityManager);
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            // No-op; this system only seeds singleton entities on create.
        }

        public static void EnsureSingletons(EntityManager entityManager)
        {
            Entity timeEntity;
            using (var timeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()))
            {
                if (timeQuery.IsEmptyIgnoreFilter)
                {
                    timeEntity = entityManager.CreateEntity(typeof(TimeState));
                    entityManager.SetComponentData(timeEntity, new TimeState
                    {
                        FixedDeltaTime = TimeSettingsDefaults.FixedDeltaTime,
                        CurrentSpeedMultiplier = TimeSettingsDefaults.DefaultSpeedMultiplier,
                        Tick = 0,
                        IsPaused = TimeSettingsDefaults.PauseOnStart
                    });
                }
                else
                {
                    timeEntity = timeQuery.GetSingletonEntity();
                }
            }

            if (!entityManager.HasComponent<GameplayFixedStep>(timeEntity))
            {
                entityManager.AddComponentData(timeEntity, new GameplayFixedStep
                {
                    FixedDeltaTime = TimeSettingsDefaults.FixedDeltaTime
                });
            }
            else
            {
                var fixedStep = entityManager.GetComponentData<GameplayFixedStep>(timeEntity);
                if (fixedStep.FixedDeltaTime <= 0f)
                {
                    fixedStep.FixedDeltaTime = TimeSettingsDefaults.FixedDeltaTime;
                    entityManager.SetComponentData(timeEntity, fixedStep);
                }
            }

            using (var historyQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<HistorySettings>()))
            {
                if (historyQuery.IsEmptyIgnoreFilter)
                {
                    var entity = entityManager.CreateEntity(typeof(HistorySettings));
                    entityManager.SetComponentData(entity, HistorySettingsDefaults.CreateDefault());
                }
            }

            Entity rewindEntity;
            using (var rewindQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()))
            {
                if (rewindQuery.IsEmptyIgnoreFilter)
                {
                    rewindEntity = entityManager.CreateEntity(typeof(RewindState));
                    entityManager.SetComponentData(rewindEntity, new RewindState
                    {
                        Mode = RewindMode.Record,
                        StartTick = 0,
                        TargetTick = 0,
                        PlaybackTick = 0,
                        PlaybackTicksPerSecond = HistorySettingsDefaults.DefaultTicksPerSecond,
                        ScrubDirection = 0,
                        ScrubSpeedMultiplier = 1f
                    });
                }
                else
                {
                    rewindEntity = rewindQuery.GetSingletonEntity();
                }
            }

            if (!entityManager.HasBuffer<TimeControlCommand>(rewindEntity))
            {
                entityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            }

            EnsureRegistry<ResourceRegistry, ResourceRegistryEntry>(entityManager, RegistryKind.Resource, "ResourceRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);
            EnsureRegistry<StorehouseRegistry, StorehouseRegistryEntry>(entityManager, RegistryKind.Storehouse, "StorehouseRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);
            EnsureRegistry<VillagerRegistry, VillagerRegistryEntry>(entityManager, RegistryKind.Villager, "VillagerRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);
            EnsureRegistry<MiracleRegistry, MiracleRegistryEntry>(entityManager, RegistryKind.Miracle, "MiracleRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            // Game-specific transport registries (MinerVessel, Hauler, Freighter, Wagon) are now created by Space4X.Systems.TransportBootstrapSystem
            EnsureRegistry<CreatureRegistry, CreatureRegistryEntry>(entityManager, RegistryKind.Creature, "CreatureRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);
            EnsureRegistry<ConstructionRegistry, ConstructionRegistryEntry>(entityManager, RegistryKind.Construction, "ConstructionRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<LogisticsRequestRegistry, LogisticsRequestRegistryEntry>(entityManager, RegistryKind.LogisticsRequest, "LogisticsRequestRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);
            EnsureRegistry<BandRegistry, BandRegistryEntry>(entityManager, RegistryKind.Band, "BandRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);
            EnsureRegistry<AbilityRegistry, AbilityRegistryEntry>(entityManager, RegistryKind.Ability, "AbilityRegistry", RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<SpawnerRegistry, SpawnerRegistryEntry>(entityManager, RegistryKind.Spawner, "SpawnerRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);

            EnsureAICommandQueue(entityManager);

            EnsureSpatialGridSingleton(entityManager);
            EnsureSpatialProviderRegistry(entityManager);

            EnsureRegistryDirectory(entityManager);
            EnsureRegistrySpatialSyncState(entityManager);

            EnsureTelemetryStream(entityManager);
            EnsureFrameTimingStream(entityManager);
            EnsureReplayCaptureStream(entityManager);
            EnsureRegistryHealthConfig(entityManager);

            // For compatibility with previous behaviour, ensure the system would be disabled after seeding.
        }

        private static void EnsureRegistrySpatialSyncState(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistrySpatialSyncState>());
            Entity syncEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                syncEntity = entityManager.CreateEntity(typeof(RegistrySpatialSyncState));
            }
            else
            {
                syncEntity = query.GetSingletonEntity();
            }

            EnsureBuffer<RegistryContinuityAlert>(entityManager, syncEntity);

            if (!entityManager.HasComponent<RegistryContinuityState>(syncEntity))
            {
                entityManager.AddComponentData(syncEntity, new RegistryContinuityState
                {
                    Version = 0,
                    LastCheckTick = 0,
                    WarningCount = 0,
                    FailureCount = 0
                });
            }
        }

        private static void EnsureRegistry<TComponent, TBuffer>(EntityManager entityManager, RegistryKind kind, FixedString64Bytes label, RegistryHandleFlags flags)
            where TComponent : unmanaged, IComponentData
            where TBuffer : unmanaged, IBufferElementData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TComponent>());
            Entity registryEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                registryEntity = entityManager.CreateEntity(typeof(TComponent));
            }
            else
            {
                registryEntity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<TBuffer>(registryEntity))
            {
                entityManager.AddBuffer<TBuffer>(registryEntity);
            }

            if (!entityManager.HasComponent<RegistryMetadata>(registryEntity))
            {
                var metadata = new RegistryMetadata();
                metadata.Initialise(kind, 0, flags, label);
                entityManager.AddComponentData(registryEntity, metadata);
            }
            else
            {
                var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
                if (metadata.Kind == RegistryKind.Unknown && metadata.Version == 0 && metadata.EntryCount == 0)
                {
                    metadata.Initialise(kind, metadata.ArchetypeId, flags, label);
                    entityManager.SetComponentData(registryEntity, metadata);
                }
            }

            if (!entityManager.HasComponent<RegistryHealth>(registryEntity))
            {
                entityManager.AddComponentData(registryEntity, default(RegistryHealth));
            }
        }

        private static void EnsureAICommandQueue(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<AICommandQueueTag>());
            Entity queueEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                queueEntity = entityManager.CreateEntity(typeof(AICommandQueueTag));
            }
            else
            {
                queueEntity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<AICommand>(queueEntity))
            {
                entityManager.AddBuffer<AICommand>(queueEntity);
            }
        }

        private static void EnsureSpatialGridSingleton(EntityManager entityManager)
        {
            Entity gridEntity;
            using (var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()))
            {
                if (query.IsEmptyIgnoreFilter)
                {
                    gridEntity = entityManager.CreateEntity(typeof(SpatialGridConfig), typeof(SpatialGridState));
                    entityManager.SetComponentData(gridEntity, CreateDefaultSpatialConfig());
                    entityManager.SetComponentData(gridEntity, new SpatialGridState
                    {
                        ActiveBufferIndex = 0,
                        TotalEntries = 0,
                        Version = 0,
                        LastUpdateTick = 0,
                        LastDirtyTick = 0,
                        DirtyVersion = 0,
                        DirtyAddCount = 0,
                        DirtyUpdateCount = 0,
                        DirtyRemoveCount = 0,
                        LastRebuildMilliseconds = 0f,
                        LastStrategy = SpatialGridRebuildStrategy.None
                    });
                }
                else
                {
                    gridEntity = query.GetSingletonEntity();
                    if (!entityManager.HasComponent<SpatialGridState>(gridEntity))
                    {
                        entityManager.AddComponentData(gridEntity, new SpatialGridState
                        {
                            ActiveBufferIndex = 0,
                            TotalEntries = 0,
                            Version = 0,
                            LastUpdateTick = 0,
                            LastDirtyTick = 0,
                            DirtyVersion = 0,
                            DirtyAddCount = 0,
                            DirtyUpdateCount = 0,
                            DirtyRemoveCount = 0,
                            LastRebuildMilliseconds = 0f,
                            LastStrategy = SpatialGridRebuildStrategy.None
                        });
                    }

                    if (!entityManager.HasComponent<SpatialRebuildThresholds>(gridEntity))
                    {
                        entityManager.AddComponentData(gridEntity, SpatialRebuildThresholds.CreateDefaults());
                    }
                }
            }

            EnsureBuffer<SpatialGridCellRange>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridEntry>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridStagingEntry>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridStagingCellRange>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridEntryLookup>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridDirtyOp>(entityManager, gridEntity);

            if (!entityManager.HasComponent<SpatialRegistryMetadata>(gridEntity))
            {
                entityManager.AddComponentData(gridEntity, default(SpatialRegistryMetadata));
            }
        }

        private static void EnsureRegistryDirectory(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>());
            Entity directoryEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                directoryEntity = entityManager.CreateEntity(typeof(RegistryDirectory));
                entityManager.SetComponentData(directoryEntity, new RegistryDirectory
                {
                    Version = 0,
                    LastUpdateTick = 0,
                    AggregateHash = 0
                });
            }
            else
            {
                directoryEntity = query.GetSingletonEntity();
            }

            EnsureBuffer<RegistryDirectoryEntry>(entityManager, directoryEntity);
            EnsureBuffer<RegistryInstrumentationSample>(entityManager, directoryEntity);

            if (!entityManager.HasComponent<RegistryInstrumentationState>(directoryEntity))
            {
                entityManager.AddComponentData(directoryEntity, new RegistryInstrumentationState
                {
                    Version = 0,
                    LastUpdateTick = 0,
                    SampleCount = 0
                });
            }
        }

        private static void EnsureTelemetryStream(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TelemetryStream>());
            Entity telemetryEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                telemetryEntity = entityManager.CreateEntity(typeof(TelemetryStream));
                entityManager.SetComponentData(telemetryEntity, new TelemetryStream
                {
                    Version = 0,
                    LastTick = 0
                });
            }
            else
            {
                telemetryEntity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                entityManager.AddBuffer<TelemetryMetric>(telemetryEntity);
            }
        }

        private static void EnsureFrameTimingStream(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FrameTimingStream>());
            Entity frameEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                frameEntity = entityManager.CreateEntity(typeof(FrameTimingStream));
                entityManager.SetComponentData(frameEntity, new FrameTimingStream
                {
                    Version = 0,
                    LastTick = 0
                });
            }
            else
            {
                frameEntity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<FrameTimingSample>(frameEntity))
            {
                entityManager.AddBuffer<FrameTimingSample>(frameEntity);
            }

            if (!entityManager.HasComponent<AllocationDiagnostics>(frameEntity))
            {
                entityManager.AddComponentData(frameEntity, new AllocationDiagnostics());
            }
        }

        private static void EnsureReplayCaptureStream(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ReplayCaptureStream>());
            Entity replayEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                replayEntity = entityManager.CreateEntity(typeof(ReplayCaptureStream));
                entityManager.SetComponentData(replayEntity, new ReplayCaptureStream
                {
                    Version = 0,
                    LastTick = 0,
                    EventCount = 0,
                    LastEventType = ReplayableEvent.EventType.Custom,
                    LastEventLabel = default
                });
            }
            else
            {
                replayEntity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<ReplayCaptureEvent>(replayEntity))
            {
                entityManager.AddBuffer<ReplayCaptureEvent>(replayEntity);
            }
        }

        private static void EnsureSpatialProviderRegistry(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialProviderRegistry>());
            Entity registryEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                registryEntity = entityManager.CreateEntity(typeof(SpatialProviderRegistry));
                entityManager.AddBuffer<SpatialProviderRegistryEntry>(registryEntity);
                entityManager.SetComponentData(registryEntity, new SpatialProviderRegistry
                {
                    NextProviderId = 2,
                    Version = 0
                });
            }
        }

        private static void EnsureVillagerBehaviorConfig(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerBehaviorConfig>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = entityManager.CreateEntity(typeof(VillagerBehaviorConfig));
                entityManager.SetComponentData(entity, VillagerBehaviorConfig.CreateDefaults());
            }
        }

        private static void EnsureResourceInteractionConfig(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceInteractionConfig>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = entityManager.CreateEntity(typeof(ResourceInteractionConfig));
                entityManager.SetComponentData(entity, ResourceInteractionConfig.CreateDefaults());
            }
        }

        private static void EnsureRegistryHealthConfig(EntityManager entityManager)
        {
            using var monitoringQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryHealthMonitoring>());

            if (monitoringQuery.IsEmptyIgnoreFilter)
            {
                var entity = entityManager.CreateEntity(typeof(RegistryHealthMonitoring), typeof(RegistryHealthThresholds));
                entityManager.SetComponentData(entity, RegistryHealthMonitoring.CreateDefaults());
                entityManager.SetComponentData(entity, RegistryHealthThresholds.CreateDefaults());
                
                // Ensure villager behavior config singleton
                EnsureVillagerBehaviorConfig(entityManager);
                
                // Ensure resource interaction config singleton
                EnsureResourceInteractionConfig(entityManager);
                return;
            }

            var monitoringEntity = monitoringQuery.GetSingletonEntity();

            if (!entityManager.HasComponent<RegistryHealthThresholds>(monitoringEntity))
            {
                entityManager.AddComponentData(monitoringEntity, RegistryHealthThresholds.CreateDefaults());
            }
        }

        private static SpatialGridConfig CreateDefaultSpatialConfig()
        {
            return new SpatialGridConfig
            {
                CellSize = 4f,
                WorldMin = new float3(-512f, -64f, -512f),
                WorldMax = new float3(512f, 64f, 512f),
                CellCounts = new int3(256, 32, 256),
                HashSeed = 0u,
                ProviderId = 0
            };
        }

        private static void EnsureBuffer<TBuffer>(EntityManager entityManager, Entity entity)
            where TBuffer : unmanaged, IBufferElementData
        {
            if (!entityManager.HasBuffer<TBuffer>(entity))
            {
                entityManager.AddBuffer<TBuffer>(entity);
            }
        }
    }
}
