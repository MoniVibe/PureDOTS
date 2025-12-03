using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Custom bootstrap that creates the single simulation world we use for the pure DOTS run.
    /// Establishes the root system groups and appends the world to the player loop so no
    /// MonoBehaviour bootstrap is required.
    /// </summary>
    public sealed class PureDotsWorldBootstrap : ICustomBootstrap
    {
        private static readonly ProfilerMarker InitializeMarker = new("PureDOTS.Bootstrap.Initialize");
        private static readonly ProfilerMarker ConfigureGroupsMarker = new("PureDOTS.Bootstrap.ConfigureRootGroups");
        private static readonly ProfilerMarker MaterializeGroupsMarker = new("PureDOTS.Bootstrap.MaterializeGroups");

        public bool Initialize(string defaultWorldName)
        {
            using (InitializeMarker.Auto())
            {
                var profile = SystemRegistry.ResolveActiveProfile();

                var world = new World(defaultWorldName, WorldFlags.Game);
                World.DefaultGameObjectInjectionWorld = world;

                var systems = SystemRegistry.GetSystems(profile);
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

                // Ensure the standard EntityCommandBuffer systems exist so Burst systems
                // can safely grab their singletons during OnCreate.
                world.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
                world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

                if (world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>() is { } fixedStepGroup)
                {
                    fixedStepGroup.Timestep = 1f / 60f;
                }

                ConfigureRootGroups(world);

                using (MaterializeGroupsMarker.Auto())
                {
                    var cameraInputGroup = world.GetOrCreateSystemManaged<CameraInputSystemGroup>();
                    var cameraPhaseGroup = world.GetOrCreateSystemManaged<CameraPhaseGroup>();
                    var environmentGroup = world.GetOrCreateSystemManaged<EnvironmentSystemGroup>();
                    var spatialGroup = world.GetOrCreateSystemManaged<SpatialSystemGroup>();
                    var gameplayGroup = world.GetOrCreateSystemManaged<GameplaySystemGroup>();
                    var transportPhaseGroup = world.GetOrCreateSystemManaged<TransportPhaseGroup>();
                    var historyPhaseGroup = world.GetOrCreateSystemManaged<HistoryPhaseGroup>();

                    world.GetOrCreateSystemManaged<TimeSystemGroup>();
                    world.GetOrCreateSystemManaged<VillagerSystemGroup>();
                    world.GetOrCreateSystemManaged<ResourceSystemGroup>();
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

                ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

                UnityEngine.Debug.Log($"[PureDotsWorldBootstrap] DOTS world initialized with profile '{profile.DisplayName}' ({profile.Id}).");
            }

            return true;
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
    }
}
