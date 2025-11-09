using System;
using PureDOTS.Runtime.Registry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    public enum MiracleType : byte
    {
        Rain = 0,
        Fireball = 1,
        Heal = 2,
        Shield = 3
    }

    public struct RainMiracleConfig : IComponentData
    {
        public Entity RainCloudPrefab;
        public int CloudCount;
        public float SpawnRadius;
        public float SpawnHeightOffset;
        public float SpawnSpreadAngle;
        public uint Seed;
    }

    public struct RainMiracleCommandQueue : IComponentData { }

    /// <summary>
    /// Per-player miracle selection & casting state. One singleton per hand/god.
    /// </summary>
    public struct MiracleCasterState : IComponentData
    {
        public Entity HandEntity;
        public byte SelectedSlot;        // 0-based index for miracle list
        public byte SustainedCastHeld;   // 1 = channeling
        public byte ThrowCastTriggered;  // 1 this frame
    }

    /// <summary>
    /// Mapping between slot indices and miracle prefab/config.
    /// </summary>
    [InternalBufferCapacity(6)]
    public struct MiracleSlotDefinition : IBufferElementData
    {
        public byte SlotIndex;
        public Entity MiraclePrefab;
        public MiracleType Type;
        public Entity ConfigEntity;
    }

    public struct RainMiracleCommand : IBufferElementData
    {
        public float3 Center;
        public int CloudCount;
        public float Radius;
        public float HeightOffset;
        public Entity RainCloudPrefab;
        public uint Seed;
    }

    public enum MiracleLifecycleState : byte
    {
        Idle = 0,
        Charging = 1,
        Ready = 2,
        Active = 3,
        CoolingDown = 4
    }

    public enum MiracleCastingMode : byte
    {
        Token = 0,
        Sustained = 1,
        Instant = 2
    }

    /// <summary>
    /// Core definition for a miracle instance.
    /// </summary>
    public struct MiracleDefinition : IComponentData
    {
        public MiracleType Type;
        public MiracleCastingMode CastingMode;
        public float BaseRadius;
        public float BaseIntensity;
        public float BaseCost;
        public float SustainedCostPerSecond;
    }

    public struct MiracleRuntimeState : IComponentData
    {
        public MiracleLifecycleState Lifecycle;
        public float ChargePercent;
        public float CurrentRadius;
        public float CurrentIntensity;
        public float CooldownSecondsRemaining;
        public uint LastCastTick;
        public byte AlignmentDelta;
    }

    public struct MiracleTarget : IComponentData
    {
        public float3 TargetPosition;
        public Entity TargetEntity;
    }

    public struct MiracleCaster : IComponentData
    {
        public Entity CasterEntity;
        public Entity HandEntity;
    }

    public struct MiracleToken : IComponentData
    {
        public MiracleType Type;
        public Entity ConfigEntity;
    }

    [InternalBufferCapacity(4)]
    public struct MiracleReleaseEvent : IBufferElementData
    {
        public MiracleType Type;
        public float3 Position;
        public float3 Direction;
        public float Impulse;
        public Entity ConfigEntity;
    }

    /// <summary>
    /// Registry summary for miracles.
    /// </summary>
    public struct MiracleRegistry : IComponentData
    {
        public int TotalMiracles;
        public int ActiveMiracles;
        public int SustainedMiracles;
        public int CoolingMiracles;
        public float TotalEnergyCost;
        public float TotalCooldownSeconds;
        public uint LastUpdateTick;
        public uint LastSpatialVersion;
        public int SpatialResolvedCount;
        public int SpatialFallbackCount;
        public int SpatialUnmappedCount;
    }

    [System.Flags]
    public enum MiracleRegistryFlags : byte
    {
        None = 0,
        Active = 1 << 0,
        Sustained = 1 << 1,
        CoolingDown = 1 << 2
    }

    public struct MiracleRegistryEntry :
        IBufferElementData,
        IComparable<MiracleRegistryEntry>,
        IRegistryEntry,
        IRegistryFlaggedEntry
    {
        public Entity MiracleEntity;
        public Entity CasterEntity;
        public MiracleType Type;
        public MiracleCastingMode CastingMode;
        public MiracleLifecycleState Lifecycle;
        public MiracleRegistryFlags Flags;
        public float3 TargetPosition;
        public int TargetCellId;
        public uint SpatialVersion;
        public float ChargePercent;
        public float CurrentRadius;
        public float CurrentIntensity;
        public float CooldownSecondsRemaining;
        public float EnergyCostThisCast;
        public uint LastCastTick;

        public int CompareTo(MiracleRegistryEntry other)
        {
            return MiracleEntity.Index.CompareTo(other.MiracleEntity.Index);
        }

        public Entity RegistryEntity => MiracleEntity;

        public byte RegistryFlags => (byte)Flags;
    }
}
