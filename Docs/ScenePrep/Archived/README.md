# Archived: Hybrid Showcase Documentation

**Date Archived**: During project split implementation
**Reason**: Hybrid showcase approach deprecated in favor of independent project development

## Contents

This folder contains documentation and plans related to the hybrid showcase concept that combined both Godgame and Space4x in a single Unity scene.

## Key Documents

- `HybridAuthoringPlan.md` - Prefab authoring plan
- `HybridGapAnalysis.md` - Gap analysis for hybrid setup
- `HybridOneClickSetup.md` - One-click setup plan
- `HybridSceneSetupInstructions.md` - Step-by-step setup guide
- `HybridShowcaseChecklist.md` - Implementation checklist
- `HybridSpawnConfig.md` - Spawn configuration
- `HybridValidationPlan.md` - Validation steps
- `ImplementationSummary.md` - Summary of what was implemented
- `MCPProgress.md` - MCP tool progress (if hybrid-specific)
- `PrefabCreationGuide.md` - Prefab creation guide
- `QuickStart.md` - Quick start guide (if hybrid-specific)

## Current Status

- **Hybrid systems remain in package**: `HybridControlCoordinator`, `HybridControlToggleSystem`, etc. are still available in PureDOTS package for use in individual projects
- **Projects are independent**: Each project (Godgame, Space4x) now develops its own scenes independently
- **Shared package**: PureDOTS continues to provide shared DOTS framework systems

## Reference

See `HybridShowcaseDecision.md` in parent directory for full context on why the split was made.


