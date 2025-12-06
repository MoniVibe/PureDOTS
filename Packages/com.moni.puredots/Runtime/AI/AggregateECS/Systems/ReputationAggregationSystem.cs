using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// Reputation aggregation system for aggregates.
    /// Aggregates trust to faction/cultural levels when trust converges.
    /// Runs at lower frequency than TrustNetworkSystem (every 5 seconds).
    /// </summary>
    public class ReputationAggregationSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 5.0f; // 5 seconds (slower than trust updates)
        private const float TrustConvergenceThreshold = 0.05f; // Trust variance threshold for convergence

        public ReputationAggregationSystem(World world) 
            : base(world.GetEntities().With<AggregateEntity>().With<TrustNetwork>().AsSet())
        {
            _lastUpdateTime = 0f;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Temporal batching: update every 5 seconds
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<AggregateEntity>(entity) || !World.Has<TrustNetwork>(entity))
            {
                return;
            }

            var aggregate = World.Get<AggregateEntity>(entity);
            var trustNetwork = World.Get<TrustNetwork>(entity);

            // Aggregate trust to faction level when stable
            AggregateReputationToFactionLevel(aggregate, trustNetwork);

            _lastUpdateTime = currentTime;
        }

        private void AggregateReputationToFactionLevel(AggregateEntity aggregate, TrustNetwork trustNetwork)
        {
            // Check if trust has converged (low variance)
            if (trustNetwork.TrustMap.Count < 2)
            {
                return; // Need at least 2 relationships to check convergence
            }

            // Calculate trust variance
            float meanTrust = 0f;
            foreach (var trust in trustNetwork.TrustMap.Values)
            {
                meanTrust += trust;
            }
            meanTrust /= trustNetwork.TrustMap.Count;

            float variance = 0f;
            foreach (var trust in trustNetwork.TrustMap.Values)
            {
                var diff = trust - meanTrust;
                variance += diff * diff;
            }
            variance /= trustNetwork.TrustMap.Count;

            // If trust has converged (low variance), aggregate to faction level
            if (variance < TrustConvergenceThreshold)
            {
                // Aggregate reputation: average of all trust values
                float aggregateReputation = meanTrust;

                // Store aggregate reputation (could be stored in a FactionReputation component)
                // For now, we update reputation map with aggregate value
                foreach (var agentGuid in trustNetwork.TrustMap.Keys)
                {
                    trustNetwork.SetReputation(agentGuid, aggregateReputation);
                }
            }
        }
    }
}

