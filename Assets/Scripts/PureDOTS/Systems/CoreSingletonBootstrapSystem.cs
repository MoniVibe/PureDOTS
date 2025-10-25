using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Transport;
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
            EnsureRegistry<MinerVesselRegistry, MinerVesselRegistryEntry>(entityManager, RegistryKind.MinerVessel, "MinerVesselRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<HaulerRegistry, HaulerRegistryEntry>(entityManager, RegistryKind.Hauler, "HaulerRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<FreighterRegistry, FreighterRegistryEntry>(entityManager, RegistryKind.Freighter, "FreighterRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<WagonRegistry, WagonRegistryEntry>(entityManager, RegistryKind.Wagon, "WagonRegistry", RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);

            EnsureAICommandQueue(entityManager);

            EnsureSpatialGridSingleton(entityManager);

            EnsureRegistryDirectory(entityManager);

            // For compatibility with previous behaviour, ensure the system would be disabled after seeding.
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
                        LastUpdateTick = 0
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
                            LastUpdateTick = 0
                        });
                    }
                }
            }

            EnsureBuffer<SpatialGridCellRange>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridEntry>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridStagingEntry>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridStagingCellRange>(entityManager, gridEntity);

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

            if (!entityManager.HasBuffer<RegistryDirectoryEntry>(directoryEntity))
            {
                entityManager.AddBuffer<RegistryDirectoryEntry>(directoryEntity);
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
