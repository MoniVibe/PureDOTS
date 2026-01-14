using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Systems.Telemetry
{
    internal static class SimPhaseProfiler
    {
        public static void BeginPhase(ref SystemState state, SimPhase phase)
        {
            var entity = EnsureProfilerEntity(ref state);
            var profilerState = state.EntityManager.GetComponentData<SimPhaseProfilerState>(entity);
            var tick = ResolveTick(ref state);
            if (profilerState.Tick != tick)
            {
                profilerState.ResetForTick(tick);
                state.EntityManager.SetComponentData(entity, profilerState);
            }

            var startTimes = state.EntityManager.GetComponentData<SimPhaseProfilerPhaseStartTimes>(entity);
            startTimes.SetStart(phase, state.WorldUnmanaged.Time.ElapsedTime);
            state.EntityManager.SetComponentData(entity, startTimes);
        }

        public static void EndPhase(ref SystemState state, SimPhase phase)
        {
            var entity = EnsureProfilerEntity(ref state);
            var startTimes = state.EntityManager.GetComponentData<SimPhaseProfilerPhaseStartTimes>(entity);
            var start = startTimes.GetStart(phase);
            if (start == double.MinValue)
            {
                return;
            }

            var now = state.WorldUnmanaged.Time.ElapsedTime;
            var durationMs = (float)math.max(0d, (now - start) * 1000d);
            startTimes.ClearStart(phase);
            var profilerState = state.EntityManager.GetComponentData<SimPhaseProfilerState>(entity);
            profilerState.TickTotalMs += durationMs;
            profilerState.SetPhaseDuration(phase, durationMs);
            state.EntityManager.SetComponentData(entity, startTimes);
            state.EntityManager.SetComponentData(entity, profilerState);
        }

        private static uint ResolveTick(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<ScenarioRunnerTick>(out var scenarioTick) && scenarioTick.Tick > 0)
            {
                return scenarioTick.Tick;
            }

            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickState))
            {
                var tick = tickState.Tick;
                if (SystemAPI.TryGetSingleton<TimeState>(out var timeState) && timeState.Tick > tick)
                {
                    tick = timeState.Tick;
                }

                if (tick == 0 && Application.isBatchMode)
                {
                    var elapsedTick = ResolveBatchElapsedTick(ref state);
                    if (elapsedTick > tick)
                    {
                        tick = elapsedTick;
                    }
                }

                return tick;
            }

            if (SystemAPI.TryGetSingleton<TimeState>(out var legacyTime))
            {
                var tick = legacyTime.Tick;
                if (tick == 0 && Application.isBatchMode)
                {
                    var elapsedTick = ResolveBatchElapsedTick(ref state);
                    if (elapsedTick > tick)
                    {
                        tick = elapsedTick;
                    }
                }

                return tick;
            }

            return Application.isBatchMode ? ResolveBatchElapsedTick(ref state) : 0u;
        }

        private static uint ResolveBatchElapsedTick(ref SystemState state)
        {
            var dt = (float)state.WorldUnmanaged.Time.DeltaTime;
            var elapsed = (float)state.WorldUnmanaged.Time.ElapsedTime;
            if (dt > 0f && elapsed > 0f)
            {
                return (uint)(elapsed / dt);
            }

            return 0u;
        }

        private static Entity EnsureProfilerEntity(ref SystemState state)
        {
            if (state.EntityManager.TryGetSingletonEntity<SimPhaseProfilerState>(out var entity))
            {
                return entity;
            }

            entity = state.EntityManager.CreateEntity(typeof(SimPhaseProfilerState), typeof(SimPhaseProfilerPhaseStartTimes));
            var telemetryState = default(SimPhaseProfilerState);
            var phaseStarts = SimPhaseProfilerPhaseStartTimesExtensions.CreateDefault();
            state.EntityManager.SetComponentData(entity, telemetryState);
            state.EntityManager.SetComponentData(entity, phaseStarts);
            return entity;
        }
    }

    [UpdateBefore(typeof(TimeSystemGroup))]
    public partial struct SimPhaseScenarioApplyStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.ScenarioApply);
        }
    }

    [UpdateAfter(typeof(TimeSystemGroup))]
    public partial struct SimPhaseScenarioApplyEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.ScenarioApply);
        }
    }

    [UpdateBefore(typeof(SpatialSystemGroup))]
    public partial struct SimPhaseMovementStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Movement);
        }
    }

    [UpdateAfter(typeof(SpatialSystemGroup))]
    public partial struct SimPhaseMovementEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Movement);
        }
    }

    [UpdateBefore(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    public partial struct SimPhasePhysicsStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Physics);
        }
    }

    [UpdateAfter(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    public partial struct SimPhasePhysicsEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Physics);
        }
    }

    [UpdateBefore(typeof(PerceptionSystemGroup))]
    public partial struct SimPhaseSensorsStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Sensors);
        }
    }

    [UpdateAfter(typeof(PerceptionSystemGroup))]
    public partial struct SimPhaseSensorsEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Sensors);
        }
    }

    [UpdateBefore(typeof(InterruptSystemGroup))]
    public partial struct SimPhaseCommsStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Comms);
        }
    }

    [UpdateAfter(typeof(InterruptSystemGroup))]
    public partial struct SimPhaseCommsEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Comms);
        }
    }

    [UpdateBefore(typeof(VillagerSystemGroup))]
    public partial struct SimPhaseKnowledgeStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Knowledge);
        }
    }

    [UpdateAfter(typeof(VillagerSystemGroup))]
    public partial struct SimPhaseKnowledgeEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Knowledge);
        }
    }

    [UpdateBefore(typeof(ResourceSystemGroup))]
    public partial struct SimPhaseEconomyStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Economy);
        }
    }

    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial struct SimPhaseEconomyEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Economy);
        }
    }

    [UpdateBefore(typeof(PureDotsPresentationSystemGroup))]
    public partial struct SimPhasePresentationStartSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.PresentationBridge);
        }
    }

    [UpdateAfter(typeof(PureDotsPresentationSystemGroup))]
    public partial struct SimPhasePresentationEndSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.PresentationBridge);
        }
    }
}
