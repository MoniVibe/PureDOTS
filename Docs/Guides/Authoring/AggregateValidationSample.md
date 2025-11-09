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
The goal is to guarantee at least one `VillageId` entity with residents so the membership/belonging systems have data to process while keeping authoring identical between Godgame and Space4x.

### 2.1 Villager Baseline (`VillagerAuthoring`)
- Duplicate `PureDOTS/Assets/PureDOTS/Prefabs/Villager.prefab` into `Assets/Samples/PureDOTS/Aggregates/Prefabs/ValidationVillager.prefab` so you have a sandbox copy that won’t clash with runtime tweaks.
- Keep the following defaults, which mirror the design brief for general-purpose villagers (see `Runtime/Authoring/VillagerAuthoring.cs` and `Docs/VillagerLoopAnalysis.md`):

  | Category | Field | Baseline | Notes |
  | --- | --- | --- | --- |
  | Vital stats | `initialHealth`, `maxHealth` | 100 | Protects against early death during validation runs. |
  | Needs & pools | `initialMorale` = 50, `initialEnergy` = 100, `initialHunger` = 30, `initialMana`/`maxMana` = 10 | Mana uses floating-point worship points; morale/energy baseline matches the spec you shared. |
  | Mobility & senses | Base speed 3, sight ranges 50/30/10, hearing 20/10/3 | Matches `Docs/Mechanics/StealthFramework.md` signal falloffs and the “quiet/noisy/crowded” expectations. |
  | Combat | `attackDamage` = 5, `attackSpeed` = 1, `defenseRating` = 10 | Unarmed villagers follow the 5 damage baseline. Each point of `strengthBonus` or `defenseRating` scales damage/mitigation by ~5% within `VillagerCombatSystem`. |
  | Attributes | `physique`/`finesse`/`willpower` = 5, derived bonuses = 5 | Bakers clamp the derived stats to 200 max and add the baseline 10 so traits/perks can stack later. |
  | Belief & reputation | `primaryDeityId` = `divine.hand`, `faith` = 0.5 | Keeps worship/mana exchange running so the Worship Site prefab can be validated. |

- Assign unique `villagerId` values (1..n) while sharing one `factionId` per validation slice.
- Set `initialJob` to mixed roles (e.g., half Gatherers, half Builders) so shortages remain non-zero and `VillageWorkforceDemandSystem` keeps emitting data.
- Toggle `startAvailableForJobs` on for everyone; leave `isCombatCapable` false unless you want guard demand to flip. Villagers without a discipline fallback to “closest important task” because `VillagerDisciplineSystem` sees their availability flag.

### 2.2 Building Prefabs (`Housing`, `VillageCenter`, `Storehouse`, `WorshipSite`)
All placeholder structures already live under `PureDOTS/Assets/PureDOTS/Prefabs/` and expose the fields you described:

1. **VillageCenter** (`VillageCenterAuthoring` in `Runtime/Authoring/BuildingAuthoring.cs`)
   - Use a fixed `villageId` (e.g., 101), `factionId = 0`, `maxPopulation = 50`, and point `villagerPrefab` to `ValidationVillager.prefab`.
   - Alignment/cohesion/initiative defaults of 50 keep bootstrap systems simple, while `residencyRange = 30` lets villagers register without scene navigation yet.
2. **Housing** (`HousingAuthoring`)
   - Default values (`maxResidents = 4`, `restBonusMultiplier = 1.25`, `energyRestoreRate = 2.5`) ensure rest/sleep loops function even before art arrives. These numbers align with `Docs/Mechanics/MoraleDynamics.md` and keep limb/health recovery simple.
3. **Storehouse** (`StorehouseAuthoring` in `Runtime/Authoring/Resource/StorehouseAuthoring.cs`)
   - `capacities` already cover ore/food/wood and map to the `ResourceTypeId_CRITICAL.md` definitions; no scene work is required. The `inputRate`/`outputRate` fields act as throughput clamps for hauling validation.
4. **WorshipSite** (`WorshipSiteAuthoring`)
   - Provides mana/worship sinks so belief/faith systems get exercised. Keep the prefab referenced even if no visual exists; the baker only needs the authoring component.

### 2.3 Scene Spawn Profile
1. Create `Assets/Samples/PureDOTS/Aggregates/Profiles/AggregateValidationSpawnProfile.asset` via **Create ➜ PureDOTS ➜ Scene Spawn Profile**.
2. Add at least two entries:
   - **Entry 1** (`SceneSpawnCategory.Structure`): prefab = `ValidationVillageCenter`, placement = `Point`, count = 1. This guarantees a baked `VillageId` entity even before gameplay spawners kick in.
   - **Entry 2** (`SceneSpawnCategory.Unit`): prefab = `ValidationVillager`, placement = `Grid`, count = 8–12 with spacing `(2, 2)` so the population surrounds the village. Keep `payloadId` empty; the villager baker seeds IDs for you.
