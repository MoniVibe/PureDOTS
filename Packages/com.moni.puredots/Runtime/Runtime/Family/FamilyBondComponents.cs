using Unity.Entities;

namespace PureDOTS.Runtime.Family
{
    /// <summary>
    /// Policy for how families should be joined when marriage occurs.
    /// </summary>
    public enum MarriageJoinPolicy : byte
    {
        KeepSeparate = 0,
        JoinPartnerA = 1,
        JoinPartnerB = 2,
        CreateNewFamily = 3
    }

    /// <summary>
    /// Event that establishes a marriage bond between two entities.
    /// </summary>
    public struct MarriageEvent : IComponentData
    {
        public Entity PartnerA;
        public Entity PartnerB;
        public MarriageJoinPolicy JoinPolicy;
        public byte IsPolitical;
        public uint RequestedTick;
    }

    /// <summary>
    /// Event that establishes an adoption relationship between a parent and child.
    /// </summary>
    public struct AdoptionEvent : IComponentData
    {
        public Entity Parent;
        public Entity Child;
        public byte JoinDynasty;
        public uint RequestedTick;
    }
}
