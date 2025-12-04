using PureDOTS.Runtime.Platform;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Platform
{
    /// <summary>
    /// Checks pilot preferences against tuning state.
    /// Sets refusal flags if below standards. Emits narrative events.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PilotAcceptanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (tuningState, pilotPref, crewMembers, kind, entity) in SystemAPI.Query<RefRO<PlatformTuningState>, RefRO<PlatformPilotPreference>, DynamicBuffer<PlatformCrewMember>, RefRO<PlatformKind>>().WithEntityAccess())
            {
                if ((kind.ValueRO.Flags & (PlatformFlags.Craft | PlatformFlags.Drone)) == 0)
                {
                    continue;
                }

                CheckPilotAcceptance(
                    ref state,
                    ref ecb,
                    entity,
                    in tuningState.ValueRO,
                    in pilotPref.ValueRO,
                    in crewMembers);
            }
        }

        [BurstCompile]
        private static void CheckPilotAcceptance(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity platformEntity,
            in PlatformTuningState tuningState,
            in PlatformPilotPreference pilotPref,
            in DynamicBuffer<PlatformCrewMember> crewMembers)
        {
            var reliabilityBelowMin = tuningState.Reliability < pilotPref.MinReliability;
            var performanceBelowMin = tuningState.PerformanceFactor < pilotPref.MinPerformance;

            if (!reliabilityBelowMin && !performanceBelowMin)
            {
                return;
            }

            var willRefuse = pilotPref.WillFlyIfBelow == 0;
            var grudgingAccept = pilotPref.WillFlyIfBelow == 1;

            if (willRefuse)
            {
                for (int i = 0; i < crewMembers.Length; i++)
                {
                    var crewEntity = crewMembers[i].CrewEntity;
                    if (SystemAPI.Exists(crewEntity) && crewMembers[i].RoleId == 4)
                    {
                        EmitPilotRefusalEvent(ref ecb, crewEntity, platformEntity);
                        break;
                    }
                }
            }
            else if (grudgingAccept)
            {
                for (int i = 0; i < crewMembers.Length; i++)
                {
                    var crewEntity = crewMembers[i].CrewEntity;
                    if (SystemAPI.Exists(crewEntity) && crewMembers[i].RoleId == 4)
                    {
                        EmitGrudgingAcceptanceEvent(ref ecb, crewEntity, platformEntity);
                        break;
                    }
                }
            }
        }

        [BurstCompile]
        private static void EmitPilotRefusalEvent(ref EntityCommandBuffer ecb, Entity pilotEntity, Entity craftEntity)
        {
        }

        [BurstCompile]
        private static void EmitGrudgingAcceptanceEvent(ref EntityCommandBuffer ecb, Entity pilotEntity, Entity craftEntity)
        {
        }
    }
}

