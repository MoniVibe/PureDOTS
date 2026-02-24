using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Dynasty;
using PureDOTS.Runtime.Family;
using PureDOTS.Runtime.Lifecycle;
using PureDOTS.Runtime.Social;

namespace PureDOTS.Systems.Family
{
    /// <summary>
    /// Processes marriage and adoption events, updating family/dynasty membership and relations.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct FamilyBondEventSystem : ISystem
    {
        private EntityQuery _marriageQuery;
        private EntityQuery _adoptionQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _marriageQuery = SystemAPI.QueryBuilder().WithAll<MarriageEvent>().Build();
            _adoptionQuery = SystemAPI.QueryBuilder().WithAll<AdoptionEvent>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_marriageQuery.IsEmptyIgnoreFilter && _adoptionQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            state.Dependency.Complete();
            var em = state.EntityManager;
            uint currentTick = timeState.Tick;

            if (!_marriageQuery.IsEmptyIgnoreFilter)
            {
                using var marriageEntities = _marriageQuery.ToEntityArray(Allocator.Temp);
                using var marriageEvents = _marriageQuery.ToComponentDataArray<MarriageEvent>(Allocator.Temp);

                for (int i = 0; i < marriageEntities.Length; i++)
                {
                    HandleMarriage(em, marriageEvents[i], currentTick);
                    if (em.Exists(marriageEntities[i]))
                    {
                        em.DestroyEntity(marriageEntities[i]);
                    }
                }
            }

            if (!_adoptionQuery.IsEmptyIgnoreFilter)
            {
                using var adoptionEntities = _adoptionQuery.ToEntityArray(Allocator.Temp);
                using var adoptionEvents = _adoptionQuery.ToComponentDataArray<AdoptionEvent>(Allocator.Temp);

                for (int i = 0; i < adoptionEntities.Length; i++)
                {
                    HandleAdoption(em, adoptionEvents[i], currentTick);
                    if (em.Exists(adoptionEntities[i]))
                    {
                        em.DestroyEntity(adoptionEntities[i]);
                    }
                }
            }
        }

        private static void HandleMarriage(EntityManager em, in MarriageEvent marriageEvent, uint currentTick)
        {
            if (marriageEvent.PartnerA == Entity.Null || marriageEvent.PartnerB == Entity.Null)
            {
                return;
            }

            if (!em.Exists(marriageEvent.PartnerA) || !em.Exists(marriageEvent.PartnerB))
            {
                return;
            }

            EnsureSpouseRelation(em, marriageEvent.PartnerA, marriageEvent.PartnerB, currentTick);

            Entity familyA = GetFamily(em, marriageEvent.PartnerA);
            Entity familyB = GetFamily(em, marriageEvent.PartnerB);
            if (familyA != Entity.Null && familyB != Entity.Null && familyA != familyB)
            {
                EnsureHouseRelation(em, familyA, familyB, currentTick);
            }

            Entity dynastyA = GetDynasty(em, marriageEvent.PartnerA);
            Entity dynastyB = GetDynasty(em, marriageEvent.PartnerB);
            if (dynastyA != Entity.Null && dynastyB != Entity.Null && dynastyA != dynastyB)
            {
                EnsureDynastyRelation(em, dynastyA, dynastyB, currentTick);
            }

            switch (marriageEvent.JoinPolicy)
            {
                case MarriageJoinPolicy.JoinPartnerA:
                    JoinFamilyForMarriage(em, marriageEvent.PartnerB, marriageEvent.PartnerA, familyA, currentTick);
                    JoinDynastyForMarriage(em, marriageEvent.PartnerB, marriageEvent.PartnerA, dynastyA, currentTick);
                    break;
                case MarriageJoinPolicy.JoinPartnerB:
                    JoinFamilyForMarriage(em, marriageEvent.PartnerA, marriageEvent.PartnerB, familyB, currentTick);
                    JoinDynastyForMarriage(em, marriageEvent.PartnerA, marriageEvent.PartnerB, dynastyB, currentTick);
                    break;
                case MarriageJoinPolicy.CreateNewFamily:
                    CreateFamilyForMarriage(em, marriageEvent.PartnerA, marriageEvent.PartnerB, currentTick);
                    break;
                case MarriageJoinPolicy.KeepSeparate:
                default:
                    break;
            }
        }

