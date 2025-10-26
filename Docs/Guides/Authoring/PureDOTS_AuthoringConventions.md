# PureDOTS Authoring Conventions & Governance

Updated: 2025-10-26

This document establishes naming, versioning, validation, and migration practices for all data-driven assets that feed the PURE DOTS runtime. Treat it as the authoritative contract for designers, engineers, and tooling pipelines.

## 1. Naming & Folder Structure

- **Assemblies & Namespaces**: Runtime code lives under `PureDOTS.Runtime.*`; authoring and editor extensions use `PureDOTS.Authoring` and `PureDOTS.Editor`. Avoid domain-specific names (e.g., "Farmland")—prefer neutral terminology ("ResourceProducer").
- **Asset Locations**: By default place shared profiles under `Assets/PureDOTS/Config/`. Domain-specific assets (vegetation species, miracles, villager archetypes) live under `Assets/PureDOTS/Data/<Domain>/`.
- **File Naming**: Use PascalCase without spaces (e.g., `EnvironmentGridConfig.asset`, `SpatialPartitionProfile.asset`, `VillagerArchetype.asset`). When multiple variations exist, append purpose-specific suffixes (`PureDotsRuntimeConfig.development.asset`).
- **SubScenes**: Bootstrap SubScenes should mirror the scene name (e.g., `PureDotsBootstrap.SubScene`). Entities that host environment/spatial authoring components belong in the bootstrap SubScene and should not be duplicated in gameplay scenes.
- **ScriptableObjects**: Name data assets with the profile type and optional qualifier (e.g., `ResourceTypeCatalog.default.asset`, `SpatialPartitionProfile.largeRealm.asset`). Keep names under 64 characters to avoid truncation in tooling.
- **GameObject Authoring Components**: When multiple authoring components exist on a GameObject, use the suffix `Authoring` (e.g., `StorehouseAuthoring`, `VillagerSpawnerAuthoring`). MonoBehaviour names should match the file exactly to preserve auto binding.

## 2. Versioning & Change Management

- **Semantic Labels**: Version shared ScriptableObjects using numeric suffixes (`PureDotsRuntimeConfig.v1.asset`, `v2` etc.) when breaking changes occur. Maintain a `CHANGELOG` section in the asset's inspector notes or companion README.
- **Migration Scripts**: For each breaking change to a profile Schema (e.g., fields added to `PoolingSettingsData`), create an editor migration script under `Assets/Scripts/PureDOTS/Editor/Migrations/` that:
  - Scans for older assets (by version or missing fields).
  - Applies default values / structural adjustments.
  - Records the change in the asset's `Version` field (if present) and logs to the console.
- **Schema Versions**: Shared profiles and authoring components expose `LatestSchemaVersion` constants and serialized `_schemaVersion` fields. Migration scripts should set the field to `LatestSchemaVersion` after updating data.
- **Runtime Guards**: Bakers should emit warnings when required data is missing or outdated (e.g., `EnvironmentGridConfig` missing channel ids). Prefer early exits that leave the GameObject in place rather than partially baked data.
- **Git / Branch Workflow**: When migrating assets, include the migration script and run it in the same commit. Provide instructions in PR description and update relevant guides.

## 3. Validation & CLI Integration

- Use the `PureDOTS/Validation/Run Asset Validation` menu command (or CLI equivalent) before committing. CI should call `PureDOTS.Editor.PureDotsAssetValidator.RunValidationFromCommandLine` and fail on errors.
- When validation rules change, update both the validator implementation and this document. Add tests that load representative assets (editmode or playmode) to cover the changes.
- Inspectors for key ScriptableObjects must expose a `Validate` button so designers can check assets without running the global validation pass.

## 4. Authoring Workflows

### 4.1 EnvironmentGridConfig

1. **Create Asset**: `Assets → Create → PureDOTS → Environment → Grid Config`. Store in `Assets/PureDOTS/Config/EnvironmentGridConfig.asset`.
2. **Configure Channels**: Set resolution/cell size per channel. Ensure channel IDs are unique; validation will error otherwise.
3. **Bounds Alignment**: Align world bounds with the playable space. Match `SpatialPartitionProfile` bounds to prevent sampling issues.
4. **Assign to Scene**: Add `EnvironmentGridConfigAuthoring` to a bootstrap GameObject and assign the asset. Ensure the object lives in the bootstrap SubScene.
5. **Run Validation**: Use inspector `Validate Environment Grid` or global menu to confirm no errors/warnings.
6. **Bake & Test**: Enter playmode; confirm `MoistureGrid`, `TemperatureGrid`, etc., appear in the entity debugger with expected metadata.

### 4.2 SpatialPartitionProfile

