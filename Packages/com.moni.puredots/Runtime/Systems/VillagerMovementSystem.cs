using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villager;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Moves villagers toward their current target positions with simple steering.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup))]
    [UpdateAfter(typeof(VillagerTargetingSystem))]
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

            public void Execute(ref VillagerMovement movement, ref LocalTransform transform, in VillagerAIState aiState, in VillagerNeeds needs)
            {
                if (aiState.TargetPosition.Equals(float3.zero) || aiState.TargetEntity == Entity.Null)
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = 0;
                    return;
                }

                var toTarget = aiState.TargetPosition - transform.Position;
                var distance = math.length(toTarget);

                if (distance <= ArrivalDistance)
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = 0;
                    return;
                }

                var direction = math.normalize(toTarget);
                var speedMultiplier = 1f;

                if (aiState.CurrentState == VillagerAIState.State.Fleeing)
                {
                    speedMultiplier = FleeSpeedMultiplier;
                }
                else if (needs.Energy < LowEnergyThreshold)
                {
                    speedMultiplier = LowEnergySpeedMultiplier;
                }

                movement.CurrentSpeed = movement.BaseSpeed * speedMultiplier;
                movement.Velocity = direction * movement.CurrentSpeed;
                transform.Position += movement.Velocity * DeltaTime;

                if (math.lengthsq(movement.Velocity) > VelocityThreshold)
                {
                    movement.DesiredRotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.Rotation = math.slerp(transform.Rotation, movement.DesiredRotation, DeltaTime * RotationSpeed);
                }

                movement.IsMoving = 1;
                movement.LastMoveTick = CurrentTick;
            }
        }
    }
}
