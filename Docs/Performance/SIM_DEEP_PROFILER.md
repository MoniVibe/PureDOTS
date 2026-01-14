# SIM Deep Profiler

The `SimPhaseProfiler` instrumentation pipes deterministic-friendly COSMOS-scale metrics into the existing telemetry pipeline so both headless runs and editor smoke scenes expose the same story.

## What is tracked

| Metric group | Description |
|--------------|-------------|
| **Phase timing** | `simphase.phase.{scenarioApply|movement|physics|sensors|comms|knowledge|economy|presentation}.ms` plus `simphase.tickTotalMs` describe each major phase from start-to-finish. Phases are bounded by start/end markers that run before/after their canonical system groups. |
| **Queue/backpressure** | `simphase.queue.commsMessages`, `...commsInboxEntries`, `...commsOutboxEntries`, `...ackEvents`, `...detectedEntities`, `...interruptsPending` count the backlog inside the comms stream, inbox/outbox buffers, AI ack queue, sensor `DetectedEntity` buffers, and interrupt buffers. |
| **ECS counts** | `simphase.entities.carriers/vessels/villagers/projectiles` give coarse archetype awareness alongside `simphase.entities.total`, `simphase.chunks.total`, `simphase.archetypes.total`, and `simphase.chunks.perArchetype`. |
| **Rendering stats (dev-only)** | When the `EntitiesGraphicsStatsDrawer` is available (editor/development builds) the system emits `simphase.render.drawCommands`, `simphase.render.instances`, and `simphase.render.instancesPerCommand`. |
| **Scale regression harness** | The three worst ticks seen so far expose `simphase.regression.worstTick{0..2}`, `...DurationMs`, and `...Phase`. These values are overwritten only when a new tick exceeds the previous worst duration, so the telemetry stream has an easy “slowest frames” section. |

## How to read the output

1. Enable `TelemetryExportConfig` with `TelemetryExportFlags.IncludeTelemetryMetrics` and point the path at a persistent NDJSON file.
2. The metrics are emitted every `CadenceTicks` (default 30) inside the same telemetry export used by other systems. Each `metric` record contains the `runId`, `scenario`, and `tick` along with the keys above.
3. Use standard tooling (log parsers, spreadsheets, Grafana) to plot the phase timings and queue depths. The regression keys produce a concise “worst tick” dataset you can filter for repeated regressions.
4. For editor/development builds, the rendering stats only appear when the `EntitiesGraphicsStatsDrawer` type exists; they are skipped on batch/headless runners.

## Backpressure tuning

- Watch `simphase.queue.commsInboxEntries` and `simphase.queue.commsOutboxEntries` for sustained growth; if they never drain the communication system is under-provisioned.
- High `simphase.queue.detectedEntities` counts could signal sensor flooding loops; review the entities that feed into the `PerceptionSystemGroup`.
- Use the regression harness (`simphase.regression.*`) to tie individual ticks to complaining phase durations so you can correlate queue spikes to expensive systems.

## Notes

- The profiler relies on the `SimPhaseProfilerState` singleton created by `SimPhaseProfilerBootstrapSystem`. No additional setup is needed beyond adding the package to your world.
- Because the instrumentation runs inside `LateSimulationSystemGroup` before `TelemetryExportSystem`, the exported path (`TelemetryExportConfig.OutputPath`) receives the metrics at the same cadence as other telemetry streams.
- Rendering stats are guarded by `UNITY_EDITOR` so the runtime and CI payload remains lean; the fields use reflection to avoid hard dependencies on `Unity.Rendering`.