        private static void HandleAdoption(EntityManager em, in AdoptionEvent adoptionEvent, uint currentTick)
        {
            if (adoptionEvent.Parent == Entity.Null || adoptionEvent.Child == Entity.Null)
            {
                return;
            }

            if (!em.Exists(adoptionEvent.Parent) || !em.Exists(adoptionEvent.Child))
            {
                return;
            }

            EnsureParentChildRelation(em, adoptionEvent.Parent, adoptionEvent.Child, currentTick);

            Entity parentFamily = GetFamily(em, adoptionEvent.Parent);
            if (parentFamily == Entity.Null)
            {
                parentFamily = CreateFamilyImmediate(em, adoptionEvent.Parent, ResolveFamilyName(em, adoptionEvent.Parent, adoptionEvent.Child), currentTick);
            }

            MoveToFamily(em, adoptionEvent.Child, parentFamily, FamilyRole.Child, currentTick, adoptionEvent.Parent, Entity.Null);

            if (adoptionEvent.JoinDynasty != 0)
            {
                Entity parentDynasty = GetDynasty(em, adoptionEvent.Parent);
                if (parentDynasty != Entity.Null)
                {
                    JoinDynastyByAdoption(em, adoptionEvent.Child, adoptionEvent.Parent, parentDynasty, currentTick);
                }
            }
        }

        private static void JoinFamilyForMarriage(EntityManager em, Entity joiner, Entity spouse, Entity targetFamily, uint currentTick)
        {
            if (targetFamily == Entity.Null)
            {
                CreateFamilyForMarriage(em, spouse, joiner, currentTick);
                return;
            }

            MoveToFamily(em, joiner, targetFamily, FamilyRole.Spouse, currentTick, Entity.Null, Entity.Null);
        }

        private static void CreateFamilyForMarriage(EntityManager em, Entity partnerA, Entity partnerB, uint currentTick)
        {
            RemoveFamilyMembership(em, partnerA);
            RemoveFamilyMembership(em, partnerB);

            var familyName = ResolveFamilyName(em, partnerA, partnerB);
            var familyEntity = CreateFamilyImmediate(em, partnerA, familyName, currentTick);

            AddOrUpdateFamilyMember(em, familyEntity, partnerB, FamilyRole.Spouse, currentTick);
            EnsureFamilyTreeEntry(em, familyEntity, partnerB, Entity.Null, Entity.Null, ResolveBirthTick(em, partnerB, currentTick));
        }

        private static void JoinDynastyForMarriage(EntityManager em, Entity joiner, Entity spouse, Entity targetDynasty, uint currentTick)
        {
            if (targetDynasty == Entity.Null)
            {
                return;
            }

            JoinDynastyAsSpouse(em, joiner, spouse, targetDynasty, currentTick);
        }

        private static void JoinDynastyAsSpouse(EntityManager em, Entity joiner, Entity spouse, Entity targetDynasty, uint currentTick)
        {
            if (em.HasComponent<DynastyMember>(joiner))
            {
                var existing = em.GetComponentData<DynastyMember>(joiner);
                if (existing.DynastyEntity == targetDynasty)
                {
                    return;
                }

                RemoveDynastyMembership(em, joiner);
            }

            byte spouseGeneration = ResolveDynastyGeneration(em, targetDynasty, spouse);
            var lineageStrength = 0.2f;
            AddOrUpdateDynastyMember(em, targetDynasty, joiner, DynastyRank.Member, lineageStrength, currentTick);
            EnsureDynastyLineageEntry(em, targetDynasty, joiner, Entity.Null, Entity.Null, ResolveBirthTick(em, joiner, currentTick), spouseGeneration);
        }

