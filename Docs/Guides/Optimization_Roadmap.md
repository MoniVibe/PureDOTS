# Optimization Implementation Roadmap (PureDOTS)

**Goal:** Deliver the advertised optimizations incrementally with working, profiled code that underpin the demos. Four agents work in parallel on clearly scoped foundations.

## Agent Tracks (parallel)
- **Agent A — Event/Domain Scheduling**
  - Make `EventQueue` real (payload struct, bounded `NativeQueue`, enqueue/dequeue API, drain once per tick).
  - Add change-filters/periodic ticks to 2–3 hot systems (pick real ones).
  - Implement `TickDomainCoordinatorSystem` to gate `CognitiveSystemGroup`/`EconomySystemGroup` via integer ratios; sync with `TickTimeState`.
  - Telemetry: events processed, skipped chunks, domain ticks executed vs skipped.

- **Agent B — Temporal Cache**
  - Implement `ResultCache<T>` (ring + hash + dispose + `CombineHashes`).
  - ✔ Implemented `ResultCache<T>` (ring + hash + dispose) in `Runtime/Caching/ResultCache.cs`.
  - ✔ Wired `CacheKey/CacheStats` into `PerceptionUpdateSystem` (hash over detectables + sensor transform/range/channels; short-circuits unchanged frames; stats tracked).
  - TODO: Add invalidation-on-input-change for another consumer (pathfinding or perception fusion) and validate under rewind with tests/telemetry.
  - Telemetry: cache lookups/hits/misses/hit-rate (via `CacheStats`).

- **Agent C — Streaming Cells**
  - Implement activate/deactivate by camera/player AABB; maintain `SimulationCell` + `CellAgentBuffer`.
  - ✔ In-memory deterministic “serialization”: disable/enable agents per cell; track `CellStreamingState`.
  - Telemetry: `CellStreamingMetrics` (active/serialized cells & agents; bytes placeholder).
  - Next: replace `Disabled` toggling with real EntityScene-backed snapshotting; add memory accounting; integrate with camera/player updater for `CellStreamingWindow`.

- **Agent D — Telemetry + Authoring Safety + Integration Glue**
  - ✔ Telemetry hub (main-thread + ParallelWriter) and aggregator now drain into `TelemetryStream`; `LocalTelemetryBuffer` hardened (creation checks, clear/count, ParallelWriter); `SystemMetricsCollector` metric keys normalized; `BlobAssetJsonConverter` no longer throws (returns default blob with warning, `{}` JSON).
  - ✔ Telemetry key reference added (`Docs/Guides/Telemetry_Key_Reference.md`) for standardized keys.
  - ✔ Aligned WorldMetricsCollectorSystem, ModuleTelemetrySystem, AffiliationComplianceSystem, ChronoProfilingSystem, PresentationPoolTelemetrySystem, SocialPerformanceProfiler, CarrierModuleTelemetrySystem, CameraRigTelemetrySystem (legacy), and SpawnTelemetrySystem (devtools) to emit via TelemetryHub with standard keys.
  - Next: Continue aligning remaining emitters (if any) to the key reference via TelemetryHub; audit producers for allocator/disposal hygiene; migrate ad-hoc emitters to the hub where appropriate. Optional smoke validation of aggregator/hub disposal; extend blob serialization when schemas are ready.
  - Later: Optional smoke validation of aggregator/hub disposal; extend blob serialization when schemas are ready; remove stub guards for telemetry only when all producers are aligned.

## Phasing (shared milestones)
- **Phase 0 — Safety & Transparency (now)**: stubs stay disabled; guides note status.
- **Phase 1 — Foundations**: Agent A, B, C, D ship MVPs with telemetry; each feature must be deterministic, Burst-safe, and disposed correctly.
- **Phase 2 — Demo Wiring**: After Phase 1 lands, wire smallest demos that prove each feature (event-driven skips, cache hit-rate, cell streaming enter/leave, telemetry dashboard).
- **Phase 3 — Optional higher-order (after stability)**: meta-scheduler, genetic balancer, network replication only after metrics and foundations are solid.

## Definition of Done (per feature)
- Measurable perf/behavior impact (telemetry shows the effect).
- Deterministic & Burst-safe; no managed allocs in hot paths.
- Allocator hygiene verified; disposals covered.
- Tests or repeatable repro steps documented.
- Guides updated; stub guards removed for that feature only.

