using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Village;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Ensures each resident villager carries a membership entry for the village aggregate, updating loyalty/sympathy from stats.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct VillagerVillageMembershipSystem : ISystem
    {
        private EntityQuery _villageQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _villageQuery = SystemAPI.QueryBuilder()
                .WithAll<VillageId, VillageStats>()
                .WithAll<VillageResidentEntry>()
                .Build();
            state.RequireForUpdate(_villageQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var villages = _villageQuery.ToEntityArray(state.WorldUpdateAllocator);
            var villageIds = _villageQuery.ToComponentDataArray<VillageId>(state.WorldUpdateAllocator);
            var stats = _villageQuery.ToComponentDataArray<VillageStats>(state.WorldUpdateAllocator);
            var alignments = state.GetComponentLookup<VillageAlignmentState>(true);
            var residentBufferLookup = state.GetBufferLookup<VillageResidentEntry>(false);

            for (int i = 0; i < villages.Length; i++)
            {
                var villageEntity = villages[i];
                var villageStat = stats[i];
                var loyalty = math.clamp(villageStat.Cohesion / 100f, 0f, 1f);
                float sympathy = 0f;
                if (alignments.HasComponent(villageEntity))
                {
                    sympathy = math.saturate(alignments[villageEntity].Integrity);
                    sympathy = sympathy * 2f - 1f; // convert 0-1 to -1..1
                }

                if (!residentBufferLookup.HasBuffer(villageEntity))
                {
                    continue;
                }

                var residents = residentBufferLookup[villageEntity];
                for (int r = residents.Length - 1; r >= 0; r--)
                {
                    var resident = residents[r];
                    var villager = resident.VillagerEntity;
                    if (!state.EntityManager.Exists(villager))
                    {
                        residents.RemoveAt(r);
                        continue;
                    }

                    EnsureMembership(ref state, villager, villageEntity, loyalty, sympathy);
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private static void EnsureMembership(ref SystemState state, Entity villager, Entity villageEntity, float loyalty, float sympathy)
        {
            DynamicBuffer<VillagerAggregateMembership> buffer;
            if (!state.EntityManager.HasComponent<VillagerAggregateMembership>(villager))
            {
                buffer = state.EntityManager.AddBuffer<VillagerAggregateMembership>(villager);
            }
            else
            {
                buffer = state.EntityManager.GetBuffer<VillagerAggregateMembership>(villager);
            }

            var updated = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Aggregate == villageEntity && buffer[i].Category == AggregateCategory.Village)
                {
                    buffer[i] = new VillagerAggregateMembership
                    {
                        Aggregate = villageEntity,
                        Category = AggregateCategory.Village,
                        Loyalty = loyalty,
                        Sympathy = sympathy
                    };
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                buffer.Add(new VillagerAggregateMembership
                {
                    Aggregate = villageEntity,
                    Category = AggregateCategory.Village,
                    Loyalty = loyalty,
                    Sympathy = sympathy
                });
            }
        }
    }
}
