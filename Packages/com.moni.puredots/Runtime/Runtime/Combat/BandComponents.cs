using System;
using PureDOTS.Runtime.Registry;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Identifies the logical band or squad an entity belongs to.
    /// </summary>
    public struct BandId : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Aggregated status information for a band.
    /// </summary>
    public struct BandStats : IComponentData
    {
        public int MemberCount;
        public float Morale;
        public byte Flags;
    }

    /// <summary>
    /// Flag set describing high level band states. Keep values generic for reuse across game modes.
    /// </summary>
    public static class BandStatusFlags
    {
        public const byte Engaged = 1 << 0;
        public const byte Retreating = 1 << 1;
        public const byte Idle = 1 << 2;
    }

    /// <summary>
    /// Singleton aggregating registry level totals for all bands.
    /// </summary>
    public struct BandRegistry : IComponentData
    {
        public int TotalBands;
        public int TotalMembers;
        public uint LastUpdateTick;
        public uint LastSpatialVersion;
        public int SpatialResolvedCount;
        public int SpatialFallbackCount;
        public int SpatialUnmappedCount;
    }

    /// <summary>
    /// Buffer entry describing a single band snapshot used by deterministic registry tooling.
    /// </summary>
    public struct BandRegistryEntry : IBufferElementData, IComparable<BandRegistryEntry>, IRegistryEntry, IRegistryFlaggedEntry
    {
        public Entity BandEntity;
        public int BandId;
        public float3 Position;
        public int MemberCount;
        public float Morale;
        public byte Flags;
        public int CellId;
        public uint SpatialVersion;

        public int CompareTo(BandRegistryEntry other)
        {
            return BandEntity.Index.CompareTo(other.BandEntity.Index);
        }

        public Entity RegistryEntity => BandEntity;

        public byte RegistryFlags => Flags;
    }
}

