using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ResourcePileMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ResourcePileVelocity>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (pile, velocity) in SystemAPI.Query<RefRW<ResourcePile>, RefRO<ResourcePileVelocity>>())
            {
                pile.ValueRW.Position += velocity.ValueRO.Velocity * deltaTime;
                velocity.ValueRO.Velocity *= 0.99f; // slight damping
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
