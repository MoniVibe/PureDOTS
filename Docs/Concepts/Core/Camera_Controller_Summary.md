# Camera Controller Summary

**Status:** Reference Document
**Category:** Core - Camera / Input
**Audience:** Implementers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Purpose:** Summary of the camera controller implementation (BW2StyleCameraController) for B&W2-style camera controls. Documents pivot-based orbit system, input handling, pointer world position computation, and integration with hand/interaction systems.

**Key Features:**
- **LMB + Drag:** Pan across terrain
- **MMB + Drag:** Orbit around pivot
- **Scroll Wheel:** Zoom in/out (zoom-to-cursor)
- **Terrain Clamping:** Prevents going through ground

**Architecture:** Camera controller publishes `CameraRigState` via `CameraRigService` for DOTS systems to read. Input is handled via `HandCameraInputRouter` and `BW2CameraInputBridge`.

---

## File Mapping

### Core Camera Files

- `Packages/com.moni.puredots/Runtime/Camera/BW2StyleCameraController.cs`
  - Main camera controller MonoBehaviour
  - Handles input, pan/orbit/zoom, publishes `CameraRigState`

- `Packages/com.moni.puredots/Runtime/Camera/BW2CameraInputBridge.cs`
  - Input bridge from Input System to camera controller
  - Provides `BW2CameraInputBridge.Snapshot` for input state

- `Packages/com.moni.puredots/Runtime/Camera/CameraRigService.cs`
  - Service for publishing/reading `CameraRigState`
  - Singleton service pattern

- `Packages/com.moni.puredots/Runtime/Camera/CameraRigState.cs`
  - Camera rig state data structure
  - Focus, pitch/yaw/roll, distance, mode, etc.

- `Packages/com.moni.puredots/Runtime/Camera/CameraRigApplier.cs`
  - Applies `CameraRigState` to Unity Camera transform
  - MonoBehaviour bridge to Unity Camera

- `Packages/com.moni.puredots/Runtime/Input/HandCameraInputRouter.cs`
  - Input router for hand/camera input coordination
  - Provides `RmbContext` for raycast/world hit information

---

## Camera Controller Implementation

### BW2StyleCameraController

**Purpose:** Black & White 2 inspired camera controller with pivot-based orbit system.

**Components:**
```csharp
[RequireComponent(typeof(UnityEngineCamera))]
[RequireComponent(typeof(CameraRigApplier))]
[RequireComponent(typeof(BW2CameraInputBridge))]
public sealed class BW2StyleCameraController : MonoBehaviour
{
    [SerializeField] UnityEngine.Camera targetCamera;
    [SerializeField] Transform pivotTransform;
    [SerializeField] CameraRigType rigType = CameraRigType.BW2;
    [SerializeField] HandCameraInputRouter inputRouter;
    
    // Ground/clamping
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundProbeDistance = 600f;
    
    // Pan
    [SerializeField] float panScale = 1f;
    [SerializeField] bool allowPanOverUI;
    
    // Orbit
    [SerializeField] float orbitYawSensitivity = 0.25f;
    [SerializeField] float orbitPitchSensitivity = 0.25f;
    [SerializeField] Vector2 pitchClamp = new(-30f, 85f);
    [SerializeField] bool allowOrbitOverUI = true;
    
    // Zoom
    [SerializeField] float zoomSpeed = 6f;
    [SerializeField] float minDistance = 6f;
    [SerializeField] float maxDistance = 220f;
    [SerializeField] bool invertZoom;
    [SerializeField] bool allowZoomOverUI = true;
    
    // Internal state
    float yaw;
    float pitch;
    float distance;
    Vector3 pivotPosition;
    bool grabbing;  // LMB drag pan state
    Plane panPlane;
    Vector3 panWorldStart;
    Vector3 panPivotStart;
    bool orbitPivotLocked;
    Vector3 lockedPivot;
    float lockedDistance;
}
```

---

## Input Handling

### Input Flow

1. **Input System** → Raw input (mouse, keyboard)
2. **BW2CameraInputBridge** → Reads input, provides `BW2CameraInputBridge.Snapshot`
3. **BW2StyleCameraController** → Consumes snapshot, applies camera movement
4. **CameraRigService** → Publishes `CameraRigState` for DOTS systems

### Input Snapshot

