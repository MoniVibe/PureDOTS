using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Explainability system generating explanations for AI decisions.
    /// Makes agents explain their choices for human-readable diagnostics.
    /// </summary>
    public class ExplainabilitySystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.5f; // 2 Hz explanation updates
        private AgentSyncBus _syncBus;

        public ExplainabilitySystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().AsSet())
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Generate explanations for decisions
            // In full implementation, would:
            // 1. Read decision reasoning from Mind ECS entities
            // 2. Generate human-readable explanations
            // 3. Store in TelemetryStream for debugging/UI
            // 4. Export structured decision trees
            // 5. Provide diagnostics for AI tuning

            // Example explanations:
            // - "Reason: threat proximity" (DecisionReasonCode.ThreatProximity)
            // - "Reason: low focus" (DecisionReasonCode.LowFocus)
            // - "Reason: reputation penalty" (DecisionReasonCode.ReputationPenalty)
#endif

            _lastUpdateTime = currentTime;
        }
    }
}