3. Drop a `SceneSpawnAuthoring` component in the SubScene root and assign the spawn profile. On play, you should see villagers plus the center entity in the Entities Hierarchy (`SceneSpawnSystem` instantiates both and tags them with `SceneSpawned` for easy filtering).

### 2.4 Mirroring Into Godgame & Space4x
- Godgame ships the same prefabs under `Godgame/Assets/Prefabs/PureDOTS/`. Space4x mirrors them under `Space4x/Space4x/Assets/Prefabs/PureDOTS/` so you can test adapters per-game without touching the shared assets.
- Keep the PureDOTS copy authoritative. When you change stats, reapply them to the downstream prefab variants (or script it via GUID remapping once automation lands). Document deltas in `Docs/Mechanics/FacilityArchetypes.md` if a game needs different shelter/rest entries.

## 3. Space4x Aggregate Prefabs & Loops
Space4x leans on the same aggregate stack but exposes ships instead of villagers. The validation slice therefore lives entirely in prefabs and ScriptableObjects—no scene authoring required until spatial navigation drops in.

### 3.1 Capital Ship / Carrier Prefab
- `Space4x/Space4x/Assets/Prefabs/PureDOTS/CapitalShip.prefab` combines several authoring scripts:
  - `VillageCenterAuthoring` – reused as a “ship = floating village” aggregate root. Set `maxPopulation = 120`, `residencyQuota = 200`, and reuse the villager prefab so crews can still be represented as individuals when required.
  - `HousingAuthoring` – gives internal bunks (`maxResidents = 20`, `restBonusMultiplier = 1.1`).
  - `CarrierDockingAuthoring` (`Runtime/Authoring/Space/CarrierDockingAuthoring.cs`) – exposes `maxDockedCraft`, `maxHangarSlots`, `dockingThroughput`, `undockingThroughput`, plus patrol/harvest defaults so throughput tuning is data-driven.
  - `SpaceVesselLoadoutAuthoring` – defines baseline mass capacity (400), 10% over-cap penalties, and placeholder equipment slots for weapons/engines/shields/sensors/etc. Equipment assets live under `Space4x/Space4x/Assets/Data/Equipment/`.
- Use this prefab when validating ship aggregates, crew morale, or docking throughput before nav is online.

### 3.2 Strike Craft, Maneuvers & Targeting
- `StrikeCraft.prefab` bundles `StrikeCraftAuthoring`, `SpaceVesselLoadoutAuthoring`, and `CombatLoadoutAuthoring`:
  - `CombatLoadoutAuthoring` exposes `strafeThreshold`, `kiteThreshold`, and `jTurnThreshold` values that unlock manoeuvres once pilot experience (from `pilotExperience`) crosses each threshold—exactly the behaviour requested for strafing/kiting/J-turns.
  - Targeting parameters (`engagementRange`, `weaponCooldownSeconds`, `retreatThreshold`) feed `SpaceCombatTargetingSystem`, which already factors projectile speed, target velocity, sensor tech level, and gunnery skill (see `Runtime/Systems/Space/Combat/StrikeCombatTargetingSystem.cs`).
  - Keep loadouts baseline for now; once AI is validated you can swap in per-weapon assets without touching the systems.

### 3.3 Harvesters, Resource Piles & Haulers
- `MiningVessel.prefab` uses `HarvesterAuthoring` (`harvestRadiusMeters = 2000`, `dropoffIntervalSeconds = 45`) and a loadout for resource extraction. It spawns resource piles through `ResourceDropSpawnerSystem` at the configured rate.
- Resource piles (`ResourcePile`, `ResourcePileMeta`, `ResourcePileVelocity`) drift in zero-g, merge on contact, and decay if ignored (`ResourcePileSystem`, `ResourcePileMovementSystem`, `ResourcePileDecaySystem`). This mirrors the “resource piles combine on touch” requirement.
- `Hauler.prefab` (with `HaulerAuthoring`) establishes the job/role hierarchy:
  - `HaulingJobPrioritySystem`, `HaulingJobManagerSystem`, and `HaulingJobAssignmentSystem` score jobs using the priority > speed > value/urgency > distance heuristic you specified. Dedicated freighters get first dibs, then idle civilian ships fill gaps inside the radius.
  - `HaulingLoopSystem` simulates travel/load/unload without nav yet, but the travel-time calc already accepts straight-line distance so swapping in spatial grid samples later is trivial.
- All ships with cargo components can serve as haulers—the `HaulerRole.IsDedicatedFreighter` flag is simply a weighting hint. For holders like civilian carriers, add `HaulerAuthoring` to their prefab to let them participate when emergencies arise.

### 3.4 Formation Seeds & Crew Aggregates
- Formation data lives in `Space4x/Space4x/Assets/Prefabs/PureDOTS/CarrierFormation.prefab`. It is currently a data-only layout (line/wedge/sphere) until nav arrives. The formation service references `Docs/Runtime/Systems/Formations/FormationAssignmentSystem.cs` for enforcement.
- Crews stay aggregate-first: ship prefabs include `ShipAggregateAuthoring` (`Godgame/Assets/Scripts/Space4x/Authoring/ShipAggregateAuthoring.cs`) so crew stats/morale/outlooks average across the member buffer. Cold-storage ships just omit crew prefabs until contracts assign them.

