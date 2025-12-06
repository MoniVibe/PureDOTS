using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Systems;

namespace PureDOTS.Core
{
    /// <summary>
    /// Registers core simulation systems during bootstrap.
    /// </summary>
    public static class CoreSystems
    {
        /// <summary>
        /// Registers time spine with fixed delta time.
        /// </summary>
        public static void RegisterTimeSpine(World world, float fixedDeltaTime)
        {
            if (world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>() is { } fixedStepGroup)
            {
                fixedStepGroup.Timestep = fixedDeltaTime;
            }

            // Ensure time singletons exist
            CoreSingletonBootstrapSystem.EnsureSingletons(world.EntityManager);
        }

        /// <summary>
        /// Registers spatial grid with grid specification.
        /// </summary>
        public static void RegisterSpatialGrid(World world, SpatialGridConfig gridSpec)
        {
            var entityManager = world.EntityManager;
            
            // Create spatial grid config if it doesn't exist
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>());
            if (query.IsEmptyIgnoreFilter)
            {
                var gridEntity = entityManager.CreateEntity(typeof(SpatialGridConfig));
                entityManager.SetComponentData(gridEntity, gridSpec);
            }
            else
            {
                var gridEntity = query.GetSingletonEntity();
                entityManager.SetComponentData(gridEntity, gridSpec);
            }
        }

        /// <summary>
        /// Registers communication bus (AgentSyncBus).
        /// </summary>
        public static void RegisterCommunicationBus(World world)
        {
            // AgentSyncBridgeCoordinator system will initialize the bus
            // Just ensure it exists in the world
            world.GetOrCreateSystemManaged<AgentSyncBridgeCoordinator>();
        }

        /// <summary>
        /// Registers rewind system with buffer size in seconds.
        /// </summary>
        public static void RegisterRewind(World world, int bufferSeconds)
        {
            var entityManager = world.EntityManager;
            
            // Ensure HistorySettings exists with buffer size
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<HistorySettings>());
            Entity historyEntity;
            
            if (query.IsEmptyIgnoreFilter)
            {
                historyEntity = entityManager.CreateEntity(typeof(HistorySettings));
                var settings = HistorySettingsDefaults.CreateDefault();
                settings.DefaultHorizonSeconds = bufferSeconds;
                entityManager.SetComponentData(historyEntity, settings);
            }
            else
            {
                historyEntity = query.GetSingletonEntity();
                var settings = entityManager.GetComponentData<HistorySettings>(historyEntity);
                settings.DefaultHorizonSeconds = bufferSeconds;
                entityManager.SetComponentData(historyEntity, settings);
            }
        }
    }
}

