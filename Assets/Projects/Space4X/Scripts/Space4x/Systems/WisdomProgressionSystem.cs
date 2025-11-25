using Space4X.Individuals;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace Space4X.Systems
{
    /// <summary>
    /// Manages wisdom earning and spending into experience pools.
    /// Updates wisdom earn rate based on total wisdom earned (diminishing returns).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct WisdomProgressionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;
            var deltaTime = timeState.DeltaTime;

            // Process wisdom spending requests
            new ProcessWisdomSpendingJob
            {
                CurrentTick = currentTick
            }.ScheduleParallel();

            // Update wisdom earn rate based on total wisdom earned
            new UpdateWisdomEarnRateJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessWisdomSpendingJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                ref WisdomPool wisdomPool,
                ref ExperiencePools experiencePools,
                ref DynamicBuffer<WisdomSpendRequest> spendRequests)
            {
                for (int i = spendRequests.Length - 1; i >= 0; i--)
                {
                    var request = spendRequests[i];

                    // Check if enough wisdom available
                    if (wisdomPool.CurrentWisdom < request.Amount)
                    {
                        spendRequests.RemoveAt(i);
                        continue; // Not enough wisdom
                    }

                    // Spend wisdom into target pool
                    wisdomPool.CurrentWisdom -= request.Amount;

                    switch (request.TargetPool)
                    {
                        case ExperiencePoolType.Physique:
                            experiencePools.PhysiqueXP += request.Amount;
                            break;
                        case ExperiencePoolType.Finesse:
                            experiencePools.FinesseXP += request.Amount;
                            break;
                        case ExperiencePoolType.Will:
                            experiencePools.WillXP += request.Amount;
                            break;
                    }

                    spendRequests.RemoveAt(i);
                }
            }
        }

        [BurstCompile]
        public partial struct UpdateWisdomEarnRateJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(ref WisdomPool wisdomPool)
            {
                // Calculate wisdom earn rate based on total wisdom earned
                // Diminishing returns: rate = baseRate / (1 + totalWisdom / scaleFactor)
                // Example: baseRate = 1.0, scaleFactor = 1000.0
                // At 0 total wisdom: rate = 1.0
                // At 1000 total wisdom: rate = 0.5
                // At 10000 total wisdom: rate = 0.09
                const float baseRate = 1.0f;
                const float scaleFactor = 1000.0f;

                float totalWisdom = wisdomPool.TotalWisdomEarned;
                wisdomPool.WisdomEarnRate = baseRate / (1.0f + totalWisdom / scaleFactor);
            }
        }
    }
}

