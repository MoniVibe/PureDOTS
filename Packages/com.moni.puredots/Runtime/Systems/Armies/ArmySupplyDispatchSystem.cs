using PureDOTS.Runtime.Armies;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArmySupplyRequestSystem))]
    public partial struct ArmySupplyDispatchSystem : ISystem
    {
        private EntityQuery _depotQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _depotQuery = state.GetEntityQuery(ComponentType.ReadOnly<ArmySupplyDepot>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadWrite<ArmySupplyRequest>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = SystemAPI.GetSingleton<TimeState>();
            if (time.IsPaused)
            {
                return;
            }

            foreach (var (depot, transform, requests, entity) in SystemAPI.Query<RefRO<ArmySupplyDepot>, RefRO<LocalTransform>, DynamicBuffer<ArmySupplyRequest>>().WithEntityAccess())
            {
                for (int i = requests.Length - 1; i >= 0; i--)
                {
                    var request = requests[i];
                    if (!state.EntityManager.Exists(request.Army))
                    {
                        requests.RemoveAt(i);
                        continue;
                    }

                    if (math.lengthsq(request.Destination - transform.ValueRO.Position) < 1f)
                    {
                        requests.RemoveAt(i);
                        continue;
                    }

                    var stats = state.EntityManager.GetComponentData<ArmyStats>(request.Army);
                    stats.SupplyLevel = math.saturate(stats.SupplyLevel + request.SupplyNeeded);
                    state.EntityManager.SetComponentData(request.Army, stats);
                    requests.RemoveAt(i);
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
