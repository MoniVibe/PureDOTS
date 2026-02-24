using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Systems.Telemetry
{
    internal struct SimPhaseProfilerQueries
    {
        public EntityQuery ScenarioRunnerTickQuery;
        public EntityQuery TickTimeStateQuery;
        public EntityQuery TimeStateQuery;
        public EntityQuery ProfilerStateQuery;

        public void Initialize(ref SystemState state)
        {
            ScenarioRunnerTickQuery = state.GetEntityQuery(ComponentType.ReadOnly<ScenarioRunnerTick>());
            TickTimeStateQuery = state.GetEntityQuery(ComponentType.ReadOnly<TickTimeState>());
            TimeStateQuery = state.GetEntityQuery(ComponentType.ReadOnly<TimeState>());
            ProfilerStateQuery = state.GetEntityQuery(ComponentType.ReadOnly<SimPhaseProfilerState>());
        }
    }

    internal static class SimPhaseProfiler
    {
        public static void BeginPhase(ref SystemState state, SimPhase phase, ref SimPhaseProfilerQueries queries)
        {
            var entity = EnsureProfilerEntity(ref state, ref queries);
            var profilerState = state.EntityManager.GetComponentData<SimPhaseProfilerState>(entity);
            var tick = ResolveTick(ref state, ref queries);
            if (profilerState.Tick != tick)
            {
                profilerState.ResetForTick(tick);
                state.EntityManager.SetComponentData(entity, profilerState);
            }

            var startTimes = state.EntityManager.GetComponentData<SimPhaseProfilerPhaseStartTimes>(entity);
            startTimes.SetStart(phase, state.WorldUnmanaged.Time.ElapsedTime);
            state.EntityManager.SetComponentData(entity, startTimes);
        }

        public static void EndPhase(ref SystemState state, SimPhase phase, ref SimPhaseProfilerQueries queries)
        {
            var entity = EnsureProfilerEntity(ref state, ref queries);
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

        private static uint ResolveTick(ref SystemState state, ref SimPhaseProfilerQueries queries)
        {
            if (TryGetSingleton(queries.ScenarioRunnerTickQuery, out ScenarioRunnerTick scenarioTick) && scenarioTick.Tick > 0)
            {
                return scenarioTick.Tick;
            }

            if (TryGetSingleton(queries.TickTimeStateQuery, out TickTimeState tickState))
            {
                var tick = tickState.Tick;
                if (TryGetSingleton(queries.TimeStateQuery, out TimeState timeState) && timeState.Tick > tick)
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

            if (TryGetSingleton(queries.TimeStateQuery, out TimeState legacyTime))
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

        private static Entity EnsureProfilerEntity(ref SystemState state, ref SimPhaseProfilerQueries queries)
        {
            if (TryGetSingletonEntity<SimPhaseProfilerState>(queries.ProfilerStateQuery, out var entity))
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

        private static bool TryGetSingleton<T>(EntityQuery query, out T value)
            where T : unmanaged, IComponentData
        {
            return query.TryGetSingleton(out value);
        }

        private static bool TryGetSingletonEntity<T>(EntityQuery query, out Entity entity)
            where T : unmanaged, IComponentData
        {
            return query.TryGetSingletonEntity<T>(out entity);
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(TimeSystemGroup))]
    public partial struct SimPhaseScenarioApplyStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.ScenarioApply, ref _queries);
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(TimeSystemGroup))]
    public partial struct SimPhaseScenarioApplyEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.ScenarioApply, ref _queries);
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SpatialSystemGroup))]
    public partial struct SimPhaseMovementStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Movement, ref _queries);
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    public partial struct SimPhaseMovementEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Movement, ref _queries);
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    public partial struct SimPhasePhysicsStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Physics, ref _queries);
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    public partial struct SimPhasePhysicsEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Physics, ref _queries);
        }
    }

    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateBefore(typeof(PerceptionSystemGroup))]
    public partial struct SimPhaseSensorsStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Sensors, ref _queries);
        }
    }

    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(PerceptionSystemGroup))]
    public partial struct SimPhaseSensorsEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Sensors, ref _queries);
        }
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(InterruptSystemGroup))]
    public partial struct SimPhaseCommsStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Comms, ref _queries);
        }
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(InterruptSystemGroup))]
    public partial struct SimPhaseCommsEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Comms, ref _queries);
        }
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(VillagerSystemGroup))]
    public partial struct SimPhaseKnowledgeStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Knowledge, ref _queries);
        }
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    public partial struct SimPhaseKnowledgeEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Knowledge, ref _queries);
        }
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(ResourceSystemGroup))]
    public partial struct SimPhaseEconomyStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.Economy, ref _queries);
        }
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial struct SimPhaseEconomyEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.Economy, ref _queries);
        }
    }

    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    [UpdateBefore(typeof(PureDotsPresentationSystemGroup))]
    public partial struct SimPhasePresentationStartSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.BeginPhase(ref state, SimPhase.PresentationBridge, ref _queries);
        }
    }

    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    [UpdateAfter(typeof(PureDotsPresentationSystemGroup))]
    public partial struct SimPhasePresentationEndSystem : ISystem
    {
        private SimPhaseProfilerQueries _queries;

        public void OnCreate(ref SystemState state)
        {
            _queries.Initialize(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimPhaseProfiler.EndPhase(ref state, SimPhase.PresentationBridge, ref _queries);
        }
    }
}
