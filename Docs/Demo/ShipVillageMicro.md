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

Use the canonical headless entrypoint:

```bash
python Tools/HeadlessRebuildTool/Tools/Headless/headlessctl.py run_task <task_id_that_uses_scenario_ship_micro_01> --seed 4101 --pack nightly-default
```

Or run through your existing ScenarioRunner flow pointing at:

`Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ship_micro_01.json`

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
