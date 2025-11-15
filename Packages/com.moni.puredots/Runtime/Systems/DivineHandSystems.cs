using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Hand;
using PureDOTS.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

#pragma warning disable 0618 // IAspect is deprecated but retained until refactor lands

namespace PureDOTS.Systems
{
    public readonly partial struct DivineHandAspect : IAspect
    {
        public readonly Entity Entity;
        public readonly RefRW<DivineHandState> HandState;
        public readonly RefRO<DivineHandConfig> HandConfig;
        public readonly RefRO<DivineHandInput> HandInput;
        public readonly RefRW<DivineHandCommand> Command;
        public readonly RefRW<HandInteractionState> Interaction;
        public readonly DynamicBuffer<DivineHandEvent> Events;
        public readonly DynamicBuffer<HandQueuedThrowElement> QueuedEntries;
        public readonly DynamicBuffer<MiracleReleaseEvent> MiracleEvents;
        public readonly RefRO<GodIntent> Intent;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(HandSystemGroup))]
    public partial struct DivineHandSystem : ISystem
    {
        private const byte FlagHasCargo = 1 << 0;
        private const byte FlagSecondaryHeld = 1 << 1;
        private const byte FlagThrowModeSlingshot = 1 << 2;

        private EntityQuery _handQuery;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<HandPickable> _pickableLookup;
        private ComponentLookup<HandHeldTag> _heldLookup;
        private ComponentLookup<RainCloudState> _rainCloudStateLookup;
        private ComponentLookup<ResourceTypeId> _resourceTypeIdLookup;
        private ComponentLookup<StorehouseInventory> _storehouseInventoryLookup;
        private BufferLookup<StorehouseInventoryItem> _storeItemsLookup;
        private BufferLookup<StorehouseCapacityElement> _storeCapacityLookup;
        private ComponentLookup<HandQueuedTag> _queuedTagLookup;
        private ComponentLookup<MiracleToken> _miracleTokenLookup;
        private ComponentLookup<PhysicsVelocity> _physicsVelocityLookup;
        private ComponentLookup<PhysicsGravityFactor> _physicsGravityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _handQuery = SystemAPI.QueryBuilder()
                .WithAllRW<DivineHandState>()
                .WithAllRW<HandInteractionState>()
                .WithAllRW<ResourceSiphonState>()
                .WithAllRW<DivineHandEvent>()
                .WithAllRW<DivineHandCommand>()
                .WithAll<DivineHandTag, DivineHandConfig, DivineHandInput, PureDOTS.Runtime.Hand.GodIntent>()
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
            _queuedTagLookup = state.GetComponentLookup<HandQueuedTag>(false);
            _physicsVelocityLookup = state.GetComponentLookup<PhysicsVelocity>(false);
            _physicsGravityLookup = state.GetComponentLookup<PhysicsGravityFactor>(false);
            _miracleTokenLookup = state.GetComponentLookup<MiracleToken>(true);

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
            _queuedTagLookup.Update(ref state);
            _physicsVelocityLookup.Update(ref state);
            _physicsGravityLookup.Update(ref state);
            _miracleTokenLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var resourceCatalog = SystemAPI.GetSingleton<ResourceTypeIndex>();
            var catalogRef = resourceCatalog.Catalog;

            foreach (var hand in SystemAPI.Query<DivineHandAspect>())
            {
                var entity = hand.Entity;
                RefRW<ResourceSiphonState> siphonRef = SystemAPI.GetComponentRW<ResourceSiphonState>(entity);
                ref var stateData = ref hand.HandState.ValueRW;
                var config = hand.HandConfig.ValueRO;
                var input = hand.HandInput.ValueRO;
                var intent = hand.Intent.ValueRO;
                ref var command = ref hand.Command.ValueRW;
                var events = hand.Events;
                var queuedBuffer = hand.QueuedEntries;
                var miracleEvents = hand.MiracleEvents;

                events.Clear();

                var previousState = stateData.CurrentState;
                var previousResourceType = stateData.HeldResourceTypeIndex;
                int previousAmount = stateData.HeldAmount;

                stateData.HeldCapacity = math.max(1, config.HeldCapacity);
                stateData.CursorPosition = input.CursorWorldPosition;
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

                bool slingshotMode = input.ThrowModeIsSlingshot != 0;
                stateData.Flags = slingshotMode
                    ? (byte)(stateData.Flags | FlagThrowModeSlingshot)
                    : (byte)(stateData.Flags & ~FlagThrowModeSlingshot);

                bool prevSecondaryHeld = (stateData.Flags & FlagSecondaryHeld) != 0;
                bool secondaryHeld = input.SecondaryHeld != 0;
                stateData.Flags = secondaryHeld
                    ? (byte)(stateData.Flags | FlagSecondaryHeld)
                    : (byte)(stateData.Flags & ~FlagSecondaryHeld);
                bool secondaryReleased = !secondaryHeld && prevSecondaryHeld;

                bool hasHeldEntity = stateData.HeldEntity != Entity.Null && entityManager.Exists(stateData.HeldEntity);
                byte playerId = intent.PlayerId;

                // Consume intent for state transitions
                // Intent.StartSelect triggers grab
                if (intent.StartSelect != 0 && !hasHeldEntity && stateData.CooldownTimer <= 0f)
                {
                    var candidate = FindPickable(ref state, stateData.CursorPosition, config, playerId);
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
                    if (!MaintainHeldTransform(ref state, ref stateData, in config, playerId))
                    {
                        ForceDropHeldEntity(ref ecb, ref stateData);
                        hasHeldEntity = false;
                    }
                    else
                    {
                        bool queuedInstead = false;
                        if (secondaryReleased && input.QueueModifierHeld != 0)
                        {
                            queuedInstead = TryQueueHeldEntity(ref ecb, entityManager, entity, ref stateData, in config, in input, queuedBuffer);
                            if (queuedInstead)
                            {
                                hasHeldEntity = false;
                            }
                        }

                        bool releaseRequested = false;
                        bool releaseViaThrow = false;

                        if (!queuedInstead)
                        {
                            if (secondaryReleased && input.QueueModifierHeld == 0)
                            {
                                releaseRequested = true;
                                releaseViaThrow = true;
                            }
                            else if (intent.CancelAction != 0 || intent.ConfirmPlace != 0)
                            {
                                releaseRequested = true;
                                releaseViaThrow = intent.ConfirmPlace != 0;
                            }
                        }

                        if (!queuedInstead && releaseRequested && _miracleTokenLookup.HasComponent(stateData.HeldEntity))
                        {
                            HandleMiracleTokenRelease(ref ecb, ref stateData, in config, in input, miracleEvents);
                            hasHeldEntity = false;
                            releaseRequested = false;
                            releaseViaThrow = false;
                        }

                        if (releaseRequested)
                        {
                            var releaseResult = ReleaseHeldEntity(ref state, ref ecb, ref stateData, in config, in input, in intent, playerId, releaseViaThrow);
                            if (releaseResult != ReleaseResult.None)
                            {
                                bool appliedThrow = releaseResult == ReleaseResult.Thrown;
                                hasHeldEntity = false;
                                stateData.HeldAmount = 0;
                                stateData.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
                                if (appliedThrow)
                                {
                                    stateData.CooldownTimer = math.max(stateData.CooldownTimer, config.CooldownAfterThrowSeconds);
                                }
                            }
                        }
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

                stateData.Flags = hasCargo
                    ? (byte)(stateData.Flags | FlagHasCargo)
                    : (byte)(stateData.Flags & ~FlagHasCargo);

                // Slingshot aim is active when holding cargo, charging, and not releasing
                bool slingshotAimActive = slingshotMode && hasCargo && config.MinChargeSeconds > 0f &&
                                           stateData.ChargeTimer >= config.MinChargeSeconds &&
                                           intent.ConfirmPlace == 0 && intent.CancelAction == 0;

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

                ref var interaction = ref hand.Interaction.ValueRW;
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

                if (queuedBuffer.Length > 0)
                {
                    if (input.ReleaseAllTriggered != 0)
                    {
                        ReleaseQueuedEntries(ref ecb, entityManager, queuedBuffer, queuedBuffer.Length, in config, ref stateData);
                    }
                    else if (input.ReleaseSingleTriggered != 0)
                    {
                        ReleaseQueuedEntries(ref ecb, entityManager, queuedBuffer, 1, in config, ref stateData);
                    }
                }

                hand.Command.ValueRW = command;
            }

            ecb.Playback(entityManager);
        }

        private bool MaintainHeldTransform(ref SystemState systemState, ref DivineHandState handState, in DivineHandConfig config, byte playerId)
        {
            if (handState.HeldEntity == Entity.Null || !_transformLookup.HasComponent(handState.HeldEntity))
            {
                return true;
            }

            var heldTransform = _transformLookup[handState.HeldEntity];
            float targetHeight = math.max(config.HoldHeightOffset, handState.CursorPosition.y);
            var targetPosition = handState.CursorPosition;
            targetPosition.y = targetHeight;

            if (!IsWithinInfluence(ref systemState, targetPosition, playerId, 0f))
            {
                return false;
            }

            float followLerp = config.HoldLerp;
            if (_pickableLookup.HasComponent(handState.HeldEntity))
            {
                followLerp = math.clamp(_pickableLookup[handState.HeldEntity].FollowLerp, 0.01f, 1f);
            }

            heldTransform.Position = math.lerp(heldTransform.Position, targetPosition, followLerp);
            heldTransform.Rotation = quaternion.identity;
            _transformLookup[handState.HeldEntity] = heldTransform;

            if (_rainCloudStateLookup.HasComponent(handState.HeldEntity))
            {
                var rainState = _rainCloudStateLookup[handState.HeldEntity];
                rainState.Velocity = float3.zero;
                _rainCloudStateLookup[handState.HeldEntity] = rainState;
            }

            return true;
        }

        private Entity FindPickable(ref SystemState state, float3 cursorPosition, DivineHandConfig config, byte playerId)
        {
            Entity bestEntity = Entity.Null;
            float bestDistanceSq = config.PickupRadius * config.PickupRadius;

            // Try spatial query first if available
            if (SystemAPI.TryGetSingleton(out SpatialGridConfig spatialConfig) &&
                SystemAPI.TryGetSingleton(out SpatialGridState spatialState))
            {
                var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
                var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);
                var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
                
                var nearbyEntities = new NativeList<Entity>(32, Allocator.Temp);
                SpatialQueryHelper.GetEntitiesWithinRadius(
                    ref cursorPosition,
                    config.PickupRadius,
                    spatialConfig,
                    ranges,
                    entries,
                    ref nearbyEntities);

                // Filter by HandPickable and check held status
                foreach (var entity in nearbyEntities)
                {
                    if (!state.EntityManager.Exists(entity) || 
                        !_pickableLookup.HasComponent(entity) ||
                        _heldLookup.HasComponent(entity))
                    {
                        continue;
                    }

                    if (!_transformLookup.HasComponent(entity))
                    {
                        continue;
                    }

                    float3 position = _transformLookup[entity].Position;
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

                    if (!IsWithinInfluence(ref state, position, playerId, 0f))
                    {
                        continue;
                    }

                    bestDistanceSq = distanceSq;
                    bestEntity = entity;
                }

                nearbyEntities.Dispose();
                
                if (bestEntity != Entity.Null)
                {
                    return bestEntity;
                }
            }

            // Fallback to full entity scan if spatial grid unavailable
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

                if (!IsWithinInfluence(ref state, position, playerId, 0f))
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

        private ReleaseResult ReleaseHeldEntity(ref SystemState systemState,
            ref EntityCommandBuffer ecb,
            ref DivineHandState handState,
            in DivineHandConfig config,
            in DivineHandInput input,
            in GodIntent intent,
            byte playerId,
            bool forceThrow)
        {
            if (handState.HeldEntity == Entity.Null)
            {
                return ReleaseResult.None;
            }

            bool throwRequested = forceThrow || intent.ConfirmPlace != 0;
            if (throwRequested && !IsWithinInfluence(ref systemState, handState.CursorPosition, playerId, 0f))
            {
                return ReleaseResult.None;
            }

            if (_heldLookup.HasComponent(handState.HeldEntity))
            {
                ecb.RemoveComponent<HandHeldTag>(handState.HeldEntity);
            }

            bool slingshotMode = (handState.Flags & FlagThrowModeSlingshot) != 0;
            ComputeThrowParameters(ref handState, in config, in input, slingshotMode, out var direction, out var impulse);

            bool appliedThrow = throwRequested;
            if (appliedThrow)
            {
                ApplyThrowToEntity(ref ecb, handState.HeldEntity, direction, impulse);
            }

            handState.HeldEntity = Entity.Null;
            handState.HeldLocalOffset = float3.zero;
            handState.ChargeTimer = 0f;
            handState.Flags = (byte)(handState.Flags & ~FlagHasCargo);

            return appliedThrow ? ReleaseResult.Thrown : ReleaseResult.Dropped;
        }

        private void ForceDropHeldEntity(ref EntityCommandBuffer ecb, ref DivineHandState state)
        {
            if (state.HeldEntity == Entity.Null)
            {
                return;
            }

            if (_heldLookup.HasComponent(state.HeldEntity))
            {
                ecb.RemoveComponent<HandHeldTag>(state.HeldEntity);
            }

            state.HeldEntity = Entity.Null;
            state.HeldLocalOffset = float3.zero;
            state.HeldAmount = 0;
            state.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
            state.ChargeTimer = 0f;
            state.Flags = (byte)(state.Flags & ~FlagHasCargo);
        }

        private bool TryQueueHeldEntity(ref EntityCommandBuffer ecb,
            EntityManager entityManager,
            Entity handEntity,
            ref DivineHandState state,
            in DivineHandConfig config,
            in DivineHandInput input,
            DynamicBuffer<HandQueuedThrowElement> queuedBuffer)
        {
            if (state.HeldEntity == Entity.Null || !entityManager.Exists(state.HeldEntity))
            {
                return false;
            }

            bool slingshotMode = (state.Flags & FlagThrowModeSlingshot) != 0;
            ComputeThrowParameters(ref state, in config, in input, slingshotMode, out var direction, out var impulse);

            queuedBuffer.Add(new HandQueuedThrowElement
            {
                Entity = state.HeldEntity,
                Direction = direction,
                Impulse = impulse
            });

            FreezeQueuedEntity(ref ecb, state.HeldEntity, handEntity);

            if (_heldLookup.HasComponent(state.HeldEntity))
            {
                ecb.RemoveComponent<HandHeldTag>(state.HeldEntity);
            }

            state.HeldEntity = Entity.Null;
            state.HeldLocalOffset = float3.zero;
            state.HeldAmount = 0;
            state.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
            state.ChargeTimer = 0f;
            state.Flags = (byte)(state.Flags & ~FlagHasCargo);

            return true;
        }

        private void FreezeQueuedEntity(ref EntityCommandBuffer ecb, Entity entity, Entity handEntity)
        {
            float3 storedLinear = float3.zero;
            float3 storedAngular = float3.zero;
            float storedGravity = 1f;

            if (_physicsVelocityLookup.HasComponent(entity))
            {
                var velocity = _physicsVelocityLookup[entity];
                storedLinear = velocity.Linear;
                storedAngular = velocity.Angular;
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
                _physicsVelocityLookup[entity] = velocity;
            }

            if (_physicsGravityLookup.HasComponent(entity))
            {
                storedGravity = _physicsGravityLookup[entity].Value;
                var gravity = _physicsGravityLookup[entity];
                gravity.Value = 0f;
                _physicsGravityLookup[entity] = gravity;
            }

            if (_queuedTagLookup.HasComponent(entity))
            {
                ecb.SetComponent(entity, new HandQueuedTag
                {
                    Holder = handEntity,
                    StoredLinearVelocity = storedLinear,
                    StoredAngularVelocity = storedAngular,
                    StoredGravityFactor = storedGravity
                });
            }
            else
            {
                ecb.AddComponent(entity, new HandQueuedTag
                {
                    Holder = handEntity,
                    StoredLinearVelocity = storedLinear,
                    StoredAngularVelocity = storedAngular,
                    StoredGravityFactor = storedGravity
                });
            }
        }

        private void ReleaseQueuedEntries(ref EntityCommandBuffer ecb,
            EntityManager entityManager,
            DynamicBuffer<HandQueuedThrowElement> queuedBuffer,
            int releaseCount,
            in DivineHandConfig config,
            ref DivineHandState state)
        {
            releaseCount = math.clamp(releaseCount, 0, queuedBuffer.Length);
            if (releaseCount <= 0)
            {
                return;
            }

            for (int i = 0; i < releaseCount; i++)
            {
                var entry = queuedBuffer[i];
                if (entry.Entity == Entity.Null || !entityManager.Exists(entry.Entity))
                {
                    continue;
                }

                ApplyQueuedThrow(ref ecb, entry);
            }

            queuedBuffer.RemoveRange(0, releaseCount);
            state.CooldownTimer = math.max(state.CooldownTimer, config.CooldownAfterThrowSeconds);
        }

        private void ApplyQueuedThrow(ref EntityCommandBuffer ecb, HandQueuedThrowElement entry)
        {
            RestoreQueuedEntity(ref ecb, entry.Entity);
            ApplyThrowToEntity(ref ecb, entry.Entity, entry.Direction, entry.Impulse);
        }

        private void RestoreQueuedEntity(ref EntityCommandBuffer ecb, Entity entity)
        {
            if (!_queuedTagLookup.HasComponent(entity))
            {
                return;
            }

            var queued = _queuedTagLookup[entity];

            if (_physicsVelocityLookup.HasComponent(entity))
            {
                var velocity = _physicsVelocityLookup[entity];
                velocity.Linear = queued.StoredLinearVelocity;
                velocity.Angular = queued.StoredAngularVelocity;
                _physicsVelocityLookup[entity] = velocity;
            }

            if (_physicsGravityLookup.HasComponent(entity))
            {
                var gravity = _physicsGravityLookup[entity];
                gravity.Value = queued.StoredGravityFactor;
                _physicsGravityLookup[entity] = gravity;
            }

            ecb.RemoveComponent<HandQueuedTag>(entity);
        }

        private void ComputeThrowParameters(ref DivineHandState state,
            in DivineHandConfig config,
            in DivineHandInput input,
            bool slingshotMode,
            out float3 direction,
            out float impulse)
        {
            float baseImpulse = math.max(1f, config.ThrowImpulse);
            float chargeDuration = math.max(0f, state.ChargeTimer);
            float minCharge = math.max(0f, config.MinChargeSeconds);
            float maxCharge = math.max(minCharge, config.MaxChargeSeconds);

            float normalizedCharge;
            if (maxCharge > minCharge)
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

            if (slingshotMode)
            {
                var baseDirection = math.normalizesafe(input.AimDirection, new float3(0f, -1f, 0f));
                var planar = math.normalizesafe(new float3(baseDirection.x, 0f, baseDirection.z), new float3(0f, 0f, 1f));
                float arcAngle = math.lerp(0.1f, 0.85f, normalizedCharge);
                float cos = math.cos(arcAngle);
                float sin = math.sin(arcAngle);
                var vertical = new float3(0f, 1f, 0f);
                direction = math.normalizesafe(planar * cos + vertical * sin, baseDirection);
                float chargeMultiplier = math.max(1f, 1f + normalizedCharge * config.ThrowChargeMultiplier);
                impulse = baseImpulse * chargeMultiplier;
            }
            else
            {
                direction = math.normalizesafe(input.AimDirection, new float3(0f, -1f, 0f));
                float pointerSpeed = math.saturate(math.length(input.PointerDelta) * 0.01f);
                float boost = math.max(pointerSpeed, normalizedCharge);
                impulse = baseImpulse * math.max(1f, 1f + boost * config.ThrowChargeMultiplier);
            }
        }

        private void ApplyThrowToEntity(ref EntityCommandBuffer ecb, Entity entity, float3 direction, float impulse)
        {
            var normalizedDir = math.normalizesafe(direction, new float3(0f, -1f, 0f));

            if (_rainCloudStateLookup.HasComponent(entity))
            {
                var rainState = _rainCloudStateLookup[entity];
                rainState.Velocity = normalizedDir * impulse;
                _rainCloudStateLookup[entity] = rainState;
            }

            if (_physicsVelocityLookup.HasComponent(entity))
            {
                var velocity = _physicsVelocityLookup[entity];
                velocity.Linear = normalizedDir * impulse;
                velocity.Angular = float3.zero;
                _physicsVelocityLookup[entity] = velocity;
            }

            if (_physicsGravityLookup.HasComponent(entity))
            {
                var gravity = _physicsGravityLookup[entity];
                if (gravity.Value == 0f)
                {
                    gravity.Value = 1f;
                    _physicsGravityLookup[entity] = gravity;
                }
            }

            if (_queuedTagLookup.HasComponent(entity))
            {
                ecb.RemoveComponent<HandQueuedTag>(entity);
            }
        }

        private void HandleMiracleTokenRelease(ref EntityCommandBuffer ecb,
            ref DivineHandState state,
            in DivineHandConfig config,
            in DivineHandInput input,
            DynamicBuffer<MiracleReleaseEvent> miracleEvents)
        {
            if (state.HeldEntity == Entity.Null)
            {
                return;
            }

            var token = _miracleTokenLookup[state.HeldEntity];
            ecb.DestroyEntity(state.HeldEntity);

            bool slingshotMode = (state.Flags & FlagThrowModeSlingshot) != 0;
            ComputeThrowParameters(ref state, in config, in input, slingshotMode, out var direction, out var impulse);

            miracleEvents.Add(new MiracleReleaseEvent
            {
                Type = token.Type,
                Position = state.CursorPosition,
                Direction = direction,
                Impulse = impulse,
                ConfigEntity = token.ConfigEntity
            });

            state.HeldEntity = Entity.Null;
            state.HeldLocalOffset = float3.zero;
            state.HeldAmount = 0;
            state.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
            state.ChargeTimer = 0f;
            state.Flags = (byte)(state.Flags & ~FlagHasCargo);
        }

        private bool IsWithinInfluence(ref SystemState systemState, float3 position, byte playerId, float padding)
        {
            bool hasSources = false;
            float paddingSq = padding <= 0f ? 0f : padding * padding;

            foreach (var (source, transform) in SystemAPI
                         .Query<RefRO<InfluenceSource>, RefRO<LocalTransform>>())
            {
                if (source.ValueRO.PlayerId != playerId)
                {
                    continue;
                }

                hasSources = true;
                float radius = math.max(0f, source.ValueRO.Radius);
                if (radius <= 0f)
                {
                    continue;
                }

                float radiusSq = radius * radius + paddingSq;
                float distanceSq = math.lengthsq(position - transform.ValueRO.Position);
                if (distanceSq <= radiusSq)
                {
                    return true;
                }
            }

            // When no sources exist yet, avoid blocking gameplay.
            return !hasSources;
        }

        private enum ReleaseResult : byte
        {
            None,
            Dropped,
            Thrown
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
                if (item.TierId == 0)
                {
                    item.TierId = (byte)ResourceQualityTier.Unknown;
                }
                if (item.AverageQuality == 0)
                {
                    item.AverageQuality = 200;
                }
                itemsBuffer[itemIndex] = item;
            }
            else
            {
                itemsBuffer.Add(new StorehouseInventoryItem
                {
                    ResourceTypeId = resourceId,
                    Amount = depositUnits,
                    Reserved = 0f,
                    TierId = (byte)ResourceQualityTier.Unknown,
                    AverageQuality = 200
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
#pragma warning restore 0618
