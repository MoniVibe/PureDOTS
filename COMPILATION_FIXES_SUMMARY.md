# Compilation Fixes Summary

**IMPORTANT**: This workspace is PureDOTS (the shared framework). Game-specific fixes must be applied in the actual game project directories:
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame`
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x`

## ✅ Completed Fixes (PureDOTS Workspace)

### Bucket 1: PureDOTS Camera Physics
- **Status**: ✅ Already fixed
- Camera physics stubs exist in `Packages/com.moni.puredots/Runtime/Physics/CameraPhysicsStubs.cs`
- Camera controller uses Unity's `Physics.Raycast` directly

### Bucket 3: Namespace / Naming Issues  
- **PresentationSystemGroup**: ✅ Already aliased in `GodgameRegistryBridgeSystem.cs`
- **Godgame.Debug**: ✅ Already uses `Godgame.Debugging` namespace (no collision)

### Bucket 4: Godgame Runtime vs Presentation Assembly Layering
- **SwappablePresentation types**: Should exist in Godgame project. If missing, create them there.
- **Presentation Components**: ⚠️ **MUST BE CREATED IN GODGAME PROJECT** (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`)
  - Create `Assets/Scripts/Godgame/Presentation/PresentationTagComponents.cs` with:
    - `PresentationLODState`
    - `VillagerVisualState` (with enum)
    - `ResourceChunkVisualState`
    - `VillageCenterVisualState`
    - `BiomePresentationData`
    - `PresentationConfig`
    - All presentation tags (`VillagerPresentationTag`, `ResourceChunkPresentationTag`, `VillageCenterPresentationTag`, `VegetationPresentationTag`)

### Bucket 5: PureDOTS Time / Misc References
- **TimeControlCommand**: ✅ Verified location
  - Located in `PureDOTS.Runtime.Components` namespace
  - File: `Packages/com.moni.puredots/Runtime/Runtime/TimeControlComponents.cs`
  - Any files using `using PureDOTS.Systems.Time;` should use `using PureDOTS.Runtime.Components;` instead

## ⏳ Pending Fixes (Require File Access)

### Bucket 2: DOTS SourceGen / Jobs Errors
- **ApplyLODTintJob**: Need to split into 3 separate jobs when file is accessible
- Files likely in Godgame project directory: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame`

### Bucket 3: Namespace / Naming Issues
- **using Bakings;**: Remove from these files when accessible:
  - `BiomeTerrainAgent.cs`
  - `BiomeTerrainBindingAuthoring.cs`
  - `GroundTileAuthoring.cs`
  - `GroundTileSystems.cs`
  - `FaunaAmbientSpawnSystem.cs`
  - `MiraclePresentationAuthoring.cs`
  - `MiraclePresentationSystem.cs`

### Bucket 4: Assembly Layering
- **Move Presentation Systems**: When files are accessible, move presentation-only systems to `Godgame.Presentation` asmdef (or create one if needed)
- **Assembly References**: Ensure `Godgame.Runtime` does NOT reference `Godgame.Presentation`

## Files Created (PureDOTS Workspace)

1. **`FIXES_TO_APPLY.md`**: Comprehensive step-by-step guide for all fixes
2. **`COMPILATION_FIXES_SUMMARY.md`**: This summary document

**Note**: Game-specific files (like `PresentationTagComponents.cs`) must be created in the actual Godgame project directory, not in PureDOTS workspace.

## Next Steps

1. **For PureDOTS fixes**: Apply in this workspace (`C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`)
2. **For Godgame fixes**: Apply in the Godgame project (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`)
3. When compilation errors appear, check `FIXES_TO_APPLY.md` for detailed fixes
4. Apply fixes in order (later errors often resolve as side-effects)
5. Remember: Game code belongs in game projects, NOT in PureDOTS workspace

## Notes

- **CRITICAL**: Game-specific code must be in the actual Godgame project directory (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`), NOT in PureDOTS workspace
- The `Assets/Projects/Godgame` folder in PureDOTS workspace is likely artifacts, not the real project
- PureDOTS fixes (like camera physics stubs) are correctly in PureDOTS workspace
- Game fixes (like presentation components) must be applied in the Godgame project directory
- See `TRI_PROJECT_BRIEFING.md` section "Camera Organization & Artifacts Warning" for details

