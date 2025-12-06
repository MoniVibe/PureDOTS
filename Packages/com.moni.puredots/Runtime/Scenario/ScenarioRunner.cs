using System.IO;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using PureDOTS.Runtime.Scenarios;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// Enhanced scenario runner with JSON loading support.
    /// </summary>
    public static class ScenarioRunner
    {
        /// <summary>
        /// Loads a scenario from JSON file and applies it to the world.
        /// </summary>
        public static bool LoadScenario(World world, string path)
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, path);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[ScenarioRunner] Scenario file not found at {fullPath}");
                return false;
            }

            try
            {
                var json = File.ReadAllText(fullPath);
                
                if (!PureDOTS.Runtime.Scenarios.ScenarioRunner.TryParse(json, out var data, out var parseError))
                {
                    Debug.LogError($"[ScenarioRunner] Failed to parse scenario JSON: {parseError}");
                    return false;
                }

                if (!PureDOTS.Runtime.Scenarios.ScenarioRunner.TryBuild(data, Allocator.Temp, out var scenario, out var buildError))
                {
                    Debug.LogError($"[ScenarioRunner] Failed to build scenario: {buildError}");
                    return false;
                }

                // Apply scenario to world
                ApplyScenario(world, scenario);
                
                Debug.Log($"[ScenarioRunner] Loaded scenario '{scenario.ScenarioId}' with {scenario.EntityCounts.Length} entity types");
                
                scenario.EntityCounts.Dispose();
                scenario.InputCommands.Dispose();
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ScenarioRunner] Failed to load scenario from {fullPath}: {ex.Message}");
                return false;
            }
        }

        private static void ApplyScenario(World world, PureDOTS.Runtime.Scenarios.ResolvedScenario scenario)
        {
            var entityManager = world.EntityManager;
            
            // Apply entity counts (spawn entities based on registry IDs)
            // This is a simplified version - full implementation would look up prefabs from registries
            for (int i = 0; i < scenario.EntityCounts.Length; i++)
            {
                var entityCount = scenario.EntityCounts[i];
                Debug.Log($"[ScenarioRunner] Would spawn {entityCount.Count} entities of type '{entityCount.RegistryId}'");
                // TODO: Look up prefab from registry and spawn entities
            }
        }
    }
}

