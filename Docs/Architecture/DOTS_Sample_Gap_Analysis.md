# DOTS Sample vs PureDOTS Architecture – Gap Analysis

This document captures the architectural patterns present in the legacy `DOTSSample-master` project (2019.3 timeframe) and contrasts them with the current `PureDOTS` implementation. The intent is to surface the deltas that matter for our modernization effort so we can selectively adopt proven practices without regressing on the newer Entities 1.x stack.

## Entities Version Context (2025 Update)

- **Legacy sample baseline**: built on Unity 2019.3, Entities 0.4, Hybrid Renderer v0.3, NetCode/Transport preview packages, and manual HDRP configuration. Several APIs used there were removed or renamed post Entities 1.0 (e.g., `ClientServerBootstrap`, `GhostCollection`, legacy `ManualComponentSystemGroup`).
- **PureDOTS baseline (Oct 2025)**: Unity 6000.0+ with Entities 1.4.2, Entities Graphics 1.4.15, Physics 1.0.16, URP 17.x. Multiplayer stack is currently out of scope; we target single-world simulation with optional headless worlds. Hybrid Renderer now runs through `Unity.Rendering` authoring with baking workflows.
- **Compatibility guideline**: When porting ideas from the sample, replace deprecated APIs with the Entities 1.x equivalents (`ICustomBootstrap`, `SystemHandle` metadata, baking systems). Maintain Burst compliance and IL2CPP compatibility; reflection-heavy utilities must execute on the managed main thread before Burst workloads begin.

## PureDOTS Bootstrap Flow (Entities 1.4.2)

Current runtime boot sequence aligned with `PureDotsWorldBootstrap.cs` and `SystemGroups.cs`:

```
PureDotsWorldBootstrap.Initialize()
  -> create World(defaultWorldName, WorldFlags.Game)
  -> DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(...)
  -> ConfigureRootGroups(world)
     -> InitializationSystemGroup.SortSystems()
     -> FixedStepSimulationSystemGroup.SortSystems()
     -> SimulationSystemGroup.SortSystems()
     -> PresentationSystemGroup.SortSystems()
  -> materialize custom groups
     -> EnvironmentSystemGroup / SpatialSystemGroup / GameplaySystemGroup
     -> TimeSystemGroup / HistorySystemGroup / Domain groups (villager, resource...)
  -> Set FixedStepSimulationSystemGroup.Timestep = 1/60
  -> ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world)

SimulationSystemGroup order (high-level)
  [CameraInputSystemGroup]
  [PhysicsSystemGroup] (BuildPhysicsWorld...ExportPhysicsWorld)
  [EnvironmentSystemGroup]
  [SpatialSystemGroup]
  [GameplaySystemGroup]
  [LateSimulationSystemGroup { HistorySystemGroup }]
```

This flow highlights where manual instantiation hooks, future world profiles, and phase controllers must integrate. Each new manual group should slot into the diagram without changing the baked order guarantees recorded in `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`.

## Overview

- **DOTS Sample strengths**: manual bootstrapping via `GameBootStrap`, deterministic phase orchestration with `ManualComponentSystemGroup`, pervasive runtime config/console tooling, ECS-driven camera activation (`PlayerCameraControl` + `GameApp.CameraStack`), and physics history capture (`PhysicsWorldHistory`).
- **PureDOTS snapshot**: single-world bootstrapper (`PureDotsWorldBootstrap.cs`), curated domain system groups (`SystemGroups.cs`), heavy singleton seeding through `CoreSingletonBootstrapSystem.cs`, MonoBehaviour ↔ DOTS bridges for camera/input (`Space4XCameraMouseController.cs`), and bespoke diagnostics hooks but no generalized runtime console/config layer.
- **Key opportunities**: layer in explicit manual system activation, modular runtime configuration, ECS-first camera lifecycle, and upgraded history snapshotting while preserving our Entities 1.x compliant structure.

## Bootstrapping & World Lifecycle

- **DOTS Sample**: `Assets/Unity.Sample.Game/GameBootstrap/GameBootStrap.cs` extends `ClientServerBootstrap` (Entities 0.4) to build client/server worlds, resolves all assemblies to cache system types, and constructs worlds manually before wiring them into the player loop. Combined with `ManualComponentSystemGroup.cs`, the bootstrapper instantiates and tracks nested system groups explicitly.
- **PureDOTS (Entities 1.4.2)**: `Packages/com.moni.puredots/Runtime/Systems/PureDotsWorldBootstrap.cs` creates a single `World`, pulls in all auto-created systems via 1.x `DefaultWorldInitialization`, and calls `SortSystems()` on root groups. We pre-create domain-specific groups (environment, spatial, gameplay, etc.) but rely on standard Entities discovery rather than manual instantiation. Headless/server worlds and replay timelines require a new bootstrap profile layer because `ClientServerBootstrap` no longer exists in Entities 1.x.
- **Gap**: no mechanism to mirror the sample’s manual system lifecycle management, nor a way to build custom world variants (e.g., offline replay vs. authoring). Introducing a managed registry of system types and explicit group creation in a 1.x-compatible fashion (`WorldUnmanaged`, `SystemHandle`) lets us extend initialization predictably without reviving removed APIs.
- **Progress (Oct 2025)**: `SystemRegistry` now resolves `BootstrapWorldProfile`s (default/headless/replay) before world creation and feeds the profile-filtered system list into `PureDotsWorldBootstrap`. Manual phase controllers remain todo, but profile selection and inclusion/exclusion hooks match the legacy sample’s flexibility.

