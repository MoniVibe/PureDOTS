using Unity.Entities;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Settings controlling performance telemetry warmup and measurement windows.
    /// </summary>
    public struct PerformanceTelemetrySettings : IComponentData
    {
        /// <summary>Number of ticks to skip before measurements begin.</summary>
        public uint WarmupTicks;
        /// <summary>Number of ticks to measure (0 = unlimited).</summary>
        public uint MeasureTicks;
    }

    public static class PerformanceTelemetryDefaults
    {
        public static PerformanceTelemetrySettings CreateDefault()
        {
            return new PerformanceTelemetrySettings
            {
                WarmupTicks = 0,
                MeasureTicks = 0
            };
        }
    }
}
