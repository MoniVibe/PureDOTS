using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villager;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Minimal behaviour layer that evaluates high level villager goals based on needs and job data.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup))]
    [UpdateBefore(typeof(VillagerTargetingSystem))]
    public partial struct VillagerAISystem : ISystem
    {
        private EntityQuery _villagerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerAIState, VillagerNeeds, VillagerJob, VillagerJobTicket>()
                .WithNone<VillagerDeadTag, PlaybackGuardTag>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate(_villagerQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Get villager behavior config or use defaults
            var config = SystemAPI.HasSingleton<VillagerBehaviorConfig>()
                ? SystemAPI.GetSingleton<VillagerBehaviorConfig>()
                : VillagerBehaviorConfig.CreateDefaults();

            var job = new EvaluateVillagerAIJob
            {
                DeltaTime = timeState.FixedDeltaTime,
                CurrentTick = timeState.Tick,
                HungerThreshold = config.HungerThreshold,
                EnergyThreshold = config.EnergyThreshold,
                FleeHealthThreshold = config.FleeHealthThreshold,
                EatingHungerThresholdMultiplier = config.EatingHungerThresholdMultiplier,
                EatingDuration = config.EatingDuration,
                FleeDuration = config.FleeDuration,
                RestEnergyThreshold = config.RestEnergyThreshold
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct EvaluateVillagerAIJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;
            public float HungerThreshold;
            public float EnergyThreshold;
            public float FleeHealthThreshold;
            public float EatingHungerThresholdMultiplier;
            public float EatingDuration;
            public float FleeDuration;
            public float RestEnergyThreshold;

            public void Execute(ref VillagerAIState aiState, in VillagerNeeds needs, in VillagerJob job, in VillagerJobTicket ticket)
            {
                aiState.StateTimer += DeltaTime;

                var desiredGoal = aiState.CurrentGoal;

                if (needs.Health < FleeHealthThreshold)
                {
                    desiredGoal = VillagerAIState.Goal.Flee;
                }
                else if (needs.Hunger >= HungerThreshold)
                {
                    desiredGoal = VillagerAIState.Goal.SurviveHunger;
                }
                else if (needs.Energy <= EnergyThreshold)
                {
                    desiredGoal = VillagerAIState.Goal.Rest;
                }
                else if (job.Type != VillagerJob.JobType.None &&
                         (VillagerJob.JobPhase)ticket.Phase != VillagerJob.JobPhase.Idle &&
                         ticket.ResourceEntity != Entity.Null)
                {
                    desiredGoal = VillagerAIState.Goal.Work;
                }
                else
                {
                    desiredGoal = VillagerAIState.Goal.None;
                }

                if (desiredGoal != aiState.CurrentGoal)
                {
                    aiState.CurrentGoal = desiredGoal;
                    aiState.CurrentState = GoalToState(desiredGoal);
                    aiState.StateTimer = 0f;
                    aiState.StateStartTick = CurrentTick;
                }

                // Clamp states when conditions change.
                switch (aiState.CurrentState)
                {
                    case VillagerAIState.State.Working:
                        if (ticket.ResourceEntity == Entity.Null)
                        {
                            SetIdle(ref aiState);
                        }
                        break;
                    case VillagerAIState.State.Eating:
                        if (needs.Hunger < HungerThreshold * EatingHungerThresholdMultiplier || aiState.StateTimer >= EatingDuration)
                        {
                            SetIdle(ref aiState);
                        }
                        break;
                    case VillagerAIState.State.Sleeping:
                        if (needs.Energy > RestEnergyThreshold)
                        {
                            SetIdle(ref aiState);
                        }
                        break;
                    case VillagerAIState.State.Fleeing:
                        if (aiState.StateTimer >= FleeDuration)
                        {
                            SetIdle(ref aiState);
                        }
                        break;
                }

                if (aiState.CurrentState != VillagerAIState.State.Working)
                {
                    aiState.TargetEntity = Entity.Null;
                }
                else if (ticket.ResourceEntity != Entity.Null)
                {
                    aiState.TargetEntity = ticket.ResourceEntity;
                }
            }

            private static VillagerAIState.State GoalToState(VillagerAIState.Goal goal)
            {
                return goal switch
                {
                    VillagerAIState.Goal.SurviveHunger => VillagerAIState.State.Eating,
                    VillagerAIState.Goal.Work => VillagerAIState.State.Working,
                    VillagerAIState.Goal.Rest => VillagerAIState.State.Sleeping,
                    VillagerAIState.Goal.Flee => VillagerAIState.State.Fleeing,
                    VillagerAIState.Goal.Fight => VillagerAIState.State.Fighting,
                    _ => VillagerAIState.State.Idle
                };
            }

            private static void SetIdle(ref VillagerAIState aiState)
            {
                aiState.CurrentGoal = VillagerAIState.Goal.None;
                aiState.CurrentState = VillagerAIState.State.Idle;
                aiState.TargetEntity = Entity.Null;
                aiState.TargetPosition = float3.zero;
                aiState.StateTimer = 0f;
            }
        }
    }
}