## System Orchestration & Update Phases

- **DOTS Sample**: `AbilitySystemGroups.cs` showcases deterministic phase groupings (request, movement, resolve, prepare, update) implemented as `ManualComponentSystemGroup` derivatives with profiling scopes. Systems opt into these phases via `[UpdateInGroup]` attributes, and the parent group owns creation/destruction.
- **PureDOTS**: `Packages/com.moni.puredots/Runtime/Systems/SystemGroups.cs` defines numerous `ComponentSystemGroup` subclasses with ordering attributes. Systems are still auto-created. Entities 1.x adds `ISystem`/`SystemState` variants that we must track manually when instantiating; our current flow relies on default discovery, so there is no parent-driven dynamic enable/disable model or profiling at the group boundary.
- **Gap**: we lack a consistent pattern for phased execution (e.g., Space4X camera input vs. simulation vs. render sync). Adopting manual groups means building a 1.x-safe controller that can toggle `SystemState.Enabled`, manage `ISystem` lifetimes, and inject instrumentation without relying on deprecated `ManualComponentSystemGroup` APIs.
- **Progress (Oct 2025)**: Introduced `ManualPhaseSystemGroup` base with `CameraPhaseGroup`, `TransportPhaseGroup`, and `HistoryPhaseGroup`. `ManualPhaseControl` + controller system mirror the sample’s manual toggles, instrumentation hooks into frame timing, and `LogisticsRequestRegistrySystem` now executes under the transport phase for deterministic sequencing.

## Runtime Configuration & Debug Tooling

- **DOTS Sample**:
  - `Unity.Sample.Core/Scripts/ConfigVars/ConfigVars.cs` auto-registers `[ConfigVar]` fields across assemblies, persists user overrides, and flags dirty state.
  - The console layer (`Unity.Sample.Core/Scripts/Console/Console*.cs`) exposes config vars, commands, and debugging overlays at runtime.
  - `GameDebug`/`DebugDisplay` provide deterministic logging and on-screen graphs for gameplay metrics.
- **PureDOTS**:
  - No equivalent config var system; configuration lives in scriptable objects or hard-coded defaults (e.g., `Space4XCameraInitializationSystem` selects defaults, `CatchupHarness` uses serialized fields).
  - Diagnostics are ad hoc: `CatchupHarness.cs` logs snapshots, `Space4XCameraDiagnostics` stores counters, but there is no interactive console or persistence of runtime tweaks.
- **Gap**: missing unified runtime configuration and developer console prevents rapid iteration on camera tuning, AI thresholds, or history settings. Entities 1.x allows us to run managed configuration systems in initialization (outside Burst), so we can reintroduce config vars with updated reflection guards and persist overrides in `UserSettings` without conflicting with the baking pipeline.
- **Progress (Oct 2025)**: `RuntimeConfigRegistry` scans `[RuntimeConfigVar]` attributes, persists overrides to `UserSettings/puredots.cfg`, and drives the `RuntimeConfigConsole` overlay. Manual phase toggles (`phase.*.enabled`) now round-trip through the registry so designers can enable/disable camera/transport/history phases live.

## Camera Lifecycle & Input Bridging

- **DOTS Sample**: `PlayerCameraControl.cs` uses ECS state components to spawn camera prefabs, push/pop cameras on `GameApp.CameraStack`, and gate audio listeners. Input is coordinated via the config/console system, allowing debug detachment and instrumentation.
- **PureDOTS**: `Assets/Scripts/Space4x/Camera/Space4XCameraMouseController.cs` is a MonoBehaviour marked `ICameraStateProvider`. Each `Update()` pulls input snapshots (`Space4XCameraInputBridge`), copies them into ECS state, applies transforms directly to `Camera.main`, and repeatedly disposes/recreates queries when the world changes. No shared camera stack exists, so multiple viewer contexts would conflict; two `Consume` calls per frame reveal coordination friction. Entities Graphics 1.4 provides camera baking and `CameraAuthoring` components that we do not yet leverage.
- **Gap**: ECS systems neither own camera spawning nor coordinate activation order. The MonoBehaviour handles bridging, reducing determinism and making it hard to swap to pure ECS cameras, cinematic views, or multi-observer setups. We must migrate toward `EntityQueryBuilder`-driven camera rigs with Entities Graphics-compatible components while leaving a hybrid fallback for URP camera effects.
- **Progress (Oct 2025)**: Added `Space4XCameraEcsSystem` in `CameraPhaseGroup`, `Space4XCameraRigSyncSystem` for presentation, and `camera.ecs.enabled` runtime toggle. DOTS pipeline now owns camera state when enabled, while `Space4XCameraMouseController` becomes a fallback hybrid controller.

