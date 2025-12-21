# Environmental Systems

**Status**: Locked (Architecture Baseline)  
**Category**: Core / World / Simulation (PureDOTS)  
**Related Systems**: World Generation, Movement, Combat, Sensors/Comms, Resources/Ecology, Rendering/FX (game-side)

---

## Purpose

Provide a **game-agnostic**, **rewindable**, and **highly modifiable** environment framework that supports:

- **Tactical** effects (elevation/cover/LoS, wind/ballistics, cohesion, fatigue, etc.)
- **Strategic** effects (biomes/zones influence materials/resources, ecology/regrowth/depletion)
- **Atmospheric** effects (weather/phenomena, fauna/flora ambience, subtle "world life")
- **Multiple themes** (Godgame ↔ Space4x) via **data-driven skinning** without changing core logic.

This module is built to run in a **cheap default mode** while keeping **richer backends** available behind the same contract.

---

## Non-Goals

- Not a single "one true" weather simulation. The system supports **field simulation**, **regional state patterns**, and **entity-based phenomena** concurrently or selectively.
- Not a rendering/visuals system. It exposes **data and events**; game-side decides how to render clouds, storms, fog, etc.

---

## Core Principles

1. **Stable contract, swappable backends**: gameplay systems never depend on internal representations (grid indices, entity types, solver specifics).
2. **Channel-based data**: environment is a set of typed channels (raw + derived), not a fixed struct.
3. **Domain-driven coordinates**: surface/volume/altitude bands are handled via domains, not hardcoded assumptions.
4. **Pull sampling for hot paths + push events for changes**: sampling is bounded; changes are evented.
5. **Rewindable stochasticity**: randomness is deterministic by keying and/or event ledger, enabling rewind/replay.
6. **Authorable and moddable by design**: registries, schemas, versioning, and optional hot-reload.

---

## Architecture Overview

### 1) Environment Service Contract (PureDOTS)

Gameplay systems interact through a single façade:

- **Pull**: sample environment at a query point (`EnvironmentQuery → EnvironmentSample`)
- **Push** (optional): consume environment events (phenomena start/end, region state flips, tile dirty notifications)

**Core types (conceptual):**
- `EnvironmentQuery`: domain + position + altitude band + optional region hint + flags
- `EnvironmentSample`: typed channel access (`TryGet(ChannelId<T>, out T)`)
- `ChannelId<T>`: registry-driven, mod-extensible typed channel key
- `EnvironmentEventStream`: append-only events for consumers that prefer push

**Key guarantee:** Consumers can request **raw** or **derived** channels without knowing how they are produced.

---

### 2) Channels (Raw + Derived)

**Raw channels** describe state:
- wind vector, humidity/moisture, temperature, particulates, pressure
- terrain material/roughness, height/slope, cover tags
- climate/biome identifiers and parameters
- deposits (snow depth, mud, ash, flood depth, etc.)

**Derived channels** are computed outputs:
- visibility attenuation, comms attenuation
- ballistic drift inputs (wind/gravity anomalies), drag scalar
- movement/fatigue multipliers, cohesion stress inputs
- sensor scattering/occlusion scalars (domain-specific)

Derived channels are produced by **Derivers** (see pipeline), and may be cached per tile/region.

---

### 3) Domains (Coordinate Spaces)

A **Domain** defines:
- coordinate mapping (surface heightmap vs volume sectors vs other)
- tiling/chunking scheme
- altitude band definitions (optional axis)
- how "region ids" are computed for a position (for coarse caching and event scoping)

Typical configurations:
- **SurfaceDomain**: 2.5D heightmap + altitude bands (Godgame)
- **VolumeDomain**: 3D sector/voxel-ish partition (Space4x)
- Domains can coexist in one world (e.g., surface + sky bands + "cloud domain" as an overlay).

---

### 4) Backends: Providers, Modifiers, Derivers

Environment state is assembled from composable pieces.

#### Providers (write raw channels)
Examples:
- `GridProvider`: tiled dense fields (fast sampling, streamable)
- `SparseProvider`: sparse tiles for rare data
- `ProceduralProvider`: no storage, sampled functions/noise
- `StatePatternProvider`: coarse regional state machines (ultra-cheap "big weather")
- `EntityPhenomenaProvider`: phenomena/entities with footprints (storms, islands, anomalies)

Providers can be enabled per channel and per domain.

#### Modifiers (local overrides / influences)
Modifiers apply as a stack over provider outputs:
- additive/multiplicative/override ops
- spatially indexed (tile → list of modifier handles)
- **hard-capped** per tile/band to bound sampling cost

Use cases:
- moving storm front, local cloud bank, temporary anomaly, floating island shadowing, etc.

#### Derivers (compute derived channels)
Derivers form a dependency graph:
- inputs: raw channels (+ other derived)
- outputs: derived channels
- runs on **dirty tiles/regions only** (incremental)
- supports domain-specific derivations (LoS/occlusion differs for surface vs volume)

---

### 5) Storage & Snapshots

#### Field Storage
Dynamic field data is stored as:
- tiled/chunked arrays (dense where needed)
- optional mip pyramid for multi-resolution sampling
- optional streaming (load/unload tiles by interest)

Static data (terrain/climate templates) is stored as:
- blob assets + indices into the tile store

#### Double-Buffered Snapshots
To avoid read/write hazards and to simplify jobs:
- `ReadSnapshot`: immutable for all gameplay sampling this step
- `WriteSnapshot`: environment update writes here
- swap at commit

