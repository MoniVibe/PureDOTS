using Unity.Collections;
using Unity.Entities;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Decision reasoning component storing explanation for AI choices.
    /// Each Mind ECS system emits summary string or enum code.
    /// </summary>
    public struct DecisionReasoning : IComponentData
    {
        public FixedString128Bytes Reason;  // Human-readable reason ("Reason: threat proximity")
        public DecisionReasonCode Code;     // Enum code for structured reasoning
        public float Confidence;              // Confidence in decision (0-1)
        public uint DecisionTick;             // When decision was made
    }

    /// <summary>
    /// Enum codes for structured decision reasoning.
    /// </summary>
    public enum DecisionReasonCode : byte
    {
        None = 0,
        ThreatProximity = 1,
        LowFocus = 2,
        ReputationPenalty = 3,
        ResourceNeed = 4,
        SocialUrgency = 5,
        GoalPriority = 6,
        ConstraintViolation = 7
    }

    /// <summary>
    /// Decision tree node for explainability.
    /// Stores structured decision tree for debugging.
    /// </summary>
    public struct DecisionTreeNode : IBufferElementData
    {
        public FixedString64Bytes NodeId;   // Node identifier
        public DecisionReasonCode Reason;   // Reason at this node
        public float Score;                  // Score at this node
        public int ParentIndex;              // Index of parent node (-1 for root)
        public uint EvaluationTick;          // When node was evaluated
    }
}

