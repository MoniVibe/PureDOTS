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
    /// Limbic modulation system - 0.2Hz motivation layer.
    /// Updates emotion from reward feedback and modulates cognitive planning weights.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MotivationSystemGroup))]
    public partial struct LimbicModulationSystem : ISystem
    {
        private const float UpdateInterval = 5.0f; // 0.2Hz
        private const float StabilityThreshold = 0.1f; // Success rate variance considered "stable"
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

            var job = new LimbicModulationJob
            {
                CurrentTick = tickTime.Tick,
                StabilityThreshold = StabilityThreshold
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct LimbicModulationJob : IJobEntity
        {
            public uint CurrentTick;
            public float StabilityThreshold;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                in ProceduralMemory memory,
                ref LimbicState limbic)
            {
                // Update emotion from reward feedback
                bool successRateStable = math.abs(limbic.RecentSuccessRate - 0.5f) < StabilityThreshold;

                // Curiosity: increases when success rate is unstable (novel situations)
                if (successRateStable)
                {
                    limbic.Curiosity = math.max(0f, limbic.Curiosity - 0.01f);
                }
                else
                {
                    limbic.Curiosity = math.min(1f, limbic.Curiosity + 0.05f);
                }

                // Fear: increases with recent failures
                if (limbic.RecentFailures > 0)
                {
                    limbic.Fear = math.min(1f, limbic.Fear + 0.1f);
                }
                else
                {
                    limbic.Fear = math.max(0f, limbic.Fear - 0.02f);
                }

                // Frustration: increases with repeated failures
                const int frustrationThreshold = 3;
                if (limbic.RecentFailures > frustrationThreshold)
                {
                    limbic.Frustration = math.min(1f, limbic.Frustration + 0.1f);
                }
                else
                {
                    limbic.Frustration = math.max(0f, limbic.Frustration - 0.02f);
                }

                limbic.LastEmotionUpdateTick = CurrentTick;
            }
        }
    }
}

