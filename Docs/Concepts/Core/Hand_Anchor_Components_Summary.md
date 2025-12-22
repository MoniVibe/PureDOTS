# Hand/Anchor Components Summary

**Status:** Reference Document
**Category:** Core - Interaction / Hand System
**Audience:** Implementers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Purpose:** Summary of hand and anchor components used for pickup, throw, and primed throw systems. Documents the component structure for player/divine hand interactions, item holding, and formation/orbit anchors.

**Key Concepts:**
- **Hand Entities:** Represent player/divine hand for interaction
- **Hand Anchors:** Transform anchor points for holding items (Parent/Child hierarchy)
- **Prime Anchors:** Transform anchor points for primed throw formations
- **Held State:** Items attached to hand via transform hierarchy

---

## Hand Components

### PureDOTS Core Hand Components

**File:** `Packages/com.moni.puredots/Runtime/Runtime/Hand/DivineHandComponents.cs`

```csharp
/// <summary>
/// Tag component marking an entity as the divine hand.
/// </summary>
public struct DivineHandTag : IComponentData { }

/// <summary>
/// Tag component marking an entity as pickable by the divine hand.
/// </summary>
public struct PickableTag : IComponentData { }

/// <summary>
/// Tag component marking an entity as being held by a divine hand.
/// </summary>
public struct HandHeldTag : IComponentData
{
    public Entity Holder;  // Hand entity holding this item
}

/// <summary>
/// Divine hand state (cursor position, hovered entity, held entity).
/// </summary>
public struct DivineHandState : IComponentData
{
    public float3 CursorPosition;           // Current cursor world position
    public float3 CursorNormal;             // Surface normal at cursor
    public Entity HoveredEntity;            // Entity currently hovered
    
    // Held entity fields (used by Godgame miracle systems)
    public Entity HeldEntity;               // Currently held entity
    public ushort HeldResourceTypeIndex;    // Resource type of held item
    public float HeldAmount;                // Amount of held resource
    
    // Throw mode fields
    public bool ThrowModeEnabled;           // Whether throw mode is active
    public float3 HeldOffset;               // Local offset of held item
    public float3 LastAimDirection;         // Last aim direction for throwing
    public float LastStrength;              // Last throw strength
}
```

**File:** `Packages/com.moni.puredots/Runtime/Runtime/Hand/HandInteractionComponents.cs`

```csharp
/// <summary>
/// Hand state enum.
/// </summary>
public enum HandState : byte
{
    Idle = 0,
    Hovering = 1,
    Grabbing = 2,
    Holding = 3,
    Placing = 4,
    Casting = 5,
    Cooldown = 6
}

/// <summary>
/// Divine hand command types.
/// </summary>
public enum DivineHandCommandType : byte
{
    None = 0,
    Grab = 1,
    Drop = 2,
    Siphon = 3,
    Dump = 4,
    Miracle = 5,
    Cancel = 6
}

/// <summary>
/// Shared hand interaction state consumed by resource and miracle systems.
/// </summary>
public struct HandInteractionState : IComponentData
{
    public Entity HandEntity;               // Hand entity reference
    public HandState CurrentState;          // Current hand state
    public HandState PreviousState;         // Previous hand state
    public DivineHandCommandType ActiveCommand;  // Active command type
    public ushort ActiveResourceType;       // Active resource type
    public int HeldAmount;                  // Amount currently held
    public int HeldCapacity;                // Maximum holding capacity
    public float CooldownSeconds;           // Cooldown remaining
    public uint LastUpdateTick;             // Last update tick
    public byte Flags;                      // State flags
    
    public const byte FlagMiracleArmed = 1 << 0;
    public const byte FlagSiphoning = 1 << 1;
    public const byte FlagDumping = 1 << 2;
}

/// <summary>
/// Aggregated siphon state for resource and miracle chains.
/// </summary>
public struct ResourceSiphonState : IComponentData
{
    public Entity HandEntity;               // Hand entity doing siphoning
    public Entity TargetEntity;             // Target entity being siphoned
    public ushort ResourceTypeIndex;        // Resource type being siphoned
    public float SiphonRate;                // Units per second siphon rate
    public float DumpRate;                  // Units per second dump rate
    public float AccumulatedUnits;          // Accumulated units (for batching)
    public uint LastUpdateTick;             // Last update tick
    public byte Flags;                      // Siphon flags
    
    public const byte FlagSiphoning = 1 << 0;
    public const byte FlagDumpCommandPending = 1 << 1;
}
```

