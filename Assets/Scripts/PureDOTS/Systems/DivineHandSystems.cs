using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(HandSystemGroup))]
    public partial struct DivineHandSystem : ISystem
    {
        private EntityQuery _handQuery;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<HandPickable> _pickableLookup;
        private ComponentLookup<HandHeldTag> _heldLookup;
        private ComponentLookup<RainCloudState> _rainCloudStateLookup;
        private ComponentLookup<ResourceTypeId> _resourceTypeIdLookup;
        private ComponentLookup<StorehouseInventory> _storehouseInventoryLookup;
        private BufferLookup<StorehouseInventoryItem> _storeItemsLookup;
        private BufferLookup<StorehouseCapacityElement> _storeCapacityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _handQuery = SystemAPI.QueryBuilder()
                .WithAllRW<DivineHandState>()
                .WithAllRW<HandInteractionState>()
                .WithAllRW<ResourceSiphonState>()
                .WithAllRW<DivineHandEvent>()
                .WithAllRW<DivineHandCommand>()
                .WithAll<DivineHandTag, DivineHandConfig, DivineHandInput>()
                .Build();

            state.RequireForUpdate(_handQuery);
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(false);
            _pickableLookup = state.GetComponentLookup<HandPickable>(true);
            _heldLookup = state.GetComponentLookup<HandHeldTag>(true);
            _rainCloudStateLookup = state.GetComponentLookup<RainCloudState>(false);
            _resourceTypeIdLookup = state.GetComponentLookup<ResourceTypeId>(true);
            _storehouseInventoryLookup = state.GetComponentLookup<StorehouseInventory>(false);
            _storeItemsLookup = state.GetBufferLookup<StorehouseInventoryItem>(false);
            _storeCapacityLookup = state.GetBufferLookup<StorehouseCapacityElement>(true);

            state.RequireForUpdate<ResourceTypeIndex>();
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
            uint currentTick = timeState.Tick;
            float deltaTime = SystemAPI.Time.DeltaTime;

            _transformLookup.Update(ref state);
            _pickableLookup.Update(ref state);
            _heldLookup.Update(ref state);
            _rainCloudStateLookup.Update(ref state);
            _resourceTypeIdLookup.Update(ref state);
            _storehouseInventoryLookup.Update(ref state);
            _storeItemsLookup.Update(ref state);
            _storeCapacityLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var resourceCatalog = SystemAPI.GetSingleton<ResourceTypeIndex>();
            var catalogRef = resourceCatalog.Catalog;

            foreach (var (handState, handConfigRO, handInputRO, commandRef, interactionRef, siphonRef, eventBuffer, entity) in
                     SystemAPI.Query<RefRW<DivineHandState>, RefRO<DivineHandConfig>, RefRO<DivineHandInput>, RefRW<DivineHandCommand>, RefRW<HandInteractionState>, RefRW<ResourceSiphonState>, DynamicBuffer<DivineHandEvent>>()
                         .WithEntityAccess())
            {
                ref var stateData = ref handState.ValueRW;
                var config = handConfigRO.ValueRO;
                var input = handInputRO.ValueRO;
                ref var command = ref commandRef.ValueRW;
                var events = eventBuffer;

                events.Clear();

                var previousState = stateData.CurrentState;
                var previousResourceType = stateData.HeldResourceTypeIndex;
                int previousAmount = stateData.HeldAmount;

                stateData.HeldCapacity = math.max(1, config.HeldCapacity);
                stateData.CursorPosition = input.CursorPosition;
                stateData.AimDirection = math.normalizesafe(input.AimDirection, new float3(0f, -1f, 0f));

                if (command.Type != DivineHandCommandType.None)
                {
                    command.TimeSinceIssued += deltaTime;
                }
                else
                {
                    command.TimeSinceIssued = 0f;
                }

                if (stateData.CooldownTimer > 0f)
                {
                    stateData.CooldownTimer = math.max(0f, stateData.CooldownTimer - deltaTime);
                }

                float maxChargeWindow = math.max(config.MinChargeSeconds, config.MaxChargeSeconds);
                stateData.ChargeTimer = maxChargeWindow > 0f
                    ? math.clamp(input.ThrowCharge, 0f, maxChargeWindow)
                    : input.ThrowCharge;

                bool hasHeldEntity = stateData.HeldEntity != Entity.Null && entityManager.Exists(stateData.HeldEntity);

                if (input.GrabPressed != 0 && !hasHeldEntity && stateData.CooldownTimer <= 0f)
                {
                    var candidate = FindPickable(ref state, stateData.CursorPosition, config);
                    if (candidate != Entity.Null)
                    {
                        bool accept = true;
                        if (catalogRef.IsCreated && _resourceTypeIdLookup.HasComponent(candidate))
                        {
                            var typeId = _resourceTypeIdLookup[candidate].Value;
                            int resolvedIndex = catalogRef.Value.LookupIndex(typeId);
                            if (resolvedIndex >= 0 && resolvedIndex <= ushort.MaxValue)
                            {
                                var candidateIndex = (ushort)resolvedIndex;
                                if (stateData.HeldResourceTypeIndex != DivineHandConstants.NoResourceType &&
                                    stateData.HeldResourceTypeIndex != candidateIndex)
                                {
                                    accept = false;
                                }
                                else
                                {
                                    stateData.HeldResourceTypeIndex = candidateIndex;
                                }
                            }
                        }

                        if (accept)
                        {
                            stateData.HeldEntity = candidate;
                            stateData.HeldLocalOffset = float3.zero;
                            stateData.HeldAmount = 1;
                            hasHeldEntity = true;
                            ecb.AddComponent(candidate, new HandHeldTag { Holder = entity });
                        }
                    }
                }

                if (hasHeldEntity && !_transformLookup.HasComponent(stateData.HeldEntity))
                {
                    stateData.HeldEntity = Entity.Null;
                    hasHeldEntity = false;
                }

                if (command.Type == DivineHandCommandType.SiphonPile && config.SiphonRate > 0f)
                {
                    if (stateData.HeldAmount < stateData.HeldCapacity)
                    {
                        float potentialUnits = command.TimeSinceIssued * config.SiphonRate;
                        int addUnits = (int)math.min(potentialUnits, stateData.HeldCapacity - stateData.HeldAmount);
                        if (addUnits > 0)
                        {
                            stateData.HeldAmount += addUnits;
                            command.TimeSinceIssued -= addUnits / config.SiphonRate;
                        }
                        if (stateData.HeldAmount >= stateData.HeldCapacity)
                        {
                            command.Type = DivineHandCommandType.None;
                            command.TargetEntity = Entity.Null;
                        }
                    }
                    else
                    {
                        command.Type = DivineHandCommandType.None;
                        command.TargetEntity = Entity.Null;
                    }
                }

                if ((command.Type == DivineHandCommandType.DumpToStorehouse || command.Type == DivineHandCommandType.GroundDrip) && config.DumpRate > 0f)
                {
                    if (stateData.HeldAmount > 0 && stateData.HeldResourceTypeIndex != DivineHandConstants.NoResourceType)
                    {
                        float potentialUnits = command.TimeSinceIssued * config.DumpRate;
                        int desiredUnits = (int)math.min(potentialUnits, stateData.HeldAmount);
                        if (desiredUnits > 0)
                        {
                            int deposited = DepositToStorehouse(ref state, command.TargetPosition, stateData.HeldResourceTypeIndex, desiredUnits, catalogRef, currentTick);
                            if (deposited > 0)
                            {
                                stateData.HeldAmount -= deposited;
                                command.TimeSinceIssued -= deposited / config.DumpRate;
                                if (stateData.HeldAmount <= 0)
                                {
                                    stateData.HeldAmount = 0;
                                    stateData.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
                                }
                            }
                            else
                            {
                                command.Type = DivineHandCommandType.None;
                                command.TargetEntity = Entity.Null;
                            }
                        }
                    }

                    if (stateData.HeldAmount <= 0)
                    {
                        command.Type = DivineHandCommandType.None;
                        command.TargetEntity = Entity.Null;
                    }
                }

                if (!hasHeldEntity)
                {
                    stateData.HeldEntity = Entity.Null;
                    stateData.HeldLocalOffset = float3.zero;
                    if (stateData.HeldAmount <= 0)
                    {
                        stateData.HeldAmount = 0;
                        stateData.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
                    }
                    stateData.ChargeTimer = 0f;
                }
                else
                {
                    MaintainHeldTransform(ref stateData, in config);

                    bool releaseRequested = input.GrabReleased != 0 || input.ThrowPressed != 0;
                    if (releaseRequested)
                    {
                        bool appliedThrow = ReleaseHeldEntity(ref ecb, ref stateData, in config, in input);
                        hasHeldEntity = false;
                        stateData.HeldAmount = 0;
                        stateData.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
                        stateData.CooldownTimer = math.max(stateData.CooldownTimer, appliedThrow ? config.CooldownAfterThrowSeconds : 0f);
                    }
                }

                if (command.Type == DivineHandCommandType.SiphonPile && stateData.HeldAmount > 0)
                {
                    command.Type = DivineHandCommandType.None;
                }

                if ((command.Type == DivineHandCommandType.DumpToStorehouse || command.Type == DivineHandCommandType.GroundDrip) &&
                    stateData.HeldAmount <= 0 && !hasHeldEntity)
                {
                    command.Type = DivineHandCommandType.None;
                }

                bool hasCargo = hasHeldEntity || stateData.HeldAmount > 0;

                stateData.Flags = hasCargo ? (byte)(stateData.Flags | 0x1) : (byte)(stateData.Flags & 0xFE);

                bool slingshotAimActive = hasCargo && config.MinChargeSeconds > 0f &&
                                           stateData.ChargeTimer >= config.MinChargeSeconds &&
                                           input.ThrowPressed == 0 && input.GrabReleased == 0;

                HandState nextState;
                if (command.Type == DivineHandCommandType.DumpToStorehouse || command.Type == DivineHandCommandType.GroundDrip)
                {
                    nextState = HandState.Dumping;
                }
                else if (command.Type == DivineHandCommandType.SiphonPile)
                {
                    nextState = HandState.Dragging;
                }
                else if (slingshotAimActive)
                {
                    nextState = HandState.SlingshotAim;
                }
                else if (hasCargo)
                {
                    nextState = HandState.Holding;
                }
                else
                {
                    nextState = HandState.Empty;
                }

                ref var interaction = ref interactionRef.ValueRW;
                interaction.HandEntity = entity;
                interaction.PreviousState = previousState;
                interaction.CurrentState = nextState;
                interaction.ActiveCommand = command.Type;
                interaction.ActiveResourceType = stateData.HeldResourceTypeIndex;
                interaction.HeldAmount = stateData.HeldAmount;
                interaction.HeldCapacity = stateData.HeldCapacity;
                interaction.CooldownSeconds = stateData.CooldownTimer;
                interaction.LastUpdateTick = currentTick;
                interaction.Flags = 0;
                if (command.Type == DivineHandCommandType.SiphonPile)
                {
                    interaction.Flags |= HandInteractionState.FlagSiphoning;
                }
                if (command.Type == DivineHandCommandType.DumpToStorehouse)
                {
                    interaction.Flags |= HandInteractionState.FlagDumping;
                }

                ref var siphonState = ref siphonRef.ValueRW;
                siphonState.HandEntity = entity;
                siphonState.TargetEntity = command.TargetEntity;
                siphonState.ResourceTypeIndex = stateData.HeldResourceTypeIndex;
                siphonState.SiphonRate = config.SiphonRate;
                siphonState.DumpRate = config.DumpRate;
                siphonState.LastUpdateTick = currentTick;
                siphonState.Flags = 0;
                if (command.Type == DivineHandCommandType.SiphonPile)
                {
                    siphonState.Flags |= ResourceSiphonState.FlagSiphoning;
                }
                if (command.Type == DivineHandCommandType.DumpToStorehouse)
                {
                    siphonState.Flags |= ResourceSiphonState.FlagDumpCommandPending;
                }

                if (nextState != stateData.CurrentState)
                {
                    events.Add(DivineHandEvent.StateChange(stateData.CurrentState, nextState));
                    stateData.PreviousState = stateData.CurrentState;
                    stateData.CurrentState = nextState;
                }

                if (previousResourceType != stateData.HeldResourceTypeIndex)
                {
                    events.Add(DivineHandEvent.TypeChange(stateData.HeldResourceTypeIndex));
                }

                if (previousAmount != stateData.HeldAmount)
                {
                    events.Add(DivineHandEvent.AmountChange(stateData.HeldAmount, stateData.HeldCapacity));
                }

                commandRef.ValueRW = command;
            }

            ecb.Playback(entityManager);
        }

        private void MaintainHeldTransform(ref DivineHandState state, in DivineHandConfig config)
        {
            if (state.HeldEntity == Entity.Null || !_transformLookup.HasComponent(state.HeldEntity))
            {
                return;
            }

            var heldTransform = _transformLookup[state.HeldEntity];
            float targetHeight = math.max(config.HoldHeightOffset, state.CursorPosition.y);
            var targetPosition = state.CursorPosition;
            targetPosition.y = targetHeight;

            float followLerp = config.HoldLerp;
            if (_pickableLookup.HasComponent(state.HeldEntity))
            {
                followLerp = math.clamp(_pickableLookup[state.HeldEntity].FollowLerp, 0.01f, 1f);
            }

            heldTransform.Position = math.lerp(heldTransform.Position, targetPosition, followLerp);
            heldTransform.Rotation = quaternion.identity;
            _transformLookup[state.HeldEntity] = heldTransform;

            if (_rainCloudStateLookup.HasComponent(state.HeldEntity))
            {
                var rainState = _rainCloudStateLookup[state.HeldEntity];
                rainState.Velocity = float3.zero;
                _rainCloudStateLookup[state.HeldEntity] = rainState;
            }
        }

        private Entity FindPickable(ref SystemState state, float3 cursorPosition, DivineHandConfig config)
        {
            Entity bestEntity = Entity.Null;
            float bestDistanceSq = config.PickupRadius * config.PickupRadius;

            foreach (var (pickable, transform, entity) in SystemAPI.Query<RefRO<HandPickable>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (_heldLookup.HasComponent(entity))
                {
                    continue;
                }

                float3 position = transform.ValueRO.Position;
                float distanceSq = math.lengthsq(position - cursorPosition);
                if (distanceSq > bestDistanceSq)
                {
                    continue;
                }

                if (config.MaxGrabDistance > 0f)
                {
                    float vertical = math.abs(position.y - cursorPosition.y);
                    if (vertical > config.MaxGrabDistance)
                    {
                        continue;
                    }
                }

                bestDistanceSq = distanceSq;
                bestEntity = entity;
            }

            return bestEntity;
        }

        private bool ReleaseHeldEntity(ref EntityCommandBuffer ecb,
            ref DivineHandState state,
            in DivineHandConfig config,
            in DivineHandInput input)
        {
            if (state.HeldEntity == Entity.Null)
            {
                return false;
            }

            bool appliedThrow = input.ThrowPressed != 0 && state.ChargeTimer >= math.max(0f, config.MinChargeSeconds);

            if (_heldLookup.HasComponent(state.HeldEntity))
            {
                ecb.RemoveComponent<HandHeldTag>(state.HeldEntity);
            }

            float baseImpulse = config.ThrowImpulse;
            float minCharge = config.MinChargeSeconds;
            float maxCharge = math.max(minCharge, config.MaxChargeSeconds);
            float chargeDuration = state.ChargeTimer;

            float normalizedCharge;
            if (maxCharge > minCharge && maxCharge > 0f)
            {
                float clamped = math.clamp(chargeDuration, minCharge, maxCharge);
                normalizedCharge = math.saturate((clamped - minCharge) / math.max(0.0001f, maxCharge - minCharge));
            }
            else if (maxCharge > 0f)
            {
                normalizedCharge = math.saturate(chargeDuration / math.max(0.0001f, maxCharge));
            }
            else
            {
                normalizedCharge = math.saturate(chargeDuration);
            }

            float chargeMultiplier = math.max(1f, 1f + normalizedCharge * config.ThrowChargeMultiplier);

            if (appliedThrow && _rainCloudStateLookup.HasComponent(state.HeldEntity))
            {
                var rainState = _rainCloudStateLookup[state.HeldEntity];
                rainState.Velocity = math.normalizesafe(input.AimDirection, new float3(0f, -1f, 0f)) * baseImpulse * chargeMultiplier;
                _rainCloudStateLookup[state.HeldEntity] = rainState;
            }

            state.HeldEntity = Entity.Null;
            state.HeldLocalOffset = float3.zero;
            state.ChargeTimer = 0f;
            state.Flags &= 0xFE;

            return appliedThrow;
        }

        private int DepositToStorehouse(ref SystemState state, float3 targetPosition, ushort resourceTypeIndex, int units, BlobAssetReference<ResourceTypeIndexBlob> catalog, uint currentTick)
        {
            if (!catalog.IsCreated || units <= 0)
            {
                return 0;
            }

            ref var resourceIds = ref catalog.Value.Ids;
            if (resourceTypeIndex >= resourceIds.Length)
            {
                return 0;
            }

            var resourceId = resourceIds[resourceTypeIndex];
            Entity bestStorehouse = Entity.Null;
            float bestDistanceSq = float.MaxValue;

            foreach (var (transform, storehouseEntity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<StorehouseInventory>()
                         .WithEntityAccess())
            {
                if (!_storeCapacityLookup.HasBuffer(storehouseEntity) || !_storeItemsLookup.HasBuffer(storehouseEntity) || !_storehouseInventoryLookup.HasComponent(storehouseEntity))
                {
                    continue;
                }

                var capacities = _storeCapacityLookup[storehouseEntity];
                if (FindCapacityIndex(capacities, resourceId) < 0)
                {
                    continue;
                }

                float distanceSq = math.lengthsq(transform.ValueRO.Position - targetPosition);
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestStorehouse = storehouseEntity;
                }
            }

            if (bestStorehouse == Entity.Null)
            {
                return 0;
            }

            var capacityBuffer = _storeCapacityLookup[bestStorehouse];
            int capacityIndex = FindCapacityIndex(capacityBuffer, resourceId);
            if (capacityIndex < 0)
            {
                return 0;
            }

            float maxCapacity = capacityBuffer[capacityIndex].MaxCapacity;
            var itemsBuffer = _storeItemsLookup[bestStorehouse];
            int itemIndex = FindInventoryIndex(itemsBuffer, resourceId);
            float currentAmount = itemIndex >= 0 ? itemsBuffer[itemIndex].Amount : 0f;
            float available = math.max(0f, maxCapacity - currentAmount);
            if (available <= 0f)
            {
                return 0;
            }

            int depositUnits = math.min(units, (int)math.floor(available));
            if (depositUnits <= 0)
            {
                return 0;
            }

            if (itemIndex >= 0)
            {
                var item = itemsBuffer[itemIndex];
                item.Amount += depositUnits;
                itemsBuffer[itemIndex] = item;
            }
            else
            {
                itemsBuffer.Add(new StorehouseInventoryItem
                {
                    ResourceTypeId = resourceId,
                    Amount = depositUnits,
                    Reserved = 0f
                });
            }

            if (_storehouseInventoryLookup.HasComponent(bestStorehouse))
            {
                var inventory = _storehouseInventoryLookup[bestStorehouse];
                inventory.TotalStored += depositUnits;
                inventory.LastUpdateTick = currentTick;
                _storehouseInventoryLookup[bestStorehouse] = inventory;
            }

            return depositUnits;
        }

        private static int FindCapacityIndex(DynamicBuffer<StorehouseCapacityElement> capacities, FixedString64Bytes resourceId)
        {
            for (int i = 0; i < capacities.Length; i++)
            {
                if (capacities[i].ResourceTypeId.Equals(resourceId))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FindInventoryIndex(DynamicBuffer<StorehouseInventoryItem> items, FixedString64Bytes resourceId)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].ResourceTypeId.Equals(resourceId))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
