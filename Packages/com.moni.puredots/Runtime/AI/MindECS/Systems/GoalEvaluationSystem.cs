using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.MindECS.Components;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Processes goal priorities and updates active goals.
    /// Integrates with personality/behavior profiles.
    /// </summary>
    public class GoalEvaluationSystem : AEntitySetSystem<float>
    {
        public GoalEvaluationSystem(World world) : base(world.GetEntities().With<GoalProfile>().With<PersonalityProfile>().AsSet())
        {
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            if (!World.Has<GoalProfile>(entity) || !World.Has<PersonalityProfile>(entity))
            {
                return;
            }

            var goals = World.Get<GoalProfile>(entity);
            var personality = World.Get<PersonalityProfile>(entity);

            // Update goal state machine
            UpdateGoalState(goals, personality);

            // Remove completed/failed goals
            for (int i = goals.ActiveGoals.Count - 1; i >= 0; i--)
            {
                var goal = goals.ActiveGoals[i];
                if (goals.CurrentState == GoalProfile.GoalState.Completed ||
                    goals.CurrentState == GoalProfile.GoalState.Failed)
                {
                    goals.CompleteGoal(goal.Id);
                }
            }
        }

        private void UpdateGoalState(GoalProfile goals, PersonalityProfile personality)
        {
            switch (goals.CurrentState)
            {
                case GoalProfile.GoalState.Idle:
                    if (goals.ActiveGoals.Count > 0)
                    {
                        goals.CurrentState = GoalProfile.GoalState.Planning;
                    }
                    break;

                case GoalProfile.GoalState.Planning:
                    // Planning complete, start executing
                    goals.CurrentState = GoalProfile.GoalState.Executing;
                    break;

                case GoalProfile.GoalState.Executing:
                    // Continue executing (state changes handled by other systems)
                    break;

                case GoalProfile.GoalState.Paused:
                    // Can resume if conditions are met
                    if (personality.RiskTolerance > 0.3f)
                    {
                        goals.CurrentState = GoalProfile.GoalState.Executing;
                    }
                    break;
            }
        }
    }
}

