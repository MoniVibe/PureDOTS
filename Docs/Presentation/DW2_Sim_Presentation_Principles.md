# DW2-Style Sim + Presentation Principles (PureDOTS)

This captures publicly described DW2 approaches and maps them into Tri/PureDOTS constraints.

## Core premise
- The galaxy stays alive in simulation.
- Performance comes from caching + invalidation in sim, and aggressive batching/instancing in presentation.

## Sim domain separation
- Strategic/empire pass: periodic, cached, coarse-grained state.
- Tactical/unit pass: high-frequency, local decision-making.
- Avoid recomputing empire-scale aggregates every tick.

## Cache + invalidate discipline
- Cache heavy, empire-wide views (reachability, threat maps, logistics, resource flows).
- Add explicit invalidation triggers for key events (ownership change, hub destroyed, treaty change, new colony, etc.).
- Avoid caches with no invalidation or invalidations that force full rebuilds too often.

## Presentation domain (cheap at distance)
- Omit small meshes at distance; use icons/impostors instead.
- Batch/instance high-count visuals (stars, props, projectiles, impacts).
- Prefer procedural shader variation over per-asset uniqueness for large counts.

## Travel as cost fields (not hard lanes)
- Model nebulae/storms as volumes that modify cost, damage risk, and sensor occlusion.
- Navigation should be a best-path over cost fields, not fixed lanes.

## Ownership domains (economy)
- Separate state vs private economy controllers with narrow interfaces (fees, taxes, requests).
- Avoid cross-system writes into a shared ledger without clear ownership.
