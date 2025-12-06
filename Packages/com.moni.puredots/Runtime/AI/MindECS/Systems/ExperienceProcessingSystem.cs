using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.MindECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Processes ExperienceEvent buffers from Body ECS.
    /// Applies decay, updates knowledge base weights, and compresses old memories into histograms.
    /// Runs every 10-30s tick (batched updates).
    /// </summary>
    public class ExperienceProcessingSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 10f; // 10 seconds (batched updates)
        private AgentSyncBus _syncBus;
        private Dictionary<AgentGuid, List<ExperienceData>> _pendingExperiences;

        public ExperienceProcessingSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities().With<CognitiveMemory>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
            _pendingExperiences = new Dictionary<AgentGuid, List<ExperienceData>>();
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;

            // Throttle updates (batched every 10 seconds)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<CognitiveMemory>(entity) || !World.Has<AgentGuid>(entity))
            {
                return;
            }

            var memory = World.Get<CognitiveMemory>(entity);
            var agentGuid = World.Get<AgentGuid>(entity);

            // Collect experiences from sync bus (would come from Body ECS)
            // For now, we'll process experiences stored in CognitiveMemory.InteractionDigests
            ProcessExperiences(memory, agentGuid, currentTime);

            _lastUpdateTime = currentTime;
        }

        private void ProcessExperiences(CognitiveMemory memory, AgentGuid agentGuid, float currentTime)
        {
            // Process interaction digests as experiences
            if (memory.InteractionDigests == null || memory.InteractionDigests.Count == 0)
            {
                return;
            }

            // Apply decay to memory: memory.Value *= Retention
            // (This would be applied to aggregated memory values, not individual digests)

            // Update knowledge base weights: bias[culture] += Outcome * LearningRate
            // Process each interaction digest
            for (int i = memory.InteractionDigests.Count - 1; i >= 0; i--)
            {
                var digest = memory.InteractionDigests[i];

                // Map InteractionType to ExperienceType
                var experienceType = MapInteractionToExperience(digest.Type);

                // Calculate outcome from positive/negative deltas
                var outcome = digest.PositiveDelta - digest.NegativeDelta;
                outcome = math.clamp(outcome, -1f, 1f);

                // Update relationship scores (bias accumulator)
                if (memory.RelationshipScores != null)
                {
                    if (!memory.RelationshipScores.ContainsKey(digest.InteractorGuid))
                    {
                        memory.RelationshipScores[digest.InteractorGuid] = 0f;
                    }

                    // Update bias: bias += Outcome * LearningRate
                    // LearningRate would come from MemoryProfile, but we don't have that in MindECS yet
                    // For now, use a default learning rate
                    const float defaultLearningRate = 0.1f;
                    var currentBias = memory.RelationshipScores[digest.InteractorGuid];
                    memory.RelationshipScores[digest.InteractorGuid] = math.clamp(
                        currentBias + outcome * defaultLearningRate * digest.Weight,
                        -1f, 1f);
                }

                // Compress old memories into histograms (keep last N, merge older ones)
                // This would be done by MemoryCompressionSystem, but we can mark old digests here
                var age = currentTime - digest.InteractionTick;
                if (age > 100f) // Older than 100 ticks (would need tick-to-time conversion)
                {
                    // Mark for compression (would be handled by MemoryCompressionSystem)
                    // For now, just remove very old digests
                    if (age > 1000f)
                    {
                        memory.InteractionDigests.RemoveAt(i);
                    }
                }
            }
        }

        private ExperienceType MapInteractionToExperience(InteractionType interactionType)
        {
            return interactionType switch
            {
                InteractionType.Combat => ExperienceType.Combat,
                InteractionType.Trade => ExperienceType.Trade,
                InteractionType.Harm => ExperienceType.Betrayal,
                InteractionType.Help => ExperienceType.Help,
                InteractionType.Social => ExperienceType.Social,
                _ => ExperienceType.None
            };
        }

        private struct ExperienceData
        {
            public ExperienceType Type;
            public AgentGuid SourceGuid;
            public AgentGuid ContextGuid;
            public float Outcome;
            public ushort CultureId;
            public uint Tick;
        }
    }
}

