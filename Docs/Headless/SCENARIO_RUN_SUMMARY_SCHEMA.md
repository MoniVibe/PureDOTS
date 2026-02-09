# Scenario Run NDJSON Schema (Headless)

This document summarizes the NDJSON records written by `ScenarioRunRecorder` for headless runs.

## Records

### `type: "run"`
Run header written once at start.

Key fields:
- `scenarioId`, `seed`, `runTicks`, `fixedDeltaTime`, `source`
- `gitCommit`, `gitBranch`, `gitDirty`
- `buildConfig`, `platform`, `unityVersion`, `timestampUtc`
- `exitPolicy`, `exitPolicyEnvRaw`
- `exitGraceMs`, `exitKillMs`, `telemetryFlushGraceMs`
- `registryAggregateHash`, `catalogs[]`

### `type: "digest"`
Written per tick interval for determinism validation.

Key fields:
- `tick`, `hash`, `entityCount`
- `scenarioHash`, `registryHash`, `randomHash`, `timeHash`
- `rewindHash`, `tickTimeHash`
- `commandLogCount`, `snapshotLogCount`

### `type: "summary"`
Run summary written once at end.

Key fields:
- `scenarioId`, `seed`, `finalTick`, `runTicks`
- `commandLogCount`, `snapshotLogCount`
- `frameBudgetExceeded`, `worstFrameMs`, `worstFrameGroup`
- `registryContinuityWarnings`, `registryContinuityFailures`
- `logBytes`
- `perfBudgetFailed`, `perfBudgetMetric`, `perfBudgetValue`, `perfBudgetLimit`, `perfBudgetTick`
- `exitPolicy`, `exitPolicyEnvRaw`, `highestSeverity`
- `telemetryBytesWritten`, `telemetryMaxBytes`, `telemetryCapReached`
- `exitGraceMs`, `exitKillMs`, `telemetryFlushGraceMs`
- `metrics[]`

### Telemetry export marker (if capped)
When telemetry output hits its byte cap, the telemetry exporter emits:
- `type: "telemetryTruncated"` with `runId`, `scenario`, `seed`, `tick`, `maxBytes`

## Notes
- All records are newline-delimited JSON.
- New fields are appended; consumers should tolerate extra keys.
