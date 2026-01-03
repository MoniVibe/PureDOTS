# Ops Bus Rules (Headless Cycles)

- Agents running headless cycles must write heartbeats every 30-60s.
- Use ops bus requests/results (`TRI_STATE_DIR/ops`) instead of chat-only coordination.
- Do not exit early; idle and keep polling during no-work windows.

# Phase 3 DOTS Push - Agent Prompts

Use these prompts to run agents on the Phase 3 TODO in `phase3.md`. PureDOTS is the shared Entities 1.4 template (see `TRI_PROJECT_BRIEFING.md`); keep new systems inside `Packages/com.moni.puredots` and touch `projects/space4x` / `projects/godgame` only for demo/bootstrap wiring. Reuse PureDOTS time/rewind + ScenarioRunner; avoid per-game time pipelines or hybrid references. Keep work Burst-safe, deterministic, and ScenarioRunner-runnable. Current state: Agent C landed mobility registry scaffolds (waypoints/highways/gateways) with HUD/telemetry plus batch inventory (spoilage/FIFO/pricing multipliers) and sample scenarios; Agent C reports nothing else to do under the current instruction set. Agent Beta delivered presentation schema/samples with a pending companion sync follow-up.

## Agent A - Alignment & Doctrine
```
You are Agent A (Implementation). Build the alignment/compliance/doctrine stack from phase3.md.
Goals:
- Push alignment/affiliation buffers onto crew/fleet/colony/faction entities, add CrewAggregationSystem, slot Space4XAffiliationComplianceSystem after aggregation, and route suspicion deltas into intel/alert outputs.
- Ship DoctrineAuthoring baker to doctrine buffers, generate shared enums for ethics/outlooks/affiliations, add inspector validation (range conflicts, fanatic caps), and stage a micro mutiny/desertion demo scene.
- Tests/integration: NUnit for compliance/loyalty/suspicion scaling; runtime assertions for missing doctrine/affiliation data; bridge breach outputs into AI planner tickets, telemetry snapshots, and narrative triggers.
Constraints:
- Keep changes inside Packages/com.moni.puredots; per-game edits limited to demo/bootstrap wiring. Burst/deterministic, PureDOTS time/rewind friendly, ScenarioRunner compatible.
Deliverables:
- Code/tests, enums + baker + demo scene, runtime assertions, and phase3.md handoff (touched files/follow-ups).
```

## Agent B - Modules/Degradation + Crew Skills
```
You are Agent B (Implementation). Deliver the modules/degradation pipeline and crew skill follow-ups from phase3.md.
Goals:
- Implement carrier module slots/refit/archetype transitions with stat aggregation (modules as entities), refit gating, and prioritized repair queues; cover component health/degradation/field repair/station overhaul/failure flows.
- Extend crew skill effects/XP sources across refit, repair, combat, hauling, and hazard resistance; ensure skill modifiers feed module stats and repair math.
- Tests/telemetry: widen skill/hazard coverage; add assertions/telemetry around degradation/failure/repair queues; keep Burst-safe and rewind-friendly.
Constraints:
- Work inside Packages/com.moni.puredots; only light demo hooks per game. Deterministic ordering, no per-frame allocations, ScenarioRunner + PureDOTS time compliant.
Deliverables:
- Code/tests, sample hooks or minimal demo wiring, and phase3.md handoff with touched files/follow-ups.
```

## Agent C - Mobility/Economy/Tech-Time (Delivered)
```
Agent C reports completion of the current instruction set.
Delivered:
- Mobility registry scaffolds (waypoints/highways/gateways) with HUD/telemetry plus path queue stub and interception events.
- Batch inventory with spoilage/FIFO and pricing multipliers, plus sample scenarios wired for ScenarioRunner.
Pending:
- None under the current instruction set; await new goals before resuming.
Handoff:
- List touched files, sample scenario paths, telemetry counters exposed, and any caveats for Error/Glue and Documentation agents to capture in phase3.md.
```

## Agent Beta - Presentation Follow-up
```
You are Agent Beta (Implementation). Close the remaining presentation follow-up.
Goals:
- Implement the companion sync system (attach/follow smoothing) to pair with the delivered presentation binding schema and samples (GrayboxMinimal/GrayboxFancy).
- Confirm screenshot hash utility and presentation pool stats stay stable under the sync changes; keep the bridge allocation-free and rewind-safe.
Constraints:
- Work inside Packages/com.moni.puredots; per-game wiring limited to sample toggles/bootstraps. No hybrid references; deterministic ordering; ScenarioRunner compatible.
Deliverables:
- Code/tests and any sample updates; phase3.md handoff with touched files/follow-ups.
```

## Error & Glue Agent Prompt
```
You are the Error/Glue agent. Stabilize and harden slices A-C as they land.
Tasks:
- Run/extend tests (NUnit, PlayMode, ScenarioRunner) for new alignment/doctrine, module/repair/skills, delivered mobility/economy/tech slices, and presentation follow-up; add coverage for time-scale/rewind guards.
- Fix compile/runtime issues; enforce group ordering/ECS attributes; ensure Burst safety, ECB boundaries, and zero per-frame allocations; wire HUD/telemetry outputs where missing.
- Verify delivered sample scenarios/demo scenes run headless via ScenarioRunner for Space4x/Godgame wiring.
- Update phase3.md with verification notes and remaining gaps; handoff list with touched files, risks, follow-ups.
```

## Documentation Agent Prompt
```
You are the Documentation agent. After Error/Glue sign-off, align docs with slices A-C.
Tasks:
- Update phase3.md and Docs/Progress.md / Docs/ROADMAP_STATUS.md with completion notes and links to scenarios/demo scenes/enums/samples.
- Refresh relevant truth-sources/guides for alignment/compliance/doctrine, module/degradation/skills, delivered mobility/economy/tech/time systems, and presentation sync follow-up; add ScenarioRunner usage notes and demo scene checklists (mutiny/desertion, module/repair flows, waypoint/gateway loops, batch inventory scenarios).
- Note shared enums/tooling for Godgame mirroring and time-scale compliance; keep docs ASCII and concise.
- Handoff summary: files updated, open questions, next docs to refresh.
```
