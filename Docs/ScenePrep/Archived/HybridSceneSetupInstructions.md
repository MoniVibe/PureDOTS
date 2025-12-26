# Hybrid Showcase Scene Setup Instructions

Step-by-step guide to create the Unity scene file and wire up all components. Execute these steps in the Unity Editor.

## 1. Create Scene File

1. In Unity Editor, go to `Assets/Scenes/Hybrid/`
2. Right-click → Create → Scene
3. Name it `HybridShowcase.unity`
4. Save the scene

## 2. Add Root Bootstrap GameObject

1. In the Hierarchy, create an empty GameObject named "HybridBootstrap"
2. Add component: `HybridShowcaseBootstrap` (from `PureDOTS/Assets/Scripts/`)
3. Add component: `HybridControlToggleAuthoring` (from `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/Hybrid/`)
4. Set `HybridShowcaseBootstrap.defaultInputMode` to `Dual` (or desired default)

## 3. Create SubScenes

### Godgame SubScene

1. Create empty GameObject named "Godgame SubScene"
2. Add component: `SubScene` (Unity Entities package)
3. Create a new scene: Right-click `Godgame SubScene` → New Sub Scene
4. Name it `GodgameShowcase_SubScene.unity`
5. Save it to `Assets/Scenes/Hybrid/`
6. In the SubScene component, assign the scene asset
7. Set `Auto Load Scene` to true
8. Set `Scene Reference` to reference the subscene asset

### Space4X SubScene

1. Create empty GameObject named "Space4X SubScene"
2. Add component: `SubScene` (Unity Entities package)
3. Create a new scene: Right-click `Space4X SubScene` → New Sub Scene
4. Name it `Space4XShowcase_SubScene.unity`
5. Save it to `Assets/Scenes/Hybrid/`
6. In the SubScene component, assign the scene asset
7. Set `Auto Load Scene` to true
8. Set `Scene Reference` to reference the subscene asset

## 4. Populate Godgame SubScene

Open `GodgameShowcase_SubScene.unity`:

1. **Village Spawner** (left side, negative X):
   - Create empty GameObject at position `(-120, 0, 0)`
   - Add component: `VillageSpawnerAuthoring`
   - Assign `VillagerPrefab` = `Assets/PureDOTS/Prefabs/Villager.prefab`
   - Set `VillagerCount` = 8
   - Set `SpawnRadius` = 25

2. **Storehouses**:
   - Instantiate `Assets/PureDOTS/Prefabs/Storehouse.prefab` at `(-140, 0, 40)`
   - Instantiate `Assets/PureDOTS/Prefabs/Storehouse.prefab` at `(-100, 0, -35)`

3. **Resource Nodes**:
   - Instantiate `Assets/PureDOTS/Prefabs/ResourceNode.prefab` at 6 positions:
     - `(-140, 0, 20)`
     - `(-100, 0, 30)`
     - `(-130, 0, -25)`
     - `(-90, 0, -40)`
     - `(-150, 0, -10)`
     - `(-110, 0, 50)`
   - Configure each `ResourceNodeAuthoring` with appropriate resource types

4. **PureDOTS Config**:
   - Create empty GameObject named "PureDOTS Config"
   - Add component: `PureDotsConfigAuthoring`
   - Assign `config` = `Assets/PureDOTS/Config/PureDotsRuntimeConfig.asset`

5. **Presentation Registry**:
   - Create empty GameObject named "Presentation Registry"
   - Add component: `PresentationRegistryAuthoring`
   - Create asset: Right-click in Project → Create → PureDOTS → Presentation → Registry
   - Name it `HybridPresentationRegistry.asset`
   - Populate descriptors (see Presentation Registry section below)
   - Assign to `PresentationRegistryAuthoring.registry`

## 5. Populate Space4X SubScene

Open `Space4XShowcase_SubScene.unity`:

1. **Mining legacy Authoring** (right side, positive X):
   - Create empty GameObject at position `(120, 0, 20)`
   - Add component: `Space4XMiningDemoAuthoring`
   - Add component: `PureDotsConfigAuthoring` (required dependency)
   - Add component: `SpatialPartitionAuthoring` (required dependency)
   - Configure carriers/vessels/asteroids in inspector (see `HybridSpawnConfig.md` for coordinates)

2. **Camera Setup**:
   - Create empty GameObject at position `(125, 30, -10)` named "Space4X Camera Rig"
   - Add component: `Space4XCameraAuthoring`
   - Add component: `Space4XCameraInputAuthoring`
   - Assign `InputActions` = `Assets/InputSystem_Actions.inputactions` (from Space4X project)
   - Assign camera profile if needed

3. **PureDOTS Config**:
   - Create empty GameObject named "PureDOTS Config"
   - Add component: `PureDotsConfigAuthoring`
   - Assign `config` = `Assets/Space4X/Config/PureDotsRuntimeConfig.asset` (or shared config)

## 6. Presentation Registry Setup

Open `HybridPresentationRegistry.asset`:

Add the following descriptor entries:

| Descriptor Key | Prefab | Default Offset | Default Scale |
|----------------|--------|----------------|---------------|
| `godgame.villager` | `Assets/PureDOTS/Prefabs/Villager.prefab` | (0,0,0) | 1.0 |
| `godgame.storehouse` | `Assets/PureDOTS/Prefabs/Storehouse.prefab` | (0,0,0) | 1.0 |
| `godgame.resource_node` | `Assets/PureDOTS/Prefabs/ResourceNode.prefab` | (0,0,0) | 1.0 |
| `space4x.carrier` | `Assets/Space4X/Prefabs/Carrier.prefab` | (0,0,0) | 1.0 |
| `space4x.mining_vessel` | `Assets/Space4X/Prefabs/MiningVessel.prefab` | (0,0,0) | 1.0 |
| `space4x.asteroid` | `Assets/Space4X/Prefabs/AsteroidNode.prefab` | (0,0,0) | 1.0 |

**Note**: Space4X prefabs may need to be created first (see Prefab Creation section).

## 7. Verify System Registration

1. Enter Play Mode
2. Open `Window → Entities → Systems` (or check console logs)
3. Confirm `HybridControlToggleSystem` appears in `InitializationSystemGroup`
4. Press `F9` and verify console log shows mode switching

## 8. Runtime Config Notes

- Both subscenes can reference the same `PureDotsRuntimeConfig` asset if resources are merged
- Alternatively, create separate configs and ensure both load at runtime
- `PureDotsRuntimeConfigLoader` (MonoBehaviour) can be added to main scene if runtime loading is needed

## Troubleshooting

- **SubScenes not converting**: Ensure `Auto Load Scene` is enabled and scene assets are assigned
- **Prefabs missing**: Check that prefab paths exist; create Space4X prefabs if needed (see gap analysis)
- **Systems not running**: Verify `PureDotsWorldBootstrap` is active (check Project Settings → Player → Scripting Define Symbols)
- **Input not working**: Ensure `InputSystem_Actions.inputactions` has "Camera" action map for Space4X and "Player" action map for Godgame

