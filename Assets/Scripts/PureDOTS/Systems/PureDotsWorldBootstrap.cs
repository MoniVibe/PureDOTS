using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Custom bootstrap that creates the single simulation world we use for the pure DOTS run.
    /// Establishes the root system groups and appends the world to the player loop so no
    /// MonoBehaviour bootstrap is required.
    /// </summary>
    public sealed class PureDotsWorldBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            // Always run a single game world for now; presentation happens through system groups.
            var world = new World(defaultWorldName, WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = world;

            // Pull every auto-created system (including editor/scene streaming helpers) into the world.
            var systems = DefaultWorldInitialization.GetAllSystems(
                WorldSystemFilterFlags.Default |
                WorldSystemFilterFlags.Editor |
                WorldSystemFilterFlags.Streaming |
                WorldSystemFilterFlags.ProcessAfterLoad |
                WorldSystemFilterFlags.EntitySceneOptimizations);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

            // Align fixed-step timing with the simulation tick assumptions.
            if (world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>() is { } fixedStepGroup)
            {
                fixedStepGroup.Timestep = 1f / 60f;
            }

            // Force creation of our custom groups early so ordering attributes are respected.
            ConfigureRootGroups(world);
            var environmentGroup = world.GetOrCreateSystemManaged<EnvironmentSystemGroup>();
            var spatialGroup = world.GetOrCreateSystemManaged<SpatialSystemGroup>();
            var gameplayGroup = world.GetOrCreateSystemManaged<GameplaySystemGroup>();

            world.GetOrCreateSystemManaged<TimeSystemGroup>();
            var recordGroup = world.GetOrCreateSystemManaged<RecordSimulationSystemGroup>();
            var catchUpGroup = world.GetOrCreateSystemManaged<CatchUpSimulationSystemGroup>();
            var playbackGroup = world.GetOrCreateSystemManaged<PlaybackSimulationSystemGroup>();
            world.GetOrCreateSystemManaged<VillagerSystemGroup>();
            world.GetOrCreateSystemManaged<ResourceSystemGroup>();
            world.GetOrCreateSystemManaged<MiracleEffectSystemGroup>();
            world.GetOrCreateSystemManaged<CombatSystemGroup>();
            world.GetOrCreateSystemManaged<HandSystemGroup>();
            world.GetOrCreateSystemManaged<VegetationSystemGroup>();
            world.GetOrCreateSystemManaged<ConstructionSystemGroup>();
            world.GetOrCreateSystemManaged<HistorySystemGroup>();

            environmentGroup.SortSystems();
            spatialGroup.SortSystems();
            gameplayGroup.SortSystems();

            recordGroup.Enabled = true;
            catchUpGroup.Enabled = false;
            playbackGroup.Enabled = false;

            recordGroup.SortSystems();
            catchUpGroup.SortSystems();
            playbackGroup.SortSystems();

            // Make sure everything ends up in the player loop.
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

            Debug.Log("[PureDotsWorldBootstrap] Default DOTS world initialized.");
            return true;
        }

        private static void ConfigureRootGroups(World world)
        {
            var initializationGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            initializationGroup.SortSystems();

            if (world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>() is { } fixedStepGroup)
            {
                fixedStepGroup.SortSystems();
            }

            var simulationGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simulationGroup.SortSystems();

            var presentationGroup = world.GetOrCreateSystemManaged<PresentationSystemGroup>();
            presentationGroup.SortSystems();
        }
    }
}
