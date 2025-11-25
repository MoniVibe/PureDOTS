using Unity.Collections;
using Unity.Entities;

namespace Space4X.Knowledge
{
    //==========================================================================
    // Trade Route Knowledge Spread (Commerce-Based)
    //==========================================================================

    /// <summary>
    /// Knowledge transfer occurring via trade routes.
    /// </summary>
    public struct TradeRouteKnowledgeTransfer : IBufferElementData
    {
        /// <summary>
        /// Source colony exporting knowledge.
        /// </summary>
        public Entity SourceColony;

        /// <summary>
        /// Destination colony receiving knowledge.
        /// </summary>
        public Entity DestColony;

        /// <summary>
        /// Knowledge being transferred.
        /// </summary>
        public FixedString64Bytes KnowledgeId;

        /// <summary>
        /// Type of knowledge.
        /// </summary>
        public KnowledgeType Type;

        /// <summary>
        /// Transfer rate per trade tick.
        /// </summary>
        public float TransferRate;

        /// <summary>
        /// Current transfer progress (0-1).
        /// </summary>
        public float Progress;

        /// <summary>
        /// Trade route entity facilitating transfer.
        /// </summary>
        public Entity TradeRouteEntity;
    }

    /// <summary>
    /// Type of knowledge being transferred.
    /// </summary>
    public enum KnowledgeType : byte
    {
        /// <summary>
        /// Technical/scientific knowledge.
        /// </summary>
        Technology = 0,

        /// <summary>
        /// Cultural stories/traditions.
        /// </summary>
        Culture = 1,

        /// <summary>
        /// Tactical/military knowledge.
        /// </summary>
        Tactical = 2,

        /// <summary>
        /// Faction-restricted classified info.
        /// </summary>
        Classified = 3,

        /// <summary>
        /// Economic/trade knowledge.
        /// </summary>
        Economic = 4,

        /// <summary>
        /// Navigation/exploration data.
        /// </summary>
        Navigation = 5
    }

    //==========================================================================
    // Communication Network Spread (Instant Within Faction)
    //==========================================================================

    /// <summary>
    /// Communication network state for a colony/ship.
    /// </summary>
    public struct CommNetworkState : IComponentData
    {
        /// <summary>
        /// Faction this entity belongs to.
        /// </summary>
        public Entity FactionEntity;

        /// <summary>
        /// Whether connected to faction network.
        /// </summary>
        public bool IsConnected;

        /// <summary>
        /// Signal strength (0-1, affects transfer speed).
        /// </summary>
        public float SignalStrength;

        /// <summary>
        /// Bandwidth available for transfers.
        /// </summary>
        public float Bandwidth;

        /// <summary>
        /// Tick of last successful sync.
        /// </summary>
        public uint LastSyncTick;

        /// <summary>
        /// Network status flags.
        /// </summary>
        public CommNetworkFlags Flags;
    }

    /// <summary>
    /// Communication network flags.
    /// </summary>
    [System.Flags]
    public enum CommNetworkFlags : byte
    {
        None = 0,
        HasRelay = 1 << 0,        // Has local relay station
        IsJammed = 1 << 1,        // Communications disrupted
        IsEncrypted = 1 << 2,     // Using secure channels
        HasQuantumLink = 1 << 3,  // Instant communication
        IsIsolated = 1 << 4       // Cut off from network
    }

    /// <summary>
    /// Pending message in communication queue.
    /// </summary>
    public struct CommMessage : IBufferElementData
    {
        /// <summary>
        /// Message identifier.
        /// </summary>
        public uint MessageId;

        /// <summary>
        /// Sender entity.
        /// </summary>
        public Entity SenderEntity;

        /// <summary>
        /// Target entity (or Entity.Null for broadcast).
        /// </summary>
        public Entity TargetEntity;

        /// <summary>
        /// Knowledge being transmitted.
        /// </summary>
        public FixedString64Bytes KnowledgeId;

        /// <summary>
        /// Knowledge type.
        /// </summary>
        public KnowledgeType Type;

        /// <summary>
        /// Priority level.
        /// </summary>
        public MessagePriority Priority;

        /// <summary>
        /// Tick when message was sent.
        /// </summary>
        public uint SentTick;

