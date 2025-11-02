# Dual-Loop Demo Best Practices

This guide summarizes the workflow for keeping the GodGame villager loop and Space4X vessel loop deterministic, observable, and easy to profile.

## Scene Automation

- Run `Space4X > Setup Dual Mining Demo Scene` to seed villagers/vessels with presets, attach placeholder meshes, and wire resource authoring components.
- Use `Tools/PureDOTS/Apply Placeholder Visuals (SubScene)` to refresh all `PlaceholderVisualAuthoring` components if objects are modified manually.
- Villager job presets live in `VillagerJobPresetCatalog`. The setup helper falls back to defaults if the catalog is missing.

## Camera & Input

- `CameraInputBudget` amortises scroll/mouse deltas across catch-up ticks. Keep it in play mode when testing determinism.
- Preferred bridge sample rate: 60–240 Hz. Adjust via `Space4XCameraMouseController` inspector or `Space4XCameraInputBridge.ConfigureSampleRate`.
- `Space4XCameraDiagnostics` records ticks-per-frame, stale input counts, and budget backlogs. Review it through **Space4X ▸ Diagnose Camera & Entities**.

## Villager Loop Health

- `VillagerJobDiagnostics` (singleton) tracks total/idle villagers, pending requests, and active tickets.
- The diagnostic menu prints these counters; keep the HUD in sync if you build a runtime overlay.
- Deterministic presets ensure villagers start with reproducible stats (speed, morale, hunger). Override the catalog rather than tweaking individuals.

## Profiling & Testing

- Attach `PureDOTS.Diagnostics.CatchupHarness` to force periodic frame hitches and log diagnostics for both loops.
- Run `DualMiningDeterminismTests` (playmode) after scene changes; the test hashes relevant ECS components after 180 ticks and compares runs.
- When profiling manually, log `Space4XCameraDiagnostics` + `VillagerJobDiagnostics` alongside Unity Profiler markers to correlate catch-up bursts.

## Checklist Before Shipping Changes

- [ ] Dual mining scene rebuilt via setup menu and saved.
- [ ] `Space4XCameraDiagnostics` reports <=1 catch-up tick during smooth runs; stale tick count matches expected hitches.
- [ ] `VillagerJobDiagnostics` shows no runaway job queue or idle villagers during steady state.
- [ ] Determinism playmode test passes locally.
- [ ] Catch-up harness verifies camera budgets behave across 2–3 induced hitches.




