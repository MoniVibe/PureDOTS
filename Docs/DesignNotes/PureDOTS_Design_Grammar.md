# PureDOTS Design Grammar (v0)
Date: 2026-02-09
Owner: shonh
Status: draft

## Purpose
Define a shared design grammar so entities can generate ship/weapon/module designs that express their profiles without violating physical or balance constraints.

This grammar enables:
- Modular, Borderlands-style composition.
- Profile-driven bias (lawful/chaotic/materialist/etc.).
- Validation and cost checks separate from creative freedom.

## Core Concepts
- **Blueprint**: declarative graph describing a ship or weapon build.
- **Module**: an entity with slots (can hold sub-modules).
- **Slot**: a typed connector with constraints and capacity.
- **Profile Bias**: weights that influence module selection and tuning.
- **Validation Gate**: hard/soft checks applied after generation.

## Design Object Model
### Blueprint
- blueprintId
- rootModuleId
- profileBiasId
- seed
- intentTags[] (e.g., "chaotic", "long_range_flak", "boarding")
- modules[]

### Module
- moduleId
- moduleType
- tier
- tags[]
- stats{}
- slots[]
- submodules[]

### Slot
- slotId
- slotType
- capacity
- constraints{}
- allowedTags[]

## Grammar (EBNF-ish)
Blueprint := RootModule
RootModule := HullModule [Subsystems]
Subsystems := (Propulsion | Power | Weapons | Defense | Sensors | Crew | Utility)+
Weapons := WeaponModule [AmmoModule] [ProjectileModule]
ProjectileModule := BehaviorModule [EffectModule]*

## Module Types (Shared)
- Hull, Propulsion, Power, Weapons, Defense, Sensors, Crew, Utility
- Ammo, Projectile, Behavior, Effect

## Profile Bias (Examples)
- Lawful: low variance, prefers vetted modules, conservative ratios.
- Chaotic: high variance, mixed ammo, experimental ratios.
- Materialist: drones/automation bias.
- Spiritual: mana/psi bias.
- Warlike: aggressive cadence, high DPS risk.
- Pure: support/repair bias.
- Corrupt: risky/high collateral bias.

## Validation Gates
### Hard Gates (fail build)
- Power draw <= capacity
- Mass <= thrust * ratio
- Heat <= dissipation
- Slot constraints satisfied

### Soft Gates (score penalties)
- Efficiency below threshold
- Reliability below threshold
- Excess ammo volatility

## Cost Model
- Costs scale with mass, power, heat, and maintenance.
- Risky designs can pass but accumulate reliability penalties.

## Example Blueprints (Sketches)
### Lawful Testudo Cruiser (lawful + pure)
Intent: defensive formation anchor, predictable cadence.
```
blueprintId: ship.testudo_cruiser.v0
profileBiasId: lawful_pure
seed: 1101
intentTags: ["formation", "defensive", "predictable"]
modules:
  - moduleId: hull.cruiser_a
    moduleType: Hull
    slots: [power, propulsion, defense, weapons, sensors, crew]
  - moduleId: power.stable_core
    moduleType: Power
    tags: ["stable", "low_variance"]
  - moduleId: defense.shield_bubble
    moduleType: Defense
    tags: ["barrier", "cohesion_boost"]
  - moduleId: weapons.burst_laser_battery
    moduleType: Weapons
    tags: ["disciplined", "low_heat_spike"]
  - moduleId: crew.cohesion_matrix
    moduleType: Crew
    tags: ["formation"]
```

### Chaotic Mixed-Ammo Raider (chaotic + corrupt + warlike)
Intent: unpredictable payloads, high variance, risky ratios.
```
blueprintId: ship.chaos_raider.v0
profileBiasId: chaotic_corrupt_warlike
seed: 667
intentTags: ["mixed_ammo", "high_variance", "shock"]
modules:
  - moduleId: hull.raider_b
    moduleType: Hull
  - moduleId: propulsion.overthrust
    moduleType: Propulsion
    tags: ["unstable_heat_curve"]
  - moduleId: weapons.everything_torpedo_launcher
    moduleType: Weapons
    slots: [ammo, projectile]
  - moduleId: ammo.mixed_magazine
    moduleType: Ammo
    stats: { mix: ["plasma", "kinetic", "emp"] }
  - moduleId: projectile.behavior.scatter
    moduleType: Behavior
    tags: ["wide_spread"]
  - moduleId: projectile.effect.cluster_burst
    moduleType: Effect
```

### Materialist Drone Carrier (materialist + cooperative)
Intent: drone-heavy force multiplier, high automation.
```
blueprintId: ship.drone_carrier.v0
profileBiasId: materialist_cooperative
seed: 2402
intentTags: ["drones", "automation", "support"]
modules:
  - moduleId: hull.carrier_c
    moduleType: Hull
  - moduleId: utility.drone_bay
    moduleType: Utility
    tags: ["repair_drones", "intercept_drones"]
  - moduleId: weapons.point_defense_grid
    moduleType: Weapons
    tags: ["intercept_focus"]
  - moduleId: sensors.targeting_net
    moduleType: Sensors
    tags: ["drone_tasking"]
```

### Spiritual Psionic Artillery (spiritual + warlike)
Intent: long-range precision, focus-driven cadence.
```
blueprintId: ship.psionic_artillery.v0
profileBiasId: spiritual_warlike
seed: 303
intentTags: ["long_range", "focus_cadence", "precision"]
modules:
  - moduleId: weapons.psi_lance_array
    moduleType: Weapons
    tags: ["focus_dependent"]
  - moduleId: utility.focus_conduit
    moduleType: Utility
    tags: ["mana_regen", "timing_window_bonus"]
```

## Telemetry Keys
- design.valid
- design.score
- design.profile_bias
- design.risk_penalty

## Notes
Games skin the outcomes, but the grammar + validation live in PureDOTS.
