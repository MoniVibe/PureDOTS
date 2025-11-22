# Presentation Bridge Architecture

> Active work/tracking lives in `Docs/TODO/PresentationBridge_TODO.md`. Keep this architecture doc in sync when the bridge evolves.

## Objectives
- Keep gameplay (hot archetypes) deterministic while allowing rich visuals (cold archetypes) to evolve independently.
- Provide a reusable bridge that both Godgame and Space4X can extend without duplicating glue.
- Ensure rewind/playback never depends on GameObject state—visuals must be recreatable from simulation data.

## Layers & Responsibilities
| Layer | Owner | Responsibilities | Notes |
| --- | --- | --- | --- |
| **Simulation (Hot)** | PureDOTS Runtime | Authoritative state (`LocalTransform`, gameplay components, registry buffers). | Runs in SimulationSystemGroup / fixed step. |
| **Bridge** | PureDOTS Presentation Systems | Converts simulation events into presentation commands (`PresentationSpawnRequest`, `PresentationRecycleRequest`). | Lives in SimulationSystemGroup but only touches command buffers. |
| **Visual (Cold)** | Game-specific presentation assemblies | Instantiates & updates visuals based on command queues and registry descriptors. | Runs in PresentationSystemGroup; may depend on Entities Graphics or classic GameObjects. |
| **UI / HUD** | Game projects | Reads registry snapshots, presentation handles, and spatial data for widgets. | Built on top of bridge output; no direct coupling to gameplay systems. |

## Data Flow
1. **Authoring**: Designers tag simulation prefabs with a deterministic presentation key (e.g., `villager.default`). Bakers write the key into a component (`PresentationKey`) or emit spawn requests during conversion.
2. **Simulation Event**: When gameplay systems spawn or despawn entities they enqueue `PresentationSpawnRequest` or `PresentationRecycleRequest` onto the shared `PresentationCommandQueue`. Each request references the descriptor hash, transform overrides, tint, variant seed, and optional offset/follow lerp to drive companion sync.
3. **Bridge Processing**: `PresentationSpawnSystem`/`PresentationRecycleSystem` (PureDOTS.Runtime) process the command buffers, materialize visuals from the registry blob, and attach a `PresentationHandle` to the simulation entity. These systems obey `RewindState` so Playback/CatchUp mode skips visual churn.
4. **Visual Update**: Game-specific systems under `PresentationSystemGroup` read `PresentationHandle` + authoritative transforms to drive render meshes, VFX, Audio, or UI. Visual entities can interpolate, attach particle systems, etc., without feeding back into gameplay. `CompanionPresentationSyncSystem` keeps pooled bridge companions aligned with targets using offset and optional lerp.
5. **Telemetry / UI**: Presentation adapters publish lightweight metrics (active visual count, pool usage) and optionally expose stable IDs so HUD layers can look up the correct simulation entity via `PresentationHandle.Visual`.

## Companion Entity Pattern
- Simulation entities stay lean: `LocalTransform`, gameplay components, registry tags, and optionally a `PresentationKey`.
- Presentation entities (companions) contain render-specific data (`MaterialMeshInfo`, animation state, VFX tags). They should always be replaceable using the data stored in `PresentationHandle` + simulation state.
- Pooling is centralized via `PresentationSpawnFlags.AllowPooling`. If pooling is enabled, visual entities are recycled by the spawn/recycle systems instead of destroyed.

## Registry & Authoring
1. `PresentationRegistryAsset` defines descriptors (key hash, prefab, default offset/scale/tint, default flags).
2. `PresentationRegistryAuthoring` baker writes the blob and adds `PresentationRegistryReference` + `PresentationCommandQueue` to the world bootstrap.
3. Game projects extend the registry by adding new descriptors in their own assets (e.g., `GodgamePresentationRegistry.asset`). The asset path is referenced from a scene-level `PresentationBootstrapAuthoring`.
4. Keys should follow `<domain>.<variant>` naming and must not exceed 48 characters (enforced by `PresentationKeyUtility`).
5. Binding samples: `presentation.binding.sample` config toggles GrayboxMinimal vs GrayboxFancy (see `Samples~/PresentationBindings`); bindings include style blocks (palette/size/speed) plus lifetime and attach rules for effects/companions.