        private static void JoinDynastyByAdoption(EntityManager em, Entity child, Entity parent, Entity targetDynasty, uint currentTick)
        {
            if (em.HasComponent<DynastyMember>(child))
            {
                var existing = em.GetComponentData<DynastyMember>(child);
                if (existing.DynastyEntity == targetDynasty)
                {
                    return;
                }

                RemoveDynastyMembership(em, child);
            }

            byte parentGeneration = ResolveDynastyGeneration(em, targetDynasty, parent);
            byte childGeneration = (byte)(parentGeneration + 1);

            float parentALineage = GetDynastyLineageStrength(em, parent, targetDynasty);
            float lineageStrength = DynastyService.CalculateLineageStrength(childGeneration, parentALineage, 0f) * 0.6f;

            AddOrUpdateDynastyMember(em, targetDynasty, child, DynastyRank.Member, lineageStrength, currentTick);
            EnsureDynastyLineageEntry(em, targetDynasty, child, parent, Entity.Null, ResolveBirthTick(em, child, currentTick), childGeneration);
        }

        private static void MoveToFamily(
            EntityManager em,
            Entity member,
            Entity targetFamily,
            FamilyRole role,
            uint currentTick,
            Entity parentA,
            Entity parentB)
        {
            if (targetFamily == Entity.Null)
            {
                return;
            }

            if (em.HasComponent<FamilyMember>(member))
            {
                var existing = em.GetComponentData<FamilyMember>(member);
                if (existing.FamilyEntity == targetFamily)
                {
                    AddOrUpdateFamilyMember(em, targetFamily, member, role, currentTick);
                    EnsureFamilyTreeEntry(em, targetFamily, member, parentA, parentB, ResolveBirthTick(em, member, currentTick));
                    return;
                }
            }

            RemoveFamilyMembership(em, member);
            AddOrUpdateFamilyMember(em, targetFamily, member, role, currentTick);

            EnsureFamilyTreeEntry(em, targetFamily, member, parentA, parentB, ResolveBirthTick(em, member, currentTick));
        }

        private static Entity CreateFamilyImmediate(EntityManager em, Entity founder, FixedString64Bytes familyName, uint currentTick)
        {
            var familyEntity = em.CreateEntity();
            em.AddComponentData(familyEntity, new FamilyIdentity
            {
                FamilyName = familyName,
                FounderEntity = founder,
                FoundedTick = currentTick
            });
            em.AddComponentData(familyEntity, new FamilyWealth { LastUpdatedTick = currentTick });
            em.AddComponentData(familyEntity, new FamilyReputation { LastUpdatedTick = currentTick });

            em.AddBuffer<FamilyMemberEntry>(familyEntity);
            em.AddBuffer<FamilyTree>(familyEntity);
            em.AddBuffer<FamilyLegacyEntry>(familyEntity);

            AddOrUpdateFamilyMember(em, familyEntity, founder, FamilyRole.Founder, currentTick);
            EnsureFamilyTreeEntry(em, familyEntity, founder, Entity.Null, Entity.Null, ResolveBirthTick(em, founder, currentTick));

            return familyEntity;
        }

        private static void RemoveFamilyMembership(EntityManager em, Entity member)
        {
            if (!em.HasComponent<FamilyMember>(member))
            {
                return;
            }

            var family = em.GetComponentData<FamilyMember>(member).FamilyEntity;
            if (family != Entity.Null && em.HasBuffer<FamilyMemberEntry>(family))
            {
                var members = em.GetBuffer<FamilyMemberEntry>(family);
                for (int i = members.Length - 1; i >= 0; i--)
                {
                    if (members[i].MemberEntity == member)
                    {
                        members.RemoveAt(i);
                        break;
                    }
                }
            }

            em.RemoveComponent<FamilyMember>(member);
        }

