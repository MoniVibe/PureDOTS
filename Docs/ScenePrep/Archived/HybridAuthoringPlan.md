# Hybrid Authoring Pass

This pass inventories the prefabs that will feed the hybrid showcase scene and documents the authoring components already in the project.

## Godgame Assets

| Prefab Path | Authoring Component | Notes |
|-------------|--------------------|-------|
| `Assets/PureDOTS/Prefabs/Villager.prefab` | `Godgame.Authoring.VillagerAuthoring` | Handles base identity + needs. |
| `Assets/PureDOTS/Prefabs/VillagerSpawner.prefab` | `Godgame.Authoring.VillageSpawnerAuthoring` | Configurable spawn counts + reproduction rate. |
| `Assets/PureDOTS/Prefabs/Storehouse.prefab` | `Godgame.Authoring.StorehouseAuthoring` | Provides storage + resource registry link. |
| `Assets/PureDOTS/Prefabs/ResourceNode.prefab` | `Godgame.Authoring.ResourceNodeAuthoring` | Supplies harvest definitions for villagers. |

Verification steps:

1. Load each prefab in the Unity inspector and confirm the expected authoring component is present.
2. Ensure each prefab references the correct render mesh / material pairing for URP.
3. Run Play Mode with `VillagerSpawnerAuthoring` to confirm conversion completes (no console errors).

## Space4X Assets

| Prefab Path | Authoring Component | Notes |
|-------------|--------------------|-------|
| `Assets/Space4X/Prefabs/Carrier.prefab` | `Space4X.Authoring.Space4XCarrierAuthoring` | Fleet transports for mining drones. |
| `Assets/Space4X/Prefabs/MiningVessel.prefab` | `Space4X.Authoring.Space4XMiningVesselAuthoring` | Primary miner logic. |
| `Assets/Space4X/Prefabs/AsteroidNode.prefab` | `Space4X.Authoring.Space4XMiningDemoAuthoring` | Provides resource node definitions. |
| `Assets/Space4X/Prefabs/CameraRig.prefab` | `Space4X.Authoring.Space4XCameraAuthoring` + `Space4XCameraInputAuthoring` | Feeds RTS camera systems. |

Verification steps:

1. Load each prefab and confirm authoring fields are populated (profiles, config assets, visual prefabs).
2. Ensure the prefabs are assigned to the `Space4X.Editor` validation utilities (`ValidateSpace4XMaterials`).
3. Bake the prefabs into a temporary subscene to confirm conversion passes.

## Presentation Hooks

- Update `Packages/com.moni.puredots/Runtime/Authoring/PresentationRegistryAsset.cs` to register visuals for both factions.
- Confirm `PresentationSpawnSystem` has entries for villager mesh, storehouse mesh, carrier hull, and mining laser effects.
- Capture references to URP materials within the presentation registry so both subscenes share the same render pipeline assets.

Once each checklist item above is validated, mark the "Authoring & Prefabs" section in `HybridShowcaseChecklist.md` as complete.


