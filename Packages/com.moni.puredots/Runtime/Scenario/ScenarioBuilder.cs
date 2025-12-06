using System;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using PureDOTS.Runtime.Scenarios;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// Implementation of IScenarioBuilder that backs editor actions with ScenarioRunner.
    /// Stores actions in ECS buffers and serializes to deterministic JSON.
    /// </summary>
    public class ScenarioBuilder : IScenarioBuilder, IDisposable
    {
        private readonly EntityManager _entityManager;
        private readonly Entity _scenarioEntity;
        private bool _disposed;

        public ScenarioBuilder(EntityManager entityManager)
        {
            _entityManager = entityManager;
            _scenarioEntity = entityManager.CreateEntity();
            entityManager.AddComponent<ScenarioBuilderState>(_scenarioEntity);
            
            if (!entityManager.HasBuffer<ScenarioAction>(_scenarioEntity))
            {
                entityManager.AddBuffer<ScenarioAction>(_scenarioEntity);
            }
        }

        public void AddEntity(Entity prefab, float3 pos)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScenarioBuilder));
            if (prefab == Entity.Null) return;

            var buffer = _entityManager.GetBuffer<ScenarioAction>(_scenarioEntity);
            buffer.Add(new ScenarioAction
            {
                Type = ScenarioActionType.AddEntity,
                PrefabEntity = prefab,
                Position = pos
            });
        }

        public void AddComponent<T>(Entity e, in T component) where T : unmanaged, IComponentData
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScenarioBuilder));
            if (e == Entity.Null) return;

            var buffer = _entityManager.GetBuffer<ScenarioAction>(_scenarioEntity);
            var typeName = typeof(T).FullName ?? typeof(T).Name;
            
            // Serialize component to JSON (simplified - in production use proper serialization)
            var json = SerializeComponent(component);
            
            buffer.Add(new ScenarioAction
            {
                Type = ScenarioActionType.AddComponent,
                TargetEntity = e,
                ComponentTypeName = new FixedString128Bytes(typeName),
                ComponentDataJson = new FixedString512Bytes(json)
            });
        }

        public void SaveScenario(string path)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScenarioBuilder));

            var data = BuildScenarioData();
            var json = SerializeScenarioData(data);
            
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(path, json);
        }

        private ScenarioDefinitionData BuildScenarioData()
        {
            var state = _entityManager.GetComponentData<ScenarioBuilderState>(_scenarioEntity);
            var actions = _entityManager.GetBuffer<ScenarioAction>(_scenarioEntity);
            
            var data = new ScenarioDefinitionData
            {
                scenarioId = state.ScenarioId.ToString(),
                seed = state.Seed,
                runTicks = state.RunTicks,
                entityCounts = Array.Empty<ScenarioEntityCountData>(),
                inputCommands = Array.Empty<ScenarioInputCommandData>()
            };

            // Count entities by registry type (simplified - would need registry lookup)
            var entityCounts = new System.Collections.Generic.Dictionary<string, int>();
            
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                if (action.Type == ScenarioActionType.AddEntity)
                {
                    // Determine registry ID from prefab (simplified)
                    var registryId = GetRegistryIdForPrefab(action.PrefabEntity);
                    if (!string.IsNullOrEmpty(registryId))
                    {
                        entityCounts.TryGetValue(registryId, out var count);
                        entityCounts[registryId] = count + 1;
                    }
                }
            }

            var countList = new System.Collections.Generic.List<ScenarioEntityCountData>();
            foreach (var kvp in entityCounts)
            {
                countList.Add(new ScenarioEntityCountData
                {
                    registryId = kvp.Key,
                    count = kvp.Value
                });
            }
            data.entityCounts = countList.ToArray();

            return data;
        }

        private string SerializeScenarioData(ScenarioDefinitionData data)
        {
            return JsonUtility.ToJson(data, true);
        }

        private string SerializeComponent<T>(in T component) where T : unmanaged
        {
            // Simplified serialization - in production use proper component serialization
            // For now, return empty JSON - components would need custom serialization
            return "{}";
        }

        private string GetRegistryIdForPrefab(Entity prefab)
        {
            // Simplified - would need to look up prefab in registry system
            // For now, return empty string
            return string.Empty;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// State component for scenario builder singleton.
    /// </summary>
    public struct ScenarioBuilderState : IComponentData
    {
        public FixedString64Bytes ScenarioId;
        public uint Seed;
        public int RunTicks;
    }
}

