# DOTS Registry Rewrite TODO

## Goal
- Replace legacy service/locator patterns with deterministic DOTS registries (resources, storehouses, villagers, construction, miracles) using buffers and blobs only.
- Provide efficient SoA-style accessors and pooled containers to minimize per-frame allocations.
- Align registry contracts with `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and cross-domain expectations in `Docs/TODO/SystemIntegration_TODO.md`.

## Plain-Language Primer
- **Registry** = a single entity that holds a list (buffer) giving us “all of X” (e.g., all storehouses) so other systems can read it quickly without searching the world.
- **DOTS rules**: we avoid `GameObject.Find`, we keep data in components/structs, and we run jobs that prefer contiguous “Structure of Arrays” (SoA) data.
- **Our task**: create those central lists the DOTS way, keep them updated when entities spawn/despawn, and make them easy for Burst/Jobs to read. Think of it as maintaining a spreadsheet for each domain.

## Alignment
- Delivers on PureDOTS_TODO section 4 and helps realize the “Service/Registry Replacement” pillar in the reboot.
- Supports systems that depend on fast lookups (job assignment, AI, presentation bridges).
- Registries supply spatial/environment-driven systems; ensure entries cache environment grid cell indices and honour shared rebuild cadence.

## Workstreams & Tasks

### 1. Registry Blueprint (Data Layout)
- [x] Sketch each registry as "one singleton entity + buffer". Example: `ResourceRegistry` entity with `DynamicBuffer<ResourceRegistryEntry>` (delivered via `DeterministicRegistryBuilder`).
- [x] Decide per registry what fields we need (position, type index, capacity, reservations, etc.) in SoA-friendly structs.
- [x] Add optional `BlobAssetReference` tables for lookup by string ID (e.g., map resource type name to ushort index). (Implemented via `ResourceTypeIndexBlob` with `LookupIndex` method)
- [x] Document hot (frequently updated) vs. cold (rare fields) splits to keep main chunks lean. (See `Docs/DesignNotes/RegistryHotColdSplits.md`)
- [x] Finalise theme-agnostic registry schemas for villagers, transport units, miracles, and construction sites so game-layer code only supplies intent/resources (see `Docs/DesignNotes/RegistryDomainPlan.md`).
- [x] Document standard registry entry schema and shared utilities rollout plan. (See "Standard Registry Schema" section below)

### 2. Update Systems
- [x] Build lightweight systems that populate/refresh each registry at a controlled point in the frame (e.g., `ResourceRegistrySystem` running before villager jobs) using `DeterministicRegistryBuilder`.
- [x] Create global registry directory so shared systems can resolve handles generically (`RegistryDirectorySystem` + buffer helpers).
- [x] Update spatial grid rebuild to read from directory, ensuring registry handles remain consistent even as domains expand.
- [x] Provide shared lookup utilities (`RegistryDirectoryLookup`) so systems fetch registry entities/buffers by kind without domain-specific knowledge.
- [x] Add runtime continuity validation (`RegistryContinuityValidationSystem`) to flag spatial drift and surface alerts for consumers.
- [x] Logistics request registry system (`LogisticsRequestRegistrySystem`) rebuilds request entries with continuity snapshots.
- [x] Construction registry system exposes build sites with shared continuity + instrumentation hooks.
- [x] Creature/threat registry system keeps environmental entities discoverable across domains.
- [x] Band/squad registry system surfaces formation data for AI/pathfinding consumers.
- [x] Ability registry system enumerates player-triggered actions for miracle/ability layers.
- [x] Spawner registry system exposes shared spawn pads for villagers/fauna/ships with continuity + instrumentation.
- [x] Handle spawn/despawn: when entities arise or die, ensure registry entries are inserted/removed deterministically (use ECB and predictable sorting). (Implemented via deterministic rebuild every frame from queries; entities matching query are included/excluded automatically - see `Docs/DesignNotes/RegistryLifecycle.md`)
- [x] Integrate with rewind: either rebuild registries every frame from authoritative components or record minimal history to reapply on playback. (Rebuild-every-frame strategy implemented; registries rebuild from queries deterministically, skip during playback as state is already deterministic - see `Docs/DesignNotes/RegistryLifecycle.md`)
- [x] Provide helper static methods (or extension structs) so other systems can query registries from Burst without copying data. (Implemented in `RegistryQueryHelpers` - methods accept `NativeArray<TEntry>` which can be obtained from buffers via `AsNativeArray()` without copying; `RegistryEntryLookup.TryFindEntryIndex` provides binary search lookup)

### 3. Memory & Pooling Utilities
- [x] Implement pooled `NativeList<T>` / `NativeQueue<T>` wrappers for temporary per-system scratch use (ties into SoA utilities). (See `Runtime/Pooling/NativeContainerPools.cs`)
- [x] Introduce a "command buffer pool" or reuse existing ECB systems to avoid repeated allocations. (Implemented in `NxPoolingService` with ECB pooling)
- [x] Ensure all pooling utilities dispose correctly on world shutdown and respect determinism. (Pooling services dispose correctly; determinism maintained)

### 4. Migration of Existing Systems
- [ ] Resource gathering/storehouse systems: switch lookups to the new registries instead of ad-hoc queries.
- [ ] Villager job assignment: consume registry instead of scanning every frame; share reservation data via registry entries.
- [ ] Rain miracles/divine hand: use registries to quickly find nearest valid targets once spatial grid is ready (future integration point).
- [ ] Remove or obsolete any legacy service singletons/MonoBehaviours that duplicate registry responsibilities.
- [ ] Stand up generic consumption samples/tests so downstream games can plug domain-specific logic into PureDOTS registries without modifying template code.

### 5. Tooling & Docs
- [ ] Add inspector gizmos or debug HUD panels so designers can view registry contents at runtime (counts, capacities).
- [ ] Extend authoring docs (`SceneSetup`, `DesignNotes`) with “how to configure registries” (e.g., required components on prefabs).
- [ ] Update `Docs/Progress.md` when each registry migrates; link to relevant truth sources.
- [x] Introduce console instrumentation toggle (`RegistryConsoleInstrumentation`) for headless logging of registry snapshots.
- [x] Emit registry instrumentation buffers (`RegistryInstrumentationSystem`) so debug HUDs and telemetry can read per-registry health metrics.

### 6. Testing & Validation
- [ ] Unit tests for each registry update system (spawn, despawn, reorder, rewind).
- [ ] Playmode tests ensuring villagers/resources behave identically before/after migration.
- [ ] Playmode `RegistryContinuity` suite covering villager, miracle, transport, and logistics request registries (spatial sync + rewind).
 - [x] Playmode coverage for miracle registry aggregates (`MiracleRegistryTests`).
- [ ] Stress tests with 50k entities verifying no GC allocations and acceptable frame time.
- [ ] Regression checks for deterministic ordering (sorted indexes stable between runs).
- [x] Playmode smoke test for console instrumentation output (`ResourceRegistry_ConsoleInstrumentation_LogsSummary`).

## Standard Registry Schema

**Current State**: Each registry uses domain-specific entry structs (`ResourceRegistryEntry`, `VillagerRegistryEntry`, etc.) but follows consistent patterns documented in `RegistryHotColdSplits.md`.

**Proposed Standard Base** (Future Refactor):
- Consider introducing `IRegistryEntry` interface with common fields (`Entity`, `float3 Position`, `int CellId`, `uint SpatialVersion`, `uint LastMutationTick`)
- Domain-specific entries embed or extend base pattern
- Shared utilities (`RegistryQueryHelpers`) work with `IRegistryEntry` for generic queries

**Rollout Plan**:
1. **Phase 1** (Current): Domain-specific entries with consistent hot/cold splits ✅
2. **Phase 2** (Future): Extract common base interface, migrate existing entries gradually
3. **Phase 3** (Future): Consolidate shared utilities (`RegistryCommon.cs`) if duplication emerges

**Reference**: See `Docs/DesignNotes/RegistryHotColdSplits.md` for current layout patterns and `Docs/DesignNotes/SoA_Expectations.md` for SoA guidelines.

## Open Questions
- Which registries require historical data for rewind vs. live-only caches?
- How to expose registry queries to Burst jobs safely (read-only vs. RW access)?
- Can we share reservation/ticket systems across domains without coupling?

## Next Steps
- Inventory current registry usage to identify gaps and duplication (audit existing systems).
- Draft a mini design doc summarizing chosen layouts and update order (tie into `Docs/DesignNotes`).
- Once approved, break down per-registry tasks (Resources, Storehouses, Villagers, Miracles, Construction) and track progress here.
