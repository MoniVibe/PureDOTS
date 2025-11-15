# Presentation & Companion Bridges TODO

## Goal
- Define a pure DOTS-friendly presentation strategy (hot/cold archetypes, companion entities, UI bridges) that respects rewind and keeps Mono glue minimal.
- Prepare the template for future visual polish without blocking current simulation work.

## Alignment
- Maps to PureDOTS vision pillars: “Presentation Bridges” and “Observability & Automation”.
- Supports SceneSetup guidelines and upcoming content expansion (creature, city visuals, HUD).

## Completed Workstreams
- [x] Archetype design: documented hot/cold splits, companion entities, and conversion workflows (`Docs/DesignNotes/PresentationBridgeContracts.md`).
- [x] Companion systems: core request processors live in `PresentationSpawnSystem` / `PresentationRecycleSystem` with rewind guards.
- [x] Authoring tooling: `PresentationRegistryAsset` + baker provide registry blobs consumed by `PresentationBootstrapSystem`.
- [x] Architecture doc: `Docs/PresentationBridgeArchitecture.md` (formerly `PresentationBridges.md`) captures high-level responsibilities and data flow.

## Milestone A – Registry & Prefab Alignment
- [ ] Produce per-game registry assets (Godgame + Space4X) listing all required descriptors, keys, prefabs, and default flags; document the keys in `Docs/PresentationBridgeArchitecture.md`.
- [ ] Add CI check or editor validation that every `PresentationKey` baked into simulation entities resolves to a descriptor (no orphan keys).
- [ ] Extend `PresentationRegistryAuthoring` so games can merge multiple registry assets (core + project overrides) without manual code changes.
- [ ] Document prefab authoring guidelines (mesh requirements, material rules, pooling flags) alongside screenshots for reference.

- [x] Implement `Godgame.VillagerPresentationAdapterSystem` that enqueues spawn/recycle requests based on villager lifecycle events and stores the assigned descriptor hash.
- [ ] Implement `Godgame.StructurePresentationAdapterSystem` for storehouses, miracles, and construction sites (handles progress-based tinting).
- [x] Implement `Space4X.CrewAggregationSystem` + `Space4X.CrewPresentationAdapterSystem` to mirror crew aggregates, average stats, and duty states for presentation.
- [ ] Implement `Space4X.FleetPresentationAdapterSystem` to mirror carriers/mining vessels using registry snapshots rather than Mono behaviours.
- [ ] Implement `Space4X.ColonyPresentationAdapterSystem` for colonies/logistics routes/anomalies (use descriptor variants for posture/severity).
- [ ] Update docs with adapter responsibilities and sample code snippets so future domains can follow the same pattern.
- [ ] Hook crew aggregate creation to workforce/military assignment logic (crews spawn when non-officer workers join departments; destroy/recycle when departments empty) and ensure `AggregateEntity` category metadata stays in sync across fleets/colonies/guilds/businesses.

## Milestone C – Presentation Sync & Pooling
- [x] Create a shared `PresentationHandleSyncSystem` that copies authoritative transforms into companion visuals each frame (configurable interpolation, optional offset curves).
- [x] Add pooling metrics + configuration (`PresentationPoolStats`, `presentation.pool.*` counters) and expose them through Debug HUD + console.
- [x] Provide a lightweight Mono/Entities Graphics bridge sample that demonstrates swapping materials, playing Animator states, and binding VFX graphs via `PresentationHandle`.
- [x] Add a `PresentationReloadCommand` (console or menu item) that flushes visuals and respawns them from handles for debugging.

## Milestone D – Testing & Observability
- [ ] Expand `PresentationBridgeRewindTests` to cover spawn → recycle → respawn under Record/Playback/CatchUp modes and detect leaked handles.
- [ ] Add stress test scene (50k presentation entities) to profile pooling + transform sync with Entities Graphics enabled.
- [ ] Hook presentation stats into `Docs/Progress.md` + dashboards so designers can see active visual counts per domain.
- [ ] Document UI/HUD integration steps (mapping registry snapshots to overlays) and ensure telemetry topics exist for villager/colony visuals.

## Open Questions
- Should Space4X maintain a dedicated presentation world for orbit-scale visuals, or can we stay in the main world with LOD gating?
- Do we need event recording for visuals (Option 2 in the architecture doc) before we ship cinematic replays, or is regeneration acceptable for now?
- What is the minimum feature set for designers (e.g., gizmos, spawn preview, registry inspector) before handing off the bridge?

Track progress in this file and keep `Docs/PresentationBridgeArchitecture.md` updated whenever new adapters or registry rules are introduced.
