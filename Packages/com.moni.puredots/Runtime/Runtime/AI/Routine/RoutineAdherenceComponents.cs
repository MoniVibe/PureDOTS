using Unity.Entities;

namespace PureDOTS.Runtime.AI.Routine
{
    /// <summary>
    /// Adherence tuning for schedules - drives punctuality and variance.
    /// </summary>
    public struct ScheduleAdherence : IComponentData
    {
        public float Adherence;       // 0..1, higher = more punctual
        public float Variance;        // 0..1, higher = more unpredictable
        public float RecoveryRate;    // How fast returns to baseline
        public uint LastDeviationTick;
    }

    /// <summary>
    /// Current deviation from the scheduled routine.
    /// </summary>
    public struct ScheduleDeviation : IComponentData
    {
        public float LatenessMinutes;
        public float SkippedWeight;
        public uint DeviationTick;
    }

    /// <summary>
    /// Deviation event buffer for telemetry or relation/morale penalties.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct ScheduleDeviationEvent : IBufferElementData
    {
        public RoutineActivity Activity;
        public float LatenessMinutes;
        public uint Tick;
    }
}
