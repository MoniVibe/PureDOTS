# Divine Hand State Machine (Godgame)

## Overview

The Divine Hand is the player's primary interaction mechanism in Godgame. It handles multiple overlapping behaviors: picking up entities, throwing objects, casting miracles, siphoning resources, dumping to storehouses, and ground interactions. This document defines the **unified state machine** and **priority resolution system** to prevent conflicts between these competing actions.

---

## Core Concept

The divine hand operates on a **priority-based input router** that resolves which action wins when multiple inputs occur simultaneously. The hand maintains **explicit state** (Idle, Holding, Casting, etc.) with deterministic transitions and cooldown timers to prevent action spam.

---

## Hand State Machine

### State Enum

```csharp
public enum HandState : byte
{
    Idle,               // No action, cursor visible
    Hovering,           // Cursor over interactable (highlight feedback)
    PickingUp,          // Grab initiated, lerping entity to hand (5 tick transition)
    Holding,            // Entity attached to hand, following cursor (full mobility)
    Charging,           // Throw charge building (0-100% over time)
    Throwing,           // Throw released, entity detached with impulse
    CastingMiracle,     // Gesture recognition active OR miracle token spawning
    HoldingMiracle,     // Miracle token in hand, can charge or throw
    ChannelingMiracle,  // Sustained miracle active (rain, heal, shield)
    Siphoning,          // RMB hold over resource pile, extracting
    DumpingStorehouse,  // RMB hold over storehouse, depositing held resources
    Forcing,            // Push/pull entities without picking up (directional force)
    RaisingLowering,    // Vertical manipulation (limited horizontal mobility)
    Digging,            // Excavating terrain/resources (MMB hold over ground)
    Cooldown            // Post-action lockout (prevents spam)
}
```

### State Transitions

```
Idle
  ├─→ Hovering (cursor over HandPickable/Forceable/Raiseable)
  ├─→ CastingMiracle (miracle hotkey OR gesture start)
  ├─→ Siphoning (RMB hold over ResourcePile)
  ├─→ Forcing (MMB hold over Forceable entity)
  └─→ Digging (MMB hold over terrain/ground)

Hovering
  ├─→ PickingUp (LMB press on HandPickable)
  ├─→ RaisingLowering (LMB press on Raiseable entity)
  ├─→ Forcing (MMB hold on Forceable entity)
  ├─→ Idle (cursor leaves interactable)
  └─→ CastingMiracle (miracle hotkey)

PickingUp (5 tick lerp)
  └─→ Holding (lerp complete)

Holding (full mobility, entity follows cursor in 3D space)
  ├─→ Charging (LMB hold >0.2s)
  ├─→ Throwing (LMB release if not charged)
  ├─→ DumpingStorehouse (RMB hold over Storehouse, if holding resource)
  └─→ Idle (RMB press = drop entity)

RaisingLowering (vertical manipulation, limited horizontal mobility)
  ├─→ Idle (LMB release = drop entity at new height)
  └─→ Holding (Shift + LMB = convert to full hold)

Charging
  ├─→ Throwing (LMB release, apply throw impulse)
  └─→ Holding (charge interrupted)

Throwing (1 tick impulse, then release)
  └─→ Cooldown (50 tick lockout)

CastingMiracle (gesture recognition OR instant hotkey spawn)
  └─→ HoldingMiracle (miracle token spawned)

HoldingMiracle
  ├─→ Charging (LMB hold, miracle charge builds)
  ├─→ Throwing (LMB release, throw miracle token)
  └─→ ChannelingMiracle (RMB hold for sustained miracles)

ChannelingMiracle
  └─→ Cooldown (RMB release OR mana depleted)

Siphoning
  ├─→ Holding (siphon capacity reached, holding resource chunk)
  └─→ Cooldown (RMB release, partial siphon)

DumpingStorehouse
  └─→ Cooldown (RMB release OR all resources deposited)

Forcing (push/pull without picking up)
  ├─→ Idle (MMB release)
  └─→ Cooldown (force duration exceeded)

Digging (excavate terrain/resources)
  ├─→ Holding (resource excavated, chunk in hand)
  ├─→ Idle (MMB release, partial dig)
  └─→ Cooldown (dig capacity reached)

Cooldown (N tick timer)
  └─→ Idle (timer expires)
```

---

## Input Priority Resolution

When multiple inputs occur on the same frame, the **RMB Router** resolves priority:

### Priority Ladder (Highest to Lowest)

1. **UI Elements** (always win, hand blocked)
   - Mouse over UI panels, buttons, HUD elements
   - Hand interactions disabled while UI focused

2. **Active Miracle Gesture** (blocks all other actions)
   - If `HandState == CastingMiracle`, gesture recognition owns input
   - LMB/RMB during gesture = continue/abort gesture, NOT other actions

3. **Miracle Channeling** (sustained delivery)
   - If `HandState == ChannelingMiracle`, RMB hold sustains effect
   - No pickups, siphons, or throws allowed during channeling

4. **Modal Tools** (build mode, zone painting, etc.)
   - Future implementation: construction tools, selection boxes
   - When active, hand interactions disabled

5. **Storehouse Dump** (RMB over Storehouse while Holding)
   - If `HandState == Holding` AND cursor over Storehouse AND RMB pressed
   - Transition to `DumpingStorehouse`

