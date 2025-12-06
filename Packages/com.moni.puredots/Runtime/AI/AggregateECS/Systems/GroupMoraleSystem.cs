using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// Group morale system for aggregates.
    /// Calculates group morale: mean(Members.Morale) ± variance * Cohesion
    /// Updates aggregate stats with group morale.
    /// </summary>
    public class GroupMoraleSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 1.0f; // 1 Hz (same as aggregate updates)
        private AgentSyncBus _syncBus;

        public GroupMoraleSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().With<AggregateEntity>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Throttle updates (1 Hz)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<AggregateEntity>(entity))
            {
                return;
            }

            var aggregate = World.Get<AggregateEntity>(entity);

            // Calculate group morale from member motivations
            // In full implementation, would:
            // 1. Query Body ECS for member Motivation components
            // 2. Calculate mean morale
            // 3. Calculate variance
            // 4. Apply cohesion factor
            // 5. Update aggregate stats

            // Simplified calculation based on aggregate stats
            var groupMorale = CalculateGroupMorale(aggregate);
            aggregate.Stats.Morale = groupMorale;

            _lastUpdateTime = currentTime;
        }

        private float CalculateGroupMorale(AggregateEntity aggregate)
        {
            // Simplified: use aggregate stats as proxy for morale
            // In full implementation, would query member Motivation components
            var baseMorale = aggregate.Stats.Morale; // Already aggregated from members

            // Apply variance and cohesion (simplified)
            // GroupMorale = mean(Members.Morale) ± variance * Cohesion
            // For now, return base morale
            return Mathf.Clamp01(baseMorale / 100f); // Normalize to 0-1
        }
    }
}

