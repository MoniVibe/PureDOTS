# Critical Resource Setup - Why resourceTypeId is Required

## Problem: Empty resourceTypeId = No Movement

If `resourceTypeId` is empty in `ResourceSourceAuthoring` components, the following happens:

1. **Baker skips ResourceTypeId component**: The `ResourceSourceBaker` only adds `ResourceTypeId` if the string is not empty (line 61-65 in ResourceAuthoring.cs)
2. **ResourceRegistrySystem skips resources**: The registry system requires `ResourceTypeId` component (line 28 in ResourceRegistrySystem.cs)
3. **Vessels can't find targets**: `VesselAISystem` queries the resource registry, but if resources aren't registered, the registry is empty
4. **No movement**: Vessels stay idle because they have no targets

## Solution: Set resourceTypeId Values

**CRITICAL**: You MUST set `resourceTypeId` on all resource nodes for them to be registered and usable:

- **Wood nodes**: `resourceTypeId: wood`
- **Ore nodes (ground)**: `resourceTypeId: ore`  
- **Asteroids**: `resourceTypeId: ore`

**DO NOT leave resourceTypeId empty** - it will break the entire mining system!

## Resource Type Registration

The resource types "wood" and "ore" need to be registered in the `ResourceTypeIndex` catalog. This typically happens:
- Automatically when resources are baked (if using a catalog system)
- Or through a bootstrap/system that registers known types

If types aren't recognized, check the Unity Console for warnings about unknown resource types.

