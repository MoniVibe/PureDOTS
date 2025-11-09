using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Simple debug system to verify villager movement is working.
    /// Moves villagers in a small circle to test basic movement.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(VillagerMiningSystem))]
    public partial struct VillagerMovementDebugSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            const float rotationSpeed = 1f; // radians per second
            const float radius = 2f;

            var count = 0;
            foreach (var (transform, villagerId, entity) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<VillagerId>>()
                         .WithEntityAccess())
            {
                count++;
                
                // Simple circular movement for testing
                var time = (float)SystemAPI.Time.ElapsedTime;
                var angle = time * rotationSpeed + (villagerId.ValueRO.Value * 0.5f);
                var basePos = new float3(0f, 0f, 0f); // Center point
                
                var offset = new float3(
                    math.cos(angle) * radius,
                    0f,
                    math.sin(angle) * radius
                );
                
                transform.ValueRW.Position = basePos + offset;
                
                // Face movement direction
                var direction = math.normalize(offset);
                if (math.lengthsq(direction) > 0.0001f)
                {
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                }
            }
            
            // Log once per second
            if (SystemAPI.Time.ElapsedTime % 1.0 < deltaTime && count > 0)
            {
                UnityEngine.Debug.Log($"[VillagerMovementDebug] Moving {count} villagers in circles");
            }
        }
    }
}





