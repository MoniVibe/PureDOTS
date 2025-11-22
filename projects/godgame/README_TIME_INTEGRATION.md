Time Integration - Godgame
==========================

This project consumes PureDOTS time/rewind as-is. Do not add game-specific time pipelines. Note: the actual Godgame repo lives at `C:\Users\shonh\OneDrive\Documents\claudeprojects\Unity\Godgame`; this file is a pointer/reminder.

- Source of truth: `PureDOTS/Packages/com.moni.puredots` seeds `TimeState`, `TickTimeState`, `RewindState`, time-control commands, and log buffers via `CoreSingletonBootstrapSystem` + `PureDotsWorldBootstrap`.
- Use shared controls: write `TimeControlCommand` to the rewind singleton buffer for pause/play/step/rewind; use ScenarioRunner (see PureDOTS Runtime/Scenarios) from the package for headless scenarios.
- HUD/debug: rely on `DebugDisplayReader` + `RewindTimelineDebug` wired to the existing `DebugDisplayData` singleton; avoid duplicate HUDs.
- Scenarios: hook game-specific spawns into the shared ScenarioRunner (see PureDOTS Runtime/Scenarios) samples; keep scenario JSONs asset-agnostic and avoid custom time systems.
- CLI: run `-batchmode -nographics -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioFromArgs --scenario <path>` to drive headless scenarios; emit reports via `--report <path>` if needed.
