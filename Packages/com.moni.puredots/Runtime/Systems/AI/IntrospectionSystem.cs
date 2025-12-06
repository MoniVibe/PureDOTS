using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Introspection system collecting decision summaries from Mind ECS.
    /// Zero-cost in release builds (conditional compilation).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct IntrospectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Collect decision reasoning
            var reasoningQuery = state.GetEntityQuery(typeof(DecisionReasoning));
            
            if (reasoningQuery.IsEmpty)
            {
                return;
            }

            var job = new CollectReasoningJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(reasoningQuery, state.Dependency);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [BurstCompile]
        private partial struct CollectReasoningJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(in DecisionReasoning reasoning)
            {
                // Collect reasoning for telemetry
                // In full implementation, would:
                // 1. Collect decision reasoning from entities
                // 2. Store in TelemetryStream for debugging/UI
                // 3. Generate human-readable diagnostics
                // 4. Export for AI tuning
            }
        }
#endif
    }
}

