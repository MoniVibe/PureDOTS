# Biodeck + Biosculpting System (Startopia-Style, Scalable, Shared Core)

**Status**: Draft (authoritative direction)  
**Category**: Core / Environment / Vegetation / Space4X Feature (shared engine core)  
**Applies To**: Space4X, Godgame, shared PureDOTS, future projects  

Biodeck is a nostalgic feature: a terraformable “nanosoil” surface where you sculpt local environment knobs to create home-biomes, then let ecology yield resources. The implementation must be **presentation-agnostic**, **deterministic**, and **scale-friendly** (patch-first, not entity-first).

Anchor inspiration:
- Startopia’s Bio Deck: locally adjustable terrain/temperature/moisture/water that drives species comfort + plant types + yields.

This maps cleanly to our existing shared environment scaffolding:
- `PureDOTS.Environment.ClimateVector` + `BiomeType` (`Packages/com.moni.puredots/Runtime/Runtime/Environment/ClimateComponents.cs`, `.../EnvironmentGrids.cs`)
- Space4X scaffolding: `BioDeckModule` + `BioDeckCell` (`space4x/Assets/Scripts/Space4x/Climate/TerraformingComponents.cs`)

Related architecture:
- `puredots/Docs/Architecture/Scalability_Contract.md` (no per-entity scans; LOD; anti-pattern bans)
- `puredots/Docs/Architecture/Production_Consumption_Accounting.md` (event accounting; Actual vs Reference)
- `puredots/Docs/Architecture/Ship_Interiors_Streaming_MicroWorlds.md` (SimInterior always-on; PresentInterior streamed)
- `puredots/Docs/Architecture/Senses_And_Comms_Medium_First.md` (smell/sound in a medium; compartment graphs)

---

## 1) Core principle (what makes this scalable)

Biodeck is three systems:
1) a **climate field you can sculpt** (grid/graph caches),
2) a **biome classifier with hysteresis** (stable labels, no flicker),
3) **vegetation simulated as stands/patches** (yields from patches, not from millions of plant entities).

Only materialize “hero plants” (individual vegetation entities) when the camera/boarding/inspection needs it.

Recurring error to avoid:
- “every blade of grass is an entity” (even with Burst you drown).

---

## 2) Data model (PureDOTS core; patch-first)

### 2.1 Biodeck grid is authoritative interior environment

Biodeck is “a small environment grid bound to a parent entity”:
- Space4X: parent = ship/station module entity.
- Godgame: parent = world/region grid provider (same systems, different grid source).

Minimum authoritative cell payload:
- `ClimateVector Climate`
- `BiomeType Biome`

Existing Space4X data:
- `BioDeckModule { GridResolution, CellSize, LocalOrigin }`
- `BioDeckCell { ClimateVector Climate, BiomeType Biome }`

### 2.2 Optional slow-morph state (separate from the climate vector)

Add a second buffer to support slow morphing and edit batching without resampling everything:

`BioDeckCellState` (conceptual):
- `float WaterLevel01` (distinct from humidity/moisture)
- `float SoilNutrients01`
- `uint DirtyFlags`
- `uint LastEditTick`
- optional: `ClimateVector TargetClimate` (or store target deltas by axis)

Note: `ClimateVector` already includes `WaterLevel` and `Fertility`, but keeping explicit “edit targets” and “dirty” markers avoids global recompute and supports slow relaxation.

### 2.3 Biosculpt command buffer (edit is an event, not a scan)

Edits are commands accumulated on the biodeck parent (or planet/region):

`BioSculptCommand` (conceptual):
- cell selection (rect, radius, list, mask)
- operation: `Heat/Cool/Hydrate/Dehydrate/RaiseWater/LowerWater/NutrientUp/NutrientDown`
- magnitude + falloff
- `uint TickStamp` (for deterministic ordering)

Commands are applied deterministically:
- stable order = `(TickStamp, bufferIndex)`; no random iteration ordering.

---

## 3) Biome math (slow change + stable classification)

### 3.1 Biome resolution from climate axes

Biome classification primarily uses the dominant ecological axes:
- temperature (normalized or Celsius)
- moisture/precipitation/dryness

Our shared classifier already exists for world grids:
- `PureDOTS.Systems.Environment.BiomeDerivationSystem` (`.../BiomeDerivationSystem.cs`)

Biodeck extends this with:
- water level and nutrients as additional signals (useful for “pond vs soil bed” distinction),
- **hysteresis** so biomes don’t flicker when climate hovers near a boundary.

### 3.2 Hysteresis rule (required)

Biome changes only when the best candidate beats the current biome by a margin:
- `if score(best) > score(current) + hysteresisMargin => switch`
- otherwise keep current biome

This gives stable UI/presentation and prevents “biome thrash” in production/yield logic.

