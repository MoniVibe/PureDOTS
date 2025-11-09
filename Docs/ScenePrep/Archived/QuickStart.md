# Hybrid Showcase Quick Start

Quick reference for setting up the hybrid showcase scene. See detailed guides for complete instructions.

## Prerequisites Checklist

- [ ] Unity Editor with DOTS Entities 1.4 and New Input System packages
- [ ] PureDOTS package installed and configured
- [ ] Godgame prefabs exist (`Assets/PureDOTS/Prefabs/`)
- [ ] Space4X authoring scripts exist (`Space4x/Assets/Scripts/Space4x/Authoring/`)

## Setup Steps (30-60 minutes)

1. **Create Scene Files** (5 min)
   - Create `Assets/Scenes/Hybrid/HybridShowcase.unity`
   - Create two SubScenes: `GodgameShowcase_SubScene.unity` and `Space4XShowcase_SubScene.unity`
   - See `HybridSceneSetupInstructions.md` section 1-3

2. **Add Bootstrap** (2 min)
   - Create "HybridBootstrap" GameObject in main scene
   - Add `HybridShowcaseBootstrap` component
   - Add `HybridControlToggleAuthoring` component

3. **Create Space4X Prefabs** (15-20 min)
   - Follow `PrefabCreationGuide.md`
   - Create Carrier, MiningVessel prefabs with authoring components
   - Add visual meshes (primitives work fine for prototyping)

4. **Setup Godgame SubScene** (10 min)
   - Add `VillageSpawnerAuthoring` at `(-120, 0, 0)`
   - Place storehouses and resource nodes per `HybridSpawnConfig.md`
   - Add `PureDotsConfigAuthoring` + `PresentationRegistryAuthoring`

5. **Setup Space4X SubScene** (10 min)
   - Add `Space4XMiningDemoAuthoring` at `(120, 0, 20)`
   - Configure carriers/vessels/asteroids in inspector
   - Add camera authoring components

6. **Presentation Registry** (5 min)
   - Create `HybridPresentationRegistry.asset`
   - Add descriptor entries for all prefabs
   - Assign to registry authoring component

7. **Test** (5 min)
   - Enter Play Mode
   - Press `F9` to cycle control modes
   - Verify both sides spawn entities

## Key Controls

- **F9**: Cycle input mode (Dual → Space4X Only → Godgame Only → Dual)
- **Space4X Camera**: WASD pan, Q/E vertical, scroll zoom, right-click + mouse rotate
- **Godgame Hand**: Mouse cursor moves divine hand, click to interact

## Troubleshooting

- **Systems not running**: Check console for `HybridControlToggleSystem` registration log
- **Prefabs missing**: Verify paths in presentation registry match actual prefab locations
- **Input not working**: Ensure InputActionAsset has correct action maps ("Camera" for Space4X, "Player" for Godgame)
- **Entities not spawning**: Check that spawner authoring components are configured and prefabs assigned

## Documentation Index

- **Setup Guide**: `HybridSceneSetupInstructions.md`
- **Prefab Creation**: `PrefabCreationGuide.md`
- **Spawn Coordinates**: `HybridSpawnConfig.md`
- **Gap Analysis**: `HybridGapAnalysis.md`
- **Implementation Summary**: `ImplementationSummary.md`
- **Validation Plan**: `HybridValidationPlan.md`

