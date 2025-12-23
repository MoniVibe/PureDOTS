using System;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Space4x.Bootstrap
{
    /// <summary>
    /// Bootstrap MonoBehaviour for Space4X demo scenes.
    /// Sets up the PureDOTS world and injects scenario metadata.
    /// </summary>
    public class Space4XBootstrap : MonoBehaviour
    {
        private const string LegacyScenarioId = "scenario.space4x.smoke";
        private const string SharedScenarioId = "space4x_smoke";

        [SerializeField]
        private string scenarioId = SharedScenarioId;

        [SerializeField]
        private uint seed = 77;

        [SerializeField]
        private int runTicks = 240;

        [SerializeField]
        private ScenarioEntityCountData[] entityCounts = new ScenarioEntityCountData[]
        {
            new ScenarioEntityCountData { registryId = "registry.spawner.miner", count = 2 },
            new ScenarioEntityCountData { registryId = "registry.storehouse", count = 1 }
        };

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[Space4XBootstrap] Default world not found. Ensure PureDotsWorldBootstrap is active.");
                return;
            }

            var entityManager = world.EntityManager;

            // Inject scenario metadata
            var scenarioEntity = entityManager.CreateEntity(typeof(ScenarioInfo), typeof(ScenarioEntityCountElement));
            var resolvedScenarioId = ResolveScenarioId();
            entityManager.SetComponentData(scenarioEntity, new ScenarioInfo
            {
                ScenarioId = new FixedString64Bytes(resolvedScenarioId),
                Seed = seed,
                RunTicks = runTicks
            });

            var buffer = entityManager.GetBuffer<ScenarioEntityCountElement>(scenarioEntity);
            for (int i = 0; i < entityCounts.Length; i++)
            {
                buffer.Add(new ScenarioEntityCountElement
                {
                    RegistryId = new FixedString64Bytes(entityCounts[i].registryId),
                    Count = entityCounts[i].count
                });
            }

            Debug.Log($"[Space4XBootstrap] Scenario '{resolvedScenarioId}' initialized with {entityCounts.Length} entity count entries.");
        }

        private string ResolveScenarioId()
        {
            if (string.IsNullOrWhiteSpace(scenarioId) ||
                string.Equals(scenarioId, LegacyScenarioId, StringComparison.OrdinalIgnoreCase))
            {
                scenarioId = SharedScenarioId;
            }

            return scenarioId;
        }
    }
}







