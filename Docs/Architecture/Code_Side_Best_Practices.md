# Code-Side Best Practices (PureDOTS)

## Overview

**Status**: v1 (active)
**Category**: Architecture / Best Practices / Guidelines
**Applies To**: PureDOTS runtime + systems code (`PureDOTS.*` assemblies)

PureDOTS exists to be **game-agnostic**, **scalable**, and **mod-friendly**. We assume player-driven entity composition ("living buildings", "planet with moods", etc.). The patterns below aim to keep that flexibility **without** blowing up archetype count, memory, or cache behavior.

---

## Golden Rules

1. **Stable archetypes win.** Prefer enable-bits, pooling, and feature-entities over add/remove component churn.
2. **Hot data is typed.** Anything used frequently belongs in small, cache-friendly `IComponentData`.
3. **Cold data is referenced.** Rare/long-tail properties live in blobs, registries, or feature entities.
4. **Config is immutable + shared.** Use blob assets (baked or runtime-built) for read-only config.
5. **Parallel first.** Jobs + Burst by default; avoid managed allocations and managed containers in update.
6. **Structural changes are phase-bound.** Record changes in `EntityCommandBuffer` (ECB), play back at explicit boundaries.

---

## Component Design Patterns

### Decision Matrix

Use this to decide the storage shape:

- **`IComponentData`**
  - Fixed-size, hot, frequently accessed.
  - Prefer: POD (blittable) primitives, small structs.
  - Avoid: big structs that force cache misses.

- **`IEnableableComponent`**
  - Optional feature/state that should NOT create new archetypes.
  - Use for toggles such as alive/undead/active/disabled states.

- **`IBufferElementData`**
  - Variable-length list per entity.
  - Must have a *cap policy* (hard cap, soft cap + spill, or promoted to feature-entity).

- **Blob Asset (`BlobAssetReference<T>`)**
  - Read-only config shared across many entities.
  - Mandatory for authored config and recommended for runtime-defined blueprints.

- **Feature Entity (linked/owned entity)**
  - Use when data is:
    - large,
    - rare,
    - or leads to archetype explosion.
  - Example: inventories, equipment, social graphs, long memory logs, construction graphs.

- **(Rare) Generic Property Bag**
  - For truly arbitrary modded attributes.
  - Keep out of hot loops. Promote hot properties into typed components later.

### Size & Locality Guidance

- Prefer components that fit in one cache line when possible.
- Split **hot vs cold**:
  - Hot: position, velocity, main state flags, small stats.
  - Cold: cosmetics, long descriptions, large behavior graphs, long lists.

### Buffers: Cap Policies (Required)

Dynamic buffers are powerful but dangerous at scale. Every buffer type must declare one of these policies:

- **Fixed small list**: Use `FixedListXXBytes<T>` inside an `IComponentData` when max size is small and known.
- **Capped dynamic buffer**: Use `DynamicBuffer<T>` with a documented max element count and a defined overflow behavior (reject / evict / spill).
- **Feature entity**: If the list can become large or is rare, move it to a feature entity owned by the core entity.

Rule: **No unbounded buffers on high-count populations**.

### SharedComponentData Policy

- **PureDOTS simulation hot-path**: **disallowed** (chunk fragmentation + frequent main-thread sync).
- **Allowed** only in: rendering/presentation, authoring/baking, editor tooling (and even there: use sparingly).

### Tags, Flags, and "Anything Can Be Anything"

Your vision ("entity starts as nothing; add flags like alive/undead/building") is best modeled as **data**, not as infinite tag-component combinations.

**Recommended pattern:**
- Keep **a small set** of broad partitioning tags (optional).
- Put "arbitrary flags/traits" into:
  - a **bitmask** for common traits (fast), and/or
  - a **trait list** buffer for extended traits.

This prevents combinatorial archetype growth while still allowing insane combinations.

---

## Archetypes & Composition (Mod-Friendly Without Archetype Explosion)

### Standard Entity Shapes

**1) Core Entity (almost everything has this):**
- Minimal, stable set of components.
- Holds identity, transform, high-level flags, and references.

**2) Feature Entities (attached to a core entity):**
- Each feature owns its own data and can be queried independently.
- The feature entity references its owner core entity.

This allows a "living building" by simply attaching a BuildingFeature entity to an Alive core entity (instead of forcing an archetype that is both Building+Alive+… forever).

### What to Avoid

- Unlimited mixing of tag components like `IsAliveTag + IsBuildingTag + IsUndeadTag + ...`.
  - This generates a unique archetype per combination.
  - That becomes unbounded with player creativity.

### Canonical Patterns to Support Player Blueprints

- **BlueprintRef on the core entity** points to an immutable blob describing:
  - base stats
  - trait set
  - capability toggles
  - module IDs
- Systems interpret blueprints and enable/disable capability components or spawn feature entities.

