using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Lightweight Utility AI system in DefaultEcs.
    /// Adjusts goal priorities from global telemetry.
    /// </summary>
    public class UtilityLearningSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 1.0f; // 1 Hz learning updates
        private AgentSyncBus _syncBus;

        public UtilityLearningSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().AsSet())
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

            // Update utility learning for entities with goal priorities
            // In full implementation, would:
            // 1. Read global telemetry from TelemetryAggregationSystem
            // 2. Adjust goal priorities based on telemetry
            // 3. Update SoftPreferences based on outcomes
            // 4. Maintain deterministic replay via RewindState.Seed

            _lastUpdateTime = currentTime;
        }
    }
}

