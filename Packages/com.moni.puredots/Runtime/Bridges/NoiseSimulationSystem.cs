using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Burst-math noise simulation system.
    /// Simulates message loss, jitter, and signal decay for stress-testing.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AgentMappingSystem))]
    public partial struct NoiseSimulationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentSyncState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Simulate noise for entities with CommReliability
            var noiseQuery = state.GetEntityQuery(
                typeof(CommReliability),
                typeof(NoiseProfile));

            if (noiseQuery.IsEmpty)
            {
                return;
            }

            var job = new SimulateNoiseJob
            {
                CurrentTick = tickState.Tick,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.ScheduleParallel(noiseQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct SimulateNoiseJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;

            public void Execute(
                ref CommReliability reliability,
                in NoiseProfile profile)
            {
                // Simulate signal decay over distance
                var decayFactor = 1f - (profile.SignalDecayRate * DeltaTime);
                reliability.SignalDecay = math.clamp(decayFactor, 0f, 1f);

                // Update reliability based on loss rate and signal decay
                reliability.Reliability = (1f - profile.LossRate) * reliability.SignalDecay;

                // Simulate jitter (random variation in latency)
                // In full implementation, would use RNG seeded from RewindState.Seed
                reliability.Jitter = profile.JitterMean + (profile.JitterStdDev * 0.1f); // Simplified

                reliability.LastUpdateTick = CurrentTick;
            }
        }
    }
}

