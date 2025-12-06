using System;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Bands
{
    public struct BandId : IComponentData
    {
        public int Value;
        public int FactionId;
        public Entity Leader;
    }

    [Flags]
    public enum BandStatusFlags : byte
    {
        None = 0,
        Idle = 1 << 0,
        Moving = 1 << 1,
        Engaged = 1 << 2,
        Routing = 1 << 3,
        NeedsSupply = 1 << 4,
        Resting = 1 << 5
    }

    public struct BandStats : IComponentData
    {
        public int MemberCount;
        public float AverageDiscipline;
        public float Morale;
        public float Cohesion;
        public float Fatigue;
        public BandStatusFlags Flags;
        public uint LastUpdateTick;
    }

    public enum BandFormationType : byte
    {
        Column = 0,
        Line = 1,
        Wedge = 2,
        Circle = 3
    }

    public struct BandFormation : IComponentData
    {
        public BandFormationType Formation;
        public float Spacing;
        public float Width;
        public float Depth;
        public float3 Facing;
        public float3 Anchor;
        public float Stability;
        public uint LastSolveTick;
        public float Cohesion;      // 0-1, average alignment of members
        public float Morale;        // Group morale (0-1)
        public ushort FormationId;  // Unique formation identifier
    }

    public struct BandMember : IBufferElementData
    {
        public Entity Villager;
        public byte Role;
    }

    public struct BandIntent : IComponentData
    {
        public byte DesiredAction;
        public float IntentWeight;
    }

    /// <summary>
    /// Command issued to a formation from strategic layer.
    /// </summary>
    public struct FormationCommand : IComponentData
    {
        public ushort CommandId;  // Move, Attack, Hold, Regroup
        public float3 TargetPos;
        public float3 Facing;
    }

    /// <summary>
    /// Member of a formation with desired offset and alignment.
    /// </summary>
    public struct FormationMember : IComponentData
    {
        public Entity FormationEntity;
        public float3 Offset;      // Desired offset from formation center
        public float Alignment;    // Adherence to group (0-1)
    }

    /// <summary>
    /// Leader influence field that radiates cohesion and morale bonuses.
    /// </summary>
    public struct CommandAura : IComponentData
    {
        public float Radius;
        public float CohesionBonus;
        public float MoraleBonus;
    }
}
