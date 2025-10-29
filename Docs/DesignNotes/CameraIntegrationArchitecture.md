# Camera Integration Architecture for PureDOTS

**Status**: Architectural planning document for Space4X and Godgame camera implementations.

This document provides architectural guidance to ensure both camera implementations integrate seamlessly with PureDOTS foundation while remaining game-specific and avoiding conflicts.

## Core Principles

1. **DOTS-First Logic**: All camera control logic lives in DOTS systems; MonoBehaviour bridges only handle input/output
2. **Configurable Profiles**: All camera parameters come from ScriptableObject profiles (data-driven)
3. **Input System Only**: Zero legacy Input Manager usage
4. **Rewind-Safe**: All camera state respects `RewindState` and can be snapshotted
5. **Theme-Agnostic Core**: PureDOTS provides base patterns; games extend with mode-specific logic

## Shared PureDOTS Foundation (Recommended)

### 1. Base Camera Components (`PureDOTS.Runtime.Camera`)

Create reusable base components that both games can extend:

```csharp
// Base camera control input (game-agnostic)
public struct CameraControlInput : IComponentData
{
    public float2 PanInput;      // WASD/arrows/gamepad stick
    public float ZoomInput;      // Scroll wheel/gamepad triggers
    public float2 RotateInput;   // Mouse delta/gamepad right stick
    public bool ResetRequested;  // Reset button
    public bool ModeToggleRequested; // Mode switch button
}

// Base camera state (game-agnostic)
public struct CameraState : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public float ZoomDistance;   // Distance from focus point (for orbital)
    public float3 FocusPoint;     // Pivot/orbit center
    public float PitchAngle;      // For pitch limits
    public float YawAngle;
}

// Base camera config (extensible)
public struct CameraConfig : IComponentData
{
    public float PanSpeed;
    public float ZoomSpeed;
    public float MinZoomDistance;
    public float MaxZoomDistance;
    public float RotationSensitivity;
    public float MinPitchAngle;
    public float MaxPitchAngle;
    public float3 PanBoundsMin;
    public float3 PanBoundsMax;
    public bool EnablePan;
    public bool EnableZoom;
    public bool EnableRotation;
}
```

### 2. Base Input Bridge Pattern (`PureDOTS.Runtime.Input`)

Create a base `CameraInputBridge` MonoBehaviour following `DivineHandInputBridge` pattern:

```csharp
public abstract class CameraInputBridge : MonoBehaviour
{
    protected abstract void ReadInput(ref CameraControlInput input);
    
    void Update()
    {
        if (!HasWorld()) return;
        
        var input = new CameraControlInput();
        ReadInput(ref input);
        
        // Write to DOTS singleton
        var entityManager = World.EntityManager;
        if (SystemAPI.TryGetSingletonEntity<CameraControlInput>(out var entity))
        {
            entityManager.SetComponentData(entity, input);
        }
    }
}
```

### 3. Camera System Group

Add to `SystemGroups.cs`:

```csharp
/// <summary>
/// Camera control systems run in presentation group after simulation.
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CameraSystemGroup : ComponentSystemGroup { }
```

## Game-Specific Extensions

### Space4X: RTS-Style Camera

**Namespace**: `Space4X.Camera`

**Components**:
- Extend `CameraControlInput` with RTS-specific fields (optional)
- Extend `CameraState` with RTS mode flag

**Profile**: `Space4XCameraProfile` ScriptableObject
- RTS-specific: sector bounds, vertical movement speed (Q/E)
- Inherits base config from PureDOTS `CameraConfig`

**System**: `Space4XCameraControlSystem`
- Implements RTS pan (WASD + Q/E)
- Implements free-fly rotation (mouse look)
- Implements zoom (scroll wheel)

**Integration Points**:
- Uses `CameraSystemGroup` for ordering
- Uses `CameraControlInput` singleton from bridge
- Extends `CameraConfig` with Space4X-specific bounds

### Godgame: BW2 Orbital + RTS Toggle

**Namespace**: `Godgame.Camera`

**Components**:
- `CameraMode` enum component (RTSFreeFly = 0, Orbital = 1)
- Extend `CameraControlInput` with mode-specific inputs
- Extend `CameraState` with orbital-specific state (grab plane, pivot lock)

**Profile**: `GodgameCameraProfile` ScriptableObject
- BW2 reference values (sensitivity curves, zoom ranges, pitch limits)
- Mode-specific parameters
- Terrain collision settings

**Systems**:
- `GodgameCameraControlSystem`: Main control logic with mode switching
- `GodgameCameraTerrainRaycastSystem`: Terrain queries for orbital mode
- Uses `CameraSystemGroup` for ordering

**Integration Points**:
- Hand cursor integration: camera zoom pivots to hand cursor position (from `DivineHandState`)
- Uses shared `CameraControlInput` singleton
- Respects `RewindState` for rewind safety

## Integration Pattern

### Input Flow

```
Unity Input System
    ↓
Game-Specific Input Bridge (MonoBehaviour)
    ↓
CameraControlInput (DOTS Singleton)
    ↓
Game-Specific Camera System (ISystem)
    ↓
CameraState (DOTS Singleton)
    ↓
Camera Render Bridge (MonoBehaviour)
    ↓
Unity Camera Transform
```

### Configuration Flow

```
ScriptableObject Profile (Game-Specific)
    ↓
Baker/Authoring Component
    ↓
CameraConfig (DOTS Singleton)
    ↓
Camera System reads config each frame
```

