# Hybrid Spawn Configuration

The showcase requires deterministic spawn locations so both games display clearly within one camera frame.  Use the reference numbers below when authoring the hybrid scene.

## Layout Guidelines

- **Origin Split:** Treat world origin `(0,0,0)` as the seam.  Place Godgame content on the negative X axis and Space4X content on the positive X axis.
- **Scale:** Keep both sandboxes within a 250m cube so the shared camera rigs do not need extreme clipping planes.
- **Nav Separation:** Leave a 50m buffer between sandboxes to avoid villagers wandering into the Space4X mining area.

## Godgame Spawn Defaults

| Entity | Count | Position | Notes |
|--------|-------|----------|-------|
| Village Hub | 1 | `(-120, 0, 0)` | Use `VillageSpawnerAuthoring` for initial structures. |
| Villagers | 8 | Random within 25m of hub | Reference `VillagerSpawnerAuthoring`. |
| Storehouses | 2 | `(-140,0,40)` and `(-100,0,-35)` | Use prefab `Storehouse.prefab`. |
| Resource Nodes | 6 | Spread in 60m radius | `ResourceNodeAuthoring` populates harvest data. |

## Space4X Spawn Defaults

| Entity | Count | Position | Notes |
|--------|-------|----------|-------|
| Carrier | 1 | `(120, 0, 20)` | Parent of mining vessels; uses `Space4XCarrierAuthoring`. |
| Mining Vessels | 4 | `(140 ± 15, 0, 25 ± 15)` | Use `Space4XMiningVesselAuthoring` with active mining orders. |
| Asteroid Nodes | 6 | `(160 ± 20, 0, 40 ± 20)` | Provide resource output balancing Godgame nodes. |
| Camera Rig | 1 | `(125, 30, -10)` | `Space4XCameraAuthoring` plus input authoring. |

## Presentation Registry Entries

| Registry Id | Prefab | Notes |
|-------------|--------|-------|
| `godgame.villager` | `Assets/PureDOTS/Prefabs/Villager.prefab` | Animated villager mesh. |
| `godgame.storehouse` | `Assets/PureDOTS/Prefabs/Storehouse.prefab` | Stores resources. |
| `space4x.carrier` | `Assets/Space4X/Prefabs/Carrier.prefab` | Large fleet mothership. |
| `space4x.mining_vessel` | `Assets/Space4X/Prefabs/MiningVessel.prefab` | Drones for mining. |
| `space4x.asteroid` | `Assets/Space4X/Prefabs/AsteroidNode.prefab` | Visuals for resource nodes. |

Record the actual GUIDs in `PresentationRegistryAsset` after hooking the prefabs.  Ensure both spawn sets reference the same lighting profile (`Assets/Settings/DefaultVolumeProfile.asset`).


