using System;
using System.Collections.Generic;
using System.Linq;
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
#if UNITY_EDITOR
            // Guard: Skip world creation during domain reload/compilation to prevent editor freeze
            if (!Application.isPlaying && (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating))
            {
                return false; // Let Unity use default bootstrap during reload
            }
#endif

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
                    // Only create system groups that are actually needed based on the profile
                    // Core infrastructure groups - always needed
                    world.GetOrCreateSystemManaged<TimeSystemGroup>();

                    // Check if profile includes systems that would use specific groups
                    bool hasCameraSystems = systems.Contains(typeof(CameraInputSystemGroup)) ||
                                           systems.Contains(typeof(CameraPhaseGroup)) ||
                                           HasSystemsInNamespace(systems, "PureDOTS.Camera");

                    bool hasEnvironmentSystems = systems.Contains(typeof(EnvironmentSystemGroup)) ||
                                               HasSystemsInNamespace(systems, "PureDOTS.Environment");

                    bool hasSpatialSystems = systems.Contains(typeof(SpatialSystemGroup)) ||
                                           HasSystemsInNamespace(systems, "PureDOTS.Spatial");

                    bool hasGameplaySystems = systems.Contains(typeof(GameplaySystemGroup)) ||
                                            HasSystemsInNamespace(systems, "PureDOTS.Gameplay");

                    bool hasVillagerSystems = systems.Contains(typeof(VillagerSystemGroup)) ||
                                            HasSystemsInNamespace(systems, "PureDOTS.Villager");

                    bool hasResourceSystems = systems.Contains(typeof(ResourceSystemGroup)) ||
                                            HasSystemsInNamespace(systems, "PureDOTS.Resource");

                    bool hasPowerSystems = systems.Contains(typeof(PowerSystemGroup)) ||
                                         HasSystemsInNamespace(systems, "PureDOTS.Power");

                    bool hasMiracleSystems = systems.Contains(typeof(MiracleEffectSystemGroup)) ||
                                           HasSystemsInNamespace(systems, "PureDOTS.Miracle");

                    bool hasCombatSystems = systems.Contains(typeof(CombatSystemGroup)) ||
                                          HasSystemsInNamespace(systems, "PureDOTS.Combat");

                    bool hasTransportSystems = systems.Contains(typeof(TransportPhaseGroup)) ||
                                             HasSystemsInNamespace(systems, "PureDOTS.Transport");

                    bool hasHistorySystems = systems.Contains(typeof(HistorySystemGroup)) ||
                                           systems.Contains(typeof(HistoryPhaseGroup)) ||
                                           HasSystemsInNamespace(systems, "PureDOTS.History");

                    // Create groups conditionally based on profile
                    ComponentSystemGroup cameraInputGroup = null, cameraPhaseGroup = null,
                                        environmentGroup = null, spatialGroup = null,
                                        gameplayGroup = null, transportPhaseGroup = null,
                                        historyPhaseGroup = null;

                    if (hasCameraSystems)
                    {
                        cameraInputGroup = world.GetOrCreateSystemManaged<CameraInputSystemGroup>();
                        cameraPhaseGroup = world.GetOrCreateSystemManaged<CameraPhaseGroup>();
                    }

                    if (hasEnvironmentSystems)
                    {
                        environmentGroup = world.GetOrCreateSystemManaged<EnvironmentSystemGroup>();
                    }

                    if (hasSpatialSystems)
                    {
                        spatialGroup = world.GetOrCreateSystemManaged<SpatialSystemGroup>();
                    }

                    if (hasGameplaySystems)
                    {
                        gameplayGroup = world.GetOrCreateSystemManaged<GameplaySystemGroup>();
                    }

                    if (hasTransportSystems)
                    {
                        transportPhaseGroup = world.GetOrCreateSystemManaged<TransportPhaseGroup>();
                    }

                    if (hasHistorySystems)
                    {
                        historyPhaseGroup = world.GetOrCreateSystemManaged<HistoryPhaseGroup>();
                        world.GetOrCreateSystemManaged<HistorySystemGroup>();
                    }

                    // Always create core simulation groups that might be needed
                    if (hasVillagerSystems)
                        world.GetOrCreateSystemManaged<VillagerSystemGroup>();

                    if (hasResourceSystems)
                        world.GetOrCreateSystemManaged<ResourceSystemGroup>();

                    if (hasPowerSystems)
                        world.GetOrCreateSystemManaged<PowerSystemGroup>();

                    if (hasMiracleSystems)
                        world.GetOrCreateSystemManaged<MiracleEffectSystemGroup>();

                    if (hasCombatSystems)
                        world.GetOrCreateSystemManaged<CombatSystemGroup>();

                    // Always create groups that provide essential infrastructure
                    world.GetOrCreateSystemManaged<HandSystemGroup>();
                    world.GetOrCreateSystemManaged<VegetationSystemGroup>();
                    world.GetOrCreateSystemManaged<ConstructionSystemGroup>();

                    // Sort groups that were created
                    cameraInputGroup?.SortSystems();
                    cameraPhaseGroup?.SortSystems();
                    environmentGroup?.SortSystems();
                    spatialGroup?.SortSystems();
                    transportPhaseGroup?.SortSystems();
                    gameplayGroup?.SortSystems();
                    historyPhaseGroup?.SortSystems();
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

        private static bool HasSystemsInNamespace(IReadOnlyList<Type> systems, string namespacePrefix)
        {
            foreach (var systemType in systems)
            {
                if (systemType?.Namespace?.StartsWith(namespacePrefix) == true)
                    return true;
            }
            return false;
        }
    }
}
