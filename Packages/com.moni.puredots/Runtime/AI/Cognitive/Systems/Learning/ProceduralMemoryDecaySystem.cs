using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Learning
{
    /// <summary>
    /// Applies Wisdom-based decay to procedural memory success scores.
    /// Formula: memory.Value *= 1 - (0.01f / Wisdom)
    /// Higher Wisdom = slower decay (better retention).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    [UpdateAfter(typeof(ProceduralMemoryReinforcementSystem))]
    public partial struct ProceduralMemoryDecaySystem : ISystem
    {
        private const float DecayUpdateInterval = 1.0f; // 1Hz decay updates
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
            if (currentTime - _lastUpdateTime < DecayUpdateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            var job = new ProceduralMemoryDecayJob
            {
                CurrentTick = tickTime.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ProceduralMemoryDecayJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref ProceduralMemory memory,
                in CognitiveStats cognitiveStats)
            {
                // Skip if no memory entries
                if (memory.TriedActions.Length == 0 || memory.SuccessScores.Length == 0)
                {
                    return;
                }

                // Calculate decay rate based on Wisdom: memory.Value *= 1 - (0.01f / Wisdom)
                // Higher Wisdom = slower decay (better retention)
                float wisdomNorm = CognitiveStats.Normalize(cognitiveStats.Wisdom);
                
                // Prevent division by zero and ensure minimum decay
                float wisdomForDecay = math.max(0.1f, wisdomNorm * 10f); // Convert back to 0-10 range, clamp minimum
                float decayRate = 0.01f / wisdomForDecay;
                float retentionFactor = 1f - decayRate;

                // Apply decay to all success scores
                for (int i = 0; i < memory.SuccessScores.Length; i++)
                {
                    float oldScore = memory.SuccessScores[i];
                    float newScore = oldScore * retentionFactor;
                    memory.SuccessScores[i] = math.max(0f, newScore); // Clamp to prevent negative
                }
            }
        }
    }
}

