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
    /// Mission chain execution coordinator.
    /// Manages execution of multi-stage tasks (mine → refine → deliver → build).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct MissionChainSystem : ISystem
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

            // Process active execution paths
            var pathQuery = state.GetEntityQuery(typeof(ExecutionPath), typeof(DynamicBuffer<MissionNode>));
            
            var job = new ProcessMissionPathsJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(pathQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct ProcessMissionPathsJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref ExecutionPath path,
                DynamicBuffer<MissionNode> nodes)
            {
                if (path.Status != ExecutionPathStatus.Active)
                {
                    return;
                }

                // Find current node
                var currentNodeIndex = -1;
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].NodeId == path.StartNodeId)
                    {
                        currentNodeIndex = i;
                        break;
                    }
                }

                if (currentNodeIndex < 0)
                {
                    path.Status = ExecutionPathStatus.Failed;
                    return;
                }

                var currentNode = nodes[currentNodeIndex];
                
                // Check if current node is completed
                if (currentNode.Status == MissionNodeStatus.Completed)
                {
                    // Move to next node
                    if (currentNode.NextNodeId >= 0)
                    {
                        // Find next node
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            if (nodes[i].NodeId == currentNode.NextNodeId)
                            {
                                path.StartNodeId = currentNode.NextNodeId;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Path completed
                        path.Status = ExecutionPathStatus.Completed;
                    }
                }
                else if (currentNode.Status == MissionNodeStatus.Failed)
                {
                    path.Status = ExecutionPathStatus.Failed;
                }
            }
        }
    }
}

