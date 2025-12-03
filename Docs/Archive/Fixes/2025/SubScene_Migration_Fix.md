# Critical Fix: Move GameObjects into SubScene File

## Problem
The SubScene component is linked correctly, but `GameEntities.unity` is EMPTY. GameObjects are still in the main scene (`SpawnerDemoScene.unity`), not in the subscene file. Unity can only bake GameObjects that are **inside** the subscene file.

## Root Cause
Unity SubScenes work differently than regular parenting:
- Parented GameObjects stay in the main scene (won't be baked)
- GameObjects must be **inside** the subscene file to be baked into entities

## Solution Options

### Option 1: Use Unity Editor (RECOMMENDED)
1. Select all GameObjects that should be in subscene:
   - Carrier (and its children MiningVessel1, MiningVessel2)
   - ResourceNode_Wood1, ResourceNode_Wood2, ResourceNode_Wood3
   - OreNode1, OreNode2
   - Asteroid_Ore1, Asteroid_Ore2, Asteroid_Ore3
   - Storehouse
   - Villager GameObjects (if they exist)
2. Right-click selected GameObjects → **New Sub Scene** → **From Selection**
3. This creates a NEW subscene and moves GameObjects into it
4. Delete the old `GameEntities_SubScene` GameObject if it still exists
5. Assign the new subscene file to `GameEntities_SubScene` component

### Option 2: Manually Copy GameObjects (ADVANCED)
Manually copy GameObject YAML entries from `SpawnerDemoScene.unity` to `GameEntities.unity`:
- Copy entire GameObject entries (including Transform, all MonoBehaviours)
- Update parent references
- Ensure proper YAML structure

### Option 3: Unity MCP Tools
Use Unity MCP to programmatically:
1. Find GameObjects in main scene
2. Create new subscene from selection
3. Assign to SubScene component

## Verification
After fix:
- `GameEntities.unity` should contain GameObject entries (not empty)
- Enter Play Mode
- Console should show: `Vessels: Total>0`, `Villagers: Total>0`, `ResourceRegistry: Entries>0`
- Entities window (Window > Entities > Hierarchy) should show entities

## Current Status
- ❌ GameEntities.unity is EMPTY (no GameObjects)
- ✅ SubScene component is linked
- ❌ GameObjects are in main scene, not subscene
- ❌ Result: Zero entities created















