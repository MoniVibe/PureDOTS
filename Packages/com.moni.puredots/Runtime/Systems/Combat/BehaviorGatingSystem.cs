using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Skills;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(BehaviorCatalogSystem))]
    public partial struct BehaviorGatingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            if (timeState.IsPaused)
                return;

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            if (timeState.Tick % 2 != 0)
                return;

            var job = new EvaluateBehaviorGatingJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct EvaluateBehaviorGatingJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                ref BehaviorTierState tierState,
                [ChunkIndexInQuery] int chunkIndex,
                in SkillSet skillSet,
                in ImplantTag implantTag,
                DynamicBuffer<BehaviorSet> behaviorSet)
            {
                BehaviorTier newTier = BehaviorTier.Baseline;

                bool hasLearned = false;
                bool hasMastered = false;

                for (int i = 0; i < behaviorSet.Length; i++)
                {
                    hasLearned = true;
                }

                if (hasMastered)
                    newTier = BehaviorTier.Mastered;
                else if (hasLearned)
                    newTier = BehaviorTier.Learned;
                else
                    newTier = BehaviorTier.Baseline;

                tierState.Tier = newTier;
            }
        }
    }
}
