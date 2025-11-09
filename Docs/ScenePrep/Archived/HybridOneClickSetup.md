# Hybrid Showcase One-Click Setup

## Overview

The `SetupHybridShowcase` editor script (`Space4x/Assets/Editor/SetupHybridShowcase.cs`) provides a single menu command that automates the entire hybrid scene creation process. Run `Tools/Space4X/Setup Hybrid Showcase Scene (One-Click)` to generate a complete scene with Godgame village and Space4X carrier loops.

## Implementation Status

All 5 phases are implemented and ready for incremental feature additions:

### Phase 1: Core Scene Automation ✅
- Validates prerequisites (prefabs, configs)
- Handles Unity nested folder bugs
- Creates/clears main scene hierarchy
- Sets up lighting and camera

### Phase 2: Godgame Village Pass ✅
- Creates Godgame SubScene with proper assignment
- Adds terrain tile (150x150m) for village area
- Configures PureDOTS Config and Spatial Partition
- Creates Village Spawner at (-120, 0, 0) with 8 villagers
- Instantiates 2 storehouses at documented positions
- Places 6 resource nodes with "wood" resource type
- All entities use proper authoring components for DOTS conversion

### Phase 3: Space4X Carrier Loop Pass ✅
- Creates Space4X SubScene with proper assignment
- Configures PureDOTS Config and Spatial Partition
- Sets up Space4XMiningDemoAuthoring with:
  - 1 carrier at (120, 0, 20) with patrol configuration
  - 4 mining vessels linked to carrier
  - 6 asteroids with Minerals resource type
- Adds Space4X camera rig with input authoring

### Phase 4: Shared Infrastructure ✅
- Creates HybridBootstrap GameObject with HybridShowcaseBootstrap component
- Adds HybridControlToggleAuthoring for runtime mode switching
- Creates MiningVisualManifest GameObject with authoring component
- Generates/assigns HybridPresentationRegistry.asset with Godgame descriptors

### Phase 5: Validation ✅
- Checks for required GameObjects
- Validates SubScene setup
- Verifies prefab references
- Provides clear error/warning logs

## Usage

1. Open Unity Editor
2. Navigate to `Tools/Space4X/Setup Hybrid Showcase Scene (One-Click)`
3. Wait for setup to complete (check console for progress)
4. Scene will be saved to `Assets/Scenes/Hybrid/HybridShowcase.unity`
5. Press Play to test

## Extensibility

The script is designed for incremental feature additions:

### Adding New Features

**For Godgame:**
- Add new prefab instantiation in `SetupGodgameVillage()`
- Update presentation registry in `SetupSharedInfrastructure()`
- Add validation checks in `ValidateSetup()`

**For Space4X:**
- Extend `Space4XMiningDemoAuthoring` configuration in `SetupSpace4XCarrierLoop()`
- Add new visual descriptors to presentation registry
- Update camera/input setup as needed

**For Shared Systems:**
- Add new singleton GameObjects in `SetupSharedInfrastructure()`
- Extend validation checks in `ValidateSetup()`

### Example: Adding Combat Loop

```csharp
// In SetupGodgameVillage():
// Add combat structures, band spawners, etc.

// In SetupSpace4XCarrierLoop():
// Add defensive stations, patrol routes, etc.

// In SetupSharedInfrastructure():
// Add combat registry, event systems, etc.
```

## Technical Considerations

### SubScenes
- Both SubScenes are configured with `Auto Load Scene = true`
- SubScenes convert into the shared DOTS world automatically
- Entity baking happens when SubScenes are opened/baked

### Presentation Registry
- Automatically generates `HybridPresentationRegistry.asset` if missing
- Populates Godgame descriptors (villager, storehouse, resource_node)
- Space4X visuals use MiningVisualManifest system (no registry entries needed)

### Runtime Configs
- Godgame SubScene uses Space4X config (or PureDOTS config if Space4X missing)
- Space4X SubScene uses Space4X config
- Both share the same resource catalog (ensure resources are merged)

### Input Systems
- Space4X camera input configured via `Space4XCameraInputAuthoring`
- Godgame divine hand input works via `HybridControlCoordinator`
- F9 key cycles between Dual/Space4X Only/Godgame Only modes

## Known Limitations

1. **Space4X Prefabs**: Carrier/MiningVessel prefabs must exist for full functionality (currently uses authoring components)
2. **Terrain Generation**: TerrainAuthoring creates a simple plane mesh (not Unity Terrain)
3. **Divine Hand**: Godgame divine hand setup requires additional manual configuration (not yet automated)
4. **Resource Types**: Resource nodes default to "wood" - should be configurable

## Future Enhancements

- [ ] Auto-detect and use Space4X prefabs when available
- [ ] Generate terrain with height variations
- [ ] Add divine hand authoring setup
- [ ] Support multiple resource types per node
- [ ] Add UI overlay for mode switching visualization
- [ ] Generate runtime configs if missing
- [ ] Add timeline/playback controls setup

