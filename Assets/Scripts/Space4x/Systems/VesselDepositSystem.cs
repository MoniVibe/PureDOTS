using PureDOTS.Runtime.Components;
using Space4X.Runtime;
using Space4X.Runtime.Transport;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Systems
{
    /// <summary>
    /// Handles vessels depositing resources to carriers when they return.
    /// Similar to ResourceDepositSystem but for vessels and carriers.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PureDOTS.Systems.ResourceSystemGroup))]
    public partial struct VesselDepositSystem : ISystem
    {
        private ComponentLookup<Carrier> _carrierLookup;
        private BufferLookup<CarrierInventoryItem> _carrierInventoryLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _carrierLookup = state.GetComponentLookup<Carrier>(false);
            _carrierInventoryLookup = state.GetBufferLookup<CarrierInventoryItem>(false);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<MinerVessel>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _carrierLookup.Update(ref state);
            _carrierInventoryLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var depositDistance = 2f; // Vessels deposit when within 2 units of carrier
            var depositDistanceSq = depositDistance * depositDistance;

            foreach (var (vessel, aiState, transform, entity) in SystemAPI.Query<RefRW<MinerVessel>, RefRW<VesselAIState>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                // Only deposit if returning and has cargo
                if (aiState.ValueRO.CurrentState != VesselAIState.State.Returning ||
                    vessel.ValueRO.Load <= 0f ||
                    aiState.ValueRO.TargetEntity == Entity.Null)
                {
                    continue;
                }

                // Check if target is a carrier
                if (!_carrierLookup.HasComponent(aiState.ValueRO.TargetEntity) ||
                    !_transformLookup.HasComponent(aiState.ValueRO.TargetEntity))
                {
                    continue;
                }

                var carrierTransform = _transformLookup[aiState.ValueRO.TargetEntity];
                var distSq = math.distancesq(transform.ValueRO.Position, carrierTransform.Position);

                if (distSq > depositDistanceSq)
                {
                    // Not close enough yet - keep moving toward carrier
                    continue;
                }

                // Arrived at carrier - stop moving
                // (VesselMovementSystem will handle stopping when in Returning state, but we ensure it here)

                // Close enough to deposit
                var carrier = _carrierLookup[aiState.ValueRO.TargetEntity];
                var cargoToDeposit = vessel.ValueRO.Load;

                // Check carrier capacity
                var capacityRemaining = carrier.TotalCapacity - carrier.CurrentLoad;
                if (capacityRemaining <= 0f)
                {
                    // Carrier is full, find another carrier or wait
                    // For now, just keep waiting
                    continue;
                }

                var depositAmount = math.min(cargoToDeposit, capacityRemaining);

                // Add to carrier inventory
                if (!_carrierInventoryLookup.HasBuffer(aiState.ValueRO.TargetEntity))
                {
                    continue;
                }

                var inventory = _carrierInventoryLookup[aiState.ValueRO.TargetEntity];
                var resourceIndex = -1;

                // Find existing inventory item for this resource type
                for (int i = 0; i < inventory.Length; i++)
                {
                    if (inventory[i].ResourceTypeIndex == vessel.ValueRO.ResourceTypeIndex)
                    {
                        resourceIndex = i;
                        break;
                    }
                }

                if (resourceIndex >= 0)
                {
                    var item = inventory[resourceIndex];
                    item.Amount += depositAmount;
                    inventory[resourceIndex] = item;
                }
                else
                {
                    // Add new inventory item
                    inventory.Add(new CarrierInventoryItem
                    {
                        ResourceTypeIndex = vessel.ValueRO.ResourceTypeIndex,
                        Amount = depositAmount
                    });
                }

                // Update carrier load
                carrier.CurrentLoad += depositAmount;
                carrier.LastUpdateTick = timeState.Tick;
                _carrierLookup[aiState.ValueRO.TargetEntity] = carrier;

                // Update vessel load
                var vesselValue = vessel.ValueRO;
                vesselValue.Load -= depositAmount;
                vesselValue.LastCommandTick = timeState.Tick;

                // If vessel is empty, return to idle to find new target
                if (vesselValue.Load <= 0.1f)
                {
                    vesselValue.Load = 0f;
                    vesselValue.Flags &= ~TransportUnitFlags.Carrying;
                    vesselValue.Flags |= TransportUnitFlags.Idle;

                    aiState.ValueRW.CurrentState = VesselAIState.State.Idle;
                    aiState.ValueRW.CurrentGoal = VesselAIState.Goal.Idle;
                    aiState.ValueRW.TargetEntity = Entity.Null;
                    aiState.ValueRW.TargetPosition = float3.zero;
                }

                vessel.ValueRW = vesselValue;
            }
        }
    }
}

