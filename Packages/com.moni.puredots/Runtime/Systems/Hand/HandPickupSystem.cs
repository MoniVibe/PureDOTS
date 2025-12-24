using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using PureDOTS.Runtime.Interaction;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using HandStateData = PureDOTS.Runtime.Hand.HandState;
using InteractionPickable = PureDOTS.Runtime.Interaction.Pickable;

namespace PureDOTS.Systems.Hand
{
    /// <summary>
    /// Handles pickup commands: attaches HandHeldTag, suppresses movement,
    /// zeros physics velocity, and updates HandState.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(HandCommandEmitterSystem))]
    public partial struct HandPickupSystem : ISystem
    {
        private ComponentLookup<PickableTag> _pickableTagLookup;
        private ComponentLookup<InteractionPickable> _pickableLookup;
        private ComponentLookup<HandHeldTag> _heldLookup;
        private ComponentLookup<PhysicsVelocity> _velocityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _pickableTagLookup = state.GetComponentLookup<PickableTag>(true);
            _pickableLookup = state.GetComponentLookup<InteractionPickable>(true);
            _heldLookup = state.GetComponentLookup<HandHeldTag>(false);
            _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            _pickableTagLookup.Update(ref state);
            _pickableLookup.Update(ref state);
            _heldLookup.Update(ref state);
            _velocityLookup.Update(ref state);

            foreach (var (handStateRef, commandBuffer, handEntity) in SystemAPI.Query<RefRW<HandStateData>, DynamicBuffer<HandCommand>>().WithEntityAccess())
            {
                var handState = handStateRef.ValueRW;
                var buffer = commandBuffer;

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var cmd = buffer[i];
                    if (cmd.Tick != currentTick || cmd.Type != HandCommandType.Pick)
                    {
                        continue;
                    }

                    if (ProcessPickCommand(ref state, handEntity, ref handState, cmd, ref ecb))
                    {
                        buffer.RemoveAt(i);
                    }
                }

                handStateRef.ValueRW = handState;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private bool ProcessPickCommand(ref SystemState state, Entity handEntity, ref HandStateData handState, HandCommand cmd, ref EntityCommandBuffer ecb)
        {
            var target = cmd.TargetEntity;
            if (target == Entity.Null || !state.EntityManager.Exists(target))
            {
                return false;
            }

            // Already held by someone else? skip
            if (_heldLookup.HasComponent(target))
            {
                return false;
            }

            // Require pickable tag/component if present
            bool hasPickable = _pickableTagLookup.HasComponent(target) || _pickableLookup.HasComponent(target);
            if (!hasPickable)
            {
                return false;
            }

            ecb.AddComponent(target, new HandHeldTag { Holder = handEntity });
            ecb.AddComponent<MovementSuppressed>(target);

            if (_velocityLookup.HasComponent(target))
            {
                var velocity = _velocityLookup[target];
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
                ecb.SetComponent(target, velocity);
            }

            handState.HeldEntity = target;
            handState.CurrentState = HandStateType.Holding;
            handState.StateTimer = 0;
            handState.HoldPoint = cmd.TargetPosition;
            handState.HoldDistance = math.max(handState.HoldDistance, 0f);

            return true;
        }
    }
}

