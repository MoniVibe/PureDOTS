using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Godgame.Bootstrap
{
    /// <summary>
    /// Bootstrap MonoBehaviour for Godgame demo scenes.
    /// Sets up the PureDOTS world and injects scenario metadata.
    /// </summary>
    public class GodgameBootstrap : MonoBehaviour
    {
        [SerializeField]
        private string scenarioId = "scenario.godgame.smoke";

        [SerializeField]
        private uint seed = 42;

        [SerializeField]
        private int runTicks = 180;

        [SerializeField]
        private ScenarioEntityCountData[] entityCounts = new ScenarioEntityCountData[]
        {
            new ScenarioEntityCountData { registryId = "registry.villager", count = 6 },
            new ScenarioEntityCountData { registryId = "registry.storehouse", count = 1 }
        };

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[GodgameBootstrap] Default world not found. Ensure PureDotsWorldBootstrap is active.");
                return;
            }

            var entityManager = world.EntityManager;

            // Inject scenario metadata
            var scenarioEntity = entityManager.CreateEntity(typeof(ScenarioInfo), typeof(ScenarioEntityCountElement));
            entityManager.SetComponentData(scenarioEntity, new ScenarioInfo
            {
                ScenarioId = new FixedString64Bytes(scenarioId),
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

            Debug.Log($"[GodgameBootstrap] Scenario '{scenarioId}' initialized with {entityCounts.Length} entity count entries.");
        }
    }
}







