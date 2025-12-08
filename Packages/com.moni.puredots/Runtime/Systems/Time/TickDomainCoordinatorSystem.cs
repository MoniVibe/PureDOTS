using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Systems.Time
{
    /// <summary>
    /// Coordinator system managing heterogeneous tick domains.
    /// Synchronizes via integer tick ratios to preserve determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct TickDomainCoordinatorSystem : ISystem
    {
        private SystemHandle _cognitiveHandle;
        private SystemHandle _economyHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();

            // Ensure domain entities exist for cognitive and economy
            EnsureDomain(ref state, TickDomainType.Cognitive, defaultRatio: 60); // ~1 Hz if base is 60Hz
            EnsureDomain(ref state, TickDomainType.Economy, defaultRatio: 600);  // ~0.1 Hz if base is 60Hz

            _cognitiveHandle = state.WorldUnmanaged.GetExistingSystem<CognitiveSystemGroup>();
            _economyHandle = state.WorldUnmanaged.GetExistingSystem<EconomySystemGroup>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            var cognitiveShouldRun = false;
            var economyShouldRun = false;
            uint cognitiveTicksExecuted = 0;
            uint economyTicksExecuted = 0;

            foreach (var domain in SystemAPI.Query<RefRW<TickDomain>>())
            {
                ref var d = ref domain.ValueRW;
                var shouldRun = tickState.Tick >= d.NextTick;
                if (shouldRun)
                {
                    d.LastTick = tickState.Tick;
                    d.NextTick = tickState.Tick + math.max(1u, d.TickRatio);
                }

                switch (d.DomainType)
                {
                    case TickDomainType.Cognitive:
                        cognitiveShouldRun |= shouldRun;
                        if (shouldRun) cognitiveTicksExecuted++;
                        break;
                    case TickDomainType.Economy:
                        economyShouldRun |= shouldRun;
                        if (shouldRun) economyTicksExecuted++;
                        break;
                }
            }

            // Gate groups deterministically
            if (_cognitiveHandle != SystemHandle.Null)
            {
                state.WorldUnmanaged.SetSystemEnabled(_cognitiveHandle, cognitiveShouldRun);
            }

            if (_economyHandle != SystemHandle.Null)
            {
                state.WorldUnmanaged.SetSystemEnabled(_economyHandle, economyShouldRun);
            }

            // TODO (Agent D): emit telemetry (domain ticks executed vs skipped) via TelemetryStream
        }

        private static void EnsureDomain(ref SystemState state, TickDomainType type, uint defaultRatio)
        {
            bool found = false;
            foreach (var domain in SystemAPI.Query<RefRO<TickDomain>>())
            {
                if (domain.ValueRO.DomainType == type)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new TickDomain
                {
                    DomainType = type,
                    TickRatio = math.max(1u, defaultRatio),
                    LastTick = 0,
                    NextTick = 0
                });
            }
        }
    }
}
