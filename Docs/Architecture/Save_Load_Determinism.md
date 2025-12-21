# Save/Load Determinism System (Design v1)

## Overview

**Goal:** Deterministic simulation continuity across **save → load**:

- Same **seed + inputs + tick schedule + content manifest** ⇒ identical simulation state after loading.
- **Rewind works across the load boundary** (history survives the save).

This document fills the stub `[To be designed]` section and locks the major design choices.

---

## Determinism Contract

### What is deterministic
- **Simulation world (tick-based):** deterministic across Windows dev and Linux headless tests.
- **Presentation world (frame-based):** not required to be deterministic; rebuilt from simulation + saved player/UI memory.

### Required invariants
1. Simulation uses **tick time** only (no frame time in authoritative sim logic).
2. All randomness is derived from the deterministic RNG design below (no UnityEngine.Random in sim).
3. Any aggregation/reduction has deterministic ordering (e.g., gather → stable sort → reduce).
4. Save files include a **Content Manifest**; load is **blocked by default** on mismatch, unless user enables **Unsafe Load**.

---

## Time Integration (mixed time)

- **TickTimeState (authoritative):** saved + restored exactly (tick, timescale, paused).
- **Presentation time:** continuous; rebuilt post-load, except minimal UX continuity (camera/UI state).

---

## RNG System (per-entity, luck-friendly, rewind-safe)

### Why "root seed only" is insufficient
After any RNG consumption, restoring only the root seed cannot reproduce the future because the system no longer knows how many draws have occurred per entity/system. Future draws diverge immediately.

### Recommended design: RootSeed + Per-Entity Counter (stateless draws)
Persist:
- **WorldRootSeed:** `uint` (singleton, saved)
- **RngCounter:** `uint` per participating entity (saved + rewinded)

Random draw procedure:
1. Compute `drawSeed = Hash(WorldRootSeed, StableId, StreamId, RngCounter)`.
2. Increment `RngCounter`.
3. Create a temporary RNG from `drawSeed` and read as many values as needed for that *one* draw.

Notes:
- **StreamId** is a compile-time constant per domain (e.g., LootRoll, CritRoll, PersonalityDrift, ProcChance…).
- **Luck buffs/debuffs** modify probability weights/thresholds. (They should not require special RNG mechanics.)
- Works under parallel scheduling because each entity consumes its own counter deterministically.
- Supports rewind because the counter is part of the state history.

### Optional compatibility mode (prototype-only)
You may keep `RNGState { Unity.Mathematics.Random Value; }` for quick prototyping,
but it is not recommended for extreme entity counts because it is larger per entity than a counter.

---

## Stable IDs + Arbitrary References (anything → anything)

### Stable identity (flexible width)
All persistent/rewindable entities must have a **StableId**.

**Flexible width policy (recommended):**
- **Default:** 64-bit (`ulong`) for runtime/per-entity storage (fast, memory-efficient).
- **Optional:** 128-bit StableId when you need effectively-zero collision risk across very large/modded ecosystems.

**Save-format rule:** save header records `StableIdByteWidth` (8 or 16). Loaders must support both.
Runtime may down-map 128-bit IDs into a compact table if desired, but persistence must round-trip losslessly.

### References
Persisted references must not rely on runtime `Entity.Index/Version`.
Store references as:
- **StableId** (preferred), or
- a small `EntityRef` that serializes as StableId and is remapped on load.

**Determinism rule:** relationship/link buffers must have deterministic ordering
(sort by StableId or enforce deterministic insertion rules).

---

## Save Scope (minimal saves)

### Saved (authoritative)
- Tick-time state (TickTimeState + any authoritative singletons)
- Rewind state/history window (see below)
- WorldRootSeed + per-entity RngCounter
- StableId for each persisted entity
- All components/buffers needed to reproduce simulation decisions
- Arbitrary references/relationships (StableId-based)

### Not saved (recomputed)
- Spatial indices, nav/path caches, broadphase acceleration structures
- Derived AI scoring caches, sensor caches
- Presentation proxy entities / VFX state