---

## System Ordering & Dependency Philosophy

### Default Fallback Pipeline

PureDOTS systems should align to a clear phase order:

1. **Ingress** (read inputs/events, pull queued commands)
2. **Sense** (gather context: spatial queries, perception caches)
3. **Decide** (AI / task selection; schedules work, emits intents)
4. **Act** (apply intents to state: movement desires, job assignments)
5. **Resolve** (apply consequences: combat, economy, physics results)
6. **Egress** (cleanup, dispatch events, presentation bridging)

### Multiple Behavior Templates (Profiles)

Different entity "outlooks" are supported by **profiles**, not by re-wiring the whole system graph.

Recommended design:
- A `BehaviorProfileId` (or similar) on the entity.
- Systems stay in the global phase order.
- Inside each phase, systems:
  - filter by profile, and/or
  - consult a scheduler component (think budget / next-eval marker).

This gives flexible behavior without turning ordering into an unmaintainable web.

### Ordering Mechanism

Prefer **system groups** over dense `UpdateBefore/UpdateAfter` meshes.

- Top-level groups define the phase order.
- Each domain plugs into the right phase group.
- Use `UpdateBefore/After` only within a domain when absolutely required.

### ISystem vs SystemBase (Project Default)

- **Default in PureDOTS runtime + systems**: `ISystem` (Burst-friendly, no managed state).
- **Allowed `SystemBase`** only for:
  - managed / hybrid bridging,
  - editor-only workflows,
  - Unity APIs that require `SystemBase`.
- If `SystemBase` is used, keep it out of hot-path simulation and isolate it in `Rendering/Authoring/Editor` assemblies.

---

## Structural Changes & ECB Policy

### What "Structural Change" Means

Any of these are structural:
- Create/destroy entities
- Add/remove components
- Add/remove buffers
- Set shared components

Structural changes are expensive and often force sync points.

### Why `EntityCommandBuffer` (ECB)

ECB is a command recorder:
- Jobs can write to ECB safely.
- Playback applies changes later at a known boundary.

### Project Policy

- **Default:** structural changes are recorded to ECB and played back at phase boundaries.
- **Direct `EntityManager` structural ops are restricted** to explicit "structural phases" only.
- Heavy churn should be replaced with:
  - pooling
  - enable-bits
  - feature entities

---

## Blob Assets (Baked + Runtime-Built)

### When to Use Blobs

Use blob assets for:
- read-only config
- lookup tables
- blueprint definitions
- behavior graphs / weights

### Runtime-Built Blobs (Player-Created Content)

Policy:
- Runtime-created blueprints produce a **new** blob.
- Blobs are immutable; "editing" means generating a new blob and swapping references.

### Versioning / Invalidation (Recommended)

Use **content-addressing**:
- compute a `Hash128` from the serialized blueprint/config
- use that hash as the registry key
- same inputs → same blob (dedupe)
- changed inputs → new hash/new blob

Persistence:
- save the serialized form + hash
- rebuild blob on load if missing

---

## Naming Conventions

### Types

- **Components (data):** `Noun` or `NounState` (no `Data` suffix unless ambiguity forces it)
- **Enableable state:** `*State` (enable/disable)
- **Tags:** `*Tag`
- **Buffer elements:** `*Element`
- **Aspects:** `*Aspect`
- **Systems:** `*System`
- **System groups:** `*SystemGroup`
- **Authoring MonoBehaviours:** `*Authoring`
- **Bakers:** `*Baker`
- **Blob roots:** `*Blob` or `*BlobData`
- **Blob refs:** `*Ref` (e.g., `BlueprintRef`)

### Files

- 1 type per file.
- File name matches type name.

### Namespaces

- Assembly root namespace matches assembly name (recommended).
- Domain namespaces: `PureDOTS.<Module>.<Domain>`

---

## Code Organization (Hybrid)

You already have a strong assembly split in PureDOTS (Config/Runtime/Systems/Rendering/Input/Camera/Authoring/Editor).

Within each assembly:
- **by domain**, then **by layer**:

Example:
- `Runtime/Spatial/Components/`
- `Runtime/Spatial/Aspects/`
- `Systems/Spatial/`
- `Config/Blueprints/`
- `Authoring/Spatial/`

Guideline:
- If a folder is "hot path sim," it must stay unmanaged and Burst-friendly.
- Presentation/UnityEngine dependencies stay in Rendering/Authoring/Editor sides.

---

## Performance Guardrails (Non-Negotiable Defaults)

- No per-entity deep decision evaluation without scheduling.
- No unbounded neighbor scans.
- No unbounded relationship graphs.
- Buffers must have a cap policy.
- Minimize archetype churn:
  - prefer enable-bits and pooling
  - prefer feature entities for large/rare capabilities

---

## Status

**Last Updated**: 2025-12-19
**Status**: v1 (active)
