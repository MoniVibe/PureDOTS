using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ResourcePileSystem))]
    public partial struct HaulingJobManagerSystem : ISystem
    {
        private Entity _jobQueueEntity;
        private EntityQuery _storehouseQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _jobQueueEntity = state.EntityManager.CreateEntity(typeof(HaulingJobQueueEntry));
            _storehouseQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<StorehouseInventory>(), ComponentType.ReadOnly<LocalTransform>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var queue = state.EntityManager.GetBuffer<HaulingJobQueueEntry>(_jobQueueEntity);
            queue.Clear();

            var storehouseEntities = _storehouseQuery.ToEntityArray(Allocator.Temp);
            var storehouseTransforms = _storehouseQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            foreach (var (pile, urgency, entity) in SystemAPI.Query<RefRO<ResourcePile>, RefRO<ResourceUrgency>>().WithEntityAccess())
            {
                var destination = FindNearestStorehouse(pile.ValueRO.Position, storehouseEntities, storehouseTransforms);
                queue.Add(new HaulingJobQueueEntry
                {
                    Priority = HaulingJobPriority.Normal,
                    SourceEntity = entity,
                    DestinationEntity = destination,
                    RequestedAmount = pile.ValueRO.Amount,
                    Urgency = urgency.ValueRO.UrgencyWeight,
                    ResourceValue = urgency.ValueRO.UrgencyWeight
                });
            }

            storehouseEntities.Dispose();
            storehouseTransforms.Dispose();
        }

        private static Entity FindNearestStorehouse(float3 origin, NativeArray<Entity> entities, NativeArray<LocalTransform> transforms)
        {
            if (entities.Length == 0)
            {
                return Entity.Null;
            }

            var best = Entity.Null;
            var bestDist = float.MaxValue;
            for (int i = 0; i < entities.Length; i++)
            {
                var dist = math.lengthsq(transforms[i].Position - origin);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = entities[i];
                }
            }

            return best;
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