## Guardrails
- No new systems without telemetry and disposal plans.
- Keep `Allocator.Persistent` only when owned/disposed by a system lifecycle.
- Ship incrementally: one feature at a time, validated before moving on.

## Progress Log (Agent C)
- Streaming pipeline live with in-memory deterministic toggling: `CellStreamingSystem` activates/deactivates by window; `CellSerializationSystem` disables/enables agents per cell and tracks `CellStreamingState`.
 - Window updater: `CellStreamingWindowUpdateSystem` copies from `CellStreamingWindowTarget` into `CellStreamingWindow` each tick.
- Telemetry: `CellStreamingMetrics` reports active/serialized cells and agents; approx bytes derived from `EstimatedAgentBytes` in `CellStreamingConfig`.
- Configs: `CellStreamingConfig` (CellSize, Hysteresis, EstimatedAgentBytes), `CellStreamingWindow`, `CellStreamingWindowTarget` for feeding camera/player position.
- Next: replace `Disabled` toggling with EntityScene-backed snapshotting; add real memory accounting; keep `CellAgentBuffer` in sync with agent add/remove; wire a minimal demo and optional gizmo/debug draw; integrate window updater with actual camera/player feed.

## Progress Log (Agent A)
- EventQueue implemented: payload buffer, enqueue helper with capacity guard, processed/dropped counters, per-tick reset. Systems can now enqueue/consume events via the singleton queue/buffer.
- Tick-domain gating implemented: `TickDomainCoordinatorSystem` creates cognitive/economy domains, advances `NextTick` by integer ratios, and enables/disables `CognitiveSystemGroup`/`EconomySystemGroup` deterministically.
- Change-filter tightening: `FormationCommandSystem` and `GroupMoraleSystem` now use `WithChangeFilter`; `GroupMoraleSystem` emits bounded morale events to the queue (`EventType.MoraleChange`).
- ResourceProcessingSystem and ResourceGatheringSystem now respect tick gating and throttle via `PeriodicTickComponent` (stride=5) with auto-add for processors/gatherers; creates periodic components when missing.
- Telemetry hooks added: WorldMetricsCollectorSystem now emits event queue metrics (buffer len, processed, dropped) and tick-domain metrics (ticks until next cognitive/economy execution). Debug consumer added (`EventQueueDebugConsumerSystem`) to validate event flow (disabled by default).
- Event consumer telemetry added: WorldMetricsCollectorSystem reports event.consumed and event.consumed_last_tick from EventQueueConsumerStats.
- AQL execution: parser captures entity type + simple WHERE; executor fills AQLResultElement buffers by matching AQLTag.Name; conditions are parsed but currently not evaluated.

## Next Actions (Agent A)
- Wire a consumer example for EventQueue (e.g., a lightweight logger or handler) to validate end-to-end event flow.
- Identify and throttle 1–2 additional hot systems with `PeriodicTickComponent`/`WithChangeFilter` to demonstrate skip gains (VillagerAISystem throttling is in via domain gating; resource processing and gathering now throttled; consider one more candidate if needed).

## Progress Log (Agent B)
- ResultCache implemented at Runtime/Caching/ResultCache.cs (ring + hash + dispose).
- Perception caching: PerceptionUpdateSystem hashes detectables + transform/range/channels; short-circuits unchanged frames; stats recorded in CacheStats.
- Pathfinding caching: PathfindingSystem hashes start/goal/locomotion/positions; skips recompute on hit; stats recorded in CacheStats; PathCacheState introduced to track invalidation.
- Graph mutation hook: NavGraphStateUpdateSystem marks PathCacheState.Dirty when edge states expire; cache clears on next path tick. Cache stats now emitted via WorldMetricsCollectorSystem for telemetry (lookups/hits/misses/hit_rate).

## Next Actions (Agent B)
- Add explicit invalidation on all graph mutations (node/edge edits, rebuilds) and validate under rewind with tests/telemetry.
- Expose CacheStats (lookups/hits/misses/hit-rate) via telemetry overlay/dashboard.
- Tune cache capacity or split per-system if profiling shows collisions/evictions.
