# Modding Architecture

## Overview

**Status**: Draft (locked decisions)  
**Applies to**: Godgame + Space4x  
**Shared Library**: PureDOTS (game-agnostic runtime + Mod API), game-side integrates presentation/UI  
**Primary Platforms**: Windows client, Linux headless (dev + automation)  
**Scope**: Sandbox Mode and (within reason) all systems

This architecture targets **maximum flexibility** (data + scripting + code) while remaining **safe-by-default** and **tool-friendly** (profiles, deterministic load order, lockfiles, and reports).

---

## Goals

1. **Full moddability**: mods can add/modify content, behaviors, and ECS systems.
2. **Safe-by-default**: powerful capabilities require explicit opt-in.
3. **Scalable simulation**: mod hooks must not force per-entity-per-tick work.
4. **Deterministic management**: profiles, ordering, and resolved sets must be reproducible.
5. **Headless parity**: headless builds can run user mods (gated), enabling CI/AI experimentation.

---

## Non-Goals (explicit)

- Perfect sandbox security for in-process native/managed code is not feasible. Code mods are treated as **trusted** when enabled.
- Hot-reload of C# code mods is not required; restart is acceptable.
- Remote distribution/workshop is out of scope for the baseline; local folders + external tools handle acquisition.

---

## Mod Types

Mods are supported via three lanes, unified under one loader:

### 1) Data Mods (default / safe)
- Adds and patches structured content ("Defs") and assets.
- Preferred for compatibility and performance.

### 2) Lua Script Mods (safe-ish / sandboxed)
- Lua runs inside a restricted environment.
- Lua interacts via stable Mod API (events + commands + queries), not direct engine internals.

### 3) Code Mods (unsafe / explicit opt-in)
- C# assemblies (DLLs) that can:
  - register ECS systems
  - register extension modules
  - register content via registries
- Requires explicit unsafe enablement + capability grants.

---

## Security Model (Safe-by-default)

### Safe Mode (default)
- Data + Lua only
- No code mods
- Restricted capabilities

### Unsafe Mode (explicit)
- Allows C# code mods
- Capabilities explicitly granted per profile
- Headless can enable unsafe mode only via allow settings (see below)

### Capability model
Mods request capabilities in `mod.json`; profiles grant/deny. Examples:

- `ecs_register_systems`
- `ecs_modify_systems`
- `spawn_entities`
- `edit_defs`
- `filesystem_read`
- `filesystem_write_moddir_only`
- `network_access`
- `headless_enable`

**Default**: deny anything that touches filesystem/network or core system modification.

---

## Mod Package Format (local folders)

A mod is a folder (or a zip extracted into a folder) with:

- `mod.json` (manifest)
- `Defs/` (data defs)
- `Patches/` (patch operations)
- `Scripts/` (Lua)
- `Assemblies/` (C# DLLs, unsafe)
- `Assets/` (optional)

### Search paths
- User mod directory (configurable)
- Optional additional `--moddir` directories
- Profile-relative paths supported for portability

---

## Manifest: `mod.json` (baseline contract)

Minimum fields:

- `id` (stable namespaced id, e.g. `author.modname`)
- `name`
- `version` (SemVer)
- `apiMin` / `apiMax` (Mod API SemVer range)
- `dependencies[]` (id + version range)
- `conflicts[]`
- `loadBefore[]` / `loadAfter[]` (optional ordering hints)
- `capabilities[]` (requested permissions)
- `entrypoints[]` (C# types implementing `IMod`, for code mods)
- `headlessAllowed` (bool; default true, but capability-gated)

Recommended fields:

- `description`
- `authors[]`
- `website` (informational only)
- `hashes` / `signature` (reserved for allowlist/signing support)

---

## Profiles (tool-first)

A profile is the unit of reproducibility and tool integration.

A profile defines:
- enabled mods + deterministic order
- pinned versions (optional)
- unsafe allow toggles + capability grants
- per-mod config overrides

Profiles can be stored:
- globally
- per save
- in a standalone file for external tooling

### CLI integration (for rim.py-like tooling)
- `--profile <path>`
- `--moddir <path>` (repeatable)
- `--safe-mode`
- `--unsafe-mods` (enables unsafe lane; still requires capability grants)
- `--dump-mod-report <path>`
- `--dump-lockfile <path>`

### Lockfile
A lockfile captures:
- resolved mod IDs + versions
- content hashes
- computed load order
- granted capabilities

---

## Loader Pipeline (order-of-execution)

1) **Discover** mods from search paths  
2) **Read manifests**  
3) **Resolve graph** (dependencies, conflicts, pins)  
4) **Compute load order**  
   Base → DLC → Framework mods → Content mods → Patch mods  
   Then apply `loadBefore/loadAfter` constraints deterministically.
5) **Security gate**  
   - Safe mode: refuse code mods; restrict capabilities
   - Unsafe mode: allow code mods only if profile grants capabilities and (headless) allow settings pass
6) **Load Data**  
   - register defs → apply patch ops → validate → compile to runtime registries (tables / BlobAssets)
7) **Load Lua**  
   - create per-mod Lua sandboxes → register event handlers / scheduled hooks
8) **Load Code (unsafe only)**  
   - load assemblies → find `IMod` entrypoints → call registration methods
9) **Register ECS systems** into mod groups with explicit ordering constraints
10) **Emit Mod Report** and start sim

---

## Data Modding (Defs + Patches)

### Def model
- All content is described by stable-ID defs (e.g. `ItemDef`, `FactionDef`, `AbilityDef`, `AIArchetypeDef`, `TechDef`, `MapDef`, etc.)
- Each def type has:
  - `schemaVersion`
  - stable `id` (namespaced: `modid:Thing`)
  - optional `tags`/categories

