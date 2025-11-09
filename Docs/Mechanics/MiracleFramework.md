# Mechanic: Miracle Command Framework

**Status**: Concept  
**Complexity**: Complex  
**Category**: Systems / Commands

## Overview

Provide a deterministic, DOTS-friendly way to queue, simulate, and resolve miracles (rain, water burst, fire, lightning, heal, time effects). The framework exposes reusable components and systems so projects can author miracle variations without rewiring the command pipeline.

## Command Flow

1. **Request**: UI or gameplay logic writes a `MiracleRequest` to a command buffer, specifying miracle type, location, intensity, and caster/god references.
2. **Validation**: `MiracleEligibilitySystem` checks resources (prayer, mana, focus), alignment, cooldowns, and environmental constraints.
3. **Execution**: Type-specific systems spawn effect entities (rain clouds, fire pulses) with deterministic life cycles.
4. **Resolution**: Effects apply status changes (wetness, burning, healed, slowed time) and consume resources.

## Core Components

```
struct MiracleRequest : IBufferElementData {
    MiracleType Type;
    float3 Position;
    float Intensity;
    Entity Caster;
}

struct MiracleCooldown : IComponentData {
    float RemainingSeconds;
}

struct RainCloudComponent : IComponentData {
    float Mass;
    float Moisture;
    float GlideSpeed;
    float Altitude;
}

struct MoistureBurst : IComponentData {
    float Radius;
    float WaterAmount;
}

struct FirePulse : IComponentData {
    float Radius;
    float DamagePerSecond;
    float SpreadFactor;
}

struct LightningArc : IComponentData {
    Entity Target;
    float Damage;
    float ChainRange;
}

struct HealPulse : IComponentData {
    float Radius;
    float HealAmount;
    float CleanseStrength;
}

struct TimeDistortion : IComponentData {
    float Radius;
    float SpeedMultiplier; // >1 haste, <1 slow
    float Duration;
}
```

## Systems

- `MiracleEligibilitySystem`: Validates resources, faith density modifiers, and mana debt penalties.
- `RainMiracleSystem`: Spawns `RainCloudComponent` entities, handles glide + dispersion.
- `WaterBurstSystem`: Resolves moisture explosions (extinguish fires, boost crops).
- `FireMiracleSystem`: Applies fire pulses, integrates with building/crop durability.
- `LightningMiracleSystem`: Traces arcs, applies damage and shock statuses.
- `HealMiracleSystem`: Applies heal pulses, interacts with medical/healthcare systems.
- `TimeMiracleSystem`: Applies haste/slow zones, updates focus/mana regen rates.
- `MiracleCleanupSystem`: Reclaims effect entities when finished.

## Resource Hooks

- Works with prayer power + focus + mana debt rules; cost modifiers from faith density are applied during validation.
- Supports overspending (entering mana debt) with consequences from the focus doc.

## Integration Notes

- All miracle entities carry `PlaybackGuardTag` to stay deterministic under rewind.
- Author ScriptableObject configs for base miracles; bakers convert them into BlobAssets consumed by runtime systems.
- Presentations (VFX/SFX) subscribe via companion entities, keeping core systems pure DOTS.

---

*Last Updated: October 31, 2025*  
*Owner: Systems Team*
