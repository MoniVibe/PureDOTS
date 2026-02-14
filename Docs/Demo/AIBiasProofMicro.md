# AI Bias Proof Micro

## What It Proves
`scenario_ai_biasproof_micro.json` proves that two otherwise-equivalent cohorts diverge in action selection when only behavior bias tuning differs.

The micro harness holds utility factors and baseline sensor scores constant, then checks that:
- Group A skews toward aggression-tagged decisions.
- Group B skews toward social-tagged decisions.
- same-seed reruns produce the same digest.

## How To Run (Batchmode)
Run via ScenarioRunner entrypoint:

```powershell
"<UNITY_EDITOR_PATH>\Unity.exe" -batchmode -nographics -projectPath "C:\Dev\unity_clean\_worktrees\puredots_scale_harness_20260213" -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioExecutorFromArgs --scenario "Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/scenario_ai_biasproof_micro.json" --report "Temp/Reports/aibiasproof_micro_report.json" -quit
```

## Expected Metrics
From the generated report:
- `ai.biasproof.groupA.aggression_chosen` should be high (dominant within Group A choices).
- `ai.biasproof.groupB.social_chosen` should be high (dominant within Group B choices).
- `ai.biasproof.digest` should be stable across same-seed reruns.

Related observability keys currently emitted by the micro system:
- `ai.biasproof.scheduled_count`
- `ai.biasproof.fired_count`
- `ai.biasproof.digest`