```csharp
public struct Snapshot
{
    public Vector2 PointerPosition;      // Screen position
    public Vector2 PointerDelta;         // Screen delta (pixels)
    public float Scroll;                 // Scroll delta
    public bool LeftPressed;             // LMB pressed this frame
    public bool LeftHeld;                // LMB held
    public bool LeftReleased;            // LMB released this frame
    public bool MiddlePressed;           // MMB pressed this frame
    public bool MiddleHeld;              // MMB held
    public bool RightPressed;            // RMB pressed this frame
    public bool RightHeld;               // RMB held
    public bool EdgeLeft, EdgeRight, EdgeTop, EdgeBottom;  // Edge scroll flags
}
```

---

## Camera Controls

### LMB + Drag: Pan (Grab-Land Pan)

**Mechanism:** Lock a ground plane on press; keep the grabbed point under cursor.

```csharp
// On LMB press: lock ground plane at hit point
if (LeftPressed)
{
    var ray = camera.ScreenPointToRay(PointerPosition);
    if (Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask))
    {
        grabbing = true;
        panWorldStart = hit.point;        // World point under cursor
        panPivotStart = pivotPosition;    // Current pivot position
        panPlane = new Plane(Vector3.up, panWorldStart);  // Ground plane
    }
}

// While LMB held: move pivot to keep grabbed point under cursor
if (grabbing && LeftHeld)
{
    var ray = camera.ScreenPointToRay(PointerPosition);
    if (panPlane.Raycast(ray, out float enter))
    {
        Vector3 worldNow = ray.GetPoint(enter);          // Current world point under cursor
        Vector3 deltaWorld = panWorldStart - worldNow;   // Move pivot by delta
        pivotPosition = panPivotStart + deltaWorld;
    }
}
```

**Key Point:** The world point under cursor stays "pinned" - camera moves to maintain it.

---

### MMB + Drag: Orbit

**Mechanism:** Rotate yaw/pitch around pivot point.

```csharp
// Lock pivot on MMB press (orbit around point under cursor)
if (MiddlePressed)
{
    orbitPivotLocked = true;
    lockedDistance = distance;
    
    var ray = camera.ScreenPointToRay(PointerPosition);
    if (Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask))
    {
        lockedPivot = hit.point;  // Orbit around hit point
    }
    else
    {
        lockedPivot = cameraPosition;
    }
}

// Rotate yaw/pitch while MMB held
if (MiddleHeld)
{
    yaw += PointerDelta.x * orbitYawSensitivity;
    pitch = math.clamp(pitch - PointerDelta.y * orbitPitchSensitivity, pitchClamp.x, pitchClamp.y);
}

// Camera position = pivot - forward * distance
Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
Vector3 camPos = pivotPosition - rot * Vector3.forward * distance;
```

**Key Point:** Pivot stays locked during orbit; camera rotates around it.

---

### Scroll Wheel: Zoom to Cursor

**Mechanism:** Zoom while keeping point under cursor "pinned" (move camera along ray).

```csharp
// Zoom (keep point under cursor pinned)
float scroll = Scroll;
if (math.abs(scroll) > 0.01f)
{
    float zoomDir = invertZoom ? -scroll : scroll;
    float scrollNotches = zoomDir / 120f;
    
    // Update distance (clamped)
    distance = math.clamp(distance - scrollNotches * (zoomSpeed * 2f), minDistance, maxDistance);
    
    // Camera position recomputed from pivot and distance
    Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
    Vector3 camPos = pivotPosition - rot * Vector3.forward * distance;
}
```

**Unity Manual Pattern:** Use a ray from the camera through the cursor and move the camera along that ray.

**For zoom-to-cursor (advanced):**
```csharp
// Get world point under cursor
var ray = camera.ScreenPointToRay(PointerPosition);
if (Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask))
{
    var worldHitPoint = hit.point;
    
    // Move camera toward/away from hit point
    var currentDistance = math.distance(cameraPosition, worldHitPoint);
    var newDistance = math.clamp(currentDistance - zoomDelta, minZoom, maxZoom);
    
    var direction = math.normalize(worldHitPoint - cameraPosition);
    var newCameraPosition = worldHitPoint - direction * newDistance;
    
    // Update pivot so hit point stays stable
    pivotPosition = worldHitPoint;
    cameraPosition = newCameraPosition;
}
```