6. **Resource Pile Siphon** (RMB over ResourcePile)
   - If `HandState == Idle` AND cursor over ResourcePile AND RMB hold
   - Transition to `Siphoning`

7. **Object Grab/Throw** (LMB on HandPickable)
   - If `HandState == Idle/Hovering` AND cursor over HandPickable AND LMB press
   - Transition to `PickingUp`

8. **Ground Drip** (RMB hold over terrain)
   - If `HandState == Holding` AND holding resource AND RMB over terrain
   - Drop single resource unit per 10 ticks (slow drip)

**Priority Rules**:
- Higher priority actions **interrupt** lower priority actions
- Same priority actions are **queued** (finish current before starting next)
- Cooldown state **blocks all actions** (except UI)

---

## Hand Components

```csharp
// Singleton divine hand state
public struct DivineHandState : IComponentData
{
    public HandState CurrentState;
    public Entity HeldEntity;                    // Entity.Null if not holding
    public HandAction PendingAction;             // Queued action if priority blocked

    // Timers
    public ushort StateTimer;                    // Ticks in current state
    public ushort CooldownRemaining;             // Ticks until Cooldown → Idle
    public ushort GrabLerpProgress;              // 0-100 during PickingUp
    public ushort ChargePercent;                 // 0-100 during Charging

    // Targeting
    public float3 CursorWorldPosition;
    public Entity HoveredEntity;                 // What cursor is over
    public InteractionType HoveredType;          // HandPickable, ResourcePile, Storehouse, etc.

    // Config tunables
    public float PickupRadius;                   // Max distance to grab
    public float HoldOffset;                     // Height above cursor when holding
    public float ThrowStrengthMax;               // Max throw impulse
    public ushort ChargeTimeMax;                 // Ticks to reach 100% charge
}

public enum HandAction : byte
{
    None,
    Grab,
    Throw,
    CastMiracle,
    Siphon,
    Dump,
    Drip
}

public enum InteractionType : byte
{
    None,
    HandPickable,           // Generic grabbable entity
    ResourcePile,           // Can siphon
    Storehouse,             // Can dump
    MiracleToken,           // Can pick up and throw
    Villager,               // Can bless/curse (future)
    Building                // Can select/upgrade (future)
}
```

```csharp
// Input component (written by gameplay bridge every frame)
public struct DivineHandInput : IComponentData
{
    public float3 CursorPosition;                // World position under cursor
    public float3 AimDirection;                  // Camera forward (for throw)

    public bool LMBPressed;                      // This frame press
    public bool LMBHeld;                         // Continuous hold
    public bool LMBReleased;                     // This frame release

    public bool RMBPressed;
    public bool RMBHeld;
    public bool RMBReleased;

    public MiracleHotkey MiracleKey;             // Optional miracle hotkey
    public float ScrollDelta;                    // Future: adjust hold height
}

public enum MiracleHotkey : byte
{
    None,
    Rain,
    Fire,
    Heal,
    Shield,
    Lightning,
    Earthquake,
    Forest,
    Freeze
}
```

```csharp
// Tag for entities the hand can pick up
public struct HandPickable : IComponentData
{
    public float FollowLerp;                     // 0.0-1.0 (how fast entity follows cursor)
    public float Mass;                           // Affects throw physics
    public float ThrowMultiplier;                // Modifies throw strength
    public PickableCategory Category;
}

public enum PickableCategory : byte
{
    Generic,                // Misc objects
    Resource,               // Resource chunks (wood, ore, food)
    MiracleToken,           // Miracle orbs
    Villager,               // Individual villagers (can throw, rude but possible)
    Creature                // Animals, enemies
}
```

---

## Core Systems

### 1. HandInputRouterSystem

**Group**: `HandSystemGroup` (runs early in simulation)

**Responsibility**: Read `DivineHandInput`, detect hovered entities, resolve priority, update `DivineHandState`

**Logic**:
```csharp
public partial struct HandInputRouterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();
        var input = SystemAPI.GetSingleton<DivineHandInput>();
        var rewindState = SystemAPI.GetSingleton<RewindState>();

        // Skip during playback
        if (rewindState.Mode != RewindMode.Record)
            return;

        // Update cursor position
        handState.ValueRW.CursorWorldPosition = input.CursorPosition;

        // Detect hovered entity (raycast/spatial query)
        DetectHoveredEntity(ref handState.ValueRW, input.CursorPosition);

        // State machine transitions
        switch (handState.ValueRO.CurrentState)
        {
            case HandState.Idle:
                HandleIdleState(ref handState.ValueRW, input);
                break;

            case HandState.Hovering:
                HandleHoveringState(ref handState.ValueRW, input);
                break;

            case HandState.Holding:
                HandleHoldingState(ref handState.ValueRW, input);
                break;

            case HandState.Charging:
                HandleChargingState(ref handState.ValueRW, input);
                break;

            case HandState.CastingMiracle:
                HandleCastingMiracleState(ref handState.ValueRW, input);
                break;

            case HandState.Siphoning:
                HandleSiphoningState(ref handState.ValueRW, input);
                break;

            case HandState.DumpingStorehouse:
                HandleDumpingState(ref handState.ValueRW, input);
                break;

            case HandState.Cooldown:
                HandleCooldownState(ref handState.ValueRW);
                break;
        }

        // Increment timers
        handState.ValueRW.StateTimer++;
    }
}
```