### Saved (player immersive material)
Persist these explicitly:
- **Fog of war** (discovery/visibility memory)
- **Story/event ledger** (narrative continuity)
- **Relationship memories** (if not fully derivable from current authoritative state)
- **UI selections** (StableId list) + basic view state (camera/orbit)
  - Missing selection targets are **cleared silently** (v1).

---

## Rewind Across Load Boundary

### Requirement
After loading a save, player can rewind into the pre-save history window.

### Strategy (persist the full configured window)
Persist both:
1. **World snapshot checkpoints** ring buffer (coarse baselines)
2. **Per-track history buffers** (fine-grained deltas/events)

Rules:
- Rewind data must be serialized **StableId-keyed**, even if runtime uses `Entity` references.
- On load:
  - rebuild StableId → Entity map
  - remap StableId refs into runtime handles where needed
  - continue recording into the same history window

**Important:** saves preserve the **full configured rewind window** (no clamping on save).

---

## File Format (hybrid)

### Binary container (authoritative)
Chunked binary format:
- **Header** (fixed size)
- **Manifest chunk**
- **Entity table chunk** (StableId + prefab/type ids + component layout)
- **Component chunks** (typed by TypeHash)
- **Buffer chunks** (typed by TypeHash)
- **Rewind chunks**
- **PlayerMemory chunk** (FOW, story ledger, relationship memories)
- **UIState chunk**
- **Footer** (checksums)

Header minimum:
- Magic, format major version, endianness
- Save tick, WorldRootSeed
- StableIdByteWidth (8/16)
- Content manifest hash
- Flags (UnsafeLoadAllowed=false by default)
- Chunk checksums

### Debug sidecar (optional)
Small JSON summary:
- header copy, manifest, counts, warnings, migration report

---

## Mods / Content Manifest

### Default policy
**Blocked by default** when manifest mismatch.

Manifest should include:
- Game major version
- Enabled mods list (id + version)
- Registry/catalog hashes for critical defs
- Optional: build id / git hash (dev)

### Unsafe Load toggle
If user enables Unsafe Load:
- Load proceeds with placeholder definitions for missing content
- Produce a **Load Report** (missing mods, replaced defs, dropped entities, etc.)
- UI selections referencing missing StableIds are cleared silently (v1)

---

## Migration (major versions only)

- If save major == runtime major: load directly
- If save major older: attempt migration chain (v1→v2→...)
- If save major newer: reject (no downgrade)

On migration failure:
- Default: hard fail with report
- Unsafe Load: continue with fallbacks/placeholders where possible

---

## Determinism Verification (headless)

### Save/Load A/B test (primary)
A) Start with seed S, run N ticks ⇒ digest H1  
B) Start with seed S, run K ticks, save, load, run to N ⇒ digest H2  
Assert H1 == H2.

Digest rules:
- Iterate entities in deterministic order (StableId sort)
- Hash authoritative components/buffers only (exclude presentation/UI unless intentionally tested)
- Type ordering deterministic (TypeHash sort)

### Cross-platform confidence
Run the same scenario + digest on Windows and Linux headless and compare hashes.

---

## Performance Notes

- Prefer 64-bit StableId for most persistent entities; omit StableId for pure presentation entities.
- Keep per-entity RNG state to a 4-byte counter where possible.
- Avoid serializing derived caches; rebuild after load.
- Compress per chunk (LZ4/Zstd) for large saves; keep header/manifest uncompressed for fast inspection.

---

## Defaults (v1)

- StableId width: **Flexible** (default 64-bit; 128-bit optional; recorded in file header)
- RNG: **WorldRootSeed + per-entity RngCounter** + StreamId domains
- Rewind persistence: **persist full configured rewind window (no clamping on save)**
- UI selection persistence: **StableId list; missing targets ⇒ cleared silently (v1)**
- Mods: **blocked by default; Unsafe Load toggle to proceed**
- Migration: **major versions only**
- Verification: **headless A/B save-load digest test + cross-platform digest compare**