**Reference:** [Unity Manual - Camera Zoom to Cursor](https://docs.unity3d.com/Manual/cameras-section.html)

---

## Pointer World Position Computation

**Purpose:** Compute world position under cursor for hand interactions, hover detection, etc.

### HandCameraInputRouter

**Provides `RmbContext` with raycast information:**

```csharp
public struct RmbContext
{
    public Vector2 PointerPosition;       // Screen position
    public Ray PointerRay;                // Camera ray
    public bool PointerOverUI;            // UI blocking
    public bool HasWorldHit;              // Raycast hit ground/object
    public RaycastHit WorldHit;           // Hit information
    public Vector3 WorldPoint;            // World position (hit point or projected)
    public int WorldLayer;                // Hit layer
    public float DeltaTime;
    public float UnscaledDeltaTime;
    public bool HandHasCargo;
    public bool HitStorehouse;
    public bool HitPile;
    public bool HitDraggable;
    public bool HitGround;
}

// Build context from camera raycast
void BuildContext()
{
    bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
    
    Ray pointerRay = cam.ScreenPointToRay(pointerPosition);
    
    bool hasWorldHit = Physics.Raycast(pointerRay, out RaycastHit hit, rayDistance, interactionMask);
    Vector3 worldPoint = hasWorldHit ? hit.point : ProjectToGround(pointerRay, rayDistance);
    
    // Layer checks
    bool hitStorehouse = hasWorldHit && (storehouseMask & (1 << hit.collider.gameObject.layer)) != 0;
    bool hitPile = hasWorldHit && (pileMask & (1 << hit.collider.gameObject.layer)) != 0;
    bool hitDraggable = hasWorldHit && (draggableMask & (1 << hit.collider.gameObject.layer)) != 0;
    bool hitGround = hasWorldHit && (groundMask & (1 << hit.collider.gameObject.layer)) != 0;
    
    _currentContext = new RmbContext(/* ... */);
}
```

**Usage:** Hand systems read `RmbContext` from `HandCameraInputRouter.CurrentContext` to get world position and hit information.

---

## Camera State Publishing

### CameraRigState

**Published state structure:**

```csharp
public struct CameraRigState
{
    public float3 Focus;                  // Pivot/focus point
    public float Pitch;                   // Pitch angle (degrees)
    public float Yaw;                     // Yaw angle (degrees)
    public float Roll;                    // Roll angle (degrees, usually 0)
    public float Distance;                // Distance from pivot
    public CameraRigMode Mode;            // Orbit, Free, etc.
    public bool PerspectiveMode;          // Perspective vs orthographic
    public float FieldOfView;             // FOV (perspective)
    public CameraRigType RigType;         // BW2, RTS, etc.
}
```

### CameraRigService

**Singleton service for publishing/reading state:**

```csharp
public static class CameraRigService
{
    // Publish state (called by camera controller)
    public static void Publish(CameraRigState state) { /* ... */ }
    
    // Read state (called by DOTS systems)
    public static bool TryRead(out CameraRigState state) { /* ... */ }
    
    // Get camera position/rotation (computed from state)
    public static bool TryGetCameraTransform(out float3 position, out quaternion rotation) { /* ... */ }
}
```

**Usage:**
- Camera controller publishes state each frame: `CameraRigService.Publish(state)`
- DOTS systems read state: `CameraRigService.TryRead(out var state)`
- Presentation systems apply state to Unity Camera: `CameraRigApplier` component

---

## Integration Points

### Hand System Integration

**Hand systems use camera state and pointer world position:**

1. **Read `RmbContext`** from `HandCameraInputRouter.CurrentContext` for world position
2. **Check input context flags** to avoid conflicts with camera pan/orbit
3. **Use `PointerWorld`** for hover detection, pickup targeting, etc.

### Input Priority

**Camera vs Hand input priority (see Hand/Anchor Components Summary):**

- **UI Capture** → No world interactions (camera + hand ignored)
- **MMB Held** → CameraRotate (hand cannot prime/launch/pick)
- **LMB Drag** → CameraPan (hand cannot launch/prime)
- **RMB Held** → Hand grab/prime/queue/cancel
- **Otherwise** → Hover/highlight only

---

## Performance Considerations

### Raycast Budget

**PointerWorld computation uses raycast - limit frequency/budget:**

- Raycast once per frame (cached in `RmbContext`)
- Use `groundMask` / `interactionMask` to limit raycast cost
- Cache `groundProbeDistance` (max raycast distance)

### State Publishing

**Camera state published once per frame:**

- `CameraRigService` uses simple singleton pattern (no allocations)
- State is value type (blittable, Burst-friendly)
- Systems can read state without locks (read-only access)

---

## Tightening Recommendations

**Purpose:** Improvements to make camera controller seamless with primed-throw + pick/highlight systems.

---

### 1. Fix LMB "Tap vs Drag" (Prevent Panning from Stealing Taps)

**Current Issue:** Pan code sets `grabbing = true` immediately on `LeftPressed` when ray hits ground. This steals LMB taps (needed for launching primed items).

**Current Code:**
```csharp
// ❌ PROBLEM: Immediately enters grab mode on press
if (LeftPressed)
{
    var ray = camera.ScreenPointToRay(PointerPosition);
    if (Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask))
    {
        grabbing = true;  // Immediately steals tap
        panWorldStart = hit.point;
        panPivotStart = pivotPosition;
        panPlane = new Plane(Vector3.up, panWorldStart);
    }
}
```

**Solution:**
```csharp
// ✅ CORRECT: Record candidate, only enter grab after threshold
if (LeftPressed)
{
    var ray = camera.ScreenPointToRay(PointerPosition);
    if (Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask))
    {
        // Record candidate pan (don't enter grab yet)
        candidatePanWorldStart = hit.point;
        candidatePanPivotStart = pivotPosition;
        candidatePanPlane = new Plane(Vector3.up, candidatePanWorldStart);
        candidatePanScreenStart = PointerPosition;
        candidatePanTime = Time.time;
    }
}

// Enter grab only after threshold
if (candidatePanWorldStart != Vector3.zero)
{
    float pixelDelta = Vector2.Distance(PointerPosition, candidatePanScreenStart);
    float timeDelta = Time.time - candidatePanTime;
    
    const float pixelThreshold = 5f;  // pixels
    const float timeThreshold = 0.15f;  // seconds
    
    if (pixelDelta > pixelThreshold || timeDelta > timeThreshold)
    {
        // Now enter grab mode
        grabbing = true;
        panWorldStart = candidatePanWorldStart;
        panPivotStart = candidatePanPivotStart;
        panPlane = candidatePanPlane;
        candidatePanWorldStart = Vector3.zero;  // Clear candidate
    }
}
```

**Alternative: Use Input System Tap vs Hold Interactions**

```csharp
// Use Tap vs Hold/SlowTap interactions in Input System
// Tap = press+release within duration (can launch primed items)
// Hold = press held for duration (enters pan mode)
// Multiple interactions on one binding are evaluated in order
```

**Reference:** [Unity Input System - Tap/Hold Interactions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Interactions.html#tap)

---

### 2. Make "Zoom-to-Cursor" Real (Currently Zoom-to-Pivot)

**Current Issue:** Primary zoom block only changes `distance` and recomputes `camPos` from pivot. That's not cursor-pinned zoom unless pivot happens to be under the cursor.

**Current Code:**
```csharp
// ❌ PROBLEM: Zooms to pivot, not cursor
float scroll = Scroll;
if (math.abs(scroll) > 0.01f)
{
    float zoomDir = invertZoom ? -scroll : scroll;
    float scrollNotches = zoomDir / 120f;
    distance = math.clamp(distance - scrollNotches * (zoomSpeed * 2f), minDistance, maxDistance);
    
    // Camera position recomputed from pivot (not cursor-pinned)
    Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
    Vector3 camPos = pivotPosition - rot * Vector3.forward * distance;
}
```

**Solution: Pinned-Ray Method**

```csharp
// ✅ CORRECT: Ray through cursor, move camera along that ray
float scroll = Scroll;
if (math.abs(scroll) > 0.01f)
{
    float zoomDir = invertZoom ? -scroll : scroll;
    float scrollNotches = zoomDir / 120f;
    float zoomDelta = scrollNotches * (zoomSpeed * 2f);
    
    // Get world point under cursor (from HandCameraInputRouter context)
    var context = inputRouter.CurrentContext;
    if (context.HasWorldHit)
    {
        var worldHitPoint = context.WorldPoint;
        
        // Current camera position
        var currentCamPos = _currentCameraPosition;
        var currentDistance = math.distance(currentCamPos, worldHitPoint);
        
        // New distance (clamped)
        var newDistance = math.clamp(currentDistance - zoomDelta, minDistance, maxDistance);
        
        // Direction from camera to hit point
        var direction = math.normalize(worldHitPoint - currentCamPos);
        
        // Move camera along ray direction so hit point stays fixed
        var newCameraPosition = worldHitPoint - direction * newDistance;
        
        // Update camera position and pivot (pivot follows hit point)
        _currentCameraPosition = newCameraPosition;
        pivotPosition = worldHitPoint;
        
        // Recompute distance from new camera position
        distance = newDistance;
    }
    else
    {
        // Fallback: zoom to pivot (if no hit)
        distance = math.clamp(distance - zoomDelta, minDistance, maxDistance);
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        _currentCameraPosition = pivotPosition - rot * Vector3.forward * distance;
    }
}
```

**Reference:** [Unity Manual - Camera Zoom to Cursor](https://docs.unity3d.com/Manual/cameras-section.html)

---

### 3. Don't Raycast Twice Per Frame (Camera + Router)

**Current Issue:** Raycast in camera pan/orbit lock code AND in `HandCameraInputRouter.BuildContext()` every frame. Duplicate work, potential inconsistencies.

**Current Code:**
```csharp
// ❌ PROBLEM: Camera controller raycasts
if (orbitPressed && orbitAllowed)
{
    var ray = camera.ScreenPointToRay(PointerPosition);
    if (Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask))
    {
        lockedPivot = hit.point;
    }
}

// ❌ PROBLEM: Router also raycasts
void BuildContext()
{
    Ray pointerRay = cam.ScreenPointToRay(pointerPosition);
    bool hasWorldHit = Physics.Raycast(pointerRay, out RaycastHit hit, rayDistance, interactionMask);
    // ...
}
```

**Solution: Single Source of Truth**

```csharp
// ✅ CORRECT: HandCameraInputRouter is single source, camera consumes result
void ApplyInput()
{
    // Get context from router (already raycasted)
    var context = inputRouter.CurrentContext;
    
    // Use context for pan start hit
    if (LeftPressed && context.HasWorldHit)
    {
        candidatePanWorldStart = context.WorldPoint;
        // ...
    }
    
    // Use context for orbit lock pivot
    if (orbitPressed && orbitAllowed && context.HasWorldHit)
    {
        lockedPivot = context.WorldPoint;
    }
    
    // Use context for zoom-to-cursor target
    if (scroll != 0f && context.HasWorldHit)
    {
        var worldHitPoint = context.WorldPoint;
        // ... zoom-to-cursor code
    }
}
```

**Benefits:**
- Guarantees camera+hand agree on the same hit
- Saves work (one raycast instead of two)
- Single source of truth

---

### 4. UI Blocking: Keep It Consistent and Pointer-Safe

**Current Issue:** Using `EventSystem.current.IsPointerOverGameObject()` to block interactions. For mouse this works, but Unity notes that without a `pointerId` it targets the default mouse pointer (-1), and for touch you should pass the `fingerId`.

**Current Code:**
```csharp
// ❌ PROBLEM: May not work correctly for touch
bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
```

**Solution: Compute Once in Router**

```csharp
// ✅ CORRECT: Compute once in router, reuse for both camera and hand
void BuildContext()
{
    // Compute pointer over UI (with proper pointer ID)
    bool pointerOverUI = false;
    if (EventSystem.current != null && EventSystem.current.enabled)
    {
        // For mouse: use default (-1) or explicit pointer ID
        pointerOverUI = EventSystem.current.IsPointerOverGameObject();  // Default mouse
        
        // For touch: pass finger ID (if using touch)
        // pointerOverUI = EventSystem.current.IsPointerOverGameObject(fingerId);
    }
    
    _currentContext = new RmbContext(
        pointerPosition,
        pointerRay,
        pointerOverUI,  // Single source of truth
        // ... other fields
    );
}
```

**Usage:**
```csharp
// Camera and hand systems both use context.PointerOverUI
if (context.PointerOverUI)
{
    return;  // Skip interaction
}
```

**Reference:** [Unity EventSystem - IsPointerOverGameObject](https://docs.unity3d.com/ScriptReference/EventSystems.EventSystem.IsPointerOverGameObject.html)

---

### 5. ECS-Only Pickables: Switch Raycasts to Unity Physics

**Current Issue:** Pointer hits use `Physics.Raycast` (GameObject physics). If pickables are ECS colliders, need `PhysicsWorldSingleton.CastRay` instead (Burst/job-friendly).

**Current Code:**
```csharp
// ❌ PROBLEM: Uses GameObject physics
bool hasWorldHit = Physics.Raycast(pointerRay, out RaycastHit hit, rayDistance, interactionMask);
```

**Solution: Unity Physics for ECS Colliders**

```csharp
// ✅ CORRECT: Use Unity Physics for ECS colliders
void BuildContextWithUnityPhysics()
{
    // Get Unity Physics world
    var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
    
    // Convert camera ray to Unity Physics format
    var raycastInput = new RaycastInput
    {
        Start = pointerRay.origin,
        End = pointerRay.origin + pointerRay.direction * rayDistance,
        Filter = new CollisionFilter
        {
            BelongsTo = pickableLayer,
            CollidesWith = interactionLayer,
            GroupIndex = 0
        }
    };
    
    // Cast ray (Burst-friendly)
    bool hasWorldHit = physicsWorld.CastRay(raycastInput, out var hit);
    
    if (hasWorldHit)
    {
        worldPoint = hit.Position;
        hitEntity = hit.Entity;  // ECS entity hit
        // ...
    }
}
```

**Hybrid Approach:**
```csharp
// Use GameObject raycasts for terrain (if terrain is GO)
// Use Unity Physics for ECS pickables (behind one "raycast provider" interface)

interface IRaycastProvider
{
    bool CastRay(Ray ray, float maxDistance, LayerMask mask, out RaycastHit hit, out Entity hitEntity);
}

class HybridRaycastProvider : IRaycastProvider
{
    public bool CastRay(Ray ray, float maxDistance, LayerMask mask, out RaycastHit hit, out Entity hitEntity)
    {
        // Try GameObject physics first (terrain)
        if (Physics.Raycast(ray, out hit, maxDistance, terrainMask))
        {
            hitEntity = Entity.Null;  // GameObject, not ECS
            return true;
        }
        
        // Fall back to Unity Physics (ECS pickables)
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var raycastInput = new RaycastInput { /* ... */ };
        if (physicsWorld.CastRay(raycastInput, out var physicsHit))
        {
            hit.point = physicsHit.Position;
            hitEntity = physicsHit.Entity;
            return true;
        }
        
        hitEntity = Entity.Null;
        return false;
    }
}
```

**Reference:** [Unity Physics - Raycast](https://docs.unity.cn/Packages/com.unity.physics@1.0/manual/raycast.html)

---

### 6. Hook Hover Highlight + Sound to Hover Transitions Only

**Current Issue:** Highlighting + "soft pickable sound" should fire only when `HoveredEntity` changes (edge-trigger), with a short tick cooldown, so panning/zooming doesn't spam.

**You already publish rich hit context** (`HasWorldHit`, `WorldHit`, flags like `HitPile`/`HitDraggable`). Use it for edge-triggered hover.

**Solution: Edge-Triggered Hover System**

```csharp
/// <summary>
/// Updates hover state with edge-triggered highlights and sounds.
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
            // Get context from router (contains hit entity)
            var context = inputRouter.CurrentContext;
            var curr = context.HasWorldHit ? GetEntityFromHit(context.WorldHit) : Entity.Null;
            var prev = divineHandState.ValueRO.HoveredEntity;
            
            // Edge-triggered: only process on change
            if (curr == prev)
            {
                continue;  // No change, skip
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
            
            // Add highlight to curr (if pickable)
            if (curr != Entity.Null && SystemAPI.Exists(curr))
            {
                if (SystemAPI.HasComponent<PickableTag>(curr))
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
            }
            
            // Update hovered entity
            divineHandState.ValueRW.HoveredEntity = curr;
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
```

**Key Points:**
- Only fire on transition (curr != prev)
- Cooldown prevents spam (panning/zooming won't trigger sound repeatedly)
- Uses rich hit context from router (single source of truth)

---

## Related Documentation

- **Hand/Anchor Components:** `Docs/Concepts/Core/Hand_Anchor_Components_Summary.md` - Input priority and hover pipeline
- **Pickup and Throw System:** `Docs/Concepts/Core/Pickup_And_Throw_System.md` - Hand interaction system
- **Unity Manual - Camera:** https://docs.unity3d.com/Manual/cameras-section.html
- **Unity Input System - Interactions:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Interactions.html
- **Unity Physics - Raycast:** https://docs.unity.cn/Packages/com.unity.physics@1.0/manual/raycast.html

---

**For Implementers:** Use `HandCameraInputRouter` for pointer world position, check input context flags to avoid conflicts  
**For Architects:** Camera controller publishes state, systems read state (no direct coupling)  
**For Designers:** LMB pan, MMB orbit, scroll zoom provides intuitive B&W2-style camera

