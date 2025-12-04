using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Manages tick advancement and fixed timestep simulation.
    /// Replaces TimeMonolith tick logic.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(HistorySettingsConfigSystem))]
    public partial struct TimeTickSystem : ISystem
    {
        private float _accumulator;
        private float _lastRealTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<SimulationScalars>();
            state.RequireForUpdate<SimulationOverrides>();
            _accumulator = 0f;
            _lastRealTime = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickStateHandle = SystemAPI.GetSingletonRW<TickTimeState>();
            var timeStateHandle = SystemAPI.GetSingletonRW<TimeState>();
            ref var tickState = ref tickStateHandle.ValueRW;
            ref var timeState = ref timeStateHandle.ValueRW;
            var rewind = SystemAPI.GetSingleton<RewindState>();
            var scalars = SystemAPI.GetSingleton<SimulationScalars>();
            var overrides = SystemAPI.GetSingleton<SimulationOverrides>();

            // Get effective time scale
            float effectiveTimeScale = overrides.OverrideTimeScale
                ? overrides.TimeScaleOverride
                : scalars.TimeScale;

            var elapsed = (float)SystemAPI.Time.ElapsedTime;
            if (rewind.Mode != RewindMode.Record)
            {
                tickState.TargetTick = Unity.Mathematics.math.max(tickState.TargetTick, tickState.Tick);
                _accumulator = 0f;
                _lastRealTime = elapsed;
                SyncLegacyTime(ref tickState, ref timeState);
                return;
            }

            var playing = tickState.IsPlaying && !tickState.IsPaused;

            // Skip if paused
            if (!playing)
            {
                if (tickState.Tick < tickState.TargetTick)
                {
                    tickState.Tick++;
                }

                _lastRealTime = elapsed;
                tickState.TargetTick = Unity.Mathematics.math.max(tickState.TargetTick, tickState.Tick);
                SyncLegacyTime(ref tickState, ref timeState);
                return;
            }

            float deltaRealTime = elapsed - _lastRealTime;
            _lastRealTime = elapsed;

            // Apply speed multiplier and time scale valve
            float baseSpeedMultiplier = Unity.Mathematics.math.max(0.01f, tickState.CurrentSpeedMultiplier);
            float scaledDelta = deltaRealTime * baseSpeedMultiplier * effectiveTimeScale;

            // Accumulate time for fixed timestep
            _accumulator += scaledDelta;

            var fixedDt = Unity.Mathematics.math.max(tickState.FixedDeltaTime, 1e-4f);
            const int maxStepsPerFrame = 4; // Prevent spiral of death
            var steps = 0;

            // Advance ticks based on accumulated time
            while (_accumulator >= fixedDt && steps < maxStepsPerFrame)
            {
                _accumulator -= fixedDt;
                tickState.Tick++;
                steps++;
            }

            if (tickState.TargetTick < tickState.Tick)
            {
                tickState.TargetTick = tickState.Tick;
            }

            // Clamp accumulator if we're falling too far behind
            if (_accumulator > fixedDt * maxStepsPerFrame)
            {
                _accumulator = fixedDt;
            }

            SyncLegacyTime(ref tickState, ref timeState);
        }

        private static void SyncLegacyTime(ref TickTimeState tickState, ref TimeState legacy)
        {
            legacy.Tick = tickState.Tick;
            legacy.FixedDeltaTime = tickState.FixedDeltaTime;
            legacy.DeltaTime = tickState.FixedDeltaTime;
            legacy.CurrentSpeedMultiplier = tickState.CurrentSpeedMultiplier;
            legacy.IsPaused = tickState.IsPaused;
        }
    }
}
