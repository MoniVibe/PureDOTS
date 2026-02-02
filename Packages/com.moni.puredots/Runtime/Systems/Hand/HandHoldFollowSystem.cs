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
using Unity.Transforms;
using HandStateData = PureDOTS.Runtime.Hand.HandState;

namespace PureDOTS.Systems.Hand
{
    /// <summary>
    /// Applies direct stasis positioning so held entities follow the hand exactly.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(HandPickupSystem))]
    [UpdateBefore(typeof(PhysicsInitializeGroup))]
    public partial struct HandHoldFollowSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<PhysicsVelocity> _velocityLookup;
        private ComponentLookup<WorldManipulableTag> _worldManipulableLookup;
        private ComponentLookup<HandHeldTag> _heldLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<HandInputFrame>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(false);
            _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(false);
            _worldManipulableLookup = state.GetComponentLookup<WorldManipulableTag>(true);
            _heldLookup = state.GetComponentLookup<HandHeldTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;
            var input = SystemAPI.GetSingleton<HandInputFrame>();
            var policy = new HandPickupPolicy
            {
                AutoPickDynamicPhysics = 0,
                EnableWorldGrab = 0,
                DebugWorldGrabAny = 0,
                WorldGrabRequiresTag = 1
            };
            if (SystemAPI.TryGetSingleton(out HandPickupPolicy policyValue))
            {
                policy = policyValue;
            }

            _transformLookup.Update(ref state);
            _velocityLookup.Update(ref state);
            _worldManipulableLookup.Update(ref state);
            _heldLookup.Update(ref state);

            foreach (var (handStateRef, commandBuffer) in SystemAPI.Query<RefRO<HandStateData>, DynamicBuffer<HandCommand>>())
            {
                var handState = handStateRef.ValueRO;
                var buffer = commandBuffer;

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var cmd = buffer[i];
                    if (cmd.Tick != currentTick || cmd.Type != HandCommandType.Hold)
                    {
                        continue;
                    }

                    if (ApplySpring(ref state, ref handState, cmd, input, policy))
                    {
                        buffer.RemoveAt(i);
                    }
                }
            }
        }

        private bool ApplySpring(ref SystemState state, ref HandStateData handState, HandCommand command, in HandInputFrame input, HandPickupPolicy policy)
        {
            var target = command.TargetEntity;
            if (target == Entity.Null || !_transformLookup.HasComponent(target))
            {
                return false;
            }

            var isHeld = _heldLookup.HasComponent(target);
            if (!isHeld && !_velocityLookup.HasComponent(target))
            {
                bool worldGrabActive = policy.EnableWorldGrab != 0 && input.CtrlHeld && input.ShiftHeld;
                bool allowWorldDrag = policy.EnableWorldGrab != 0 &&
                    (worldGrabActive || policy.DebugWorldGrabAny != 0 || _worldManipulableLookup.HasComponent(target));
                if (!allowWorldDrag)
                {
                    return false;
                }
            }

            var transform = _transformLookup[target];
            transform.Position = command.TargetPosition;
            _transformLookup[target] = transform;

            if (_velocityLookup.HasComponent(target))
            {
                var velocity = _velocityLookup[target];
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
                _velocityLookup[target] = velocity;
            }

            return true;
        }
    }
}
