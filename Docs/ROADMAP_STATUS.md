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
| Spatial metadata consumers beyond villagers | 🔄 In Progress | Registry + AI sensors wired; miracles/logistics adapters pending adoption of spatial residency |
| Spatial telemetry & docs (rebuild strategy/timing) | ✅ Done | HUD surfaces rebuild strategy/timing via DebugDisplayReader spatial telemetry |
| `ISpatialGridProvider` abstraction & validation | ⏳ Pending | Hashed grid still hard-wired |

## Phase 2 – Registries & Logistics
| Task | Status | Notes |
| --- | --- | --- |
| Additional domain registries (transport, miracles, construction, villager) | ✅ Done | Core registries live with continuity snapshots; construction registry available for adapters |
| Registry helper adoption across consumers | 🔄 In Progress | Villager loops migrated; miracles/logistics still reading buffers directly |
| Registry health instrumentation | ✅ Done | Health system emits stale/spatial alerts with HUD + telemetry coverage |
| Registry-spatial continuity contracts | 🔄 In Progress | Contract spec drafted + metadata/sync scaffolding landed; regression tests pending |

## Phase 3 – Environment & Terrain Cadence
| Task | Status | Notes |
| --- | --- | --- |
| Shared environment grids & sampling helpers | 🔄 In Progress | Climate + moisture grids live; temperature/wind sampling helpers still sparse |
| Biome/terrain version integration | ⏳ Pending | Awaiting environment grid rollout |
| Environment-dependent systems on shared cadence | 🔄 In Progress | Vegetation loops hooked; miracles/resources pending cadence alignment |

## Phase 4 – Rewind & Time Determinism
| Task | Status | Notes |
| --- | --- | --- |
| Guard systems for every group | ⏳ Pending | Core guards exist; spatial/environment groups need verification |
| Deterministic tests (gather/delivery, deposit/withdraw, AI transitions w/ partial rebuilds) | ⏳ Pending | External game scenes currently cover flows; dedicated harness outstanding |
| Rewind-friendly state surface for spatial grid | ⏳ Pending | Snapshot/diff contract not defined |

## Phase 5 – Input & Hand Framework
| Task | Status | Notes |
| --- | --- | --- |
| Centralized hand/router state machine | ⏳ Pending | Current logic remains TBD |
| Interaction tests (priorities, cooldowns, spatial metadata) | ⏳ Pending | Depends on router implementation |

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

