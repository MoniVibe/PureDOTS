# PureDOTS Roadmap Status

_Last updated: 2025-10-26_

This file tracks progress against the roadmap in `Docs/ROADMAP.md`. Update it at the end of each pass to keep the team aligned.

## Status Legend
- ✅ Done – work landed and validated
- 🔄 In Progress – partially delivered or actively underway
- ⏳ Pending – not started / waiting on prerequisites
- ⚠️ At Risk – blocked or needs attention

## Phase 1 – Spatial & Rebuild Foundations
| Task | Status | Notes |
| --- | --- | --- |
| Partial rebuild pipeline (dirty ops, metrics, tests) | 🔄 In Progress | Partial rebuild implementation merged; soak tests & CI validation still outstanding |
| Spatial metadata consumers beyond villagers | 🔄 In Progress | Villager registry, AI sensors, and transport registries now emit cell/version data; miracle logic still pending |
| Spatial telemetry & docs (rebuild strategy/timing) | ✅ Done | HUD surfaces rebuild strategy/timing via DebugDisplayReader spatial telemetry |
| `ISpatialGridProvider` abstraction & validation | ⏳ Pending | Hashed grid still hard-wired |

## Phase 2 – Registries & Logistics
| Task | Status | Notes |
| --- | --- | --- |
| Additional domain registries (transport, miracles, construction, villager) | In Progress | Villager + transport registries upgraded; miracle & logistics registries landed; construction registries pending |
| Registry helper adoption across consumers | In Progress | Villager loops migrated; other domains outstanding |
| Registry health instrumentation | Done | Health system emits stale/spatial alerts with HUD + telemetry coverage |
| Registry-spatial continuity contracts | In Progress | Contract spec drafted + metadata/sync scaffolding landed; health enforcement wired, regression tests pending |

## Phase 3 – Environment & Terrain Cadence
| Task | Status | Notes |
| --- | --- | --- |
| Shared environment grids & sampling helpers | ⏳ Pending | Framework defined in docs, implementation not started |
| Biome/terrain version integration | ⏳ Pending | Awaiting environment grid rollout |
| Environment-dependent systems on shared cadence | ⏳ Pending | Vegetation/miracles still reference bespoke flows |

## Phase 4 – Rewind & Time Determinism
| Task | Status | Notes |
| --- | --- | --- |
| Guard systems for every group | ⏳ Pending | Core guards exist; registry/spatial guards need verification |
| Deterministic tests (gather/delivery, deposit/withdraw, AI transitions w/ partial rebuilds) | ⏳ Pending | Only base villager loop covered |
| Rewind-friendly state surface for spatial grid | ⏳ Pending | Snapshot/diff contract not defined |

## Phase 5 – Input & Hand Framework
| Task | Status | Notes |
| --- | --- | --- |
| Centralized hand/router state machine | ⏳ Pending | Current logic remains TBD |
| Interaction tests (priorities, cooldowns, spatial metadata) | ⏳ Pending | Depends on router implementation |

## Phase 6 – Tooling & Observability
| Task | Status | Notes |
| --- | --- | --- |
| Debug HUD/console enhancements | 🔄 In Progress | Spatial rebuild telemetry now live; streaming cooldown tooling still outstanding |
| Editor tooling (validators, visualizers) | ⏳ Pending | Spatial/authoring validators not yet built |
| Telemetry pipeline hardening | ⏳ Pending | Structured snapshots/rolling averages TBD |

## Phase 7 � CI & Template Packaging
| Task | Status | Notes |
| --- | --- | --- |
| Automated playmode/editor suites | Pending | Manual runs only |
| Reference scenes (theme agnostic) | Pending | Needs template demo scenes |
| Template usage guide | Pending | To write once features stabilize |
| Package distribution (UPM/git) | In Progress | Package.json + consumer manifest guidance drafted; per-game integration still in flight |
| High-scale soak & threshold tuning harness | Pending | No perf harness yet |

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

