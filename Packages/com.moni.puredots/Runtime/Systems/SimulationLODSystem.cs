using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Assigns LOD levels based on distance to camera.
    /// Far agents = statistical simulation, near agents = high-fidelity ECS ticks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    public partial struct SimulationLODSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickTimeState.Tick;

            // Get camera position (simplified - would need actual camera system)
            float3 cameraPosition = float3.zero;
            if (SystemAPI.TryGetSingleton<Unity.Transforms.LocalTransform>(out var cameraTransform))
            {
                cameraPosition = cameraTransform.Position;
            }

            // Get LOD config or use defaults
            var lodConfig = SystemAPI.HasSingleton<LODConfig>()
                ? SystemAPI.GetSingleton<LODConfig>()
                : new LODConfig
                {
                    DistanceThresholds = new float4(50f, 100f, 200f, 500f),
                    UpdateStrides = new uint4(1, 5, 10, 20)
                };

            // Assign LOD levels based on distance
            foreach (var (transform, lod, entity) in SystemAPI.Query<
                         RefRO<LocalTransform>,
                         RefRW<LODComponent>>()
                         .WithEntityAccess())
            {
                var position = transform.ValueRO.Position;
                var distance = math.distance(position, cameraPosition);
                var lodValue = lod.ValueRO;

                // Determine LOD level from distance
                byte lodLevel = 0;
                uint updateStride = lodConfig.UpdateStrides.x;
                
                if (distance >= lodConfig.DistanceThresholds.w)
                {
                    lodLevel = 3;
                    updateStride = lodConfig.UpdateStrides.w;
                }
                else if (distance >= lodConfig.DistanceThresholds.z)
                {
                    lodLevel = 2;
                    updateStride = lodConfig.UpdateStrides.z;
                }
                else if (distance >= lodConfig.DistanceThresholds.y)
                {
                    lodLevel = 1;
                    updateStride = lodConfig.UpdateStrides.y;
                }
                else
                {
                    lodLevel = 0;
                    updateStride = lodConfig.UpdateStrides.x;
                }

                lodValue.LODLevel = lodLevel;
                lodValue.DistanceToCamera = distance;
                lodValue.UpdateStride = updateStride;
                lodValue.LastUpdateTick = currentTick;
                lod.ValueRW = lodValue;
            }
        }
    }
}