        /// <summary>
        /// Expected arrival tick (based on distance).
        /// </summary>
        public uint ExpectedArrivalTick;
    }

    /// <summary>
    /// Message priority levels.
    /// </summary>
    public enum MessagePriority : byte
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3,
        Emergency = 4
    }

    //==========================================================================
    // Physical Transfer (Crew/Ships Carry Knowledge)
    //==========================================================================

    /// <summary>
    /// Knowledge being physically transported by a ship/crew.
    /// </summary>
    public struct KnowledgeCargo : IBufferElementData
    {
        /// <summary>
        /// Knowledge identifier.
        /// </summary>
        public FixedString64Bytes KnowledgeId;

        /// <summary>
        /// Type of knowledge.
        /// </summary>
        public KnowledgeType Type;

        /// <summary>
        /// Colony where knowledge originated.
        /// </summary>
        public Entity OriginColony;

        /// <summary>
        /// Intended destination colony.
        /// </summary>
        public Entity DestinationColony;

        /// <summary>
        /// Data integrity (0-1, degrades without proper storage).
        /// </summary>
        public float Integrity;

        /// <summary>
        /// Security level of the cargo.
        /// </summary>
        public SecurityClassification Security;

        /// <summary>
        /// Tick when cargo was loaded.
        /// </summary>
        public uint LoadedTick;
    }

    /// <summary>
    /// Capacity for carrying knowledge physically.
    /// </summary>
    public struct KnowledgeCargoCapacity : IComponentData
    {
        /// <summary>
        /// Maximum knowledge items that can be carried.
        /// </summary>
        public byte MaxItems;

        /// <summary>
        /// Current number of items.
        /// </summary>
        public byte CurrentItems;

        /// <summary>
        /// Whether ship has proper archival facilities.
        /// </summary>
        public bool HasArchivalFacilities;

        /// <summary>
        /// Integrity decay rate per tick (0 if has facilities).
        /// </summary>
        public float IntegrityDecayRate;

        /// <summary>
        /// Security clearance level of carrier.
        /// </summary>
        public SecurityClassification MaxSecurityLevel;
    }

    /// <summary>
    /// Request to transfer knowledge to another entity.
    /// </summary>
    public struct KnowledgeTransferRequest : IComponentData
    {
        /// <summary>
        /// Knowledge to transfer.
        /// </summary>
        public FixedString64Bytes KnowledgeId;

        /// <summary>
        /// Type of knowledge.
        /// </summary>
        public KnowledgeType Type;

        /// <summary>
        /// Target entity to receive knowledge.
        /// </summary>
        public Entity TargetEntity;

        /// <summary>
        /// Transfer method.
        /// </summary>
        public TransferMethod Method;

        /// <summary>
        /// Tick when request was made.
        /// </summary>
        public uint RequestTick;
    }

    /// <summary>
    /// Method of knowledge transfer.
    /// </summary>
    public enum TransferMethod : byte
    {
        /// <summary>
        /// Via trade route (slow but reliable).
        /// </summary>
        TradeRoute = 0,

        /// <summary>
        /// Via communication network (fast within faction).
        /// </summary>
        CommNetwork = 1,

        /// <summary>
        /// Physical transport by ship/courier.
        /// </summary>
        PhysicalTransport = 2,

        /// <summary>
        /// Diplomatic exchange (treaty-based).
        /// </summary>
        DiplomaticExchange = 3,

        /// <summary>
        /// Espionage (stolen).
        /// </summary>
        Espionage = 4
    }

    /// <summary>
    /// Event raised when knowledge transfer completes.
    /// </summary>
    public struct KnowledgeTransferEvent : IBufferElementData
    {
        public FixedString64Bytes KnowledgeId;
        public KnowledgeType Type;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public TransferMethod Method;
        public float FinalIntegrity;
        public uint CompletedTick;
        public KnowledgeTransferResult Result;
    }

    /// <summary>
    /// Result of knowledge transfer.
    /// </summary>
    public enum KnowledgeTransferResult : byte
    {
        Success = 0,
        PartialSuccess = 1,  // Degraded integrity
        Failed = 2,
        Intercepted = 3,     // Stolen by third party
        Blocked = 4,         // Security prevented transfer
        Timeout = 5
    }
}

