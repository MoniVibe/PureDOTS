using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Routes entities to appropriate physics tier groups based on mass thresholds.
    /// Updates MassTierComponent and manages tier transitions.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(LightMassPhysicsGroup))]
    public partial struct AdaptivePhysicsSystem : ISystem
    {
        private EntityQuery _massQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _massQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<MassComponent>(),
                ComponentType.ReadWrite<MassTierComponent>()
            );

            // Ensure PhysicsTierConfig exists
            if (!SystemAPI.HasSingleton<PhysicsTierConfig>())
            {
                var entity = state.EntityManager.CreateEntity(typeof(PhysicsTierConfig));
                state.EntityManager.SetComponentData(entity, PhysicsTierConfig.Default);
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var config = SystemAPI.GetSingleton<PhysicsTierConfig>();

            foreach (var (mass, tier) in SystemAPI.Query<RefRO<MassComponent>, RefRW<MassTierComponent>>())
            {
                var massValue = mass.ValueRO.Mass;
                var newTier = MassTier.Light;

                if (massValue >= config.HeavyTierThreshold)
                {
                    newTier = MassTier.Heavy;
                }
                else if (massValue >= config.MediumTierThreshold)
                {
                    newTier = MassTier.Medium;
                }

                tier.ValueRW.Tier = newTier;
            }
        }
    }
}

