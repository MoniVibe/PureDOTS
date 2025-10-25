using System;
using PureDOTS.Runtime.Registry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Transport
{
    /// <summary>
    /// Shared enumeration describing transport unit state flags.
    /// </summary>
    [Flags]
    public enum TransportUnitFlags : byte
    {
        None = 0,
        Idle = 1 << 0,
        Assigned = 1 << 1,
        Carrying = 1 << 2,
        Disabled = 1 << 3
    }

    /// <summary>
    /// Registry summary for autonomous miner vessels.
    /// </summary>
    public struct MinerVesselRegistry : IComponentData
    {
        public int TotalVessels;
        public int AvailableVessels;
        public float TotalCapacity;
        public uint LastUpdateTick;
    }

    public struct MinerVesselRegistryEntry : IBufferElementData, IComparable<MinerVesselRegistryEntry>, IRegistryEntry, IRegistryFlaggedEntry
    {
        public Entity VesselEntity;
        public float3 Position;
        public ushort ResourceTypeIndex;
        public float Capacity;
        public float Load;
        public TransportUnitFlags Flags;
        public uint LastCommandTick;

        public int CompareTo(MinerVesselRegistryEntry other)
        {
            return VesselEntity.Index.CompareTo(other.VesselEntity.Index);
        }

        public Entity RegistryEntity => VesselEntity;

        public byte RegistryFlags => (byte)Flags;
    }

    /// <summary>
    /// Registry summary for hauler units servicing storage and production.
    /// </summary>
    public struct HaulerRegistry : IComponentData
    {
        public int TotalHaulers;
        public int IdleHaulers;
        public uint LastUpdateTick;
    }

    public struct HaulerRegistryEntry : IBufferElementData, IComparable<HaulerRegistryEntry>, IRegistryEntry, IRegistryFlaggedEntry
    {
        public Entity HaulerEntity;
        public float3 Position;
        public ushort CargoTypeIndex;
        public float ReservedCapacity;
        public float EstimatedTravelTime;
        public int RouteId;
        public TransportUnitFlags Flags;

        public int CompareTo(HaulerRegistryEntry other)
        {
            return HaulerEntity.Index.CompareTo(other.HaulerEntity.Index);
        }

        public Entity RegistryEntity => HaulerEntity;

        public byte RegistryFlags => (byte)Flags;
    }

    /// <summary>
    /// Registry summary for long-range freighters.
    /// </summary>
    public struct FreighterRegistry : IComponentData
    {
        public int TotalFreighters;
        public int ActiveFreighters;
        public uint LastUpdateTick;
    }

    public struct FreighterRegistryEntry : IBufferElementData, IComparable<FreighterRegistryEntry>, IRegistryEntry, IRegistryFlaggedEntry
    {
        public Entity FreighterEntity;
        public float3 Position;
        public float3 Destination;
        public FixedString64Bytes ManifestId;
        public float PayloadCapacity;
        public float PayloadLoaded;
        public TransportUnitFlags Flags;

        public int CompareTo(FreighterRegistryEntry other)
        {
            return FreighterEntity.Index.CompareTo(other.FreighterEntity.Index);
        }

        public Entity RegistryEntity => FreighterEntity;

        public byte RegistryFlags => (byte)Flags;
    }

    /// <summary>
    /// Registry summary for ground wagons.
    /// </summary>
    public struct WagonRegistry : IComponentData
    {
        public int TotalWagons;
        public int AvailableWagons;
        public uint LastUpdateTick;
    }

    public struct WagonRegistryEntry : IBufferElementData, IComparable<WagonRegistryEntry>, IRegistryEntry, IRegistryFlaggedEntry
    {
        public Entity WagonEntity;
        public Entity AssignedVillager;
        public float3 Position;
        public float CargoCapacity;
        public float CargoReserved;
        public TransportUnitFlags Flags;

        public int CompareTo(WagonRegistryEntry other)
        {
            return WagonEntity.Index.CompareTo(other.WagonEntity.Index);
        }

        public Entity RegistryEntity => WagonEntity;

        public byte RegistryFlags => (byte)Flags;
    }
}
