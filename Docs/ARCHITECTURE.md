# Architecture Contracts

This document defines the runtime contracts shared by PureDOTS, Space4X, and Godgame.

## 1) Runtime Boundary
- **PureDOTS owns core contracts**: deterministic tick flow, rewind/time spine, registry/scenario primitives, headless proof surfaces.
- **Game projects own domain logic**: Space4X and Godgame systems, authoring, and presentation layers.
- **No contract bypasses**: game-specific code must not fork core time truth or headless reporting conventions.

## 2) Determinism Contract
- Fixed-step simulation is authoritative.
- No wall-clock/system-time branching in simulation logic.
- Randomness is seeded and replay-safe.
- Structural changes are explicit and frame-bounded (ECB/group ordering discipline).
- Same build + same scenario + same inputs must produce equivalent invariant-level outcomes.

## 3) Time/Rewind Contract
- Single time spine controls progression, pause, playback, and catch-up behavior.
- Rewind support is not optional in core systems that participate in demo/proof flows.
- State transitions across tick phases must be explicit and testable.
- Presentation may interpolate, but may not alter simulation state.

## 4) Headless Gate Contract
Headless is the acceptance authority. A qualifying run must emit and preserve:
- `meta.json`
- `run_summary_min.json` and `run_summary.json`
- `watchdog.json`
- `invariants.json`
- relevant log tails needed for triage

Any "looks fine in scene" result without artifact proof is non-accepting.

## 5) Debuggability By Humans
Every major slice must be diagnosable through:
- telemetry artifacts
- Entities windows: **Archetypes**, **Systems**, **Journaling**

Entities Journaling is part of the standard path, including ECB action provenance (origin system visibility).

## 6) Operational Guardrails (Recurring)
- Rebuild pins must exist in all requested repos for multi-repo runs (or use shared reachable refs).
- Keep telemetry defaults bounded in headless contexts (summary-first) to avoid proof-obscuring bloat.
- Do not "fix" already-documented recurring errors by reintroducing known bad patterns.
