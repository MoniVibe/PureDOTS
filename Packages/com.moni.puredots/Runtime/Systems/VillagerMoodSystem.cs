using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using PureDOTS.Runtime.Villager;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Computes villager morale and alignment shifts based on needs, miracles, and creature actions.
    /// Runs after VillagerNeedsSystem and VillagerStatusSystem to compute mood from wellbeing.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup))]
    [UpdateAfter(typeof(VillagerStatusSystem))]
    public partial struct VillagerMoodSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Get villager behavior config or use defaults
            var config = SystemAPI.HasSingleton<VillagerBehaviorConfig>()
                ? SystemAPI.GetSingleton<VillagerBehaviorConfig>()
                : VillagerBehaviorConfig.CreateDefaults();

            var job = new UpdateMoodJob
            {
                DeltaTime = timeState.FixedDeltaTime,
                CurrentTick = timeState.Tick,
                MoodLerpRate = config.MoraleLerpRate,
                AlignmentInfluenceRate = 0.1f, // TODO: Move to config
                MiracleAlignmentBonus = 5f,    // TODO: Move to config
                CreatureAlignmentBonus = 2f     // TODO: Move to config
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct UpdateMoodJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;
            public float MoodLerpRate;
            public float AlignmentInfluenceRate;
            public float MiracleAlignmentBonus;
            public float CreatureAlignmentBonus;

            public void Execute(ref VillagerMood mood, in VillagerNeeds needs, in VillagerAvailability availability)
            {
                // Mood is primarily driven by wellbeing (computed in VillagerStatusSystem)
                // Here we handle alignment shifts and external influences

                // Lerp mood toward target (set by VillagerStatusSystem based on wellbeing)
                var adjust = math.clamp(DeltaTime * mood.MoodChangeRate, 0f, 1f);
                mood.Mood = math.lerp(mood.Mood, mood.TargetMood, adjust);

                // TODO: Add alignment shift logic based on:
                // - Miracles performed nearby (check MiracleEffectRegistry)
                // - Creature actions (check creature command events)
                // - Player interactions (check hand interaction events)
                // For now, mood is driven solely by wellbeing/needs
            }
        }
    }
}

