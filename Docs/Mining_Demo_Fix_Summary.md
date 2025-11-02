# Mining Demo Fix Summary

## Issues Identified

1. **Authoring Components Missing** ✅ FIXED
   - GameObjects had Transform but no authoring components
   - Fixed by running `Space4X/Setup Mining Demo Scene` menu item

2. **Scene Structure Issue** ⚠️ CRITICAL
   - **Problem**: `GameEntities.unity` is being opened directly as the main scene
   - **Solution**: Must open `SpawnerDemoScene.unity` which contains a SubScene component referencing `GameEntities.unity`
   - DOTS requires GameObjects to be in a subscene (not opened directly) for proper entity conversion

3. **PureDotsConfigAuthoring Setup** ⚠️ NEEDS VERIFICATION
   - Config must be in the MAIN scene (SpawnerDemoScene), not the subscene
   - Run `Space4X/Fix Mining Demo Scene Setup` to add it

4. **Camera Rendering** ⚠️ POTENTIAL ISSUE
   - Camera is seeing only background color, suggesting entities aren't rendering
   - This is likely because entities aren't being created (scene structure issue)

## How to Fix

### Step 1: Open the Correct Scene
1. **DO NOT** open `Assets/Scenes/GameEntities.unity` directly
2. **DO** open `Assets/Scenes/SpawnerDemoScene.unity/SpawnerDemoScene.unity`
3. The SubScene component in SpawnerDemoScene will load GameEntities.unity automatically

### Step 2: Verify SubScene Setup
1. In SpawnerDemoScene, find the "New Sub Scene" GameObject
2. Check the SubScene component:
   - Should reference `GameEntities.unity`
   - **Auto Load Scene** should be checked/enabled
3. If SubScene is open (checkbox checked), close it (uncheck) for Play mode

### Step 3: Add PureDotsConfigAuthoring
1. In SpawnerDemoScene (main scene), run menu: `Space4X/Fix Mining Demo Scene Setup`
2. This creates a "PureDotsConfig" GameObject with PureDotsConfigAuthoring
3. Verify it has the PureDotsRuntimeConfig asset assigned

### Step 4: Verify Authoring Components
1. All GameObjects in GameEntities should have their authoring components:
   - Villager1, Villager2 → VillagerAuthoring
   - MiningVessel1, MiningVessel2 → MiningVesselAuthoring  
   - ResourceNode_Wood1-3, OreNode1-2 → ResourceSourceAuthoring
   - Asteroid_Ore1-3 → ResourceSourceAuthoring
   - Storehouse → StorehouseAuthoring
   - Carrier → CarrierAuthoring

### Step 5: Enter Play Mode
1. Enter Play mode from SpawnerDemoScene
2. Wait a few seconds for subscene to load
3. Check Unity Console for:
   - `[ResourceCatalogDebug]` messages showing resources registered
   - `[VesselAISystem]` messages showing vessels finding targets
   - `[MovementDiagnostic]` messages showing entities moving
   - No errors about missing entities

## Expected Behavior After Fix

### Villagers
- Should find nearby resource nodes (wood/ore)
- Move toward nodes
- Gather resources
- Deposit at Storehouse
- Repeat loop

### Mining Vessels
- Should find nearest asteroids
- Move toward asteroids
- Start mining when close enough
- Return to Carrier when full
- Carrier deposits to Storehouse

### Camera
- Should see entities rendering (not just background)
- Entities should appear as they move

## Troubleshooting

### If entities still don't move:
1. Check Unity Console for errors
2. Verify SubScene is closed (not open) during Play mode
3. Check that PureDotsConfigAuthoring is in MAIN scene, not subscene
4. Verify resource type IDs match: "wood" and "ore" must exist in PureDotsResourceTypes.asset

### If camera still shows only background:
1. Entities might not be rendering - check if they have mesh renderers or Graphics components
2. Check camera culling mask
3. Verify entities are actually being created (check Entity Debugger window)

### If systems aren't running:
1. Check for RequireForUpdate failures in console
2. Verify PureDotsConfigAuthoring singleton is created
3. Check system ordering - systems might need PureDotsConfig singletons first

## Scene Structure Diagram

```
SpawnerDemoScene.unity (MAIN SCENE)
├── Main Camera (regular GameObject)
├── PureDotsConfig (PureDotsConfigAuthoring) ← MUST BE HERE
└── New Sub Scene (SubScene component)
    └── References: GameEntities.unity
    
GameEntities.unity (SUBSCRIPT - loaded via SubScene)
├── Carrier
│   ├── MiningVessel1
│   └── MiningVessel2
├── ResourceNode_Wood1-3
├── OreNode1-2
├── Asteroid_Ore1-3
├── Storehouse
├── Villager1
└── Villager2
```

## Key DOTS Concepts

- **SubScenes are REQUIRED**: GameObjects with authoring components MUST be in a subscene, not opened directly
- **Main Scene vs SubScene**: Main scene has camera, lighting, config. SubScene has gameplay entities
- **Baking**: Happens automatically when subscene is closed. Open subscene = live editing, Closed = baked entities
- **Singletons**: PureDotsConfigAuthoring creates singletons. Must be in main scene (baked once), not subscene






