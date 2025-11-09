using PureDOTS.Runtime.Ships;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Ships
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CrewAggregationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CrewAggregate>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var crew in SystemAPI.Query<RefRW<CrewAggregate>>())
            {
                // Placeholder: aggregate individual stats to update crew morale/outlook
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
