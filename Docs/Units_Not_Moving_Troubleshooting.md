# Units Not Moving - Troubleshooting Guide

## Systems Created

1. **ResourceCatalogDebugSystem** - Logs catalog and registry status (runs once)
2. **EntityStateDebugSystem** - Logs vessel/villager states every ~1 second

## Critical Fixes Applied

### 1. Resource Type Catalog
- ✅ Added "ore" to `PureDotsResourceTypes.asset`
- ✅ Assigned `PureDotsRuntimeConfig.asset` to `PureDotsConfigAuthoring` in scene
- ✅ Set all `resourceTypeId` values correctly

### 2. System Ordering
- ✅ `VesselAISystem` runs after `ResourceRegistrySystem`
- ✅ `VesselTargetingSystem` runs after `VesselAISystem` and `ResourceRegistrySystem`
- ✅ `VesselMovementSystem` runs after `VesselTargetingSystem`
- ✅ `VesselTransformSyncSystem` runs in `PresentationSystemGroup` after movement

### 3. Component Setup
- ✅ Added `VillagerAuthoring` to both villagers
- ✅ Added `MiningVesselAuthoring` to vessels (already done via MCP)

## What to Check

### Step 1: Check Unity Console

When you play the scene, look for these debug messages:

**`[ResourceCatalogDebug]` messages:**
- If you see "Catalog NOT CREATED" → Config asset not assigned
- If you see "NO RESOURCES REGISTERED" → Check `resourceTypeId` values
- If catalog shows "wood", "stone", "ore" → Good!
- If registry shows 0 entries → Resources aren't being registered

**`[EntityStateDebug]` messages (every ~1 second):**
- `Vessels: Total=X` - Are vessels detected as entities?
- `WithTargets=X` - Do vessels have `TargetEntity` assigned?
- `Moving=X` - Are vessels actually moving?
- Individual vessel logs show `State`, `Goal`, `TargetEntity`, `TargetPos`

### Step 2: Verify Entities Exist

**In Unity Editor:**
1. Open **Window > Entities > Hierarchy** (DOTS Entities window)
2. Search for "Vessel" or "Villager"
3. If no entities found → GameObjects aren't being baked into DOTS entities

**Possible causes:**
- Scene not set up for DOTS baking
- Sub-scenes not opened/baked
- Authoring components missing

### Step 3: Check System Execution

**In Unity Editor:**
1. Open **Window > Entities > Systems** (DOTS Systems window)
2. Search for "Vessel" systems
3. Verify these systems exist and are enabled:
   - `VesselAISystem`
   - `VesselTargetingSystem`
   - `VesselMovementSystem`
   - `VesselTransformSyncSystem`

### Step 4: Verify Resource Registry

Check `[ResourceCatalogDebug]` output:
- Should show 3 resource types: "wood", "stone", "ore"
- Should show registered resources > 0
- If 0 resources → `resourceTypeId` values are empty or types not in catalog

## Common Issues

### Issue 1: "Vessels: Total=0"
**Problem:** Vessels aren't being baked into entities
**Solution:** 
- Check `MiningVesselAuthoring` component exists on vessel GameObjects
- Verify scene is set up for DOTS baking
- Check Entities window to see if any entities exist

### Issue 2: "WithTargets=0" but "Total=2"
**Problem:** Vessels exist but aren't getting targets assigned
**Possible causes:**
- Resource registry is empty (check `[ResourceCatalogDebug]`)
- No resources match vessel's resource type filter
- `VesselAISystem` not finding resources in registry

### Issue 3: "Moving=0" but "WithTargets=2"
**Problem:** Vessels have targets but aren't moving
**Possible causes:**
- `TargetPosition` not being resolved by `VesselTargetingSystem`
- Movement system not running (check Systems window)
- `TimeState.IsPaused` is true
- `RewindState.Mode` is not `Record`

### Issue 4: "Villagers: Total=0"
**Problem:** Villagers aren't being baked
**Solution:**
- Check `VillagerAuthoring` component exists
- Villagers need job assignments to move (different from vessels)

## Quick Test

To test if movement works at all, temporarily modify `VesselAISystem` to always assign a target:

```csharp
// In VesselAISystem.cs, UpdateVesselAIJob.Execute method
// Add this at the start:
if (aiState.CurrentState == VesselAIState.State.Idle)
{
    // Force assign a test target
    aiState.CurrentGoal = VesselAIState.Goal.Mining;
    aiState.CurrentState = VesselAIState.State.MovingToTarget;
    aiState.TargetEntity = Entity.Null; // Will test with position only
    aiState.TargetPosition = new float3(10, 0, 10); // Test position
    return;
}
```

If vessels move toward (10,0,10), then movement works but target assignment is broken.

## Next Steps

1. **Play the scene** and check console for debug output
2. **Check Entities window** to verify entities exist
3. **Check Systems window** to verify systems are running
4. **Share the debug output** so we can identify the exact issue