---

### Godgame-Specific Hand Components

**File:** `godgame/Assets/Scripts/Godgame/Runtime/DivineHandComponents.cs`

```csharp
/// <summary>
/// Godgame-specific hand state enum.
/// </summary>
public enum HandState : byte
{
    Empty = 0,
    Holding = 1,
    Dragging = 2,
    SlingshotAim = 3,
    Dumping = 4
}

/// <summary>
/// Configuration for Divine Hand behavior.
/// </summary>
public struct DivineHandConfig : IComponentData
{
    public float PickupRadius;              // Pickup interaction radius
    public float MaxGrabDistance;           // Maximum grab distance
    public float HoldLerp;                  // Lerp speed for held items
    public float ThrowImpulse;              // Base throw impulse strength
    public float ThrowChargeMultiplier;     // Charge multiplier for throws
    public float HoldHeightOffset;          // Height offset for held items
    public float CooldownAfterThrowSeconds; // Cooldown after throwing
    public float MinChargeSeconds;          // Minimum charge time
    public float MaxChargeSeconds;          // Maximum charge time
    public int HysteresisFrames;            // Hysteresis frames for state changes
    public int HeldCapacity;                // Maximum held capacity
    public float SiphonRate;                // Siphon rate (units/second)
    public float DumpRate;                  // Dump rate (units/second)
}

/// <summary>
/// Godgame-specific Divine Hand state.
/// </summary>
public struct DivineHandState : IComponentData
{
    public Entity HeldEntity;               // Currently held entity
    public float3 CursorPosition;           // Cursor world position
    public float3 AimDirection;             // Aim direction for throwing
    public float3 HeldLocalOffset;          // Local offset of held item
    public HandState CurrentState;          // Current hand state
    public HandState PreviousState;         // Previous hand state
    public float ChargeTimer;               // Charge timer (for throws)
    public float CooldownTimer;             // Cooldown timer
    public ushort HeldResourceTypeIndex;    // Resource type of held item
    public int HeldAmount;                  // Amount currently held
    public int HeldCapacity;                // Maximum holding capacity
    public byte Flags;                      // State flags
}

/// <summary>
/// Types of highlights for Divine Hand.
/// </summary>
public enum HandHighlightType : byte
{
    None = 0,
    Storehouse = 1,
    Pile = 2,
    Draggable = 3,
    Ground = 4
}
```

**File:** `godgame/Assets/Scripts/Godgame/Interaction/Hand/HandComponents.cs`

```csharp
/// <summary>
/// Input state for hand interaction.
/// </summary>
public struct InputState : IComponentData
{
    public float2 PointerPos;               // Pointer screen position
    public float2 PointerDelta;             // Pointer delta movement
    public float3 PointerWorld;             // Pointer world position
    public float Scroll;                    // Scroll input
    public bool PrimaryHeld;                // Primary button held
    public bool PrimaryClicked;             // Primary button clicked
    public bool SecondaryHeld;              // Secondary button held
    public bool MiddleHeld;                 // Middle button held
    public bool ThrowModifier;              // Throw modifier (Shift) held
    public bool PointerWorldValid;          // Whether pointer world position is valid
    public bool EffectTriggered;            // Effect trigger flag
    public float2 Move;                     // WASD movement
    public float Vertical;                  // Q/E vertical movement
    public bool CameraToggleMode;           // Camera mode toggle
}

/// <summary>
/// Hand history for velocity calculation.
/// </summary>
public struct HandHistory : IComponentData
{
    public float3 V0;                       // Position 4 frames ago
    public float3 V1;                       // Position 3 frames ago
    public float3 V2;                       // Position 2 frames ago
    public float3 V3;                       // Position 1 frame ago
}

/// <summary>
/// Hand component (state machine).
/// </summary>
public struct Hand : IComponentData
{
    public HandState State;                 // Current hand state
    // ... additional fields
}
```

---

## Pickup/Throw Components

**File:** `Packages/com.moni.puredots/Runtime/Runtime/Interaction/PickupThrowComponents.cs`

