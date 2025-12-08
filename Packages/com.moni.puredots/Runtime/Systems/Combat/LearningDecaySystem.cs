using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Systems;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Applies periodic decay to maintain diversity of tactics.
    /// Periodic decay keeps diversity of tactics.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(CombatLearningSystem))]
    public partial struct LearningDecaySystem : ISystem
    {
        private const uint DecayIntervalTicks = 300; // Decay every 5 seconds at 60Hz

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            if (timeState.IsPaused)
                return;

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var job = new ApplyDecayJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ApplyDecayJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                ref CombatLearningState learningState,
                DynamicBuffer<BehaviorSuccessRate> successRates)
            {
                // Check if it's time for decay
                if (CurrentTick - learningState.LastDecayTick < DecayIntervalTicks)
                    return;

                learningState.LastDecayTick = CurrentTick;

                // Apply decay to all behavior weights
                for (int i = 0; i < successRates.Length; i++)
                {
                    var rate = successRates[i];
                    
                    // Decay weight toward base (1.0)
                    rate.Weight = math.lerp(rate.Weight, 1f, learningState.DecayRate);
                    
                    // Also decay success counts slightly to favor recent performance
                    if (rate.AttemptCount > 10)
                    {
                        rate.SuccessCount = (uint)(rate.SuccessCount * 0.95f);
                        rate.AttemptCount = (uint)(rate.AttemptCount * 0.95f);
                    }

                    successRates[i] = rate;
                }
            }
        }
    }
}

