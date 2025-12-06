using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.AI.Cognitive
{
    /// <summary>
    /// Action ID enum for procedural memory actions.
    /// Maps to specific action types agents can perform.
    /// </summary>
    public enum ActionId : byte
    {
        None = 0,
        Move = 1,
        Climb = 2,
        Push = 3,
        Pull = 4,
        Jump = 5,
        Throw = 6,
        Use = 7,
        Grab = 8,
        Drop = 9,
        EscapePit = 10, // Macro-action example
        Custom0 = 240
    }

    /// <summary>
    /// Procedural memory component storing state-action-outcome table per agent.
    /// Maintains tried actions and success scores for context-based learning.
    /// </summary>
    public struct ProceduralMemory : IComponentData
    {
        /// <summary>
        /// Actions tried in current context.
        /// </summary>
        public FixedList64Bytes<ActionId> TriedActions;

        /// <summary>
        /// Success scores corresponding to TriedActions (same index).
        /// Range: 0.0 (failure) to 1.0 (success).
        /// </summary>
        public FixedList64Bytes<float> SuccessScores;

        /// <summary>
        /// Situation fingerprint hash (terrain + obstacle + goal).
        /// </summary>
        public byte ContextHash;

        /// <summary>
        /// Learning rate for reinforcement updates (0.0 to 1.0).
        /// Higher values adapt faster but may be less stable.
        /// </summary>
        public float LearningRate;

        /// <summary>
        /// Last tick when memory was updated.
        /// </summary>
        public uint LastUpdateTick;

        /// <summary>
        /// Number of successful action chains stored for this context.
        /// </summary>
        public byte SuccessChainCount;
    }
}

