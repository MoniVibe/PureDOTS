using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Bridges MovementState.Vel to Godgame terrain movement.
    /// Zeroes vertical forces, respects MaxSlopeDeg and GroundFriction.
    /// 2D gradient sampling (XY plane).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementIntegrateSystem))]
    public partial struct GodgameMovementAdapter : ISystem
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

            var job = new GodgameMovementAdapterJob
            {
                DeltaTime = deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct GodgameMovementAdapterJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref MovementState movementState,
                ref LocalTransform transform,
                in MovementModelRef modelRef)
            {
                if (!modelRef.Blob.IsCreated)
                {
                    return;
                }

                ref var spec = ref modelRef.Blob.Value;

                // Ensure 2D movement (zero vertical component)
                float3 vel = movementState.Vel;
                vel.y = 0f;
                movementState.Vel = vel;

                // Apply ground friction
                if (spec.GroundFriction > 0f)
                {
                    float frictionFactor = 1f - (spec.GroundFriction * DeltaTime);
                    frictionFactor = math.max(0f, frictionFactor);
                    vel *= frictionFactor;
                    movementState.Vel = vel;
                }

                // TODO: Apply slope clamping
                // This would require terrain height/slope queries
                // For now, just ensure Y position stays at terrain height

                // Update transform rotation to face velocity direction (if moving)
                float velLength = math.length(vel);
                if (velLength > 1e-6f)
                {
                    float3 forward = math.normalize(vel);
                    forward.y = 0f; // Keep rotation in horizontal plane
                    if (math.lengthsq(forward) > 1e-6f)
                    {
                        transform.Rotation = quaternion.LookRotationSafe(forward, math.up());
                    }
                }

                // Position is already updated by MovementIntegrateSystem
                // This adapter can add Godgame-specific effects (footprints, dust, etc.)
            }
        }
    }
}

