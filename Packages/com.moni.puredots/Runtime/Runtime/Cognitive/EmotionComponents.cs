using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Cognitive
{
    /// <summary>
    /// Emotional state vector for an entity.
    /// Emotions act as modulators on learning and decision-making.
    /// Updated in MindECS (non-deterministic), synced to Body ECS.
    /// </summary>
    public struct EmotionState : IComponentData
    {
        /// <summary>Anger level (0-1, where 1 = maximum anger)</summary>
        public float Anger;

        /// <summary>Trust level (0-1, where 1 = maximum trust)</summary>
        public float Trust;

        /// <summary>Fear level (0-1, where 1 = maximum fear)</summary>
        public float Fear;

        /// <summary>Pride level (0-1, where 1 = maximum pride)</summary>
        public float Pride;

        /// <summary>Last tick when emotions were updated</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Emotion-derived modifiers for learning and decision-making.
    /// Computed from EmotionState in Body ECS for deterministic modifiers.
    /// </summary>
    public struct EmotionModulator : IComponentData
    {
        /// <summary>Learning rate multiplier: LearningRate *= (1 + Pride - Fear)</summary>
        public float LearningRateMultiplier;

        /// <summary>Bias adjustment per culture: Bias[culture] += Anger * 0.1f</summary>
        public float BiasAdjustment;

        /// <summary>Decision confidence modifier (affects utility scoring)</summary>
        public float ConfidenceModifier;

        /// <summary>Last tick when modulator was computed</summary>
        public uint LastUpdateTick;
    }
}

