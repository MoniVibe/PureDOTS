using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Dynasty;

namespace PureDOTS.Systems.Dynasty
{
    /// <summary>
    /// Maintains a legacy roster of all dynasty members (alive or dead).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DynastyLineageTrackingSystem))]
    public partial struct DynastyLegacyLedgerSystem : ISystem
    {
        private ComponentLookup<DynastyMember> _dynastyMemberLookup;
        private ComponentLookup<DeathState> _deathStateLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _dynastyMemberLookup = state.GetComponentLookup<DynastyMember>(true);
            _deathStateLookup = state.GetComponentLookup<DeathState>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            _dynastyMemberLookup.Update(ref state);
            _deathStateLookup.Update(ref state);

            var em = state.EntityManager;

            foreach (var (identity, lineage, dynastyEntity) in SystemAPI
                         .Query<RefRO<DynastyIdentity>, DynamicBuffer<DynastyLineage>>()
                         .WithEntityAccess())
            {
                if (!em.HasBuffer<DynastyLegacyEntry>(dynastyEntity))
                {
                    em.AddBuffer<DynastyLegacyEntry>(dynastyEntity);
                    continue;
                }

                var legacy = em.GetBuffer<DynastyLegacyEntry>(dynastyEntity);

                for (int i = 0; i < lineage.Length; i++)
                {
                    var lineageEntry = lineage[i];
                    if (lineageEntry.MemberEntity == Entity.Null)
                    {
                        continue;
                    }

                    var member = lineageEntry.MemberEntity;
                    bool isDead = _deathStateLookup.HasComponent(member) && _deathStateLookup[member].IsDead;
                    uint deathTick = 0;
                    if (isDead)
                    {
                        deathTick = _deathStateLookup[member].DeathTick;
                    }

                    int index = FindLegacyIndex(legacy, member);
                    if (index >= 0)
                    {
                        var entry = legacy[index];
                        entry.Generation = lineageEntry.Generation;
                        entry.IsDead = (byte)(isDead ? 1 : 0);
                        entry.BirthTick = lineageEntry.BirthTick;
                        if (isDead && entry.DeathTick == 0)
                        {
                            entry.DeathTick = deathTick;
                        }

                        ResolveRankAndStrength(identity.ValueRO.FounderEntity, member, entry.Rank, entry.LineageStrength, out var rank, out var lineageStrength);
                        entry.Rank = rank;
                        entry.LineageStrength = lineageStrength;

                        legacy[index] = entry;
                        continue;
                    }

                    ResolveRankAndStrength(identity.ValueRO.FounderEntity, member, DynastyRank.Member, 0.5f, out var resolvedRank, out var resolvedStrength);
                    legacy.Add(new DynastyLegacyEntry
                    {
                        MemberEntity = member,
                        Rank = resolvedRank,
                        LineageStrength = resolvedStrength,
                        Generation = lineageEntry.Generation,
                        IsDead = (byte)(isDead ? 1 : 0),
                        BirthTick = lineageEntry.BirthTick,
                        DeathTick = deathTick
                    });
                }
            }
        }

        private static int FindLegacyIndex(DynamicBuffer<DynastyLegacyEntry> legacy, Entity member)
        {
            for (int i = 0; i < legacy.Length; i++)
            {
                if (legacy[i].MemberEntity == member)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ResolveRankAndStrength(
            Entity founder,
            Entity member,
            DynastyRank fallbackRank,
            float fallbackStrength,
            out DynastyRank rank,
            out float lineageStrength)
        {
            if (member == founder)
            {
                rank = DynastyRank.Founder;
                lineageStrength = 1f;
                return;
            }

            if (_dynastyMemberLookup.HasComponent(member))
            {
                var dynastyMember = _dynastyMemberLookup[member];
                rank = dynastyMember.Rank;
                lineageStrength = dynastyMember.LineageStrength;
                return;
            }

            rank = fallbackRank;
            lineageStrength = fallbackStrength;
        }
    }
}
