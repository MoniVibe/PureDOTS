# DOTS Alignment Roadmap

This roadmap sequences the architectural upgrades required to bring PureDOTS up to the standard of the legacy DOTS Sample while staying compatible with Entities 1.x and our current feature set. Updated October 2025 to reflect Unity 6 / Entities 1.4.2 dependencies (no NetCode runtime, URP render stack).

## Phase 0 – Foundation (Week 0)

- ✅ Gap analysis (`DOTS_Sample_Gap_Analysis.md`), manual group blueprint, runtime config plan, camera hybrid strategy, physics history plan.
- ✅ Entities 1.4.2 compatibility review: confirm removal of `ClientServerBootstrap`, identify `ISystem` coverage, document bootstrap flow (see gap analysis update).
- Output: design documentation only (no runtime changes yet).

## Phase 1 – Manual System Group Infrastructure (Weeks 1-2)

- Implement `Space4XManualSystemGroup` and bootstrap support (type cache + factory) using 1.4 `SystemHandle` APIs.
- Stand up `Space4XCameraUpdateGroup`, `Space4XTransportUpdateGroup`, `Space4XVesselUpdateGroup`, `Space4XHistoryPhaseGroup` with profiler hooks.
- Regression tests: ensure current scenes run without behavioural deltas; add automated checks for group enable/disable toggles.
- Risks: Entities 1.x `SystemBase` vs `ISystem` compatibility—verify both creation paths and guard against default-world auto instancing.

## Phase 2 – Runtime Config & Console (Weeks 2-3)

- Build `RuntimeConfigRegistry`, attribute scanning, persistence to `puredots.cfg`.
- Ship minimal debug console (IMGUI overlay) with commands for cvar management.
- Wire camera tuning, diagnostics toggles, and manual group enables to config vars.
- Tests: editmode reflection scan; playmode console smoke test; confirm config persistence across sessions.
- Compatibility notes: isolate all reflection/IO to managed init systems so Burst/Jobs builds remain deterministic; ensure IL2CPP AOT stubs are generated for console command delegates.

## Phase 3 – Camera Pipeline Migration (Weeks 3-5)

- Implement `Space4XCameraStack`, ECS camera rig prefab + spawn system, and stack integration.
- Migrate `Space4XCameraInputSystem`/`Space4XCameraSystem` into manual phases; create fallback mode for Mono controller (config-driven).
- Introduce `Space4XCameraRigSyncSystem` to replace direct `Camera.main` manipulation; ensure bridging handles rewinds and diagnostics.
- Testing: side-by-side comparison harness toggling mono vs ECS mode; record latency/diagnostic metrics; regression tests for reset/perspective toggles.
- Compatibility notes: adopt Entities Graphics camera baking and URP camera stacks; confirm authoring path works with subscenes/bakers.

## Phase 4 – Physics History Buffer (Weeks 5-6)

- Deliver `PhysicsHistorySettings`, buffer singleton, `PhysicsHistoryCaptureSystem`, and query API.
- Integrate with transport diagnostics and record sample playback in rewind sandbox scene.
- Expose config vars for enablement and buffer length.
- Tests: automated verification that buffer depth is respected, clones match live physics snapshots, and memory is reclaimed on teardown.
- Compatibility notes: leverage `PhysicsWorldSingleton` for cloning; guard Burst jobs with `WithStructuralChanges`-free pathways; ensure history capture toggles respect rewind guards.

## Phase 5 – Tooling & Observability (Weeks 6-7)

- Extend console with scripted commands (camera push/pop, history dump).
- Surface telemetry overlays for camera diagnostics, transport registry continuity, and physics history health (leveraging manual group instrumentation).
- Add documentation updates (designer-facing how-tos, troubleshooting).

## Phase 6 – Hardening & QA (Week 8)

- Conduct performance profiling with new systems enabled vs disabled.
- Run soak tests using runtime config toggles to flip between modes mid-session.
- Lock down regression baselines for camera control, transport loops, and rewind scenarios.

## Testing Strategy Summary

- **Unit/Integration Tests**: Attribute scanning, config persistence, manual group creation/destroy, physics history ring buffer.
- **Playmode Automation**: Camera responsiveness (input latency metrics), transport registry integrity, rewind playback with physics history.
- **Manual QA**: Console command workflows, multi-camera stack operations, toggle-based scenario testing.

## Dependencies & Sequencing Notes

- Manual group infrastructure (Phase 1) underpins camera migration and history capture; treat as critical path.
- Runtime config (Phase 2) is prerequisite for toggling between mono/ECS camera pipelines and enabling physics history on demand.
- Physics history relies on the history phase group introduced in Phase 1 and config toggles from Phase 2.

## Risk Mitigation Checklist

- Schedule regular sync points with gameplay teams to validate camera feel after Phase 3.
- Profile memory usage of physics history buffer under stress before enabling by default.
- Maintain feature flags via config vars so we can ship partially completed features behind toggles if necessary.

Following this roadmap positions PureDOTS to gradually absorb DOTS Sample practices without destabilizing ongoing development.