```csharp
/// <summary>
/// Marks an entity as allowed to be grabbed by the player/god.
/// </summary>
public struct Pickable : IComponentData { }

/// <summary>
/// Marks entity currently being held by a "hand" / player entity.
/// </summary>
public struct HeldByPlayer : IComponentData
{
    public Entity Holder;                   // Player/camera/hand entity holding this entity
    public float3 LocalOffset;              // Local offset from holder pivot
    public float3 HoldStartPosition;        // World position when pickup started
    public float HoldStartTime;             // Time when pickup started (in seconds)
}

/// <summary>
/// Indicates entity movement should be skipped by movement systems.
/// </summary>
public struct MovementSuppressed : IComponentData { }

/// <summary>
/// Marks entity in flight due to a throw.
/// </summary
public struct BeingThrown : IComponentData
{
    public float3 InitialVelocity;          // Initial velocity when thrown
    public float TimeSinceThrow;            // Time since throw started (in seconds)
}

/// <summary>
/// State machine for pickup/throw interaction.
/// </summary>
public struct PickupState : IComponentData
{
    public PickupStateType State;           // Current state of the pickup interaction
    public float3 LastRaycastPosition;      // Last raycast position for cursor movement tracking
    public float CursorMovementAccumulator; // Accumulated cursor movement (in world space units)
    public float HoldTime;                  // Time holding RMB (in seconds)
    public float3 AccumulatedVelocity;      // Velocity accumulated during throw priming
    public bool IsMoving;                   // Whether player is moving while holding
    public Entity TargetEntity;             // Entity currently being targeted/held
    public float3 LastHolderPosition;       // Last holder position for movement detection
}

/// <summary>
/// State types for pickup/throw interaction.
/// </summary>
public enum PickupStateType : byte
{
    Empty = 0,          // No interaction active
    AboutToPick = 1,    // About to pick up (RMB down, waiting for cursor movement >3px)
    Holding = 2,        // Currently holding an entity
    PrimedToThrow = 3,  // Primed to throw (moving while holding)
    Queued = 4          // Queued for throw (Shift+RMB release)
}

/// <summary>
/// Optional: slingshot charge data on the player/god entity.
/// </summary>
public struct ThrowCharge : IComponentData
{
    public float Charge;                    // Accumulated charge time/energy
    public float MaxCharge;                 // Maximum charge value
    public float ChargeRate;                // Charge rate per second
    public bool IsCharging;                 // Whether currently charging
}

/// <summary>
/// Entry in the throw queue.
/// </summary>
public struct ThrowQueueEntry
{
    public Entity Target;                   // Target entity to throw
    public float3 Direction;                // Throw direction
    public float Force;                     // Throw force/speed
}

/// <summary>
/// Buffer element for throw queue.
/// </summary>
public struct ThrowQueue : IBufferElementData
{
    public ThrowQueueEntry Value;           // Queue entry value
}
```

---

## Anchor Components (Conceptual)

**Note:** Anchor components are conceptual - anchors are typically regular entities with `LocalTransform` used as `Parent` targets.

### Hand Anchor

**Concept:** Entity with `LocalTransform` that serves as the attachment point for held items.

**Structure:**
```csharp
// Hand anchor entity (created under player/camera/hand entity)
// Components:
- LocalTransform (position/rotation relative to parent)
- Parent (points to player/camera/hand entity)

// Usage: Held items set Parent.Value = HandAnchorEntity
```

**Creation:**
```csharp
/// <summary>
/// Creates or gets hand anchor entity for holding items.
/// </summary>
public static Entity GetOrCreateHandAnchor(Entity handEntity, ref SystemState state)
{
    // Check if hand anchor already exists (child entity)
    var children = SystemAPI.GetBuffer<Child>(handEntity);
    for (int i = 0; i < children.Length; i++)
    {
        var child = children[i].Value;
        if (SystemAPI.HasComponent<HandAnchorTag>(child))
        {
            return child;
        }
    }
    
    // Create new hand anchor entity
    var anchorEntity = state.EntityManager.CreateEntity();
    state.EntityManager.AddComponent<HandAnchorTag>(anchorEntity);
    state.EntityManager.AddComponent(anchorEntity, new Parent { Value = handEntity });
    state.EntityManager.AddComponent(anchorEntity, new LocalTransform
    {
        Position = float3.zero,  // At hand position
        Rotation = quaternion.identity,
        Scale = 1f
    });
    
    return anchorEntity;
}

/// <summary>
/// Tag marking entity as hand anchor.
/// </summary>
public struct HandAnchorTag : IComponentData { }
```

