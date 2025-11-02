# Quick Fix Guide - Mining Demo Scene

## Problem
Nothing is moving because GameObjects are missing authoring components that convert them to DOTS entities.

## Solution: Add Missing Components

### 1. Add VillagerAuthoring to Villagers

**Select:** Villager1
- Add Component → Search "VillagerAuthoring"
- Base Speed: `3.0` (default is fine)
- Leave other settings as default

**Select:** Villager2  
- Add Component → Search "VillagerAuthoring"
- Base Speed: `3.0` (default is fine)
- Leave other settings as default

### 2. Add MiningVesselAuthoring to Mining Vessels

**Select:** MiningVessel1 (child of Carrier)
- Add Component → Search "MiningVesselAuthoring"
- Base Speed: `5.0` (default is fine)
- Capacity: `50.0` (default is fine)

**Select:** MiningVessel2 (child of Carrier)
- Add Component → Search "MiningVesselAuthoring"
- Base Speed: `5.0` (default is fine)
- Capacity: `50.0` (default is fine)

### 3. Add ResourceSourceAuthoring to Wood Nodes

**Select:** ResourceNode_Wood1
- Add Component → Search "ResourceSourceAuthoring"
- Resource Type Id: `wood` (type exactly as shown)
- Initial Units: `200`
- Gather Rate Per Worker: `4`
- Max Simultaneous Workers: `3`
- Debug Gather Radius: `3`
- Check ✅ **Respawns**
- Respawn Seconds: `45`

**Repeat for:** ResourceNode_Wood2, ResourceNode_Wood3 (same settings)

### 4. Add ResourceSourceAuthoring to Ore Nodes

**Select:** OreNode1
- Add Component → Search "ResourceSourceAuthoring"
- Resource Type Id: `ore` (type exactly as shown)
- Initial Units: `150`
- Gather Rate Per Worker: `3`
- Max Simultaneous Workers: `2`
- Debug Gather Radius: `3`
- Check ✅ **Respawns**
- Respawn Seconds: `60`

**Repeat for:** OreNode2 (same settings)

### 5. Add ResourceSourceAuthoring to Asteroids

**Select:** Asteroid_Ore1
- Add Component → Search "ResourceSourceAuthoring"
- Resource Type Id: `ore` (type exactly as shown)
- Initial Units: `500`
- Gather Rate Per Worker: `8`
- Max Simultaneous Workers: `5`
- Debug Gather Radius: `5`
- Check ✅ **Respawns**
- Respawn Seconds: `120`

**Repeat for:** Asteroid_Ore2, Asteroid_Ore3 (same settings)

### 6. Add StorehouseAuthoring to Storehouse

**Select:** Storehouse
- Add Component → Search "StorehouseAuthoring"
- In **Capacity** list:
  - Click **+** to add entry
  - Resource Type Id: `wood`
  - Max Capacity: `1000`
  - Click **+** to add another entry
  - Resource Type Id: `ore`
  - Max Capacity: `1000`

### 7. Add CarrierAuthoring to Carrier

**Select:** Carrier
- Add Component → Search "CarrierAuthoring"
- Leave default settings

## Verify Setup

### Check Resource Types Asset
1. Navigate to: `Assets/PureDOTS/Config/PureDotsResourceTypes.asset`
2. Verify it contains entries for:
   - `wood`
   - `ore`
   - `stone` (if used)

### Check Scene Config
1. Look for GameObject with `PureDotsConfigAuthoring` component
2. Verify it references:
   - `PureDotsRuntimeConfig.asset`
   - `PureDotsResourceTypes.asset`

### Check Sub-Scene
1. Verify "New Sub Scene" GameObject is active
2. Sub-scene must be loaded for DOTS baking to work

## Test

1. **Enter Play Mode**
2. **Check Console** for debug messages:
   - `[ResourceCatalogDebug]` - Should show resources registered
   - `[VesselAISystem]` - Should show vessels finding targets
   - `[EntityStateDebug]` - Should show entities detected
   - `[MovementDiagnostic]` - Comprehensive status

## Expected Console Output (Success)

```
[ResourceCatalogDebug] Catalog has 3 resource types:
  [0] wood
  [1] stone
  [2] ore
[ResourceCatalogDebug] ResourceRegistry has 8 registered resources...
[VesselAISystem] Found 2 vessels, Registry has 8 resources...
[EntityStateDebug] Vessels: Total=2, WithTargets=2, Moving=2
[EntityStateDebug] Villagers: Total=2, WithJobs=2, Moving=2
```

## Troubleshooting

### If still no movement:

1. **Check Entities Window**: Window > Entities > Hierarchy
   - Should see entities with components like `VesselAIState`, `VillagerAIState`
   - If empty, sub-scene not baking correctly

2. **Check Systems Window**: Window > Entities > Systems
   - Verify systems are enabled:
     - `VesselAISystem`
     - `VesselTargetingSystem`
     - `VesselMovementSystem`
     - `VillagerAISystem`
     - `VillagerTargetingSystem`
     - `VillagerMovementSystem`

3. **Check Time State**:
   - Console should NOT show "Game is PAUSED"
   - Console should NOT show "RewindMode is not Record"

4. **Villagers need jobs**:
   - Villagers require worksite assignments via `VillagerJobAssignmentSystem`
   - May need to add a worksite or job assignment system setup

## Next Steps After Fix

If villagers still don't move, they may need job assignments. Check if there's a worksite system or job assignment system that needs to be configured.







