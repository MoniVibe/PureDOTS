using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.MindECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.AI;
using PureDOTS.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Updates perception skills and confidence scores via recursive Bayesian updates.
    /// For cloaked or deceptive entities: posterior = prior * likelihood / normalization
    /// As entities fight invisible opponents, likelihood sharpens (less uncertainty over time).
    /// Skill improves over engagements — they "guess better" with experience.
    /// </summary>
    public class PerceptionLearningSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.5f; // 2 Hz updates
        private AgentSyncBus _syncBus;

        public PerceptionLearningSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities().With<CognitiveMemory>().AsSet())
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

            if (!World.Has<CognitiveMemory>(entity) || !World.Has<AgentGuid>(entity))
            {
                return;
            }

            var memory = World.Get<CognitiveMemory>(entity);
            var agentGuid = World.Get<AgentGuid>(entity);

            // Get or create perception skill profile
            var perceptionSkill = GetOrCreatePerceptionSkill(entity);
            var confidenceScore = GetOrCreateConfidenceScore(entity);

            // Process percepts for Bayesian learning
            ProcessBayesianLearning(memory, ref perceptionSkill, ref confidenceScore);

            // Store perception skill back
            StorePerceptionSkill(entity, perceptionSkill, confidenceScore);

            // Send perception updates to Body ECS via sync bus
            SendPerceptionUpdates(agentGuid, perceptionSkill, confidenceScore);

            _lastUpdateTime = currentTime;
        }

        private void ProcessBayesianLearning(CognitiveMemory memory, ref PerceptionSkillData perceptionSkill, ref ConfidenceScoreData confidenceScore)
        {
            if (memory.RecentPercepts == null || memory.RecentPercepts.Count == 0)
            {
                return;
            }

            // Process recent percepts for Bayesian updates
            var recentPercepts = memory.RecentPercepts;
            var count = math.min(recentPercepts.Count, 10); // Last 10 percepts

            for (int i = recentPercepts.Count - count; i < recentPercepts.Count; i++)
            {
                var percept = recentPercepts[i];

                // Get prior confidence for this sensor type
                var prior = confidenceScore.GetConfidence(percept.Type);
                var likelihood = percept.Confidence;

                // Recursive Bayesian update: posterior = prior * likelihood / normalization
                // Normalization = prior * likelihood + (1 - prior) * (1 - likelihood)
                var normalization = prior * likelihood + (1f - prior) * (1f - likelihood);

                // Avoid division by zero
                if (normalization > 0.0001f)
                {
                    var posterior = (prior * likelihood) / normalization;

                    // Update confidence score
                    confidenceScore.SetConfidence(percept.Type, posterior);

                    // Update perception skill (improves with experience)
                    var skillDelta = (posterior - prior) * 0.1f; // 10% of confidence improvement
                    perceptionSkill.UpdateSkill(percept.Type, skillDelta);
                }
            }

            // Apply skill improvements to confidence (entities "guess better" with experience)
            ApplySkillToConfidence(ref perceptionSkill, ref confidenceScore);
        }

        private void ApplySkillToConfidence(ref PerceptionSkillData perceptionSkill, ref ConfidenceScoreData confidenceScore)
        {
            // Higher skill → higher base confidence
            // Skill acts as a multiplier on confidence

            foreach (var sensorType in System.Enum.GetValues(typeof(SensorType)))
            {
                var type = (SensorType)sensorType;
                var skill = perceptionSkill.GetSkill(type);
                var currentConfidence = confidenceScore.GetConfidence(type);

                // Apply skill multiplier: confidence = baseConfidence * (0.5 + skill * 0.5)
                // This means skill 0 = 50% base confidence, skill 1 = 100% base confidence
                var skillMultiplier = 0.5f + skill * 0.5f;
                var adjustedConfidence = currentConfidence * skillMultiplier;

                confidenceScore.SetConfidence(type, math.clamp(adjustedConfidence, 0f, 1f));
            }
        }

        private PerceptionSkillData GetOrCreatePerceptionSkill(Entity entity)
        {
            // In production, this would read from a PerceptionSkillComponent
            return new PerceptionSkillData();
        }

        private ConfidenceScoreData GetOrCreateConfidenceScore(Entity entity)
        {
            // In production, this would read from a ConfidenceScoreComponent
            return new ConfidenceScoreData();
        }

        private void StorePerceptionSkill(Entity entity, PerceptionSkillData perceptionSkill, ConfidenceScoreData confidenceScore)
        {
            // In production, this would update PerceptionSkillComponent and ConfidenceScoreComponent
        }

        private void SendPerceptionUpdates(AgentGuid agentGuid, PerceptionSkillData perceptionSkill, ConfidenceScoreData confidenceScore)
        {
            if (_syncBus == null)
            {
                return;
            }

            // Send perception updates to Body ECS
            // This would be a new message type: PerceptionUpdateMessage
            // The Body ECS system would apply these perception modifiers to sensor systems
        }

        private struct PerceptionSkillData
        {
            private float _visionSkill;
            private float _smellSkill;
            private float _hearingSkill;
            private float _radarSkill;

            public float GetSkill(SensorType type)
            {
                return type switch
                {
                    SensorType.Vision => _visionSkill,
                    SensorType.Smell => _smellSkill,
                    SensorType.Hearing => _hearingSkill,
                    SensorType.Radar => _radarSkill,
                    _ => 0.5f // Default neutral skill
                };
            }

            public void UpdateSkill(SensorType type, float delta)
            {
                switch (type)
                {
                    case SensorType.Vision:
                        _visionSkill = math.clamp(_visionSkill + delta, 0f, 1f);
                        break;
                    case SensorType.Smell:
                        _smellSkill = math.clamp(_smellSkill + delta, 0f, 1f);
                        break;
                    case SensorType.Hearing:
                        _hearingSkill = math.clamp(_hearingSkill + delta, 0f, 1f);
                        break;
                    case SensorType.Radar:
                        _radarSkill = math.clamp(_radarSkill + delta, 0f, 1f);
                        break;
                }
            }
        }

        private struct ConfidenceScoreData
        {
            private float _visionConfidence;
            private float _smellConfidence;
            private float _hearingConfidence;
            private float _radarConfidence;

            public float GetConfidence(SensorType type)
            {
                return type switch
                {
                    SensorType.Vision => _visionConfidence,
                    SensorType.Smell => _smellConfidence,
                    SensorType.Hearing => _hearingConfidence,
                    SensorType.Radar => _radarConfidence,
                    _ => 0.5f // Default neutral confidence
                };
            }

            public void SetConfidence(SensorType type, float confidence)
            {
                switch (type)
                {
                    case SensorType.Vision:
                        _visionConfidence = math.clamp(confidence, 0f, 1f);
                        break;
                    case SensorType.Smell:
                        _smellConfidence = math.clamp(confidence, 0f, 1f);
                        break;
                    case SensorType.Hearing:
                        _hearingConfidence = math.clamp(confidence, 0f, 1f);
                        break;
                    case SensorType.Radar:
                        _radarConfidence = math.clamp(confidence, 0f, 1f);
                        break;
                }
            }
        }
    }
}

