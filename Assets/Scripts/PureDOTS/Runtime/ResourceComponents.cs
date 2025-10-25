using System;
using PureDOTS.Runtime.Registry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    public struct ResourceTypeId : IComponentData
    {
        public FixedString64Bytes Value;
    }

    public struct ResourceSourceConfig : IComponentData
    {
        public float GatherRatePerWorker;
        public int MaxSimultaneousWorkers;
        public float RespawnSeconds;
        public byte Flags;

        public const byte FlagInfinite = 1 << 0;
        public const byte FlagRespawns = 1 << 1;
        public const byte FlagHandUprootAllowed = 1 << 2;
    }

    public struct ResourceSourceState : IComponentData
    {
        public float UnitsRemaining;
    }

    public struct StorehouseConfig : IComponentData
    {
        public float ShredRate;
        public int MaxShredQueueSize;
        public float InputRate;
        public float OutputRate;
    }

    public struct StorehouseInventory : IComponentData
    {
        public float TotalStored;
        public float TotalCapacity;
        public int ItemTypeCount;
        public byte IsShredding;
        public uint LastUpdateTick;
    }

    public struct StorehouseInventoryItem : IBufferElementData
    {
        public FixedString64Bytes ResourceTypeId;
        public float Amount;
        public float Reserved;
    }

    public struct ResourceChunkConfig : IComponentData
    {
        public float MassPerUnit;
        public float MinScale;
        public float MaxScale;
        public float DefaultUnits;
    }

    [System.Flags]
    public enum ResourceChunkFlags : byte
    {
        None = 0,
        Carried = 1 << 0,
        Thrown = 1 << 1,
        PendingDestroy = 1 << 2
    }

    public struct ResourceChunkState : IComponentData
    {
        public ushort ResourceTypeIndex;
        public float Units;
        public Entity SourceEntity;
        public Entity Carrier;
        public ResourceChunkFlags Flags;
        public float3 Velocity;
        public float Age;
    }

    [System.Flags]
    public enum ResourceChunkSpawnFlags : byte
    {
        None = 0,
        AttachToRequester = 1 << 0,
        InheritVelocity = 1 << 1
    }

    public struct ResourceChunkSpawnCommand : IBufferElementData
    {
        public ushort ResourceTypeIndex;
        public float Units;
        public Entity Requester;
        public float3 SpawnPosition;
        public float3 LocalOffset;
        public float3 InitialVelocity;
        public ResourceChunkSpawnFlags Flags;
    }

    public struct ConstructionSiteProgress : IComponentData
    {
        public float RequiredProgress;
        public float CurrentProgress;
    }

    public struct ConstructionCostElement : IBufferElementData
    {
        public FixedString64Bytes ResourceTypeId;
        public float UnitsRequired;
    }

    public struct StorehouseCapacityElement : IBufferElementData
    {
        public FixedString64Bytes ResourceTypeId;
        public float MaxCapacity;
    }

    public struct ConstructionDeliveredElement : IBufferElementData
    {
        public FixedString64Bytes ResourceTypeId;
        public float UnitsDelivered;
    }

    public struct ConstructionSiteId : IComponentData
    {
        public int Value;
    }

    public struct ConstructionSiteFlags : IComponentData
    {
        public const byte Completed = 1 << 0;
        public byte Value;
    }

    public struct ConstructionCompletionPrefab : IComponentData
    {
        public Entity Prefab;
        public bool DestroySiteEntity;
    }

    public struct ConstructionCommandTag : IComponentData
    {
    }

    public struct ConstructionDepositCommand : IBufferElementData
    {
        public int SiteId;
        public FixedString64Bytes ResourceTypeId;
        public float Amount;
    }

    public struct ConstructionProgressCommand : IBufferElementData
    {
        public int SiteId;
        public float Delta;
    }

    // Resource Registry Components
    public struct ResourceTypeIndex : IComponentData
    {
        public BlobAssetReference<ResourceTypeIndexBlob> Catalog;
    }

    public struct ResourceRegistry : IComponentData
    {
        public int TotalResources;
        public int TotalActiveResources;
        public uint LastUpdateTick;
    }

    public struct ResourceRegistryEntry : IBufferElementData, IComparable<ResourceRegistryEntry>, IRegistryEntry, IRegistryFlaggedEntry
    {
        public ushort ResourceTypeIndex;
        public Entity SourceEntity;
        public float3 Position;
        public float UnitsRemaining;
        public byte ActiveTickets;
        public byte ClaimFlags;
        public uint LastMutationTick;

        public int CompareTo(ResourceRegistryEntry other)
        {
            return SourceEntity.Index.CompareTo(other.SourceEntity.Index);
        }

        public Entity RegistryEntity => SourceEntity;

        public byte RegistryFlags => ClaimFlags;
    }

    public struct StorehouseRegistry : IComponentData
    {
        public int TotalStorehouses;
        public float TotalCapacity;
        public float TotalStored;
        public uint LastUpdateTick;
    }

    public struct StorehouseRegistryCapacitySummary
    {
        public ushort ResourceTypeIndex;
        public float Capacity;
        public float Stored;
        public float Reserved;
    }

    public struct StorehouseRegistryEntry : IBufferElementData, IComparable<StorehouseRegistryEntry>, IRegistryEntry
    {
        public Entity StorehouseEntity;
        public float3 Position;
        public float TotalCapacity;
        public float TotalStored;
        public FixedList32Bytes<StorehouseRegistryCapacitySummary> TypeSummaries;
        public uint LastMutationTick;

        public int CompareTo(StorehouseRegistryEntry other)
        {
            return StorehouseEntity.Index.CompareTo(other.StorehouseEntity.Index);
        }

        public Entity RegistryEntity => StorehouseEntity;
    }

    public struct ResourceJobReservation : IComponentData
    {
        public byte ActiveTickets;
        public byte PendingTickets;
        public float ReservedUnits;
        public uint LastMutationTick;
        public byte ClaimFlags;
    }

    public struct ResourceActiveTicket : IBufferElementData
    {
        public Entity Villager;
        public uint TicketId;
        public float ReservedUnits;
    }

    public struct StorehouseJobReservation : IComponentData
    {
        public float ReservedCapacity;
        public uint LastMutationTick;
    }

    public struct StorehouseReservationItem : IBufferElementData
    {
        public ushort ResourceTypeIndex;
        public float Reserved;
    }

    public static class ResourceRegistryClaimFlags
    {
        public const byte PlayerClaim = 1 << 0;
        public const byte VillagerReserved = 1 << 1;
    }
}
