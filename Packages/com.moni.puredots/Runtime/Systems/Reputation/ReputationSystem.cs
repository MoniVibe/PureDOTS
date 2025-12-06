using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Reputation
{
    /// <summary>
    /// Reputation system updating reputation graphs from interactions.
    /// Feeds into Intent generation (vengefulness, trust).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(EmotionSystem))]
    public partial struct ReputationSystem : ISystem
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

            // Update reputation graphs from interactions
            var reputationQuery = state.GetEntityQuery(
                typeof(ReputationNode),
                typeof(ReputationEdge));

            if (reputationQuery.IsEmpty)
            {
                return;
            }

            var job = new UpdateReputationJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(reputationQuery, state.Dependency);

            // Compute sentiment matrices at aggregate level
            ComputeSentimentMatrices(ref state, tickState.Tick);
        }

        [BurstCompile]
        private void ComputeSentimentMatrices(ref SystemState state, uint currentTick)
        {
            // Compute sentiment matrices for factions
            // In full implementation, would:
            // 1. Aggregate reputation edges by faction
            // 2. Compute faction-to-faction sentiment values
            // 3. Store in SentimentMatrix components
            // 4. Use for aggregate-level decision making
        }

        [BurstCompile]
        private partial struct UpdateReputationJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref ReputationNode node,
                DynamicBuffer<ReputationEdge> edges)
            {
                // Update reputation from edges
                // In full implementation, would:
                // 1. Process reputation edges
                // 2. Apply weighted decay over time
                // 3. Compute overall reputation score
                // 4. Update node reputation value

                var totalReputation = 0f;
                var totalWeight = 0f;

                for (int i = 0; i < edges.Length; i++)
                {
                    var edge = edges[i];
                    
                    // Apply decay
                    var age = CurrentTick - edge.LastUpdateTick;
                    var decayFactor = math.exp(-0.001f * age); // Decay rate
                    var weight = edge.Weight * decayFactor;

                    totalReputation += edge.ReputationValue * weight;
                    totalWeight += weight;
                }

                if (totalWeight > 0f)
                {
                    node.OverallReputation = math.clamp(totalReputation / totalWeight, -1f, 1f);
                }

                node.InteractionCount = edges.Length;
                node.LastUpdateTick = CurrentTick;
            }
        }
    }
}

