using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
    /// Interpolates camera target positions/rotations between simulation ticks.
    /// Samples LocalTransform snapshots and calculates interpolation alpha based on frame time vs tick time.
    /// Supports velocity-based extrapolation for high-velocity motion.
    /// 
    /// See: Docs/Guides/SimulationPresentationTimeSeparationGuide.md for integration examples.
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    [UpdateAfter(typeof(Unity.Entities.SimulationSystemGroup))]
    public partial struct CameraInterpolationSystem : ISystem
    {
        private float _frameTimeAccumulator;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            _frameTimeAccumulator = 0f;
        }

        public void OnUpdate(ref SystemState state)
        {
            // Only interpolate during Record mode (not during playback)
            if (SystemAPI.TryGetSingleton<RewindState>(out var rewind) && rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            float fixedDeltaTime = tickState.FixedDeltaTime;
            uint currentTick = tickState.Tick;
            
            // Accumulate frame time (non-Burst, uses SystemAPI.Time)
            float frameTime = (float)SystemAPI.Time.DeltaTime;
            _frameTimeAccumulator += frameTime;
            
            // Calculate interpolation alpha: how far we are between prev and next tick
            // Alpha = (currentFrameTime - prevTickTime) / (nextTickTime - prevTickTime)
            float alpha = CalculateInterpolationAlpha(tickState, _frameTimeAccumulator, fixedDeltaTime);
            
            // Reset accumulator when we've exceeded a tick
            if (_frameTimeAccumulator >= fixedDeltaTime)
            {
                _frameTimeAccumulator -= fixedDeltaTime;
            }

            // Update camera target history for entities with LocalTransform
            foreach (var (transform, history, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<CameraTargetHistory>>()
                         .WithEntityAccess())
            {
                var currentPos = transform.ValueRO.Position;
                var currentRot = transform.ValueRO.Rotation;

                ref var hist = ref history.ValueRW;

                // Initialize if first update
                if (hist.NextTick == 0)
                {
                    hist.PrevPosition = currentPos;
                    hist.NextPosition = currentPos;
                    hist.PrevRotation = currentRot;
                    hist.NextRotation = currentRot;
                    hist.PrevTick = currentTick;
                    hist.NextTick = currentTick;
                    hist.Alpha = 0f;
                    hist.Velocity = float3.zero;
                    continue;
                }

                // If tick advanced, shift buffers
                if (currentTick > hist.NextTick)
                {
                    hist.PrevPosition = hist.NextPosition;
                    hist.PrevRotation = hist.NextRotation;
                    hist.PrevTick = hist.NextTick;
                    
                    hist.NextPosition = currentPos;
                    hist.NextRotation = currentRot;
                    hist.NextTick = currentTick;

                    // Calculate velocity for extrapolation
                    if (fixedDeltaTime > 1e-6f)
                    {
                        hist.Velocity = (hist.NextPosition - hist.PrevPosition) / fixedDeltaTime;
                    }
                }
                else if (currentTick == hist.NextTick)
                {
                    // Same tick, update next position (entity moved within tick)
                    hist.NextPosition = currentPos;
                    hist.NextRotation = currentRot;
                }

                // Update interpolation alpha
                hist.Alpha = alpha;
            }
        }

        private float CalculateInterpolationAlpha(TickTimeState tickState, float frameTime, float fixedDeltaTime)
        {
            // Simple alpha calculation: assume we're interpolating between current tick and next tick
            // More sophisticated version would track actual time between ticks
            // For now, use a simple frame-time based interpolation
            // Alpha represents how far into the current tick we are (0 = start of tick, 1 = end of tick)
            
            // Accumulate frame time to estimate position within tick
            // This is simplified - a full implementation would track exact tick boundaries
            float estimatedTickProgress = math.clamp(frameTime / fixedDeltaTime, 0f, 1f);
            return estimatedTickProgress;
        }
    }
}

