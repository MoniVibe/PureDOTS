# PureDOTS Roadmap Status

_Last updated: 2025-11-04_

This file tracks progress against the roadmap in `Docs/ROADMAP.md`. Update it at the end of each pass to keep the team aligned.

## Status Legend
- ✅ Done – work landed and validated
- 🔄 In Progress – partially delivered or actively underway
- ⏳ Pending – not started / waiting on prerequisites
- ⚠️ At Risk – blocked or needs attention

## Phase 1 – Spatial & Rebuild Foundations
| Task | Status | Notes |
| --- | --- | --- |
| Partial rebuild pipeline (dirty ops, metrics, tests) | 🔄 In Progress | Diff-based loader + bounded concurrency landed; soak harness & CI metrics still outstanding |
| Spatial metadata consumers beyond villagers | ✅ Done | Registry + AI sensors wired; spatial query helpers available for all systems |
| Spatial telemetry & docs (rebuild strategy/timing) | ✅ Done | HUD surfaces rebuild strategy/timing via DebugDisplayReader spatial telemetry |
| `ISpatialGridProvider` abstraction & validation | ⏳ Pending | Hashed grid still hard-wired |
| Spatial query helper API | ✅ Done | `GetEntitiesWithinRadius`, `FindNearestEntity`, `GetCellEntities`, `OverlapAABB` implemented |

## Phase 2 – Registries & Logistics
| Task | Status | Notes |
| --- | --- | --- |
| Additional domain registries (transport, miracles, construction, villager) | ✅ Done | Core registries live with continuity snapshots; construction registry available for adapters |
| Registry helper adoption across consumers | ✅ Done | Registry query helpers (`RegistryQueryHelpers`) available; systems can query registries from Burst jobs |
| Registry health instrumentation | ✅ Done | Health system emits stale/spatial alerts with HUD + telemetry coverage |
| Registry-spatial continuity contracts | ✅ Done | Contract spec drafted + metadata/sync scaffolding landed; deterministic rebuild strategy documented |
| Registry spawn/despawn handling | ✅ Done | Deterministic rebuild-every-frame strategy implemented; documented in `Docs/DesignNotes/RegistryLifecycle.md` |
| Meta registry authoring/tests/telemetry | ✅ Done | Phase 2 completion: authoring + profiles, integration tests, HUD/telemetry counters, soak harness |

## Phase 3 – Environment & Terrain Cadence
| Task | Status | Notes |
| --- | --- | --- |
| Shared environment grids & sampling helpers | ✅ Done | All grids (moisture, temperature, wind, sunlight, biome) with sampling helpers via `EnvironmentSampling` |
| Biome/terrain version integration | ✅ Done | `BiomeDerivationSystem` runs; terrain version propagates to all environment grids (`MoistureEvaporationSystem`, `MoistureSeepageSystem`, `BiomeDerivationSystem`, `EnvironmentEffectUpdateSystem`) |
| Environment-dependent systems on shared cadence | ✅ Done | `MoistureRainSystem` integrates rain miracles with moisture grid; resource systems can sample environment data; debug overlay exposes environment telemetry |

## Phase 4 – Rewind & Time Determinism
| Task | Status | Notes |
| --- | --- | --- |
| Guard systems for every group | ✅ Done | All system groups guarded: Environment, Spatial, Gameplay, CameraInput, Hand, Presentation. `RewindTelemetrySystem` tracks violations. |
| Deterministic tests (gather/delivery, deposit/withdraw, AI transitions w/ partial rebuilds) | ✅ Done | `DeterministicRewindTestFixture` + `DeterministicRewindFlowTests` provide harness for record/replay validation. Tests cover gather/delivery, deposit/withdraw, AI transitions, partial rebuilds. |
| Rewind-friendly state surface for spatial grid | ✅ Done | `SpatialGridSnapshot` and `SpatialGridBufferSnapshot` contracts defined. `SpatialGridSnapshotSystem` captures snapshots. Validation tests verify restore functionality. |

## Phase 5 – Input & Hand Framework
| Task | Status | Notes |
| --- | --- | --- |
| Centralized hand/router state machine | ✅ Done | Input snapshot infrastructure with edge buffers, intent mapping system (`IntentMappingSystem`), and deterministic router (`HandInputRouterSystem` consuming `GodIntent`) implemented. Mono bridge (`InputSnapshotBridge`) accumulates input per frame, flushes per DOTS tick. |
| Interaction tests (priorities, cooldowns, spatial metadata) | ✅ Done | `InputEdgeTests` validates edge events, intent mapping, and UI blocking. Recording/playback systems (`InputRecordingSystem`, `InputPlaybackSystem`) enable deterministic repro. Single-writer state systems (`DivineHandSystem`, `CameraSystem`) with multi-hand support via `PlayerId`. |

## Phase 6 – Tooling & Observability
| Task | Status | Notes |
| --- | --- | --- |
| Debug HUD/console enhancements | 🔄 In Progress | Spatial rebuild telemetry live; streaming cooldown debug UX still pending |
| Editor tooling (validators, visualizers) | 🔄 In Progress | Streaming validator shipped; spatial authoring validators outstanding |
| Telemetry pipeline hardening | ⏳ Pending | Structured snapshots/rolling averages TBD |

## Phase 7 - CI & Template Packaging
| Task | Status | Notes |
| --- | --- | --- |
| Automated playmode/editor suites | ⏳ Pending | Relying on external game scenes for validation |
| Reference scenes (theme agnostic) | ⏳ Pending | Needs template demo scenes |
| Template usage guide | 🔄 In Progress | Draft consumer workflow doc underway |
| Package distribution (UPM/git) | 🔄 In Progress | package.json present; publication flow still manual |
| High-scale soak & threshold tuning harness | ⏳ Pending | No perf harness yet |

## Phase 8 – Stabilization Gate
| Task | Status | Notes |
| --- | --- | --- |
| All template tests green in CI | ⏳ Pending | CI not configured |
| Documentation cross-linked & complete | 🔄 In Progress | Roadmap + registry docs updated; other sections outstanding |
| No outstanding template TODOs | ⏳ Pending | Multiple TODOs remain |
| Template declared ready for game-layer work | ⏳ Pending | Foundation still under development |

---

**Update Procedure**
1. After completing a slice, revisit this file.
2. Adjust status icons and notes based on the latest work.
3. Commit the changes alongside the relevant implementation.
4. Mention the update in your hand-off summary.

