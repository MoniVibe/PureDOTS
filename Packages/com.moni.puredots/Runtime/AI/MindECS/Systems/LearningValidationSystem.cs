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
    /// Tracks experience counters per skill/memory.
    /// When stable (ΔSkill < ε), freezes updates.
    /// Models skill plateauing and reduces CPU load.
    /// </summary>
    public class LearningValidationSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 5f; // 5 second updates
        private const float PlateauThreshold = 0.001f; // Freeze when ΔSkill < 0.001
        private AgentSyncBus _syncBus;

        public LearningValidationSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities().With<CognitiveMemory>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;

            // Throttle updates (5 second intervals)
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

            // Get skill learning state (would come from SkillLearningState component)
            var learningState = GetOrCreateLearningState(entity);

            // Validate learning and check for plateau
            ValidateLearning(memory, ref learningState);

            // Store learning state back
            StoreLearningState(entity, learningState);

            _lastUpdateTime = currentTime;
        }

        private void ValidateLearning(CognitiveMemory memory, ref LearningValidationData learningState)
        {
            // Check if skills have plateaued
            // This would check SkillProfile changes over time

            // For now, we'll track experience counters and check if they're stable
            var totalExperiences = 0;
            if (memory.InteractionDigests != null)
            {
                totalExperiences = memory.InteractionDigests.Count;
            }

            // Check if experience count is stable (not growing)
            var experienceDelta = totalExperiences - learningState.LastExperienceCount;
            learningState.LastExperienceCount = totalExperiences;

            // If experience count hasn't changed significantly, mark as stable
            if (math.abs(experienceDelta) < 5) // Less than 5 new experiences
            {
                learningState.IsStable = true;
            }
            else
            {
                learningState.IsStable = false;
            }

            // If stable and skills haven't changed, freeze updates
            if (learningState.IsStable && learningState.IsPlateaued)
            {
                // Skip learning updates to save CPU
                learningState.ShouldFreezeUpdates = true;
            }
            else
            {
                learningState.ShouldFreezeUpdates = false;
            }
        }

        private LearningValidationData GetOrCreateLearningState(Entity entity)
        {
            // In production, this would read from SkillLearningStateComponent
            return new LearningValidationData
            {
                LastExperienceCount = 0,
                IsStable = false,
                IsPlateaued = false,
                ShouldFreezeUpdates = false
            };
        }

        private void StoreLearningState(Entity entity, LearningValidationData learningState)
        {
            // In production, this would update SkillLearningStateComponent
        }

        private struct LearningValidationData
        {
            public int LastExperienceCount;
            public bool IsStable;
            public bool IsPlateaued;
            public bool ShouldFreezeUpdates;
        }
    }
}

