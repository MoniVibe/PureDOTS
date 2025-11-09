# Fix SubScene Loading and Missing Scripts

## Critical Issue: SubScene Not Loading

**Error**: `Loading Entity Scene failed because the entity header file couldn't be resolved. guid=00000000000000000000000000000000`

**Root Cause**: Unity hasn't properly imported or recognized the `GameEntities.unity` subscene asset, even though the link is correct in the scene file.

## Solution Steps

### Step 1: Open and Save the Subscene (CRITICAL)

1. **In Unity Editor Project Window**:
   - Navigate to `Assets/Scenes/GameEntities.unity`
   - **Double-click** to open it in the Scene view
   - Unity will import it properly when opened directly

2. **Save the Subscene**:
   - Press `Ctrl+S` or go to `File > Save`
   - This forces Unity to generate the entity header files

3. **Close the Subscene**:
   - Return to the main scene (`SpawnerDemoScene.unity`)

### Step 2: Verify SubScene Link

1. **In Main Scene Hierarchy**:
   - Select "New Sub Scene" GameObject
   - In Inspector, check the SubScene component
   - "Scene Asset" should show `GameEntities` (not empty)
   - If empty, click the circle icon and assign `Assets/Scenes/GameEntities.unity`

### Step 3: Reimport Subscene (Alternative)

If opening doesn't work:

1. **Right-click** `GameEntities.unity` in Project window
2. Select **"Reimport"**
3. Wait for import to complete
4. Check console for errors

### Step 4: Remove Missing Scripts from Main Camera

**Option A - Using Editor Menu** (Easiest):
1. **Select "Main Camera"** GameObject in Hierarchy
2. Go to menu: **`Space4X > Cleanup > Remove Missing Scripts from Selected GameObject`**
3. Missing scripts will be removed automatically

**Option B - Manual**:
1. Select "Main Camera" GameObject
2. In Inspector, you'll see "Missing (Mono Script)" entries
3. Right-click each → **"Remove Component"**
4. OR: Right-click GameObject → **"Remove Component"** → select missing script names

**Option C - Clean All Missing Scripts**:
1. Go to menu: **`Space4X > Cleanup > Remove Missing Scripts from All GameObjects`**
2. This will clean all missing scripts in the scene

### Step 5: Verify Baking Works

After fixing subscene:

1. **Enter Play Mode**
2. **Check Console**:
   - ✅ Should NOT see "Loading Entity Scene failed" error
   - ✅ Should see entity creation logs for vessels, villagers, resources
   - ⚠️ Missing script warnings should be gone (if cleaned up)

3. **Verify Entities Created**:
   - Systems should log: `[VesselAISystem] Found X vessels...`
   - `[VillagerAISystem]` should find villagers
   - `[ResourceRegistrySystem]` should register resources

## Why This Happens

- **Subscene not opened**: Unity only generates entity headers when a subscene is opened directly
- **Missing scripts**: Stale references from deleted MonoBehaviours remain in serialized data
- **Import state**: Unity cache may be stale, requiring explicit reimport

## Files Created

- **`Assets/Scripts/Space4x/Editor/RemoveMissingScripts.cs`**: Editor utility to clean missing scripts
  - Menu: `Space4X > Cleanup > Remove Missing Scripts from Selected GameObject`
  - Menu: `Space4X > Cleanup > Remove Missing Scripts from All GameObjects`

## Additional Warnings (Non-Critical)

These are warnings only and won't prevent baking:

- **VillagerSpawnerAuthoring**: Missing prefab reference - assign prefab or remove component
- **SpatialPartitionAuthoring**: Missing profile - assign profile or remove component

These can be addressed later if you're using spawners/spatial partitioning.

## Success Criteria

- ✅ No "Loading Entity Scene failed" error
- ✅ Subscene loads and bakes entities correctly
- ✅ No missing script warnings
- ✅ Entities appear in ECS world (vessels, villagers, resources)












