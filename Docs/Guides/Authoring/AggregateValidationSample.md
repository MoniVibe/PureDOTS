# Aggregate Validation Sample Authoring Guide

Updated: 2025-11-05

This guide walks through the minimal authoring needed to exercise the aggregate behaviour stack (villager aggregates, workforce demand, aggregate behaviour profiles, and optional devtools presets). Follow it to build a reproducible SubScene that keeps the DOTS-side features healthy even before the game-specific adapters are wired.

## Prerequisites
- Unity Entities 1.2+ project already consuming `com.moni.puredots` (see `Docs/Guides/UsingPureDOTSInAGame.md`).
- A sandbox scene that mirrors the shared bootstrap (`PureDotsConfigAuthoring`, `SpatialPartitionAuthoring`, `SceneSpawnAuthoring`, `TimeControlsAuthoring`, and `DebugAuthoring`). `Docs/Guides/SceneSetup.md` covers these pieces.
- The sample assets below live well under `Assets/Samples/PureDOTS/Aggregates/` (feel free to adjust, but keep paths consistent for future automation).
- Optional devtools steps require the `DEVTOOLS_ENABLED` scripting define (Player Settings ➜ Other Settings ➜ Scripting Define Symbols) so the aggregate preset utilities compile.

> **Tip:** Keep all ScriptableObjects checked into version control. Whenever you change tunable values, update this guide so designers and automation know the canonical defaults.

## Workflow Overview
1. Create an `AggregateBehaviorProfileAsset` and reference it from every validation SubScene so `VillageWorkforceDecisionSystem` has serialized tuning data.
2. Build a `VillageCenter` prefab (with `VillageCenterAuthoring`) and wire it into your validation `SceneSpawnProfileAsset`.
3. Duplicate the provided villager prefab (`PureDOTS/Assets/PureDOTS/Prefabs/Villager.prefab`) or make your own with `VillagerAuthoring` so the scene spawns at least 6–10 workers.
4. (Optional) Enable devtools presets to spawn ad-hoc aggregate crews using `PrototypeRegistryAuthoring` + `AggregatePresetAuthoring`.
5. Run through the validation checklist to confirm registries, aggregate membership, and workforce intents are updating each frame.

## 1. Aggregate Behaviour Profile Asset
The aggregate behaviour blob stored on `AggregateBehaviorProfile` controls how often villagers reconsider their job, how much shortage vs. personal ambition weighs in, and how alignment influences their decisions (`PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/AggregateBehaviorProfileAsset.cs`).

1. Create the asset via **Create ➜ PureDOTS ➜ Aggregate Behavior Profile** and save it as `Assets/Samples/PureDOTS/Aggregates/Profiles/StandardVillageProfile.asset`.
2. Recommended starting values:
   | Field | Suggested Value | Reason |
   | --- | --- | --- |
   | Initiative Interval / Jitter | 240 ticks / 12 ticks | Matches the bi-daily cadence the workforce systems were tuned for (`AggregateBehaviorProfileBlob.InitiativeIntervalTicks`). |
   | Collective Need Weight | 1.25 | Makes shortages win over personal desires in the sample scene so intents are visible almost immediately. |
   | Personal Ambition Weight | 0.8 | Keeps discipline switches from being too cheap. |
   | Emergency Override Weight | 2.0 | Guarantees that the emergency path still interrupts when you toggle conscription/defense flags. |
   | Discipline Resistance | 1.25 | Mirrors the defaults inside the authoring script. |
   | Shortage Threshold | 0.35 | Prevents micro-shortages from spamming intents. |
   | Allow Conscription Overrides | true | Lets the sample toggle emergency behaviour. |
   | Conscription / Defense Weights | 3.0 / 2.5 | Aligns with the runtime defaults. |
   | Lawfulness Compliance Curve | (-1, 0.5) ➜ (1, 1.5) | Use the linear curve from the asset template so lawful villages comply faster. |
   | Chaos Freedom Curve | (-1, 0.5) ➜ (1, 1.5) | Same idea for materialism/chaos. |
