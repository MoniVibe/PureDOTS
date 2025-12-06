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
    /// Aggregate-level routing system for self-organizing coordination.
    /// Handles routing decisions at the aggregate (village/fleet) level.
    /// </summary>
    public class AggregateRoutingSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.5f; // 2 Hz routing updates
        private AgentSyncBus _syncBus;

        public AggregateRoutingSystem(World world, AgentSyncBus syncBus) 
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

            // Update aggregate-level routing decisions
            UpdateAggregateRouting(aggregate);

            _lastUpdateTime = currentTime;
        }

        private void UpdateAggregateRouting(AggregateEntity aggregate)
        {
            if (_syncBus == null || aggregate.MemberGuids.Count == 0)
            {
                return;
            }

            // Aggregate-level routing:
            // 1. Collect routing decisions from member agents
            // 2. Compute aggregate-level importance and continuity scores
            // 3. Choose optimal routing paths for aggregate messages
            // 4. Update routing metadata in AgentSyncBus messages

            // In full implementation:
            // - Read routing decisions from member agents
            // - Aggregate scores at aggregate level
            // - Update RoutingMetadata in messages
        }
    }
}

