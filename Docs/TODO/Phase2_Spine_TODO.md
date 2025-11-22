# Phase 2 – PureDOTS Spines (A/B/C)

Prioritize these shared spines so both games can run on the same engine surface. Keep changes inside `Packages/com.moni.puredots` and document in TruthSources as you land them.

## Parallel Agent Slices (run in tandem)
- **Slice 1 – Frame/Time spine** → Agent Alpha (implementation) owns Section A; Error/Glue agent validates tests/handoffs.
- **Slice 2 – Presentation spine** → Agent Beta (implementation) owns Section B; Error/Glue agent ensures bridge safety and tests.
- **Slice 3 – Registry/Continuity spine** → Agent Gamma (implementation) owns Section C; Error/Glue agent adds validation/telemetry tests.
- Documentation agent trails all slices to update truth-sources and guides after code/tests land; note touched files on handoff.

## A. Frame/Time Spine (deterministic core, rewind-safe)
**Owner:** Agent Alpha (Implementation) → hand off to Error/Glue agent for test hardening
- [x] Implement `TickTimeState`/`RewindState` singletons with paused/playing/target tick fields surfaced to HUD/debug (TickTimeState + HUD strings, DotsDebugHUD/DebugDisplay updated).
- [x] Tie `FixedStepSimulationSystemGroup` and Simulation group gates to `TickTimeState`/`RewindState` (SimulationTickGateSystem blocks playback/pause; TimeTickSystem freezes outside Record; fixed-step only when playing).
- [x] Add ring-buffer `InputCommandLog` and `SnapshotLog` with configurable capacities (seconds * 60), budget asserts, and debug UI exposure (TimeLogSettings/defaults + TimeLogUtility, HUD text/reader wired).
- [x] Enforce Presentation read-only contract: mutations only via ECB on group boundaries; throw in editor for direct structural changes (presentation guard sentinel around Begin/End Presentation ECB).
- [x] PlayMode tests (6–10) for pause/play/step, rewind 2–5 seconds, and fixed-tick determinism under variable frame rates (`Runtime/Tests/Time/TimeSpinePlayModeTests.cs`, 6 cases covering pause/step, gating, rewind, determinism, logs).

## B. Presentation Spine (binding without assets)
**Owner:** Agent Beta (Implementation) → hand off to Error/Glue agent for bridge safety/rewind tests
- [x] Define cold data: `Presentable` tag + `PresentationBinding` blob mapping Effect/Companion IDs → presentation kinds/style tokens (lookup utility + style overrides).
- [x] Define hot request buffers: `PlayEffectRequest`, `SpawnCompanionRequest`, `DespawnCompanionRequest`, `PresentationCleanupTag` with lifetimes (request hub + failure counters).
- [x] Ordering: `BeginPresentationECBSystem` before rendering, `PresentationBridgePlaybackSystem`/`PresentationCleanupSystem` inside `PresentationSystemGroup`, `EndPresentationECBSystem` for despawns; add editor-only structural-change guard.
- [x] Hybrid-safe bridge: single `PresentationBridge` MonoBehaviour with pooled placeholders (mesh primitive, particle stub, VFX Graph stub, audio stub) driven only by IDs.
- [x] Scene checklist + docs in `PresentationBridge_TODO.md`; PlayMode tests for request playback, pool reuse, cleanup, and failure paths landed.

## C. Registry/Continuity Spine (content without commitment)
**Owner:** Agent Gamma (Implementation) → hand off to Error/Glue agent for validation + telemetry hooks
- [x] Schema: `RegistryId`/`TelemetryKey`/`ContinuityMeta` + optional hybrid prefab guid now live on `RegistryMetadata` (defaults derive from `RegistryKind` labels).
- [x] Authoring: `RegistryCatalogAsset` + baker emits sorted `RegistryDefinitionBlob` (`RegistryDefinitionCatalog` component) for downstream adapters.
- [x] Continuity validation: EditMode validator + NUnit coverage enforces unique IDs, version/residency consistency before PlayMode.
- [x] Legacy/hybrid shim: `HybridPrefabGuid` threaded through registry definitions for presentation bridges when ECS mesh is missing.
- [x] Telemetry/metrics: debug/telemetry buffers now expose registry definition counts and continuity snapshot counters (version/last tick).
