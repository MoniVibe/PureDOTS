# Entity Anatomy, Status, Memory (MVP)
**Status**: Draft (implementation-aligned)  
**Category**: Core / Combat / AI  
**Applies To**: PureDOTS, Space4X, Godgame

This doc captures the MVP-level anatomy and cognitive state scaffolding that keeps the simulation flexible
across races/species while remaining lightweight for headless runs.

---

## Anatomy (limbs + organs)

**Goal**: Entities can have different body layouts (organs, limbs, prosthetics) without code changes.

**Core components**
- `AnatomyId` (component): stable id string (e.g., `humanoid`, `insectoid`, `drone`).
- `AnatomyCatalogRef` (singleton): blob catalog mapping `AnatomyId → body part list`.
- `BodyPartState` (buffer): runtime state for each part (health, flags, parent link).
- `BodyPartDamageEvent` (buffer): routed damage to specific parts (optional).

**Notes**
- Catalog entries are data-driven; different races/species can share or override anatomies.
- If an entity has `AnatomyId`, `AnatomyBootstrapSystem` seeds its `BodyPartState`.
- If a **vital** part is destroyed, `BodyPartDamageSystem` issues a `DeathEvent`.

---

## Status Effects (buffs/debuffs)

Status effects are handled by the existing **Buff system**:
- `ActiveBuff` buffer holds current effects.
- `BuffStatCache` aggregates modifiers for hot-path combat/AI reads.
- `BuffApplicationSystem` + `BuffTickSystem` manage timing, stacking, and periodic effects.

This keeps buffs/debuffs fully data-driven and shared between Space4X and Godgame.

---

## Memory + Knowledge (cognitive facts)

**Generic memory**
- `MemoryEntry` buffer: `MemoryId`, `Magnitude`, `DecayHalfLife`, `RelatedEntity`.
- `MemoryAddRequest` buffer: requests to add/refresh memory.
- `MemorySystem` decays and prunes memories to keep buffers small.

**Knowledge facts**
- `KnowledgeFact` buffer: discrete facts with confidence + decay.
- `KnowledgeFactRequest` buffer: add/refresh facts with apply modes.
- `KnowledgeFactSystem` handles decay and pruning.

These components are **intentionally minimal** and can coexist with
domain-specific knowledge systems (lessons, diffusion, morale memory, etc.).

---

## Flexibility Rules

- **Anatomy is data**: No hardcoded limb counts. Use catalogs per race/species/variant.
- **Memory & knowledge are optional**: Entities only carry buffers when needed.
- **Status effects are unified**: Buffs/debuffs are the canonical representation.

