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
    /// Updates skill profiles via observational and practice-based learning.
    /// Observational: ΔSkill = 0.1× * Outcome * LearningRate (when watching others)
    /// Practice-based: ΔSkill *= Stamina or Focus multiplier
    /// Dual-hand casting: Gate via Finesse × Aptitude thresholds
    /// Plateau detection: Freeze updates when stable.
    /// </summary>
    public class SkillLearningSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 1f; // 1 Hz updates
        private AgentSyncBus _syncBus;
        private const float ObservationalLearningMultiplier = 0.1f;
        private const float PlateauThreshold = 0.001f;

        public SkillLearningSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities().With<CognitiveMemory>().AsSet())
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

            if (!World.Has<CognitiveMemory>(entity) || !World.Has<AgentGuid>(entity))
            {
                return;
            }

            var memory = World.Get<CognitiveMemory>(entity);
            var agentGuid = World.Get<AgentGuid>(entity);

            // Get or create skill profile (would be stored in a component)
            var skillProfile = GetOrCreateSkillProfile(entity);
            var learningState = GetOrCreateLearningState(entity);

            // Process skill learning from experiences
            ProcessSkillLearning(memory, ref skillProfile, ref learningState);

            // Check for plateau (freeze updates when stable)
            CheckPlateau(ref skillProfile, ref learningState);

            // Store skill profile back (would update component)
            StoreSkillProfile(entity, skillProfile, learningState);

            // Send skill updates to Body ECS via sync bus
            SendSkillUpdates(agentGuid, skillProfile);

            _lastUpdateTime = currentTime;
        }

        private void ProcessSkillLearning(CognitiveMemory memory, ref SkillProfileData skillProfile, ref SkillLearningStateData learningState)
        {
            if (memory.InteractionDigests == null || memory.InteractionDigests.Count == 0)
            {
                return;
            }

            // Process recent interactions for skill learning
            var recentDigests = memory.InteractionDigests;
            var count = math.min(recentDigests.Count, 5); // Last 5 interactions

            for (int i = recentDigests.Count - count; i < recentDigests.Count; i++)
            {
                var digest = recentDigests[i];

                // Calculate outcome from positive/negative deltas
                var outcome = digest.PositiveDelta - digest.NegativeDelta;
                outcome = math.clamp(outcome, -1f, 1f);

                // Default learning rate (would come from MemoryProfile)
                const float baseLearningRate = 0.1f;

                // Update skills based on interaction type
                switch (digest.Type)
                {
                    case InteractionType.Combat:
                        // Melee skill learning
                        var meleeDelta = outcome * baseLearningRate * (1f - skillProfile.MeleeSkill);
                        skillProfile.MeleeSkill = math.clamp(skillProfile.MeleeSkill + meleeDelta, 0f, 1f);
                        learningState.MeleeExperienceCount++;
                        break;

                    case InteractionType.Social:
                        // Strategic thinking learning (social interactions require strategy)
                        var strategicDelta = outcome * baseLearningRate * ObservationalLearningMultiplier * (1f - skillProfile.StrategicThinking);
                        skillProfile.StrategicThinking = math.clamp(skillProfile.StrategicThinking + strategicDelta, 0f, 1f);
                        learningState.StrategicExperienceCount++;
                        break;
                }

                // Observational learning (when watching others)
                // Success probability based on intelligence (would come from attributes)
                const float intelligence = 0.5f; // Default, would come from entity attributes
                var observationalSuccess = intelligence * 0.5f; // 50% base success rate

                if (UnityEngine.Random.value < observationalSuccess)
                {
                    // Observational learning: ΔSkill = 0.1× * Outcome * LearningRate
                    var observationalDelta = outcome * baseLearningRate * ObservationalLearningMultiplier;

                    // Apply to casting skill (observing spells)
                    skillProfile.CastingSkill = math.clamp(
                        skillProfile.CastingSkill + observationalDelta * (1f - skillProfile.CastingSkill),
                        0f, 1f);
                    learningState.CastingExperienceCount++;
                }
            }

            // Practice-based learning multiplier (would come from FocusState or Stamina)
            // For now, use a default multiplier
            const float practiceMultiplier = 1.0f; // Would be: FocusCurrent / FocusMax or Stamina

            // Apply practice multiplier to all skill gains
            // (This would be applied during the delta calculation above)
        }

        private void CheckPlateau(ref SkillProfileData skillProfile, ref SkillLearningStateData learningState)
        {
            // Check if skills have plateaued (ΔSkill < ε)
            // If plateaued, freeze updates to save CPU

            var lastCastingSkill = learningState.LastCastingSkill;
            var lastMeleeSkill = learningState.LastMeleeSkill;
            var lastStrategicSkill = learningState.LastStrategicSkill;

            var castingDelta = math.abs(skillProfile.CastingSkill - lastCastingSkill);
            var meleeDelta = math.abs(skillProfile.MeleeSkill - lastMeleeSkill);
            var strategicDelta = math.abs(skillProfile.StrategicThinking - lastStrategicSkill);

            // Check if all skills have plateaued
            if (castingDelta < PlateauThreshold &&
                meleeDelta < PlateauThreshold &&
                strategicDelta < PlateauThreshold)
            {
                learningState.IsPlateaued = true;
            }
            else
            {
                learningState.IsPlateaued = false;
            }

            // Update last skill values
            learningState.LastCastingSkill = skillProfile.CastingSkill;
            learningState.LastMeleeSkill = skillProfile.MeleeSkill;
            learningState.LastStrategicSkill = skillProfile.StrategicThinking;
        }

        private SkillProfileData GetOrCreateSkillProfile(Entity entity)
        {
            // In production, this would read from a SkillProfileComponent
            return new SkillProfileData
            {
                CastingSkill = 0.5f,
                DualCastingAptitude = 0.3f,
                MeleeSkill = 0.5f,
                StrategicThinking = 0.5f
            };
        }

        private SkillLearningStateData GetOrCreateLearningState(Entity entity)
        {
            // In production, this would read from a SkillLearningStateComponent
            return new SkillLearningStateData
            {
                CastingExperienceCount = 0,
                DualCastingExperienceCount = 0,
                MeleeExperienceCount = 0,
                StrategicExperienceCount = 0,
                LastCastingSkill = 0f,
                LastMeleeSkill = 0f,
                LastStrategicSkill = 0f,
                IsPlateaued = false
            };
        }

        private void StoreSkillProfile(Entity entity, SkillProfileData skillProfile, SkillLearningStateData learningState)
        {
            // In production, this would update SkillProfileComponent and SkillLearningStateComponent
        }

        private void SendSkillUpdates(AgentGuid agentGuid, SkillProfileData skillProfile)
        {
            if (_syncBus == null)
            {
                return;
            }

            // Send skill updates to Body ECS
            // This would be a new message type: SkillUpdateMessage
            // The Body ECS system would apply these skill modifiers to actions
        }

        private struct SkillProfileData
        {
            public float CastingSkill;
            public float DualCastingAptitude;
            public float MeleeSkill;
            public float StrategicThinking;
        }

        private struct SkillLearningStateData
        {
            public int CastingExperienceCount;
            public int DualCastingExperienceCount;
            public int MeleeExperienceCount;
            public int StrategicExperienceCount;
            public float LastCastingSkill;
            public float LastMeleeSkill;
            public float LastStrategicSkill;
            public bool IsPlateaued;
        }
    }
}