**Priority Check Example** (Holding state):
```csharp
void HandleHoldingState(ref DivineHandState hand, DivineHandInput input)
{
    // Priority 5: Storehouse dump (RMB over Storehouse)
    if (input.RMBPressed && hand.HoveredType == InteractionType.Storehouse)
    {
        if (CanDumpToStorehouse(hand.HeldEntity))
        {
            hand.CurrentState = HandState.DumpingStorehouse;
            hand.StateTimer = 0;
            return; // Highest priority, early exit
        }
    }

    // Priority 7: Throw (LMB release)
    if (input.LMBReleased)
    {
        ThrowHeldEntity(hand);
        hand.CurrentState = HandState.Throwing;
        hand.StateTimer = 0;
        return;
    }

    // Priority 7: Charge (LMB hold)
    if (input.LMBHeld && hand.StateTimer > 12) // >0.2s threshold
    {
        hand.CurrentState = HandState.Charging;
        hand.StateTimer = 0;
        hand.ChargePercent = 0;
        return;
    }

    // Priority 8: Ground drip (RMB hold over terrain)
    if (input.RMBHeld && hand.HoveredType == InteractionType.None)
    {
        if (hand.StateTimer % 10 == 0) // Every 10 ticks
        {
            DripSingleResource(hand.HeldEntity, input.CursorPosition);
        }
    }

    // Default: Drop entity (RMB press)
    if (input.RMBPressed)
    {
        DropHeldEntity(hand);
        hand.CurrentState = HandState.Idle;
        hand.HeldEntity = Entity.Null;
    }
}
```

---

### 2. HandHoldingSystem

**Group**: `HandSystemGroup` (after HandInputRouterSystem)

**Responsibility**: Lerp held entity to cursor position, apply `HandHeldTag`

**Logic**:
```csharp
[BurstCompile]
public partial struct HandHoldingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingleton<DivineHandState>();

        if (handState.HeldEntity == Entity.Null)
            return;

        if (handState.CurrentState != HandState.Holding &&
            handState.CurrentState != HandState.HoldingMiracle &&
            handState.CurrentState != HandState.Charging)
            return;

        // Get held entity transform
        var transform = SystemAPI.GetComponentRW<LocalTransform>(handState.HeldEntity);
        var pickable = SystemAPI.GetComponent<HandPickable>(handState.HeldEntity);

        // Target position = cursor + vertical offset
        var targetPos = handState.CursorWorldPosition + new float3(0, handState.HoldOffset, 0);

        // Lerp toward target
        transform.ValueRW.Position = math.lerp(
            transform.ValueRO.Position,
            targetPos,
            pickable.FollowLerp * SystemAPI.GetSingleton<TimeState>().FixedDeltaTime
        );
    }
}
```

---

### 3. HandPickupSystem

**Group**: `HandSystemGroup`

**Responsibility**: Handle `PickingUp` state lerp, attach `HandHeldTag` on completion

```csharp
public partial struct HandPickupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();

        if (handState.ValueRO.CurrentState != HandState.PickingUp)
            return;

        // Increment lerp progress
        handState.ValueRW.GrabLerpProgress += 20; // 5 ticks to complete (20*5=100)

        if (handState.ValueRO.GrabLerpProgress >= 100)
        {
            // Lerp complete, attach HandHeldTag
            state.EntityManager.AddComponent<HandHeldTag>(handState.ValueRO.HeldEntity);

            handState.ValueRW.CurrentState = HandState.Holding;
            handState.ValueRW.StateTimer = 0;
            handState.ValueRW.GrabLerpProgress = 0;
        }
    }
}
```

---

### 4. HandThrowSystem

**Group**: `HandSystemGroup`

**Responsibility**: Apply throw impulse when `Throwing` state entered

```csharp
public partial struct HandThrowSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();

        if (handState.ValueRO.CurrentState != HandState.Throwing)
            return;

        var heldEntity = handState.ValueRO.HeldEntity;
        if (heldEntity == Entity.Null)
            return;

        // Remove HandHeldTag
        state.EntityManager.RemoveComponent<HandHeldTag>(heldEntity);

        // Apply throw impulse (physics or custom velocity)
        var pickable = SystemAPI.GetComponent<HandPickable>(heldEntity);
        var input = SystemAPI.GetSingleton<DivineHandInput>();

        float throwStrength = handState.ValueRO.ThrowStrengthMax * (handState.ValueRO.ChargePercent / 100f);
        throwStrength *= pickable.ThrowMultiplier;

        float3 throwDirection = math.normalize(input.AimDirection);
        float3 throwImpulse = throwDirection * throwStrength;

        // Apply to entity (RainCloudState.Velocity, PhysicsVelocity, etc.)
        if (SystemAPI.HasComponent<RainCloudState>(heldEntity))
        {
            var cloudState = SystemAPI.GetComponentRW<RainCloudState>(heldEntity);
            cloudState.ValueRW.Velocity = throwImpulse;
        }
        else if (SystemAPI.HasComponent<PhysicsVelocity>(heldEntity))
        {
            var physicsVel = SystemAPI.GetComponentRW<PhysicsVelocity>(heldEntity);
            physicsVel.ValueRW.Linear = throwImpulse;
        }

        // Transition to cooldown
        handState.ValueRW.HeldEntity = Entity.Null;
        handState.ValueRW.CurrentState = HandState.Cooldown;
        handState.ValueRW.CooldownRemaining = 50; // 50 tick lockout
        handState.ValueRW.StateTimer = 0;
    }
}
```