## Avoiding Conflicts

### 1. Action Map Separation

- **Space4X**: Uses "Camera" action map
- **Godgame**: Uses "Look", "Move", "Camera" action maps
- **PureDOTS**: Hand uses "Hand" action map

**Recommendation**: Each game should use distinct action maps or namespaced actions to avoid binding conflicts.

### 2. Singleton Coordination

Both games use `CameraControlInput` singleton, but:
- Games can extend with their own singletons (e.g., `Space4XCameraMode`, `GodgameCameraTerrainState`)
- Systems check for game-specific components before operating
- PureDOTS base systems remain game-agnostic

**Pattern**:
```csharp
// Game system checks for its own mode component
if (!SystemAPI.HasComponent<Space4XCameraMode>(cameraEntity))
{
    return; // Not a Space4X camera
}
```

### 3. System Group Ordering

Both games use `CameraSystemGroup`, but:
- Order systems within group using `[UpdateBefore]` / `[UpdateAfter]`
- Terrain raycast systems run before control systems
- Render bridges run last (in `PresentationSystemGroup` but after camera systems)

### 4. Rewind Safety

Both games must:
- Check `RewindState.Mode` before updating camera state
- Early-out during `RewindMode.Playback` and `RewindMode.CatchUp`
- Store camera state snapshots if rewind is required

**Pattern**:
```csharp
var rewindState = SystemAPI.GetSingleton<RewindState>();
if (rewindState.Mode != RewindMode.Record)
{
    return; // Skip updates during rewind
}
```

## PureDOTS Enhancements Needed

### 1. Base Camera Components

Add to `PureDOTS.Runtime.Camera`:
- `CameraControlInput` singleton component
- `CameraState` singleton component  
- `CameraConfig` singleton component (extensible)

### 2. Base Input Bridge

Add to `PureDOTS.Runtime.Input`:
- Abstract `CameraInputBridge` MonoBehaviour base class
- Helper methods for Input System reading
- Singleton access helpers

### 3. Camera System Group

Add to `PureDOTS.Systems.SystemGroups`:
- `CameraSystemGroup` in `PresentationSystemGroup`

### 4. Documentation

Update `DivineHandCamera_TODO.md` to reference this architecture and explain how games extend the base.

## Implementation Recommendations

### Phase 1: PureDOTS Base (Foundation Team)

1. Create base camera components (`CameraControlInput`, `CameraState`, `CameraConfig`)
2. Create abstract `CameraInputBridge` base class
3. Add `CameraSystemGroup` to system groups
4. Document base patterns in `Docs/DesignNotes/CameraIntegrationArchitecture.md`

### Phase 2: Space4X Implementation (Space4X Team)

1. Extend base components with RTS-specific fields (if needed)
2. Create `Space4XCameraProfile` ScriptableObject
3. Create `Space4XCameraInputBridge` inheriting from `CameraInputBridge`
4. Create `Space4XCameraControlSystem` implementing RTS controls
5. Create `Space4XCameraRenderBridge` MonoBehaviour

### Phase 3: Godgame Implementation (Godgame Team)

1. Create `CameraMode` component and mode switching logic
2. Create `GodgameCameraProfile` ScriptableObject with BW2 values
3. Create `GodgameCameraInputBridge` inheriting from `CameraInputBridge`
4. Create `GodgameCameraControlSystem` with mode switching
5. Create `GodgameCameraTerrainRaycastSystem` for orbital queries
6. Create `GodgameCameraRenderBridge` MonoBehaviour
7. Integrate with Divine Hand cursor position for zoom pivot

### Phase 4: Integration Testing

1. Verify both cameras work in separate scenes
2. Verify both cameras work in same scene (different GameObjects)
3. Test rewind compatibility
4. Test input action map isolation

## Critical Considerations

### Determinism

- All camera calculations use `TimeState.FixedDeltaTime`
- Input reading must be frame-consistent (no async input polling)
- Terrain raycasts use deterministic physics queries (fixed inputs)

### Performance

- Camera systems run in `PresentationSystemGroup` (single-threaded, not Burst)
- Input bridges are MonoBehaviour (no performance concerns for single camera)
- Terrain raycasts use managed Unity Physics (acceptable for single camera)

### Extensibility

- Base components are extensible (games can add fields via composition)
- Profile ScriptableObjects can reference each other (profile inheritance)
- Systems check for game-specific components before operating

## Questions for PureDOTS Team

1. **Raycast Utilities**: Should PureDOTS provide deterministic raycast utilities, or use Unity Physics directly?
   - **Recommendation**: Use Unity Physics directly but document deterministic usage patterns

2. **Camera Rewind**: Should camera state be included in rewind snapshots?
   - **Recommendation**: Yes, via optional `CameraHistory` component that games can opt into

3. **Spatial Integration**: Should camera position be indexed in spatial grid?
   - **Recommendation**: Optional `SpatialIndexedTag` on camera state entity for future registry queries

4. **Hand Integration**: How should camera zoom pivot to hand cursor?
   - **Recommendation**: Camera system queries `DivineHandState.CursorPosition` when hand is visible

## Next Steps

1. **Foundation**: Implement PureDOTS base camera components and input bridge
2. **Space4X**: Implement RTS camera using base components
3. **Godgame**: Implement BW2 orbital camera using base components
4. **Integration**: Test both cameras together, verify no conflicts
5. **Documentation**: Update game-specific TODOs with final implementation details