1. **Create Asset**: `Assets → Create → PureDOTS → Spatial Partition Profile`. Store in `Assets/PureDOTS/Config/SpatialPartitionProfile.asset`.
2. **Bounds & Cell Size**: Match environment bounds. Choose cell size appropriate for agent spacing (default 4m). Hashed grid recommended.
3. **Assign to Scene**: Add `SpatialPartitionAuthoring` to bootstrap GameObject, assign profile asset.
4. **Validation**: Run inspector `Validate Spatial Profile` or global validation; adjust warnings (e.g., cell size >32m) before commit.
5. **Migrate Changes**: If bounds/cell size change significantly, document the change and run migration scripts for dependent systems (e.g., adjusting spawn volumes).

### 4.3 Runtime Config & Resource Catalog

1. **Runtime Config**: `PureDotsRuntimeConfig` controls time/history/pooling defaults. Keep only one canonical asset checked into source control. Use inspector `Validate Runtime Config` before publishing.
2. **Resource Catalog**: Maintain unique resource IDs; validation enforces trimmed, case-insensitive uniqueness. When adding new resources, update dependent tests or data-driven registries.
3. **Pooling Settings**: Ensure capacities align with expected entity counts. Document overrides in the pooling section below.

### 4.4 Registries

1. **Resource/Storehouse/Villager Registries**: Baker components automatically register entities through tags. Ensure authoring components (e.g., `ResourceSourceAuthoring`, `StorehouseAuthoring`) are present and configured.
2. **Registry Directory**: The bootstrap scene must include `RegistryDirectoryAuthoring` (if present) to publish handles through `RegistryMetadata`. Document registry ownership in `Docs/DesignNotes/SystemIntegration.md`.
3. **Custom Registries**: When adding new registry types, follow the pattern: authoring component → baker adds config/state → runtime system updates registry buffers. Update this document and validation once stabilized.

## 5. Pooling Authoring Guidelines

- **Default Config**: `PoolingSettingsData` inside `PureDotsRuntimeConfig` defines global capacities. Keep values conservative; use telemetry to justify increases.
- **Prefab Pools**: Authoring components that need pooled prefabs must register them with `EntityPoolRegistry`. Document pool warmup counts in the prefab's inspector notes.
- **Custom Pools**: When adding new pooled resources (e.g., FX particle bursts), create a ScriptableObject that references the prefab and desired counts. Provide migration scripts if structure changes.
- **Diagnostics**: Designers can inspect `DebugDisplayData` to view pool borrow/return counts. Ensure new pools hook into diagnostics for consistency.
- **Naming**: Name pooled assets with `Pool` suffix (e.g., `VillagerPool.asset`, `MiracleFxPool.asset`). Avoid theme-specific names.

## 6. Migration Scripts Overview

- **Location**: Place migration helpers under `Assets/Scripts/PureDOTS/Editor/Migrations/`. Prefix files with timestamp and summary (e.g., `M20251026_UpdatePoolingSettings.cs`).
- **Structure**: Each migration script should:
  1. Provide a static method `[MenuItem("PureDOTS/Migrations/Run XYZ Migration")]` for manual invocation.
  2. Offer a `RunFromCommandLine` method so CI can execute via `-executeMethod`.
  3. Use `AssetDatabase.FindAssets` to locate affected assets, apply transformations, and `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets` when changes occur.
  4. Log summary of updated assets. Errors should halt with `UnityEditor.EditorApplication.Exit(1)` when running headless.
- **Framework Support**: Use `PureDotsMigrationRunner.RunMigrations(bool applyChanges)` to execute migrations in apply or dry-run mode. Base class `PureDotsMigration` handles logging/reporting.
- **Documentation**: Update this guide and affected domain docs whenever migrations run. Include before/after schema snapshots in `Docs/DesignNotes/<Domain>MigrationLog.md` (create per-domain logs as needed).
- **Test Hook**: Add editmode tests that call `PureDotsMigrationRunner.RunMigrations(false)` to ensure migrations can execute without modifying assets when in dry-run.

## 7. Thematic Neutrality Guidelines

- Avoid domain-specific semantics in shared assets. Prefer generic descriptors (e.g., `ResourceProducer`, `GrowthNode`).
- Colour/default values should be data-driven; do not embed narrative-specific assumptions in runtime components.
- When adding new fields, consider how alternative themes (sci-fi, city builder) would interpret them. Document extension points for customization.

## 8. Authoring Review Checklist

Before merging authoring changes:

- [ ] Validation (menu or CLI) runs clean (no errors, warns justified).
- [ ] Assets stored under agreed folders (`Assets/PureDOTS/Config` or documented override).
- [ ] Naming conventions followed (no spaces, theme-neutral).
- [ ] Migration scripts included if schema changes.
- [ ] Tests updated (playmode/editmode) if behaviour depends on new data fields.
- [ ] Documentation updated (this guide, relevant TODOs, onboarding references).

## 9. References

- `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md`
- `Docs/DesignNotes/SystemIntegration.md`
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`
- `Docs/TODO/Utilities_TODO.md`
- `Docs/TODO/SystemIntegration_TODO.md`

Keep this document updated as new authoring flows land. Add sections for additional profile types (miracle profiles, villager archetypes, etc.) when their governance is defined.

