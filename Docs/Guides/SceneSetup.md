# Scene Setup Source of Truth

This document defines how every gameplay / validation scene in **PureDOTS** must be structured. Keep it handy whenever you create or edit a scene so we remain deterministic and aligned with TruthSource rules.

---

## 1. Naming & Location

| Scene Type           | Location                                           | Naming Pattern                                 |
|----------------------|----------------------------------------------------|------------------------------------------------|
| Gameplay / Prototype | `Assets/Scenes/<Feature>/<FeatureScene>.unity`     | `<FeatureScene>.unity`                         |
| Validation / QA      | `Assets/Scenes/Validation/<Scenario>.unity`        | `ScenarioName.unity`                           |
| SubScenes            | `Assets/Scenes/Validation/SubScenes/<Scene>.unity` | `<ParentScene>.SubScene.unity`                 |
| Shared Prefabs       | `Assets/Scenes/Validation/Prefabs/`                | Domain-specific                                |

Always keep the SubScene asset in the same folder tree as the parent scene to avoid GUID conflicts.

---

## 2. Root Hierarchy Template

Every scene begins with:

```
SceneName (root)  <-- plain GameObject
 ├─ BootstrapConfig      (MonoBehaviour layer)
 ├─ SandboxHUD / UI      (Canvas + debug readers)
 ├─ TimelineOverlay      (Rewind overlay)
 ├─ SimulationSubScene   (SubScene with DOTS entities)
 └─ Optional helpers     (Lighting, cameras, ground, etc.)
```

### 2.1 BootstrapConfig
- Add **PureDotsConfigAuthoring** (drag the script if it does not show up in the add-component menu).
- Assign `Assets/PureDOTS/Config/PureDotsRuntimeConfig.asset`.
- This seeds `TimeState`, `HistorySettings`, resource catalogs, and any other runtime singletons. Even if the SubScene contains the same data, the config is the canonical source.

### 2.2 Simulation SubScene
1. Select the GameObjects you want converted to entities (villagers, resources, storehouses, etc.).
2. Run **GameObject → Convert To Entity → Convert and Save As SubScene** and save as `Assets/Scenes/.../<Parent>.SubScene.unity`.
3. Confirm the SubScene appears in green and has a valid `.entities` cache (no warnings in Console).
4. Keep SubScene authoring GameObjects lean—only DOTS authoring components (`*Authoring`, `Baker`, `ConvertToEntity`) and Transform/Gizmos helpers belong there.

Recommended placements (world space metres):

| Object     | Position (x,z) | Authoring                                                     |
|------------|----------------|----------------------------------------------------------------|
| Villager   | `(-4,0)`       | Villager prefab (`Assets/PureDOTS/Prefabs/Villagers/...`) or authoring equivalent. |
| Resource   | `(0,0)`        | `ResourceSourceAuthoring` with valid `resourceTypeId`.        |
| Storehouse | `(6,0)`        | `StorehouseAuthoring` with capacity entries.                  |

Add **Entity** conversion (`Convert and Destroy`) if the prefab/authoring object does not already include it.

### 2.3 HUD & Overlay
- Add a Canvas (`Screen Space - Overlay`) named `SandboxHUD`.
  - Attach `DebugDisplayReader` and wire time/rewind/resource text fields if desired.
- Add a root-level GameObject `TimelineOverlay` with `RewindTimelineDebug`.
  - Keep the default window position (20, 20) and size unless the scene requires adjustments.
- Ensure an `EventSystem` exists for UI.

### 2.4 Lighting, Camera, Ground
- Use the default `Main Camera` targeted at the action (e.g. position `(0, 5, -12)`, look at `(0,0,0)`).
- A simple plane/cube can act as the ground (optional but helps orientation).
- Reuse template lighting settings unless the feature requires custom lights/shadows.

### 2.5 Placeholder Visuals (DOTS)
- Add `PlaceholderVisualAuthoring` to every surrogate mesh that will be converted into the SubScene. Choose the `Visual Type` matching the gameplay entity (crate for storehouses/resources, barrel for logistics props, miracle, vegetation).
- Keep meshes primitive: Unity cube for crates, cylinder for barrels, low-poly sphere/capsule for villagers, and simple cross-plane or cone for vegetation. Assign a URP/Lit material so Entities Graphics can drive base/emission colors.
- Miracles: enable emission on the material; the `Miracle Placeholder Pulse System` will animate both base color and emission for a lightweight glow VFX. Keep base intensity around `1`, pulse amplitude `0.3-0.5`, and pulse speed `2-3`.
- Vegetation: leave the GameObject scaled to `Base Scale`. `PlaceholderVegetationScale` values are multipliers that the DOTS system uses to grow/shrink the entity as lifecycle stages advance. Seedling defaults to `0.25`, mature to `1.0`, dying to `0.8`, dead `0.45`.
- After conversion the SubScene should contain only entities; the DOTS systems will handle scaling and pulsing automatically without any MonoBehaviours at runtime. Re-run **Convert and Save As SubScene** whenever you tweak placeholder parameters.

