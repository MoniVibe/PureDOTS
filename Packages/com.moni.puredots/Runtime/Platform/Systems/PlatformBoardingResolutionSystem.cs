using PureDOTS.Runtime.Platform;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Platform
{
    /// <summary>
    /// Resolves boarding combat per segment.
    /// Adjusts control levels based on team combat resolution.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlatformBoardingResolutionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (boardingState, boardingTeams, segmentControls, segmentStates, entity) in SystemAPI.Query<
                RefRW<BoardingState>,
                DynamicBuffer<BoardingTeam>,
                DynamicBuffer<SegmentControl>,
                DynamicBuffer<PlatformSegmentState>>().WithEntityAccess())
            {
                if (boardingState.ValueRO.Phase != BoardingPhase.Fighting)
                {
                    continue;
                }

                ResolveBoardingCombat(
                    ref boardingState.ValueRW,
                    ref boardingTeams,
                    ref segmentControls,
                    ref segmentStates);
            }
        }

        [BurstCompile]
        private static void ResolveBoardingCombat(
            ref BoardingState boardingState,
            ref DynamicBuffer<BoardingTeam> boardingTeams,
            ref DynamicBuffer<SegmentControl> segmentControls,
            ref DynamicBuffer<PlatformSegmentState> segmentStates)
        {
            var attackerFactionId = boardingState.AttackerFactionId;
            var defenderFactionId = boardingState.DefenderFactionId;

            var segmentsByIndex = new NativeHashMap<int, int>(segmentStates.Length, Allocator.Temp);

            for (int i = 0; i < segmentStates.Length; i++)
            {
                var segmentState = segmentStates[i];
                if ((segmentState.Status & SegmentStatusFlags.Destroyed) != 0)
                {
                    continue;
                }

                segmentsByIndex[segmentState.SegmentIndex] = i;
            }

            for (int i = 0; i < segmentControls.Length; i++)
            {
                var segmentControl = segmentControls[i];
                var segmentIndex = segmentControl.SegmentIndex;

                if (!segmentsByIndex.TryGetValue(segmentIndex, out var segmentStateIndex))
                {
                    continue;
                }

                var segmentState = segmentStates[segmentStateIndex];
                var environmentPenalty = GetEnvironmentPenalty(segmentState.Status);

                var attackerStrength = 0f;
                var defenderStrength = 0f;

                for (int j = 0; j < boardingTeams.Length; j++)
                {
                    var team = boardingTeams[j];
                    if (team.SegmentIndex != segmentIndex)
                    {
                        continue;
                    }

                    var strength = team.Count * team.Morale;

                    if (team.FactionId == attackerFactionId)
                    {
                        attackerStrength += strength;
                    }
                    else if (team.FactionId == defenderFactionId)
                    {
                        defenderStrength += strength;
                    }
                }

                defenderStrength *= (1f - environmentPenalty);

                var totalStrength = attackerStrength + defenderStrength;
                if (totalStrength > 0f)
                {
                    var attackerRatio = attackerStrength / totalStrength;
                    segmentControl.ControlLevel = math.lerp(segmentControl.ControlLevel, attackerRatio * 2f - 1f, 0.1f);
                }

                if (segmentControl.ControlLevel > 0.5f)
                {
                    segmentControl.FactionId = attackerFactionId;
                    segmentState.ControlFactionId = attackerFactionId;
                    segmentState.Status |= SegmentStatusFlags.Boarded;
                }
                else if (segmentControl.ControlLevel < -0.5f)
                {
                    segmentControl.FactionId = defenderFactionId;
                    segmentState.ControlFactionId = defenderFactionId;
                    segmentState.Status &= ~SegmentStatusFlags.Boarded;
                }

                segmentControls[i] = segmentControl;
                segmentStates[segmentStateIndex] = segmentState;
            }
        }

        [BurstCompile]
        private static float GetEnvironmentPenalty(SegmentStatusFlags status)
        {
            float penalty = 0f;

            if ((status & SegmentStatusFlags.Breached) != 0)
            {
                penalty += 0.2f;
            }

            if ((status & SegmentStatusFlags.Depressurized) != 0)
            {
                penalty += 0.3f;
            }

            if ((status & SegmentStatusFlags.OnFire) != 0)
            {
                penalty += 0.2f;
            }

            return math.min(penalty, 0.7f);
        }
    }
}

