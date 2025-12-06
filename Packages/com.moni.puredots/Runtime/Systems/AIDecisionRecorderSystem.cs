using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Records decision events from AI systems for introspection and replay.
    /// Hooks into AIUtilityScoringSystem and AITaskResolutionSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Systems.AI.AIUtilityScoringSystem))]
    public partial struct AIDecisionRecorderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DecisionEventRegistry>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Get decision event registry
            if (!SystemAPI.TryGetSingletonEntity<DecisionEventRegistry>(out var registryEntity))
            {
                return;
            }

            var registryHandle = SystemAPI.GetComponentRW<DecisionEventRegistry>(registryEntity);
            var buffer = state.EntityManager.GetBuffer<DecisionEventBuffer>(registryEntity);

            ref var registry = ref registryHandle.ValueRW;

            // Record utility scores from AI systems
            // This would hook into AIUtilityScoringSystem to capture utility evaluations
            // For now, this is a placeholder showing the pattern

            // Clean up old events (keep last N ticks)
            uint minTick = timeState.Tick > 1000 ? timeState.Tick - 1000 : 0;
            DecisionEventBufferHelper.ClearEventsBeforeTick(ref buffer, ref registry, minTick);
        }
    }
}

