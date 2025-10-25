# Presentation & Companion Bridges TODO

## Goal
- Define a pure DOTS-friendly presentation strategy (hot/cold archetypes, companion entities, UI bridges) that respects rewind and keeps Mono glue minimal.
- Prepare the template for future visual polish without blocking current simulation work.

## Alignment
- Maps to PureDOTS vision pillars: “Presentation Bridges” and “Observability & Automation”.
- Supports SceneSetup guidelines and upcoming content expansion (creature, city visuals, HUD).

## Workstreams (stub)
- Archetype design: document hot vs. cold component splits, companion entities, and conversion workflows.
- Companion systems: implement sync jobs that feed render meshes, VFX, and UI without breaking determinism.
- Debug/observability: extend HUD, gizmo overlays, timeline widgets using DOTS data-only pathways.
- Authoring tooling: inspector helpers, validation scripts, sample scene illustrating best practices.
- Testing: ensure presentation bridges tolerate rewind (playback/catch-up) and handle large entity counts.

## Open Questions
- How do we handle per-frame mesh updates vs. event-driven rebuilds?
- Should we centralize all presentation sync in a dedicated world or stay in the main world for now?
- What is the minimum viable HUD/UI bridge for designers?

## Next Steps
- Capture current hybrid touchpoints and identify quick wins to move into DOTS-friendly patterns.
- Draft milestones (debug HUD parity, Entities Graphics integration, UI bridge refactor) and fill this file with concrete tasks post review.
