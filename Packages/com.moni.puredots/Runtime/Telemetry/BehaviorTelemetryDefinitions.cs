using System;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Telemetry
{
    public enum BehaviorId : ushort
    {
        None = 0,
        HazardDodge = 1,
        GatherDeliver = 2,
        VillagerMind = 3
    }

    public enum BehaviorMetricId : ushort
    {
        HazardRaycastHits = 1,
        HazardAvoidanceTransitions = 2,
        HazardDodgeDistanceMm = 3,
        GatherMinedMilli = 100,
        GatherDepositedMilli = 101,
        GatherCarrierCargoMilli = 102,
        VillagerCount = 200,
        VillagerGoalIdleCount = 201,
        VillagerGoalWorkCount = 202,
        VillagerGoalEatCount = 203,
        VillagerGoalSleepCount = 204,
        VillagerGoalShelterCount = 205,
        VillagerGoalPrayCount = 206,
        VillagerGoalSocializeCount = 207,
        VillagerGoalFleeCount = 208,
        VillagerAverageFocusMilli = 210,
        VillagerPeakNeedMilli = 211
    }

    public enum BehaviorInvariantId : ushort
    {
        HazardNoOscillation = 1,
        GatherConservation = 100
    }

    public enum BehaviorTelemetryRecordKind : byte
    {
        Metric = 0,
        Invariant = 1
    }

    /// <summary>
    /// Global configuration for behavior telemetry aggregation cadence.
    /// </summary>
    public struct BehaviorTelemetryConfig : IComponentData
    {
        public int AggregateCadenceTicks;
    }

    /// <summary>
    /// Marker singleton for the telemetry state entity.
    /// </summary>
    public struct BehaviorTelemetryState : IComponentData { }

    /// <summary>
    /// Per-agent hazard avoidance counters (interval values reset each cadence).
    /// </summary>
    public struct HazardDodgeTelemetry : IComponentData
    {
        public uint RaycastHitsInterval;
        public uint AvoidanceTransitionsInterval;
        public int DodgeDistanceMmInterval;
        public uint HighUrgencyTicksInterval;
        public byte WasAvoidingLastTick;
    }

    /// <summary>
    /// Per-agent gather/deliver counters (mixed interval + snapshot fields).
    /// </summary>
    public struct GatherDeliverTelemetry : IComponentData
    {
        public int MinedAmountMilliInterval;
        public int DepositedAmountMilliInterval;
        public int CarrierCargoMilliSnapshot;
        public uint StuckTicksInterval;
    }

    /// <summary>
    /// Aggregated output record consumed by headless loggers / scenario reports.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct BehaviorTelemetryRecord : IBufferElementData
    {
        public uint Tick;
        public BehaviorId Behavior;
        public BehaviorTelemetryRecordKind Kind;
        public ushort MetricOrInvariantId;
        public long ValueA;
        public long ValueB;
        public byte Passed;
    }

    public static class BehaviorTelemetryMath
    {
        public static int ToMilli(float value) => (int)Math.Round(value * 1000f);
    }
}
