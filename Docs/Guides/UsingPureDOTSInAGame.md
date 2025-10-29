# Using PureDOTS in a Game Project

This guide records the exact steps required to consume the shared **PureDOTS** package from a standalone Unity game. Follow it when wiring new projects or refreshing existing adapters (`Godgame`, `Space4x`).

## 1. Connect the Package

1. Ensure the `com.moni.puredots` folder lives inside the game repo (or a sibling workspace) so Unity can resolve it without Git submodules.
2. Open the game project and edit `Packages/manifest.json`:
   ```json
   "dependencies": {
       "com.moni.puredots": "file:../PureDOTS/Packages/com.moni.puredots"
   }
   ```
   Adjust the relative path if the workspace layout differs.
3. Save the manifest; Unity triggers a refresh and imports the package. If errors reference missing asmdefs, continue with the next section before reloading.

## 2. Wire Assembly Definitions

1. In `Assets/Scripts/<GameName>/Gameplay`, open the `<GameName>.Gameplay.asmdef` file.
2. Add references to the PureDOTS assemblies:
   - `PureDOTS.Runtime`
   - `PureDOTS.Systems`
   - `Unity.Entities`
   - Any optional domains your project uses (`Unity.Physics`, `Unity.Transforms`, etc.).
3. For editor-only tooling, extend `<GameName>.Gameplay.Editor.asmdef` with `PureDOTS.Editor`.
4. Apply and let Unity recompile. Compilation succeeds when the asmdef inspector shows no missing references.

## 3. Install Shared Config Assets

PureDOTS expects a small set of ScriptableObjects to exist in every consuming project.

1. Copy the sample assets from `PureDOTS/Assets/PureDOTS/Config/` into your game (`Assets/Config/PureDOTS/` is a good location).
2. Reference them from your bootstrap scene:
   - `PureDotsRuntimeConfig` (time, history, resource catalogs).
   - `DefaultSpatialPartitionProfile`.
   - Any domain-specific catalogs you rely on (resource types, vegetation species, etc.).
3. Use `PureDotsConfigAuthoring` and `SpatialPartitionAuthoring` MonoBehaviours in your scene root to bake the runtime singletons.
4. Confirm the SubScene baker output includes `TimeState`, `HistorySettings`, `SpatialGridConfig`, and the registry singletons when entering Play Mode.

## 4. Bridge Game Data into Shared Registries

1. Place a DOTS bridge system in `Assets/Scripts/<GameName>/Registry`. Use the existing `GodgameRegistryBridgeSystem` or `Space4XRegistryBridgeSystem` as a template.
2. Requirements:
   - Run the system after `RegistrySpatialSyncSystem` (use `[UpdateAfter(typeof(RegistrySpatialSyncSystem))]`).
   - Query authored components (villagers, storehouses, fleets, etc.) and populate `DeterministicRegistryBuilder<T>` buffers.
   - Record summary data for telemetry (counts, averages) so the debug HUD shows game-specific insights.
3. Add a backup snapshot component so UI and tooling can display aggregate data even if the registries rewind (`GodgameRegistrySnapshot`, `Space4XRegistrySnapshot`).
4. When new registries appear in the package, extend the bridge to fill them. Keep registry labels unique so the shared directory can resolve them deterministically.

## 5. Bootstrap Scenes & Testing

1. Build a minimal validation scene that mirrors `PureDOTS/Assets/Scenes/PureDotsTemplate.unity`:
   - Root object with `PureDotsConfigAuthoring` + `SpatialPartitionAuthoring`.
   - SubScene containing DOTS entities for gameplay objects.
   - `SandboxHUD` Canvas with `DebugDisplayReader`.
2. Use this scene as your smoke test whenever the package updates. If it loads without console errors, run the game-specific scenario that exercises the new registries or systems.
3. Because this workspace relies on the external games as regression coverage, keep at least one `Godgame` and one `Space4x` scene compiling against the latest package at all times.
4. Record manual validation steps in your dev log (e.g., "Run Godgame > Sandbox scene → ensure registries populate, rewind toggles succeed"). This compensates for the lack of automated playmode suites.

## 6. Maintenance Checklist

- Rerun `Assets → Reimport All` after major package updates to rebuild Burst caches.
- Review `PureDOTS/Docs/ROADMAP_STATUS.md` for upcoming changes that may require adapter updates.
- When `package.json` version changes, update the dependency string in your manifest to match.
- Keep an eye on the pinned advisory in `PureDOTS_TODO.md`—avoid introducing Entities 1.5+ APIs or legacy input into your project until the heads-up is cleared.

Following these steps keeps each game aligned with the shared runtime while preserving deterministic behaviour and quick iteration loops for the solo development workflow.
