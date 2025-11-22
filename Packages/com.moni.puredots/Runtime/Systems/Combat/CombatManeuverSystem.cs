using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CombatLoopSystem))]
    public partial struct CombatManeuverSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PilotExperience>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (experience, profile, loopState) in SystemAPI
                         .Query<RefRO<PilotExperience>, RefRO<VesselManeuverProfile>, RefRW<CombatLoopState>>())
            {
                var xp = experience.ValueRO.Experience;
                // TODO: integrate maneuver into loop state/commands; wake placeholder branch for Burst path
                if (xp >= profile.ValueRO.StrafeThreshold
                    || xp >= profile.ValueRO.KiteThreshold
                    || xp >= profile.ValueRO.JTurnThreshold)
                {
                    // placeholder no-op until maneuver integration lands
                    loopState.ValueRW.PhaseTimer = loopState.ValueRO.PhaseTimer;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
