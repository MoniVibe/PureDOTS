using System.Collections.Generic;
using PureDOTS.Shared;

namespace PureDOTS.AI.MindECS.Components
{
    /// <summary>
    /// Active goals, priorities, and goal state machine for cognitive agents.
    /// Managed class component for DefaultEcs (Mind ECS layer).
    /// </summary>
    public class GoalProfile
    {
        public struct Goal
        {
            public string Id;
            public string Type;
            public float Priority;
            public bool IsActive;
            public Dictionary<string, object> Parameters;
        }

        public List<Goal> ActiveGoals;
        public List<Goal> CompletedGoals;
        public string CurrentPrimaryGoalId;

        // Goal state machine
        public enum GoalState
        {
            Idle,
            Planning,
            Executing,
            Paused,
            Completed,
            Failed
        }

        public GoalState CurrentState;

        public GoalProfile()
        {
            ActiveGoals = new List<Goal>();
            CompletedGoals = new List<Goal>();
            CurrentPrimaryGoalId = null;
            CurrentState = GoalState.Idle;
        }

        public void AddGoal(string id, string type, float priority)
        {
            var goal = new Goal
            {
                Id = id,
                Type = type,
                Priority = priority,
                IsActive = true,
                Parameters = new Dictionary<string, object>()
            };
            ActiveGoals.Add(goal);
        }

        public void CompleteGoal(string id)
        {
            for (int i = ActiveGoals.Count - 1; i >= 0; i--)
            {
                if (ActiveGoals[i].Id == id)
                {
                    var goal = ActiveGoals[i];
                    goal.IsActive = false;
                    CompletedGoals.Add(goal);
                    ActiveGoals.RemoveAt(i);
                    break;
                }
            }
        }
    }
}

