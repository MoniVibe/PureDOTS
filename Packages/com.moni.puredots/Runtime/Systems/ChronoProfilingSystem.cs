using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Time;

namespace PureDOTS.Systems
{
    internal static class ChronoProfilingKeys
    {
        public static readonly FixedString64Bytes RealMs = "time.real_ms";
        public static readonly FixedString64Bytes SimMs = "time.sim_ms";
        public static readonly FixedString64Bytes Compression = "time.compression";
        public static readonly FixedString64Bytes DriftMs = "time.drift_ms";
    }

    /// <summary>
    /// Updates telemetry with time budget metrics each frame.
    /// Tracks real vs sim time, compression factor, and drift across worlds.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ChronoProfilingSystem : ISystem
    {
        private double _lastRealTime;
        private double _lastSimTime;

        public void OnCreate(ref SystemState state)
        {
            _lastRealTime = 0.0;
            _lastSimTime = 0.0;
            state.RequireForUpdate<TelemetryStream>();
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
    {
            var telemetryHandle = SystemAPI.GetSingletonRW<TelemetryStream>();
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();

            ref var telemetry = ref telemetryHandle.ValueRW;

            // Calculate real time delta
            double currentRealTime = SystemAPI.Time.ElapsedTime * 1000.0; // Convert to ms
            double realDelta = currentRealTime - _lastRealTime;
            if (_lastRealTime == 0.0)
            {
                realDelta = 0.0;
            }
            _lastRealTime = currentRealTime;

            // Calculate sim time delta
            double currentSimTime = tickTimeState.Tick * timeState.FixedDeltaTime * 1000.0; // Convert to ms
            double simDelta = currentSimTime - _lastSimTime;
            if (_lastSimTime == 0.0)
            {
                simDelta = 0.0;
            }
            _lastSimTime = currentSimTime;

            // Update telemetry
            telemetry.RealTimeMs = (float)currentRealTime;
            telemetry.SimTimeMs = (float)currentSimTime;

            // Calculate compression factor
            if (realDelta > 0.0)
            {
                telemetry.CompressionFactor = (float)(simDelta / realDelta);
            }
            else
            {
                telemetry.CompressionFactor = 1.0f;
            }

            // Calculate drift (if multi-world, compare to primary world)
            // For now, drift is 0 for single-world
            telemetry.DriftMs = 0.0f;

            // Enqueue metrics via telemetry hub (standardized keys)
            TelemetryHub.Enqueue(new TelemetryMetric { Key = ChronoProfilingKeys.RealMs, Value = telemetry.RealTimeMs, Unit = TelemetryMetricUnit.DurationMilliseconds });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = ChronoProfilingKeys.SimMs, Value = telemetry.SimTimeMs, Unit = TelemetryMetricUnit.DurationMilliseconds });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = ChronoProfilingKeys.Compression, Value = telemetry.CompressionFactor, Unit = TelemetryMetricUnit.Ratio });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = ChronoProfilingKeys.DriftMs, Value = telemetry.DriftMs, Unit = TelemetryMetricUnit.DurationMilliseconds });
        }
    }
}
