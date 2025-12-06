using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.AI.AggregateECS.Systems;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.AI.Social.Systems;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Goal balancing system for Mind ECS.
    /// Maximizes PersonalUtility + WeightedGroupUtility.
    /// Consumes aggregate intents and applies bias to individual agent goals.
    /// Based on Pagliuca et al. (2023) goal balancing patterns.
    /// </summary>
    public class GoalBalancingSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.2f; // 5 Hz updates
        private AgentSyncBus _syncBus;

        public GoalBalancingSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Throttle updates (5 Hz)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            // Consume aggregate intents from AgentSyncBus
            if (_syncBus != null && _syncBus.AggregateIntentQueueCount > 0)
            {
                var aggregateIntents = _syncBus.DequeueAggregateIntentBatch();

                // Process aggregate intents and apply bias to individual goals
                foreach (var intent in aggregateIntents)
                {
                    ProcessAggregateIntent(intent, entity);
                }
            }

            _lastUpdateTime = currentTime;
        }

        private void ProcessAggregateIntent(AggregateIntentMessage intent, Entity entity)
        {
            // In full implementation, would:
            // 1. Get agent's personal utility for current goal
            // 2. Get group utility from aggregate intent
            // 3. Calculate combined utility: PersonalUtility + WeightedGroupUtility
            // 4. Apply bias to goal priorities

            // Example calculation:
            // var personalUtility = GetPersonalUtility(entity);
            // var groupUtility = intent.Priority;
            // var groupWeight = 0.3f; // Weight for group utility
            // var combinedUtility = CooperationResolutionSystem.CalculateCombinedUtility(
            //     personalUtility, groupUtility, groupWeight);
        }
    }
}

