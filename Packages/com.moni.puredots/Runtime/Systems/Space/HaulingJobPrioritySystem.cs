using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(HaulingJobManagerSystem))]
    public partial struct HaulingJobPrioritySystem : ISystem
    {
        private EntityQuery _pileQuery;
        private EntityQuery _valueCatalogQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _pileQuery = state.GetEntityQuery(ComponentType.ReadOnly<ResourcePile>(), ComponentType.ReadOnly<ResourcePileMeta>());
            _valueCatalogQuery = state.GetEntityQuery(ComponentType.ReadOnly<ResourceValueCatalogTag>(), ComponentType.ReadOnly<ResourceValueEntry>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var catalogBuffer = _valueCatalogQuery.IsEmpty ? default : _valueCatalogQuery.GetSingletonBuffer<ResourceValueEntry>();
            var piles = _pileQuery.ToEntityArray(Allocator.Temp);
            var metas = _pileQuery.ToComponentDataArray<ResourcePileMeta>(Allocator.Temp);
            for (int i = 0; i < piles.Length; i++)
            {
                var urgency = 1f;
                var value = 1f;
                if (catalogBuffer.IsCreated)
                {
                    for (int j = 0; j < catalogBuffer.Length; j++)
                    {
                        if (catalogBuffer[j].ResourceTypeId.Equals(metas[i].ResourceTypeId))
                        {
                            value = catalogBuffer[j].BaseValue;
                            break;
                        }
                    }
                }

                state.EntityManager.AddComponentData(piles[i], new ResourceUrgency
                {
                    ResourceTypeId = metas[i].ResourceTypeId,
                    UrgencyWeight = urgency * value
                });
            }

            piles.Dispose();
            metas.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
