using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.AI.Social.Systems
{
    /// <summary>
    /// Motivation system for Body ECS.
    /// Updates Motivation component: morale, hope, pressure.
    /// Morale rises from success, decays with unmet needs or broken trust.
    /// Pressure drives individuals toward conformity or rebellion.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    [BurstCompile]
    public partial struct MotivationSystem : ISystem
    {
        private const float MoraleDecayRate = 0.001f; // Per tick decay
        private const float MoraleSuccessGain = 0.05f; // Gain from successful cooperation
        private const float PressureThreshold = 0.7f; // Threshold for revolt
        private const float CourageThreshold = 0.8f; // Courage required for revolt

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return; // Skip during playback
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var tickNumber = tickState.Tick;

            // Process motivation updates in Burst job
            var job = new UpdateMotivationJob
            {
                TickNumber = tickNumber,
                MoraleDecayRate = MoraleDecayRate,
                MoraleSuccessGain = MoraleSuccessGain,
                PressureThreshold = PressureThreshold,
                CourageThreshold = CourageThreshold
            };

            var entityQuery = state.GetEntityQuery(typeof(Motivation));
            job.ScheduleParallel(entityQuery, Dependency).Complete();
        }
    }

    [BurstCompile]
    private partial struct UpdateMotivationJob : IJobEntity
    {
        public uint TickNumber;
        public float MoraleDecayRate;
        public float MoraleSuccessGain;
        public float PressureThreshold;
        public float CourageThreshold;

        public void Execute(ref Motivation motivation)
        {
            // Skip if already updated this tick
            if (motivation.LastUpdateTick == TickNumber)
            {
                return;
            }

            // Decay morale over time
            motivation.Morale = math.max(0f, motivation.Morale - MoraleDecayRate);

            // Update hope (simplified - would be based on future expectations)
            motivation.Hope = math.lerp(motivation.Hope, motivation.Morale, 0.01f);

            // Check for revolt condition: Pressure > threshold && Courage > threshold
            if (motivation.Pressure > PressureThreshold && motivation.Courage > CourageThreshold)
            {
                // Revolt - reset pressure, reduce morale
                motivation.Pressure = 0f;
                motivation.Morale = math.max(0f, motivation.Morale - 0.2f);
            }

            motivation.LastUpdateTick = TickNumber;
        }
    }
}

