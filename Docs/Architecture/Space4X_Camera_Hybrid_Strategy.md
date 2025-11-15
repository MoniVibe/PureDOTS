# Space4X Camera Hybrid Strategy

This strategy describes how we will evolve Space4X camera control from the current MonoBehaviour-heavy bridge into an ECS-managed pipeline similar to the DOTS Sample (`PlayerCameraControl.cs` + `GameApp.CameraStack`). The focus is to retain immediate responsiveness while gaining deterministic scheduling, shared camera orchestration, and support for multi-camera scenarios.

## Current State Recap

- `Space4XCameraMouseController` (MonoBehaviour) reads bridged input, mutates the ECS `Space4XCameraState` singleton, and directly manipulates `Camera.main`. It also mirrors state for diagnostics and consumes bridge flags twice per frame.
- DOTS systems (`Space4XCameraInputSystem`, `Space4XCameraSystem`, `Space4XCameraRenderSyncSystem`) coexist but do not own camera spawning or activation. They rely on the MonoBehaviour to have already created/configured singletons.
- There is no shared camera stack, so additional cameras (cinematics, debug) would compete for `Camera.main` control.

## Target Architecture

### 1. ECS-Owned Camera Entities

- **Camera prefab**: Convert a new `Space4XCameraRig` prefab (camera + audio listener + optional Cinemachine brain) into an entity via baking. Store metadata in `Space4XCameraRigAuthoring`.
- **Spawner system**: Introduce `Space4XCameraSpawnSystem` (managed) in the `Space4XCameraInputPhase` that:
  - Ensures a single rig entity exists for the active player.
  - Adds a `CameraRigOwner` buffer/component mirroring `PlayerCameraControl.CameraEntity`.
  - Disables the MonoBehaviour controller when ECS takes ownership.

### 2. Camera Stack & Focus Management

- Implement `Space4XCameraStack` (managed singleton) inspired by `GameApp.CameraStack.cs`:
  - Maintains a stack of `Camera` component references (from converted prefabs or existing scene cameras).
  - Provides `PushCamera`, `PopCamera`, and `TopCamera` operations.
  - Emits events when activation changes so render sync systems can adjust listeners or post-processing volumes.
- Provide a DOTS wrapper component (`ActiveCameraTag`, `CameraStackEntry`) so camera swap logic can run in ECS (e.g., when a cinematic system pushes a camera).

### 3. Phase Integration (Manual Groups)

Within the planned `Space4XCameraUpdateGroup` phases:

- **Input Phase**
  - `Space4XCameraInputSystem` remains, but consults runtime config vars for sensitivity and only writes to input singletons (no direct MonoBehaviour dependencies).

- **Simulation Phase**
  - `Space4XCameraSystem` processes `Space4XCameraInput`, updates `Space4XCameraState`, and raises `CameraStateChanged` events when significant deltas occur.
  - Introduce `CameraRigBudgetSystem` to reconcile bridge-provided motion budgets with ECS-scheduled ticks.

- **Sync Phase**
  - Replace `Space4XCameraRenderSyncSystem` with `Space4XCameraRigSyncSystem` that:
    - Reads `Space4XCameraState`, applies transforms to the rig entity’s `LocalTransform` (for ECS physics/render sync), and updates the active `Camera` GameObject via `TransformAccess` when needed.
    - Talks to `Space4XCameraStack` to ensure only the top camera is enabled (matching `PlayerCameraControl.UpdatePlayerCameras`).
  - Provide optional MonoBehaviour fallback `Space4XCameraFallbackBehaviour` for editor-only play sessions when ECS rig creation fails.

### 4. Bridging Strategy & Migration Path

1. **Phase 0** – Establish manual group structure (Task 2) without changing runtime behaviour. MonoBehaviour controller still active.
2. **Phase 1** – Introduce `Space4XCameraStack` and have MonoBehaviour push itself on enable. DOTS systems remain passive.
3. **Phase 2** – Implement ECS camera rig spawning/sync, gradually moving responsibilities from `Space4XCameraMouseController` into DOTS systems. MonoBehaviour becomes optional fallback (config var `camera.input_mode = mono|ecs`).
4. **Phase 3** – Remove redundant double `Consume` calls; rely on runtime config + manual groups to decide which pipeline (Mono or ECS) runs. Update diagnostics to report camera source.
5. **Phase 4** – Introduce multi-camera support (e.g., debug spectator) by leveraging the stack and adding commands `camera.push`, `camera.pop`, `camera.switch` via the new console.

### 5. Runtime Configuration Hook

- Config vars defined in `Runtime_Config_Service_Plan.md` (`camera.enable_pan`, etc.) feed the ECS pipeline and the Mono fallback. `Space4XCameraMouseController` reads config only when operating in fallback mode.
- Additional config var: `camera.mode` (enum) to select `mono`, `ecs`, or `hybrid`. Systems check this before updating.

## Key Deliverables

- `Space4XCameraStack` service + DOTS wrappers.
- ECS camera rig prefab & spawn system.
- Refactored sync system aligning with manual group phases.
- Migration toggles & diagnostics to compare Mono vs ECS behaviour (frame timings, pending budgets).
- Updated documentation/tutorial (for designers) explaining how to push cinematic cameras using the stack.

## Risks & Mitigations

- **Hybrid complexity**: Running both Mono and ECS simultaneously risks double-updates. Mitigate by gating each pipeline with config vars and ensuring only one writes to `Space4XCameraState` per frame.
- **Legacy scenes**: Scenes relying on `Camera.main` may break when ECS spawns its own rig. Provide an authoring component to adopt existing cameras into the stack during conversion.
- **Performance**: Ensure the stack and sync system operate in managed land to avoid Burst restrictions, similar to DOTS sample’s implementation.

This strategy positions us to match the DOTS sample’s camera robustness while keeping our tailored RTS controls and input bridge responsive.









