using PureDOTS.Runtime.Groups;
using PureDOTS.Runtime.Time;
using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Biases formation and targeting heuristics toward successful doctrines.
    /// Adapt formation and targeting heuristics based on learned tactics.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(FleetCommandSystem))]
    public partial struct FormationAdaptationSystem : ISystem
    {
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

            var job = new AdaptFormationJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct AdaptFormationJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                in LeaderTag leaderTag,
                in FleetCommandState commandState,
                ref GroupFormation formation,
                ref GroupStanceState stance)
            {
                if (commandState.Tactics.IsCreated)
                {
                    // Formation adaptation logic placeholder.
                }
            }
        }
    }
}
