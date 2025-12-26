# Configure Godgame Prefabs for legacy Scene

## Objective
Create reusable prefabs for villagers and storehouses that can be placed in scenes and instantiated at runtime. These prefabs should integrate with the Godgame registry bridge system and support incremental feature development.

## Requirements

### 1. Individual Authoring Components
- Create `VillagerAuthoring` component that wraps `GodgameVillager` data for individual prefabs
- Create `StorehouseAuthoring` component that wraps `GodgameStorehouse` data for individual prefabs
- Both should use DOTS baking to convert to runtime entities
- Each prefab should be able to be instantiated independently at runtime

### 2. Prefab Structure
- **Villager Prefab**:
  - GameObject with `VillagerAuthoring` component
  - Visual representation (simple primitive or placeholder mesh)
  - Transform positioned at origin (will be moved at runtime)
  - Should bake into entity with `GodgameVillager`, `LocalTransform`, and `SpatialIndexedTag` components

- **Storehouse Prefab**:
  - GameObject with `StorehouseAuthoring` component
  - Visual representation (simple primitive or placeholder mesh)
  - Transform positioned at origin
  - Should bake into entity with `GodgameStorehouse`, `LocalTransform`, and `SpatialIndexedTag` components

### 3. legacy Scene Setup
- Create or update a legacy scene (`Assets/Scenes/GodgameDemoScene.unity`) that:
  - Contains a SubScene with some prefab instances placed for visual verification
  - Can serve as a foundation for incremental feature additions
  - Includes clear visual separation between villagers and storehouses
  - Includes basic lighting and camera setup

### 4. Runtime Instantiation Support
- Prefabs should be marked as prefabs (not just scene instances)
- Enable runtime instantiation via Entity prefab references
- Ensure prefabs register properly with the registry bridge system

## Implementation Notes

- Follow DOTS baking patterns similar to `DivineHandAuthoring`
- Use `GetEntity(TransformUsageFlags.Dynamic)` for prefab root entities
- Ensure all required components (`GodgameVillager`/`GodgameStorehouse`, `LocalTransform`, `SpatialIndexedTag`) are added during baking
- Default values should match realistic game state (e.g., villagers with reasonable health/morale, storehouses with capacity)
- Visual representations can be simple primitives (Capsule for villagers, Cube for storehouses) - these can be replaced with proper models later

## Success Criteria

- [ ] `VillagerAuthoring` component exists and bakes correctly
- [ ] `StorehouseAuthoring` component exists and bakes correctly
- [ ] Villager prefab exists in `Assets/Prefabs/Villager.prefab`
- [ ] Storehouse prefab exists in `Assets/Prefabs/Storehouse.prefab`
- [ ] legacy scene exists with prefab instances visible
- [ ] Prefabs can be instantiated at runtime and appear in registry bridge
- [ ] Scene can be played in editor and entities appear in DOTS Hierarchy

## Follow-up Tasks

After this is complete, future work can:
- Add visual models and animations to prefabs
- Implement spawning systems that use these prefabs
- Add interaction components (colliders, triggers)
- Expand prefab variants (different villager types, storehouse sizes)
- Add UI markers or gizmos for debugging

