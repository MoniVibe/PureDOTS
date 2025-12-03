# ⚠️ CRITICAL: resourceTypeId MUST Be Set

## Why This Matters

**If `resourceTypeId` is empty, units WILL NOT MOVE** because:

1. **Resources won't be registered**: `ResourceRegistrySystem` requires `ResourceTypeId` component
2. **Baker skips empty types**: The baker only adds `ResourceTypeId` if the string is not empty
3. **Vessels can't find targets**: No resources in registry = no targets for vessels
4. **Villagers can't get jobs**: No resources in registry = no job assignments

## Required Values

**You MUST set these values in the Unity Inspector for each resource node:**

- **Wood nodes** (ResourceNode_Wood1, Wood2, Wood3): `resourceTypeId = "wood"`
- **Ground ore nodes** (OreNode1, OreNode2): `resourceTypeId = "ore"`
- **Asteroids** (Asteroid_Ore1, Ore2, Ore3): `resourceTypeId = "ore"`

## Resource Type Catalog

The resource types "wood" and "ore" **must** be registered in the `PureDotsRuntimeConfig` asset assigned to `PureDotsConfigAuthoring` in the scene.

To verify:
1. Select `PureDOTSConfig` GameObject in scene
2. Check `PureDotsConfigAuthoring` component
3. Ensure `config` asset has `ResourceTypes` with entries for "wood" and "ore"

## If Values Keep Getting Cleared

If `resourceTypeId` keeps becoming empty:
- Check if there's a script modifying it
- Check Unity's prefab override system isn't resetting it
- Verify the scene file isn't being reverted by version control

## Debugging

Check Unity Console for `[ResourceCatalogDebug]` messages:
- If catalog not found: Check `PureDotsConfigAuthoring` setup
- If no resources registered: Check `resourceTypeId` values are set correctly
- If types not in catalog: Add "wood" and "ore" to the config asset

