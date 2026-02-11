# Firing Range Scenario (PureDOTS v0)
Date: 2026-02-09
Owner: shonh
Status: draft

## Intent
A reusable stress-and-skill scenario to validate weapon/projectile behavior, accuracy, cadence, and response policies.
Firing range is the cleanest place to test deflection, ammo/energy constraints, and profile-driven decisions.

## Core Loop
- Shooters select targets and fire in controlled lanes.
- Targets vary in armor/shield type and motion profile.
- Range officers enforce penalties for friendly fire and unsafe behavior.

## Entities
- **Shooter**: weapon mount, resource pools, profile biases.
- **Target Drone**: armor/shield variants, optional stealth.
- **Range Officer**: rule enforcement + penalties.
- **Spectators** (optional): morale modifiers and event logging.

## Data Primitives (Draft)
### Weapon + Projectile
- `WeaponMount` (cadence, energy/heat, target intent)
- `WeaponCatalog`, `ProjectileCatalog` (spec blobs)
- `WeaponMagazine`, `AmmoStockpile` (ammo type mix, reload policy)
- `ProjectileSpec`, `ProjectileSignature` (ballistic/homing, countermeasures)
- `ProjectileControlRequest` (redirect/hijack/disrupt hooks)

### Resources
- `WeaponMount.EnergyReserve` (per mount or shared)
- `WeaponMount.HeatLevel` (cooldown throttling)
- `DeflectionBudget` (energy/mana/focus pool for defense)

### Profile + Relations
- `EntityProfile` (lawful/chaotic/materialist/spiritual)
- `FactionRelationships` + `RelationEvent` (friend/neutral/hostile)
- `FriendlyFireIncident` + `FriendlyFirePenalty` (morale + cohesion impact)

### Scheduling + Discipline
- `EntityRoutine` + `RoutineSchedule` (drill cadence)
- `ScheduleAdherence` (punctuality vs chaos)

### Scoring + Morale
- `RangeScore` (accuracy, time-to-hit, waste)
- `SquadCohesion` + `CohesionCombatMultipliers`
- `SafetyViolations` (penalty stack)

## Behavior Notes
- **Negligible Threat Policy**: do not waste ammo/energy on harmless projectiles.
- **Dodge vs Deflect**: higher finesse entities choose dodge more often if success probability is high.
- **Chaotic Profiles**: mixed ammo mags, risky cadence; can trigger range incidents.
- **Lawful Profiles**: disciplined cadence, structured formations, consistent loadouts.

## Tuning Knobs
- Target speed/rotation (fast drones vs slow hulks).
- Armor/shield type (kinetic-weak, energy-weak, balanced).
- Cloak/stealth toggles (reduced detection, increased miss rate).
- Turret rotation limits (slow capital vs agile craft).
- Ammo mix ratios and reload policies.
- Deflection and control budgets (energy/mana/focus).

## Scenario Variants
1) **Ballistic Drill**: railguns vs static targets.
2) **Homing Drill**: launchers vs moving targets.
3) **Friendly-Fire Stress**: tight lanes, higher accidental hits.
4) **Cloak Test**: partial visibility targets.
5) **Cohesion Race**: squads compete for accuracy/time.

## PureDOTS Mapping
- Scenario bootstrap spawns shooters + targets only.
- Presentation deferred; everything data-only.
- Default IDs: `weapon.basic.railgun`, `weapon.basic.launcher`.

## Notes
- Designed for deterministic headless runs.
- Metrics to add later: hit ratio, energy per hit, friendly-fire rate, cohesion drift.