---

### 5. HandSiphonSystem

**Group**: `HandSystemGroup`

**Responsibility**: Extract resources from ResourcePile while `Siphoning` state active

```csharp
public partial struct HandSiphonSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();

        if (handState.ValueRO.CurrentState != HandState.Siphoning)
            return;

        var input = SystemAPI.GetSingleton<DivineHandInput>();

        // RMB released → stop siphoning
        if (input.RMBReleased)
        {
            handState.ValueRW.CurrentState = HandState.Cooldown;
            handState.ValueRW.CooldownRemaining = 30;
            return;
        }

        // Siphon rate: 1 resource per 5 ticks
        if (handState.ValueRO.StateTimer % 5 != 0)
            return;

        var pileEntity = handState.ValueRO.HoveredEntity;
        if (pileEntity == Entity.Null)
            return;

        // Get pile data (custom component, not defined here)
        // Example: ResourcePile has Units, TypeId
        // Extract 1 unit, add to hand's virtual inventory
        // When capacity reached (e.g., 50 units), spawn ResourceChunk entity and transition to Holding

        // Pseudo-code:
        // ExtractResourceFromPile(pileEntity, 1);
        // handInventory.Count++;
        // if (handInventory.Count >= 50)
        // {
        //     var chunk = SpawnResourceChunk(handInventory.TypeId, 50);
        //     handState.ValueRW.HeldEntity = chunk;
        //     handState.ValueRW.CurrentState = HandState.Holding;
        // }
    }
}
```

---

### 6. HandStorehouseDumpSystem

**Group**: `HandSystemGroup`

**Responsibility**: Deposit held resources into Storehouse while `DumpingStorehouse` state active

```csharp
public partial struct HandStorehouseDumpSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();

        if (handState.ValueRO.CurrentState != HandState.DumpingStorehouse)
            return;

        var input = SystemAPI.GetSingleton<DivineHandInput>();

        // RMB released → stop dumping
        if (input.RMBReleased)
        {
            handState.ValueRW.CurrentState = HandState.Cooldown;
            handState.ValueRW.CooldownRemaining = 30;
            return;
        }

        // Dump rate: 10 resources per tick (fast)
        var storehouseEntity = handState.ValueRO.HoveredEntity;
        if (storehouseEntity == Entity.Null)
            return;

        var heldEntity = handState.ValueRO.HeldEntity;
        if (heldEntity == Entity.Null)
            return;

        // Get held resource chunk data
        // Example: ResourceChunk has Units, TypeId
        // Transfer units to Storehouse
        // When chunk depleted, destroy chunk entity and transition to Idle

        // Pseudo-code:
        // TransferResourcesToStorehouse(storehouseEntity, heldEntity, 10);
        // if (chunk.Units <= 0)
        // {
        //     state.EntityManager.DestroyEntity(heldEntity);
        //     handState.ValueRW.HeldEntity = Entity.Null;
        //     handState.ValueRW.CurrentState = HandState.Idle;
        // }
    }
}
```

---

## Miracle Integration

Miracles hook into the hand state machine via specific states:

### Miracle Casting Flow

1. **Hotkey Pressed** OR **Gesture Recognized**
   - `HandState.Idle` → `HandState.CastingMiracle`
   - Gesture recognition system (if implemented) runs during `CastingMiracle` state
   - On success: spawn miracle token entity with `HandPickable + MiracleToken` components

2. **Miracle Token Spawned**
   - `HandState.CastingMiracle` → `HandState.HoldingMiracle`
   - Token becomes `HeldEntity`
   - Hand can now charge or throw token

3. **Charge Miracle** (optional)
   - LMB hold → `HandState.Charging`
   - `ChargePercent` increments based on `ChargeTimeMax` config
   - Visual feedback: token glows brighter
   - Power level determined by charge: <30% = Basic, 30-70% = Increased, >70% = Extreme

4. **Throw Miracle Token**
   - LMB release → `HandState.Throwing`
   - Token detached, throw impulse applied
   - Token physics/collision triggers miracle effect on impact

5. **Channeling Sustained Miracle** (Rain, Heal, Shield)
   - RMB hold while `HoldingMiracle` → `HandState.ChannelingMiracle`
   - Miracle effect follows cursor position
   - Mana drains per tick
   - RMB release OR mana depleted → `HandState.Cooldown`

**Key Difference from Regular Pickups**:
- Miracle tokens use `HoldingMiracle` state instead of `Holding`
- Enables channeling option (RMB hold sustains effect instead of dumping)
- Miracle hotkeys bypass grab priority (can cast even when holding entity - drop first)

---

## Resource Discipline & Single-Type Locking

**Problem**: Player holding 50 wood shouldn't be able to siphon ore (resource type mismatch)

**Solution**: Enforce single resource type per hand session

```csharp
public struct HandResourceInventory : IComponentData
{
    public ushort TypeId;                        // 0 = empty, >0 = locked to type
    public ushort Units;                         // Current count
    public ushort CapacityMax;                   // Max before spawning chunk
}
```

**Rules**:
- Siphoning first unit locks `TypeId`
- Subsequent siphons must match locked type (or rejected)
- Dumping to storehouse clears lock when `Units == 0`
- Dropping chunk clears lock immediately