## Time, History & Rewind Infrastructure

- **DOTS Sample**:
  - `GameTime.cs` and `GameTimeSystem.cs` maintain tick-based timing accessible via singleton `GlobalGameTime`.
  - `PhysicsWorldHistory.cs` clones the physics collision world every tick (buffer depth 16) so reconciliation and rewind can be deterministic.
- **PureDOTS**:
  - `CoreSingletonBootstrapSystem.cs` seeds `TimeState`, `GameplayFixedStep`, `HistorySettings`, `RewindState`, plus numerous registries. `HistorySystemGroup` (see `SystemGroups.cs`) hosts systems that capture history, and design docs outline usage.
  - We **do not** currently snapshot the `PhysicsWorld` or expose frame-accurate history buffers; rewind relies on higher-level registries and time adapters (e.g., `StorehouseInventoryTimeAdapterSystem`).
- **Gap**: history capture covers domain data but omits the underlying physics world, limiting fidelity when we require deterministic hit or collision replay. Entities Physics 1.0 exposes `PhysicsWorldSingleton` and `SimulationSingleton` APIs for cloning without relying on deprecated `BuildPhysicsWorld` internals; we need to wrap these in Burst-friendly history buffers and integrate them with our rewind adapters.
- **Progress (Oct 2025)**: `PhysicsHistoryCaptureSystem` records a configurable ring buffer of cloned `PhysicsWorld` instances (toggled via `history.physics.enabled`), with `PhysicsHistoryHandle`/`PhysicsHistory.TryClonePhysicsWorld` exposing snapshots for diagnostics and rewind tests.

## Diagnostic & Debug Overlay Parity

- **DOTS Sample**: `Unity.Sample.Core/Scripts/DebugDisplay` and `Unity.Sample.Core/Scripts/DebugOverlay` provide on-screen graphs, line rendering, and real-time overlays, tightly integrated with the console/config system.
- **PureDOTS**: instrumentation exists via ECS components (`TelemetryStream`, `FrameTimingStream`, `RegistryInstrumentationState`) but surfaces only through logs or bespoke editor inspectors. There is no immediate-mode overlay or console toggles to visualize metrics in play mode.
- **Gap**: bridging telemetry to UI requires custom effort; we miss the plug-and-play visibility the sample offers for diagnosing camera motion, registry stability, or network states. Entities 1.4 supports `SystemAPI.Time` profiler markers and editor-only IMGUI overlays; we can layer an in-game overlay that respects Burst restrictions by executing on the main thread in presentation systems.
- **Progress (Oct 2025)**: Added `RuntimeConfigConsole` commands (`overlay`, `history`) plus `DiagnosticsOverlayBehaviour` (toggled via `overlay.runtime.enabled`) to show camera state, manual phase toggles, and physics history status at runtime.

## Summary of Needed Enhancements

| Area | DOTS Sample Capability | PureDOTS Status | Required Action |
| --- | --- | --- | --- |
| Bootstrapping | Manual world + system instantiation (`GameBootStrap`, `ManualComponentSystemGroup`) | Single-world bootstrap without manual lifecycle | Introduce managed system registry + optional world profiles |
| Update Phases | Explicit phase groups with profiling (`AbilitySystemGroups`) | Attribute-driven ordering only | Create manual groups for camera, transport, resource phases with instrumentation |
| Runtime Config | `[ConfigVar]` discovery + console | None | Port lightweight config var & console layer, integrate with diagnostics |
| Camera Control | ECS-managed cameras + `GameApp.CameraStack` | MonoBehaviour bridge, no stack | Shift to ECS ownership of camera entities, add shared stack for focus control |
| History Capture | `PhysicsWorldHistory` clones, `GameTime` API | High-level history only, no physics snapshots | Implement physics/world history buffers aligned with Entities 1.x |
| Debug Overlay | In-game overlays & graphs | Telemetry via components only | Surface telemetry through overlays/console commands |

This analysis underpins the subsequent design tasks: designing manual system groups, defining runtime config services, reworking camera orchestration, and prototyping physics history capture so we attain the DOTS Sample’s architectural rigor while remaining tailored to PureDOTS.


