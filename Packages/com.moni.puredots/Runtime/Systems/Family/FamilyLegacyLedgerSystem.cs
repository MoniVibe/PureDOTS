using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Family;

namespace PureDOTS.Systems.Family
{
    /// <summary>
    /// Maintains a legacy roster of all family members (alive or dead).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FamilyTreeUpdateSystem))]
    public partial struct FamilyLegacyLedgerSystem : ISystem
    {
        private ComponentLookup<FamilyMember> _familyMemberLookup;
        private ComponentLookup<DeathState> _deathStateLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _familyMemberLookup = state.GetComponentLookup<FamilyMember>(true);
            _deathStateLookup = state.GetComponentLookup<DeathState>(true);
        }

        [BurstCompile]
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

            _familyMemberLookup.Update(ref state);
            _deathStateLookup.Update(ref state);

            var em = state.EntityManager;

            foreach (var (identity, familyTree, familyEntity) in SystemAPI
                         .Query<RefRO<FamilyIdentity>, DynamicBuffer<FamilyTree>>()
                         .WithEntityAccess())
            {
                if (!em.HasBuffer<FamilyLegacyEntry>(familyEntity))
                {
                    em.AddBuffer<FamilyLegacyEntry>(familyEntity);
                    continue;
                }

                var legacy = em.GetBuffer<FamilyLegacyEntry>(familyEntity);
                using var generationMap = BuildGenerationMap(familyTree, identity.ValueRO.FounderEntity);

                for (int i = 0; i < familyTree.Length; i++)
                {
                    var treeEntry = familyTree[i];
                    if (treeEntry.MemberEntity == Entity.Null)
                    {
                        continue;
                    }

                    var member = treeEntry.MemberEntity;
                    byte generation = generationMap.TryGetValue(member, out var gen) ? gen : (byte)0;

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
                        entry.Generation = generation;
                        entry.IsDead = (byte)(isDead ? 1 : 0);
                        entry.BirthTick = treeEntry.BirthTick;
                        if (isDead && entry.DeathTick == 0)
                        {
                            entry.DeathTick = deathTick;
                        }

                        var role = ResolveRole(identity.ValueRO.FounderEntity, member, entry.Role);
                        entry.Role = role;

                        legacy[index] = entry;
                        continue;
                    }

                    legacy.Add(new FamilyLegacyEntry
                    {
                        MemberEntity = member,
                        Role = ResolveRole(identity.ValueRO.FounderEntity, member, FamilyRole.Extended),
                        Generation = generation,
                        IsDead = (byte)(isDead ? 1 : 0),
                        BirthTick = treeEntry.BirthTick,
                        DeathTick = deathTick
                    });
                }
            }
        }

        private static int FindLegacyIndex(DynamicBuffer<FamilyLegacyEntry> legacy, Entity member)
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

        private FamilyRole ResolveRole(Entity founder, Entity member, FamilyRole fallback)
        {
            if (member == founder)
            {
                return FamilyRole.Founder;
            }

            if (_familyMemberLookup.HasComponent(member))
            {
                return _familyMemberLookup[member].Role;
            }

            return fallback;
        }

        private static NativeHashMap<Entity, byte> BuildGenerationMap(
            DynamicBuffer<FamilyTree> familyTree,
            Entity founder)
        {
            var generations = new NativeHashMap<Entity, byte>(familyTree.Length, Allocator.Temp);
            var childrenMap = new NativeParallelMultiHashMap<Entity, Entity>(familyTree.Length, Allocator.Temp);
            var queue = new NativeQueue<Entity>(Allocator.Temp);

            for (int i = 0; i < familyTree.Length; i++)
            {
                var entry = familyTree[i];
                if (entry.ParentA != Entity.Null)
                {
                    childrenMap.Add(entry.ParentA, entry.MemberEntity);
                }
                if (entry.ParentB != Entity.Null && entry.ParentB != entry.ParentA)
                {
                    childrenMap.Add(entry.ParentB, entry.MemberEntity);
                }
            }

            if (founder != Entity.Null)
            {
                generations.TryAdd(founder, 0);
                queue.Enqueue(founder);
            }
            else
            {
                for (int i = 0; i < familyTree.Length; i++)
                {
                    var entry = familyTree[i];
                    if (entry.ParentA == Entity.Null && entry.ParentB == Entity.Null)
                    {
                        if (generations.TryAdd(entry.MemberEntity, 0))
                        {
                            queue.Enqueue(entry.MemberEntity);
                        }
                    }
                }
            }

            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                var parentGen = generations[parent];

                var iterator = childrenMap.GetValuesForKey(parent);
                while (iterator.MoveNext())
                {
                    var child = iterator.Current;
                    if (child == Entity.Null)
                    {
                        continue;
                    }

                    if (generations.TryAdd(child, (byte)(parentGen + 1)))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            childrenMap.Dispose();
            queue.Dispose();

            return generations;
        }
    }
}