**System Logic** (in HandSiphonSystem):
```csharp
var inventory = SystemAPI.GetSingletonRW<HandResourceInventory>();

if (inventory.ValueRO.TypeId != 0 && inventory.ValueRO.TypeId != pileTypeId)
{
    // Type mismatch, deny siphon with audio/visual feedback
    return;
}

// Lock to this type if first siphon
if (inventory.ValueRO.TypeId == 0)
{
    inventory.ValueRW.TypeId = pileTypeId;
}

// Extract resource
inventory.ValueRW.Units++;
```

---

## Cooldown & Hysteresis

**Problem**: Rapid action spam (throw → grab → throw loop)

**Solution**: Cooldown state after high-impact actions

**Cooldown Durations** (in ticks):
- **Throw**: 50 ticks (~0.8s)
- **Miracle cast**: 100 ticks (~1.6s, prevents miracle spam)
- **Siphon interrupt**: 30 ticks (~0.5s)
- **Dump interrupt**: 30 ticks (~0.5s)

**Hysteresis** (prevents accidental triggers):
- LMB must be held >12 ticks (0.2s) before Charging activates
- RMB must be held >6 ticks (0.1s) before Siphoning starts
- Prevents accidental charges/siphons from brief clicks

---

## Rewind Integration

Hand state must save/restore during rewind.

```csharp
public struct HandHistorySample : IBufferElementData
{
    public ushort Tick;
    public HandState State;
    public Entity HeldEntity;
    public ushort ChargePercent;
    public ushort CooldownRemaining;
    public float3 CursorPosition;
}
```

**Recording** (every tick during Record mode):
```csharp
var history = SystemAPI.GetSingletonBuffer<HandHistorySample>();
history.Add(new HandHistorySample
{
    Tick = timeState.CurrentTick,
    State = handState.CurrentState,
    HeldEntity = handState.HeldEntity,
    ChargePercent = handState.ChargePercent,
    CooldownRemaining = handState.CooldownRemaining,
    CursorPosition = handState.CursorWorldPosition
});
```

**Playback** (during Playback mode):
```csharp
var sample = FindHistorySampleForTick(rewindState.PlaybackTick);
handState.CurrentState = sample.State;
handState.HeldEntity = sample.HeldEntity;
handState.ChargePercent = sample.ChargePercent;
handState.CooldownRemaining = sample.CooldownRemaining;
handState.CursorWorldPosition = sample.CursorPosition;
```

---

## Hover Detection & Targeting

**HandHoverDetectionSystem** runs before HandInputRouterSystem to populate `HoveredEntity` and `HoveredType`.

```csharp
[BurstCompile]
public partial struct HandHoverDetectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();
        var cursorPos = handState.ValueRO.CursorWorldPosition;

        // Raycast or spatial query to find nearest interactable
        Entity closest = Entity.Null;
        InteractionType closestType = InteractionType.None;
        float closestDist = handState.ValueRO.PickupRadius;

        // Query HandPickable entities
        foreach (var (transform, pickable, entity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<HandPickable>>()
                          .WithEntityAccess())
        {
            float dist = math.distance(cursorPos, transform.ValueRO.Position);
            if (dist < closestDist)
            {
                closest = entity;
                closestType = InteractionType.HandPickable;
                closestDist = dist;
            }
        }

        // Query ResourcePile entities (custom component, not defined here)
        // Query Storehouse entities
        // ... similar loops ...

        // Update hand state
        handState.ValueRW.HoveredEntity = closest;
        handState.ValueRW.HoveredType = closestType;

        // Transition Idle → Hovering if entity detected
        if (handState.ValueRO.CurrentState == HandState.Idle && closest != Entity.Null)
        {
            handState.ValueRW.CurrentState = HandState.Hovering;
        }
        else if (handState.ValueRO.CurrentState == HandState.Hovering && closest == Entity.Null)
        {
            handState.ValueRW.CurrentState = HandState.Idle;
        }
    }
}
```

---

## Camera Integration

**Problem**: Camera orbiting conflicts with hand drag panning

**Solution**: Hand state broadcasts "input locked" flag to camera system

```csharp
public struct CameraInputLock : IComponentData
{
    public bool IsLocked;                        // True when hand owns input
    public CameraLockReason Reason;
}

public enum CameraLockReason : byte
{
    None,
    HandHoldingEntity,
    HandCastingMiracle,
    HandSiphoning,
    HandDumping
}
```

**Camera system checks lock**:
```csharp
var cameraLock = SystemAPI.GetSingleton<CameraInputLock>();
if (cameraLock.IsLocked)
{
    // Disable orbit, zoom, pan during hand actions
    return;
}
```

**Hand system sets lock**:
```csharp
var cameraLock = SystemAPI.GetSingletonRW<CameraInputLock>();

switch (handState.CurrentState)
{
    case HandState.Holding:
    case HandState.Charging:
    case HandState.HoldingMiracle:
        cameraLock.ValueRW.IsLocked = true;
        cameraLock.ValueRW.Reason = CameraLockReason.HandHoldingEntity;
        break;

    case HandState.Siphoning:
        cameraLock.ValueRW.IsLocked = true;
        cameraLock.ValueRW.Reason = CameraLockReason.HandSiphoning;
        break;

    default:
        cameraLock.ValueRW.IsLocked = false;
        break;
}
```

---

## Presentation & Feedback

Hand state drives visual/audio cues via presentation bridge.

