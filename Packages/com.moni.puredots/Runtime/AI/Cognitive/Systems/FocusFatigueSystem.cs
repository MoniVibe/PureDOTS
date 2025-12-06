using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.Cognitive.Systems
{
    /// <summary>
    /// Manages cognitive Focus fatigue: decays Focus during heavy reasoning, regenerates when idle.
    /// Gates heavy planning operations when Focus drops below threshold.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct FocusFatigueSystem : ISystem
    {
        private const float FocusDecayRate = 0.5f; // Focus decay per second during active reasoning
        private const float FocusRegenRate = 0.2f; // Focus regeneration per second when idle
        private const float FocusThreshold = 2.0f; // Below this, gate heavy planning
        private const float UpdateInterval = 0.1f; // 10Hz updates for smooth fatigue
        private float _lastUpdateTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<TimeState>();
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

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            var job = new FocusFatigueJob
            {
                DeltaTime = timeState.FixedDeltaTime,
                DecayRate = FocusDecayRate,
                RegenRate = FocusRegenRate,
                Threshold = FocusThreshold,
                CurrentTick = tickTime.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct FocusFatigueJob : IJobEntity
        {
            public float DeltaTime;
            public float DecayRate;
            public float RegenRate;
            public float Threshold;
            public uint CurrentTick;

            public void Execute(
                ref CognitiveStats cognitiveStats,
                ref ProceduralMemory memory)
            {
                // Check if agent is actively reasoning (has recent memory updates)
                bool isActivelyReasoning = (CurrentTick - memory.LastUpdateTick) < 10; // Active if updated in last 10 ticks

                if (isActivelyReasoning)
                {
                    // Decay Focus during active reasoning: Focus = max(0, Focus - DecayRate * DeltaTime)
                    cognitiveStats.Focus = math.max(0f, cognitiveStats.Focus - DecayRate * DeltaTime);
                }
                else
                {
                    // Regenerate Focus when idle: Focus = min(MaxFocus, Focus + RegenRate * DeltaTime)
                    cognitiveStats.Focus = math.min(cognitiveStats.MaxFocus, cognitiveStats.Focus + RegenRate * DeltaTime);
                }

                cognitiveStats.LastFocusDecayTick = CurrentTick;
            }
        }

        /// <summary>
        /// Checks if agent has sufficient Focus for heavy planning operations.
        /// </summary>
        [BurstCompile]
        public static bool CanPerformHeavyPlanning(in CognitiveStats cognitiveStats)
        {
            return cognitiveStats.Focus >= FocusThreshold;
        }
    }
}

