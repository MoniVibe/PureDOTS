using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Graph
{
    /// <summary>
    /// Influence propagation system using BFS/DFS kernels.
    /// Propagates influence (morale, temperature, signal strength) through entity graphs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(EntityGraphSystem))]
    public partial struct InfluencePropagationSystem : ISystem
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

            // Propagate influence through graph
            // In full implementation, would:
            // 1. Use BFS/DFS kernels to traverse graph
            // 2. Propagate morale, temperature, signal strength along edges
            // 3. Apply decay based on edge weights and distance
            // 4. Update InfluenceFieldData components

            var graphQuery = state.GetEntityQuery(
                typeof(EntityGraphNode),
                typeof(EntityGraphEdge));

            if (graphQuery.IsEmpty)
            {
                return;
            }

            var job = new PropagateInfluenceJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(graphQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct PropagateInfluenceJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref EntityGraphNode node,
                in DynamicBuffer<EntityGraphEdge> edges)
            {
                // Propagate influence through edges
                // In full implementation, would:
                // 1. Traverse edges using BFS/DFS
                // 2. Apply influence propagation with decay
                // 3. Update target node influence values
                // 4. Handle cycles and prevent infinite propagation
            }
        }
    }
}

