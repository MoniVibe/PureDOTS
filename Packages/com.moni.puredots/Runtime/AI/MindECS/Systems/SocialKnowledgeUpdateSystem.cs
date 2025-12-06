using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Social knowledge update system for Mind ECS.
    /// Updates SocialKnowledge components from interactions received via AgentSyncBus.
    /// Runs at 2-5 Hz (configurable per entity).
    /// </summary>
    public class SocialKnowledgeUpdateSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.2f; // 5 Hz updates
        private AgentSyncBus _syncBus;

        public SocialKnowledgeUpdateSystem(World world, AgentSyncBus syncBus) 
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

            // In full implementation, would:
            // 1. Read social interaction outcomes from AgentSyncBus
            // 2. Update SocialKnowledge components in Mind ECS
            // 3. Sync back to Body ECS via AgentSyncBus

            // For now, we provide the structure
            // Actual data would come from CooperationSystem processing social messages

            _lastUpdateTime = currentTime;
        }
    }
}