        private static void AddOrUpdateFamilyMember(EntityManager em, Entity family, Entity member, FamilyRole role, uint currentTick)
        {
            if (em.HasComponent<FamilyMember>(member))
            {
                var existing = em.GetComponentData<FamilyMember>(member);
                if (existing.Role == FamilyRole.Founder)
                {
                    role = FamilyRole.Founder;
                }
                existing.FamilyEntity = family;
                existing.Role = role;
                em.SetComponentData(member, existing);
            }
            else
            {
                em.AddComponentData(member, new FamilyMember
                {
                    FamilyEntity = family,
                    Role = role
                });
            }

            if (!em.HasBuffer<FamilyMemberEntry>(family))
            {
                em.AddBuffer<FamilyMemberEntry>(family);
            }

            var members = em.GetBuffer<FamilyMemberEntry>(family);
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].MemberEntity == member)
                {
                    var entry = members[i];
                    entry.Role = role;
                    members[i] = entry;
                    return;
                }
            }

            members.Add(new FamilyMemberEntry
            {
                MemberEntity = member,
                Role = role,
                JoinedTick = currentTick
            });
        }

        private static void EnsureFamilyTreeEntry(
            EntityManager em,
            Entity family,
            Entity member,
            Entity parentA,
            Entity parentB,
            uint birthTick)
        {
            if (!em.HasBuffer<FamilyTree>(family))
            {
                em.AddBuffer<FamilyTree>(family);
            }

            var tree = em.GetBuffer<FamilyTree>(family);
            for (int i = 0; i < tree.Length; i++)
            {
                if (tree[i].MemberEntity == member)
                {
                    var entry = tree[i];
                    if (entry.ParentA == Entity.Null)
                    {
                        entry.ParentA = parentA;
                    }
                    if (entry.ParentB == Entity.Null)
                    {
                        entry.ParentB = parentB;
                    }
                    if (entry.BirthTick == 0)
                    {
                        entry.BirthTick = birthTick;
                    }
                    tree[i] = entry;
                    return;
                }
            }

            tree.Add(new FamilyTree
            {
                MemberEntity = member,
                ParentA = parentA,
                ParentB = parentB,
                BirthTick = birthTick
            });
        }

        private static void RemoveDynastyMembership(EntityManager em, Entity member)
        {
            if (!em.HasComponent<DynastyMember>(member))
            {
                return;
            }

            var dynasty = em.GetComponentData<DynastyMember>(member).DynastyEntity;
            if (dynasty != Entity.Null && em.HasBuffer<DynastyMemberEntry>(dynasty))
            {
                var members = em.GetBuffer<DynastyMemberEntry>(dynasty);
                for (int i = members.Length - 1; i >= 0; i--)
                {
                    if (members[i].MemberEntity == member)
                    {
                        members.RemoveAt(i);
                        break;
                    }
                }
            }

            em.RemoveComponent<DynastyMember>(member);
        }

        private static void AddOrUpdateDynastyMember(
            EntityManager em,
            Entity dynasty,
            Entity member,
            DynastyRank rank,
            float lineageStrength,
            uint currentTick)
        {
            if (em.HasComponent<DynastyMember>(member))
            {
                var existing = em.GetComponentData<DynastyMember>(member);
                if (existing.Rank == DynastyRank.Founder)
                {
                    rank = DynastyRank.Founder;
                }
                existing.DynastyEntity = dynasty;
                existing.Rank = rank;
                existing.LineageStrength = lineageStrength;
                em.SetComponentData(member, existing);
            }
            else
            {
                em.AddComponentData(member, new DynastyMember
                {
                    DynastyEntity = dynasty,
                    Rank = rank,
                    LineageStrength = lineageStrength
                });
            }

            if (!em.HasBuffer<DynastyMemberEntry>(dynasty))
            {
                em.AddBuffer<DynastyMemberEntry>(dynasty);
            }

            var members = em.GetBuffer<DynastyMemberEntry>(dynasty);
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].MemberEntity == member)
                {
                    var entry = members[i];
                    entry.Rank = rank;
                    entry.LineageStrength = lineageStrength;
                    members[i] = entry;
                    return;
                }
            }

            members.Add(new DynastyMemberEntry
            {
                MemberEntity = member,
                Rank = rank,
                LineageStrength = lineageStrength,
                JoinedTick = currentTick
            });
        }

        private static void EnsureDynastyLineageEntry(
            EntityManager em,
            Entity dynasty,
            Entity member,
            Entity parentA,
            Entity parentB,
            uint birthTick,
            byte generation)
        {
            if (!em.HasBuffer<DynastyLineage>(dynasty))
            {
                em.AddBuffer<DynastyLineage>(dynasty);
            }

            var lineage = em.GetBuffer<DynastyLineage>(dynasty);
            for (int i = 0; i < lineage.Length; i++)
            {
                if (lineage[i].MemberEntity == member)
                {
                    var entry = lineage[i];
                    if (entry.ParentA == Entity.Null)
                    {
                        entry.ParentA = parentA;
                    }
                    if (entry.ParentB == Entity.Null)
                    {
                        entry.ParentB = parentB;
                    }
                    if (entry.Generation == 0 && generation != 0)
                    {
                        entry.Generation = generation;
                    }
                    if (entry.BirthTick == 0)
                    {
                        entry.BirthTick = birthTick;
                    }
                    lineage[i] = entry;
                    return;
                }
            }

            lineage.Add(new DynastyLineage
            {
                MemberEntity = member,
                ParentA = parentA,
                ParentB = parentB,
                BirthTick = birthTick,
                Generation = generation
            });
        }

        private static Entity GetFamily(EntityManager em, Entity member)
        {
            if (!em.HasComponent<FamilyMember>(member))
            {
                return Entity.Null;
            }

            return em.GetComponentData<FamilyMember>(member).FamilyEntity;
        }

        private static Entity GetDynasty(EntityManager em, Entity member)
        {
            if (!em.HasComponent<DynastyMember>(member))
            {
                return Entity.Null;
            }

            return em.GetComponentData<DynastyMember>(member).DynastyEntity;
        }

        private static FixedString64Bytes ResolveFamilyName(EntityManager em, Entity partnerA, Entity partnerB)
        {
            if (TryGetFamilyName(em, partnerA, out var name))
            {
                return name;
            }

            if (TryGetFamilyName(em, partnerB, out name))
            {
                return name;
            }

            return new FixedString64Bytes("Family");
        }

        private static bool TryGetFamilyName(EntityManager em, Entity member, out FixedString64Bytes name)
        {
            name = default;
            if (!em.HasComponent<FamilyMember>(member))
            {
                return false;
            }

            var familyEntity = em.GetComponentData<FamilyMember>(member).FamilyEntity;
            if (familyEntity == Entity.Null || !em.HasComponent<FamilyIdentity>(familyEntity))
            {
                return false;
            }

            name = em.GetComponentData<FamilyIdentity>(familyEntity).FamilyName;
            return name.Length > 0;
        }

        private static uint ResolveBirthTick(EntityManager em, Entity member, uint fallbackTick)
        {
            if (em.HasComponent<LifecycleState>(member))
            {
                return em.GetComponentData<LifecycleState>(member).BirthTick;
            }

            return fallbackTick;
        }

        private static byte ResolveDynastyGeneration(EntityManager em, Entity dynasty, Entity member)
        {
            if (!em.HasBuffer<DynastyLineage>(dynasty))
            {
                return 0;
            }

            var lineage = em.GetBuffer<DynastyLineage>(dynasty);
            for (int i = 0; i < lineage.Length; i++)
            {
                if (lineage[i].MemberEntity == member)
                {
                    return lineage[i].Generation;
                }
            }

            return 0;
        }

        private static float GetDynastyLineageStrength(EntityManager em, Entity member, Entity dynasty)
        {
            if (!em.HasComponent<DynastyMember>(member))
            {
                return 0f;
            }

            var dynastyMember = em.GetComponentData<DynastyMember>(member);
            if (dynastyMember.DynastyEntity != dynasty)
            {
                return 0f;
            }

            return dynastyMember.LineageStrength;
        }

        private static void EnsureSpouseRelation(EntityManager em, Entity partnerA, Entity partnerB, uint currentTick)
        {
            const sbyte intensity = 85;
            const byte trust = 90;
            const byte familiarity = 100;
            const byte respect = 70;
            const byte fear = 0;

            EnsureMutualRelation(em, partnerA, partnerB, RelationType.Spouse, intensity, trust, familiarity, respect, fear, currentTick);
        }

        private static void EnsureParentChildRelation(EntityManager em, Entity parent, Entity child, uint currentTick)
        {
            const sbyte intensity = 80;
            const byte trust = 90;
            const byte familiarity = 100;
            const byte respectParent = 70;
            const byte respectChild = 50;
            const byte fear = 0;

            EnsureRelation(em, child, parent, RelationType.Parent, intensity, trust, familiarity, respectParent, fear, currentTick);
            EnsureRelation(em, parent, child, RelationType.Child, intensity, trust, familiarity, respectChild, fear, currentTick);
        }

        private static void EnsureHouseRelation(EntityManager em, Entity familyA, Entity familyB, uint currentTick)
        {
            const sbyte intensity = 45;
            const byte trust = 60;
            const byte familiarity = 40;
            const byte respect = 60;
            const byte fear = 0;

            EnsureMutualRelation(em, familyA, familyB, RelationType.InLaw, intensity, trust, familiarity, respect, fear, currentTick);
        }

        private static void EnsureDynastyRelation(EntityManager em, Entity dynastyA, Entity dynastyB, uint currentTick)
        {
            const sbyte intensity = 35;
            const byte trust = 50;
            const byte familiarity = 20;
            const byte respect = 50;
            const byte fear = 0;

            EnsureMutualRelation(em, dynastyA, dynastyB, RelationType.Ally, intensity, trust, familiarity, respect, fear, currentTick);
        }

        private static void EnsureMutualRelation(
            EntityManager em,
            Entity entityA,
            Entity entityB,
            RelationType type,
            sbyte intensity,
            byte trust,
            byte familiarity,
            byte respect,
            byte fear,
            uint currentTick)
        {
            EnsureRelation(em, entityA, entityB, type, intensity, trust, familiarity, respect, fear, currentTick);
            EnsureRelation(em, entityB, entityA, type, intensity, trust, familiarity, respect, fear, currentTick);
        }

        private static void EnsureRelation(
            EntityManager em,
            Entity owner,
            Entity other,
            RelationType type,
            sbyte intensity,
            byte trust,
            byte familiarity,
            byte respect,
            byte fear,
            uint currentTick)
        {
            if (!em.HasBuffer<EntityRelation>(owner))
            {
                em.AddBuffer<EntityRelation>(owner);
            }

            var relations = em.GetBuffer<EntityRelation>(owner);
            int index = RelationCalculator.FindRelationIndex(relations, other);
            if (index >= 0)
            {
                var relation = relations[index];
                relation.Type = type;
                relation.Intensity = (sbyte)math.max((int)relation.Intensity, (int)intensity);
                relation.Trust = (byte)math.max((int)relation.Trust, (int)trust);
                relation.Familiarity = (byte)math.max((int)relation.Familiarity, (int)familiarity);
                relation.Respect = (byte)math.max((int)relation.Respect, (int)respect);
                relation.Fear = (byte)math.max((int)relation.Fear, (int)fear);
                relation.LastInteractionTick = currentTick;
                relations[index] = relation;
                return;
            }

            relations.Add(new EntityRelation
            {
                OtherEntity = other,
                Type = type,
                Intensity = intensity,
                InteractionCount = 0,
                FirstMetTick = currentTick,
                LastInteractionTick = currentTick,
                Trust = trust,
                Familiarity = familiarity,
                Respect = respect,
                Fear = fear
            });
        }
    }
}
