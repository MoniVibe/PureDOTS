using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// DefaultEcs system that handles aggregate-level consensus voting.
    /// Collects votes from aggregate members and publishes consensus outcomes.
    /// </summary>
    public class AggregateConsensusSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.5f; // 2 Hz consensus updates
        private AgentSyncBus _syncBus;

        public AggregateConsensusSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().With<AggregateEntity>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Throttle updates (2 Hz)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<AggregateEntity>(entity))
            {
                return;
            }

            var aggregate = World.Get<AggregateEntity>(entity);

            // Collect votes from aggregate members (simulated for now)
            // In full implementation, this would read votes from AgentSyncBus
            CollectAndPublishConsensus(aggregate);

            _lastUpdateTime = currentTime;
        }

        private void CollectAndPublishConsensus(AggregateEntity aggregate)
        {
            if (_syncBus == null || aggregate.MemberGuids.Count == 0)
            {
                return;
            }

            // For each member, collect their vote (simplified - in reality would read from bus)
            // This is a placeholder - actual implementation would:
            // 1. Read votes from AgentSyncBus consensus vote queue
            // 2. Filter votes by cluster GUID matching aggregate GUID
            // 3. Aggregate votes and publish outcome

            // Example: Publish a local consensus vote for this aggregate
            // In practice, individual agents would cast votes via AgentSyncBus.EnqueueConsensusVote
            // This system would read those votes and resolve them

            // For now, we'll just ensure the aggregate can participate in consensus
            // The actual voting happens at the agent level via ConsensusArbitrationSystem
        }
    }
}

