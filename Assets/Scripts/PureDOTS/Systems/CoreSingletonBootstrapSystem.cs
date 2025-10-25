using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
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
            using (var timeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()))
            {
                if (timeQuery.IsEmptyIgnoreFilter)
                {
                    var entity = entityManager.CreateEntity(typeof(TimeState));
                    entityManager.SetComponentData(entity, new TimeState
                    {
                        FixedDeltaTime = TimeSettingsDefaults.FixedDeltaTime,
                        CurrentSpeedMultiplier = TimeSettingsDefaults.DefaultSpeedMultiplier,
                        Tick = 0,
                        IsPaused = TimeSettingsDefaults.PauseOnStart
                    });
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

            EnsureRegistry<ResourceRegistry, ResourceRegistryEntry>(entityManager);
            EnsureRegistry<StorehouseRegistry, StorehouseRegistryEntry>(entityManager);
            EnsureRegistry<VillagerRegistry, VillagerRegistryEntry>(entityManager);

            EnsureSpatialGridSingleton(entityManager);

            // For compatibility with previous behaviour, ensure the system would be disabled after seeding.
        }

        private static void EnsureRegistry<TComponent, TBuffer>(EntityManager entityManager)
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
                        Version = 0
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
                            Version = 0
                        });
                    }
                }
            }

            EnsureBuffer<SpatialGridCellRange>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridEntry>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridStagingEntry>(entityManager, gridEntity);
            EnsureBuffer<SpatialGridStagingCellRange>(entityManager, gridEntity);
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
