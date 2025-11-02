using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Camera
{
    /// <summary>
    /// Pure DOTS system that syncs CameraTransform singleton to the camera entity's LocalTransform.
    /// Runs after CameraControlSystem to ensure camera state is applied to rendering.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CameraControlSystem))]
    [BurstCompile]
    public partial struct CameraSyncSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Only require CameraTag - CameraTransform singleton might not exist yet
            state.RequireForUpdate<CameraTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get CameraTransform singleton
            if (!SystemAPI.TryGetSingleton<CameraTransform>(out var cameraTransform))
            {
                return;
            }

            // Sync to camera entity's LocalTransform
            foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<CameraTag>())
            {
                transform.ValueRW.Position = cameraTransform.Position;
                transform.ValueRW.Rotation = cameraTransform.Rotation;
                // Scale remains unchanged (defaults to 1.0)
            }
        }
    }
}

