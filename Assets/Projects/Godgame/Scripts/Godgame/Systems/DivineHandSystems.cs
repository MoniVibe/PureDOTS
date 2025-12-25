using Godgame.Runtime;
using MiracleToken = Godgame.Runtime.MiracleToken;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Hand;
using PureDOTS.Input;
using PureDOTS.Systems;
using PureDOTS.Systems.Hand;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using PureHandState = PureDOTS.Runtime.Hand.HandState;
using HandStateType = PureDOTS.Runtime.Hand.HandStateType;
using HandState = PureDOTS.Runtime.Components.HandState;
using HandCommandElement = PureDOTS.Runtime.Hand.HandCommand;
using HandCommandKind = PureDOTS.Runtime.Hand.HandCommandType;
using GodgameHandState = Godgame.Runtime.HandState;
using MiracleSlotDefinition = PureDOTS.Runtime.Components.MiracleSlotDefinition;
using MiracleType = PureDOTS.Runtime.Components.MiracleType;

#pragma warning disable 618
namespace Godgame.Systems
{
    public readonly partial struct DivineHandAspect : IAspect
    {
        public readonly Entity Entity;
        public readonly RefRW<DivineHandState> HandState;
        public readonly RefRW<PureHandState> PureHandState;
        public readonly RefRO<DivineHandConfig> HandConfig;
        public readonly RefRW<DivineHandCommand> Command;
        public readonly RefRW<HandInteractionState> Interaction;
        public readonly DynamicBuffer<DivineHandEvent> Events;
        public readonly DynamicBuffer<HandQueuedThrowElement> QueuedEntries;
        public readonly DynamicBuffer<MiracleReleaseEvent> MiracleEvents;
        public readonly DynamicBuffer<HandCommandElement> Commands;
        public readonly RefRO<GodIntent> Intent;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(HandCommandEmitterSystem))]
    public partial struct DivineHandSystem : ISystem
    {
        private EntityQuery _handQuery;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<HandPickable> _pickableLookup;
        private ComponentLookup<HandHeldTag> _heldLookup;
        private ComponentLookup<RainCloudState> _rainCloudStateLookup;
        private ComponentLookup<ResourceTypeId> _resourceTypeIdLookup;
        private ComponentLookup<HandQueuedTag> _queuedTagLookup;
        private ComponentLookup<MiracleToken> _miracleTokenLookup;
        private ComponentLookup<PhysicsVelocity> _physicsVelocityLookup;
        private ComponentLookup<PhysicsGravityFactor> _physicsGravityLookup;
        private ComponentLookup<MiracleCasterState> _miracleCasterLookup;
        private BufferLookup<MiracleSlotDefinition> _miracleSlotLookup;
        private ComponentLookup<StorehouseInventory> _storehouseInventoryLookup;
        private BufferLookup<StorehouseInventoryItem> _storeItemsLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _handQuery = SystemAPI.QueryBuilder()
                .WithAllRW<DivineHandState>()
                .WithAllRW<PureDOTS.Runtime.Hand.HandState>()
                .WithAllRW<HandInteractionState>()
                .WithAllRW<ResourceSiphonState>()
                .WithAllRW<DivineHandEvent>()
                .WithAllRW<DivineHandCommand>()
                .WithAll<DivineHandTag, DivineHandConfig, PureDOTS.Runtime.Hand.GodIntent>()
                .Build();

            state.RequireForUpdate(_handQuery);
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<HandInputFrame>();
            state.RequireForUpdate<HandHover>();
            state.RequireForUpdate<HandAffordances>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(false);
            _pickableLookup = state.GetComponentLookup<HandPickable>(true);
            _heldLookup = state.GetComponentLookup<HandHeldTag>(true);
            _rainCloudStateLookup = state.GetComponentLookup<RainCloudState>(false);
            _resourceTypeIdLookup = state.GetComponentLookup<ResourceTypeId>(true);
            _queuedTagLookup = state.GetComponentLookup<HandQueuedTag>(false);
            _physicsVelocityLookup = state.GetComponentLookup<PhysicsVelocity>(false);
            _physicsGravityLookup = state.GetComponentLookup<PhysicsGravityFactor>(false);
            _miracleTokenLookup = state.GetComponentLookup<MiracleToken>(true);
            _miracleCasterLookup = state.GetComponentLookup<MiracleCasterState>(true);
            _miracleSlotLookup = state.GetBufferLookup<MiracleSlotDefinition>(true);
            _storehouseInventoryLookup = state.GetComponentLookup<StorehouseInventory>(false);
            _storeItemsLookup = state.GetBufferLookup<StorehouseInventoryItem>(false);

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
            _queuedTagLookup.Update(ref state);
            _physicsVelocityLookup.Update(ref state);
            _physicsGravityLookup.Update(ref state);
            _miracleTokenLookup.Update(ref state);
            _miracleCasterLookup.Update(ref state);
            _miracleSlotLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            try
            {
                var resourceCatalog = SystemAPI.GetSingleton<ResourceTypeIndex>();
                var catalogRef = resourceCatalog.Catalog;

                // Read HandInputFrame singleton and affordances
                var inputFrame = SystemAPI.GetSingleton<HandInputFrame>();
                var hover = SystemAPI.GetSingleton<HandHover>();
                var affordances = SystemAPI.GetSingleton<HandAffordances>();

                foreach (var hand in SystemAPI.Query<DivineHandAspect>())
                {
                var entity = hand.Entity;
                RefRW<ResourceSiphonState> siphonRef = SystemAPI.GetComponentRW<ResourceSiphonState>(entity);
                ref var stateData = ref hand.HandState.ValueRW;
                ref var pureHandState = ref hand.PureHandState.ValueRW;
                var config = hand.HandConfig.ValueRO;
                var intent = hand.Intent.ValueRO;
                ref var command = ref hand.Command.ValueRW;
                var events = hand.Events;
                var queuedBuffer = hand.QueuedEntries;
                var miracleEvents = hand.MiracleEvents;
                var commands = hand.Commands;

                events.Clear();
                commands.Clear(); // Clear commands each tick, state machine will emit new ones

                var previousState = stateData.CurrentState;
                var previousLegacyState = MapGodgameStateToLegacy(previousState);
                var previousResourceType = stateData.HeldResourceTypeIndex;
                int previousAmount = stateData.HeldAmount;

                stateData.HeldCapacity = math.max(1, config.HeldCapacity);
                
                // Compute cursor world position from ray (intersect with ground plane at y=0)
                float3 cursorWorldPos = inputFrame.RayOrigin;
                float3 rayDir = inputFrame.RayDirection;
                if (math.abs(rayDir.y) > 0.0001f)
                {
                    float t = -inputFrame.RayOrigin.y / rayDir.y;
                    if (t > 0f)
                    {
                        cursorWorldPos = inputFrame.RayOrigin + rayDir * t;
                    }
                }
                stateData.CursorPosition = cursorWorldPos;
                stateData.AimDirection = math.normalizesafe(inputFrame.RayDirection, new float3(0f, -1f, 0f));

                if (stateData.CooldownTimer > 0f)
                {
                    stateData.CooldownTimer = math.max(0f, stateData.CooldownTimer - deltaTime);
                }

                float maxChargeWindow = math.max(config.MinChargeSeconds, config.MaxChargeSeconds);

                // Update charge timer based on RMB held state
                if (inputFrame.RmbHeld)
                {
                    if (maxChargeWindow > 0f)
                    {
                        stateData.ChargeTimer = math.min(stateData.ChargeTimer + deltaTime, maxChargeWindow);
                    }
                    else
                    {
                        stateData.ChargeTimer += deltaTime;
                    }
                }
                else
                {
                    stateData.ChargeTimer = 0f;
                }

                float normalizedChargeLevel = maxChargeWindow > 0f
                    ? math.saturate(stateData.ChargeTimer / math.max(0.0001f, maxChargeWindow))
                    : 0f;

                bool hasHeldEntity = stateData.HeldEntity != Entity.Null && entityManager.Exists(stateData.HeldEntity);

                // Hold lock: only resolve pick candidate when nothing is currently held.
                Entity pickCandidate = Entity.Null;
                if (!hasHeldEntity && intent.StartSelect != 0 && stateData.CooldownTimer <= 0f)
                {
                    pickCandidate = ResolveHandCandidate(ref state, in hover, stateData.CursorPosition, config);
                }

                var aim = ResolveAimPoint(in hover, stateData.CursorPosition);

                // Compute affordance flags and miracle cast intent early
                bool affordanceHasSiphon = (affordances.Flags & HandAffordanceFlags.CanSiphon) != 0 &&
                                           affordances.TargetEntity != Entity.Null;
                bool affordanceHasDumpStorehouse = (affordances.Flags & HandAffordanceFlags.CanDumpStorehouse) != 0;
                bool affordanceHasDumpConstruction = (affordances.Flags & HandAffordanceFlags.CanDumpConstruction) != 0;
                bool affordanceHasDumpGround = (affordances.Flags & HandAffordanceFlags.CanDumpGround) != 0;
                bool affordanceHasDump = affordanceHasDumpStorehouse || affordanceHasDumpConstruction || affordanceHasDumpGround;
                bool affordanceHasMiracle = (affordances.Flags & HandAffordanceFlags.CanCastMiracle) != 0;
                bool wantsMiracleCast = affordanceHasMiracle && inputFrame.RmbReleased;

                // Declare miracleCastTriggered early, initialized to false
                bool miracleCastTriggered = false;

                if (pickCandidate != Entity.Null)
                {
                    bool accept = true;
                    if (catalogRef.IsCreated && _resourceTypeIdLookup.HasComponent(pickCandidate))
                    {
                        var typeId = _resourceTypeIdLookup[pickCandidate].Value;
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
                        stateData.HeldEntity = pickCandidate;
                        stateData.HeldLocalOffset = float3.zero;
                        stateData.HeldAmount = 1;
                        hasHeldEntity = true;
                        ecb.AddComponent(pickCandidate, new HandHeldTag { Holder = entity });
                        
                        // Emit Pick command
                        commands.Add(new PureDOTS.Runtime.Hand.HandCommand
                        {
                            Tick = currentTick,
                            Type = PureDOTS.Runtime.Hand.HandCommandType.Pick,
                            TargetEntity = pickCandidate,
                            TargetPosition = aim.TargetPosition,
                            Direction = float3.zero,
                            Speed = 0f,
                            ChargeLevel = 0f,
                            ResourceTypeIndex = 0,
                            Amount = 0f
                        });
                    }
                }

                if (hasHeldEntity && !_transformLookup.HasComponent(stateData.HeldEntity))
                {
                    stateData.HeldEntity = Entity.Null;
                    hasHeldEntity = false;
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

                    // Release is triggered by CancelAction or ConfirmPlace intent, or RMB release
                    bool releaseRequested = intent.CancelAction != 0 || intent.ConfirmPlace != 0 || inputFrame.RmbReleased;
                    bool queuedInstead = releaseRequested && inputFrame.ShiftHeld &&
                                         TryQueueHeldEntity(ref ecb, entityManager, entity, ref stateData, in config, inputFrame, normalizedChargeLevel, currentTick, commands, queuedBuffer);
                    if (!queuedInstead && releaseRequested && _miracleTokenLookup.HasComponent(stateData.HeldEntity))
                    {
                        HandleMiracleTokenRelease(ref ecb, ref stateData, in config, inputFrame, aim, normalizedChargeLevel, currentTick, commands, miracleEvents);
                        hasHeldEntity = false;
                        releaseRequested = false;
                        miracleCastTriggered = true;
                    }
                    if (queuedInstead)
                    {
                        hasHeldEntity = false;
                        releaseRequested = false;
                    }

                    if (releaseRequested)
                    {
                        bool appliedThrow = ReleaseHeldEntity(ref ecb, ref stateData, in config, inputFrame, aim, normalizedChargeLevel, in intent, currentTick, commands);
                        hasHeldEntity = false;
                        stateData.HeldAmount = 0;
                        stateData.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
                        stateData.CooldownTimer = math.max(stateData.CooldownTimer, appliedThrow ? config.CooldownAfterThrowSeconds : 0f);
                    }

                    if (hasHeldEntity && stateData.HeldEntity != Entity.Null)
                    {
                        EmitHoldCommand(ref commands, currentTick, stateData.HeldEntity, aim.TargetPosition, stateData.AimDirection, normalizedChargeLevel, stateData.HeldResourceTypeIndex);
                    }
                }

                if (!hasHeldEntity && wantsMiracleCast && !miracleCastTriggered)
                {
                    miracleCastTriggered = TryEmitMiracleCastFromSlot(entity, in stateData, aim, normalizedChargeLevel, currentTick, commands, miracleEvents);
                }

                // Compute verb intents from affordances
                bool wantsSiphon = !hasHeldEntity &&
                                   stateData.HeldAmount < stateData.HeldCapacity &&
                                   affordanceHasSiphon &&
                                   inputFrame.LmbHeld;

                bool wantsDump = !hasHeldEntity &&
                                 stateData.HeldAmount > 0 &&
                                 affordanceHasDump &&
                                 inputFrame.LmbHeld;

                bool hasCargo = hasHeldEntity || stateData.HeldAmount > 0;
                
                // Compute miracleVerbActive after all miracle cast attempts
                bool miracleVerbActive = wantsMiracleCast && miracleCastTriggered;

                stateData.Flags = hasCargo ? (byte)(stateData.Flags | 0x1) : (byte)(stateData.Flags & 0xFE);

                // Slingshot aim is active when holding cargo, charging, and not releasing
                bool slingshotAimActive = hasCargo && config.MinChargeSeconds > 0f &&
                                           stateData.ChargeTimer >= config.MinChargeSeconds &&
                                           intent.ConfirmPlace == 0 && intent.CancelAction == 0;

                GodgameHandState nextState;
                if (wantsDump)
                {
                    nextState = GodgameHandState.Dumping;
                }
                else if (wantsSiphon)
                {
                    nextState = GodgameHandState.Dragging;
                }
                else if (miracleVerbActive)
                {
                    nextState = GodgameHandState.Holding; // reuse holding state for casting visuals
                }
                else if (slingshotAimActive)
                {
                    nextState = GodgameHandState.SlingshotAim;
                }
                else if (hasCargo)
                {
                    nextState = GodgameHandState.Holding;
                }
                else
                {
                    nextState = GodgameHandState.Empty;
                }

                // Sync HandInteractionState from PureDOTS.Runtime.Hand.HandState (authoritative)
                ref var interaction = ref hand.Interaction.ValueRW;
                interaction.HandEntity = entity;
                interaction.PreviousState = MapToLegacyHandState(pureHandState.PreviousState);
                interaction.CurrentState = MapToLegacyHandState(pureHandState.CurrentState);
                
                // Mirror affordance decisions to legacy command component
                var desiredCommandType = DivineHandCommandType.None;
                Entity desiredCommandTarget = Entity.Null;
                if (wantsSiphon)
                {
                    desiredCommandType = DivineHandCommandType.SiphonPile;
                    desiredCommandTarget = affordances.TargetEntity;
                }
                else if (wantsDump)
                {
                    // Dump priority: storehouse targets win, then construction, then ground drip.
                    if (affordanceHasDumpStorehouse)
                    {
                        desiredCommandType = DivineHandCommandType.DumpToStorehouse;
                        desiredCommandTarget = affordances.TargetEntity;
                    }
                    else if (affordanceHasDumpConstruction)
                    {
                        desiredCommandType = DivineHandCommandType.DumpToConstruction;
                        desiredCommandTarget = affordances.TargetEntity;
                    }
                    else if (affordanceHasDumpGround)
                    {
                        desiredCommandType = DivineHandCommandType.GroundDrip;
                        desiredCommandTarget = Entity.Null;
                    }
                }

                if (command.Type != desiredCommandType || command.TargetEntity != desiredCommandTarget)
                {
                    command.TimeSinceIssued = 0f;
                }
                command.Type = desiredCommandType;
                command.TargetEntity = desiredCommandTarget;
                command.TargetPosition = aim.TargetPosition;
                command.TargetNormal = aim.Normal;
                if (command.Type != DivineHandCommandType.None)
                {
                    command.TimeSinceIssued += deltaTime;
                }
                else
                {
                    command.TimeSinceIssued = 0f;
                }

                interaction.ActiveCommand =
                    wantsSiphon ? PureDOTS.Runtime.Components.DivineHandCommandType.Siphon :
                    wantsDump ? PureDOTS.Runtime.Components.DivineHandCommandType.Dump :
                    miracleVerbActive ? PureDOTS.Runtime.Components.DivineHandCommandType.Miracle :
                    PureDOTS.Runtime.Components.DivineHandCommandType.None;
                
                interaction.ActiveResourceType = stateData.HeldResourceTypeIndex;
                interaction.HeldAmount = stateData.HeldAmount;
                interaction.HeldCapacity = stateData.HeldCapacity;
                interaction.CooldownSeconds = stateData.CooldownTimer;
                interaction.LastUpdateTick = currentTick;
                interaction.Flags = 0;
                if (wantsSiphon)
                {
                    interaction.Flags |= HandInteractionState.FlagSiphoning;
                }
                if (wantsDump)
                {
                    interaction.Flags |= HandInteractionState.FlagDumping;
                }

                ref var siphonState = ref siphonRef.ValueRW;
                siphonState.HandEntity = entity;
                siphonState.TargetEntity = affordances.TargetEntity;
                siphonState.ResourceTypeIndex = stateData.HeldResourceTypeIndex;
                siphonState.SiphonRate = config.SiphonRate;
                siphonState.DumpRate = config.DumpRate;
                siphonState.LastUpdateTick = currentTick;
                siphonState.Flags = 0;
                if (wantsSiphon)
                {
                    siphonState.Flags |= ResourceSiphonState.FlagSiphoning;
                }
                if (wantsDump)
                {
                    siphonState.Flags |= ResourceSiphonState.FlagDumpCommandPending;
                }

                EmitContinuousCommands(ref commands, currentTick, wantsSiphon, wantsDump, in affordances, in stateData, aim, normalizedChargeLevel);

                var mappedState = MapToPureState(nextState);
                if (pureHandState.CurrentState != mappedState)
                {
                    pureHandState.PreviousState = pureHandState.CurrentState;
                    pureHandState.CurrentState = mappedState;
                    pureHandState.StateTimer = 0;
                }
                else
                {
                    pureHandState.StateTimer++;
                }

                pureHandState.HeldEntity = stateData.HeldEntity;
                pureHandState.HoldPoint = aim.TargetPosition;
                pureHandState.HoldDistance = stateData.HeldEntity == Entity.Null ? 0f : math.length(stateData.HeldLocalOffset);
                pureHandState.ChargeTimer = stateData.ChargeTimer;
                pureHandState.CooldownTimer = stateData.CooldownTimer;

                if (nextState != stateData.CurrentState)
                {
                    var nextLegacyState = MapGodgameStateToLegacy(nextState);
                    events.Add(new DivineHandEvent
                    {
                        Type = DivineHandEventType.StateChanged,
                        FromState = previousLegacyState,
                        ToState = nextLegacyState,
                        ResourceTypeIndex = stateData.HeldResourceTypeIndex,
                        Amount = stateData.HeldAmount,
                        Capacity = stateData.HeldCapacity
                    });
                    stateData.PreviousState = stateData.CurrentState;
                    stateData.CurrentState = nextState;
                }

                var legacyCurrentState = MapGodgameStateToLegacy(stateData.CurrentState);

                if (previousResourceType != stateData.HeldResourceTypeIndex)
                {
                    events.Add(new DivineHandEvent
                    {
                        Type = DivineHandEventType.TypeChanged,
                        FromState = previousLegacyState,
                        ToState = legacyCurrentState,
                        ResourceTypeIndex = stateData.HeldResourceTypeIndex,
                        Amount = stateData.HeldAmount,
                        Capacity = stateData.HeldCapacity
                    });
                }

                if (previousAmount != stateData.HeldAmount)
                {
                    events.Add(new DivineHandEvent
                    {
                        Type = DivineHandEventType.AmountChanged,
                        FromState = previousLegacyState,
                        ToState = legacyCurrentState,
                        ResourceTypeIndex = stateData.HeldResourceTypeIndex,
                        Amount = stateData.HeldAmount,
                        Capacity = stateData.HeldCapacity
                    });
                }

                if (queuedBuffer.Length > 0)
                {
                    if (inputFrame.ReleaseAllPressed)
                    {
                        ReleaseQueuedEntries(ref ecb, entityManager, queuedBuffer, queuedBuffer.Length, in config, ref stateData);
                    }
                    else if (inputFrame.ReleaseOnePressed)
                    {
                        ReleaseQueuedEntries(ref ecb, entityManager, queuedBuffer, 1, in config, ref stateData);
                    }
                }

                hand.Command.ValueRW = command;
            }
            }
            finally
            {
                ecb.Playback(entityManager);
                ecb.Dispose();
            }
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

        private Entity ResolveHandCandidate(ref SystemState state, in HandHover hover, float3 cursorPosition, DivineHandConfig config)
        {
            var hoverTarget = hover.TargetEntity;
            if (hoverTarget != Entity.Null &&
                state.EntityManager.Exists(hoverTarget) &&
                _pickableLookup.HasComponent(hoverTarget) &&
                !_heldLookup.HasComponent(hoverTarget))
            {
                return hoverTarget;
            }

            return FindPickable(ref state, cursorPosition, config);
        }

        private struct AimPoint
        {
            public bool HasHit;
            public Entity TargetEntity;
            public float3 TargetPosition;
            public float3 Normal;
        }

        private AimPoint ResolveAimPoint(in HandHover hover, float3 cursorFallback)
        {
            if (hover.TargetEntity != Entity.Null && hover.Distance < float.MaxValue)
            {
                return new AimPoint
                {
                    HasHit = true,
                    TargetEntity = hover.TargetEntity,
                    TargetPosition = hover.HitPosition,
                    Normal = hover.HitNormal
                };
            }

            return new AimPoint
            {
                HasHit = false,
                TargetEntity = Entity.Null,
                TargetPosition = cursorFallback,
                Normal = new float3(0f, 1f, 0f)
            };
        }

        private bool ReleaseHeldEntity(ref EntityCommandBuffer ecb,
            ref DivineHandState state,
            in DivineHandConfig config,
            HandInputFrame inputFrame,
            AimPoint aim,
            float normalizedChargeLevel,
            in GodIntent intent,
            uint currentTick,
            DynamicBuffer<HandCommandElement> commands)
        {
            if (state.HeldEntity == Entity.Null)
            {
                return false;
            }

            if (_heldLookup.HasComponent(state.HeldEntity))
            {
                ecb.RemoveComponent<HandHeldTag>(state.HeldEntity);
            }

            var releasedEntity = state.HeldEntity;
            ComputeThrowParameters(ref state, in config, inputFrame, out var direction, out var impulse);

            bool appliedThrow = intent.ConfirmPlace != 0 || inputFrame.RmbReleased;
            if (appliedThrow)
            {
                ApplyThrowToEntity(ref ecb, releasedEntity, direction, impulse);
            EmitSimpleCommand(ref commands, currentTick, HandCommandKind.Throw, releasedEntity, aim.TargetPosition, direction, impulse, normalizedChargeLevel, state.HeldResourceTypeIndex, state.HeldAmount);
            }

            state.HeldEntity = Entity.Null;
            state.HeldLocalOffset = float3.zero;
            state.ChargeTimer = 0f;
            state.Flags &= 0xFE;

            return appliedThrow;
        }

        private bool TryQueueHeldEntity(ref EntityCommandBuffer ecb,
            EntityManager entityManager,
            Entity handEntity,
            ref DivineHandState state,
            in DivineHandConfig config,
            HandInputFrame inputFrame,
            float normalizedChargeLevel,
            uint currentTick,
            DynamicBuffer<HandCommandElement> commands,
            DynamicBuffer<HandQueuedThrowElement> queuedBuffer)
        {
            if (state.HeldEntity == Entity.Null || !entityManager.Exists(state.HeldEntity))
            {
                return false;
            }

            var queuedEntity = state.HeldEntity;
            ComputeThrowParameters(ref state, in config, inputFrame, out var direction, out var impulse);

            queuedBuffer.Add(new HandQueuedThrowElement
            {
                Entity = queuedEntity,
                Direction = direction,
                Impulse = impulse,
                ChargeLevel = normalizedChargeLevel
            });

            FreezeQueuedEntity(ref ecb, queuedEntity, handEntity);

            if (_heldLookup.HasComponent(queuedEntity))
            {
                ecb.RemoveComponent<HandHeldTag>(queuedEntity);
            }

            var queuedResourceType = state.HeldResourceTypeIndex;
            state.HeldEntity = Entity.Null;
            state.HeldLocalOffset = float3.zero;
            state.HeldAmount = 0;
            state.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
            state.ChargeTimer = 0f;
            state.Flags &= 0xFE;

            EmitSimpleCommand(ref commands, currentTick, HandCommandKind.QueueThrow, queuedEntity, state.CursorPosition, direction, impulse, normalizedChargeLevel, queuedResourceType, 0f);

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

                ApplyQueuedThrow(ref ecb, entry, in config);
            }

            queuedBuffer.RemoveRange(0, releaseCount);
            state.CooldownTimer = math.max(state.CooldownTimer, config.CooldownAfterThrowSeconds);
        }

        private void ApplyQueuedThrow(ref EntityCommandBuffer ecb, HandQueuedThrowElement entry, in DivineHandConfig config)
        {
            RestoreQueuedEntity(ref ecb, entry.Entity);

            float charge = math.saturate(entry.ChargeLevel);
            float speed = math.lerp(config.MinThrowSpeed, config.MaxThrowSpeed, charge);
            float3 velocity = entry.Direction * speed;

            if (_physicsVelocityLookup.HasComponent(entry.Entity))
            {
                var physicsVelocity = _physicsVelocityLookup[entry.Entity];
                physicsVelocity.Linear = velocity;
                physicsVelocity.Angular = float3.zero;
                ecb.SetComponent(entry.Entity, physicsVelocity);
            }

            if (_physicsGravityLookup.HasComponent(entry.Entity))
            {
                var gravity = _physicsGravityLookup[entry.Entity];
                gravity.Value = math.max(gravity.Value, 1f);
                ecb.SetComponent(entry.Entity, gravity);
            }

            if (_heldLookup.HasComponent(entry.Entity))
            {
                ecb.RemoveComponent<HandHeldTag>(entry.Entity);
            }

            if (_queuedTagLookup.HasComponent(entry.Entity))
            {
                ecb.RemoveComponent<HandQueuedTag>(entry.Entity);
            }
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
            HandInputFrame inputFrame,
            out float3 direction,
            out float impulse)
        {
            direction = math.normalizesafe(inputFrame.RayDirection, new float3(0f, -1f, 0f));
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

            float chargeMultiplier = math.max(1f, 1f + normalizedCharge * config.ThrowChargeMultiplier);
            impulse = baseImpulse * chargeMultiplier;
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
            HandInputFrame inputFrame,
            AimPoint aim,
            float normalizedChargeLevel,
            uint currentTick,
            DynamicBuffer<HandCommandElement> commands,
            DynamicBuffer<MiracleReleaseEvent> miracleEvents)
        {
            if (state.HeldEntity == Entity.Null)
            {
                return;
            }

            var tokenEntity = state.HeldEntity;
            var token = _miracleTokenLookup[tokenEntity];
            ecb.DestroyEntity(tokenEntity);

            ComputeThrowParameters(ref state, in config, inputFrame, out var direction, out var impulse);

            miracleEvents.Add(new MiracleReleaseEvent
            {
                Type = token.Type,
                Position = aim.TargetPosition,
                Normal = aim.Normal,
                TargetEntity = aim.TargetEntity,
                Direction = direction,
                Impulse = impulse,
                ConfigEntity = token.ConfigEntity
            });

            state.HeldEntity = Entity.Null;
            state.HeldLocalOffset = float3.zero;
            state.HeldAmount = 0;
            state.HeldResourceTypeIndex = DivineHandConstants.NoResourceType;
            state.ChargeTimer = 0f;
            state.Flags &= 0xFE;

            EmitSimpleCommand(ref commands, currentTick, HandCommandKind.CastMiracle, tokenEntity, aim.TargetPosition, direction, impulse, normalizedChargeLevel, 0, 0f);
        }

        private static HandStateType MapToPureState(GodgameHandState state)
        {
            return state switch
            {
                GodgameHandState.Holding => HandStateType.Holding,
                GodgameHandState.Dragging => HandStateType.Siphoning,
                GodgameHandState.SlingshotAim => HandStateType.Charging,
                GodgameHandState.Dumping => HandStateType.Dumping,
                _ => HandStateType.Idle
            };
        }

        /// <summary>
        /// Maps PureDOTS.Runtime.Hand.HandStateType to Godgame.Runtime.HandState for telemetry/bridging.
        /// </summary>
        private static GodgameHandState MapToGodgameState(HandStateType state)
        {
            return state switch
            {
                HandStateType.Holding => GodgameHandState.Holding,
                HandStateType.Siphoning => GodgameHandState.Dragging,
                HandStateType.Charging => GodgameHandState.SlingshotAim,
                HandStateType.Dumping => GodgameHandState.Dumping,
                _ => GodgameHandState.Empty
            };
        }

        /// <summary>
        /// Maps PureDOTS.Runtime.Hand.HandStateType to PureDOTS.Runtime.Components.HandState for HandInteractionState sync.
        /// </summary>
        private static HandState MapToLegacyHandState(HandStateType state)
        {
            return state switch
            {
                HandStateType.Idle => HandState.Idle,
                HandStateType.Hovering => HandState.Hovering,
                HandStateType.AttemptPick => HandState.Grabbing,
                HandStateType.Holding => HandState.Holding,
                HandStateType.Releasing => HandState.Placing,
                HandStateType.CastingMiracle => HandState.Casting,
                HandStateType.Cooldown => HandState.Cooldown,
                HandStateType.Siphoning => HandState.Holding, // Approximate
                HandStateType.Dumping => HandState.Holding, // Approximate
                HandStateType.Charging => HandState.Holding, // Approximate
                HandStateType.Aiming => HandState.Holding, // Approximate
                _ => HandState.Idle
            };
        }

        private static HandState MapGodgameStateToLegacy(GodgameHandState state)
        {
            return MapToLegacyHandState(MapToPureState(state));
        }

        /// <summary>
        /// Maps PureDOTS.Runtime.Hand.HandCommandType to PureDOTS.Runtime.Components.DivineHandCommandType for HandInteractionState sync.
        /// </summary>
        private static PureDOTS.Runtime.Components.DivineHandCommandType MapToLegacyCommandType(HandCommandKind type)
        {
            return type switch
            {
                HandCommandKind.Pick => PureDOTS.Runtime.Components.DivineHandCommandType.Grab,
                HandCommandKind.Throw => PureDOTS.Runtime.Components.DivineHandCommandType.Drop,
                HandCommandKind.Siphon => PureDOTS.Runtime.Components.DivineHandCommandType.Siphon,
                HandCommandKind.Dump => PureDOTS.Runtime.Components.DivineHandCommandType.Dump,
                HandCommandKind.CastMiracle => PureDOTS.Runtime.Components.DivineHandCommandType.Miracle,
                _ => PureDOTS.Runtime.Components.DivineHandCommandType.None
            };
        }

        private static void EmitHoldCommand(ref DynamicBuffer<HandCommandElement> commands,
            uint currentTick,
            Entity heldEntity,
            float3 cursorPosition,
            float3 aimDirection,
            float normalizedChargeLevel,
            ushort resourceTypeIndex)
        {
            if (heldEntity == Entity.Null)
            {
                return;
            }

            EmitSimpleCommand(ref commands, currentTick, HandCommandKind.Hold, heldEntity, cursorPosition, aimDirection, 0f, normalizedChargeLevel, resourceTypeIndex, 0f);
        }

        private static void EmitContinuousCommands(ref DynamicBuffer<HandCommandElement> commands,
            uint currentTick,
            bool wantsSiphon,
            bool wantsDump,
            in HandAffordances affordances,
            in DivineHandState state,
            AimPoint aim,
            float normalizedChargeLevel)
        {
            if (wantsSiphon)
            {
                var target = affordances.TargetEntity;
                if (target != Entity.Null)
                {
                    ushort siphonResource = affordances.ResourceTypeIndex != DivineHandConstants.NoResourceType
                        ? affordances.ResourceTypeIndex
                        : state.HeldResourceTypeIndex;

                    EmitSimpleCommand(ref commands, currentTick, HandCommandKind.Siphon, target, aim.TargetPosition, float3.zero, 0f, normalizedChargeLevel, siphonResource, state.HeldAmount);
                }
            }

            if (wantsDump)
            {
                EmitSimpleCommand(ref commands, currentTick, HandCommandKind.Dump, affordances.TargetEntity, aim.TargetPosition, float3.zero, 0f, normalizedChargeLevel, state.HeldResourceTypeIndex, state.HeldAmount);
            }
        }

        private bool TryEmitMiracleCastFromSlot(Entity handEntity,
            in DivineHandState state,
            AimPoint aim,
            float normalizedChargeLevel,
            uint currentTick,
            DynamicBuffer<HandCommandElement> commands,
            DynamicBuffer<MiracleReleaseEvent> miracleEvents)
        {
            if (!_miracleCasterLookup.HasComponent(handEntity) || !_miracleSlotLookup.TryGetBuffer(handEntity, out var slots))
            {
                return false;
            }

            var casterState = _miracleCasterLookup[handEntity];

            if (!TryResolveMiracleSlot(slots, casterState.SelectedSlot, out var slot))
            {
                return false;
            }

            if (slot.Type == MiracleType.None)
            {
                return false;
            }

            var release = new MiracleReleaseEvent
            {
                Type = slot.Type,
                Position = aim.TargetPosition,
                Normal = aim.Normal,
                TargetEntity = aim.TargetEntity,
                Direction = state.AimDirection,
                Impulse = 0f,
                ConfigEntity = slot.ConfigEntity
            };
            miracleEvents.Add(release);

            EmitSimpleCommand(ref commands, currentTick, HandCommandKind.CastMiracle, aim.TargetEntity, aim.TargetPosition, state.AimDirection, 0f, normalizedChargeLevel, 0, 0f);
            return true;
        }

        private static bool TryResolveMiracleSlot(DynamicBuffer<MiracleSlotDefinition> slots, byte selectedSlot, out MiracleSlotDefinition slot)
        {
            if (!slots.IsCreated || slots.Length == 0)
            {
                slot = default;
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].SlotIndex == selectedSlot)
                {
                    slot = slots[i];
                    return true;
                }
            }

            slot = slots[0];
            return true;
        }

        private static void EmitSimpleCommand(ref DynamicBuffer<HandCommandElement> commands,
            uint currentTick,
            HandCommandKind type,
            Entity targetEntity,
            float3 targetPosition,
            float3 direction,
            float speed,
            float chargeLevel,
            ushort resourceTypeIndex,
            float amount)
        {
            var command = new HandCommandElement
            {
                Tick = currentTick,
                Type = type,
                TargetEntity = targetEntity,
                TargetPosition = targetPosition,
                Direction = direction,
                Speed = speed,
                ChargeLevel = chargeLevel,
                ResourceTypeIndex = resourceTypeIndex,
                Amount = amount
            };
            commands.Add(command);
        }

    }
}
#pragma warning restore 618
