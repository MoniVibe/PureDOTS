# Mining Demo Setup - Automated Fix

## Editor Script Created

I've created an automated setup script at:
`Assets/Scripts/Editor/MiningDemoSetupHelper.cs`

## How to Run

### Option 1: Use Menu Item (Recommended)
1. Wait for Unity to finish compiling (watch bottom-right corner)
2. Go to menu: **Space4X > Setup Mining Demo Scene**
3. Click the menu item
4. A dialog will appear confirming setup completion

### Option 2: Manual Component Addition
If the menu item doesn't appear or doesn't work, follow the manual steps in `Docs/Mining_Demo_Quick_Fix.md`

## What the Script Does

The script automatically adds the following components:

✅ **VillagerAuthoring** → Villager1, Villager2
- Base Speed: 3.0

✅ **MiningVesselAuthoring** → MiningVessel1, MiningVessel2  
- Base Speed: 5.0
- Capacity: 50.0

✅ **ResourceSourceAuthoring (wood)** → ResourceNode_Wood1, Wood2, Wood3
- Resource Type: "wood"
- Initial Units: 200
- Gather Rate: 4 per worker
- Max Workers: 3
- Respawns: Yes (45 seconds)

✅ **ResourceSourceAuthoring (ore)** → OreNode1, OreNode2
- Resource Type: "ore"
- Initial Units: 150
- Gather Rate: 3 per worker
- Max Workers: 2
- Respawns: Yes (60 seconds)

✅ **ResourceSourceAuthoring (asteroid ore)** → Asteroid_Ore1, Ore2, Ore3
- Resource Type: "ore"
- Initial Units: 500
- Gather Rate: 8 per worker
- Max Workers: 5
- Respawns: Yes (120 seconds)

✅ **StorehouseAuthoring** → Storehouse
- Capacity: wood (1000), ore (1000)

✅ **CarrierAuthoring** → Carrier
- Total Capacity: 1000

## After Running the Script

1. **Verify Components**: Select a few GameObjects in the Hierarchy and verify they have the authoring components in the Inspector

2. **Enter Play Mode**: Press Play

3. **Check Console** for these debug messages:
   - `[MiningDemoSetup]` - Confirms components were added
   - `[ResourceCatalogDebug]` - Shows resource registration
   - `[VesselAISystem]` - Shows vessels finding targets
   - `[EntityStateDebug]` - Shows entity states
   - `[MovementDiagnostic]` - Comprehensive movement diagnostics

## Expected Behavior

**Vessels:**
- Should move toward asteroids automatically
- Start mining when close
- Return to carrier when full

**Villagers:**
- Need job assignments (may require additional setup)
- Should move toward resource nodes when assigned
- Gather and deposit at storehouse

## Troubleshooting

### Menu Item Not Appearing
- Wait for Unity to finish compiling scripts
- Check Console for compilation errors
- Verify `Assets/Scripts/Editor/MiningDemoSetupHelper.cs` exists

### Components Not Added
- Check Console for `[MiningDemoSetup]` messages
- Verify GameObjects exist in scene (names must match exactly)
- Some GameObjects might be in sub-scenes - verify sub-scene is loaded

### Still No Movement
- Check `Docs/Mining_Demo_Diagnosis.md` for detailed troubleshooting
- Verify sub-scene is enabled and loaded
- Check that `PureDotsConfigAuthoring` exists in scene
- Verify resource types ("wood", "ore") exist in `PureDotsResourceTypes.asset`







