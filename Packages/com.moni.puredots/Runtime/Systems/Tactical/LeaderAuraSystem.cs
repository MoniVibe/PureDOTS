using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Morale;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Tactical
{
    /// <summary>
    /// Applies leader influence field (CommandAura) effects to formation members.
    /// Updates cohesion and morale based on proximity to leader.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TacticalSystemGroup))]
    public partial struct LeaderAuraSystem : ISystem
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
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var deltaTime = timeState.FixedDeltaTime;

            // Process leaders with CommandAura
            foreach (var (leaderEntity, aura, leaderTransform, bandId) in SystemAPI
                         .Query<Entity, RefRO<CommandAura>, RefRO<LocalTransform>, RefRO<BandId>>()
                         .WithEntityAccess())
            {
                var auraValue = aura.ValueRO;
                var leaderPos = leaderTransform.ValueRO.Position;

                // Find formation entity (leader's band)
                var formationEntity = Entity.Null;
                if (bandId.ValueRO.Leader == leaderEntity)
                {
                    // Leader is part of a band - find the band entity
                    // This is a simplified lookup - in practice would use registry or component links
                    foreach (var (entity, id) in SystemAPI.Query<Entity, RefRO<BandId>>().WithEntityAccess())
                    {
                        if (id.ValueRO.Leader == leaderEntity)
                        {
                            formationEntity = entity;
                            break;
                        }
                    }
                }

                if (formationEntity == Entity.Null || !SystemAPI.Exists(formationEntity))
                {
                    continue;
                }

                // Process members within aura radius
                if (SystemAPI.HasBuffer<BandMember>(formationEntity))
                {
                    var members = SystemAPI.GetBuffer<BandMember>(formationEntity);
                    var cohesionSum = 0f;
                    var moraleSum = 0f;
                    var memberCount = 0;

                    for (int i = 0; i < members.Length; i++)
                    {
                        var memberEntity = members[i].Villager;
                        if (memberEntity == Entity.Null || !SystemAPI.Exists(memberEntity) ||
                            !SystemAPI.HasComponent<LocalTransform>(memberEntity))
                        {
                            continue;
                        }

                        var memberTransform = SystemAPI.GetComponent<LocalTransform>(memberEntity);
                        var distance = math.distance(leaderPos, memberTransform.Position);

                        if (distance <= auraValue.Radius)
                        {
                            memberCount++;

                            // Apply cohesion bonus: member.Morale += CohesionBonus * Discipline
                            if (SystemAPI.HasComponent<BehaviorProfile>(memberEntity))
                            {
                                var profile = SystemAPI.GetComponent<BehaviorProfile>(memberEntity);
                                var cohesionBonus = auraValue.CohesionBonus * profile.Discipline * deltaTime;

                                // Update FormationMember alignment (which affects cohesion)
                                if (SystemAPI.HasComponent<FormationMember>(memberEntity))
                                {
                                    var member = SystemAPI.GetComponentRW<FormationMember>(memberEntity);
                                    var memberValue = member.ValueRO;
                                    member.ValueRW = new FormationMember
                                    {
                                        FormationEntity = memberValue.FormationEntity,
                                        Offset = memberValue.Offset,
                                        Alignment = math.min(1f, memberValue.Alignment + cohesionBonus)
                                    };
                                }

                                // Apply morale bonus
                                var moraleBonus = auraValue.MoraleBonus * profile.Discipline * deltaTime;
                                if (SystemAPI.HasComponent<GroupMorale>(formationEntity))
                                {
                                    var groupMorale = SystemAPI.GetComponentRW<GroupMorale>(formationEntity);
                                    var morale = groupMorale.ValueRO;
                                    groupMorale.ValueRW = new GroupMorale
                                    {
                                        CurrentMorale = math.min(1f, morale.CurrentMorale + moraleBonus),
                                        CasualtyCount = morale.CasualtyCount,
                                        LastCasualtyTick = morale.LastCasualtyTick,
                                        LeaderAlive = morale.LeaderAlive,
                                        AlliesNearby = morale.AlliesNearby,
                                        LastUpdateTick = morale.LastUpdateTick
                                    };
                                }

                                cohesionSum += math.min(1f, memberValue.Alignment + cohesionBonus);
                            }
                            else
                            {
                                // Default cohesion if no BehaviorProfile
                                cohesionSum += 0.5f;
                            }
                        }
                    }

                    // Update formation cohesion (average of member alignments)
                    if (memberCount > 0 && SystemAPI.HasComponent<BandFormation>(formationEntity))
                    {
                        var formation = SystemAPI.GetComponentRW<BandFormation>(formationEntity);
                        var formationValue = formation.ValueRO;
                        var averageCohesion = cohesionSum / memberCount;

                        formation.ValueRW = new BandFormation
                        {
                            Formation = formationValue.Formation,
                            Spacing = formationValue.Spacing,
                            Width = formationValue.Width,
                            Depth = formationValue.Depth,
                            Facing = formationValue.Facing,
                            Anchor = formationValue.Anchor,
                            Stability = formationValue.Stability,
                            LastSolveTick = formationValue.LastSolveTick,
                            Cohesion = averageCohesion,
                            Morale = formationValue.Morale,
                            FormationId = formationValue.FormationId
                        };
                    }
                }
            }
        }
    }
}

