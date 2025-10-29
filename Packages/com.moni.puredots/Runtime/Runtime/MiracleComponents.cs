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
