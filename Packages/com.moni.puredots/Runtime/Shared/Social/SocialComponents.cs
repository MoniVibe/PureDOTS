using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.AI.Social
{
    /// <summary>
    /// Social message for communication and cooperation protocols.
    /// Used in message passing between agents for structured interactions.
    /// Based on Hoey et al. (2018) communication patterns.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct SocialMessage : IBufferElementData
    {
        public SocialMessageType Type;      // Offer, Request, Threat, Praise, Inquiry, etc.
        public AgentGuid SenderGuid;
        public AgentGuid ReceiverGuid;
        public float Urgency;               // Priority weight (0-1)
        public float Payload;               // Trade value, trust delta, etc.
        public ushort Flags;                // Message flags (urgent, broadcast, etc.)
        public uint TickNumber;              // Timestamp from simulation tick
        public float3 ContextPosition;      // Optional spatial context
    }

    /// <summary>
    /// Social knowledge component storing trust, reputation, and cooperation bias per relationship.
    /// Sparse relationship matrix entry - only stores relationships with known agents.
    /// Based on Kozlowski et al. (2016) trust network patterns.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct SocialRelationship : IBufferElementData
    {
        public AgentGuid OtherAgentGuid;
        public float Trust;                 // Trust toward this individual (0-1)
        public float Reputation;            // Aggregate perception of the other (0-1)
        public float CooperationBias;       // Bias toward cooperation with this agent (-1 to 1)
        public uint LastInteractionTick;    // Last time interaction occurred
        public float InteractionCount;     // Number of interactions (for decay calculations)
    }

    /// <summary>
    /// Social knowledge component aggregating relationship data.
    /// Stores aggregate trust/reputation metrics and cooperation preferences.
    /// </summary>
    public struct SocialKnowledge : IComponentData
    {
        public float BaseTrust;             // Base trust level (0-1)
        public float BaseReputation;        // Base reputation (0-1)
        public float CooperationBias;       // General cooperation bias (-1 to 1, positive = cooperative)
        public float LearningRate;          // Rate of trust/reputation updates (0-1)
        public uint LastUpdateTick;         // Last time social knowledge was updated
    }

    /// <summary>
    /// Group goal component for aggregates (villages, fleets, bands).
    /// Manages cooperation/competition weights and shared goals.
    /// Based on Pagliuca et al. (2023) goal balancing patterns.
    /// </summary>
    public struct GroupGoal : IComponentData
    {
        public float CooperationWeight;     // Weight for cooperative actions (0-1)
        public float CompetitionWeight;     // Weight for competitive actions (0-1)
        public float ResourcePriority;      // Priority for resource acquisition (0-1)
        public float ThreatLevel;           // Perceived threat level (0-1)
        public float GroupCohesion;         // Group cohesion metric (0-1)
        public uint LastEvaluationTick;     // Last time goals were evaluated
    }

    /// <summary>
    /// Cultural signal for social learning and cultural propagation.
    /// Broadcast when successful cooperation occurs, received by nearby entities.
    /// Based on Nehaniv & Dautenhahn (2009) cultural evolution patterns.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct CulturalSignal : IBufferElementData
    {
        public ushort DoctrineId;           // ID of the cultural doctrine/strategy
        public float Strength;              // Signal strength (0-1)
        public float Decay;                 // Decay rate per tick (0-1)
        public AgentGuid SourceGuid;         // Source agent that broadcast the signal
        public uint BroadcastTick;          // Tick when signal was broadcast
    }

    /// <summary>
    /// Motivation component for morale, hope, and social pressure.
    /// Drives individual motivation outside of combat.
    /// </summary>
    public struct Motivation : IComponentData
    {
        public float Morale;                // Morale level (0-1), rises from success, decays with unmet needs
        public float Hope;                  // Hope level (0-1), future expectation
        public float Pressure;              // External constraint/pressure (0-1)
        public float Courage;               // Courage stat (0-1), affects revolt threshold
        public uint LastUpdateTick;         // Last time motivation was updated
    }

    /// <summary>
    /// Doctrine weight component for cultural memory.
    /// Stores weights for different cultural doctrines/strategies.
    /// Used in Mind ECS for cultural propagation.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct DoctrineWeight : IBufferElementData
    {
        public ushort DoctrineId;
        public float Weight;                // Weight/strength of this doctrine (0-1)
        public uint LastUpdateTick;         // Last time weight was updated
    }
}

