# AI Tier Cadence Pipeline Micro

## What this proves

- Tier cadence affects the full AI pipeline, not just scoring.
- `AIFidelityTier` + `TierProfileSettings` cadence gating changes:
  - `AISensorUpdateSystem` sampling frequency
  - `AIUtilityScoringSystem` evaluation frequency
  - `AITaskResolutionSystem` command emission frequency
- With identical setup per cohort, tier cadence is the primary differentiator.

## Scenario

- File: `Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ai_tierpipe_micro.json`
- Scenario ID: `scenario.ai.tierpipe.micro`

## Batchmode run command

```powershell
"C:\Program Files\Unity\Hub\Editor\6000.3.1f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "C:\dev\Tri\puredots" `
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioExecutorFromArgs `
  --scenario "Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ai_tierpipe_micro.json" `
  --report "CI/Reports/scenario_ai_tierpipe_micro_report.json" `
  -logFile "CI/Reports/scenario_ai_tierpipe_micro.log"
```

## Expected metrics

- Sensor metrics:
  - `ai.tierpipe.tier0.sensor_samples`
  - `ai.tierpipe.tier1.sensor_samples`
  - `ai.tierpipe.tier2.sensor_samples`
  - `ai.tierpipe.tier3.sensor_samples`
- Command metrics:
  - `ai.tierpipe.tier0.commands_emitted`
  - `ai.tierpipe.tier1.commands_emitted`
  - `ai.tierpipe.tier2.commands_emitted`
  - `ai.tierpipe.tier3.commands_emitted`
- Determinism metric:
  - `ai.tierpipe.digest`

Expected relationship:

- `tier0 > tier1 > tier2 >= tier3` for both sensor samples and commands emitted.

Determinism note:

- `ai.tierpipe.digest` is produced from deterministic per-tick/per-entity folds and should remain stable on same-seed reruns.
