# Weapon + Projectile Primitives (PureDOTS)
Date: 2026-02-09
Owner: shonh
Status: draft

## Purpose
Define the data-only pipeline for weapons to spawn projectiles and deliver damage.
This is the shared PureDOTS baseline used by Space4X and Godgame skins.

## Pipeline (Data Flow)
1) **WeaponMount** chooses to fire (target present + resources).
2) **WeaponProjectileSpawnSystem** creates `ProjectileSpawnRequest` buffer entries.
3) **ProjectilePoolSpawnSystem** activates pooled projectiles and writes `ProjectileEntity`.
4) **ProjectileFlightSystem** advances projectiles (ballistic/homing).
5) **ProjectileCollisionSystem** (optional physics) detects impacts and writes hit buffers.
6) **ProjectileDamageSystem** creates `DamageEvent` for target.
7) **DamageApplicationSystem** applies health/shield/armor changes.

## Core Data Components
- `WeaponMount`: weapon ID, target entity/position, energy/heat, cadence.
- `WeaponCatalog`: blob of `WeaponSpec`.
- `ProjectileCatalog`: blob of `ProjectileSpec`.
- `ProjectileEntity`: active projectile state.
- `ProjectileSpawnRequest`: buffered spawn requests.
- `ProjectilePoolConfig`: pool prefab + capacity.
- `Health`, `DamageEvent`, `DeathEvent`: damage resolution.
- `AmmoStockpile`, `WeaponMagazine`: ammo primitives for reload/consumption.
- `DeflectionProfile`, `DeflectionBudget`, `DeflectionRequest`: deflection primitives.
- `ProjectileControlRequest`, `ProjectileControlState`: redirect/hijack/disrupt hooks.
- `FriendlyFireIncident`, `FriendlyFirePenalty`: relation/morale hooks for accidents.
- `EngagementEnvelope`, `PositioningIntent`, `StrafeProfile`: positioning primitives.

## Defaults (v0)
Weapons:
- `weapon.basic.railgun` (MassDriver)
- `weapon.basic.launcher` (Missile)

Projectiles:
- `projectile.simple.ballistic`
- `projectile.simple.homing`

## Notes
- Presentation systems are optional; this pipeline runs headless.
- Physics colliders are optional; damage system can run with target-only data.
- The catalog design is intentionally minimal; games expand as needed.

## Next Extensions
- Ammo depletion behaviors (batch reloads, mixed magazines, quality variance).
- Deflection resolvers (shield hits, limb deflect, drone screens).
- Projectile control systems (ECM or mana sway, countermeasure suites).
- Weapon profile biases (lawful/chaotic/materialist) for generator pipelines.
