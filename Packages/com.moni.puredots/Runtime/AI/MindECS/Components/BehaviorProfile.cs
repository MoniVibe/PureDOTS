using System.Collections.Generic;
using PureDOTS.Shared;

namespace PureDOTS.AI.MindECS.Components
{
    /// <summary>
    /// Behavior tree references and action preferences for cognitive agents.
    /// Managed class component for DefaultEcs (Mind ECS layer).
    /// </summary>
    public class BehaviorProfile
    {
        // Behavior tree node IDs (references to external behavior tree definitions)
        public List<int> BehaviorTreeNodes;

        // Action preference weights (higher = more likely to choose)
        public Dictionary<string, float> ActionPreferences;

        // Decision weights for different contexts
        public float CombatWeight;
        public float ExplorationWeight;
        public float SocialWeight;
        public float ResourceGatheringWeight;

        // Current behavior state
        public string CurrentBehaviorState;

        public BehaviorProfile()
        {
            BehaviorTreeNodes = new List<int>();
            ActionPreferences = new Dictionary<string, float>();
            CombatWeight = 0.25f;
            ExplorationWeight = 0.25f;
            SocialWeight = 0.25f;
            ResourceGatheringWeight = 0.25f;
            CurrentBehaviorState = "Idle";
        }
    }
}