3. Reference the asset inside every validation SubScene: select the SubScene root, find the **Referenced Assets** list in the inspector, and add `StandardVillageProfile.asset`. Unity will bake it through `AggregateBehaviorProfileBaker` and you will see an `AggregateBehaviorProfile` singleton when the scene plays (`Entities Hierarchy ➜ All Entities ➜ Singleton > AggregateBehaviorProfile`). Without this reference, both `AggregateBehaviorBootstrapSystem` and `VillageWorkforceDecisionSystem` stay disabled.

## 2. Village Prefab & Scene Spawn Profile
The goal is to guarantee at least one `VillageId` entity with residents so the membership/belonging systems have data to process.

1. Duplicate `PureDOTS/Assets/PureDOTS/Prefabs/Villager.prefab` into `Assets/Samples/PureDOTS/Aggregates/Prefabs/ValidationVillager.prefab` and tweak `VillagerAuthoring`:
   - Assign unique `villagerId` values (1..n) and share a `factionId` that matches the village.
   - Set `initialJob` to mixed roles (e.g., half Gatherers, half Builders) so shortages remain non-zero.
   - Toggle `startAvailableForJobs` on for everyone; leave `isCombatCapable` false unless you want guard demand to flip.
2. Create a new prefab `ValidationVillageCenter.prefab` containing:
   - `VillageCenterAuthoring` (from `Packages/com.moni.puredots/Runtime/Authoring/BuildingAuthoring.cs`). Use a fixed `villageId` (e.g., 101), `factionId = 0`, `maxPopulation = 50`, and point `villagerPrefab` to `ValidationVillager.prefab`. Alignment/cohesion/initiative defaults of 50 keep bootstrap systems simple.
   - Optional: add `WorshipSiteAuthoring` or `HousingAuthoring` if you want to test cross-system interactions—these components already bake `VillageResidentEntry` buffers that feed the membership system.
3. Build a `SceneSpawnProfileAsset` (`Assets/Samples/PureDOTS/Aggregates/Profiles/AggregateValidationSpawnProfile.asset`) with at least two entries:
   - Entry 1 (category: `SceneSpawnCategory.Structure`): prefab = `ValidationVillageCenter`, placement = `Point`, count = 1. This guarantees a baked `VillageId` entity even before gameplay spawners kick in.
   - Entry 2 (category: `SceneSpawnCategory.Unit`): prefab = `ValidationVillager`, placement = `Grid`, count = 8–12 with spacing of `(2,2)` so the sample population surrounds the village. Keep `payloadId` empty; the villager baker already seeds IDs.
4. Drop a `SceneSpawnAuthoring` component in your SubScene root and assign the spawn profile. On play, you should see villagers + the center entity inside the SubScene section of the Entities Hierarchy.

## 3. Optional Devtools Aggregate Preset
When `DEVTOOLS_ENABLED` is defined you can create aggregates on demand to hit edge cases faster.

1. Create a `PrototypeRegistryAuthoring` GameObject inside the SubScene, list the prefabs you intend to spawn (e.g., `ValidationVillager`) and assign prototype names (`villager.validation.worker`). The baker emits a `PrototypeRegistryBlob` singleton used by the devtools console (`Runtime/Authoring/Devtools/PrototypeRegistryAuthoring.cs`).
2. Create an `AggregatePresetAsset` (**Create ➜ PureDOTS ➜ Devtools ➜ Aggregate Preset**) called `ValidationVillagePreset.asset` with member entries such as:
   - Prototype Name: `villager.validation.worker`
   - Min/Max Count: 6 / 8
   - Optional stat overrides (health, speed) if you want to see weighted aggregation.
3. Add an `AggregatePresetAuthoring` component pointing at the preset so the blob lands in the world.
4. At runtime, call `DevtoolsConsole.CreateAggregateSpawnRequest(World.DefaultGameObjectInjectionWorld.EntityManager, aggregatePresetId: 0, totalCount: 8);` from the in-editor C# console (or bind it to your devtools UI). `ProcessAggregateSpawnSystem` (DEVTOOLS) expands the preset, builds an `AggregateGroup` entity, and populates `AggregateMembers`. Check `AggregateAggregationSystem` afterwards to confirm `AggregateEntity.MemberCount`, `Morale`, and `Cohesion` fields are filled in.

