# Ship Village Micro

## What this slice proves

- One aggregate `ShipRoot` entity acts as a tiny village root.
- Seat entities (`captain`, `navigation`, `sensors`, `weapons`, `logistics`, `engineering`) coordinate through bounded comms events.
- Crew entities are deterministically assigned to seats.
- Orders are injected, seats acknowledge, and ship intent advances from `Issued` to `Executing`/`Complete`.
- Headless transcript lines are emitted every 50 ticks.

## Scenario

- File: `Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ship_micro_01.json`
- Scenario ID: `scenario.space4x.ship_micro.01`

## How to run

Option 1: headless task run (`ship_micro_01`):

```bash
python Tools/HeadlessRebuildTool/Tools/Headless/headlessctl.py run_task ship_micro_01 --seed 4101 --pack nightly-default
```

Option 2: direct Unity batchmode run (fallback if task wiring is blocked):

```powershell
"C:\Program Files\Unity\Hub\Editor\6000.3.1f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "C:\Dev\unity_clean\puredots" `
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioExecutorFromArgs `
  --scenario "Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ship_micro_01.json" `
  --report "CI/Reports/scenario_ship_micro_01_report.json" `
  -logFile "CI/Reports/scenario_ship_micro_01.log"
```

Option 3: Tier-0 vibe proof smoke via scale harness metrics gate:

```bash
./CI/run_scale_tests.sh --tier0
```

## Expected output

Every ~50 ticks, logs include:

- tick
- ship id
- order type/state
- readiness
- seat readiness average
- comms event counts
- last bridge event summary

Metrics written via `ScenarioMetricsUtility`:

- `ship.micro.events.count`
- `ship.micro.seat.readiness`
- `ship.micro.order.state`