### Prime Anchor

**Concept:** Entity with `LocalTransform` that serves as the formation center for primed throw items.

**Structure:**
```csharp
// Prime anchor entity (created under player/camera entity)
// Components:
- LocalTransform (position/rotation relative to parent)
- Parent (points to player/camera entity)

// Usage: Primed items set Parent.Value = PrimeAnchorEntity
// Items are placed at LocalTransform offsets based on slot (ring/spiral formation)
```

**Creation:**
```csharp
/// <summary>
/// Creates or gets prime anchor entity for primed throw formations.
/// </summary>
public static Entity GetOrCreatePrimeAnchor(Entity playerEntity, ref SystemState state)
{
    // Check if prime anchor already exists
    var children = SystemAPI.GetBuffer<Child>(playerEntity);
    for (int i = 0; i < children.Length; i++)
    {
        var child = children[i].Value;
        if (SystemAPI.HasComponent<PrimeAnchorTag>(child))
        {
            return child;
        }
    }
    
    // Create new prime anchor entity
    var anchorEntity = state.EntityManager.CreateEntity();
    state.EntityManager.AddComponent<PrimeAnchorTag>(anchorEntity);
    state.EntityManager.AddComponent(anchorEntity, new Parent { Value = playerEntity });
    state.EntityManager.AddComponent(anchorEntity, new LocalTransform
    {
        Position = new float3(0, 2f, -1f),  // In front of player, slightly above
        Rotation = quaternion.identity,
        Scale = 1f
    });
    
    return anchorEntity;
}

/// <summary>
/// Tag marking entity as prime anchor.
/// </summary>
public struct PrimeAnchorTag : IComponentData { }

/// <summary>
/// Calculates formation offset for primed item based on slot.
/// </summary>
public static float3 CalculateFormationOffset(ushort slot)
{
    // Ring formation (circular arrangement)
    const float radius = 1.5f;
    const float heightStep = 0.3f;
    const int itemsPerRing = 8;
    
    var ringIndex = slot / itemsPerRing;
    var slotInRing = slot % itemsPerRing;
    var angle = (slotInRing / (float)itemsPerRing) * 2f * math.PI;
    
    return new float3(
        math.cos(angle) * radius,
        ringIndex * heightStep,
        math.sin(angle) * radius
    );
}
```

---

## Transform Hierarchy Pattern

**Pattern:** Use `Parent` + `LocalTransform` for attachment/formation.

```csharp
// Player/Camera Entity
// └── Hand Anchor Entity (Parent = Player, LocalTransform = offset)
//     └── Held Item Entity (Parent = Hand Anchor, LocalTransform = grip offset)

// Player/Camera Entity
// └── Prime Anchor Entity (Parent = Player, LocalTransform = formation center)
//     ├── Primed Item 0 (Parent = Prime Anchor, LocalTransform = slot 0 offset)
//     ├── Primed Item 1 (Parent = Prime Anchor, LocalTransform = slot 1 offset)
//     └── Primed Item 2 (Parent = Prime Anchor, LocalTransform = slot 2 offset)
```

**Benefits:**
- Entities transform system built around parent/child hierarchies
- Automatic world transform calculation
- Easy to detach (remove Parent component)
- Deterministic positioning

---

## Integration Points

### Pickup System Integration

- `PickableTag` → Marks entity as pickable
- `HeldByPlayer` / `HeldBy` → Marks entity as held
- `Parent` → Attaches item to hand anchor
- `LocalTransform` → Positions item relative to hand anchor
- `PhysicsMassOverride` → Makes item kinematic while held

### Throw System Integration

- `ThrowCharge` → Charge state while button held
- `QueuedThrow` → Queued throw command (exact release tick)
- `BeingThrown` → Marks entity as thrown (in flight)
- `PhysicsVelocity` → Applied impulse on release

### Primed Throw System Integration