---

## Rewindability & Determinism

### Baseline (guaranteed)
**Rewindable stochasticity** with deterministic reconstruction using:
- **Deterministic RNG keys** derived from:
  - `stepIndex`, `domainId`, `tileId/regionId`, `phenomenonId`, `channelId`
- **Event ledger** for discrete changes:
  - phenomenon spawn/despawn
  - region state transitions
  - authored interventions
- Optional **checkpoints** to accelerate scrubbing (configurable cadence)

This supports rewind/replay without requiring all environment systems to be "fully physical".

### Strict Mode (optional, kept flexible)
If required later, environment can run under a stricter determinism profile:
- deterministic math path selection (Burst-friendly)
- fixed-point / quantized channels for sensitive outputs
- constrained solver choices for cross-platform reproducibility

Strictness is a **configuration**, not a rewrite.

---

## Order of Execution (per simulation step)

1. **Freeze ReadSnapshot** (gameplay reads only this)
2. **Update Providers → WriteSnapshot (raw channels)**
3. **Apply Modifiers** (influences/phenomena footprints)
4. **Run Derivation Graph** (dirty tiles/regions only)
5. **Emit Environment Events** (state flips, phenomenon lifecycle, dirty notifications)
6. **Commit** (swap WriteSnapshot → ReadSnapshot)

Gameplay systems never write environment fields directly; they request changes via **commands** that become events/modifiers/providers inputs.

---

## Integration Contract (consumer systems)

### Sampling Patterns
Consumers can:
- sample by `position + altitudeBand` (typical)
- sample by `regionId` for coarse decisions
- prefetch a **ContextHandle** (cached sample for repeated use within a system pass)

### Push Patterns
Consumers can subscribe to:
- phenomenon start/end
- region state changes
- "tile became dirty" notifications (optional optimization)

### Anti-Coupling Rule
No consumer system may:
- assume a specific provider exists
- depend on tile resolution or grid indexing
- read provider internals
They can only request channels via the environment contract.

---

## Authoring, Modding, and Configuration

### Registries (extensible)
- Channel registry: `ChannelId<T>` + metadata (units, default values, visualization hints)
- Provider registry
- Modifier registry
- Deriver registry
- Domain registry

### Data-Driven Composition
World/environment configuration defines:
- domains
- enabled providers per channel
- modifier types allowed per domain
- derivation graph (or derivation presets)
- streaming settings
- rewind settings (ledger/checkpoints)

### Versioning
- schema version for environment configs
- migration support (data → data)
- save compatibility: environment state reconstructable from ledger + checkpoints + base seed

### Mod Capability (chosen baseline)
- **Code + data mods** are supported via registries (assemblies can register types).
- A data-only mode can be enforced by build/policy if desired later (registry whitelist).

---

## Debugging & Overlays (first-class)

### Debug View Interface
Backends implement `IEnvironmentDebugView` to expose:
- raw channels (wind vectors, moisture, terrain type, elevation)
- derived channels (visibility %, comms attenuation, ballistic drift inputs)
- provenance (optional): contribution breakdown by provider/modifier/deriver

### Overlay Outputs
- tile/region heatmaps
- vector fields (wind)
- contour maps (elevation, pressure)
- per-domain and per-band toggles

Debug tooling reads from **ReadSnapshot** and optional provenance caches to stay non-invasive.

---

## Theme Mapping (game-agnostic logic, game-specific skins)

Environment phenomena are defined as **archetypes** with parameters and channel effects.
Games apply **theme skins** via data:
- vegetation regrowth ↔ crystal regrowth
- blizzard ↔ magnetic storm
- shade from terrain ↔ shade from asteroid occluders
- cloud dispersion ↔ particulate fields

Skins affect visuals, names, and parameter presets, not the underlying contract.

---

## Reference Implementations (PureDOTS ships these)

- `GridProvider` (tiled dense fields, streamable)
- `StatePatternProvider` (regional state patterns)
- `ProceduralProvider` (function/noise sampling)
- `EntityPhenomenaProvider` (phenomena with footprints + lifecycle)
- `ModifierStack` (indexed modifiers with hard caps)
- `DerivationGraph` (dirty-driven incremental derivation)
- `EventLedger` (append-only, rewind source)
- `CheckpointStore` (optional)

Games can replace any of these while keeping the same interface.

---

## Performance Guardrails (architecture-level)

- Sampling must be bounded:
  - O(1) provider reads per requested channel (typical)
  - O(k) modifier ops with **hard cap per tile/band**
- Derivation must be incremental:
  - compute only dirty tiles/regions
  - memoize derived channels where stable
- Avoid structural churn:
  - phenomena entities are pooled and footprint updates are batched
- Keep memory predictable:
  - tile sizes fixed per domain
  - per-channel enablement (don't store what you don't use)
  - multi-resolution optional, not mandatory

---

## Notes for Godgame & Space4x Compatibility (architecture only)

- Godgame: SurfaceDomain + altitude bands; floating islands are best modeled as **phenomena modifiers** affecting terrain/cover/visibility channels; clouds as modifiers/providers in higher bands.
- Space4x: VolumeDomain + sector tiling; anomalies/storms as entity phenomena; "occluders" (asteroids) can contribute to derived sensor/comms channels via domain-specific derivers.

These are **configuration choices**, not special cases in the core contract.
