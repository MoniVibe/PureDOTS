using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Navigation;
using PureDOTS.Runtime.Villager;
using PureDOTS.Systems.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Moves villagers toward their current target positions with simple steering.
    /// Integrates with flow field navigation when available.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup))]
    [UpdateAfter(typeof(VillagerTargetingSystem))]
    [UpdateAfter(typeof(FlowFieldFollowSystem))]
    public partial struct VillagerMovementSystem : ISystem
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
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Get villager behavior config or use defaults
            var config = SystemAPI.HasSingleton<VillagerBehaviorConfig>()
                ? SystemAPI.GetSingleton<VillagerBehaviorConfig>()
                : VillagerBehaviorConfig.CreateDefaults();

            var job = new UpdateVillagerMovementJob
            {
                DeltaTime = timeState.FixedDeltaTime,
                CurrentTick = timeState.Tick,
                ArrivalDistance = config.ArrivalDistance,
                FleeSpeedMultiplier = config.FleeSpeedMultiplier,
                LowEnergySpeedMultiplier = config.LowEnergySpeedMultiplier,
                LowEnergyThreshold = config.LowEnergyThreshold,
                VelocityThreshold = config.VelocityThreshold,
                RotationSpeed = config.RotationSpeed
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct UpdateVillagerMovementJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;
            public float ArrivalDistance;
            public float FleeSpeedMultiplier;
            public float LowEnergySpeedMultiplier;
            public float LowEnergyThreshold;
            public float VelocityThreshold;
            public float RotationSpeed;

            public void Execute(
                ref VillagerMovement movement,
                ref LocalTransform transform,
                in VillagerAIState aiState,
                in VillagerNeeds needs,
                [ChunkIndexInQuery] int chunkIndex)
            {
                // Check if flow field navigation is available
                float3 direction = float3.zero;
                bool useFlowField = false;

                // Try to use flow field if available (checked via optional component)
                // FlowFieldFollowSystem will have already set movement.Velocity if agent has FlowFieldAgentTag
                // Otherwise fall back to direct targeting

                if (aiState.TargetPosition.Equals(float3.zero) || aiState.TargetEntity == Entity.Null)
                {
                    // If flow field didn't set velocity, stop
                    if (math.lengthsq(movement.Velocity) < 0.01f)
                    {
                        movement.Velocity = float3.zero;
                        movement.IsMoving = 0;
                        return;
                    }
                    // Otherwise continue with flow field direction
                    useFlowField = true;
                }
                else
                {
                    var toTarget = aiState.TargetPosition - transform.Position;
                    var distance = math.length(toTarget);

                    if (distance <= ArrivalDistance)
                    {
                        movement.Velocity = float3.zero;
                        movement.IsMoving = 0;
                        return;
                    }

                    direction = math.normalize(toTarget);
                }

                // Apply speed multipliers
                var speedMultiplier = 1f;
                if (aiState.CurrentState == VillagerAIState.State.Fleeing)
                {
                    speedMultiplier = FleeSpeedMultiplier;
                }
                else if (needs.EnergyFloat < LowEnergyThreshold)
                {
                    speedMultiplier = LowEnergySpeedMultiplier;
                }

                // If not using flow field, compute velocity from direction
                if (!useFlowField && math.lengthsq(direction) > 0.01f)
                {
                    movement.CurrentSpeed = movement.BaseSpeed * speedMultiplier;
                    movement.Velocity = direction * movement.CurrentSpeed;
                }
                else if (useFlowField)
                {
                    // Flow field already set velocity, just apply speed multiplier
                    if (math.lengthsq(movement.Velocity) > 0.01f)
                    {
                        movement.CurrentSpeed = movement.BaseSpeed * speedMultiplier;
                        movement.Velocity = math.normalize(movement.Velocity) * movement.CurrentSpeed;
                    }
                }

                // Apply movement
                if (math.lengthsq(movement.Velocity) > 0.01f)
                {
                    transform.Position += movement.Velocity * DeltaTime;

                    var moveDirection = math.normalize(movement.Velocity);
                    movement.DesiredRotation = quaternion.LookRotationSafe(moveDirection, math.up());
                    transform.Rotation = math.slerp(transform.Rotation, movement.DesiredRotation, DeltaTime * RotationSpeed);
                    movement.IsMoving = 1;
                }
                else
                {
                    movement.IsMoving = 0;
                }

                movement.LastMoveTick = CurrentTick;
            }
        }
    }
}
