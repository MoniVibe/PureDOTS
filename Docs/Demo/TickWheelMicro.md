# TickWheel Micro

## What It Proves
`scenario.puredots.tickwheel.micro` proves the EXP tick-wheel scheduler can enqueue and dispatch a large deterministic event set using bucketed scheduling, with stable same-seed ordering.

The micro harness verifies:
- event volume dispatch works from wheel buckets
- due-tick dispatch has no lateness in the controlled test
- same-seed reruns produce stable digest output

## How To Run
Use the ScenarioRunner executor entrypoint in batchmode:

```powershell
"<UNITY_EDITOR_PATH>\Unity.exe" -batchmode -nographics -projectPath "C:\Dev\unity_clean\_worktrees\puredots_scale_harness_20260213" -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioExecutorFromArgs --scenario "Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_tickwheel_micro.json" --report "Temp/Reports/tickwheel_micro_report.json" -quit
```

## Expected Metrics
The report should include and satisfy:
- `tickwheel.scheduled_count == tickwheel.fired_count`
- `tickwheel.max_lateness_ticks == 0`
- `tickwheel.digest` is stable across same-seed reruns

The bundled micro scenario also asserts:
- `tickwheel.scheduled_count == 4096`
- `tickwheel.fired_count == 4096`
- `tickwheel.max_lateness_ticks == 0`
- `tickwheel.digest > 0`

## Integration Notes
Intended Tier-2 usage:
- event-driven scheduling where immediate polling is too expensive
- effect expiry timers (buff/debuff end, cooldown end)
- wakeups for sleeping entities/systems
- deferred work queues that must execute at deterministic future ticks

Design intent:
- O(1) amortized insert and per-tick bucket scan
- deterministic tie-break ordering (`dueTick`, tie fields, payload/target, sequence)
