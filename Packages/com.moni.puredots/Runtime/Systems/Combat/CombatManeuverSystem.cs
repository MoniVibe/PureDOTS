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
                var maneuver = CombatManeuver.None;
                if (xp >= profile.ValueRO.JTurnThreshold)
                {
                    maneuver = CombatManeuver.JTurn;
                }
                else if (xp >= profile.ValueRO.KiteThreshold)
                {
                    maneuver = CombatManeuver.Kite;
                }
                else if (xp >= profile.ValueRO.StrafeThreshold)
                {
                    maneuver = CombatManeuver.Strafe;
                }

                // TODO: integrate maneuver into loop state/commands
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