### Patch operations
Mods may:
- add new defs
- patch existing defs via operations:
  - `Add`, `Remove`, `Replace`, `Merge`

**Conflict policy (locked)**:
- Deterministic load order is the primary resolution mechanism.
- For the same target field, patch mods apply last.
- "Last writer wins" applies only after ordering; patch mods should be explicit in intent and minimal in scope.

### Runtime compilation
Raw def files are not read on hot paths.
Defs compile into:
- stable runtime registries
- BlobAssets / tables optimized for Burst jobs

---

## Lua Scripting (sandboxed)

### Purpose
Lua is for:
- event reactions
- lightweight rules
- behavior glue around defs
- sandbox gameplay experimentation

Lua does **not** access engine internals directly.
Lua calls stable Mod API services only.

### Sandbox rules (locked defaults)
- No OS / IO / networking libraries
- No unrestricted reflection/interop
- Deterministic RNG access only via provided API
- CPU guardrails (instruction quota / tick budget)
- Memory limit per mod environment

### Scheduling model
Lua can:
- register event handlers (preferred)
- register scheduled hooks at coarse granularity
Lua should not be used for per-entity-per-tick loops.

---

## Code Mods (C# assemblies) — unsafe lane

### Build/runtime policy (locked)
- Code mods are supported where runtime assembly loading is available.
- The mod-enabled build uses a managed backend compatible with dynamic assembly loading.
- If a build configuration cannot load assemblies at runtime, code mods are disabled and the loader falls back to Data + Lua.

### Entrypoints
Code mods implement:

- `IMod`:
  - `RegisterContent(IModContentRegistry r)`
  - `RegisterSystems(IModSystemRegistry r)`
  - optional: `OnProfileActivated(IModRuntime ctx)`

### ECS system registration (mods can add systems)
Mods register systems into explicit phases/groups:

- `ModPreSimGroup`
- `ModSimGroup`
- `ModPostSimGroup`
- optional: `ModPresentationGroup` (view-only)

Ordering is expressed via stable keys:
- Core systems expose `SystemKey` identifiers.
- Mods declare ordering: `After(SystemKey.X)` / `Before(SystemKey.Y)`.

### Modifying existing systems
Supported paths (preferred):
- def-driven configuration
- event-driven extensions
- strategy interfaces/extension points resolved from registries
- command API mutations

Unsupported-by-default (allowed only in unsafe mode; no compatibility guarantees):
- runtime IL patching / Harmony-style interception

---

## Mod API Surface (stable contract)

Expose a single versioned Mod API package (PureDOTS):

- `IModContentRegistry`  
  Register defs, localization, assets metadata, etc.
- `IModEventBus`  
  Subscribe/publish high-level events (avoid per-entity spam).
- `IModCommandBus`  
  Enqueue sim-safe commands that mutate state deterministically.
- `IModQueryAPI`  
  Approved queries: id/tag lookups, registry access, coarse spatial facade, etc.
- `IModSystemRegistry`  
  Register ECS systems into phases with ordering constraints.

**Rule**: mods depend on Mod API and `SystemKey`, not internal type names.

---

## Version Compatibility Strategy (best practices, locked defaults)

### Two version tracks
- **Game Version**: internal
- **Mod API Version**: stable contract, SemVer

Mods declare `apiMin/apiMax`. Loader rejects incompatible mods early.

### Data schema evolution
- Each def type has `schemaVersion`.
- Loader runs migrations for known old schema versions (data-only).
- Unknown schema versions fail with clear diagnostics.

### Deprecation
- Deprecate Mod API calls before removal.
- Compatibility shims live inside Mod API.
- Anything relying on internal system types/reflection is "unsafe/unsupported."

---

## Headless / Server Mode (Linux)

### Locked posture
- Default: **safe mode** (Data + Lua)
- Unsafe: enabled only via explicit allow settings:
  - allowlist by mod id and/or hash
  - optional signature support reserved

This enables user mods for headless experimentation without silently accepting arbitrary code by default.

---

## Instrumentation & Guardrails (project-scale focused)

To protect performance and debugging at MMO-sim scale:

- Per-mod diagnostics:
  - loaded content counts
  - resolved order + conflict decisions
  - capability grants
- Runtime monitoring:
  - exceptions per mod (auto-disable option for repeated faults)
  - per-mod Lua budget violations
  - per-mod system timing sampling (where available)
- Headless harness:
  - "load profile → run N ticks → emit report" for automated validation

---

## Modding Best Practices (publish to modders)

### Performance (critical)
- Prefer defs + parameters over per-entity logic.
- Prefer event-driven reactions over "think every tick."
- Avoid unbounded radius scans and pairwise relationship tracking.
- Keep buffers bounded (no unbounded histories).
- Batch queries; use coarse spatial partitioning when possible.
- Avoid structural churn in hot paths (spawn/despawn/add/remove components repeatedly).
- For ECS systems: keep Burst-friendly; avoid managed allocations in update loops.

### Compatibility
- Do not depend on internal types; depend on Mod API + `SystemKey`.
- Patch using official operations, not reflection.
- Keep IDs stable; provide migrations when renaming or splitting defs.

### UX
- Provide sane defaults and per-mod configs.
- Emit clear errors (missing deps, API mismatch, schema mismatch).
- Avoid "silent overrides"; prefer patch mods with explicit intent.

---

## Templates to Provide

1) Data-only starter mod (new defs + one patch)
2) Lua mod (event handler + command)
3) Code mod (unsafe) registering one ECS system + one extension hook
4) Headless test harness profile + sample lockfile + mod report example

---

## Reserved / Deferred (kept minimal by design)

- Workshop/remote distribution (handled by external tools initially)
- Signatures/certificates (manifest fields reserved; enforce later if needed)
- Hot-reload for C# code mods (restart is the standard)

---
