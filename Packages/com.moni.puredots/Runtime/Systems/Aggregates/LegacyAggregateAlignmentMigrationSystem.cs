using PureDOTS.Runtime.Aggregates;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Village;
using PureDOTS.Runtime.Villagers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

#pragma warning disable 0618 // Legacy components are intentionally referenced for migration

namespace PureDOTS.Systems
{
    /// <summary>
    /// Converts legacy VillageAlignmentState / GuildAlignment components into the shared VillagerAlignment contract.
    /// Runs opportunistically so newly streamed entities are migrated automatically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct LegacyAggregateAlignmentMigrationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var hasTime = SystemAPI.TryGetSingleton(out TimeState timeState);
            var currentTick = hasTime ? timeState.Tick : 0u;

            foreach (var (legacy, entity) in SystemAPI
                         .Query<RefRO<VillageAlignmentState>>()
                         .WithEntityAccess())
            {
                if (SystemAPI.HasComponent<VillagerAlignment>(entity))
                {
                    ecb.RemoveComponent<VillageAlignmentState>(entity);
                    continue;
                }

                ecb.AddComponent(entity, ConvertVillageAlignment(legacy.ValueRO, currentTick));
                ecb.RemoveComponent<VillageAlignmentState>(entity);
            }

            foreach (var (legacy, entity) in SystemAPI
                         .Query<RefRO<GuildAlignment>>()
                         .WithEntityAccess())
            {
                if (!SystemAPI.HasComponent<VillagerAlignment>(entity))
                {
                    var converted = ConvertGuildAlignment(legacy.ValueRO, currentTick);
                    ecb.AddComponent(entity, converted.Alignment);
                    if (SystemAPI.HasComponent<GuildOutlookSet>(entity))
                    {
                        ecb.SetComponent(entity, converted.Outlooks);
                    }
                    else
                    {
                        ecb.AddComponent(entity, converted.Outlooks);
                    }
                    ecb.RemoveComponent<GuildAlignment>(entity);
                    continue;
                }

                if (!SystemAPI.HasComponent<GuildOutlookSet>(entity))
                {
                    var outlooks = new GuildOutlookSet
                    {
                        Outlook1 = legacy.ValueRO.Outlook1,
                        Outlook2 = legacy.ValueRO.Outlook2,
                        Outlook3 = legacy.ValueRO.Outlook3,
                        IsFanatic = legacy.ValueRO.IsFanatic
                    };
                    ecb.AddComponent(entity, outlooks);
                }

                ecb.RemoveComponent<GuildAlignment>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private static VillagerAlignment ConvertVillageAlignment(in VillageAlignmentState legacy, uint currentTick)
        {
            return new VillagerAlignment
            {
                MoralAxis = VillagerAlignment.ToAxisValue(-legacy.Materialism),
                OrderAxis = VillagerAlignment.ToAxisValue(legacy.LawChaos),
                PurityAxis = VillagerAlignment.ToAxisValue(legacy.Integrity),
                AlignmentStrength = math.saturate(math.abs(legacy.Integrity)),
                LastShiftTick = currentTick
            };
        }

        private static (VillagerAlignment Alignment, GuildOutlookSet Outlooks) ConvertGuildAlignment(in GuildAlignment legacy, uint currentTick)
        {
            var alignment = new VillagerAlignment
            {
                MoralAxis = legacy.MoralAxis,
                OrderAxis = legacy.OrderAxis,
                PurityAxis = legacy.PurityAxis,
                AlignmentStrength = math.saturate(math.abs(legacy.MoralAxis) * 0.01f),
                LastShiftTick = currentTick
            };

            var outlooks = new GuildOutlookSet
            {
                Outlook1 = legacy.Outlook1,
                Outlook2 = legacy.Outlook2,
                Outlook3 = legacy.Outlook3,
                IsFanatic = legacy.IsFanatic
            };

            return (alignment, outlooks);
        }
    }
}
#pragma warning restore 0618
