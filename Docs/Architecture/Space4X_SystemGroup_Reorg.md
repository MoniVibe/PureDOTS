# Space4X System Group Reorganization Blueprint

This proposal defines how we will migrate the Space4X gameplay loop toward the structured, manually orchestrated model used in `DOTSSample-master`. The aim is to introduce explicit update phases, configurable enable/disable controls, and instrumentation points without regressing our Entities 1.x compatibility.

## Design Goals

1. **Deterministic phase ordering** – mirror the sample’s `ManualComponentSystemGroup` pattern so Space4X subsystems run in well-defined stages (input, simulation, resolution, presentation).
2. **Selective activation** – allow test harnesses and replay tools to enable/disable whole phases (e.g. camera simulation, transport AI) without editing attributes on individual systems.
3. **Instrumentation hooks** – wrap each phase with profiling and telemetry entry points to give us the same observability that `AbilitySystemGroups` and `GameDebug` provided in the sample.
4. **Compatibility with existing systems** – accomodate both `ISystem` and `SystemBase` implementations while keeping integration straightforward for our current `PureDotsWorldBootstrap`.

## Manual Group Infrastructure

We will introduce a reusable base similar to `ManualComponentSystemGroup` but updated for Entities 1.2:

- `Space4XManualSystemGroup : ComponentSystemGroup`
  - Marked `[DisableAutoCreation]` so it is only instantiated by its parent group.
  - On creation, enumerates cached `Type` metadata (built at bootstrap) for systems tagged with `[UpdateInGroup(typeof(ThisGroup))]` and manually creates them via `World.GetOrCreateSystemManaged`.
  - Maintains a `List<ComponentSystemBase>` so the group can deterministically destroy children when disabled (mirroring the sample’s `DestroyGroup`).
  - Wraps `OnUpdate()` with `Profiler.BeginSample`/`EndSample` using a configurable label.

Bootstrap support:

- Extend `PureDotsWorldBootstrap` to cache the entity-system type list (equivalent of `GameBootStrap.Systems`). We can generate this once during initialization and reuse it for manual groups.
- Add helper APIs to instantiate manual groups (`ManualGroupFactory.Create<TGroup>(World)`) and ensure they are inserted into parent groups in the right order.

## Target Group Hierarchy

The following manual groups will sit under existing `PureDOTS.Systems` groups. Each phase will use nested manual groups to mirror the sample’s structured approach.

### 1. Space4X Camera Update

Parent: `PureDOTS.Systems.CameraInputSystemGroup` (already `OrderFirst` in Simulation).

- `Space4XCameraUpdateGroup : Space4XManualSystemGroup`
  - Profiling label: `Space4X/Camera`
  - Sub-phases (all `[DisableAutoCreation]` manual groups):
    1. `Space4XCameraInputPhase` → systems reading raw input.
    2. `Space4XCameraSimulationPhase` → systems mutating camera ECS state.
    3. `Space4XCameraSyncPhase` → systems syncing ECS state to GameObject renderers.

| Existing System | Target Phase | Notes |
| --- | --- | --- |
| `Space4XCameraInputSystem` | `Space4XCameraInputPhase` | Remains managed, phase wraps instrumentation; remove direct `[UpdateBefore]` once manual ordering exists. |
| `Space4XCameraSystem` (`ISystem`) | `Space4XCameraSimulationPhase` | Gains deterministic slot immediately after input; manual group enforces rewind gating. |
| `Space4XCameraRenderSyncSystem` | `Space4XCameraSyncPhase` | Handles applying state to `Camera`/`Transform` components before presentation. |
| `Space4XCameraDiagnostics` writer systems (future) | `Space4XCameraSyncPhase` | Placeholder for instrumentation updates. |

### 2. Space4X Transport Update

Parent: `PureDOTS.Systems.GameplaySystemGroup`

- `Space4XTransportUpdateGroup : Space4XManualSystemGroup`
  - Profiling label: `Space4X/Transport`
  - Phases:
    1. `Space4XTransportBootstrapPhase` – one-time registry setup / maintenance (`TransportBootstrapSystem`, `SingletonCleanupSystem`).
    2. `Space4XTransportRegistryPhase` – registry population (`TransportRegistrySystem`).
    3. `Space4XTransportCommandPhase` – command issuing systems (future expansions: route planners, request brokers).

### 3. Space4X Vessel Update

Parent: `Space4XTransportUpdateGroup` (after registries, before history capture) or directly under `GameplaySystemGroup`.

- `Space4XVesselUpdateGroup : Space4XManualSystemGroup`
  - Profiling label: `Space4X/Vessels`
  - Phases mirroring gameplay lifecycle:
    1. `Space4XVesselTargetingPhase` → `VesselTargetingSystem`, `VesselAISystem`.
    2. `Space4XVesselMovementPhase` → `VesselMovementSystem`, `VesselMovementDebugSystem`.
    3. `Space4XVesselInteractionPhase` → `VesselGatheringSystem`, `VesselDepositSystem`.
    4. `Space4XVesselDiagnosticsPhase` → `EntityStateDebugSystem`, `MovementDiagnosticSystem`.

### 4. Space4X History Recording

Parent: `PureDOTS.Systems.HistorySystemGroup`

- `Space4XHistoryPhaseGroup : Space4XManualSystemGroup`
  - Houses any Space4X-specific time adapters or replay emitters so they can be toggled or profiled together.
  - Prepares ground for the physics history buffer (task 5) by giving us a consistent injection point after gameplay and before `HistorySystemGroup` finishes.

## Ordering Guarantees

1. `Space4XCameraUpdateGroup` runs before any other simulation systems because it remains inside `CameraInputSystemGroup`, which is `OrderFirst` in `SimulationSystemGroup`.
2. `Space4XTransportUpdateGroup` executes after `SpatialSystemGroup`, leveraging the existing `[UpdateAfter(typeof(SpatialSystemGroup))]` attribute on the parent group, so registries have resolved spatial metadata.
3. `Space4XVesselUpdateGroup` executes after the transport registries to ensure targeting and movement have up-to-date data.
4. `Space4XHistoryPhaseGroup` sits at the tail end of simulation via `HistorySystemGroup`, guaranteeing it sees final state for the frame.

## Implementation Steps (High Level)

1. **Manual group scaffolding**
   - Add `Space4XManualSystemGroup` + helper factory (`Assets/Scripts/Space4x/Systems/Groups` namespace).
   - Extend `PureDotsWorldBootstrap` to compute & cache system `Type` metadata (reuse for all manual groups).

2. **Camera pipeline migration**
   - Create the three camera phases and retarget existing systems with `[UpdateInGroup]` attributes.
   - Strip redundant `UpdateBefore/After` directives once manual ordering is validated.

3. **Transport & vessel phases**
   - Introduce `Space4XTransportUpdateGroup` & `Space4XVesselUpdateGroup`, adding `[DisableAutoCreation]` and `Profiler` wrappers.
   - Register phases within `PureDotsWorldBootstrap.ConfigureRootGroups()` (after world creation).

4. **History phase group**
   - Stand up `Space4XHistoryPhaseGroup` and move Space4X-specific replay/time adapter systems into it for consistency.

5. **Testing hooks**
   - Provide editor-only tools or console commands (once the runtime config layer exists) to toggle whole groups for soak tests and diagnostics.

This reorganization blueprint ensures our upcoming runtime config, camera refactor, and history buffer work all plug into a coherent, phase-driven update model analogous to the DOTS sample while staying native to the latest Entities packages.




