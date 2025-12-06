using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Goal priorities for utility-aware learning.
    /// Agents learn soft preferences instead of hard scripts.
    /// </summary>
    public struct GoalPriorities : IComponentData
    {
        public float HarvestPriority;      // Priority for harvest goals (0-1)
        public float DefendPriority;       // Priority for defend goals (0-1)
        public float PatrolPriority;       // Priority for patrol goals (0-1)
        public float RestPriority;          // Priority for rest goals (0-1)
        public float BuildPriority;         // Priority for build goals (0-1)
        public uint LastUpdateTick;         // When priorities were last updated
    }

    /// <summary>
    /// Soft preferences for utility learning.
    /// Stores learned preferences that adapt over time.
    /// </summary>
    public struct SoftPreferences : IComponentData
    {
        public float FoodPreference;        // Preference for food-related tasks (0-1)
        public float DefensePreference;     // Preference for defense tasks (0-1)
        public float GrowthPreference;      // Preference for growth tasks (0-1)
        public float SocialPreference;      // Preference for social tasks (0-1)
        public float LearningRate;          // How quickly preferences adapt (0-1)
        public uint LastLearningTick;       // When preferences were last updated
    }
}

