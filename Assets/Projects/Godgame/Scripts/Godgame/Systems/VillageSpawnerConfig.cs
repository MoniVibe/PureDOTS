using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace Godgame.Authoring
{
    /// <summary>
    /// Runtime component for villager spawner configuration used by both authoring and gameplay systems.
    /// </summary>
    public struct VillageSpawnerConfig : IComponentData
    {
        public Entity VillagerPrefab;
        public int VillagerCount;
        public float SpawnRadius;
        public VillagerJob.JobType DefaultJobType;
        public VillagerAIState.Goal DefaultAIGoal;
        public int SpawnedCount;
    }
}

