using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using PureDOTS.Systems.Movement;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Systems
{
    /// <summary>
    /// Bridges MovementState.Vel to Space4X ship transform/physics.
    /// Full 3D kinematics for space combat.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementIntegrateSystem))]
    public partial struct Space4XMovementAdapter : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var deltaTime = timeState.DeltaTime;

            var job = new Space4XMovementAdapterJob
            {
                DeltaTime = deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct Space4XMovementAdapterJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(
                Entity entity,
                in MovementState movementState,
                ref LocalTransform transform,
                ref VelocitySample velocity)
            {
                // Update velocity sample for other systems (targeting, etc.)
                velocity.Velocity = movementState.Vel;
                velocity.LastPosition = transform.Position;

                // Update transform rotation to face velocity direction (if moving)
                float velLength = math.length(movementState.Vel);
                if (velLength > 1e-6f)
                {
                    float3 forward = math.normalize(movementState.Vel);
                    transform.Rotation = quaternion.LookRotationSafe(forward, math.up());
                }

                // Position is already updated by MovementIntegrateSystem
                // This adapter can add Space4X-specific effects (thruster particles, etc.)
            }
        }
    }
}

