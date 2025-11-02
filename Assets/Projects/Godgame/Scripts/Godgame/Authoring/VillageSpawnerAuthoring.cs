using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Godgame.Authoring
{
    /// <summary>
    /// Authoring component for spawning villagers at runtime.
    /// Pure DOTS runtime system will handle actual spawning.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VillageSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject villagerPrefab;

        [SerializeField]
        private int villagerCount = 3;

        [SerializeField]
        private float spawnRadius = 5f;

        [SerializeField]
        private VillagerJob.JobType defaultJobType = VillagerJob.JobType.Gatherer;

        [SerializeField]
        private VillagerAIState.Goal defaultAIGoal = VillagerAIState.Goal.Work;

        public GameObject VillagerPrefab => villagerPrefab;
        public int VillagerCount => villagerCount;
        public float SpawnRadius => spawnRadius;
        public VillagerJob.JobType DefaultJobType => defaultJobType;
        public VillagerAIState.Goal DefaultAIGoal => defaultAIGoal;

        private sealed class Baker : Unity.Entities.Baker<VillageSpawnerAuthoring>
        {
            public override void Bake(VillageSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                if (authoring.villagerPrefab == null)
                {
                    return;
                }

                var prefabEntity = GetEntity(authoring.villagerPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);
                
                AddComponent(entity, new VillageSpawnerConfig
                {
                    VillagerPrefab = prefabEntity,
                    VillagerCount = authoring.villagerCount,
                    SpawnRadius = authoring.spawnRadius,
                    DefaultJobType = authoring.defaultJobType,
                    DefaultAIGoal = authoring.defaultAIGoal,
                    SpawnedCount = 0
                });
            }
        }
    }

}



