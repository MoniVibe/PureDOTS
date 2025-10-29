# Camera Integration Suggestions for Space4X and Godgame

**Status**: Recommendations to ensure seamless integration with PureDOTS architecture.

Based on analysis of both camera plans and PureDOTS `DivineHandInputBridge` / `HandCameraInputRouter` patterns, here are critical suggestions:

## Critical Architecture Decisions

### 1. Shared Base Components (REQUIRED)

**Recommendation**: PureDOTS should provide base camera components that both games extend.

**Rationale**:
- Prevents duplicate singleton components
- Ensures consistent DOTS patterns
- Enables shared rewind/state management
- Reduces integration conflicts

**Implementation**: See `Docs/DesignNotes/CameraIntegrationArchitecture.md` for full component definitions.

### 2. Input System Integration Pattern

**Current Pattern** (from `HandCameraInputRouter`):
- MonoBehaviour router reads Input System actions
- Writes aggregated state to DOTS singletons
- Handles priority/routing logic

**Recommendation for Cameras**:
- Use same pattern: MonoBehaviour bridge → DOTS singleton → DOTS system
- **Space4X**: Create `Space4XCameraInputBridge` that reads "Camera" action map
- **Godgame**: Create `GodgameCameraInputBridge` that reads "Look", "Move", "Camera" action maps
- Both write to shared `CameraControlInput` singleton (PureDOTS base component)

**Why This Works**:
- Avoids action map conflicts (each game uses distinct maps)
- Shared singleton doesn't conflict (systems check for game-specific components)
- Follows established PureDOTS patterns

### 3. System Group Placement

**Recommendation**: Add `CameraSystemGroup` to PureDOTS `SystemGroups.cs`:

```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CameraSystemGroup : ComponentSystemGroup { }
```

**Ordering**:
1. Terrain raycast systems run first (if needed)
2. Camera control systems run next
3. Camera render bridges run last (MonoBehaviour Update)

**Rationale**:
- Matches PureDOTS pattern (hand systems run in `HandSystemGroup`)
- Ensures cameras update after simulation, before rendering
- Keeps deterministic logic in DOTS, presentation in MonoBehaviour

### 4. Rewind Safety Pattern

**Critical Requirement**: Both camera systems must respect `RewindState`.

**Pattern** (from PureDOTS systems):
```csharp
var rewindState = SystemAPI.GetSingleton<RewindState>();
if (rewindState.Mode != RewindMode.Record)
{
    return; // Skip updates during playback/catch-up
}
```

**Additional Consideration**: 
- Camera state should be snapshottable if rewind is needed
- Consider optional `CameraHistory` component for games that need camera rewind
- PureDOTS base can provide snapshot helpers

### 5. Profile Pattern Consistency

**Recommendation**: Both games should use ScriptableObject profiles with Baker pattern.

**Pattern** (from `HandCameraInputProfile`):
1. Create ScriptableObject profile asset
2. Create Baker that reads profile → writes to DOTS config component
3. System reads config singleton each frame

**Benefits**:
- Data-driven configuration
- Designer-friendly inspector
- Runtime tuning capability (if profile references updated)

### 6. Hand Integration (Godgame Only)

**Requirement**: Camera zoom pivots to hand cursor position when hand is visible.

**Implementation Pattern**:
```csharp
// In GodgameCameraControlSystem
if (SystemAPI.TryGetSingleton<DivineHandState>(out var handState))
{
    if (handState.CurrentState != HandState.Empty)
    {
        // Use hand cursor position as zoom pivot
        focusPoint = handState.CursorPosition;
    }
}
```

**Recommendation**: Query hand state only when needed (e.g., during zoom), cache result if used multiple times in same frame.

## Conflict Prevention Strategies

### 1. Action Map Namespacing

**Space4X Actions**:
- Action Map: "Camera"
- Actions: Pan, Zoom, Rotate, Reset

**Godgame Actions**:
- Action Map: "HandCamera" (already exists per `HandCameraInputRouter`)
- Actions: Look, Move, CameraToggleMode, CameraVertical

**PureDOTS Hand Actions**:
- Action Map: "HandCamera" (shared with Godgame camera)
- Actions: Hand-specific actions

**Recommendation**: 
- Godgame should use separate action map name (e.g., "GodgameCamera") OR
- Use action naming convention to avoid conflicts (e.g., "CameraPan" vs "HandPan")

### 2. Singleton Component Coordination

**Shared Base Singletons** (PureDOTS):
- `CameraControlInput` - both games write to this
- `CameraState` - both games update this
- `CameraConfig` - both games configure this

**Game-Specific Singletons**:
- `Space4XCameraMode` - Space4X-only
- `GodgameCameraMode` - Godgame-only
- `GodgameCameraTerrainState` - Godgame-only

**Pattern**: Systems check for game-specific components before operating:
```csharp
// Space4X system
if (!SystemAPI.HasComponent<Space4XCameraMode>(cameraEntity))
{
    return; // Not Space4X camera, skip
}
```

### 3. Scene Setup Strategy

