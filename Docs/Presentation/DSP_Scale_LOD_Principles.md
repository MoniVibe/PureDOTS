# DSP-Style Scale + LOD Principles (PureDOTS)

This captures the publicly described DSP approach and maps it into Tri/PureDOTS constraints.

## Non-negotiables
- Simulation remains fixed-step and deterministic.
- Presentation is the only layer that scales, culls, or swaps impostors.
- LOD must never change gameplay state or entity existence.

## Presentation-only scale compression
- Use layer-based multipliers to control perceived scale without touching sim state.
- Presentation layers should adjust:
  - RenderKey.LOD
  - cull distances
  - impostor/icon swaps
  - update cadence for presentation-only systems

## Planets: chunk first, then LOD inside chunks
- Planet surfaces are chunked; do not render entire planets as monoliths.
- Per-chunk visibility + LOD computed from camera distance and layer multipliers.
- Chunk generation/refresh must be idempotent and cacheable (seed + chunk id + LOD).

## Parallelism and offload
- Use staged pipelines when dependencies exist.
- Within a stage, keep work order-independent and job-friendly.
- Push embarrassingly parallel updates to Burst or GPU.

## Engine hooks to standardize
- PresentationLayerConfig (authoring + singleton) drives per-layer distance multipliers.
- RenderKey.LOD is the canonical bridge to impostor/icon selection.
- RenderCullable distance is presentation-only and can be tuned per layer.

## Reminders
- Accuracy vs scale is a design choice: compressing visible scale is allowed, simulation scale is not.
- Any scale or LOD change must be reversible and non-destructive to sim state.
