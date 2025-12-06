using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Field gradient sampler system in Mind ECS.
    /// Samples gradients from potential fields to decide actions.
    /// </summary>
    public class FieldGradientSamplerSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.2f; // 5 Hz gradient sampling
        private AgentSyncBus _syncBus;

        public FieldGradientSamplerSystem(World world, AgentSyncBus syncBus) 
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

            // Sample gradients from potential fields
            // In full implementation, would:
            // 1. Read FieldGradientSample buffers from Body ECS via AgentSyncBus
            // 2. Sample gradients at agent position
            // 3. Use gradient direction/magnitude to influence decisions
            // 4. Apply behavior field coefficients (social bias, aggression, fear)
            // 5. Generate intents based on gradient samples

            _lastUpdateTime = currentTime;
        }
    }
}