## 4. Optional Devtools Aggregate Preset
When `DEVTOOLS_ENABLED` is defined you can create aggregates on demand to hit edge cases faster.

1. Create a `PrototypeRegistryAuthoring` GameObject inside the SubScene, list the prefabs you intend to spawn (e.g., `ValidationVillager`) and assign prototype names (`villager.validation.worker`). The baker emits a `PrototypeRegistryBlob` singleton used by the devtools console (`Runtime/Authoring/Devtools/PrototypeRegistryAuthoring.cs`).
2. Create an `AggregatePresetAsset` (**Create ➜ PureDOTS ➜ Devtools ➜ Aggregate Preset**) called `ValidationVillagePreset.asset` with member entries such as:
   - Prototype Name: `villager.validation.worker`
   - Min/Max Count: 6 / 8
   - Optional stat overrides (health, speed) if you want to see weighted aggregation.
3. Add an `AggregatePresetAuthoring` component pointing at the preset so the blob lands in the world.
4. At runtime, call `DevtoolsConsole.CreateAggregateSpawnRequest(World.DefaultGameObjectInjectionWorld.EntityManager, aggregatePresetId: 0, totalCount: 8);` from the in-editor C# console (or bind it to your devtools UI). `ProcessAggregateSpawnSystem` (DEVTOOLS) expands the preset, builds an `AggregateGroup` entity, and populates `AggregateMembers`. Check `AggregateAggregationSystem` afterwards to confirm `AggregateEntity.MemberCount`, `Morale`, and `Cohesion` fields are filled in.

## 5. Validation Checklist
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

## 6. Wiring This Guide To The Rest Of The Docs
- `Docs/Guides/SceneSetup.md` – describes the shared bootstrap (`PureDotsConfigAuthoring`, `SpatialPartitionAuthoring`, `SceneSpawnAuthoring`, `TimeControlsAuthoring`) that every validation scene must keep in sync with this guide.
- `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md` – covers the spatial grid and navigation placeholders. Reference it before you replace the straight-line travel estimations inside hauling/combat systems.
- `Docs/Mechanics/MiningLoop.md`, `Docs/Mechanics/HaulLoop.md`, and `Docs/Mechanics/CombatLoop.md` – detail the loops we are exercising with the villager and ship prefabs (mining piles ➜ hauling jobs ➜ combat patrols). Keep their tables updated when you tweak prefab stats.
- `Docs/Mechanics/MoraleDynamics.md` and `Docs/Vessel_And_Villager_Movement_Setup.md` – explain how rest, morale, and movement speeds should be tuned so your prefab values remain canonical across both games.
- `Docs/TODO/VillagerSystems_TODO.md` – lists upcoming aggregate/villager work. When you add new authoring fields, log follow-ups there so the validation sample evolves alongside runtime systems.
- `Docs/Guides/UsingPureDOTSInAGame.md` – summarize per-project adapter steps. Link any Godgame/Space4x prefab overrides back to this guide so downstream teams know when shared defaults changed.

## 7. Troubleshooting
- **VillageWorkforceDecisionSystem disabled**: the system requires `AggregateBehaviorProfile`, `VillageId`, and `VillagerId` before it runs, and it also skips work when `RewindState.Mode != Record`. Ensure your time controls aren’t paused and the profile asset is referenced by the SubScene.
- **No VillagerAggregateBelonging**: confirm the village prefab still has a `VillageResidentEntry` buffer (added by `VillageCenterBaker`) and that villagers are within the residency range. You can also manually seed membership by adding a baker that fills `DynamicBuffer<VillagerAggregateMembership>` on bake if you need deterministic tests without movement.
- **Devtools aggregate spawn requests do nothing**: double-check the `PrototypeRegistryAuthoring` list—`ProcessAggregateSpawnSystem` currently grabs the first `AggregatePresetBlobReference` it finds, so keep only one active preset in validation scenes.

## 8. References
- `Runtime/Authoring/AggregateBehaviorProfileAsset.cs` – defines the asset you author.
- `Runtime/Systems/Village/VillageWorkforceDecisionSystem.cs` – consumes the behaviour blob and emits `VillagerWorkforceIntent`.
- `Runtime/Systems/Aggregates/VillagerVillageMembershipSystem.cs` & `VillagerAggregateBelongingSystem.cs` – derive belonging from village residency and aggregate buffers.
- `Runtime/Authoring/BuildingAuthoring.cs` – provides `VillageCenterAuthoring` and related bakers used for the sample prefab.
- `Runtime/Authoring/Devtools/*` – optional prototype + aggregate preset tooling for rapid aggregate validation.

Keep this document up to date whenever the aggregate pipeline gains new data requirements (additional aggregate categories, presentation overlays, etc.) so anyone can rebuild the validation SubScene in under an hour.