**Recommendation**: Each game uses separate camera GameObjects:
- Space4X scenes: "Space4X Camera" GameObject with `Space4XCameraController`
- Godgame scenes: "Godgame Camera" GameObject with `GodgameCameraController`
- Hand scenes: "Main Camera" with hand camera systems

**Rationale**: Avoids MonoBehaviour conflicts, allows both cameras in same scene if needed (different GameObjects).

## Implementation Sequence

### Phase 1: PureDOTS Foundation (DO FIRST)

1. **Create Base Components** (`PureDOTS.Runtime.Camera`):
   - `CameraControlInput`
   - `CameraState`
   - `CameraConfig`

2. **Create Base Input Bridge** (`PureDOTS.Runtime.Input`):
   - Abstract `CameraInputBridge` MonoBehaviour
   - Follows `DivineHandInputBridge` pattern

3. **Add System Group** (`PureDOTS.Systems.SystemGroups`):
   - `CameraSystemGroup` in `PresentationSystemGroup`

4. **Document Patterns**:
   - Update `DivineHandCamera_TODO.md` with base architecture
   - Create `CameraIntegrationArchitecture.md` (done)

### Phase 2: Space4X Implementation

1. Create `Space4XCameraProfile` ScriptableObject
2. Create `Space4XCameraInputBridge` inheriting from `CameraInputBridge`
3. Create `Space4XCameraControlSystem` in `CameraSystemGroup`
4. Create `Space4XCameraRenderBridge` MonoBehaviour
5. Test in Space4X demo scenes

### Phase 3: Godgame Implementation

1. Create `GodgameCameraProfile` ScriptableObject with BW2 values
2. Create `GodgameCameraInputBridge` inheriting from `CameraInputBridge`
3. Create `GodgameCameraControlSystem` with mode switching
4. Create `GodgameCameraTerrainRaycastSystem` for orbital queries
5. Integrate with Divine Hand cursor (zoom pivot)
6. Create `GodgameCameraRenderBridge` MonoBehaviour
7. Test in Godgame scenes

### Phase 4: Integration Testing

1. Verify both cameras work independently
2. Verify both cameras work in same scene (different GameObjects)
3. Test rewind compatibility
4. Test input action isolation
5. Performance profiling

## Critical Code Patterns

### Input Bridge Pattern (Both Games)

```csharp
public class Space4XCameraInputBridge : CameraInputBridge
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] string actionMapName = "Camera";
    
    InputActionMap _map;
    InputAction _panAction;
    InputAction _zoomAction;
    InputAction _rotateAction;
    
    protected override void ReadInput(ref CameraControlInput input)
    {
        // Read from Input System
        input.PanInput = _panAction.ReadValue<Vector2>();
        input.ZoomInput = _zoomAction.ReadValue<Vector2>();
        input.RotateInput = _rotateAction.ReadValue<Vector2>();
    }
}
```

### Camera System Pattern (Both Games)

```csharp
[UpdateInGroup(typeof(CameraSystemGroup))]
public partial struct Space4XCameraControlSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Rewind guard
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record) return;
        
        // Read input
        var input = SystemAPI.GetSingleton<CameraControlInput>();
        var config = SystemAPI.GetSingleton<CameraConfig>();
        var cameraState = SystemAPI.GetSingletonRW<CameraState>();
        
        // Apply camera logic
        // ... RTS-specific logic ...
        
        // Update state
        cameraState.ValueRW = newState;
    }
}
```

### Render Bridge Pattern (Both Games)

```csharp
public class Space4XCameraRenderBridge : MonoBehaviour
{
    void Update()
    {
        if (!HasWorld()) return;
        
        var entityManager = World.EntityManager;
        if (SystemAPI.TryGetSingleton<CameraState>(out var state))
        {
            transform.position = state.Position;
            transform.rotation = state.Rotation;
        }
    }
}
```

## Questions Answered

### Q: Are there PureDOTS raycast utilities?
**A**: No, use Unity Physics directly. Ensure inputs are deterministic (fixed raycast origin/direction from DOTS state).

### Q: Should camera be in rewind snapshots?
**A**: Optional. PureDOTS base can provide `CameraHistory` component that games opt into. Hand systems show this pattern.

### Q: Should camera be spatially indexed?
**A**: Optional. Add `SpatialIndexedTag` to camera state entity if registry queries are needed (future enhancement).

### Q: How to handle hand cursor integration?
**A**: Godgame camera system queries `DivineHandState.CursorPosition` when hand is visible. See pattern above.

## Next Steps for PureDOTS Team

1. **Review**: Validate base component design with game teams
2. **Implement**: Create base components and input bridge
3. **Document**: Update TODO files with camera integration steps
4. **Coordinate**: Ensure Space4X and Godgame teams follow patterns

## Next Steps for Game Teams

1. **Space4X**: Wait for PureDOTS base, then implement RTS camera
2. **Godgame**: Wait for PureDOTS base, then implement BW2 orbital camera
3. **Both**: Test independently, then test together in shared scene