## 4. Validation Checklist
Run the scene in Play Mode and verify the following in the Entities Hierarchy or Entity Inspector:

1. **Singletons**
   - `AggregateBehaviorProfile` exists and its blob mirrors the asset values (search for `AggregateBehaviorProfile` in the Entities Hierarchy).
   - `TimeState`, `RewindState`, and `RegistryDirectory` exist (bootstrap sanity check).
2. **Village Entity**
   - `VillageId`, `VillageStats`, `VillageResidencyState`, `VillageOutlook`, and `VillageWorkforcePolicy` components are present (most are added automatically by `VillageCenterBaker`, `VillageOutlookBootstrapSystem`, and `VillageWorkforcePolicyBootstrapSystem`).
   - `DynamicBuffer<VillageWorkforceDemandEntry>` contains at least one shortage entry after a few ticks (produced in `VillageWorkforceDemandSystem`).
3. **Villagers**
   - Each villager has `VillagerJob`, `VillagerDisciplineState`, `VillagerNeeds`, `VillagerFlags`, and `VillagerAggregateBelonging` (the latter is added by `VillagerAggregateBelongingSystem` once `VillagerAggregateMembership` buffers exist). If belonging is missing, confirm villagers are added to the village resident buffer (`VillageVillageMembershipSystem` uses `VillageResidentEntry`).
   - `WorkforceDecisionCooldown` and `VillagerWorkforceIntent` components appear shortly after play (produced by `VillageWorkforceDecisionSystem`). Inspect the `DesiredJob` field to ensure it matches the dominant shortage.
4. **Aggregate Members**
   - If you spawned devtools aggregates, look for entities with `AggregateEntity`, `AggregateMembers`, and optionally `AggregateMemberStats`. `AggregateAggregationSystem` should update the aggregate after one frame (check `MemberCount` > 0).
5. **Telemetry / HUD**
   - Open the `DebugDisplayReader` UI and confirm the registry counts update (resource/storehouse plus the meta registries if you enabled them). Aggregate-specific HUD work is pending, but registry health staying `Healthy` verifies the validation slice did not regress instrumentation.

## Troubleshooting
- **VillageWorkforceDecisionSystem disabled**: the system requires `AggregateBehaviorProfile`, `VillageId`, and `VillagerId` before it runs, and it also skips work when `RewindState.Mode != Record`. Ensure your time controls aren’t paused and the profile asset is referenced by the SubScene.
- **No VillagerAggregateBelonging**: confirm the village prefab still has a `VillageResidentEntry` buffer (added by `VillageCenterBaker`) and that villagers are within the residency range. You can also manually seed membership by adding a baker that fills `DynamicBuffer<VillagerAggregateMembership>` on bake if you need deterministic tests without movement.
- **Devtools aggregate spawn requests do nothing**: double-check the `PrototypeRegistryAuthoring` list—`ProcessAggregateSpawnSystem` currently grabs the first `AggregatePresetBlobReference` it finds, so keep only one active preset in validation scenes.

## References
- `Runtime/Authoring/AggregateBehaviorProfileAsset.cs` – defines the asset you author.
- `Runtime/Systems/Village/VillageWorkforceDecisionSystem.cs` – consumes the behaviour blob and emits `VillagerWorkforceIntent`.
- `Runtime/Systems/Aggregates/VillagerVillageMembershipSystem.cs` & `VillagerAggregateBelongingSystem.cs` – derive belonging from village residency and aggregate buffers.
- `Runtime/Authoring/BuildingAuthoring.cs` – provides `VillageCenterAuthoring` and related bakers used for the sample prefab.
- `Runtime/Authoring/Devtools/*` – optional prototype + aggregate preset tooling for rapid aggregate validation.

Keep this document up to date whenever the aggregate pipeline gains new data requirements (additional aggregate categories, presentation overlays, etc.) so anyone can rebuild the validation SubScene in under an hour.
