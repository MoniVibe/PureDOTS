using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.AI.Social.Systems;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// Trust network system for aggregates.
    /// Maintains sparse relationship matrices and implements trust update rules.
    /// Updates trust networks every 2-5 seconds (temporal batching).
    /// Based on Kozlowski et al. (2016) trust network patterns.
    /// </summary>
    public class TrustNetworkSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 2.0f; // 2 seconds (temporal batching)
        private AgentSyncBus _syncBus;
        private const float LearningRate = 0.1f; // Default learning rate for trust updates
        private const float IndirectTrustPropagationFactor = 0.5f; // Factor for indirect trust
        private const int MaxNeighborsPerAgent = 100; // Limit relationship tracking

        public TrustNetworkSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().With<AggregateEntity>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Temporal batching: update every 2 seconds
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<AggregateEntity>(entity))
            {
                return;
            }

            var aggregate = World.Get<AggregateEntity>(entity);

            // Get or create trust network
            TrustNetwork trustNetwork;
            if (World.Has<TrustNetwork>(entity))
            {
                trustNetwork = World.Get<TrustNetwork>(entity);
            }
            else
            {
                trustNetwork = new TrustNetwork();
                World.Set(entity, trustNetwork);
            }

            // Update trust network from member interactions
            UpdateTrustNetwork(aggregate, trustNetwork);

            // Prune to keep only nearest neighbors (performance optimization)
            trustNetwork.PruneToNearestNeighbors(MaxNeighborsPerAgent);

            trustNetwork.LastUpdateTick = (uint)(currentTime * 60); // Approximate tick number

            _lastUpdateTime = currentTime;
        }

        private void UpdateTrustNetwork(AggregateEntity aggregate, TrustNetwork trustNetwork)
        {
            // In full implementation, this would:
            // 1. Collect interaction outcomes from member agents via AgentSyncBus
            // 2. Update trust values using trust update rule
            // 3. Calculate indirect trust propagation
            // 4. Aggregate trust to faction/cultural levels

            // For now, we provide the structure - actual data would come from Body ECS
            // via social message processing in CooperationSystem

            // Example: Update trust based on aggregate stats
            // If aggregate has high morale, members trust each other more
            if (aggregate.Stats.Morale > 70f)
            {
                // Increase trust between members
                foreach (var memberGuid in aggregate.MemberGuids)
                {
                    foreach (var otherMemberGuid in aggregate.MemberGuids)
                    {
                        if (memberGuid.Equals(otherMemberGuid))
                        {
                            continue;
                        }

                        var currentTrust = trustNetwork.GetTrust(otherMemberGuid);
                        var newTrust = Mathf.Clamp01(currentTrust + 0.01f); // Small positive adjustment
                        trustNetwork.SetTrust(otherMemberGuid, newTrust);
                    }
                }
            }
        }

        /// <summary>
        /// Updates trust based on interaction outcome.
        /// Formula: Trust = Trust + (InteractionOutcome - ExpectedOutcome) * LearningRate
        /// </summary>
        public void UpdateTrustFromInteraction(
            TrustNetwork trustNetwork,
            AgentGuid agentGuid,
            float interactionOutcome,
            float expectedOutcome)
        {
            var currentTrust = trustNetwork.GetTrust(agentGuid);
            var newTrust = CooperationResolutionSystem.UpdateTrust(
                currentTrust,
                interactionOutcome,
                expectedOutcome,
                LearningRate);
            trustNetwork.SetTrust(agentGuid, newTrust);

            // Update reputation as lerp of trust
            var currentReputation = trustNetwork.GetReputation(agentGuid);
            var newReputation = Mathf.Lerp(currentReputation, newTrust, 0.1f);
            trustNetwork.SetReputation(agentGuid, newReputation);
        }

        /// <summary>
        /// Calculates indirect trust propagation.
        /// If A trusts B and B trusts C, A inherits partial trust in C.
        /// </summary>
        public void PropagateIndirectTrust(TrustNetwork trustNetwork, AgentGuid agentA, AgentGuid agentB, AgentGuid agentC)
        {
            var trustAB = trustNetwork.GetTrust(agentB);
            var trustBC = trustNetwork.GetTrust(agentC);

            var indirectTrust = CooperationResolutionSystem.CalculateIndirectTrust(
                trustAB,
                trustBC,
                IndirectTrustPropagationFactor);

            // Update trust AC if indirect trust is higher than current
            var currentTrustAC = trustNetwork.GetTrust(agentC);
            if (indirectTrust > currentTrustAC)
            {
                trustNetwork.SetTrust(agentC, indirectTrust);
            }
        }
    }
}

