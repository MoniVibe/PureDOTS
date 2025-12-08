using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using PureDOTS.Runtime.Threading;
using PureDOTS.Core;
using PureDOTS.Config;
using PureDOTS.Runtime.BlobAssets;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Scenario;
using PureDOTS.AI.MindECS;
using PureDOTS.AI.AggregateECS;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Custom bootstrap that creates all simulation worlds (Body, Mind, Aggregate, Presentation).
    /// Establishes the root system groups and appends worlds to the player loop.
    /// </summary>
    public sealed class PureDotsWorldBootstrap : ICustomBootstrap
    {
        private static readonly ProfilerMarker InitializeMarker = new("PureDOTS.Bootstrap.Initialize");
        private static readonly ProfilerMarker ConfigureGroupsMarker = new("PureDOTS.Bootstrap.ConfigureRootGroups");
        private static readonly ProfilerMarker MaterializeGroupsMarker = new("PureDOTS.Bootstrap.MaterializeGroups");

        public bool Initialize(string defaultWorldName)
        {
#if UNITY_EDITOR
            // Guard: Skip world creation during domain reload/compilation to prevent editor freeze
            if (!Application.isPlaying && (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating))
            {
                return false; // Let Unity use default bootstrap during reload
            }
#endif

            using (InitializeMarker.Auto())
            {
                InitializeRuntime();
            }

            return true;
        }

        /// <summary>
        /// Minimal bootstrap order: create worlds, load configs, register cores, load scenario, start simulation.
        /// </summary>
        private static void InitializeRuntime()
        {
            // 1. Create simulation worlds
            var bodyWorld = WorldUtility.CreateWorld<BodyECSWorld>("BodyWorld");
            World.DefaultGameObjectInjectionWorld = bodyWorld;

            // Initialize Body world systems
            var profile = SystemRegistry.ResolveActiveProfile();
            var systems = SystemRegistry.GetSystems(profile);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(bodyWorld, systems);

            // Ensure EntityCommandBuffer systems exist
            bodyWorld.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            bodyWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            ConfigureRootGroups(bodyWorld);
            InitializeThreadingInfrastructure(bodyWorld);
            MaterializeSystemGroups(bodyWorld);

            // 2. Load configs
            var config = ConfigLoader.Load<SimConfig>("Configs/Sim.json");
            if (config.FixedDeltaTime <= 0f)
            {
                config = SimConfig.Default;
            }

            // 3. Register cores
            CoreSystems.RegisterTimeSpine(bodyWorld, config.FixedDeltaTime);
            CoreSystems.RegisterSpatialGrid(bodyWorld, CreateDefaultSpatialGridConfig());
            CoreSystems.RegisterCommunicationBus(bodyWorld);
            CoreSystems.RegisterRewind(bodyWorld, config.RewindBufferSeconds);

            // 4. Inject shared blobs (materials, skills, doctrines)
            BlobRegistry.Initialize(bodyWorld);

            // 5. Initialize Mind and Aggregate worlds with communication bus
            // Ensure coordinator exists first
            bodyWorld.GetOrCreateSystemManaged<AgentSyncBridgeCoordinator>();
            var bus = AgentSyncBridgeCoordinator.GetBusFromWorld(bodyWorld);
            
            if (bus != null)
            {
                MindECSWorld.Initialize(bus);
                AggregateECSWorld.Initialize(bus);
            }

            var mindWorld = MindECSWorld.Instance;
            var aggregateWorld = AggregateECSWorld.Instance;

            // Wire inter-world communication
            if (mindWorld != null)
            {
                CommunicationBus.Connect(bodyWorld, mindWorld.World);
            }
            if (aggregateWorld != null)
            {
                CommunicationBus.Connect(bodyWorld, aggregateWorld.World);
            }

            // 6. Load demo scenario
            ScenarioRunner.LoadScenario(bodyWorld, "Scenarios/Demo_Awakening.json");

            // 7. Start simulation
            SimulationRunner.StartAllWorlds(bodyWorld, mindWorld, aggregateWorld, config.MindTickRate, config.AggregateTickRate);

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(bodyWorld);

            var mindCount = mindWorld != null ? mindWorld.World.EntityCount : 0;
            var aggregateCount = aggregateWorld != null ? aggregateWorld.World.EntityCount : 0;
            UnityEngine.Debug.Log($"[PureDotsWorldBootstrap] All worlds initialized - Body: {bodyWorld.Name}, Mind: {mindCount}, Aggregate: {aggregateCount}");
        }

        private static SpatialGridConfig CreateDefaultSpatialGridConfig()
        {
            return new SpatialGridConfig
            {
                CellSize = 4f,
                WorldMin = new Unity.Mathematics.float3(-512f, -64f, -512f),
                WorldMax = new Unity.Mathematics.float3(512f, 64f, 512f),
                CellCounts = new Unity.Mathematics.int3(256, 32, 256),
                HashSeed = 0u,
                ProviderId = 0
            };
        }

        private static void MaterializeSystemGroups(World world)
        {
            using (MaterializeGroupsMarker.Auto())
            {
                var cameraInputGroup = world.GetOrCreateSystemManaged<CameraInputSystemGroup>();
                var cameraPhaseGroup = world.GetOrCreateSystemManaged<CameraPhaseGroup>();
                var environmentGroup = world.GetOrCreateSystemManaged<EnvironmentSystemGroup>();
                var spatialGroup = world.GetOrCreateSystemManaged<SpatialSystemGroup>();
                var transportPhaseGroup = world.GetOrCreateSystemManaged<TransportPhaseGroup>();
                var historyPhaseGroup = world.GetOrCreateSystemManaged<HistoryPhaseGroup>();
                var gameplayGroup = world.GetOrCreateSystemManaged<GameplaySystemGroup>();

                world.GetOrCreateSystemManaged<TimeSystemGroup>();
                world.GetOrCreateSystemManaged<VillagerSystemGroup>();
                world.GetOrCreateSystemManaged<ResourceSystemGroup>();
                world.GetOrCreateSystemManaged<PowerSystemGroup>();
                world.GetOrCreateSystemManaged<MiracleEffectSystemGroup>();
                world.GetOrCreateSystemManaged<CombatSystemGroup>();
                world.GetOrCreateSystemManaged<HandSystemGroup>();
                world.GetOrCreateSystemManaged<VegetationSystemGroup>();
                world.GetOrCreateSystemManaged<ConstructionSystemGroup>();
                world.GetOrCreateSystemManaged<HistorySystemGroup>();

                cameraInputGroup.SortSystems();
                cameraPhaseGroup.SortSystems();
                environmentGroup.SortSystems();
                spatialGroup.SortSystems();
                transportPhaseGroup.SortSystems();
                gameplayGroup.SortSystems();
                historyPhaseGroup.SortSystems();
            }
        }

        private static void ConfigureRootGroups(World world)
        {
            using (ConfigureGroupsMarker.Auto())
            {
                var initializationGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
                initializationGroup.SortSystems();

                if (world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>() is { } fixedStepGroup)
                {
                    fixedStepGroup.SortSystems();
                }

                var simulationGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
                simulationGroup.SortSystems();

                if (world.GetExistingSystemManaged<Unity.Entities.PresentationSystemGroup>() is { } presentationGroup)
                {
                    world.GetOrCreateSystemManaged<BeginPresentationECBSystem>();
                    world.GetOrCreateSystemManaged<EndPresentationECBSystem>();
                    presentationGroup.SortSystems();
                }
            }
        }

        private static void InitializeThreadingInfrastructure(World world)
        {
            var entityManager = world.EntityManager;

            // Create ThreadingConfig singleton
            using var configQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<ThreadingConfig>());
            if (configQuery.IsEmptyIgnoreFilter)
            {
                var configEntity = entityManager.CreateEntity(typeof(ThreadingConfig));
                entityManager.SetComponentData(configEntity, ThreadingConfig.Default);
            }
            else
            {
                var configEntity = configQuery.GetSingletonEntity();
                entityManager.SetComponentData(configEntity, ThreadingConfig.Default);
            }

            // Initialize thread affinity manager
            var config = ThreadingConfig.Default;
            ThreadAffinityManager.Initialize(config);

            // Create ThreadLoadState singleton
            using var loadStateQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<ThreadLoadState>());
            if (loadStateQuery.IsEmptyIgnoreFilter)
            {
                var loadStateEntity = entityManager.CreateEntity(typeof(ThreadLoadState));
                entityManager.SetComponentData(loadStateEntity, new ThreadLoadState
                {
                    LastRebalanceTick = 0,
                    RebalanceIntervalTicks = 60, // 1 second at 60Hz
                    CurrentImbalanceRatio = 0f
                });
            }

            UnityEngine.Debug.Log("[PureDotsWorldBootstrap] Threading infrastructure initialized.");
        }
    }
}
