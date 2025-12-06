using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Motivation
{
    /// <summary>
    /// Emotion-driven learning system - 0.2Hz motivation layer.
    /// Applies emotion weights to action selection and exploration behavior.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MotivationSystemGroup))]
    [UpdateAfter(typeof(LimbicModulationSystem))]
    public partial struct EmotionDrivenLearningSystem : ISystem
    {
        private const float UpdateInterval = 5.0f; // 0.2Hz
        private float _lastUpdateTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            _lastUpdateTime = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record && rewind.Mode != RewindMode.CatchUp)
            {
                return;
            }

            var tickTime = SystemAPI.GetSingleton<TickTimeState>();
            if (tickTime.IsPaused)
            {
                return;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            // Emotion-driven learning modulates action selection in ProceduralLearningSystem
            // This system provides helper functions for emotion-based behavior modification
        }

        /// <summary>
        /// Compute exploration probability based on curiosity level.
        /// Higher curiosity increases exploration.
        /// </summary>
        [BurstCompile]
        public static float ComputeExplorationProbability(float curiosity, float baseProbability = 0.1f)
        {
            return math.clamp(baseProbability + curiosity * 0.3f, 0f, 1f);
        }

        /// <summary>
        /// Check if context should be avoided based on fear level.
        /// Higher fear causes avoidance of high-failure contexts.
        /// </summary>
        [BurstCompile]
        public static bool ShouldAvoidContext(float fear, float contextFailureRate, float threshold = 0.5f)
        {
            float avoidanceThreshold = threshold - fear * 0.3f; // Lower threshold with higher fear
            return contextFailureRate > avoidanceThreshold;
        }

        /// <summary>
        /// Check if help-seeking or aggression should be triggered based on frustration.
        /// </summary>
        [BurstCompile]
        public static bool ShouldTriggerHelpSeeking(float frustration, float threshold = 0.7f)
        {
            return frustration > threshold;
        }
    }
}

