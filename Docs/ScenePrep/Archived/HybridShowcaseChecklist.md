# Hybrid Showcase Checklist

Centralizes the steps, owners, and assets required to present Godgame and Space4X side by side in a single DOTS scene.  Use the table below to assign status as pieces come online.

## 1. Scene & SubScene Layout

- [x] Created `HybridShowcaseBootstrap` MonoBehaviour for scene orchestration.
- [x] Created `HybridSceneSetupInstructions.md` with step-by-step Unity Editor guide.
- [ ] Create `Assets/Scenes/Hybrid/HybridShowcase.unity` as the entry point (follow instructions doc).
- [ ] Add two `SubScene` game objects ("Godgame SubScene", "Space4X SubScene") per instructions.
- [ ] Configure both subscenes to convert into the default `PureDotsWorldBootstrap` world.
- [x] `HybridControlToggleSystem` registered in `InitializationSystemGroup` (auto-discovered).
- [x] `HybridControlToggleAuthoring` component ready for attachment to root GameObject.

## 2. Authoring Prefabs

- [x] Godgame prefabs exist under `Assets/PureDOTS/Prefabs/` (Villager, Storehouse, ResourceNode, VillagerSpawner).
- [x] Created `PrefabCreationGuide.md` with instructions for Space4X prefabs.
- [ ] Space4X prefabs need to be created in Unity Editor (Carrier, MiningVessel, AsteroidNode) - follow guide.
- [x] Presentation registry structure documented in setup instructions.
- [ ] Presentation registry asset needs to be created and populated (follow `HybridSceneSetupInstructions.md` section 6).
- [ ] Bake authoring prefabs to verify there are no conversion errors (deferred to validation phase).

## 3. Runtime Bootstrap

- [x] Created `HybridShowcaseBootstrap` MonoBehaviour (`Assets/Scripts/HybridShowcaseBootstrap.cs`).
- [x] Bootstrap handles input mode initialization and spawn triggering.
- [ ] Attach `HybridShowcaseBootstrap` to root GameObject in scene (follow setup instructions).
- [ ] Ensure `PureDotsConfigAuthoring` components reference appropriate runtime configs in both subscenes.
- [x] System group ordering verified: `HybridControlToggleSystem` runs in `InitializationSystemGroup` before input systems.

## 4. Input & Camera Integration

- [x] Added `HybridControlCoordinator` for shared mode switching.
- [x] Added `HybridControlToggleSystem` bound to <kbd>F9</kbd> for cycling control modes.
- [x] Updated `Space4XCameraInputSystem` to respect the active input mode.
- [x] Updated `GodgameCameraInputBridge` to respect the active input mode.
- [ ] Authoring/UI toggle for designers (button or HUD overlay) to expose mode switching in scene.

## 5. Spawn Logic

- [x] Spawn coordinates documented in `HybridSpawnConfig.md` (Godgame left/negative X, Space4X right/positive X).
- [x] `HybridShowcaseBootstrap` can trigger spawns via `TriggerSpawns()` method.
- [x] Setup instructions include spawn authoring placement (see `HybridSceneSetupInstructions.md` section 4-5).
- [ ] Configure spawn authoring in Unity Editor per documented coordinates.
- [ ] Validate resource nodes spawn in correct halves (deferred to validation phase).

## 6. Validation & QA

**Note**: Validation steps deferred to user per plan scope.

- [ ] Run Entities bake with both subscenes active; address conversion errors.
- [ ] Playmode smoke: confirm camera toggle + divine hand toggle operate without conflicting bindings.
- [ ] Verify villagers gather resources and Space4X vessels mine simultaneously.
- [ ] Capture screenshots / clips for documentation.
- [ ] Update this checklist and link results in `Docs/Progress.md`.

> Tip: Use `HybridControlCoordinator.CycleMode()` via the console to script mode changes in automated tests. System registration can be verified via console log on first frame.

## Implementation Status Summary

✅ **Completed Programmatically**:
- Input coordination system (`HybridControlCoordinator` + `HybridControlToggleSystem`)
- Bootstrap MonoBehaviour (`HybridShowcaseBootstrap`)
- Documentation (setup instructions, prefab guide, spawn config)
- System registration verification (console logging)

⏳ **Requires Unity Editor Work**:
- Scene file creation (`HybridShowcase.unity` + subscenes)
- Space4X prefab authoring (Carrier, MiningVessel, AsteroidNode)
- Presentation registry asset creation and population
- Spawn authoring placement in subscenes
- Runtime config assignment

See `HybridGapAnalysis.md` for detailed breakdown and `HybridSceneSetupInstructions.md` for step-by-step Unity Editor guide.


