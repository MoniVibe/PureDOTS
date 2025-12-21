# Scale and Zoom Framework (Presentation)

Intent: define a single, continuous presentation space that spans micro to galaxy scale without scene or layer splits. All entities exist together at real-life relative scale; presentation only changes *how* they are shown, never *where* they are.

## Core principles
- One continuous space: no scene or layer separations for presentation.
- Real-life ratios: scale differences should feel physically plausible (cat < craft < carrier < asteroid < planet < system < galaxy).
- No hard bounds: presentation assumes the scene spans the galaxy; culling is LOD-driven, not boundary-driven.
- Proxy allowed, discontinuity not: proxies may replace detail at distance, but must remain anchored to the same absolute position and cross-fade seamlessly.
- Simulation is authoritative: presentation never alters simulation state, only representation.
- Semantic-agnostic scale: size is data-driven. Names like "frigate" or "carrier" do not imply size.

## Scale tiers (presentation meters)
These are order-of-magnitude guides. Exact values can be uniformly scaled as long as ratios remain intact.

| Tier | Example | Typical size | Notes |
| --- | --- | --- | --- |
| Micro | Cat, villager, props | 0.5m - 5m | Interior/close-up scale (bridge, hangar). |
| Craft | Shuttles, fighters | 10m - 100m | Small vessels around carriers. |
| Carrier | Capital ships | 0.5km - 10km | Light carriers ~0.5km; superheavy 5-10km. |
| Titan | Supercapital, mega hulls | 20km - 200km | Massive battle anchors. |
| Mega-titan | Megastructure ships | 500km - 2,000km | Rare, galaxy-shaping assets. |
| Asteroid | Resource bodies | 10km - 200km | Overlaps titan scale; some larger than carriers. |
| Planet surface | Continents, cities | 100km - 10,000km | Surface detail resolves when near. |
| System | Orbits, moons | 10,000km - 1,000,000km | Orbital battles live here. |
| Galaxy | System placement | 1e9km+ | Map-level aggregation. |

## LOD and proxy rules
- All tiers share a single coordinate space; LOD only changes representation.
- Proxies must preserve absolute position and scale cues (size, orbit, relative spacing).
- Cross-fade or staged swap is required; no popping between layers.
- Small entities can remain visible as LOD glyphs at distance (dots, billboards, HUD anchors).
- Planets may use proxy spheres until approach thresholds, then resolve to surface detail.
- Micro detail must remain inspectable during macro battles (e.g., bridge-level duels inside a mega-scale conflict).

## Transition behavior
- Orbit-to-surface must be continuous. The player can zoom from orbital battle scale into surface scale without reloading a scene.
- Presentation may use floating-origin or camera-relative rendering to protect precision, but it must not alter perceived positions or scale ratios.

## Implementation touchpoints (present)
- Render catalogs define mesh bounds; bounds must reflect intended scale to avoid over-culling.
- Presentation systems set per-entity scale and offsets; they are the primary knobs for tier separation.
- Camera zoom thresholds must align with tier transitions (micro <-> craft <-> carrier <-> orbital <-> system <-> galaxy).

## Non-goals
- This document does not define AI behavior or simulation scale.
- This document does not mandate a specific rendering technique (entities graphics vs hybrid vs VFX).

## Validation checklist
- Zoom from micro to galaxy without any scene swaps or discontinuities.
- Craft read clearly as tiny next to carriers; carriers read tiny next to large asteroids.
- Planets remain readable at distance and resolve to surface-scale detail when approached.
- A micro-scale duel remains coherent while macro-scale battles stay visible in the same continuous space.
