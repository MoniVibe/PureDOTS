using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Morale;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Morale
{
    /// <summary>
    /// Updates group morale based on casualties, leader status, support proximity, and individual courage/discipline.
    /// Runs at tactical frequency (1-5 Hz) via PeriodicTickComponent throttling.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TacticalSystemGroup))]
    public partial struct GroupMoraleSystem : ISystem
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
            var currentTick = timeState.Tick;
            var deltaTime = timeState.FixedDeltaTime;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Process formations with GroupMorale (only if dirty or periodic update)
            foreach (var (formationEntity, groupMorale, bandStats, bandId, transform) in SystemAPI
                         .Query<Entity, RefRW<GroupMorale>, RefRO<BandStats>, RefRO<BandId>, RefRO<LocalTransform>>()
                         .WithAny<MoraleDirtyTag>()
                         .WithEntityAccess())
            {
                var morale = groupMorale.ValueRO;
                var stats = bandStats.ValueRO;
                var leaderEntity = bandId.ValueRO.Leader;

                // Check if leader is alive
                var leaderAlive = leaderEntity != Entity.Null && SystemAPI.Exists(leaderEntity);

                // Update morale based on leader death
                var newMorale = morale.CurrentMorale;
                if (!leaderAlive && morale.LeaderAlive)
                {
                    // Leader just died: Morale *= 0.7f
                    newMorale *= 0.7f;
                }

                // Apply casualty morale loss: Morale -= Losses * 0.1f
                if (morale.CasualtyCount > 0 && morale.LastCasualtyTick == currentTick - 1)
                {
                    var casualtyLoss = morale.CasualtyCount * 0.1f;
                    newMorale = math.max(0f, newMorale - casualtyLoss);
                }

                // Apply support proximity bonus: Morale += AlliesNearby * 0.02f
                var supportBonus = morale.AlliesNearby * 0.02f;
                newMorale = math.min(1f, newMorale + supportBonus * deltaTime);

                // Apply discipline/courage modifiers from members
                if (SystemAPI.HasBuffer<BandMember>(formationEntity))
                {
                    var members = SystemAPI.GetBuffer<BandMember>(formationEntity);
                    var averageDiscipline = 0f;
                    var averageCourage = 0f;
                    var memberCount = 0;

                    for (int i = 0; i < members.Length; i++)
                    {
                        var memberEntity = members[i].Villager;
                        if (memberEntity == Entity.Null || !SystemAPI.Exists(memberEntity))
                        {
                            continue;
                        }

                        if (SystemAPI.HasComponent<BehaviorProfile>(memberEntity))
                        {
                            var profile = SystemAPI.GetComponent<BehaviorProfile>(memberEntity);
                            averageDiscipline += profile.Discipline;
                            averageCourage += profile.Courage;
                            memberCount++;
                        }
                    }

                    if (memberCount > 0)
                    {
                        averageDiscipline /= memberCount;
                        averageCourage /= memberCount;

                        // Discipline reduces morale loss: MoraleLoss *= 1 - Discipline
                        // Courage increases morale gain: MoraleGain *= Courage
                        var disciplineModifier = 1f - averageDiscipline;
                        var courageModifier = averageCourage;

                        // Apply modifiers to morale changes
                        var moraleChange = (newMorale - morale.CurrentMorale);
                        if (moraleChange < 0f)
                        {
                            // Loss: reduce by discipline
                            moraleChange *= disciplineModifier;
                        }
                        else
                        {
                            // Gain: boost by courage
                            moraleChange *= (1f + courageModifier);
                        }

                        newMorale = morale.CurrentMorale + moraleChange;
                    }
                }

                newMorale = math.clamp(newMorale, 0f, 1f);

                // Update GroupMorale
                groupMorale.ValueRW = new GroupMorale
                {
                    CurrentMorale = newMorale,
                    CasualtyCount = morale.CasualtyCount,
                    LastCasualtyTick = morale.LastCasualtyTick,
                    LeaderAlive = leaderAlive,
                    AlliesNearby = morale.AlliesNearby,
                    LastUpdateTick = currentTick
                };

                // Update BandFormation morale
                if (SystemAPI.HasComponent<BandFormation>(formationEntity))
                {
                    var formation = SystemAPI.GetComponentRW<BandFormation>(formationEntity);
                    var formationValue = formation.ValueRO;
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
                        Cohesion = formationValue.Cohesion,
                        Morale = newMorale,
                        FormationId = formationValue.FormationId
                    };
                }

                // Set RoutState if morale < 0.3
                if (newMorale < 0.3f)
                {
                    if (!SystemAPI.HasComponent<RoutState>(formationEntity))
                    {
                        ecb.AddComponent(formationEntity, new RoutState
                        {
                            MoraleAtRout = newMorale,
                            RoutStartTick = currentTick
                        });
                    }
                }
                else
                {
                    // Remove rout state if morale recovered
                    if (SystemAPI.HasComponent<RoutState>(formationEntity))
                    {
                        ecb.RemoveComponent<RoutState>(formationEntity);
                    }
                }

                // Apply rout state to individual members
                if (SystemAPI.HasBuffer<BandMember>(formationEntity))
                {
                    var members = SystemAPI.GetBuffer<BandMember>(formationEntity);
                    var hasRout = newMorale < 0.3f;

                    for (int i = 0; i < members.Length; i++)
                    {
                        var memberEntity = members[i].Villager;
                        if (memberEntity == Entity.Null || !SystemAPI.Exists(memberEntity))
                        {
                            continue;
                        }

                        if (hasRout)
                        {
                            if (!SystemAPI.HasComponent<RoutState>(memberEntity))
                            {
                                ecb.AddComponent(memberEntity, new RoutState
                                {
                                    MoraleAtRout = newMorale,
                                    RoutStartTick = currentTick
                                });
                            }
                        }
                        else
                        {
                            if (SystemAPI.HasComponent<RoutState>(memberEntity))
                            {
                                ecb.RemoveComponent<RoutState>(memberEntity);
                            }
                        }
                    }
                }

                // Remove dirty tag after processing
                ecb.RemoveComponent<MoraleDirtyTag>(formationEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