```csharp
// MonoBehaviour bridge (not DOTS, reads singleton)
public class HandPresentationBridge : MonoBehaviour
{
    void Update()
    {
        var handState = GetSingletonData<DivineHandState>();

        switch (handState.CurrentState)
        {
            case HandState.Hovering:
                ShowHighlight(handState.HoveredEntity);
                SetCursorIcon(CursorIcon.Hand);
                break;

            case HandState.Holding:
                HideHighlight();
                SetCursorIcon(CursorIcon.Grab);
                break;

            case HandState.Charging:
                ShowChargeGlow(handState.ChargePercent);
                PlayChargingSound(handState.ChargePercent);
                break;

            case HandState.CastingMiracle:
                ShowGestureTrail();
                SetCursorIcon(CursorIcon.Magic);
                break;

            case HandState.Siphoning:
                ShowSiphonVFX(handState.HoveredEntity);
                PlaySiphonLoopSound();
                break;

            case HandState.Cooldown:
                SetCursorIcon(CursorIcon.Wait);
                break;

            default:
                SetCursorIcon(CursorIcon.Default);
                break;
        }
    }
}
```

---

## New Action Systems

### Force Action (Push/Pull Without Picking Up)

**Use Case**: Move heavy/large entities that can't be picked up normally, or apply directional impulses without grabbing.

```csharp
// Tag for entities that can be forced (pushed/pulled)
public struct Forceable : IComponentData
{
    public float Mass;                           // Affects force resistance
    public float ForceMultiplier;                // How easily this entity is moved by force
    public bool CanBePicked;                     // False = only forceable, not pickable
}

// Component added during Forcing state
public struct BeingForced : IComponentData
{
    public float3 ForceDirection;                // Direction of applied force
    public float ForceStrength;                  // Magnitude (0-100)
    public Entity ForcingHand;                   // Which hand is forcing (for multi-hand future)
}
```

**Force Mechanics**:
- MMB hold over Forceable entity → transition to Forcing state
- Hand cursor determines force direction (from entity toward cursor = pull, away = push)
- Force strength builds over hold duration (like throw charge): `Strength = min(100, HoldTicks * 2)`
- Entity receives impulse every tick: `Impulse = ForceDirection * ForceStrength * ForceMultiplier / Mass`
- MMB release → apply final impulse, transition to Idle
- Max force duration: 200 ticks (3.2s), then auto-cooldown

**Force System**:
```csharp
public partial struct HandForceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();

        if (handState.ValueRO.CurrentState != HandState.Forcing)
            return;

        var input = SystemAPI.GetSingleton<DivineHandInput>();
        var targetEntity = handState.ValueRO.HoveredEntity;

        // MMB released → stop forcing
        if (input.MMBReleased || handState.ValueRO.StateTimer > 200)
        {
            state.EntityManager.RemoveComponent<BeingForced>(targetEntity);
            handState.ValueRW.CurrentState = HandState.Cooldown;
            handState.ValueRW.CooldownRemaining = 30;
            return;
        }

        // Build force strength
        float forceStrength = math.min(100f, handState.ValueRO.StateTimer * 2f);

        // Calculate force direction (entity toward cursor)
        var targetTransform = SystemAPI.GetComponent<LocalTransform>(targetEntity);
        float3 forceDir = math.normalize(handState.ValueRO.CursorWorldPosition - targetTransform.Position);

        // Apply impulse
        var forceable = SystemAPI.GetComponent<Forceable>(targetEntity);
        float3 impulse = forceDir * forceStrength * forceable.ForceMultiplier / forceable.Mass;

        // Apply to entity (physics or custom velocity)
        if (SystemAPI.HasComponent<PhysicsVelocity>(targetEntity))
        {
            var physicsVel = SystemAPI.GetComponentRW<PhysicsVelocity>(targetEntity);
            physicsVel.ValueRW.Linear += impulse * SystemAPI.GetSingleton<TimeState>().FixedDeltaTime;
        }
    }
}
```

**Example Use Cases**:
- Push boulder down hill (too heavy to pick up)
- Pull large log across ground (drag without lifting)
- Push villagers away from danger (gentle nudge)
- Directional force on water (create currents)

---

### Raise/Lower Action (Limited Mobility Vertical Manipulation)

**Use Case**: Adjust entity height without full 3D freedom (lifting platforms, adjusting water levels, raising buildings).

```csharp
// Tag for entities that can be raised/lowered
public struct Raiseable : IComponentData
{
    public float MinHeight;                      // Lowest allowed position
    public float MaxHeight;                      // Highest allowed position
    public float CurrentHeight;                  // Current elevation
    public float RaiseSpeed;                     // Units per tick when raising
    public bool AllowHorizontalDrift;            // True = slight horizontal movement allowed
    public float HorizontalDriftMax;             // Max units from origin
}

// Component tracking raise/lower state
public struct BeingRaisedLowered : IComponentData
{
    public float3 OriginalPosition;              // Starting position (XZ locked, Y adjustable)
    public float TargetHeight;                   // Desired Y position
    public float RaiseRate;                      // Units per tick
}
```

**Raise/Lower Mechanics**:
- LMB press on Raiseable entity → transition to RaisingLowering state
- Cursor Y position determines target height (clamped to MinHeight/MaxHeight)
- Entity Y lerps toward cursor Y every tick
- **XZ position mostly locked**: horizontal drift limited to `HorizontalDriftMax` (e.g., ±5 units)
- LMB release → drop entity at current height, transition to Idle
- **Shift + LMB** while RaisingLowering → convert to full Holding state (unlock horizontal movement)

