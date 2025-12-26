# Project Split Snapshot

**Date**: Created during project split implementation
**Purpose**: Document current state before splitting projects

## Current Structure

### PureDOTS Package
- **Location**: `PureDOTS/Packages/com.moni.puredots`
- **Package Reference**: Both projects use `file:../../PureDOTS/Packages/com.moni.puredots`
- **Hybrid Systems**: These remain in package (reusable):
  - `Runtime/Runtime/Hybrid/HybridControlCoordinator.cs`
  - `Runtime/Systems/Hybrid/HybridControlToggleSystem.cs`
  - `Runtime/Authoring/Hybrid/HybridControlToggleAuthoring.cs`

### Project-Specific Files to Remove/Archive

#### PureDOTS/Assets
- `Assets/Scripts/HybridShowcaseBootstrap.cs` - Project-specific, should be removed or moved to projects
- `Assets/Scripts/Editor/HybridShowcaseBootstrap.cs` - Duplicate, should be removed

#### Space4x Project
- `Assets/Editor/SetupHybridShowcase.cs` - Hybrid setup script, remove or adapt
- `Assets/Scenes/Hybrid/` - Hybrid showcase scenes, archive or remove:
  - `HybridShowcase.unity`
  - `GodgameShowcase_SubScene.unity`
  - `Space4XShowcase_SubScene.unity`
  - `HybridPresentationRegistry.asset`

#### PureDOTS/Docs/ScenePrep
Hybrid-related documentation to archive:
- `HybridAuthoringPlan.md`
- `HybridGapAnalysis.md`
- `HybridOneClickSetup.md`
- `HybridSceneSetupInstructions.md`
- `HybridShowcaseChecklist.md`
- `HybridSpawnConfig.md`
- `HybridValidationPlan.md`
- `MCPProgress.md` (if hybrid-specific)
- `QuickStart.md` (if hybrid-specific)

### Projects Status

#### Godgame
- **Scenes Folder**: Does not exist - needs to be created
- **Package Reference**: Already configured correctly
- **Hybrid Assets**: None found

#### Space4x
- **Scenes Folder**: Exists with legacy and Hybrid subfolders
- **Package Reference**: Already configured correctly
- **Hybrid Assets**: Present in `Assets/Scenes/Hybrid/`

## Decisions Made

1. **PureDOTS stays as shared package** - Both projects will continue referencing the same package
2. **Hybrid systems remain in package** - They're reusable and don't cause issues
3. **Project-specific bootstrap removed** - Each project will have its own bootstrap if needed
4. **Hybrid scenes archived** - Documentation kept, scenes removed from active use


