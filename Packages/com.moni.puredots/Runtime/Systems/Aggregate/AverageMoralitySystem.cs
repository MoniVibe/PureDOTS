using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.Aggregate
{
    /// <summary>
    /// Computes average morality from villager components.
    /// Updates WorldAggregateProfile.Harmony based on morality values.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SumPopulationSystem))]
    public partial struct AverageMoralitySystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateInterval = 10;

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

            float totalMorale = 0f;
            int count = 0;

            // Aggregate morale from villagers
            foreach (var needs in SystemAPI.Query<RefRO<VillagerNeeds>>())
            {
                totalMorale += needs.ValueRO.MoraleFloat;
                count++;
            }

            float avgMorale = count > 0 ? totalMorale / count : 0f;
            
            // Normalize to 0-1 range for harmony (assuming morale is 0-100)
            float harmony = math.clamp(avgMorale / 100f, 0f, 1f);
            
            profile.ValueRW.Harmony = harmony;
        }
    }
}