**Difference vs Holding**:
- **Holding**: Full 3D freedom, entity follows cursor in all axes
- **RaisingLowering**: Vertical focus, horizontal movement constrained

**Raise/Lower System**:
```csharp
public partial struct HandRaiseLowerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();

        if (handState.ValueRO.CurrentState != HandState.RaisingLowering)
            return;

        var input = SystemAPI.GetSingleton<DivineHandInput>();
        var targetEntity = handState.ValueRO.HeldEntity;

        // LMB released → drop at current height
        if (input.LMBReleased)
        {
            state.EntityManager.RemoveComponent<BeingRaisedLowered>(targetEntity);
            handState.ValueRW.HeldEntity = Entity.Null;
            handState.ValueRW.CurrentState = HandState.Idle;
            return;
        }

        // Shift + LMB → convert to full Holding
        if (input.ShiftHeld && input.LMBHeld)
        {
            state.EntityManager.RemoveComponent<BeingRaisedLowered>(targetEntity);
            state.EntityManager.AddComponent<HandHeldTag>(targetEntity);
            handState.ValueRW.CurrentState = HandState.Holding;
            return;
        }

        // Lerp Y toward cursor Y
        var raiseable = SystemAPI.GetComponent<Raiseable>(targetEntity);
        var transform = SystemAPI.GetComponentRW<LocalTransform>(targetEntity);
        var beingRaised = SystemAPI.GetComponent<BeingRaisedLowered>(targetEntity);

        float targetY = math.clamp(handState.ValueRO.CursorWorldPosition.y, raiseable.MinHeight, raiseable.MaxHeight);
        float currentY = transform.ValueRO.Position.y;
        float newY = math.lerp(currentY, targetY, raiseable.RaiseSpeed);

        // Lock XZ (or allow limited drift)
        float3 newPos = transform.ValueRO.Position;
        newPos.y = newY;

        if (raiseable.AllowHorizontalDrift)
        {
            // Limit horizontal drift
            float2 drift = handState.ValueRO.CursorWorldPosition.xz - beingRaised.OriginalPosition.xz;
            float driftMag = math.length(drift);
            if (driftMag > raiseable.HorizontalDriftMax)
                drift = math.normalize(drift) * raiseable.HorizontalDriftMax;

            newPos.x = beingRaised.OriginalPosition.x + drift.x;
            newPos.z = beingRaised.OriginalPosition.z + drift.y;
        }

        transform.ValueRW.Position = newPos;
    }
}
```

**Example Use Cases**:
- Raise platform to create bridge
- Lower water level in reservoir (drain)
- Lift building foundation during construction
- Adjust terrain elevation (limited sculpting)

---

### Dig Action (Excavate Terrain/Resources)

**Use Case**: Extract resources from ground, excavate terrain, create holes/trenches.

```csharp
// Tag for terrain/ground that can be dug
public struct Diggable : IComponentData
{
    public DiggableType Type;
    public ushort ResourceTypeId;                // What resource this yields (if any)
    public ushort ResourceAmount;                // How much resource remains
    public float DigHardness;                    // 0.0-10.0 (how hard to dig, affects speed)
    public bool IsDepletable;                    // True = runs out, false = infinite (terrain)
}

public enum DiggableType : byte
{
    Soil,           // Dirt, farmland
    Rock,           // Stone deposits
    Ore,            // Metal ore veins
    Clay,           // Clay deposits
    Sand,           // Sand terrain
    Ice,            // Frozen ground
    Terrain         // Generic terrain sculpting
}

// Component tracking active dig
public struct BeingDug : IComponentData
{
    public float3 DigPosition;                   // Where hand is digging
    public ushort DigProgress;                   // 0-100% (to extract 1 resource unit)
    public ushort ExtractedTotal;                // Total units extracted this session
}
```

**Dig Mechanics**:
- MMB hold over Diggable terrain/ground → transition to Digging state
- Dig progress accumulates: `Progress += (100 / DigHardness) per tick`
- When progress reaches 100: extract 1 resource unit, reset progress
- Extracted resources accumulate in hand inventory (like siphoning)
- When hand capacity reached (50 units): spawn ResourceChunk, transition to Holding
- MMB release → stop digging, transition to Idle (partial dig OK)
- Max dig duration: 500 ticks before cooldown

