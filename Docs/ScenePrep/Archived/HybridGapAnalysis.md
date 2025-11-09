# Hybrid Showcase Gap Analysis

Inventory of missing pieces to complete the hybrid showcase scene, with brief summaries of what's needed to close each gap.

## Completed Items ✓

- **Input Coordination System**: `HybridControlCoordinator` + `HybridControlToggleSystem` implemented and wired to both camera systems
- **Authoring Bridge**: `HybridControlToggleAuthoring` MonoBehaviour ready for UI hooks
- **Documentation**: Checklists and planning docs created

## Missing Items & How to Close

### 1. Scene Infrastructure

**Missing**: Unity scene file (`Assets/Scenes/Hybrid/HybridShowcase.unity`) with SubScene layout

**Gap Summary**: Create a new Unity scene with:
- Two `SubScene` GameObjects ("Godgame SubScene", "Space4X SubScene")
- Root GameObject with `HybridControlToggleAuthoring` component
- Both subscenes configured to convert into default `PureDotsWorldBootstrap` world
- Optional UI Canvas with button/text to visualize current control mode

**Action**: Use Unity Editor to create the scene structure; subscenes can start empty and be populated later.

---

### 2. Space4X Prefabs

**Missing**: Prefab assets for Space4X entities (carrier, mining vessel, asteroid node)

**Gap Summary**: Authoring components exist (`Space4XCarrierAuthoring`, `Space4XMiningVesselAuthoring`, `Space4XMiningDemoAuthoring`) but no prefabs reference them yet.

**Action**:
- Create `Assets/Space4X/Prefabs/Carrier.prefab` with `Space4XCarrierAuthoring` + visual mesh
- Create `Assets/Space4X/Prefabs/MiningVessel.prefab` with `Space4XMiningVesselAuthoring` + visual mesh
- Create `Assets/Space4X/Prefabs/AsteroidNode.prefab` with `Space4XMiningDemoAuthoring` + visual mesh
- Place all prefabs in SubScene or reference from spawn authoring

---

### 3. Presentation Registry Entries

**Missing**: `PresentationRegistryAsset` entries mapping both factions' visuals

**Gap Summary**: The registry asset exists but needs entries for:
- `godgame.villager` → Villager prefab mesh
- `godgame.storehouse` → Storehouse prefab mesh
- `space4x.carrier` → Carrier prefab mesh
- `space4x.mining_vessel` → Mining vessel prefab mesh
- `space4x.asteroid` → Asteroid resource node mesh

**Action**: Open/create `PresentationRegistryAsset` in `Assets/PureDOTS/Config/` and populate descriptor list. Assign registry asset to a `PresentationRegistryAuthoring` component in the scene.

---

### 4. Bootstrap Authoring Component

**Missing**: `HybridShowcaseBootstrap` MonoBehaviour to trigger initial spawns

**Gap Summary**: Need a MonoBehaviour that:
- References spawn profiles for both games
- Invokes spawn requests on scene load (via `VillagerSpawnerSystem` for Godgame, equivalent for Space4X)
- Can be attached to root GameObject in showcase scene

**Action**: Create `PureDOTS/Assets/Scripts/Editor/HybridShowcaseBootstrap.cs` that implements `MonoBehaviour.Start()` to trigger spawns via ECS singleton commands or direct system access.

---

### 5. Runtime Config Unification

**Missing**: Single `PureDotsRuntimeConfigLoader` that loads both Godgame and Space4X resource catalogs

**Gap Summary**: Current loader is Space4X-specific. Need to merge or extend it to handle both resource type catalogs simultaneously.

**Action**: Update `PureDotsRuntimeConfigLoader` to:
- Accept multiple `PureDotsRuntimeConfig` assets (or a unified asset)
- Load both `ResourceTypeIndex` entries (merge IDs if needed)
- Load both `ResourceRecipeSet` entries (merge recipe families)

Alternatively, create a shared config asset that combines both games' resources.

---

### 6. UI/HUD Visualization

**Missing**: Visual feedback for active control mode (button, HUD text, or overlay)

**Gap Summary**: Designers need visual confirmation when `F9` switches modes. A simple UI element showing "Dual / Space4X Only / Godgame Only" would suffice.

**Action**: Add a UI Canvas to the showcase scene with:
- Text component that listens to `HybridControlCoordinator.ModeChanged` event
- Or a button that calls `HybridControlToggleAuthoring.CycleMode()`
- Position overlay in top-right corner to avoid gameplay interference

---

### 7. Spawn Configuration

**Missing**: Scene-authoring setup for spawn locations per `HybridSpawnConfig.md` guidelines

**Gap Summary**: Need to place `VillagerSpawnerAuthoring` GameObjects on left side (negative X) and Space4X spawners on right side (positive X) according to documented coordinates.

**Action**:
- Place Godgame spawners at `(-120, 0, 0)` with villagers/stores/resources in left half
- Place Space4X spawners at `(120, 0, 20)` with carriers/miners/asteroids in right half
- Ensure spawn radii don't overlap (50m buffer recommended)

---

### 8. System Registration Verification

**Missing**: Confirmation that `HybridControlToggleSystem` is auto-discovered

**Gap Summary**: Systems using `[UpdateInGroup]` should auto-register via `DefaultWorldInitialization`, but should verify it appears in the world.

**Action**: Add a debug log in `HybridControlToggleSystem.OnCreate()` to confirm registration, or check Entities window at runtime to see system list.

---

### 9. Validation & Testing

**Missing**: All validation steps from `HybridValidationPlan.md`

**Gap Summary**: No conversion bake runs, playmode tests, or documentation captures have been performed yet.

**Action**: Follow validation checklist once scene is assembled:
- Run Entities bake with both subscenes
- Test `F9` mode switching in playmode
- Verify both gameplay loops run simultaneously
- Capture screenshots/clips for docs

---

## Priority Order

1. **Scene Infrastructure** (enables all other work)
2. **Space4X Prefabs** (required for Space4X side to function)
3. **Presentation Registry** (needed for visuals to render)
4. **Spawn Configuration** (needed for entities to appear)
5. **Bootstrap Component** (triggers spawns at runtime)
6. **Runtime Config Unification** (ensures both games' resources load)
7. **UI Visualization** (quality-of-life for designers)
8. **System Registration Check** (verification step)
9. **Validation & Testing** (final QA pass)

---

## Estimated Effort

- **Quick Wins** (< 30 min): Scene file creation, UI overlay, system registration check
- **Medium** (1-2 hours): Space4X prefabs, presentation registry entries, spawn configuration
- **Complex** (2-4 hours): Bootstrap component, runtime config unification, full validation pass

**Total estimated time to completion**: 4-7 hours of focused Unity editor work.