### Initial Descriptor Keys
- **Godgame**: `godgame.villager.default`, `godgame.villager.worker`, `godgame.villager.warrior`, `godgame.villager.worshipper`, `godgame.villager.builder`.
- **Space4X**: `space4x.crew.idle`, `space4x.crew.docked`, `space4x.crew.sortie`, `space4x.crew.combat`, `space4x.crew.transfer`.

Populate the per-game registry assets with these descriptors (prefabs, offsets, default flags) so the new adapter systems resolve hashes immediately.

### Aggregate Entity Categories
- **Godgame**: villagers are always individual entities with direct presentation. Aggregate play emerges through registry snapshots (villages, bands, dynasties, guilds, armies) that will later receive their own adapters.
- **Space4X**: individuals (crew specialists) roll up into aggregate entities typed via the shared `AggregateEntity` component (crew, fleet, colony, village, guild, dynasty, elite, business/company). Crews are created automatically whenever individuals join a workforce/military role that is not an officer/pilot/executive; fleets aggregate capital ships and their crews; outside combat the dominant aggregates are villages, colonies, guilds, families/dynasties, ruling elites, and businesses/companies.
- Presentation adapters should respect these categories when choosing descriptor hashes (e.g., `space4x.crew.*` for aggregate crews, future `space4x.fleet.*` for strike groups, `space4x.business.*` for trade houses, `godgame.guild.*` for guild halls).
- **No standalone visuals**: Aggregate entities never spawn bespoke meshes; their presence is implied through their member entities, owned assets, and optional UI/flag overlays. For example, a mage guild exists as its hall + member villagers + contracts, while an army appears as its soldier entities plus a HUD banner at strategic zoom. Presentation adapters should only highlight aggregates via overlays/handles, not instantiate separate representations.

## Domain Adapters
Each gameplay domain emits presentation commands through a thin adapter system. Recommended adapters: 
- `VillagerPresentationAdapterSystem` – emits spawn requests keyed by villager archetype and updates animation/VFX from `VillagerState` + `PresentationHandle`.
- `StructurePresentationAdapterSystem` – mirrors storehouses, miracles, logistics hubs; synchronises construction progress.
- `ResourcePresentationAdapterSystem` – shows resource nodes, veins, and depletion VFX.
- `CrewPresentationAdapterSystem` (Space4X) – spawns visuals for crew aggregates, tinting by average morale and following carrier/craft transforms.
- `FleetPresentationAdapterSystem` (Space4X) – maps registries (`Space4XFleetRegistryEntry`) to carrier/ship visuals.

Adapters live in each game project but consume the shared request buffers and registry contracts. They should:
1. Read simulation/registry data in SimulationSystemGroup.
2. Decide when to enqueue spawn or recycle requests.
3. Update `PresentationHandle` driven components (e.g., set animator parameters) in PresentationSystemGroup.

## Rewind & Determinism
- All bridge systems check `RewindState.Mode` and `PlaybackGuardTag` before mutating visuals.
- Simulation data remains the source of truth; handles contain enough info (`DescriptorHash`, `VariantSeed`) to respawn visuals after a rewind.
- Optional: record presentation commands to `HistorySystemGroup` when deterministic replay of visuals is required.
- Visual validation: use `PresentationScreenshotCapture` + `PresentationScreenshotUtility` to capture hashes during scenarios or CI; attach-rule/ palette-only changes can be whitelisted in comparisons.

## Observability & Debugging
- Add counters (`presentation.spawned`, `presentation.pooleduse`) via `PresentationTelemetrySystem` (pending) to monitor pool pressure.
- Debug HUD can highlight entities missing `PresentationHandle` or descriptors.
- Provide a CLI/Console command (`presentationsim.reload`) to rebuild the registry and respawn visuals in editor builds.

## Implementation Checklist
1. **Registry alignment** – per-game registry assets referencing all required prefabs; documented keys.
2. **Simulation emitters** – adapter systems that enqueue spawn/recycle requests based on component state.
3. **Companion sync** – presentation systems that update LocalTransform/animation for visual entities using handles.
4. **Pooling strategy** – configure pooling per descriptor and expose pool metrics.
5. **Testing** – add playmode tests ensuring spawn/recycle honour rewind and that orphaned handles are cleaned up.
6. **Docs** – keep `PresentationBridgeContracts.md` and `PresentationBridge_TODO.md` in sync with new adapters and assets.

See `Docs/DesignNotes/PresentationBridgeContracts.md` for API-level details. This document focuses on the architectural split and responsibilities so future contributors can extend visuals without touching gameplay determinism.
