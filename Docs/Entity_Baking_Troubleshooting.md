# Entity Baking Troubleshooting - Units Not Moving

## Critical Issue: GameObjects Must Be in a Subscene

In Unity DOTS, GameObjects with authoring components (`MiningVesselAuthoring`, `VillagerAuthoring`, etc.) **MUST be in a subscene** for baking to occur. If they're in the main scene, they won't be converted to DOTS entities.

## How to Fix

### Step 1: Create a Subscene

1. In Unity Editor, open your scene (`SpawnerDemoScene` or `SampleScene`)
2. In the Hierarchy, right-click and select **New Sub Scene** > **Empty Scene**
3. Name it something like `GameEntities` and save it
4. The subscene will appear in the Hierarchy with a small icon

### Step 2: Move GameObjects to Subscene

1. **Select** the GameObjects that need to be entities:
   - `MiningVessel1`
   - `MiningVessel2`
   - `Villager` GameObjects
   - Resource nodes (`ResourceNode_Wood1`, `OreNode1`, `Asteroid_Ore1`, etc.)
   - Storehouse
   - Any other GameObjects with authoring components

2. **Drag** them into the subscene in the Hierarchy

3. **Double-click** the subscene to open it (you'll see it switch to a new scene view)

4. **Verify** all GameObjects are now children of the subscene

### Step 3: Verify Baking

1. **Open** the Entities window: **Window > Entities > Hierarchy**
2. **Check** if vessel/villager entities appear after baking
3. Look for entities with `VesselMovement`, `VesselAIState`, `MinerVessel` components

### Step 4: Use Diagnostic Tool

1. In the main scene (not subscene), create an empty GameObject
2. Add the `VesselEntityDiagnostic` component (it's in `Space4X.Editor` namespace)
3. Play the scene
4. Check Console for diagnostic output showing:
   - How many vessel entities exist
   - Entity positions and states
   - Resource registry status

## What Baking Does

When you:
- **Open** a subscene → Live baking occurs (converts GameObjects to entities immediately)
- **Close** a subscene → Background baking occurs (converts when scene loads)

During baking:
- `MiningVesselAuthoring` → Creates entity with `MinerVessel`, `VesselAIState`, `VesselMovement`
- `VillagerAuthoring` → Creates entity with villager components
- `ResourceSourceAuthoring` → Creates entity with resource components

## Common Issues

### Issue 1: "NO VESSEL ENTITIES FOUND"
**Cause:** GameObjects not in subscene  
**Fix:** Move GameObjects to subscene (see Step 2)

### Issue 2: "Entities exist but aren't moving"
**Possible causes:**
- Resources not registered (check `[ResourceCatalogDebug]` output)
- Vessels don't have targets (check `[VesselAISystem]` output)
- Systems not running (check Systems window)

### Issue 3: "Subscene shows as modified but entities don't exist"
**Cause:** Baking errors  
**Fix:** 
- Check Console for baking errors
- Verify authoring components exist on GameObjects
- Ensure all required assemblies are referenced

## Quick Verification Checklist

- [ ] Subscene exists in Hierarchy
- [ ] Vessels/Villagers are children of subscene
- [ ] Subscene is open (for live baking) or closed (for background baking)
- [ ] No baking errors in Console
- [ ] Entities window shows entities with correct components
- [ ] `VesselEntityDiagnostic` shows entity count > 0 when playing

## Next Steps After Baking Works

Once entities exist:
1. Check Console for `[ResourceCatalogDebug]` - should show resources registered
2. Check Console for `[VesselAISystem]` - should show vessels found targets
3. Check Console for `[VesselMovementDebug]` - should show vessels moving

If entities exist but still not moving, share the console output for further diagnosis.

