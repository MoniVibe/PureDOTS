using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.MindECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Updates EmotionState from interactions (combat, betrayal, kindness).
    /// Modulates learning: LearningRate *= (1 + Pride - Fear)
    /// Updates bias: Bias[culture] += Anger * 0.1f
    /// Feeds decision modifiers back to Body ECS via sync bus.
    /// </summary>
    public class EmotionalLearningSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.25f; // 4 Hz updates
        private AgentSyncBus _syncBus;

        public EmotionalLearningSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities().With<CognitiveMemory>().With<PersonalityProfile>().AsSet())
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

            if (!World.Has<CognitiveMemory>(entity) || 
                !World.Has<PersonalityProfile>(entity) ||
                !World.Has<AgentGuid>(entity))
            {
                return;
            }

            var memory = World.Get<CognitiveMemory>(entity);
            var personality = World.Get<PersonalityProfile>(entity);
            var agentGuid = World.Get<AgentGuid>(entity);

            // Get or create emotion state (would be stored in a component, but we'll use a dictionary for now)
            // In production, this would be a component: EmotionStateComponent
            var emotionState = GetOrCreateEmotionState(entity);

            // Update emotions from interaction digests
            UpdateEmotionsFromInteractions(memory, personality, ref emotionState);

            // Modulate learning rate: LearningRate *= (1 + Pride - Fear)
            var learningRateMultiplier = 1f + emotionState.Pride - emotionState.Fear;
            learningRateMultiplier = math.clamp(learningRateMultiplier, 0.1f, 2f);

            // Update bias: Bias[culture] += Anger * 0.1f
            // (This would update culture-specific bias values)

            // Store emotion state back (would update component)
            StoreEmotionState(entity, emotionState);

            // Send emotion modulator to Body ECS via sync bus
            SendEmotionModulator(agentGuid, emotionState, learningRateMultiplier);

            _lastUpdateTime = currentTime;
        }

        private void UpdateEmotionsFromInteractions(CognitiveMemory memory, PersonalityProfile personality, ref EmotionStateData emotionState)
        {
            if (memory.InteractionDigests == null || memory.InteractionDigests.Count == 0)
            {
                return;
            }

            // Process recent interactions (last 10)
            var recentDigests = memory.InteractionDigests;
            var count = math.min(recentDigests.Count, 10);

            for (int i = recentDigests.Count - count; i < recentDigests.Count; i++)
            {
                var digest = recentDigests[i];

                // Update emotions based on interaction type and outcome
                switch (digest.Type)
                {
                    case InteractionType.Combat:
                        if (digest.PositiveDelta > digest.NegativeDelta)
                        {
                            // Won combat: increase pride, decrease fear
                            emotionState.Pride += 0.1f * digest.Weight;
                            emotionState.Fear -= 0.05f * digest.Weight;
                        }
                        else
                        {
                            // Lost combat: increase fear, decrease pride
                            emotionState.Fear += 0.1f * digest.Weight;
                            emotionState.Pride -= 0.05f * digest.Weight;
                        }
                        break;

                    case InteractionType.Harm:
                        // Betrayal: increase anger, decrease trust
                        emotionState.Anger += 0.15f * digest.Weight;
                        emotionState.Trust -= 0.1f * digest.Weight;
                        break;

                    case InteractionType.Help:
                        // Kindness: increase trust, decrease anger
                        emotionState.Trust += 0.1f * digest.Weight;
                        emotionState.Anger -= 0.05f * digest.Weight;
                        break;

                    case InteractionType.Trade:
                        // Successful trade: increase trust
                        if (digest.PositiveDelta > 0f)
                        {
                            emotionState.Trust += 0.05f * digest.Weight;
                        }
                        break;
                }

                // Apply personality modifiers
                emotionState.Anger *= (0.5f + personality.Aggressiveness * 0.5f);
                emotionState.Trust *= (0.5f + personality.TrustLevel * 0.5f);
            }

            // Clamp emotions to 0-1 range
            emotionState.Anger = math.clamp(emotionState.Anger, 0f, 1f);
            emotionState.Trust = math.clamp(emotionState.Trust, 0f, 1f);
            emotionState.Fear = math.clamp(emotionState.Fear, 0f, 1f);
            emotionState.Pride = math.clamp(emotionState.Pride, 0f, 1f);

            // Apply decay over time (emotions fade)
            var decayRate = 0.01f; // 1% decay per update
            emotionState.Anger *= (1f - decayRate);
            emotionState.Trust *= (1f - decayRate);
            emotionState.Fear *= (1f - decayRate);
            emotionState.Pride *= (1f - decayRate);
        }

        private EmotionStateData GetOrCreateEmotionState(Entity entity)
        {
            // In production, this would read from an EmotionStateComponent
            // For now, return default neutral state
            return new EmotionStateData
            {
                Anger = 0f,
                Trust = 0.5f,
                Fear = 0f,
                Pride = 0.5f
            };
        }

        private void StoreEmotionState(Entity entity, EmotionStateData emotionState)
        {
            // In production, this would update an EmotionStateComponent
            // For now, we'll just store it in memory or send via sync bus
        }

        private void SendEmotionModulator(AgentGuid agentGuid, EmotionStateData emotionState, float learningRateMultiplier)
        {
            if (_syncBus == null)
            {
                return;
            }

            // Send emotion modulator to Body ECS
            // This would be a new message type: EmotionModulatorMessage
            // For now, we'll extend MindToBodyMessage or create a new message type
            // The Body ECS system would apply these modifiers to decision-making
        }

        private struct EmotionStateData
        {
            public float Anger;
            public float Trust;
            public float Fear;
            public float Pride;
        }
    }
}

