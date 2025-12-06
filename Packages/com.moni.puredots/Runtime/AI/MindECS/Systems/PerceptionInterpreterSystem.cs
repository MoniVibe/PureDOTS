using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Perception interpreter system (Mind ECS).
    /// Interprets compressed feature vectors via profile weights.
    /// </summary>
    public class PerceptionInterpreterSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.25f; // 4 Hz interpretation updates
        private AgentSyncBus _syncBus;

        public PerceptionInterpreterSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Throttle updates (4 Hz)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            // Interpret perception features
            // In full implementation, would:
            // 1. Read PerceptionFeatureVector from Body ECS via AgentSyncBus
            // 2. Apply perception profile weights ("smell bias", "radar trust")
            // 3. Interpret features into cognitive decisions
            // 4. Update Mind ECS cognitive state

            // Environmental sensor interpretation:
            // - Humidity detection (moisture grid sampling)
            // - Heat detection (temperature grid sampling)
            // - Sound detection (spatial queries)
            // These feed into environmental desirability evaluation

            _lastUpdateTime = currentTime;
        }
    }
}