### 2.6 Scene Spawner Profiles
- Drop a `SceneSpawnAuthoring` GameObject in `SimulationSubScene` whenever you need bulk placement (villagers, vegetation, miracles, animals, etc.).
- Create a `SceneSpawnProfileAsset` (`Create → PureDOTS → Scene Spawn Profile`) and assign it to the authoring component. Each profile entry maps to a prefab, spawn category, placement (point, circle, ring, grid, or custom points), rotation mode, and optional payload tags.
- Use the profile seed (or per-entry seed offsets) to keep layouts deterministic. Random modes rely on the seed so rewind/replay stays aligned, and re-running conversion preserves placement.
- For custom points, author local offsets in the asset to describe handcrafted layouts—ideal for plazas or scripted events. Grid and ring helpers are faster for generic villages or forests.
- On play, `SceneSpawnSystem` instantiates everything once in `InitializationSystemGroup` and tags the controller. Add or edit entries, reconvert the SubScene, and the system will rebuild the layout automatically.

### 2.7 Divine Hand & Rain Miracles
- Add `DivineHandAuthoring` to the GameObject that represents the player's cursor/hand. The baker seeds `DivineHandTag`, config, and input/state components—Coplay or gameplay code can write to `DivineHandInput` each frame (cursor position, aim, grab/throw booleans).
- Objects that should be grab-able must include `HandPickable` (added automatically by `RainCloudAuthoring`). While held, the hand lerps the object's transform to follow the cursor and zeros it out when released.
- Use `RainCloudAuthoring` on your rain cloud prefab. It sets `RainCloudConfig`, default drift velocity, moisture broadcast parameters, and registers the entity as `HandPickable` so the hand can carry and throw it.
- Drop a `RainMiracleAuthoring` in the scene (or convert it into a prefab/config singleton). The config stores which rain-cloud prefab to instantiate and how many clouds the miracle should create.
- Rain miracles are queued via the global `RainMiracleCommand` buffer (`RainMiracleCommandQueue`). Gameplay systems—or Coplay for debugging—append commands; `RainMiracleSystem` instantiates clouds around the target position, and `RainCloudMoistureSystem` hydrates vegetation under them.
- `DivineHandAuthoring` now exposes cooldown/charge and capacity fields—keep defaults aligned with `Hand_StateMachine.md` unless balancing a prototype.
- Add `HandCameraInputRouter` + `DivineHandInputBridge` on the camera rig (or a dedicated GameObject) so right-click routing, cursor rays, and camera/hand actions flow through `Assets/InputSystem_Actions.inputactions`. The bridge now feeds the DOTS `HandInputRouterSystem`, which resolves priorities before `DivineHandSystem` runs. Configure `interactionMask` / `groundMask` according to `Layers_Tags_Physics.md`.
- Optional: attach `DivineHandEventBridge` to surface hand state/type/amount events to HUD/UI. `DotsDebugHUD` subscribes automatically and shows the latest state/amount snapshot.

---

## 3. Required Components & Scripts

| Component / Script                  | Purpose                                         |
|-------------------------------------|-------------------------------------------------|
| PureDotsConfigAuthoring             | Seeds runtime singletons & catalogs.            |
| ResourceSourceAuthoring             | Converts authored resource nodes to entities.   |
| StorehouseAuthoring                 | Defines storehouse capacities & queues.         |
| VillagerAuthoring / Prefab          | Adds villager data & conversion pipeline.       |
| DebugDisplayReader                  | HUD for time/registry data.                     |
| RewindTimelineDebug                 | Displays current record/playback/catch-up state.|
| HandCameraInputRouter               | Hybrid pointer/right-click router feeding DOTS `HandInputRouterSystem`. |
| DivineHandInputBridge               | Pushes router signals into `DivineHandInput` and manages RMB phases. |
| DivineHandEventBridge               | Bridges DOTS hand events to UnityEvents/HUD listeners. |
| Optional: OverrideAutomaticNetcodeBootstrap | Disable NetCode bootstrap when using our custom world. |

**Important:** The `PureDOTS.Authoring` asmdef must NOT be editor-only; otherwise MonoBehaviours cannot be attached. Verify `Assets/Scripts/PureDOTS/Authoring/PureDOTS.Authoring.asmdef` has an empty “Include Platforms” list.

---

## 4. Rewind Sandbox Minimum Checklist

Before committing a new/updated validation scene:

1. Scene loads without Console errors or “entity header” warnings.
2. SubScene assets (`*.SubScene.unity` and `*.entities`) exist and reimport cleanly.
3. Holding the rewind input switches overlay mode to Playback → CatchUp → Record.
4. Villager job loop, resource registry, and storehouse registry all reset deterministically when rewound.
5. `Docs/QA/SinglePlayerRewind.md` smoke test passes (use the checklist there).

---

## 5. QA & Automation Notes

- For every new validation scene, file an entry in `Docs/QA/` describing its purpose and the manual test flow.
- Consider adding PlayMode tests that use the same setup to keep behaviour deterministic across refactors.
- Keep scenes small: prefer one feature per validation scene to isolate issues.

---

By following this template we ensure every scene matches project expectations, stays deterministic, and remains ready for automated testing. Update this document whenever the process changes.***
