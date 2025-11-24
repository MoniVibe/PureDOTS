using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villager;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Bridges shared AICommand queue to Godgame-specific VillagerAIState and job systems.
    /// Consumes commands from AISystemGroup pipeline and translates them into villager goals.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Systems.AI.AITaskResolutionSystem))]
    public partial struct GodgameVillagerAICommandBridgeSystem : ISystem
    {
        private ComponentLookup<VillagerAIUtilityBinding> _utilityBindingLookup;
        private ComponentLookup<VillagerAIState> _aiStateLookup;
        private ComponentLookup<VillagerJob> _jobLookup;
        private ComponentLookup<VillagerJobTicket> _ticketLookup;
        private ComponentLookup<VillagerNeeds> _needsLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _utilityBindingLookup = state.GetComponentLookup<VillagerAIUtilityBinding>(true);
            _aiStateLookup = state.GetComponentLookup<VillagerAIState>(false);
            _jobLookup = state.GetComponentLookup<VillagerJob>(false);
            _ticketLookup = state.GetComponentLookup<VillagerJobTicket>(true);
            _needsLookup = state.GetComponentLookup<VillagerNeeds>(true);

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<AICommandQueueTag>();
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

            var queueEntity = SystemAPI.GetSingletonEntity<AICommandQueueTag>();
            if (!state.EntityManager.HasBuffer<AICommand>(queueEntity))
            {
                return;
            }

            var commands = state.EntityManager.GetBuffer<AICommand>(queueEntity);
            if (commands.Length == 0)
            {
                return;
            }

            _utilityBindingLookup.Update(ref state);
            _aiStateLookup.Update(ref state);
            _jobLookup.Update(ref state);
            _ticketLookup.Update(ref state);
            _needsLookup.Update(ref state);

            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                if (!_aiStateLookup.HasComponent(command.Agent))
                {
                    continue;
                }

                var aiState = _aiStateLookup[command.Agent];
                var goal = MapActionToGoal(command.Agent, command.ActionIndex, ref state);

                if (goal != VillagerAIState.Goal.None && goal != aiState.CurrentGoal)
                {
                    aiState.CurrentGoal = goal;
                    aiState.CurrentState = GoalToState(goal);
                    aiState.TargetEntity = command.TargetEntity;
                    aiState.TargetPosition = command.TargetPosition;
                    aiState.StateTimer = 0f;
                    aiState.StateStartTick = timeState.Tick;
                    _aiStateLookup[command.Agent] = aiState;

                    // If goal is Work and we have a job ticket, ensure job phase is set
                    if (goal == VillagerAIState.Goal.Work && _ticketLookup.HasComponent(command.Agent))
                    {
                        var ticket = _ticketLookup[command.Agent];
                        if (ticket.ResourceEntity != Entity.Null && _jobLookup.HasComponent(command.Agent))
                        {
                            var job = _jobLookup[command.Agent];
                            if (job.Phase == VillagerJob.JobPhase.Idle)
                            {
                                job.Phase = VillagerJob.JobPhase.Gathering;
                                _jobLookup[command.Agent] = job;
                            }
                        }
                    }
                }
            }
        }

        private VillagerAIState.Goal MapActionToGoal(Entity agent, byte actionIndex, ref SystemState state)
        {
            // Check if villager has utility binding that maps actions to goals
            if (_utilityBindingLookup.HasComponent(agent))
            {
                var binding = _utilityBindingLookup[agent];
                if (actionIndex < binding.Goals.Length)
                {
                    return binding.Goals[actionIndex];
                }
            }

            // Fallback: map action indices to goals based on needs
            // Action 0: SatisfyHunger -> SurviveHunger
            // Action 1: Rest -> Rest
            // Action 2: ImproveMorale -> Rest (social rest)
            // Action 3: Work -> Work
            if (_needsLookup.HasComponent(agent))
            {
                var needs = _needsLookup[agent];
                switch (actionIndex)
                {
                    case 0:
                        return VillagerAIState.Goal.SurviveHunger;
                    case 1:
                        return VillagerAIState.Goal.Rest;
                    case 2:
                        return VillagerAIState.Goal.Rest;
                    case 3:
                        return VillagerAIState.Goal.Work;
                    default:
                        return VillagerAIState.Goal.None;
                }
            }

            return VillagerAIState.Goal.None;
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
    }
}

