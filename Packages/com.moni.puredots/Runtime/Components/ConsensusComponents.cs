using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Consensus tier levels for hierarchical arbitration.
    /// </summary>
    public enum ConsensusTier : byte
    {
        Local = 0,      // Village/fleet level
        Regional = 1,   // Alliance/cross-cluster level
        Global = 2      // God-layer/macro decisions
    }

    /// <summary>
    /// Local cluster consensus state for village/fleet voting.
    /// Agents vote on outcomes within their cluster.
    /// </summary>
    public struct LocalConsensusState : IComponentData
    {
        public AgentGuid ClusterGuid;           // Which cluster this agent belongs to
        public AgentGuid ClusterLeaderGuid;    // Leader of the cluster
        public byte VoteWeight;                 // Voting power (0-255)
        public uint LastVoteTick;               // Last time agent voted
        public ConsensusTier CurrentTier;      // Current consensus tier
    }

    /// <summary>
    /// Regional hub consensus state for cross-cluster conflict resolution.
    /// Resolves conflicts between multiple clusters.
    /// </summary>
    public struct RegionalConsensusState : IComponentData
    {
        public AgentGuid HubGuid;              // Regional hub identifier
        public AgentGuid HubLeaderGuid;         // Leader of the regional hub
        public int ClusterCount;                // Number of clusters in this region
        public uint LastArbitrationTick;       // Last time arbitration occurred
    }

    /// <summary>
    /// Global orchestrator state for macro decisions.
    /// Handles god-layer decisions affecting all entities.
    /// </summary>
    public struct GlobalConsensusState : IComponentData
    {
        public AgentGuid OrchestratorGuid;     // Global orchestrator identifier
        public int RegionalHubCount;            // Number of regional hubs
        public uint LastMacroDecisionTick;      // Last time macro decision was made
    }

    /// <summary>
    /// Consensus vote cast by an agent.
    /// </summary>
    public struct ConsensusVote : IBufferElementData
    {
        public AgentGuid VoterGuid;            // Who cast the vote
        public AgentGuid ClusterGuid;           // Which cluster the vote belongs to
        public byte VoteValue;                  // Vote value (0-255)
        public ConsensusTier Tier;              // Which tier this vote is for
        public uint TickNumber;                  // When the vote was cast
    }

    /// <summary>
    /// Consensus outcome resolved at a tier.
    /// </summary>
    public struct ConsensusOutcome : IBufferElementData
    {
        public AgentGuid ClusterGuid;          // Cluster this outcome applies to
        public ConsensusTier Tier;              // Tier that resolved this
        public byte ResolvedValue;              // Resolved consensus value
        public int VoteCount;                   // Number of votes that contributed
        public uint ResolutionTick;            // When this was resolved
    }
}

