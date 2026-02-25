using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using Unity.Entities;
using Unity.Transforms;

namespace PureDOTS.Systems.Movement
{
    /// <summary>
    /// Captures canonical movement pose for MovementKernelOwned entities after movement integration.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementIntegrateSystem))]
    [UpdateAfter(typeof(MovementModeSystem))]
    public partial struct MovementKernelPoseCaptureSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<MovementKernelOwned>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var tick = SystemAPI.GetSingleton<TimeState>().Tick;
            foreach (var (transform, pose) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<MovementKernelPose>>().WithAll<MovementKernelOwned>())
            {
                pose.ValueRW.Position = transform.ValueRO.Position;
                pose.ValueRW.Rotation = transform.ValueRO.Rotation;
                pose.ValueRW.Scale = transform.ValueRO.Scale;
                pose.ValueRW.CapturedTick = tick;
            }
        }
    }
}
