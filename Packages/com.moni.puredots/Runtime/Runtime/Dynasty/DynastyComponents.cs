using Unity.Entities;
using Unity.Collections;

namespace PureDOTS.Runtime.Dynasty
{
    /// <summary>
    /// Dynasty identity - extended family lineage controlling aggregates.
    /// </summary>
    public struct DynastyIdentity : IComponentData
    {
        public FixedString64Bytes DynastyName;
        public Entity FounderEntity;
        public Entity ControlledAggregate;
        public uint FoundedTick;
    }

    /// <summary>
    /// Dynasty member - entity belonging to a dynasty.
    /// </summary>
    public struct DynastyMember : IComponentData
    {
        public Entity DynastyEntity;
        public DynastyRank Rank;
        public float LineageStrength;
    }

    /// <summary>
    /// Dynasty ranks.
    /// </summary>
    public enum DynastyRank : byte
    {
        Founder = 0,
        Heir = 1,
        Noble = 2,
        Member = 3
    }

    /// <summary>
    /// Dynasty lineage - bloodline tracking with generations.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct DynastyLineage : IBufferElementData
    {
        public Entity MemberEntity;
        public Entity ParentA;
        public Entity ParentB;
        public uint BirthTick;
        public byte Generation;
    }

    /// <summary>
    /// Dynasty prestige - reputation of the dynasty.
    /// </summary>
    public struct DynastyPrestige : IComponentData
    {
        public float PrestigeScore;
        public float DynastyReputation;
        public uint LastUpdatedTick;
    }

    /// <summary>
    /// Dynasty member list - buffer of all members in the dynasty.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct DynastyMemberEntry : IBufferElementData
    {
        public Entity MemberEntity;
        public DynastyRank Rank;
        public float LineageStrength;
        public uint JoinedTick;
    }

    /// <summary>
    /// Legacy roster entry for all dynasty members (alive or dead).
    /// Used for lineage/legacy queries without keeping members alive in the roster.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct DynastyLegacyEntry : IBufferElementData
    {
        public Entity MemberEntity;
        public DynastyRank Rank;
        public float LineageStrength;
        public byte Generation;
        public byte IsDead;
        public uint BirthTick;
        public uint DeathTick;
    }

    /// <summary>
    /// Dynasty succession rules - how leadership passes through the dynasty.
    /// </summary>
    public struct DynastySuccessionRules : IComponentData
    {
        public PureDOTS.Runtime.Succession.SuccessionType SuccessionType;
        public byte AllowFemaleHeirs;
        public byte RequiresBloodline;
        public float MinLineageStrength;
    }

    /// <summary>
    /// Succession preferences that weight candidate selection (data-driven ethos).
    /// </summary>
    public struct DynastySuccessionPreferences : IComponentData
    {
        public float LineageWeight;
        public float WarlikeWeight;
        public float MaterialistWeight;
        public float IntellectWeight;
        public float DiplomacyWeight;
        public float SpiritualWeight;
        public float WealthWeight;
        public float PrestigeWeight;
        public float ClaimDisputeThreshold; // 0-1 claim strength for crisis
        public float MeritTieBreak;         // extra weight for merit when priority ties
    }

    /// <summary>
    /// Inheritance policy for dynasty assets and legacies.
    /// </summary>
    public struct DynastyInheritancePolicy : IComponentData
    {
        public float WealthTransferRate;   // Portion of shared wealth transferred to successor
        public float LegacyTransferRate;   // Portion of legacy value transferred
        public float InheritanceTaxRate;   // Tax/fee applied to inheritance
        public byte RequiresAcceptance;    // Heir can refuse inheritance items
    }

    /// <summary>
    /// Tracks inheritance processing for a dynasty.
    /// </summary>
    public struct DynastyInheritanceState : IComponentData
    {
        public uint LastProcessedTick;
    }

    /// <summary>
    /// Dynasty wealth - aggregated wealth of the dynasty.
    /// </summary>
    public struct DynastyWealth : IComponentData
    {
        public float TotalWealth;        // Sum of individual member balances
        public float SharedWealth;       // Shared dynasty wallet balance
        public float AverageWealth;
        public uint LastUpdatedTick;
    }
}
