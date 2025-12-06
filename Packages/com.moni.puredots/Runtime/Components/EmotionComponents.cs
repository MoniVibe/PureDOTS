using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Emotion state for an entity.
    /// Stores current emotional state with decay over time.
    /// </summary>
    public struct EmotionState : IComponentData
    {
        public float Happiness;             // Happiness level (0-1)
        public float Fear;                  // Fear level (0-1)
        public float Anger;                 // Anger level (0-1)
        public float Trust;                  // Trust level (0-1)
        public float DecayRate;              // Emotion decay rate per tick
        public uint LastUpdateTick;          // When emotion was last updated
    }

    /// <summary>
    /// Interaction digest storing compressed event data.
    /// Positive/negative deltas with weighted decay over time.
    /// </summary>
    public struct InteractionDigest : IBufferElementData
    {
        public AgentGuid InteractorGuid;    // Who interacted
        public AgentGuid TargetGuid;         // Who was interacted with
        public float PositiveDelta;          // Positive interaction value
        public float NegativeDelta;          // Negative interaction value
        public float Weight;                 // Interaction weight (decays over time)
        public uint InteractionTick;         // When interaction occurred
        public InteractionType Type;          // Type of interaction
    }

    /// <summary>
    /// Types of interactions.
    /// </summary>
    public enum InteractionType : byte
    {
        Help = 0,
        Harm = 1,
        Trade = 2,
        Social = 3,
        Combat = 4
    }
}