- `PrimedThrow` → Marks item as primed
- `PrimedThrowEntry` buffer → Queue on player entity
- `PrimeAnchorTag` → Prime anchor entity
- `Parent` → Attaches item to prime anchor
- `LocalTransform` → Formation position based on slot

---

## Input Priority and Hover/Highlight Pipeline

**Purpose:** Clean input gating to prevent camera and hand from fighting, plus edge-triggered hover/highlight system (no spam).

---

### 1. Input Gating: Priority Table

**Add one derived flag on the player each frame: `InputContextFlags` (or booleans) computed from `InputState`:**

```csharp
/// <summary>
/// Input context flags (computed each frame from InputState).
/// </summary>
public struct InputContextFlags : IComponentData
{
    public byte Flags;
    
    public const byte FlagUICapture = 1 << 0;        // UI capture active (no world interactions)
    public const byte FlagCameraRotate = 1 << 1;     // MMB held (pitch/yaw)
    public const byte FlagCameraPan = 1 << 2;        // LMB drag (pan)
    public const byte FlagHandGrab = 1 << 3;         // RMB held (grab/prime/queue/cancel)
    
    public bool UICapture => (Flags & FlagUICapture) != 0;
    public bool CameraRotate => (Flags & FlagCameraRotate) != 0;
    public bool CameraPan => (Flags & FlagCameraPan) != 0;
    public bool HandGrab => (Flags & FlagHandGrab) != 0;
    public bool InputConsumedByCamera => CameraRotate || CameraPan;
}

/// <summary>
/// Computes input context flags from InputState.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(InputSystemGroup))]
public partial struct InputContextComputationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (inputState, contextFlags) in
            SystemAPI.Query<RefRO<InputState>, RefRW<InputContextFlags>>())
        {
            byte flags = 0;
            
            // Priority (top wins):

            // 1. UI capture → no world interactions (camera + hand ignored)
            if (IsPointerOverUI())
            {
                flags |= InputContextFlags.FlagUICapture;
            }
            
            // 2. MMB held → CameraRotate (pitch/yaw)
            // Hand cannot prime/launch/pick; only update pointer world if you want
            else if (inputState.ValueRO.MiddleHeld)
            {
                flags |= InputContextFlags.FlagCameraRotate;
            }
            
            // 3. LMB drag → CameraPan
            // Hand cannot launch/prime
            else if (IsLmbDrag(inputState.ValueRO))
            {
                flags |= InputContextFlags.FlagCameraPan;
            }
            
            // 4. RMB held → Hand grab/prime/queue/cancel
            else if (inputState.ValueRO.SecondaryHeld)
            {
                flags |= InputContextFlags.FlagHandGrab;
            }
            
            // 5. Otherwise → Hover/highlight only
            // (flags remain 0)

            contextFlags.ValueRW.Flags = flags;
        }
    }
    
    private bool IsLmbDrag(InputState inputState)
    {
        // Treat LMB as Tap vs Drag: "tap" can launch, "drag" pans
        // Use pixel/world delta threshold to decide "dragging"
        const float dragThreshold = 3f;  // pixels

        return inputState.PrimaryHeld &&
               math.length(inputState.PointerDelta) > dragThreshold;
    }
    
    private bool IsPointerOverUI() { /* ... */ }
}
```

**Usage in Hand Systems:**

```csharp
// Early-out check in all hand systems
if (inputContextFlags.ValueRO.InputConsumedByCamera)
{
    return;  // Camera is consuming input, skip hand processing
}
```

**Rule of Thumb:** A launch is only allowed on LMB Tap while NOT in CameraPan/CameraRotate.

---

### 2. Zoom to Cursor: Camera Ray "Pinned Under Mouse"

**For mouse wheel zoom-to-cursor, use a ray from the camera through the cursor and move the camera along that ray:**

