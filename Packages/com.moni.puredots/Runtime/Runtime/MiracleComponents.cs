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
        Shield = 3,
        Healing = Heal,
        Blessing = 4,
        Fertility = 5,
        Sunlight = 6,
        Fire = 7,
        Lightning = 8
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
    /// Reason why a miracle was cancelled.
    /// Used for UX telemetry and design tuning.
    /// </summary>
    public enum MiracleCancelReason : byte
    {
        None = 0,
        UserCancelled = 1,
        TargetInvalid = 2,
        Interrupted = 3,
        InsufficientResources = 4,
        Timeout = 5,
        CasterDied = 6
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

        // UX Telemetry fields
        /// <summary>
        /// Tick when the user first provided input for this miracle.
        /// </summary>
        public uint LastInputTick;

        /// <summary>
        /// Tick when casting actually began (transitioned to Active/Charging).
        /// </summary>
        public uint CastStartTick;

        /// <summary>
        /// Tick when the miracle was cancelled (0 if not cancelled).
        /// </summary>
        public uint CancelTick;

        /// <summary>
        /// Reason for cancellation if the miracle was cancelled.
        /// </summary>
        public MiracleCancelReason CancelReason;

        public int CompareTo(MiracleRegistryEntry other)
        {
            return MiracleEntity.Index.CompareTo(other.MiracleEntity.Index);
        }

        public Entity RegistryEntity => MiracleEntity;

        public byte RegistryFlags => (byte)Flags;
    }

    /// <summary>
    /// UX telemetry entry for miracle casting latency and cancellation tracking.
    /// Collected by telemetry systems for HUD display and design tuning.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct MiracleUxTelemetry : IBufferElementData
    {
        /// <summary>
        /// ID of the miracle type for aggregation.
        /// </summary>
        public FixedString64Bytes MiracleId;

        /// <summary>
        /// Tick when user input was first received.
        /// </summary>
        public uint InputTick;

        /// <summary>
        /// Tick when the miracle became active.
        /// </summary>
        public uint ActivationTick;

        /// <summary>
        /// Tick when cancelled (0 if completed normally).
        /// </summary>
        public uint CancelTick;

        /// <summary>
        /// Reason for cancellation.
        /// </summary>
        public MiracleCancelReason CancelReason;

        /// <summary>
        /// Latency from input to activation in seconds.
        /// </summary>
        public float LatencySeconds;

        /// <summary>
        /// Entity that cast this miracle.
        /// </summary>
        public Entity CasterEntity;

        /// <summary>
        /// Whether this entry represents a completed (vs. in-progress) cast.
        /// </summary>
        public byte IsCompleted;
    }

    /// <summary>
    /// Singleton component to track miracle UX telemetry buffer capacity.
    /// </summary>
    public struct MiracleUxTelemetryState : IComponentData
    {
        /// <summary>
        /// Maximum buffer capacity before recycling oldest entries.
        /// </summary>
        public const int MaxCapacity = 64;

        /// <summary>
        /// Number of entries currently in the buffer.
        /// </summary>
        public int EntryCount;

        /// <summary>
        /// Version incremented on each telemetry update.
        /// </summary>
        public uint Version;

        /// <summary>
        /// Running total of cast latencies for average calculation (milliseconds).
        /// </summary>
        public float TotalLatencyMs;

        /// <summary>
        /// Number of completed casts for average calculation.
        /// </summary>
        public int CompletedCastCount;

        /// <summary>
        /// Total number of cancellations.
        /// </summary>
        public int TotalCancellations;

        /// <summary>
        /// Cancellation counts by reason.
        /// </summary>
        public int CancellationsUserCancelled;
        public int CancellationsTargetInvalid;
        public int CancellationsInterrupted;
        public int CancellationsInsufficientResources;

        /// <summary>
        /// Average cast latency in milliseconds.
        /// </summary>
        public readonly float AverageLatencyMs => CompletedCastCount > 0 ? TotalLatencyMs / CompletedCastCount : 0f;

        public void RecordCompletion(float latencyMs)
        {
            TotalLatencyMs += latencyMs;
            CompletedCastCount++;
            Version++;
        }

        public void RecordCancellation(MiracleCancelReason reason)
        {
            TotalCancellations++;
            switch (reason)
            {
                case MiracleCancelReason.UserCancelled:
                    CancellationsUserCancelled++;
                    break;
                case MiracleCancelReason.TargetInvalid:
                    CancellationsTargetInvalid++;
                    break;
                case MiracleCancelReason.Interrupted:
                    CancellationsInterrupted++;
                    break;
                case MiracleCancelReason.InsufficientResources:
                    CancellationsInsufficientResources++;
                    break;
            }
            Version++;
        }
    }
}
