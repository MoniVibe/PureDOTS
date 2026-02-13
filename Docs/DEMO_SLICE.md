# Demo Slice: Full Vibe

This is the canonical definition of a "full demo vibe" for Space4X.

## Six-Point Target
1. **Galaxy Presence**
Space reads as a true 3D battlespace with meaningful distance, travel time, and positioning.

2. **Carrier as Village**
Every capital ship demonstrates internal society signals: crew state, readiness, doctrine, and operational health.

3. **Command Ladder in Action**
Player intent can be expressed and observed across:
`God -> Admiral -> Captain -> Officer -> Pilot`.
Orders should decompose into lower-level execution without breaking simulation truth.

4. **Interlocked Loops**
At least one end-to-end chain is visible in the same slice:
explore -> gather/logistics -> upgrade/readiness -> conflict/response.

5. **Headless-Proven Truth**
The slice is accepted only when headless artifacts pass determinism and invariant checks for the same scenario/build.

6. **Human-Debuggable Evidence**
Issues can be triaged from telemetry plus ECS debugging tools.
Entities Journaling and Entities windows (Archetypes, Systems, Journaling) are required parts of the workflow, including ECB origin visibility.

## Acceptance Checklist
- Scenario has stable `scenario_id` and reproducible seed behavior.
- Required run artifacts exist (`meta`, `run_summary`, `watchdog`, `invariants`, relevant log tails).
- No regression against active perf gate for the slice scale tier.
- Findings can be explained from evidence, not guesswork.
