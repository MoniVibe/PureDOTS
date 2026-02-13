# Performance Gates

Canonical scale/perf gate policy for headless acceptance.

## Principles
- Use scenario-driven gates, not ad-hoc scene impressions.
- Compare against stable baselines for the same scenario and build family.
- Treat determinism and perf as a coupled gate: perf "wins" are invalid if determinism regresses.

## Space4X Gate Ladder
- `Assets/Scenarios/scenario_space_01_perf_gate_100k.json`
- `Assets/Scenarios/scenario_space_01_perf_gate_250k.json`
- `Assets/Scenarios/scenario_space_01_perf_gate_500k.json`
- `Assets/Scenarios/scenario_space_01_perf_gate_1m.json`

### Default Budget Targets (Headless)
- **G1 (100k)**: sim frame p95 <= 40 ms
- **G2 (250k)**: sim frame p95 <= 60 ms
- **G3 (500k)**: sim frame p95 <= 90 ms
- **G4 (1m)**: sim frame p95 <= 140 ms

## Godgame Companion Gates
- `Assets/Scenarios/Godgame/godgame_scale_50k.json`
- `Assets/Scenarios/Godgame/godgame_scale_200k.json`

### Default Budget Targets (Headless)
- **50k**: sim frame p95 <= 35 ms
- **200k**: sim frame p95 <= 80 ms

## Common Pass/Fail Rules
- Pass only with valid artifact bundle and invariant success.
- Fail on deterministic mismatch, invariant failure, or watchdog timeout.
- Fail if perf exceeds gate target without an approved budget update.

## Telemetry Budget Discipline
- Headless default telemetry mode should remain summary-oriented unless a debug run requires more detail.
- Treat telemetry bloat as a regression when it obscures diagnostics or causes truncation/churn.
- Any cap increase requires a before/after artifact comparison and documented rationale.

## Updating Budgets
- Budget changes require:
  - scenario id
  - before/after evidence
  - reason for change (content growth vs regression acceptance)
  - explicit sign-off in run notes
