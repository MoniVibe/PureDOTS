using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Applies global time stop/slow requests by overriding time scale and pause state.
    /// </summary>
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(TimeScaleResolutionSystem))]
    [UpdateBefore(typeof(TimeTickSystem))]
    public partial struct TimeStopSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<SimulationOverrides>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeEntity = SystemAPI.GetSingletonEntity<TimeState>();
            if (!state.EntityManager.HasComponent<TimeStopState>(timeEntity))
            {
                state.EntityManager.AddComponentData(timeEntity, new TimeStopState
                {
                    ActiveTimeScale = 1f
                });
            }

            var stopState = state.EntityManager.GetComponentData<TimeStopState>(timeEntity);
            var overridesHandle = SystemAPI.GetSingletonRW<SimulationOverrides>();
            var tickStateHandle = SystemAPI.GetSingletonRW<TickTimeState>();

            bool hasRequest = false;
            var request = default(TimeStopRequest);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (req, entity) in SystemAPI.Query<RefRO<TimeStopRequest>>().WithEntityAccess())
            {
                if (!hasRequest || req.ValueRO.DurationSeconds > request.DurationSeconds)
                {
                    request = req.ValueRO;
                    hasRequest = true;
                }

                ecb.RemoveComponent<TimeStopRequest>(entity);
            }

            if (hasRequest)
            {
                var duration = math.max(0.01f, request.DurationSeconds);
                var requestedScale = ResolveRequestedScale(request);

                stopState.Source = request.Source;
                stopState.RemainingSeconds = duration;
                stopState.ActiveTimeScale = requestedScale;
                stopState.IsActive = 1;

                if (stopState.OverrideApplied == 0)
                {
                    stopState.PreviousOverride = overridesHandle.ValueRO.OverrideTimeScale ? (byte)1 : (byte)0;
                    stopState.PreviousTimeScaleOverride = overridesHandle.ValueRO.TimeScaleOverride;
                    stopState.OverrideApplied = 1;
                }

                overridesHandle.ValueRW.OverrideTimeScale = true;
                overridesHandle.ValueRW.TimeScaleOverride = requestedScale;

                if (requestedScale <= 0.0001f && stopState.PauseApplied == 0)
                {
                    stopState.PreviousPaused = tickStateHandle.ValueRO.IsPaused ? (byte)1 : (byte)0;
                    stopState.PauseApplied = 1;
                    tickStateHandle.ValueRW.IsPaused = true;
                }
            }

            if (stopState.IsActive != 0)
            {
                stopState.RemainingSeconds -= math.max(0f, (float)SystemAPI.Time.DeltaTime);

                if (stopState.RemainingSeconds <= 0f)
                {
                    if (stopState.OverrideApplied != 0)
                    {
                        overridesHandle.ValueRW.OverrideTimeScale = stopState.PreviousOverride != 0;
                        overridesHandle.ValueRW.TimeScaleOverride = stopState.PreviousTimeScaleOverride;
                        stopState.OverrideApplied = 0;
                    }

                    if (stopState.PauseApplied != 0 && stopState.PreviousPaused == 0)
                    {
                        tickStateHandle.ValueRW.IsPaused = false;
                    }

                    stopState.IsActive = 0;
                    stopState.PauseApplied = 0;
                    stopState.ActiveTimeScale = 1f;
                    stopState.RemainingSeconds = 0f;
                }
                else
                {
                    if (stopState.OverrideApplied != 0)
                    {
                        overridesHandle.ValueRW.OverrideTimeScale = true;
                        overridesHandle.ValueRW.TimeScaleOverride = stopState.ActiveTimeScale;
                    }

                    if (stopState.ActiveTimeScale <= 0.0001f && stopState.PauseApplied != 0)
                    {
                        tickStateHandle.ValueRW.IsPaused = true;
                    }
                }
            }

            state.EntityManager.SetComponentData(timeEntity, stopState);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static float ResolveRequestedScale(in TimeStopRequest request)
        {
            if (request.Mode == TimeStopMode.Stop)
            {
                return 0f;
            }

            var scale = math.max(0.01f, request.TimeScale);
            return math.min(scale, 1f);
        }
    }
}
