using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Village;
using PureDOTS.Runtime.Villagers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Adds default outlook/policy components to villages based on alignment until bespoke data is authored.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(VillageJobPreferenceSystem))]
    public partial struct VillageOutlookBootstrapSystem : ISystem
    {
        private EntityQuery _villageQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _villageQuery = state.GetEntityQuery(ComponentType.ReadOnly<VillageId>());
            state.RequireForUpdate(_villageQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (villageId, entity) in SystemAPI.Query<RefRO<VillageId>>().WithNone<VillageOutlook>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new VillageOutlook { Flags = VillageOutlookFlags.None });
            }

            foreach (var (villageId, entity) in SystemAPI.Query<RefRO<VillageId>>().WithNone<VillageWorkforcePolicy>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new VillageWorkforcePolicy
                {
                    ConscriptionUrgency = 0f,
                    DefenseUrgency = 0f,
                    ConscriptionActive = 0
                });
            }

            foreach (var (villageId, entity) in SystemAPI.Query<RefRO<VillageId>>().WithNone<VillagerAlignment>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new VillagerAlignment());
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            var alignments = state.GetComponentLookup<VillagerAlignment>(true);
            var outlooks = state.GetComponentLookup<VillageOutlook>();

            foreach (var (villageId, entity) in SystemAPI.Query<RefRO<VillageId>>().WithEntityAccess())
            {
                if (!outlooks.HasComponent(entity))
                {
                    continue;
                }

                var flags = VillageOutlookFlags.None;
                if (alignments.HasComponent(entity))
                {
                    var alignment = alignments[entity];
                    if (alignment.MaterialismNormalized > 0.25f)
                    {
                        flags |= VillageOutlookFlags.Materialistic;
                    }
                    if (alignment.MaterialismNormalized < -0.25f)
                    {
                        flags |= VillageOutlookFlags.Ascetic;
                    }
                    if (alignment.PurityNormalized < -0.25f && math.abs(alignment.OrderNormalized) > 0.2f)
                    {
                        flags |= VillageOutlookFlags.Warlike;
                    }
                    if (alignment.OrderNormalized > 0.3f)
                    {
                        flags |= VillageOutlookFlags.Expansionist;
                    }
                }

                var outlook = outlooks[entity];
                outlook.Flags = flags;
                outlooks[entity] = outlook;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
