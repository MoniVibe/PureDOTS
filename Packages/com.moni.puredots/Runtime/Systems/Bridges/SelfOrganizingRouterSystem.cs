using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Shared;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Self-organizing router system for decentralized coordination.
    /// Nodes choose next message recipient locally (no central coordinator).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ConsensusArbitrationSystem))]
    public partial struct SelfOrganizingRouterSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentSyncState>();
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

            // Update routing decisions for nodes with routing state
            var routingQuery = state.GetEntityQuery(
                typeof(AgentSyncId),
                typeof(RoutingNodeState),
                typeof(ImportanceScore),
                typeof(ContinuityScore));

            if (routingQuery.IsEmpty)
            {
                return;
            }

            var job = new UpdateRoutingDecisionsJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(routingQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct UpdateRoutingDecisionsJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                in AgentSyncId syncId,
                ref RoutingNodeState nodeState,
                ref ImportanceScore importanceScore,
                ref ContinuityScore continuityScore,
                ref RoutingDecision decision)
            {
                // Compute combined score
                var combinedScore = importanceScore.Score + continuityScore.Score;

                // Update routing decision
                decision.CombinedScore = combinedScore;
                decision.DecisionTick = CurrentTick;
                decision.Reason = RoutingDecisionReason.HighestScore;

                // In full implementation, would:
                // 1. Query neighbors from spatial grid
                // 2. Evaluate importance and continuity scores for each neighbor
                // 3. Choose neighbor with highest combined score
                // 4. Set NextRecipientGuid to chosen neighbor
            }
        }
    }
}

