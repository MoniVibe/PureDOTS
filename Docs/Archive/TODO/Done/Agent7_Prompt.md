# Agent 7: Integration Verification

**Status:** Agent 7 ran verification pass context update (Unity not launched in this session; console checks pending when Editor is available).

## Your Mission
Verify that all fixes from Agents 1-6 were applied correctly and the projects compile without the targeted errors.

## Projects to Verify
1. `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`
2. `C:\Users\Moni\Documents\claudeprojects\unity\Godgame`

## Required Reading First
Read `TRI_PROJECT_BRIEFING.md` in either project root to understand the error patterns.

---

## Wait for Other Agents

Before starting verification, confirm that Agents 1-6 have completed their work. Check for their report files or ask the user.

---

## Verification Steps

### Step 1: PureDOTS Verification

Open the PureDOTS project in Unity Editor and trigger a domain reload.

**Check for BC1016 Errors (Agents 1 & 2):**
```
Search Console for: BC1016
Should NOT find errors in:
- BandFormationSystem
- SpellEffectExecutionSystem
- TimelineBranchSystem / WhatIfSimulationSystem
- LessonAcquisitionSystem
```

**Check for CreateAssetMenu Warnings (Agents 5 & 6):**
```
Search Console for: CreateAssetMenu
Should NOT find warnings for:
- CultureStoryCatalogAuthoring
- LessonCatalogAuthoring
- SpellCatalogAuthoring
- ItemPartCatalogAuthoring
- EnlightenmentProfileAuthoring
- BuffCatalogAuthoring
- SchoolComplexityCatalogAuthoring
- QualityFormulaAuthoring
- SpellSignatureCatalogAuthoring
- QualityCurveAuthoring
```

---

### Step 2: Godgame Verification

Open the Godgame project in Unity Editor and trigger a domain reload.

**Check for CS0234/CS0246 Errors (Agent 3):**
```
Search Console for: CS0234, CS0246
Should NOT find errors in:
- SwappablePresentationBindingEditor
- Anything mentioning "Presentation"
```

**Check for CS0618 Warnings (Agent 4):**
```
Search Console for: CS0618, FindObjectOfType, FindObjectsOfType
Should NOT find warnings in:
- GodgameDevSceneSetup
- GodgameDemoSceneWizard
```

**Check for Missing Type Errors (Agent 4):**
```
Search Console for: DevTools, Demo
Should NOT find CS0246 errors for these types
```

---

### Step 3: Cross-Project Check

Since Godgame depends on PureDOTS package, verify that:
1. Godgame can still import and use PureDOTS types
2. No new errors were introduced by the fixes
3. Basic scenes load without errors

---

## Checklist

Use this checklist to track verification:

### PureDOTS
- [ ] Domain reload completes without errors
- [ ] No BC1016 for BandFormationSystem
- [ ] No BC1016 for SpellEffectExecutionSystem
- [ ] No BC1016 for WhatIfSimulationSystem
- [ ] No BC1016 for LessonAcquisitionSystem
- [ ] No CreateAssetMenu warning for CultureStoryCatalogAuthoring
- [ ] No CreateAssetMenu warning for LessonCatalogAuthoring
- [ ] No CreateAssetMenu warning for SpellCatalogAuthoring
- [ ] No CreateAssetMenu warning for ItemPartCatalogAuthoring
- [ ] No CreateAssetMenu warning for EnlightenmentProfileAuthoring
- [ ] No CreateAssetMenu warning for BuffCatalogAuthoring
- [ ] No CreateAssetMenu warning for SchoolComplexityCatalogAuthoring
- [ ] No CreateAssetMenu warning for QualityFormulaAuthoring
- [ ] No CreateAssetMenu warning for SpellSignatureCatalogAuthoring
- [ ] No CreateAssetMenu warning for QualityCurveAuthoring

### Godgame
- [ ] Domain reload completes without errors
- [ ] No CS0234/CS0246 for SwappablePresentationBindingEditor
- [ ] No CS0618 for GodgameDevSceneSetup
- [ ] No CS0618 for GodgameDemoSceneWizard
- [ ] No CS0246 for DevTools
- [ ] No CS0246 for Demo
- [ ] SampleScene loads without errors

---

## Handling Failures

If any check fails:

1. **Identify the responsible agent** (see table below)
2. **Document the remaining error** with full error message
3. **Flag for re-work** or escalate to user

| Error Type | Responsible Agent |
|------------|-------------------|
| BC1016 BandFormation/SpellEffect | Agent 1 |
| BC1016 Timeline/Lesson | Agent 2 |
| CS0234/CS0246 Presentation | Agent 3 |
| CS0618 FindObject* | Agent 4 |
| CS0246 DevTools/Demo | Agent 4 |
| CreateAssetMenu (Culture,Lesson,Spell,Item,Enlightenment) | Agent 5 |
| CreateAssetMenu (Buff,School,Quality,Signature,Curve) | Agent 6 |

---

## Final Report

Generate a comprehensive report:

```
# Error Fix Batch Verification Report
Date: 2025-11-27
Verified by: Agent 7

## PureDOTS Project
Build Status: [SUCCESS / FAILED]

### BC1016 Burst Errors
- BandFormationSystem: [✅ FIXED / ❌ STILL BROKEN]
- SpellEffectExecutionSystem: [✅ FIXED / ❌ STILL BROKEN]
- WhatIfSimulationSystem: [✅ FIXED / ❌ STILL BROKEN]
- LessonAcquisitionSystem: [✅ FIXED / ❌ STILL BROKEN]

### CreateAssetMenu Warnings
- CultureStoryCatalogAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- LessonCatalogAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- SpellCatalogAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- ItemPartCatalogAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- EnlightenmentProfileAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- BuffCatalogAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- SchoolComplexityCatalogAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- QualityFormulaAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- SpellSignatureCatalogAuthoring: [✅ FIXED / ❌ STILL BROKEN]
- QualityCurveAuthoring: [✅ FIXED / ❌ STILL BROKEN]

## Godgame Project
Build Status: [SUCCESS / FAILED]

### Stale References
- SwappablePresentationBindingEditor: [✅ FIXED / ❌ STILL BROKEN]

### Obsolete APIs
- GodgameDevSceneSetup: [✅ FIXED / ❌ STILL BROKEN]
- GodgameDemoSceneWizard: [✅ FIXED / ❌ STILL BROKEN]

### Missing Types
- DevTools references: [✅ RESOLVED / ❌ STILL BROKEN]
- Demo references: [✅ RESOLVED / ❌ STILL BROKEN]

## New Errors Introduced
[List any NEW errors that appeared after fixes, or "None"]

## Summary
Total targeted errors: ~25
Fixed: [X]
Remaining: [Y]
New issues: [Z]

Recommendation: [READY TO MERGE / NEEDS REWORK ON: ...]
```

---

## Escalation

If verification reveals systemic issues (e.g., many fixes didn't work), escalate to user with:
1. Summary of what's broken
2. Error messages
3. Suspected cause
4. Recommended next steps

