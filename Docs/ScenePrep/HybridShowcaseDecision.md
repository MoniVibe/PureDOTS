# Hybrid Showcase: Project Separation Decision

## Decision

**Split the hybrid showcase work** - Each project (`Godgame` and `Space4x`) should be developed independently. The hybrid showcase scene concept, while technically feasible, introduces unnecessary complexity due to cross-project asset access limitations.

## Why Split?

1. **Unity Project Boundaries**: `AssetDatabase` only works within a single Unity project. Prefabs from `PureDOTS` project aren't accessible from `Space4x` project without copying/importing.

2. **Asset Management**: Each project has its own asset database, prefabs, and scene files. Trying to share assets across projects requires:
   - Copying assets (duplication)
   - Package references (complex setup)
   - Symlinks (OS-dependent)

3. **Development Workflow**: Independent projects allow:
   - Faster iteration per game
   - Independent versioning
   - Clearer ownership of features
   - Easier testing and debugging

## What Was Accomplished (Reusable)

All the **code systems** we built are still valuable and can be reused:

### ✅ Completed (Reusable in Both Projects)

1. **Input Coordination System**
   - `HybridControlCoordinator` - Static coordinator for input mode switching
   - `HybridControlToggleSystem` - DOTS system for F9 key toggle
   - `HybridControlToggleAuthoring` - MonoBehaviour bridge for UI
   - **Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Hybrid/`
   - **Status**: ✅ Complete and reusable

2. **Input Integration**
   - `Space4XCameraInputSystem` - Updated to respect coordinator
   - `GodgameCameraInputBridge` - Updated to respect coordinator
   - **Status**: ✅ Already integrated into respective projects

3. **Bootstrap Component**
   - `HybridShowcaseBootstrap` - Scene orchestration MonoBehaviour
   - **Location**: `PureDOTS/Assets/Scripts/HybridShowcaseBootstrap.cs`
   - **Status**: ✅ Can be copied to either project if needed

4. **Setup Script**
   - `SetupHybridShowcase.cs` - One-click scene setup automation
   - **Location**: `Space4x/Assets/Editor/SetupHybridShowcase.cs`
   - **Status**: ✅ Can be adapted for single-project use

## Next Steps Per Project

### For Godgame Project

1. **Create standalone showcase scene** in `Godgame/Assets/Scenes/`
2. **Copy needed prefabs** from PureDOTS to Godgame project
3. **Use `VillagerSpawnerAuthoring`** with prefabs in same project
4. **Test independently** - villagers, storehouses, resource nodes

### For Space4x Project

1. **Create standalone showcase scene** in `Space4x/Assets/Scenes/`
2. **Create Space4X prefabs** (Carrier, MiningVessel, AsteroidNode) within Space4x project
3. **Use `Space4XMiningDemoAuthoring`** with prefabs in same project
4. **Test independently** - carriers, mining vessels, asteroids

### For PureDOTS Package

1. **Keep hybrid systems** (`HybridControlCoordinator`, etc.) in package
2. **Document usage** - Each project can use these systems independently
3. **No hybrid scene** - Remove hybrid scene setup scripts from PureDOTS

## Migration Guide

If you want to use the setup script in a single project:

1. **Copy prefabs** from PureDOTS to target project:
   ```
   PureDOTS/Assets/PureDOTS/Prefabs/Villager.prefab 
   → Godgame/Assets/PureDOTS/Prefabs/Villager.prefab
   ```

2. **Update `SetupHybridShowcase.cs`** to only set up one game's systems
   - Remove cross-project references
   - Use only local prefabs
   - Simplify to single-game setup

3. **Or create separate setup scripts**:
   - `SetupGodgameShowcase.cs` for Godgame project
   - `SetupSpace4XShowcase.cs` for Space4x project

## Benefits of Separation

- ✅ **Clearer boundaries** - Each project owns its assets
- ✅ **Faster development** - No cross-project dependencies
- ✅ **Easier testing** - Test each game independently
- ✅ **Better organization** - Clear ownership of features
- ✅ **Reusable systems** - PureDOTS package systems still work

## Files Archived/Moved

- `PureDOTS/Docs/ScenePrep/Archived/` - All hybrid-related documentation archived here
- `Space4x/Assets/Editor/SetupHybridShowcase.cs` - Removed (no longer needed)
- `PureDOTS/Assets/Scripts/HybridShowcaseBootstrap.cs` - Removed (project-specific)
- `Space4x/Assets/Scenes/Hybrid/` - Marked as archived (see ARCHIVED_README.md)

## Conclusion

The hybrid showcase was a good learning exercise and produced valuable reusable systems. However, **working on projects separately** is the better long-term approach. Each project can still use the shared PureDOTS package systems, but should maintain its own assets, prefabs, and scenes.

