using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Cultural propagation system for Mind ECS.
    /// Receives cultural signals and updates doctrine weights.
    /// Formula: DoctrineWeight[DoctrineId] += Strength * (Wisdom + Empathy)
    /// Based on Nehaniv & Dautenhahn (2009) cultural evolution patterns.
    /// </summary>
    public class CulturalPropagationSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.2f; // 5 Hz updates
        private AgentSyncBus _syncBus;

        public CulturalPropagationSystem(World world, AgentSyncBus syncBus) 
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

            // Dequeue cultural signals from AgentSyncBus
            if (_syncBus != null && _syncBus.CulturalSignalQueueCount > 0)
            {
                using var signalBatch = _syncBus.DequeueCulturalSignalBatch(Unity.Collections.Allocator.Temp);

                // Process cultural signals and update doctrine weights
                for (int i = 0; i < signalBatch.Length; i++)
                {
                    var signal = signalBatch[i];
                    ProcessCulturalSignal(signal, entity);
                }
            }

            _lastUpdateTime = currentTime;
        }

        private void ProcessCulturalSignal(CulturalSignal signal, Entity entity)
        {
            // In full implementation, would:
            // 1. Get agent's Wisdom and Empathy stats
            // 2. Calculate doctrine weight update: Strength * (Wisdom + Empathy)
            // 3. Update DoctrineWeight buffer

            // Example:
            // var wisdom = GetWisdom(entity);
            // var empathy = GetEmpathy(entity);
            // var weightDelta = signal.Strength * (wisdom + empathy);
            // UpdateDoctrineWeight(entity, signal.DoctrineId, weightDelta);
        }
    }
}

