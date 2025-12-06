using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Reputation graph node for an entity.
    /// Tracks reputation with other entities.
    /// </summary>
    public struct ReputationNode : IComponentData
    {
        public AgentGuid EntityGuid;        // Entity this node represents
        public float OverallReputation;      // Overall reputation score (-1 to 1)
        public int InteractionCount;         // Number of interactions
        public uint LastUpdateTick;           // When reputation was last updated
    }

    /// <summary>
    /// Reputation edge representing reputation relationship between entities.
    /// </summary>
    public struct ReputationEdge : IBufferElementData
    {
        public AgentGuid SourceGuid;         // Source entity
        public AgentGuid TargetGuid;          // Target entity
        public float ReputationValue;         // Reputation value (-1 to 1)
        public float Weight;                  // Edge weight (decays over time)
        public uint LastUpdateTick;           // When edge was last updated
    }

    /// <summary>
    /// Sentiment matrix for aggregate/faction level reputation.
    /// Computed from individual reputation graphs.
    /// </summary>
    public struct SentimentMatrix : IComponentData
    {
        public AgentGuid FactionGuid;        // Faction identifier
        public int MatrixSize;                // Size of sentiment matrix
        public uint LastComputationTick;      // When matrix was last computed
    }

    /// <summary>
    /// Sentiment matrix entry storing faction-to-faction sentiment.
    /// </summary>
    public struct SentimentMatrixEntry : IBufferElementData
    {
        public AgentGuid SourceFactionGuid;   // Source faction
        public AgentGuid TargetFactionGuid;    // Target faction
        public float SentimentValue;          // Sentiment value (-1 to 1)
        public uint LastUpdateTick;           // When entry was last updated
    }
}

