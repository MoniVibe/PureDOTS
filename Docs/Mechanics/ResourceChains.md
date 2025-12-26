# Mechanic: Resource Chains

## Overview

**Status**: Concept  
**Complexity**: Moderate  
**Category**: Economy

**One-line description**: A concise set of resource transformations keeps production interesting without overloading players with materials.

CONTRACT:RESOURCE.CHAINS.V1

## Depends on
- CONTRACT:RESOURCE.LOGISTICS.V1
- CONTRACT:PRODUCTION.ACCOUNTING.V1
- CONTRACT:QUALITY.ITEM.V1

## Provides
- Deterministic resource family taxonomy and recipe boundaries.

## Consumes
- Resource catalogs, recipe catalogs, facility throughput.

## Invariants
1. Resource types are stable IDs across save/load.
2. Quality/rarity/decay are quantized.
3. Recipes use fixed inputs/outputs per version.

## Allowed staleness
- Catalog updates apply on load or explicit hot-reload only.

## Failure handling
- Invalid recipes fail closed and emit catalog errors; no partial outputs.

## Telemetry/Test hooks
- Recipe execution counts, invalid recipe count, batch split count.

## Contract test
- Recipe execution never produces negative inventory or mismatched outputs.

## Core Concept

The economy relies on a handful of intuitive resource families that flow from mining to processing to construction. We emphasise depth through combinations rather than volume of unique items, enabling players to understand supply lines quickly while still making meaningful tradeoffs.

## Foundational Taxonomy

| Family | Raw Input | Refined Output | Advanced Composite | Notes |
|--------|-----------|----------------|--------------------|-------|
| Metals | Iron Ore | Iron Ingots | Steel (Iron + Carbon) | Baseline structural material for most builds. |
| Advanced Metals | Titanium Ore | Titanium Ingots | Plasteel (Steel + Polymers) | Used for hulls, armour, high-stress components. |
| Organics | Biomass | Nutrients | Biopolymers (Nutrients + Petrochemicals) | Supports life support, biotech modules. |
| Petrochemicals | Hydrocarbon Ice | Refined Fuels | Polymers (Refined Fuels + Catalysts) | Drives propulsion, forms composites. |
| Electronics | Rare Earths | Conductors | Quantum Cores (Conductors + Silicates) | Powers advanced systems, sensors, AI cores. |

### Combination Conventions

- **Steel** = Iron Ingots + Carbon (refined from biomass or hydrocarbon sources).
- **Plasteel** = Steel + Polymers.
- **Composite Alloys** = Plasteel + Quantum Cores (for late-game hulls and reactors).
- **Circuit Mesh** = Conductors + Polymers (baseline electronics output).
- **Life Support Kits** = Nutrients + Polymers (colony growth accelerators).

These placeholders set expectations for future expansion without committing to exhaustive chains. Each combination should appear in no more than two tiers to keep logistics manageable.

## Processing Principles

- Facilities may specialise (e.g., refinery vs fab) or operate as hybrids on mobile carriers.
- Recipes remain data-driven so modders can define alternate inputs if they alter starting conditions.
- Conversion ratios stay simple (e.g., 2:1 raw to refined) unless a tech upgrade deliberately breaks the rule.

## Resource Logic Contract (Tightening)

- **Stable IDs**: `ResourceTypeId` values are stable across save/load and catalog merges.
- **Quantized grades**: quality/rarity/decay are quantized to prevent infinite lot fragmentation.
- **Recipe determinism**: inputs and outputs are fixed per recipe version; upgrades swap the recipe, not its math.
- **Yield gates**: higher-tier facilities can improve yield, never silently change inputs.

## Integration Touchpoints

- **Mining Loop** supplies raw families; exploration identifies rare sources for advanced composites.
- **Construction Loop** consumes refined and composite outputs, with project blueprints specifying tier requirements.
- **Haul Loop** moves intermediate goods between processing nodes and build sites.

## Tuning Guardrails

- Keep total unique resource SKUs under ~12 for base game to avoid bloat.
- Focus complexity on production choices (which composites to prioritise) and logistics rather than memorising recipes.
- Introduce tech upgrades that consolidate steps (e.g., direct Plasteel fabrication) as late-game pacing levers.

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-10-31 | Initial draft | Established low-bloat resource framework |

---

*Last Updated: October 31, 2025*  
*Document Owner: Design Team*
