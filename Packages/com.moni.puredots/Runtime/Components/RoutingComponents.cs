using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Importance score for routing decisions.
    /// Formula: task_value / global_relevance
    /// </summary>
    public struct ImportanceScore : IComponentData
    {
        public float TaskValue;            // Value of the task (0-1)
        public float GlobalRelevance;      // How relevant this is globally (0-1)
        public float Score;                 // Computed score (task_value / global_relevance)
        public uint LastUpdateTick;        // When score was last computed
    }

    /// <summary>
    /// Continuity score for routing decisions.
    /// Formula: contextual_fit / local_consistency
    /// </summary>
    public struct ContinuityScore : IComponentData
    {
        public float ContextualFit;        // How well this fits context (0-1)
        public float LocalConsistency;     // Local consistency measure (0-1)
        public float Score;                 // Computed score (contextual_fit / local_consistency)
        public uint LastUpdateTick;        // When score was last computed
    }

    /// <summary>
    /// Routing decision component for self-organizing message routing.
    /// Nodes choose next message recipient locally (no central coordinator).
    /// </summary>
    public struct RoutingDecision : IComponentData
    {
        public AgentGuid NextRecipientGuid; // Next node to route message to
        public float CombinedScore;          // ImportanceScore + ContinuityScore
        public RoutingDecisionReason Reason; // Why this routing decision was made
        public uint DecisionTick;            // When decision was made
    }

    /// <summary>
    /// Reason for routing decision.
    /// </summary>
    public enum RoutingDecisionReason : byte
    {
        HighestScore = 0,
        LocalOptimal = 1,
        Fallback = 2,
        Random = 3
    }

    /// <summary>
    /// Routing node state for self-organizing network.
    /// </summary>
    public struct RoutingNodeState : IComponentData
    {
        public AgentGuid NodeGuid;          // This node's identifier
        public int NeighborCount;            // Number of known neighbors
        public float AverageLatency;        // Average message latency
        public uint LastRoutingTick;        // When routing was last updated
    }
}

