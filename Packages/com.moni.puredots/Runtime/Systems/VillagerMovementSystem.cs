using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Interaction;
using PureDOTS.Runtime.Movement;
using PureDOTS.Runtime.Navigation;
using PureDOTS.Runtime.Time;
using PureDOTS.Runtime.Villager;
using PureDOTS.Systems.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Moves villagers toward their current target positions with simple steering.
    /// Integrates with flow field navigation when available.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HotPathSystemGroup))]
    [UpdateAfter(typeof(VillagerTargetingSystem))]
    [UpdateAfter(typeof(FlowFieldFollowSystem))]
    public partial struct VillagerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            
            // Use TimeHelpers to check if we should update (handles pause, rewind, stasis)
            var defaultMembership = default(TimeBubbleMembership);
            if (!TimeHelpers.ShouldUpdate(timeState, rewindState, defaultMembership))
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
                RotationSpeed = config.RotationSpeed,
                TickTimeState = tickTimeState,
                TimeState = timeState,
                RewindState = rewindState,
                BubbleMembershipLookup = state.GetComponentLookup<TimeBubbleMembership>(true),
                MovementSuppressedLookup = state.GetComponentLookup<MovementSuppressed>(true)
            };
            
            state.Dependency = job.ScheduleParallel(state.Dependency);
            
            // Count frozen entities for debug log (outside Burst)
#if UNITY_EDITOR
            LogFrozenEntities(ref state);
#endif
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
            public TickTimeState TickTimeState;
            public TimeState TimeState;
            public RewindState RewindState;
            [ReadOnly] public ComponentLookup<TimeBubbleMembership> BubbleMembershipLookup;
            [ReadOnly] public ComponentLookup<MovementSuppressed> MovementSuppressedLookup;

            [Unity.Burst.CompilerServices.SkipLocalsInit]
            public void Execute(
                ref VillagerMovement movement,
                ref LocalTransform transform,
                in VillagerAIState aiState,
                in VillagerNeeds needs,
                in Entity entity,
                [ChunkIndexInQuery] int chunkIndex)
            {
                // Skip if movement is suppressed (e.g., being held by player)
                if (MovementSuppressedLookup.HasComponent(entity))
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = 0;
                    return;
                }
                
                // Get bubble membership for this entity (if any)
                var membership = BubbleMembershipLookup.HasComponent(entity)
                    ? BubbleMembershipLookup[entity]
                    : default(TimeBubbleMembership);
                
                // Gate movement by stasis - if entity is in stasis, don't move
                if (TimeHelpers.IsInStasis(membership))
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = 0;
                    return;
                }
                
                // Use TimeHelpers to check if we should update
                if (!TimeHelpers.ShouldUpdate(TimeState, RewindState, membership))
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = 0;
                    return;
                }
                
                // Get effective delta time (handles bubbles, pause, etc.)
                var effectiveDelta = TimeHelpers.GetEffectiveDelta(TickTimeState, TimeState, membership);
                if (effectiveDelta <= 0f)
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = 0;
                    return;
                }
                
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

                // Apply movement using effective delta time
                if (math.lengthsq(movement.Velocity) > 0.01f)
                {
                    transform.Position += movement.Velocity * effectiveDelta;

                    var moveDirection = math.normalize(movement.Velocity);
                    // Use current rotation's up vector for 3D-aware rotation
                    // For ground units, this preserves upright orientation
                    // For flying/space units, this maintains consistent roll
                    OrientationHelpers.DeriveUpFromRotation(transform.Rotation, OrientationHelpers.WorldUp, out var currentUp);
                    OrientationHelpers.LookRotationSafe3D(moveDirection, currentUp, out movement.DesiredRotation);
                    transform.Rotation = math.slerp(transform.Rotation, movement.DesiredRotation, effectiveDelta * RotationSpeed);
                    movement.IsMoving = 1;
                }
                else
                {
                    movement.IsMoving = 0;
                }

                movement.LastMoveTick = CurrentTick;
            }
        }
        
#if UNITY_EDITOR
        [BurstDiscard]
        private void LogFrozenEntities(ref SystemState state)
        {
            var frozenCount = 0;
            foreach (var (membership, _) in SystemAPI.Query<RefRO<TimeBubbleMembership>>().WithEntityAccess())
            {
                if (TimeHelpers.IsInStasis(membership.ValueRO))
                {
                    frozenCount++;
                }
            }
            if (frozenCount > 0)
            {
                UnityEngine.Debug.Log($"[Stasis] {frozenCount} entities frozen");
            }
        }
#endif
    }
}
