using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.Aggregate
{
    /// <summary>
    /// Aggregates villager/population counts into WorldAggregateProfile.
    /// Updates every N ticks (configurable, default 10).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SumPopulationSystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateInterval = 10; // Update every 10 ticks

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTick = 0;
            state.RequireForUpdate<WorldAggregateProfile>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.Tick - _lastUpdateTick < UpdateInterval)
            {
                return;
            }

            _lastUpdateTick = timeState.Tick;

            var profileEntity = SystemAPI.GetSingletonEntity<WorldAggregateProfile>();
            var profile = SystemAPI.GetComponentRW<WorldAggregateProfile>(profileEntity);

            float totalPopulation = 0f;

            // Count villagers
            foreach (var _ in SystemAPI.Query<RefRO<VillagerId>>())
            {
                totalPopulation += 1f;
            }

            // Count other population entities (bands, groups, etc.)
            // This would be extended based on game-specific entities

            profile.ValueRW.Population = totalPopulation;
        }
    }
}

