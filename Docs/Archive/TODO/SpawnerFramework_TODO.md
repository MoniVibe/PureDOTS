# Spawner Framework TODO (Shared Entity Lifecycle)

> **Generalisation Guideline**: The spawn/despawn pipeline must work for any entity type (villagers, ships, miracles, projectiles) and rely on data/config rather than game-specific logic.

## Goal
- Provide a reusable, deterministic entity lifecycle framework that covers pooling, spawn requests, despawn cleanup, and rewind integration.
- Ensure systems produce/consume spawn commands through shared utilities so future projects can drop in new entity archetypes without rewriting plumbing.

## Workstreams & Tasks

### 0. Requirements & Contracts
- [ ] Audit existing spawn/despawn patterns (resources, miracles, vegetation, rain clouds) and document common needs.
- [ ] Define `SpawnRequest`/`DespawnRequest` component or command buffer schema with deterministic ordering guarantees.
- [ ] Determine pooling requirements per archetype (chunks, tokens, vehicles, saplings, FX) and record configuration fields.

### 1. Shared Pooling Utilities
- [x] Implement pooled `Entity` factories with configurable capacities and autorefill/expansion policies.
- [x] Provide pooled command buffers / NativeList wrappers for spawning and despawning so systems avoid per-frame allocations.
- [x] Add diagnostics for pool utilisation and leaks (hooks for tooling agent).
- [x] Ensure rewind/catch-up clears or rewinds pools deterministically.
- [x] Expose pooled config via shared runtime settings (`PoolingSettingsConfig` baked from `PoolingSettingsData` in `PureDotsRuntimeConfig`).
- [x] Stand up `Nx.Pooling` service singleton managing Native container pools, entity pools, and ECB pools with deterministic ordering.
- [x] Document lifetime expectations (borrow → use within tick → return) and provide helper wrappers enforcing disposal in jobs.

### 2. Spawn Command Processing
- [x] Create `SpawnRequestSystem` and `DespawnRequestSystem` that read pooled requests, instantiate entities (from prefabs or pooled instances), and apply initial components.
- [x] Support data-driven archetype selection via `SpawnProfile` blobs (authoring + baker).
- [x] Provide API for scheduled/desynced spawns (e.g., spawn after delay, spawn in batches) without custom game code.
- [x] Integrate with registries so new entities automatically register/deregister when spawned/despawned.
- [x] Define deterministic spawn/despawn policy document (command ordering, deferred release rules, rewind reset) and link in SystemIntegration/Utilities TODOs.

### 3. Integration with Existing Systems
- [x] Refactor resources, miracles, vegetation, logistics, and environment effects to use the shared spawn framework (no manual `EntityManager.Instantiate` calls). `[Runtime Core]`
- [x] Ensure AI modules issue spawn/despawn commands via shared utilities (e.g., spawning workers, drones, projectiles). `[Runtime Core]`
- [x] Update pooling-related TODOs in subsystem documents to reference the new framework). `[Documentation]`
- [x] Replace ad-hoc `NativeQueue`/`NativeList` allocations in Vegetation reproduction/decay, resource chunk spawning, miracle emitters with pooled equivalents). `[Runtime Core]`
- [ ] Ensure shared spawn utilities expose theme-neutral configuration hooks so new content modules (e.g., asteroid crystals, wildlife) can adopt the pipeline without code changes. `[Runtime Core + Data Authoring]`

### 4. Testing & Tooling
- [x] Add spawn/despawn stress tests covering pooled vs. non-pooled paths, rewind cycles, and deterministic ordering.
- [x] Extend integration tests to validate spawn requests from different systems (resources, miracles, AI) share the pipeline correctly.
- [x] Surface spawn statistics (spawn rate, pool usage, failures) for debug overlay and telemetry.

### 5. Documentation & Authoring Workflow
- [x] Document spawn profiles, pooling configuration, and best practices in `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md` or a dedicated authoring guide.
- [x] Update `GettingStarted.md` and adoption README with instructions for using the spawn framework.
- [x] Link `SystemIntegration_TODO.md` and `Utilities_TODO.md` to this document for cross-reference.

## Dependencies & Links
- `Docs/TODO/SystemIntegration_TODO.md`
- `Docs/TODO/Utilities_TODO.md`
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`
- `Docs/TruthSources/PlatformPerformance_TruthSource.md`

## Success Criteria
- All entity types (current and future) use the shared spawn/despawn pipeline.
- Pools eliminate per-frame allocations; telemetry confirms deterministic behaviour and zero leaks.
- Rewind restores spawn state correctly.
- Designers can configure spawn behaviour via data without touching code.

Update this TODO as the spawn framework evolves and new requirements emerge.
