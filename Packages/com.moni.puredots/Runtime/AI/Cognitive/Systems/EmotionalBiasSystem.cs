using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.Cognitive.Systems
{
    /// <summary>
    /// Applies Wisdom-based damping to emotional influence in decision-making.
    /// Formula: EmotionInfluence *= 1 - Wisdom * 0.05f
    /// Higher Wisdom = less impulsive, more contextual decision-making.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MotivationSystemGroup))]
    public partial struct EmotionalBiasSystem : ISystem
    {
        private const float UpdateInterval = 0.2f; // 5Hz emotion updates (matches MotivationSystemGroup frequency)
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

            var job = new EmotionalBiasJob
            {
                CurrentTick = tickTime.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct EmotionalBiasJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref LimbicState limbicState,
                in CognitiveStats cognitiveStats)
            {
                // Apply Wisdom-based damping to emotional influence
                // Formula: EmotionInfluence *= 1 - Wisdom * 0.05f
                float wisdomNorm = CognitiveStats.Normalize(cognitiveStats.Wisdom);
                float dampingFactor = 1f - wisdomNorm * 0.05f;
                dampingFactor = math.max(0f, dampingFactor); // Clamp to prevent negative

                // Apply damping to emotional state values
                // Higher Wisdom reduces the impact of fear, frustration, and increases contextual weighting
                limbicState.Fear *= dampingFactor;
                limbicState.Frustration *= dampingFactor;
                
                // Curiosity is less affected by Wisdom (it's more about exploration)
                // But we can apply slight damping to prevent over-exploration
                limbicState.Curiosity = math.min(limbicState.Curiosity, 1f - wisdomNorm * 0.1f);

                // Update last emotion update tick
                limbicState.LastEmotionUpdateTick = CurrentTick;
            }
        }

        /// <summary>
        /// Calculates emotional bias damping factor based on Wisdom.
        /// Formula: dampingFactor = 1 - Wisdom * 0.05f
        /// </summary>
        [BurstCompile]
        public static float CalculateEmotionalDamping(in CognitiveStats cognitiveStats)
        {
            float wisdomNorm = CognitiveStats.Normalize(cognitiveStats.Wisdom);
            return math.max(0f, 1f - wisdomNorm * 0.05f);
        }

        /// <summary>
        /// Applies emotional bias damping to a utility score.
        /// Higher Wisdom reduces emotional influence on decisions.
        /// </summary>
        [BurstCompile]
        public static float ApplyEmotionalBiasDamping(float utilityScore, float emotionBias, in CognitiveStats cognitiveStats)
        {
            float dampingFactor = CalculateEmotionalDamping(in cognitiveStats);
            float dampedEmotionBias = emotionBias * dampingFactor;
            return utilityScore * (1f + dampedEmotionBias);
        }
    }
}