```csharp
/// <summary>
/// Zooms camera while keeping point under cursor "pinned".
/// </summary>
private void ZoomToCursor(float zoomDelta, float3 pointerScreenPosition)
{
    // 1. Raycast to get world point under cursor
    var ray = camera.ScreenPointToRay(pointerScreenPosition);
    RaycastHit hit;
    float3 worldHitPoint;
    
    if (Physics.Raycast(ray, out hit, maxDistance, groundMask))
    {
        worldHitPoint = hit.point;
    }
    else
    {
        // Fallback: project onto ground plane
        var plane = new Plane(Vector3.up, 0f);
        float enter;
        if (plane.Raycast(ray, out enter))
        {
            worldHitPoint = ray.GetPoint(enter);
        }
        else
        {
            return;  // Can't zoom
        }
    }
    
    // 2. Update PointerWorld first (raycast)
    var pointerWorld = worldHitPoint;
    
    // 3. Zoom (move camera along ray toward/away from hit point)
    var currentDistance = math.distance(cameraPosition, worldHitPoint);
    var newDistance = math.clamp(currentDistance - zoomDelta, minZoom, maxZoom);
    
    var direction = math.normalize(worldHitPoint - cameraPosition);
    var newCameraPosition = worldHitPoint - direction * newDistance;
    
    // 4. Optionally re-solve pivot so world hit stays stable
    cameraPosition = newCameraPosition;
    pivotPosition = worldHitPoint;  // Pivot follows world hit point
}
```

**In your terms:**

- Update `PointerWorld` first (raycast)
- Then zoom
- Optionally re-solve pivot so the world hit stays stable

