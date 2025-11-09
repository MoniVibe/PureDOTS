using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ResourcePileSystem))]
    public partial struct HaulingJobManagerSystem : ISystem
    {
        private Entity _jobQueueEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _jobQueueEntity = state.EntityManager.CreateEntity(typeof(HaulingJobQueueEntry));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var queue = state.EntityManager.GetBuffer<HaulingJobQueueEntry>(_jobQueueEntity);
            queue.Clear();

            foreach (var (pile, entity) in SystemAPI.Query<RefRO<ResourcePile>>().WithEntityAccess())
            {
                queue.Add(new HaulingJobQueueEntry
                {
                    Priority = HaulingJobPriority.Normal,
                    SourceEntity = entity,
                    DestinationEntity = Entity.Null,
                    RequestedAmount = pile.ValueRO.Amount
                });
            }
        }

        public BufferLookup<HaulingJobQueueEntry> GetQueueLookup(ref SystemState state)
        {
            return state.GetBufferLookup<HaulingJobQueueEntry>(false);
        }

        public Entity GetQueueEntity() => _jobQueueEntity;

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
