using Space4X.Individuals;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Systems
{
    /// <summary>
    /// Aggregates individual stats to vessel/carrier level for system queries.
    /// Runs in FixedStep simulation group.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct IndividualStatAggregationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO: Implement stat aggregation logic
            // For MVP, this is a placeholder system
            // Future: Aggregate Command, Tactics, Logistics, etc. from crew individuals
            // Store aggregated stats in vessel/carrier components
        }
    }
}