**Reference:** [Unity Manual - Camera Zoom to Cursor](https://docs.unity3d.com/Manual/cameras-section.html)

---

### 3. Hover + Highlight: Edge-Triggered (No Spam)

**You already track `HoveredEntity` in `DivineHandState`. Implement a tiny hover diff:**

```csharp
/// <summary>
/// Component marking entity as hovered (for highlight).
/// </summary>
public struct HoveredHighlight : IComponentData { }

/// <summary>
/// Updates hover state with edge-triggered highlights.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(InteractionSystemGroup))]
public partial struct HoverHighlightSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var timeState = SystemAPI.GetSingleton<TimeState>();
        var currentTick = timeState.Tick;
        
        foreach (var (divineHandState, handEntity) in
            SystemAPI.Query<RefRW<DivineHandState>>()
                .WithEntityAccess())
        {
            var prev = divineHandState.ValueRO.HoveredEntity;
            var curr = divineHandState.ValueRO.HoveredEntity;  // Updated by hover detection system
            
            // Edge-triggered: only process on change
            if (curr == prev)
            {
                continue;
            }

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Remove highlight from prev
            if (prev != Entity.Null && SystemAPI.Exists(prev))
            {
                if (SystemAPI.HasComponent<HoveredHighlight>(prev))
                {
                    ecb.RemoveComponent<HoveredHighlight>(prev);
                }
            }

            // Add highlight to curr
            if (curr != Entity.Null && SystemAPI.Exists(curr))
            {
                if (!SystemAPI.HasComponent<HoveredHighlight>(curr))
                {
                    ecb.AddComponent<HoveredHighlight>(curr);
                }
                
                // Play "pickable" sound once (with cooldown)
                var lastHoverSfxTick = divineHandState.ValueRO.LastHoverSfxTick;
                const uint hoverSfxCooldownTicks = 20;  // ~0.33s at 60fps
                
                if (currentTick - lastHoverSfxTick >= hoverSfxCooldownTicks)
                {
                    // Play hover sound (presentation system consumes this event)
                    ecb.AddComponent(handEntity, new HoverSoundEvent
                    {
                        HoveredEntity = curr,
                        Tick = currentTick
                    });
                    
                    divineHandState.ValueRW.LastHoverSfxTick = currentTick;
                }
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
```

**Visual Brightening (Entities Graphics Way):**

```csharp
/// <summary>
/// Drives highlight intensity for hovered entities.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct HoverHighlightVisualSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Use per-entity material overrides (URP BaseColor / EmissionColor)
        // Entities Graphics supports per-entity overrides via IComponentData components
        // and even custom Shader Graph properties via [MaterialProperty]
        
        foreach (var (hoveredHighlight, materialPropertyEmission) in
            SystemAPI.Query<RefRO<HoveredHighlight>, RefRW<URPMaterialPropertyEmissionColor>>())
        {
            // Drive emission intensity while tag is present
            materialPropertyEmission.ValueRW.Value = new float4(1f, 1f, 0.5f, 1f);  // Yellow glow
            materialPropertyEmission.ValueRW.Intensity = 2f;
        }
    }
}
```

**Practical:** Add `HoveredHighlight` tag + one override component (e.g., `URPMaterialPropertyEmissionColor`) and drive intensity in a Burst system while the tag is present.

**Soft Sound:** Don't bind it to "is hovered"; bind it to the transition not-hovered → hovered. Store `LastHoverSfxTick` in the hand state and enforce a minimal gap (e.g., 10–20 ticks).

**Reference:** [Unity Entities Graphics - Material Properties](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/material-properties.html)

---

### 4. Primed Queue UX with Anchors

**Your summary already has the right anchor pattern: a PrimeAnchor child under player/camera, and primed items parented under it in a ring/spiral formation.**

**Tie the controls to context flags:**

```csharp
// Shift + RMB release: move held item into queue (parent to PrimeAnchor, set Queued state)
if (inputState.ThrowModifier && inputState.SecondaryWasReleased && !inputContextFlags.InputConsumedByCamera)
{
    PrimeHeldItem(heldEntity, playerEntity, ref state);
}

// Hotkey "release next": pop FIFO entry, unparent, apply impulse
if (hotkeyReleaseNextPressed && !inputContextFlags.InputConsumedByCamera)
{
    ReleaseNextPrimed(playerEntity, aimDirection, ref state);
}

// Hotkey "release all": iterate queue (optionally release N per tick)
if (hotkeyReleaseAllPressed && !inputContextFlags.InputConsumedByCamera)
{
    ReleaseAllPrimed(playerEntity, aimDirection, ref state);
}

// LMB tap: launch selected/next primed item only if not CameraPan/Rotate
if (inputState.PrimaryClicked && !inputContextFlags.InputConsumedByCamera)
{
    LaunchSelectedPrimed(playerEntity, aimDirection, ref state);
}

// RMB on primed item: cancel (remove from queue, reattach to hand)
if (inputState.SecondaryClicked && hoveredEntity has PrimedThrow && !inputContextFlags.InputConsumedByCamera)
{
    CancelPrimedItem(hoveredEntity, playerEntity, handAnchor, ref state);
}
```

---

### 5. Two Tiny Polish Changes

**Convert "seconds" fields to ticks for determinism:**

```csharp
// ❌ WRONG: Using seconds (drifts under fast-forward/rewind)
public struct PickupState : IComponentData
{
    public float HoldStartTime;  // Seconds
    public float HoldTime;       // Seconds
}

// ✅ CORRECT: Use ticks (deterministic)
public struct PickupState : IComponentData
{
    public uint HoldStartTick;   // Tick when holding started
    public uint HoldTicks;       // Ticks holding (or compute on demand)
}
```

**Add `InputConsumedByCamera` (derived) so every hand system can early-out:**

```csharp
// Early-out check in all hand systems
if (inputContextFlags.ValueRO.InputConsumedByCamera)
{
    return;  // One check, clear intent
}
```

---

### Per-Frame Pipeline Order

**Recommended order:**

1. **Input** → Read raw input from Input System
2. **Input Context Computation** → Compute `InputContextFlags` from `InputState`
3. **Camera** → Process camera movement (pan/orbit/zoom) if flags allow
4. **PointerWorld** → Compute `PointerWorld` from camera raycast
5. **Hover Detection** → Update `HoveredEntity` in `DivineHandState`
6. **Hover Highlight** → Edge-triggered highlight/sound (hover diff)
7. **Hand State Machine** → Process hand interactions (grab/prime/queue) if flags allow
8. **Queue Ops** → Process primed queue (release/cancel) if flags allow

**See:** `Docs/Concepts/Core/Camera_Controller_Summary.md` for camera controller details.

---

## Related Documentation

- **Pickup and Throw System:** `Docs/Concepts/Core/Pickup_And_Throw_System.md` - Full pickup/throw system design
- **Camera Controller:** `Docs/Concepts/Core/Camera_Controller_Summary.md` - Camera controller implementation
- **Unity ECS Transforms:** https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/transforms.html
- **Unity Input System:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html
- **Unity Entities Graphics:** https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/index.html

---

**For Implementers:** Use Parent/LocalTransform for attachment, create anchor entities as children of hand/player  
**For Architects:** Understand transform hierarchy pattern for scalable attachment system  
**For Designers:** Hand anchors for holding, prime anchors for formations

