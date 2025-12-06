using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Devtools.Scenario
{
    /// <summary>
    /// Version 2 scenario loader with version detection and migration support.
    /// </summary>
    public static class ScenarioLoaderV2
    {
        /// <summary>
        /// Loads a scenario file, detects version, and applies migrations if needed.
        /// </summary>
        public static bool Load(string filePath, World world, out string error)
        {
            error = null;

            try
            {
                if (!File.Exists(filePath))
                {
                    error = $"Scenario file not found: {filePath}";
                    return false;
                }

                var json = File.ReadAllText(filePath);
                var scenario = JsonUtility.FromJson<ScenarioSerializerV2.ScenarioV2Data>(json);

                // Version detection and migration
                if (scenario.Version < ScenarioSerializerV2.CurrentVersion)
                {
                    if (!MigrateScenario(scenario, out error))
                    {
                        return false;
                    }
                }
                else if (scenario.Version > ScenarioSerializerV2.CurrentVersion)
                {
                    error = $"Scenario version {scenario.Version} is newer than supported version {ScenarioSerializerV2.CurrentVersion}";
                    return false;
                }

                // Validate schema
                if (!ValidateScenario(scenario, out error))
                {
                    return false;
                }

                // Load into world
                LoadIntoWorld(world, scenario);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool MigrateScenario(ScenarioSerializerV2.ScenarioV2Data scenario, out string error)
        {
            error = null;

            // Migration from v1 to v2
            if (scenario.Version == 1)
            {
                // Apply v1->v2 migration
                scenario.Version = 2;
                scenario.FormatVersion = ScenarioSerializerV2.FormatVersion;
                // Add any v2-specific fields with defaults
            }

            return true;
        }

        private static bool ValidateScenario(ScenarioSerializerV2.ScenarioV2Data scenario, out string error)
        {
            error = null;

            if (scenario.Metadata == null)
            {
                error = "Scenario missing metadata";
                return false;
            }

            if (scenario.Entities == null)
            {
                error = "Scenario missing entities";
                return false;
            }

            return true;
        }

        private static void LoadIntoWorld(World world, ScenarioSerializerV2.ScenarioV2Data scenario)
        {
            var entityManager = world.EntityManager;

            // Clear existing entities (optional - may want to merge instead)
            // entityManager.DestroyEntity(entityManager.GetAllEntities());

            // Load entities
            foreach (var entityData in scenario.Entities)
            {
                var entity = entityManager.CreateEntity();
                // In a full implementation, would deserialize components and buffers
                // This is a simplified version
            }

            // Load singletons
            if (scenario.Singletons != null && scenario.Singletons.ContainsKey("TimeState"))
            {
                var timeStateJson = scenario.Singletons["TimeState"].ToString();
                var timeState = JsonUtility.FromJson<TimeState>(timeStateJson);
                
                if (!entityManager.HasComponent<TimeState>(entityManager.CreateEntityQuery(typeof(TimeState)).GetSingletonEntity()))
                {
                    var singletonEntity = entityManager.CreateEntity();
                    entityManager.AddComponent<TimeState>(singletonEntity);
                }
                
                var singletonEntity2 = entityManager.CreateEntityQuery(typeof(TimeState)).GetSingletonEntity();
                entityManager.SetComponentData(singletonEntity2, timeState);
            }
        }
    }
}

