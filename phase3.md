# Phase 3 - DOTS Outstanding Work

Sources: `Space4x/Docs/TODO/4xdotsrequest.md` and `TRI_PROJECT_BRIEFING.md`. Focus is Space4x DOTS requests plus cross-cutting Godgame considerations.

## Space4x DOTS TODO
- Alignment/compliance: push alignment/affiliation buffers to crew/fleet/colony/faction, add CrewAggregationSystem, slot `Space4XAffiliationComplianceSystem` after aggregation, and route suspicion deltas into intel/alert surfaces.
- Doctrine/authoring/tooling: ship `DoctrineAuthoring` baker to doctrine buffers, generate shared enums for ethics/outlooks/affiliations, add inspector validation (range conflicts, fanatic caps), and stage a micro mutiny/desertion demo scene.
- Tests/integration: NUnit coverage for compliance/loyalty/suspicion scaling, runtime assertions for missing doctrine/affiliation data, and bridge breach outputs into AI planner tickets, telemetry snapshots, and narrative triggers.
- Crew skills follow-ups: extend XP sources beyond mining, apply skill modifiers to refit/repair/combat/hauling and hazard resistance, and widen skill/hazard test coverage.
- Modules/degradation: implement carrier module slot/refit/archetype transitions with stat aggregation (modules as entities, refit gating) plus component health/degradation/field repair/station overhaul/failure flows with prioritized repair queues.
- Mobility/infrastructure: build waypoint/highway/gateway components with registration/pathfinding/maintenance (destruction/reconfiguration) and implement interception/rendezvous broadcast + pathfinding + queue systems.
- Economy/logistics: finish supply/demand inventory tracking with dynamic pricing/trade-op identification and add FIFO batch inventory with spoilage + ordered consumption.
- Tech/time/growth: add tech diffusion state + upgrade application, ensure sim systems honor PureDOTS time scaling and mark time-independent UI, and keep breeding/cloning framework deferred until core loops land.

## Agent Split
- Agent A: Alignment/compliance chain (buffers, aggregation, compliance ordering, suspicion routing), doctrine/authoring/tooling (baker + enums + inspectors), and related tests/integration surfaces (AI tickets, telemetry, narrative hooks, runtime assertions).
- Agent B: Modules/degradation pipeline (module slots/refit/archetype transitions, stat aggregation, component health/degradation/repair/failure queues) plus crew skills follow-ups that touch refit/repair/combat/hauling/hazards.
- Agent C: Mobility/infrastructure (waypoints/highways/gateways, maintenance, interception/rendezvous queue), economy/logistics (supply-demand pricing, trade ops, FIFO+spoilage), and tech/time/growth (tech diffusion, time-scale compliance across systems, breeding/cloning remains deferred).

## Agent Notes
- Agent C: mobility registry scaffold (waypoints/highways/gateways) and batch inventory with spoilage/FIFO consumption landed in PureDOTS. Pathfinding queue, dynamic pricing, and tech diffusion slices delivered.
  - Mobility path resolver now queues requests deterministically, honours disabled/blocked nodes, traverses gateways, and publishes rendezvous/interception events each tick.
  - Batch pricing now reacts to supply/demand trends with smoothed inflow/outflow tracking and time-scale aware spoilage; defaults seeded via bootstrap. Trade opportunity surfacing added (best supply/demand pairs by price spread) for ScenarioRunner/HUD consumers, with routing into logistics requests and a lightweight fulfillment stub to keep registry metrics moving.
  - Added tech diffusion components + systems (source/target bootstrap, distance-weighted spread, time-scale aware rates) for Space4X tech progression hooks.
  - Sample scenarios live under `Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/` (`space4x_mobility_path.json`, `space4x_batch_inventory.json`, `space4x_trade_opportunities.json`) to drive ScenarioRunner + HUD verification.
- Agent A (in-flight): alignment/compliance scaffolding landed (affiliation/doctrine components, crew aggregation + compliance alerts, HUD/telemetry counters) with sample `space4x_alignment_demo.json`. Pending: richer doctrine validation, AI ticket wiring, and mutiny/desertion demo hook.
- Agent B (in-flight): module maintenance baseline added (module slot/health components, aggregation, degradation, repair queue, refit gating, telemetry counters, and unit tests). Pending: module baking/catalog wiring, combat stat aggregation, and scenario/demo coverage.

## Status Update – Presentation Track (Agent Beta)
- Expanded presentation binding schema with palette/size/speed plus lifetime/attach rules; bridge now carries style blocks instead of raw strings.
- Added GrayboxMinimal/GrayboxFancy binding samples with runtime toggle via `presentation.binding.sample` bootstrap and exported them as package samples.
- HUD/telemetry now surfaces presentation pool stats and camera rig telemetry; screenshot hash utility added for visual validation.
- Companion sync system (offset + follow lerp) landed; adapters populate offsets/lerp defaults and integration coverage was added.

## Godgame Considerations
- Maintain PureDOTS boundaries: keep new spines/systems in the package with per-project bridges rather than Godgame-specific forks.
- Time/rewind reuse: stick to PureDOTS time controls + ScenarioRunner; avoid bespoke time pipelines when mirroring Space4x features.
- Shared enums/tooling: align on the planned enum registry and doctrine/inspector helpers so Godgame can consume the same definitions without divergence.
- Compliance/event surfaces: shape Space4x breach/telemetry/narrative hooks so they mirror cleanly into Godgame AI tickets and incident/bark flows.
- Burst/validation: keep aggregation/compliance/skill/degradation systems Burst-safe and test-backed; mirror runtime assertions in Godgame for safety.
