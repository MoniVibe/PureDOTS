using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.Aggregate
{
    /// <summary>
    /// Tracks energy flux from resources, miracles, and production.
    /// Updates WorldAggregateProfile.EnergyFlux.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AverageMoralitySystem))]
    public partial struct EnergyBalanceSystem : ISystem
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

            float totalEnergy = 0f;

            // Sum resource production
            foreach (var source in SystemAPI.Query<RefRO<ResourceSourceConfig>>())
            {
                totalEnergy += source.ValueRO.ProductionRate;
            }

            // Sum miracle energy (if applicable)
            // This would be extended based on game-specific miracle systems

            profile.ValueRW.EnergyFlux = totalEnergy;
        }
    }
}

