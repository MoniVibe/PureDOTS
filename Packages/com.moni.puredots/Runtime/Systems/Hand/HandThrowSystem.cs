using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using PureDOTS.Runtime.Interaction;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using HandStateData = PureDOTS.Runtime.Hand.HandState;
using Unity.Transforms;

namespace PureDOTS.Systems.Hand
{
    /// <summary>
    /// Applies constant-speed throws for immediate (non-slingshot) releases.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(HandHoldFollowSystem))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial struct HandThrowSystem : ISystem
    {
        private ComponentLookup<PhysicsVelocity> _velocityLookup;
        private ComponentLookup<PhysicsGravityFactor> _gravityLookup;
        private ComponentLookup<HandHeldTag> _heldLookup;
        private ComponentLookup<MovementSuppressed> _movementLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<BeingThrown> _beingThrownLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(false);
            _gravityLookup = state.GetComponentLookup<PhysicsGravityFactor>(false);
            _heldLookup = state.GetComponentLookup<HandHeldTag>(false);
            _movementLookup = state.GetComponentLookup<MovementSuppressed>(false);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _beingThrownLookup = state.GetComponentLookup<BeingThrown>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            _velocityLookup.Update(ref state);
            _gravityLookup.Update(ref state);
            _heldLookup.Update(ref state);
            _movementLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _beingThrownLookup.Update(ref state);

            foreach (var (handStateRef, commandBuffer) in SystemAPI.Query<RefRW<HandStateData>, DynamicBuffer<HandCommand>>())
            {
                var handState = handStateRef.ValueRW;
                var buffer = commandBuffer;

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var cmd = buffer[i];
                    if (cmd.Tick != currentTick || cmd.Type != HandCommandType.Throw)
                    {
                        continue;
                    }

                    if (ApplyThrow(ref state, ref handState, cmd, ref ecb))
                    {
                        buffer.RemoveAt(i);
                    }
                }

                handStateRef.ValueRW = handState;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private bool ApplyThrow(ref SystemState state, ref HandStateData handState, HandCommand command, ref EntityCommandBuffer ecb)
        {
            var target = command.TargetEntity;
            if (target == Entity.Null)
            {
                return false;
            }

            if (!_velocityLookup.HasComponent(target))
            {
                return ReleaseWithoutThrow(ref handState, target, ref ecb);
            }

            var velocity = _velocityLookup[target];
            velocity.Linear = math.normalizesafe(command.Direction, new float3(0f, 1f, 0f)) * command.Speed;
            velocity.Angular = float3.zero;
            _velocityLookup[target] = velocity;

            if (_gravityLookup.HasComponent(target))
            {
                var gravity = _gravityLookup[target];
                gravity.Value = math.max(gravity.Value, 1f);
                _gravityLookup[target] = gravity;
            }

            if (_transformLookup.HasComponent(target))
            {
                var transform = _transformLookup[target];
                var thrown = new BeingThrown
                {
                    InitialVelocity = velocity.Linear,
                    TimeSinceThrow = 0f,
                    PrevPosition = transform.Position,
                    PrevRotation = transform.Rotation
                };

                if (_beingThrownLookup.HasComponent(target))
                {
                    ecb.SetComponent(target, thrown);
                }
                else
                {
                    ecb.AddComponent(target, thrown);
                }
            }

            if (_heldLookup.HasComponent(target))
            {
                ecb.RemoveComponent<HandHeldTag>(target);
            }

            if (_movementLookup.HasComponent(target))
            {
                ecb.RemoveComponent<MovementSuppressed>(target);
            }

            if (handState.HeldEntity == target)
            {
                handState.HeldEntity = Entity.Null;
                handState.CurrentState = HandStateType.Cooldown;
                handState.StateTimer = 0;
            }

            return true;
        }

        private bool ReleaseWithoutThrow(ref HandStateData handState, Entity target, ref EntityCommandBuffer ecb)
        {
            if (_heldLookup.HasComponent(target))
            {
                ecb.RemoveComponent<HandHeldTag>(target);
            }

            if (_movementLookup.HasComponent(target))
            {
                ecb.RemoveComponent<MovementSuppressed>(target);
            }

            if (handState.HeldEntity == target)
            {
                handState.HeldEntity = Entity.Null;
                handState.CurrentState = HandStateType.Cooldown;
                handState.StateTimer = 0;
            }

            return true;
        }
    }
}
