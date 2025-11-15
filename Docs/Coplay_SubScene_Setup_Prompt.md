# Coplay Agent Prompt: SubScene Setup for DOTS Entity Baking

## Context
We have a Unity DOTS project where GameObjects with authoring components need to be baked into DOTS entities. The GameObjects are currently in the main scene (`SpawnerDemoScene`) but need to be moved into a SubScene for proper baking.

## Current State
- ✅ Created `GameEntities_SubScene` GameObject in the main scene
- ✅ Created `Assets/Scenes/GameEntities.unity` subscene file
- ✅ Added SubScene component to `GameEntities_SubScene` pointing to `GameEntities.unity`
- ✅ Moved all GameObjects as children of `GameEntities_SubScene`:
  - Carrier (with MiningVessel1 and MiningVessel2 as children)
  - ResourceNode_Wood1, ResourceNode_Wood2, ResourceNode_Wood3
  - OreNode1, OreNode2
  - Asteroid_Ore1, Asteroid_Ore2, Asteroid_Ore3
  - Storehouse
  - Two Villager GameObjects
- ❌ **PROBLEM**: The `GameEntities.unity` file is empty - GameObjects haven't been moved into it yet

## Task
Unity requires manually opening a SubScene once to trigger automatic GameObject migration. Use Unity MCP tools to:

1. **Open the SubScene**:
   - Select `GameEntities_SubScene` GameObject in the Hierarchy
   - In the Inspector, find the SubScene component
   - Either double-click the GameObject OR click the "Open" button on the SubScene component
   - This will open the subscene in a separate scene window

2. **Verify Migration**:
   - Check that `Assets/Scenes/GameEntities.unity` now contains GameObjects
   - Verify all GameObjects appear in the subscene window
   - Close the subscene window and return to main scene

3. **Verify Entity Creation**:
   - Enter Play Mode
   - Check Unity Console for diagnostic logs
   - Expected logs should show:
     - `Vessels: Total=2` (or more)
     - `Villagers: Total=2` (or more)
     - `ResourceRegistry: Entries=8` (or more - 3 wood + 2 ore ground + 3 asteroids)
   - If still showing `Total=0`, investigate why entities aren't being created

## Alternative Approach (if opening subscene doesn't work)
If Unity MCP cannot open subscenes programmatically, manually copy GameObjects from main scene to subscene file:

1. Read GameObject data from main scene (`Assets/Scenes/SpawnerDemoScene.unity/SpawnerDemoScene.unity`)
2. Find all GameObjects that are children of `GameEntities_SubScene` (parentInstanceID: -97020)
3. Copy their full GameObject entries, Transform components, and all MonoBehaviours into `Assets/Scenes/GameEntities.unity`
4. Remove them from the main scene file (or update their parent to Entity.Null)
5. Update the SubScene component to reference the subscene correctly

## Success Criteria
- ✅ `GameEntities.unity` contains GameObjects (not empty)
- ✅ Entering Play Mode creates DOTS entities
- ✅ Console shows: `Vessels: Total>0`, `Villagers: Total>0`, `ResourceRegistry: Entries>0`
- ✅ No errors about missing entities or RequireForUpdate failures

## Tools Available
- Unity MCP: `manage_scene`, `manage_gameobject`, `manage_asset`
- File system: Direct scene file editing
- Console reading: `read_console` to verify entity creation

## Important Notes
- GameObjects MUST be in a SubScene for DOTS baking to work
- Authoring components (`MiningVesselAuthoring`, `VillagerAuthoring`, `ResourceSourceAuthoring`, etc.) are already on the GameObjects
- The scene structure is correct - we just need Unity to move GameObjects into the subscene file
- Once moved, Unity will automatically bake them into entities on Play













