using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ResourcePileSystem : ISystem
    {
        private EntityQuery _pileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _pileQuery = state.GetEntityQuery(ComponentType.ReadWrite<ResourcePile>(), ComponentType.ReadOnly<ResourcePileMeta>());
            state.RequireForUpdate(_pileQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var piles = _pileQuery.ToEntityArray(Allocator.Temp);
            var pileData = _pileQuery.ToComponentDataArray<ResourcePile>(Allocator.Temp);
            var pileMeta = _pileQuery.ToComponentDataArray<ResourcePileMeta>(Allocator.Temp);

            for (int i = 0; i < piles.Length; i++)
            {
                for (int j = i + 1; j < piles.Length; j++)
                {
                    if (!pileMeta[i].ResourceTypeId.Equals(pileMeta[j].ResourceTypeId))
                    {
                        continue;
                    }

                    var distSq = math.lengthsq(pileData[i].Position - pileData[j].Position);
                    if (distSq > 1f) // merge when within 1m
                    {
                        continue;
                    }

                    var total = pileData[i].Amount + pileData[j].Amount;
                    pileData[i].Amount = math.min(pileMeta[i].MaxCapacity, total);
                    pileData[j].Amount = 0f;
                }
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            for (int i = 0; i < piles.Length; i++)
            {
                if (pileData[i].Amount <= 0f)
                {
                    commandBuffer.DestroyEntity(piles[i]);
                }
                else
                {
                    commandBuffer.SetComponent(piles[i], pileData[i]);
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
            piles.Dispose();
            pileData.Dispose();
            pileMeta.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
