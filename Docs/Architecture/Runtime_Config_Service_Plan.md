# Runtime Config & Console Service Plan

This plan outlines how we will introduce a lightweight runtime configuration system—modeled after `DOTSSample-master`’s `ConfigVar` + console stack—tailored to `PureDOTS` and the Entities 1.x runtime. The goal is to expose tweakable parameters (camera tuning, AI thresholds, diagnostics toggles) without code changes or editor-only workflows.

## Objectives

1. **Zero-recompile tuning** – mirror the sample’s ability to flip settings during play mode (via console or config files).
2. **Deterministic propagation** – changes to config values must surface in DOTS systems deterministically each frame (e.g., through singletons or reactive events).
3. **Persistence** – honour user overrides across sessions with a simple text-based config file (e.g., `UserSettings/puredots.cfg`).
4. **Console bridge** – provide a minimal console overlay/interface to set and inspect vars (exposed later via UI or debug window).

## Architecture Overview

### Core Types

- `RuntimeConfigVarAttribute`
  - Applied to `static RuntimeConfigVar` fields (mirrors sample’s `[ConfigVar]`).
  - Metadata: name (optional override), description, default value, flags (`Save`, `Cheat`, `User`).

- `RuntimeConfigVar`
  - Holds string/int/float representations, change detection flag, and optional callbacks.
  - Emits events or registers with a central store on change.

- `RuntimeConfigRegistry`
  - Singleton service responsible for scanning assemblies (via `AppDomain.CurrentDomain.GetAssemblies()`), instantiating attributed fields, and managing lookup dictionary (`Dictionary<string, RuntimeConfigVar>`).
  - Provides APIs: `TryGet(string, out RuntimeConfigVar)`, `Set(string, string)`, `ResetAll()`, `Save(string path)`, `Load(string path)`.

- `RuntimeConfigSystem`
  - An `ICustomBootstrap`-spawned initialization system that ensures the registry is ready before game systems run, injects config singleton data where needed, and handles persistence events (auto-save on shutdown).

### Persistence Layer

- Default file path: `%LOCALAPPDATA%/PureDOTS/UserSettings/puredots.cfg` (Windows) with override for other platforms.
- Format: `cvar_name "value"` per line (same as the sample for familiarity).
- Load order: defaults → disk overrides → command-line overrides (future) → runtime updates.

### Console Interface

We will start with a simple `DebugConsole` service that:

- Renders a basic IMGUI or UIToolkit window when toggled (bound to `~` key by default).
- Supports commands: `cvar.list`, `cvar.get <name>`, `cvar.set <name> <value>`, `cvar.reset <name>`, `help`.
- Shows feedback messages routed through Unity’s `Debug.Log` and an on-screen log pane.

Longer term we can merge this with telemetry overlays (tying into the planned manual groups instrumentation).

## Integration Targets

### Camera Tuning

- Expose the following Space4X camera knobs as config vars:
  - `camera.pan_speed`, `camera.vertical_speed`, `camera.zoom_speed`, `camera.rotation_speed`.
  - `camera.pitch_min`, `camera.pitch_max`, `camera.zoom_min`, `camera.zoom_max`.
  - `camera.enable_pan/zoom/rotation` (boolean toggles stored as `0`/`1`).
- Add a DOTS system (`Space4XCameraConfigSyncSystem`) that reads current config vars each frame (or when `RuntimeConfigVar.ChangeCheck()` fires) and updates the `Space4XCameraConfig` singleton.
- Provide console commands such as `camera.reset` or `camera.dump` for debugging.

### Diagnostics & AI

- Convert existing toggles to config vars:
  - `debug.cameradiagnostics` → toggles `Space4XCameraDiagnostics` logging.
  - `debug.villagerjobs` → toggles `CatchupHarness` logging.
  - `transport.debug.graph` → enables future overlay graphs in transport/vessel phases.

### Manual Group Controls

- Add config vars for enabling/disabling manual groups defined in the reorg blueprint, e.g., `space4x.enable_camera_phase`, `space4x.enable_transport_phase`.
- Manual groups respond by toggling their child systems or skipping `OnUpdate()` when flags are false.

## Implementation Phases

1. **Registry & Attribute**
   - Implement `RuntimeConfigVarAttribute`, `RuntimeConfigVar`, and `RuntimeConfigRegistry` in `Packages/com.moni.puredots/Runtime/Config`.
   - Unit test scanning, change detection, and persistence using editmode tests.

2. **Console MVP**
   - Build `DebugConsole` with minimal IMGUI overlay, hooking into Unity input for toggling.
   - Register commands to query and mutate config vars.

3. **Camera Integration**
   - Declare camera-related config vars (likely in `Space4X.CameraControls.Space4XCameraConfigVars`).
   - Add sync system to push values into `Space4XCameraConfig` each frame or on change.

4. **Diagnostic Hooks**
   - Wire `CatchupHarness` and telemetry systems to read config vars instead of serialized fields only.
   - Provide sample console scripts (e.g., `cvar.set debug.cameradiagnostics 1`).

5. **Phase Toggle Support**
   - Expose config vars for manual group enablement; manual groups check the var at beginning of `OnUpdate()`.

6. **Polish & Persistence**
   - Implement delayed auto-save (e.g., save changed vars after 2 seconds of inactivity to avoid disk spam).
   - Add CLI integration (optional) to set config values via command-line at launch.

## Risks & Mitigations

- **Reflection cost**: Cache attribute lookups at bootstrap just like the sample to avoid per-frame reflection.
- **Entities compatibility**: Ensure console + registry live in managed land; DOTS systems access data via singletons or plain C# services (no burst-critical paths).
- **Thread safety**: Mutations happen on the main thread via console or UI; `RuntimeConfigVar` updates raise change flags consumed by DOTS systems during update.

Delivering this runtime configuration layer is a prerequisite for camera refactoring, manual group toggles, and future debugging overlays, aligning PureDOTS with the DOTS sample’s workflow flexibility.




