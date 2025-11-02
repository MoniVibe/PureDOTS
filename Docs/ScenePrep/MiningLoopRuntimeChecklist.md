## Mining Loop Runtime Checklist

This scene-side checklist captures everything the DOTS runtime expects in order to render the villager (GodGame) and miner vessel (Space4X) loops side by side.

### Core singletons

- `MiningVisualManifest` singleton entity with an attached `DynamicBuffer<MiningVisualRequest>`
  - Created automatically by `MiningLoopVisualBootstrapSystem` (`Packages/com.moni.puredots/Runtime/Systems/Visuals/MiningLoopVisualBootstrapSystem.cs`).
  - Scene must ensure the bootstrap system runs once (world must include `PureDOTS.Systems.TimeSystemGroup`).
- `TimeState` & `RewindState`
  - Required by both `MiningLoopVisualSyncSystem` (villagers) and `Space4XMiningLoopVisualExtensionSystem` (vessels).
  - Provided by the PureDOTS time stack; confirm the scene contains the default bootstrap (`PureDotsWorldBootstrap`).
- `MinerVesselRegistry`
  - Needed for the Space4X extension to emit vessel visual requests.
  - Created by `TransportBootstrapSystem` at startup (`Assets/Scripts/Space4x/Systems/TransportBootstrapSystem.cs`).

### Required query sources

- Villager mining loop
  - `VillagerJob` + `LocalTransform`
  - Optional: `VillagerJobProgress`, `VillagerJobTicket` (used to scale visuals / compute throughput)
  - Supplied by the GodGame simulation when gatherer villagers exist in the world.
- Miner vessel loop
  - `DynamicBuffer<MinerVesselRegistryEntry>` on the `MinerVesselRegistry` entity (populated by `TransportRegistrySystem`).
  - Each entry must carry `VesselEntity`, `Position`, `Capacity`, `Load`, and `Flags` for animation.

### Visual prefabs

- At least one entity with `MiningVisualPrefab` per `MiningVisualType` (`Villager`, `Vessel`).
- Author via `MiningLoopVisualAuthoring` (`Packages/com.moni.puredots/Runtime/Authoring/MiningLoopVisualAuthoring.cs`):
  - Place the authoring MonoBehaviour in a **SubScene** so baking runs during conversion.
  - Assign `visualPrefab` to a prefab asset containing mesh/renderer/audio as needed.
  - Ensure the target prefab is flagged as a conversion prefab (results in an entity with the `Prefab` component).
- Resulting prefab entities must include:
  - `MiningVisualPrefab { VisualType, BaseScale, Prefab }`
  - Render components (`LocalTransform`, `MaterialMeshInfo`, etc.) needed by URP Entities.

### Camera & presentation

- Disable or delete non-DOTS `MainCamera` GameObjects. `Space4XCameraRenderSyncSystem` will create/manage the runtime camera.
- The primary scene must include the `PresentationSystemGroup` (default world does). `MiningLoopVisualPresentationSystem` lives here and instantiates visuals.

### Supporting assets (validation warnings)

- `EnvironmentGridConfig` ScriptableObject at `Assets/Data/Environment/EnvironmentGridConfig.asset`.
- `link.xml` under `Assets/Config/Linker/link.xml` for IL2CPP stripping guards.

### Play mode sanity checks

1. Enter Play Mode with the combined scene.
2. Use Entities Hierarchy to confirm:
   - `MiningVisualManifest` exists with `MiningVisualRequest` buffer.
   - Two prefab entities tagged with `MiningVisualPrefab` (villager and vessel) and the `Prefab` component.
3. Observe villagers and vessels moving between their resource nodes and drop-off points. Their visual proxies should inherit positions from the `MiningVisualRequest` buffer.
4. Console should be free of "No MiningVisualPrefab" warnings and registry degradation warnings.

