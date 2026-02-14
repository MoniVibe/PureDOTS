# AI Tier Cadence Micro

## What this proves

- Equal AI cohorts run under tier cadence gating where evaluation frequency declines by tier.
- Expected ordering is: `tier0 > tier1 > tier2 >= tier3` evaluation counts.
- Determinism signal is emitted so same-seed reruns can be compared.

## Scenario

- File: `Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ai_tiercadence_micro.json`
- Scenario ID: `scenario.ai.tiercadence.micro`

## How to run (batchmode)

```powershell
"C:\Program Files\Unity\Hub\Editor\6000.3.1f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "C:\Dev\unity_clean\puredots" `
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioExecutorFromArgs `
  --scenario "Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ai_tiercadence_micro.json" `
  --report "CI/Reports/scenario_ai_tiercadence_micro_report.json" `
  -logFile "CI/Reports/scenario_ai_tiercadence_micro.log"
```

## Expected metrics

- `ai.tiercadence.tier0.eval_count`
- `ai.tiercadence.tier1.eval_count`
- `ai.tiercadence.tier2.eval_count`
- `ai.tiercadence.tier3.eval_count`
- `ai.tiercadence.digest` stable across same-seed reruns
