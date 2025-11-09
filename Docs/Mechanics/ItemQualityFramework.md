# Mechanic: Modular Item Quality Framework

## Overview

**Status**: Concept  
**Complexity**: Moderate  
**Category**: Economy / Manufacturing

**One-line description**: Represent crafted items (wagons, weapons, ships) as compositions of DOTS-friendly parts whose quality, rarity, and durability aggregate into the final product’s performance.

## Core Concept

Every manufactured product is an entity with a `CompositeItem` component referencing its part buffers. Parts (wheels, axles, bolts, chassis plates, etc.) carry lightweight data—quality, rarity, durability, material tags—that the simulation uses to compute wear, efficiency, and resale value. This keeps the system deterministic and Burst-friendly while still expressing high-level concepts like “legendary mithril axles” or “cheap iron bolts”.

## Data Representation

```
struct ItemPart : IBufferElementData {
    PartTypeId PartType;
    FixedString64Bytes Material;
    byte QualityTier;     // 0-255, maps to Poor/Common/Rare/Epic etc.
    float Durability;     // 0-1 normalized health of the part
    byte RarityWeight;    // influences crafted item rarity roll
}

struct CompositeItem : IComponentData {
    Entity OwnerEntity;           // wagon, cart, ship
    float DurabilityModifier;     // aggregated result
    byte RarityTier;
}
```

- **QualityTier** drives durability multipliers (e.g., Tier 1 = 0.8 durability, Tier 5 = 1.5).  
- **RarityWeight** feeds into craft outcomes; rare parts push the final product toward higher rarity tiers.  
- **Durability** tracks per-part wear so damage can target the specific component (wheel blowout) without heavy data.

## Aggregation Logic

During crafting or maintenance, a Burst job iterates the part buffer:

```
AggregateDurability = Σ(Durability × QualityMultiplier × PartWeight) / Σ(PartWeight)
AggregateRarity = Clamp( BaseRarity + Σ(RarityWeight × PartInfluence) )
```

Designers define part weights (e.g., wheels contribute 30% of wagon durability, axle 40%, bolts 10%). High-quality wheels with legendary rarity shift the entire wagon upward.

## Gameplay Hooks

- **Durability Drain**: Damage systems subtract wear from part durability; if a part reaches zero the product gains a broken status (e.g., wagon can’t move).  
- **Maintenance**: Craftsmen replace individual parts, updating only the relevant buffer entries, keeping DOTS structural changes minimal.  
- **Economy**: Item value = base cost × quality multipliers; merchants can inspect part metadata to offer custom pricing.  
- **Procedural Generation**: Loot tables emit parts with precomputed quality tiers; assemblers stitch them into final products on the fly.

### Durability Loss & Repair

- **Use-Based Wear**: Each attack, movement cycle, or impact rolls a deterministic chance to reduce part durability (weighted by quality, materials, and expertise). Heavy usage drains durability faster than idling.  
- **Damage Intake/Output**: Delivering or receiving high damage spikes increases wear probability, so frontline equipment degrades faster than decorative gear.  
- **Repair Skill**: Blacksmiths/technicians restore durability up to their expertise cap (e.g., journeyman repairs to 80%, master to 95%, legendary artisan to 100%).  
- **Partial Restoration**: Insufficient skill leaves latent flaws, reducing effective quality until a better artisan performs a full rebuild.  
- **DOTS Implementation**: Repair jobs adjust part durability values in-place; no need for structural swaps unless the part is replaced entirely.

## DOTS Considerations

- **Chunk Efficiency**: Keep part buffers small (e.g., max 16 entries) and store expanded stats (durability, rarity) on the parent to reduce recalculation.  
- **Authoring**: Bakers convert ScriptableObject recipes into `ItemRecipe` blobs that list required part types and their weights.  
- **Networking/Replays**: Since parts are pure data, replication only needs the buffer contents; aggregation can be recomputed deterministically.

## Example: Wagon Assembly

| Part | Quality | Rarity | Weight | Contribution |
|------|---------|--------|--------|--------------|
| Wheel A/B/C/D | Tier 3 | Uncommon | 0.3 total | Good wheels lift durability by +10% |
| Axle | Tier 5 | Rare | 0.4 | Legendary axle boosts rarity tier heading toward “Rare” wagon |
| Bolts Set | Tier 2 | Common | 0.1 | Slight penalty if low quality |

Result: Wagon durability 1.15× base, rarity tier bumped to Rare due to high-tier axle despite common bolts.

## Tech Tier Equivalency & Affixes

To keep loot progression coherent across games:

| Quality | Tech Tier Equivalence | Notes |
|---------|----------------------|-------|
| **Legendary** | ≈ Uncommon item **3 tech tiers higher** | Same baseline stats as far-future tech, plus exclusive affixes (e.g., unique procs, dual-type bonuses). |
| **Epic** | ≈ Uncommon item **2 tech tiers higher** | Gains premium affixes (overcharge efficiency, adaptive resist) unavailable to lower tiers. |
| **Rare** | ≈ Uncommon item **1 tech tier higher** | May roll mid-tier affixes (extra sockets, enhanced durability). |
| **Uncommon/Common** | Baseline for their tech tier | Limited or no affixes. |

- Affixes are quality-gated: legendary-only effects never appear on epics/rares, ensuring quality matters even when raw stats converge.  
- Crafting systems can translate this by applying `TechTierOffset` when aggregating part data; e.g., a legendary axle in a Tier 4 wagon counts as Tier 7 for stat math.  
- Balance remains deterministic: tech tier offsets are additive, not multiplicative, avoiding runaway scaling.

## Integration Steps

1. Define `PartTypeId` enums and recipe data for key products (wagons, dropships, weapon platforms).  
2. Implement crafting/maintenance systems that populate and update part buffers.  
3. Hook damage/physics systems so they read part durability rather than generic HP when applicable.  
4. Expose inspection UI to show part quality summaries without leaking DOTS internals.

---

*Last Updated: October 31, 2025*  
*Document Owner: Systems Team*
