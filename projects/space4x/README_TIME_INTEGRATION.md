Time Integration - Space4x
==========================

Space4x uses the PureDOTS time/rewind pipeline directly. Avoid any game-local time systems. Note: the actual Space4x repo lives at `C:\Users\shonh\OneDrive\Documents\claudeprojects\Unity\Space4x`; this file is a pointer/reminder.

- Source of truth: `PureDOTS/Packages/com.moni.puredots` seeds `TimeState`, `TickTimeState`, `RewindState`, time-control commands, and log buffers (via `CoreSingletonBootstrapSystem` + `PureDotsWorldBootstrap` profiles).
- Controls: emit `TimeControlCommand` to the rewind singleton for pause/play/step/rewind; reuse the shared ScenarioRunner (see PureDOTS Runtime/Scenarios) for headless scenarios and perf/determinism checks.
- HUD/debug: use `DebugDisplayReader` + `RewindTimelineDebug` bound to `DebugDisplayData`; do not fork HUD implementations.
- Scenarios: wire Space4x spawn loops into shared ScenarioRunner (see PureDOTS Runtime/Scenarios) JSON samples (asset-agnostic); keep time unified with PureDOTS.
- CLI: run `-batchmode -nographics -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioFromArgs --scenario <path>` to execute headless scenarios; add `--report <path>` to dump summary output.
