using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Runtime.Scenarios
{
    /// <summary>
    /// Serializable scenario definition for headless or in-editor runs.
    /// This shape is intentionally simple so it can be produced from JSON/blob assets.
    /// </summary>
    [Serializable]
    public class ScenarioDefinitionData
    {
        public string scenarioId = "scenario.default";
        public uint seed = 1;
        public int runTicks = 600;
        public ScenarioEntityCountData[] entityCounts = Array.Empty<ScenarioEntityCountData>();
        public ScenarioInputCommandData[] inputCommands = Array.Empty<ScenarioInputCommandData>();
    }

    [Serializable]
    public class ScenarioEntityCountData
    {
        public string registryId = string.Empty;
        public int count = 0;
    }

    [Serializable]
    public class ScenarioInputCommandData
    {
        public int tick = 0;
        public string commandId = string.Empty;
        public string payload = string.Empty;
    }

    /// <summary>
    /// Native representation used during scenario execution.
    /// Dispose after use to release temporary allocations.
    /// </summary>
    public struct ResolvedScenario : IDisposable
    {
        public FixedString64Bytes ScenarioId;
        public uint Seed;
        public int RunTicks;
        public NativeList<ScenarioEntityCount> EntityCounts;
        public NativeList<ScenarioInputCommand> InputCommands;

        public void Dispose()
        {
            if (EntityCounts.IsCreated)
            {
                EntityCounts.Dispose();
            }

            if (InputCommands.IsCreated)
            {
                InputCommands.Dispose();
            }
        }
    }

    public struct ScenarioEntityCount
    {
        public FixedString64Bytes RegistryId;
        public int Count;
    }

    public struct ScenarioInputCommand
    {
        public int Tick;
        public FixedString64Bytes CommandId;
        public FixedString64Bytes Payload;
    }

    public static class ScenarioRunner
    {
        /// <summary>
        /// Stub type alias for ScenarioDefinitionData.
        /// Use ScenarioDefinitionData directly instead.
        /// </summary>
        [Obsolete("Use ScenarioDefinitionData instead")]
        public class ScenarioData : ScenarioDefinitionData
        {
        }

        /// <summary>
        /// Parse JSON text into the serializable scenario data.
        /// </summary>
        public static bool TryParse(string json, out ScenarioDefinitionData data, out FixedString128Bytes error)
        {
            error = default;
            data = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Scenario JSON was empty";
                return false;
            }

            try
            {
                data = UnityEngine.JsonUtility.FromJson<ScenarioDefinitionData>(json);
            }
            catch (Exception ex)
            {
                error = $"Failed to parse scenario JSON: {ex.Message}";
                return false;
            }

            if (data == null)
            {
                error = "Scenario JSON produced no data";
                return false;
            }

            return ValidateData(data, out error);
        }

        /// <summary>
        /// Build a native scenario representation suitable for deterministic execution.
        /// </summary>
        public static bool TryBuild(in ScenarioDefinitionData data, Allocator allocator, out ResolvedScenario scenario, out FixedString128Bytes error)
        {
            scenario = default;
            if (!ValidateData(data, out error))
            {
                return false;
            }

            scenario = new ResolvedScenario
            {
                ScenarioId = new FixedString64Bytes(data.scenarioId ?? "scenario.unnamed"),
                Seed = data.seed,
                RunTicks = math.max(1, data.runTicks),
                EntityCounts = new NativeList<ScenarioEntityCount>(allocator),
                InputCommands = new NativeList<ScenarioInputCommand>(allocator)
            };

            if (data.entityCounts != null)
            {
                foreach (var entry in data.entityCounts)
                {
                    if (string.IsNullOrWhiteSpace(entry.registryId) || entry.count <= 0)
                    {
                        continue;
                    }

                    scenario.EntityCounts.Add(new ScenarioEntityCount
                    {
                        RegistryId = new FixedString64Bytes(entry.registryId),
                        Count = entry.count
                    });
                }
            }

            if (data.inputCommands != null)
            {
                foreach (var command in data.inputCommands)
                {
                    if (command.tick < 0 || string.IsNullOrWhiteSpace(command.commandId))
                    {
                        continue;
                    }

                    scenario.InputCommands.Add(new ScenarioInputCommand
                    {
                        Tick = command.tick,
                        CommandId = new FixedString64Bytes(command.commandId),
                        Payload = new FixedString64Bytes(command.payload ?? string.Empty)
                    });
                }
            }

            return true;
        }

        private static bool ValidateData(ScenarioDefinitionData data, out FixedString128Bytes error)
        {
            error = default;

            if (data.runTicks <= 0)
            {
                error = "Scenario runTicks must be > 0";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.scenarioId))
            {
                error = "Scenario must have a scenarioId";
                return false;
            }

            return true;
        }
    }
}
