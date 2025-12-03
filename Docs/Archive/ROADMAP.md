# PureDOTS Game Foundation Roadmap

a) Core DOTS template focusing on spatial services, registry infrastructure, and deterministic systems.

## Phase 1 – Spatial & Rebuild Foundations
- Partial rebuild pipeline: dirty tracking coverage, rebuild metrics, playmode tests moving entities across cells.
- Spatial metadata consumers: extend registry `CellId`/`SpatialVersion` usage to AI sensors, miracles, future transport.
- Spatial telemetry & docs: expose rebuild strategy/timing; add HUD fields highlighting stale registry metadata.
- Spatial provider abstraction: finalize `ISpatialGridProvider`, encapsulate hashed-grid logic, add provider validation.

## Phase 2 – Registries & Logistics
- Additional domain registries: transport (miner vessels, haulers), miracles, construction, villagers.
- Registry helper adoption: migrate consumers to `RegistryEntryLookup` with cached cell metadata and fallbacks.
- Registry instrumentation: health metrics (version mismatches, stale caches) wired into telemetry.
- Continuity contracts: interface tests confirming registry↔spatial version/cell integrity regardless of producer order.

## Phase 3 – Environment & Terrain Cadence
- Shared environment grids (moisture, temperature, wind, sunlight) with authoring assets and sampling APIs.
- Biome/terrain versioning pipeline integrating with spatial rebuilds and registries.
- Ensure dependent systems (vegetation, miracles) consume the shared cadence with deterministic ordering.

## Phase 4 – Rewind & Time Determinism
- Guard systems for every group and deterministic behaviour under playback/catch-up.
- Deterministic tests covering gather/delivery, deposit/withdraw, AI transitions, partial rebuilds under rewind.
- Rewind-friendly state surface: define snapshot/diff contract for `SpatialGridState` + dirty ops.

## Phase 5 – Input & Hand Framework
- Centralized hand/router state machine with registry-aware targeting for resources and miracles.
- Interaction tests covering priorities, cooldowns, and spatial metadata reliance.

## Phase 6 – Tooling & Observability
- Debug HUD/console enhancements (spatial stats, cooldowns, rebuild telemetry).
- Editor tooling: spatial authoring validators, partial rebuild visualizer, environment config checks.
- Telemetry pipeline hardening: structured snapshots, rolling averages, documented thresholds.

## Phase 7 – CI & Template Packaging
- Automated playmode/editor suites (partial rebuild, registry integrity, rewind scenarios).
- Reference scenes demonstrating template features without game-specific logic.
- Template usage guide covering required singletons, system order, helper APIs, and package consumption from downstream game repos (`../Godgame`, `../Space4x`, etc.).
- Publish PureDOTS as a Unity package (UPM/git) and update consumer manifests so game code never lives under the template folder.
- High-scale soak harness: spawn configurable entities, vary dirty ratios, tune partial-rebuild heuristics.

## Phase 8 – Stabilization Gate
- All template tests green in CI.
- Documentation cross-linked (RuntimeLifecycle, Spatial Partitioning, Registry Plan, Rewind Patterns).
- No outstanding template TODOs; remaining work moves to game-specific prompts.
- Template declared theme-agnostic and ready for game-layer development.
