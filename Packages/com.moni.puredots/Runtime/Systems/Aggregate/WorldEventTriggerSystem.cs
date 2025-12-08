using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Telemetry;

namespace PureDOTS.Systems.Aggregate
{
    internal static class WorldEventTriggerKeys
    {
        public static readonly FixedString64Bytes Chaos = "world.chaos";
        public static readonly FixedString64Bytes Harmony = "world.harmony";
    }

    /// <summary>
    /// System that detects major world events (wars, miracles, biome shifts) and triggers profile spikes.
    /// Broadcasts metrics to Godgame layer (divine feedback) or Space4X (biosphere health).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnergyBalanceSystem))]
    public partial struct WorldEventTriggerSystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateInterval = 10;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTick = 0;
            state.RequireForUpdate<WorldAggregateProfile>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.Tick - _lastUpdateTick < UpdateInterval)
            {
                return;
            }

            _lastUpdateTick = timeState.Tick;

            var profileEntity = SystemAPI.GetSingletonEntity<WorldAggregateProfile>();
            var profile = SystemAPI.GetComponentRW<WorldAggregateProfile>(profileEntity);

            // Detect major events and update chaos metric
            float chaosIncrease = 0f;

            // Check for conflicts/wars (simplified - would check combat systems)
            // Check for disasters (simplified - would check environment systems)
            // Check for miracles (simplified - would check miracle systems)

            profile.ValueRW.Chaos = math.clamp(profile.ValueRO.Chaos + chaosIncrease, 0f, 1f);

            // Broadcast to telemetry stream if available
            if (SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                TelemetryHub.Enqueue(new TelemetryMetric { Key = WorldEventTriggerKeys.Chaos, Value = profile.ValueRO.Chaos, Unit = TelemetryMetricUnit.Ratio });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = WorldEventTriggerKeys.Harmony, Value = profile.ValueRO.Harmony, Unit = TelemetryMetricUnit.Ratio });
            }
        }
    }
}