Optional future: keep “visual biome” separate from “sim biome” for prettier gradients; simulation uses the stable sim biome.

### 3.3 Dirty-only resolution (required)

Never re-resolve the entire biodeck grid every tick.

Pipeline (deterministic, scalable):
1) `BioSculptCommandApplySystem`: apply commands → update targets → set dirty flags
2) `ClimateRelaxationSystem`: move `Climate` toward `TargetClimate` with a stable integrator (no overshoot); clear dirty when within epsilon
3) `BiomeResolveSystem`: resolve biome only for dirty cells (and optional neighborhood ring) using hysteresis

---

## 4) Vegetation simulation (stands/patches first; plants second)

### 4.1 Stand/Patch is the default unit of simulation

Biodeck vegetation runs as stands/patches attached to cells:
- density/health/age distributions (not individual entities)
- suitability is computed from climate + biome
- yields are computed from stand state, not from iterating plant entities

This is where we get Startopia’s “ecosystem yields goods” without per-plant cost.

### 4.2 “Hero plants” are optional, camera-driven materializations

When PresentInterior loads (or a close camera inspects a biome patch):
- spawn representative vegetation entities/instances for visuals
- keep the authoritative yield logic in the stand ledger

For Space4X interiors, this plugs into the streaming micro-world contract:
- `SimInterior` continues regardless of streaming
- `PresentInterior` decides if hero plants exist visually

### 4.3 Yields integrate via flow events (Actual vs Reference)

Stand yields produce **event accounting**, not “scan and sum”:
- emit `Produced(resourceId, amount, scope)` at accrual/harvest boundaries
- emit `Consumed(...)` for upkeep if needed (nutrients/water/power)

This integrates directly with:
- `puredots/Docs/Architecture/Production_Consumption_Accounting.md` (Actual vs Reference)

Reference/capacity for biodecks:
- compute from installed biodome capacity + selected species/stand specs
- expose “ideal yield” vs “actual yield” (starved vs blocked diagnosis)

---

## 5) Species/race comfort (Startopia-style, data-driven)

Startopia used a simple preference matrix (Dry/Med/Wet × Cold/Moderate/Hot). We generalize that concept:

`SpeciesClimatePreferenceBlob` (conceptual; per species/race):
- preferred `(TempRange, MoistureRange)`
- `WaterAffinity` and `NutrientAffinity`
- `BiomeMask` / preferred biome set
- comfort curve parameters (peak + falloff)
- weight for how much “nature” affects morale/productivity

Runtime evaluation:
- crew/villager samples the current compartment/biodeck cell climate (or an aggregate “nearby nature score”)
- outputs a deterministic modifier:
  - morale delta / mood support
  - work efficiency modifier
  - optional: “homesick” / “thriving” tokens for reactions/relations

Space4X nuance:
- outside hull = no effect (vacuum/external environment)
- inside = compartment-local environment sampling (ties into compartment graph work)

---

## 6) Sharing the same biosculpting core with Godgame

Biodeck is just “a local environment grid bound to a parent”.

Godgame:
- parent grid = terrain/world region (large-scale environment grids)

Space4X:
- parent grid = ship module (small local grid)

Shared PureDOTS spine:
- biosculpt command application (event-driven)
- climate relaxation (stable integrator)
- biome resolution with hysteresis + dirty-only updates
- stand simulation + yield events

Per-game adapters:
- how the grid is authored/seeded
- how presentation materializes hero plants/props
- how inventories receive produced resources (ship hold vs village storehouse)

---

## 7) Determinism + performance guardrails (hard rules)

- Do not make `BiomeType` or `PreferenceId` a high-cardinality `ISharedComponentData` (chunk fragmentation).
- Do not let presentation (skins/meshes) drive climate/biome; it’s the other way around.
- Do not simulate individual plants by default; simulate stands and materialize visuals only when needed.
- Do not re-resolve biomes globally each tick; resolve dirty cells only.
- Use integer units for produced/consumed accounting; rates are derived for UI.

---

## 8) Current scaffolding (what exists today)

Space4X currently contains biodeck scaffolding:
- `BioDeckModule`, `BioDeckCell` (`space4x/Assets/Scripts/Space4x/Climate/TerraformingComponents.cs`)
- `BioDeckSystem` placeholder for comfort, and `BioDeckClimateControlSystem` that spawns per-cell `ClimateControlSource` (`space4x/Assets/Scripts/Space4x/Climate/BioDeckSystem.cs`)

PureDOTS already contains:
- `ClimateVector`, `BiomeType`, and world grid systems including biome derivation (`puredots/Packages/com.moni.puredots/Runtime/Runtime/Environment/*`, `.../Systems/Environment/BiomeDerivationSystem.cs`)

Next engine step (when implemented):
- migrate the biodeck grid provider + biosculpt pipeline into PureDOTS so Godgame/Space4X share the core.

