using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Culture;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Culture
{
    /// <summary>
    /// Applies cultural doctrine effects to formations.
    /// Modifies attack weights, morale gain/loss, and formation behavior based on archetype.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TacticalSystemGroup))]
    public partial struct CulturalDoctrineSystem : ISystem
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

            // Process formations with cultural doctrine
            foreach (var (formationEntity, doctrineRef, groupMorale, bandStats) in SystemAPI
                         .Query<Entity, RefRO<CulturalDoctrineReference>, RefRW<GroupMorale>, RefRO<BandStats>>()
                         .WithEntityAccess())
            {
                if (doctrineRef.ValueRO.Doctrine.IsCreated)
                {
                    ref var doctrine = ref doctrineRef.ValueRO.Doctrine.Value;

                    var morale = groupMorale.ValueRO;
                    var stats = bandStats.ValueRO;

                    // Apply soul harvest bias (converts enemy deaths to focus energy)
                    // This would modify attack weights: AttackWeight += Doctrine.SoulHarvestBias * DeadEnemiesNearby
                    // For now, we'll apply it as a morale modifier
                    var soulHarvestBonus = doctrine.SoulHarvestBias * morale.CasualtyCount * 0.01f;

                    // Apply holy entity proximity bonus
                    // This would require spatial queries to find holy entities
                    // For now, placeholder logic
                    var holyBonus = doctrine.HolyEntityMoraleBonus * 0.1f;

                    // Apply dead enemy attack weight bonus
                    var attackWeightBonus = doctrine.DeadEnemyAttackWeightBonus * morale.CasualtyCount * 0.01f;

                    // Update morale with doctrine modifiers
                    var newMorale = morale.CurrentMorale;
                    newMorale = math.min(1f, newMorale + soulHarvestBonus + holyBonus);
                    newMorale = math.clamp(newMorale, 0f, 1f);

                    groupMorale.ValueRW = new GroupMorale
                    {
                        CurrentMorale = newMorale,
                        CasualtyCount = morale.CasualtyCount,
                        LastCasualtyTick = morale.LastCasualtyTick,
                        LeaderAlive = morale.LeaderAlive,
                        AlliesNearby = morale.AlliesNearby,
                        LastUpdateTick = morale.LastUpdateTick
                    };

                    // Apply deviation multiplier to formation members
                    if (SystemAPI.HasBuffer<BandMember>(formationEntity))
                    {
                        var members = SystemAPI.GetBuffer<BandMember>(formationEntity);
                        for (int i = 0; i < members.Length; i++)
                        {
                            var memberEntity = members[i].Villager;
                            if (memberEntity != Entity.Null && SystemAPI.Exists(memberEntity) &&
                                SystemAPI.HasComponent<FormationMember>(memberEntity))
                            {
                                var member = SystemAPI.GetComponentRW<FormationMember>(memberEntity);
                                var memberValue = member.ValueRO;

                                // Apply deviation multiplier (would affect BehaviorProfile.Chaos)
                                // For now, we store it in a way that FormationDeviationSystem can read
                                // This is a placeholder - actual implementation would modify BehaviorProfile
                            }
                        }
                    }
                }
            }
        }
    }
}

