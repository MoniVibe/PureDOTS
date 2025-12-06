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
    /// Entity graph construction and maintenance system.
    /// Builds adjacency graphs using NativeParallelMultiHashMap for relationships.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(SpatialGridBuildSystem))]
    public partial struct EntityGraphSystem : ISystem
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

            // Build graph from entity relationships
            // In full implementation, would:
            // 1. Query entities with relationship components (LimbElement, AggregateMembership, etc.)
            // 2. Build adjacency graph using NativeParallelMultiHashMap<int, int>
            // 3. Store graph structure in EntityGraphNode and EntityGraphEdge components
            // 4. Update graph when relationships change

            var nodeQuery = state.GetEntityQuery(typeof(EntityGraphNode));
            if (nodeQuery.IsEmpty)
            {
                return;
            }

            var job = new BuildGraphJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(nodeQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct BuildGraphJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref EntityGraphNode node,
                DynamicBuffer<EntityGraphEdge> edges)
            {
                // Update neighbor count
                node.NeighborCount = edges.Length;
                node.LastUpdateTick = CurrentTick;
            }
        }
    }
}

