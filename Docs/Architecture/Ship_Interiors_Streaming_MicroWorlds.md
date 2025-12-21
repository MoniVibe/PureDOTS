# Ship Interiors as Streamed Micro-Worlds (Sim Always-On, Presentation Optional)

**Status**: Locked (architecture + workflow contract)  
**Category**: Architecture / Streaming / Space4X (with shared patterns)  
**Applies To**: Space4X primarily, reusable patterns for PureDOTS + Godgame “micro worlds”  

Goal:
> Treat ship interiors as **streamed micro-worlds**: simulation is always running, but geometry/animated crew are only materialized when needed (player boards, infiltration/boarding event, cinematic, debug inspect).

This prevents “every corridor on every ship” from drowning performance, while still enabling rich interior gameplay when relevant.

Related:
- `Docs/Architecture/Scalability_Contract.md` (resident vs virtual; hot vs cold; enableables; anti-patterns)
- `Docs/Architecture/Performance_Optimization_Patterns.md` (strict phase order; budgets; tier profiles)
- `Docs/Architecture/Senses_And_Comms_Medium_First.md` (compartment graphs for smell/sound; vacuum disables hearing/smell)
- `Docs/Architecture/Save_Load_Determinism.md` (sim deterministic; presentation derived)
- `Docs/Concepts/Core/Authority_And_Command_Hierarchies.md` (crew departments + seats live in SimInterior)

---

## 1) Separate the problem: `SimInterior` vs `PresentInterior`

### 1.1 SimInterior (always-on, headless-safe)
Authoritative interior simulation runs regardless of streaming state:
- compartment graph (rooms/edges/doors/hatches/vents),
- crew assignments and department readiness,
- hazards (fire, decompression, smoke/toxins, power loss),
- boarding/infiltration state,
- locks/permissions/alarms.

Hard rule:
- SimInterior must never depend on whether an interior SubScene is loaded.
  - Streaming is async and presentation-only.

### 1.2 PresentInterior (optional, streamed)
Materialized only when needed:
- interior geometry, decals, props,
- interior cameras/audio/occlusion,
- animated crew (visuals only),
- debug overlays and “walkaround” views.

PresentInterior reads SimInterior state; it does not authoritatively change it.

---

## 2) Authoring contract: “Module = layout scene”, “Deck = section”

### 2.1 Use SubScenes for authoring + build-time baking
Interiors are authored as scenes/SubScenes so they can be baked and streamed.

Do not string-load scenes. All interior scenes must be referenced by data assets so builds include them.

### 2.2 Recommended asset layout (Space4X)
- `ShipHullInterior.scene`  
  Core interior root + deck roots + minimal “interior meta” authoring.
- `Module_<Type>_<Variant>.scene`  
  The layout for a ship module (rooms, props, interaction anchors).
- `Skin_<Theme>.scene` (optional)  
  Presentation overlays only (materials/decals/props); no sim-critical state.

---

## 3) Streaming granularity: Scene Sections for multi-deck/selective load

Split interiors into scene sections so you can load only what is needed.

Practical section scheme:
- **Section 0**: minimal interior meta (interior root, deck transforms, compartment graph nodes)
- **Section 10x**: Deck 0 geometry + props
- **Section 20x**: Deck 1 geometry + props
- **Section 30x**: Deck 2 geometry + props
- **Section 1000+**: optional “high detail” dressings (FPS camera only)

Hard rule:
- avoid a single giant interior SubScene per ship (forces everything to load).

---

## 4) Runtime: interior streaming system (presentation-owned)

Implement a presentation-side streaming system (conceptual name: `ShipInteriorStreamingSystem`) that:
1) computes **interest**:
   - player camera inside,
   - boarding/infiltration action active,
   - cinematic/debug-inspect active,
2) resolves required interior scenes/sections,
3) loads required sections (and unloads when interest drops).

Notes:
- unloading may cause structural changes; prefer keeping **Section 0 meta** resident to speed reload and preserve stable references.

---

## 5) Occlusion/bulkheads: pick a culling strategy (don’t mix paradigms)

### Strategy A — Entities Graphics Burst occlusion culling (good for rooms)
Use when interior geometry is DOTS-rendered and room-separated.
- bulkheads/walls act as occluders (use low-poly occluder meshes).
- tune occlusion view buffers for cost/quality.

Constraint:
- skinned meshes aren’t supported as occluders/occludees; crew visuals must be handled via LOD/frustum or instanced/VAT-style animation.

### Strategy B — Room/portal visibility graph (deterministic, door-aware)
Use when you want strict, deterministic visibility:
- compartments are nodes; doors/hatches are edges (open/closed gates).
- for active interior camera: BFS out through open edges to depth N and enable render only for those rooms.
- implement render toggles via **enableable components** (no structural churn).

Hard rule:
- don’t rely on classic GameObject portal systems; they don’t compose cleanly with DOTS streaming/culling.

---

## 6) Crew visualization without a sim explosion

Contract:
- crew tasks are always simulated (SimInterior); visuals are optional (PresentInterior).

When PresentInterior loads:
- materialize “crew visuals” as lightweight presentation entities (or pooled hybrids only if necessary).
- fully animate only near the active camera; far crew become impostors/idle loops.

Baseline recommendation:
- prefer instanced/VAT-style animation for crowds if you need density;
- otherwise cap visible room crew counts and rely on culling/LOD.

---

## 7) Multi-deck placement & layouts (data-driven)

Make hulls and modules purely data-driven:
- hull defines deck stack (deck transforms + allowable slot grids),
- modules declare:
  - `DeckIndex`
  - `SlotFootprint`
  - `InteriorSceneRef` (layout)
  - optional `SkinRef` (presentation only)

When a module moves decks or changes skin:
- unload old module sections, load new ones.

No hand-edited interior per placement.

---

## 8) Headless & testing (core workflow)

SimInterior must run in headless mode for:
- determinism checks,
- performance checks,
- smoke/proof validation.

Recurring mistake:
- testing interiors only with full rendering enabled (you miss sim correctness and tier/streaming regressions).

---

## 9) Recurring errors to actively block

- Sim depends on streamed SubScene presence (async streaming will break sim).
- One giant subscene per ship (loads everything).
- Interior scenes loaded by strings (build won’t include them; nondeterministic content state).
- Structural churn for visibility toggles (use enableables).
- “Animate everyone” (crew visuals must be tiered and bounded).