**Dig System**:
```csharp
public partial struct HandDigSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var handState = SystemAPI.GetSingletonRW<DivineHandState>();

        if (handState.ValueRO.CurrentState != HandState.Digging)
            return;

        var input = SystemAPI.GetSingleton<DivineHandInput>();
        var targetEntity = handState.ValueRO.HoveredEntity;

        // MMB released or duration exceeded → stop digging
        if (input.MMBReleased || handState.ValueRO.StateTimer > 500)
        {
            state.EntityManager.RemoveComponent<BeingDug>(targetEntity);
            handState.ValueRW.CurrentState = HandState.Cooldown;
            handState.ValueRW.CooldownRemaining = 50;
            return;
        }

        // Dig progress
        var diggable = SystemAPI.GetComponent<Diggable>(targetEntity);
        var beingDug = SystemAPI.GetComponentRW<BeingDug>(targetEntity);

        float digSpeed = 100f / diggable.DigHardness;
        beingDug.ValueRW.DigProgress += (ushort)digSpeed;

        // Extract resource unit when progress complete
        if (beingDug.ValueRO.DigProgress >= 100 && diggable.ResourceAmount > 0)
        {
            // Extract 1 unit
            var diggableRW = SystemAPI.GetComponentRW<Diggable>(targetEntity);
            if (diggableRW.ValueRO.IsDepletable)
                diggableRW.ValueRW.ResourceAmount--;

            // Add to hand inventory
            var handInventory = SystemAPI.GetSingletonRW<HandResourceInventory>();
            handInventory.ValueRW.Units++;

            // Reset progress
            beingDug.ValueRW.DigProgress = 0;
            beingDug.ValueRW.ExtractedTotal++;

            // Check capacity
            if (handInventory.ValueRO.Units >= handInventory.ValueRO.CapacityMax)
            {
                // Spawn resource chunk, transition to Holding
                Entity chunk = SpawnResourceChunk(handInventory.ValueRO.TypeId, handInventory.ValueRO.Units);
                handState.ValueRW.HeldEntity = chunk;
                handState.ValueRW.CurrentState = HandState.Holding;

                // Clear hand inventory
                handInventory.ValueRW.Units = 0;
                handInventory.ValueRW.TypeId = 0;
            }
        }
    }
}
```

**Example Use Cases**:
- Dig clay deposits for pottery/bricks
- Excavate ore veins for metal
- Dig trenches for defense/irrigation
- Harvest sand for glass production
- Terrain sculpting (lower ground level)

---

## Interaction Type Extensions

Updated InteractionType enum to support new actions:

```csharp
public enum InteractionType : byte
{
    None,
    HandPickable,           // Can pickup with LMB (Holding state)
    Raiseable,              // Can raise/lower with LMB (RaisingLowering state)
    Forceable,              // Can push/pull with MMB (Forcing state)
    ResourcePile,           // Can siphon with RMB
    Storehouse,             // Can dump with RMB
    Diggable,               // Can dig with MMB (Digging state)
    MiracleToken,           // Can pick up and throw/channel
    Villager,               // Can bless/curse (future)
    Building                // Can select/upgrade (future)
}
```

**Hover Detection Priority**:
1. UI Elements (blocks all)
2. HandPickable (LMB interaction)
3. Raiseable (LMB interaction, different from Pickable)
4. Forceable (MMB interaction)
5. Diggable (MMB interaction)
6. ResourcePile (RMB interaction)
7. Storehouse (RMB interaction, context-sensitive)

---

## Open Questions / Design Decisions Needed

1. **Multi-Entity Grab**: Can hand grab multiple small entities at once (e.g., 5 villagers)? Or always single entity?
   - *Suggestion*: Single entity for Phase 1, multi-grab as advanced feature later

2. **Villager Throwing Ethics**: Can player throw villagers? Does it damage them? Alignment penalty?
   - *Suggestion*: Yes (funny), minor damage, small Evil alignment shift

3. **Ground Drip Rate**: 1 resource per 10 ticks too fast/slow?
   - *Suggestion*: Configurable in HandState tunables, start at 10 ticks and playtest

4. **Miracle Hotkey Conflicts**: If holding entity and press miracle hotkey, drop entity first OR deny cast?
   - *Suggestion*: Auto-drop entity (transition Holding → Idle), then cast miracle

5. **Siphon Capacity**: Fixed 50 units OR variable by resource type (ore = 20, wood = 100)?
   - *Suggestion*: Variable per type, defined in ResourceTypeCatalog

6. **Storehouse Dump Speed**: 10 units/tick very fast (500 units in 5s). Too fast?
   - *Suggestion*: Reduce to 5 units/tick (250 units in 5s), feels more deliberate

7. **Cooldown Visual Feedback**: Grayed-out cursor? Timer UI? Pulse effect?
   - *Suggestion*: Cursor icon changes to "wait/hourglass", optional timer if cooldown >2s

8. **Gesture vs. Hotkey Default**: Should gestures be required, or hotkeys primary?
   - *Suggestion*: Hotkeys primary (accessibility), gestures optional "advanced" feature

---

## Implementation Notes

- **HandInputRouterSystem** = central state machine, runs first
- **HandHoverDetectionSystem** = runs before router to populate hovered data
- **HandHoldingSystem** = lerp held entity to cursor
- **HandPickupSystem** = handle PickingUp state transition
- **HandThrowSystem** = apply throw impulse
- **HandSiphonSystem** = extract resources from piles
- **HandStorehouseDumpSystem** = deposit resources
- **HandHistoryRecordSystem** = save state for rewind
- **All systems** respect `RewindState.Mode` (skip during Playback)

---

## References

- **Miracle Framework**: [MiraclesFramework_TODO.md](../TODO/MiraclesFramework_TODO.md) - Miracle casting integration
- **Rain Miracles**: [RainMiraclesAndHand.md](../DesignNotes/RainMiraclesAndHand.md) - Current implementation
- **Rewind Patterns**: [RewindPatterns.md](../DesignNotes/RewindPatterns.md) - History recording
- **Villager Jobs**: [VillagerJobs_DOTS.md](../DesignNotes/VillagerJobs_DOTS.md) - Resource interaction integration
- **Resource Registry**: Resource pile siphoning queries registry for pile locations
