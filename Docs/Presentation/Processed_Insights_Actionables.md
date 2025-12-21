# Processed Insights → Actionables (PureDOTS)

This distills DSP + DW2 public lessons into concrete, engine-level action items for Tri.

## Presentation-only scale contract
- LOD must never alter sim state or entity existence.
- RenderKey.LOD is the only allowed bridge from sim → presentation.
- Culling is presentation-only (RenderCullable), not a sim decision.

## Layered scale + authoring
- Standardize PresentationLayerConfig authoring across games.
- Require a PresentationLayer tag for all renderable entities.
- Enforce layer multipliers in presentation LOD systems; no per-system ad hoc distances.

## Chunking is mandatory for planets
- Planet surfaces must be chunked; per-chunk LOD drives render and update cadence.
- Chunk refresh is idempotent, cacheable, and keyed by (seed, chunk id, lod).

## Cheap distance rendering
- Distant visuals should resolve to icons/impostors via LOD variants.
- High-count visuals use instancing/batching (stars, debris, projectiles, impacts).
- Prefer shader variation over unique meshes/textures at scale.

## Sim performance guardrails (shared)
- Separate strategic caches from tactical updates.
- Cache heavy views and invalidate on explicit events.
- Avoid per-tick rebuilds of empire/region aggregates.

## Travel as cost fields (engine patterns)
- Volumetric modifiers: cost, damage risk, sensor occlusion.
- Navigation uses a cost field; no hard lane graph unless gameplay demands it.

## System ordering expectations
- Presentation LOD runs before render variant resolution.
- Presenter mode selection runs after variant resolution.
- Cached sim views update in a staged pipeline with clear dependencies.

## BAR/Recoil-derived constraints (apply across games)
- Enforce a hard sim/presentation boundary (sim must not read camera/UI/presentation state).
- Expensive systems must be batched, incremental, and multithreaded (pathing/LOS/terrain).
- Use dirty-region invalidation for nav/visibility; avoid global recompute.
- Adopt fail-fast determinism checks for sim-critical mismatches (strict mode).

## Ship interior micro-worlds (streamed presentation)
- Treat interiors as SimInterior (always-on) + PresentInterior (streamed) micro-worlds.
- Stream interior SubScenes by interest (boarding, inspect, cinematic) and keep meta sections resident.
- Use room/portal graphs or DOTS occlusion; toggle visibility via enableables (no structural churn).

## Biodeck + biosculpting (core patterns)
- Biodecks are patch-first environment grids bound to a parent (ship module or world region).
- Biosculpting is event-driven (command buffers) with deterministic ordering.
- Climate/biome updates are dirty-only with hysteresis; no global recompute.
- Vegetation sim uses stands/patches; hero plants are presentation-only when inspected.

## Content registry + presentation contracts (non-negotiables)
- Assets never hang off sim entities: sim stores stable IDs + small overrides only.
- Presentation resolves IDs via catalogs/registry + streaming; editor writes patches/commands only.
- No UnityEngine.Object pointers or AssetDatabase GUIDs in runtime sim state.
- Avoid SharedComponentData for skins/profiles (chunk fragmentation).
 - Use RegistryIdentity + PresentationContentRegistryAsset as the shared spine.

## Registry spine (shared)
- RegistryId is the only cross-project identity.
- Each entry can bind: ECS render (mesh/material/bounds/base scale), hybrid prefab (optional), scene/section ref (optional).
- RenderCatalog + RenderKey stay as the fast path; registry only supplies indices/handles.

## Rendering and transforms
- Use RenderMeshArray + MaterialMeshInfo swaps; avoid per-entity RenderMeshArray.
- Prefer prefab instantiation/pooling over RenderMeshUtility.AddComponents in bulk.
- Use Parent/Child + LocalTransform; PostTransformMatrix only for non-uniform scale.

## Runtime content refs and streaming
- Use WeakObjectReference/UntypedWeakReferenceId + RuntimeContentManager.
- For interiors/modules: EntitySceneReference + SceneSystem.LoadSceneAsync.
- Use Scene Sections to load only what’s needed; keep section 0 meta resident.

## In-game editor rules
- All edits are command streams + patches (ECB for structural changes).
- Persist patches keyed by StableId + component diffs + content IDs.
- Presentation bridge handles gizmos/ghosts/effects; keep it unsynced when appropriate.
