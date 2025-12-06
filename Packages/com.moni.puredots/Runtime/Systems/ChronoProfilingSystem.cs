using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Time;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Updates telemetry with time budget metrics each frame.
    /// Tracks real vs sim time, compression factor, and drift across worlds.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ChronoProfilingSystem : ISystem
    {
        private double _lastRealTime;
        private double _lastSimTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastRealTime = 0.0;
            _lastSimTime = 0.0;
            state.RequireForUpdate<TelemetryStream>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
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

            // Add metrics to telemetry buffer
            if (SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                var buffer = state.EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
                buffer.AddMetric("RealTimeMs", telemetry.RealTimeMs, TelemetryMetricUnit.DurationMilliseconds);
                buffer.AddMetric("SimTimeMs", telemetry.SimTimeMs, TelemetryMetricUnit.DurationMilliseconds);
                buffer.AddMetric("CompressionFactor", telemetry.CompressionFactor, TelemetryMetricUnit.Ratio);
                buffer.AddMetric("DriftMs", telemetry.DriftMs, TelemetryMetricUnit.DurationMilliseconds);
            }
        }
    }
}

