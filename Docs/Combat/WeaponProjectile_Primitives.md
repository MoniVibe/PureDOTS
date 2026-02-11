# Weapon + Projectile Primitives (PureDOTS)
Date: 2026-02-09
Owner: shonh
Status: draft

## Purpose
Define the data-only pipeline for weapons to spawn projectiles and deliver damage.
This is the shared PureDOTS baseline used by Space4X and Godgame skins.

## Pipeline (Data Flow)
1) **WeaponMount** chooses to fire (target present + resources).
2) **WeaponProjectileSpawnSystem** creates `ProjectileSpawnRequest` entries.
3) **WeaponSpawnerSystem** emits `ProjectileSpawnRequest` for data-only spawners.
4) **ProjectilePoolSpawnSystem** activates pooled projectiles and writes `ProjectileEntity`.
5) **ProjectileFlightSystem** advances projectiles (ballistic/homing).
6) **ProjectileCollisionSystem** (optional physics) detects impacts and writes hit buffers.
7) **ProjectileEffectExecutionSystem** processes OnHit + ammo effects → `DamageEvent`.
8) **DamageApplicationSystem** applies health/shield/armor changes.

## Core Data Components
- `WeaponMount`: weapon ID, target entity/position, energy/heat, cadence.
- `WeaponSpawner`: data-only emitter (aim mode + ammo ID).
- `WeaponCatalog`: blob of `WeaponSpec`.
- `ProjectileCatalog`: blob of `ProjectileSpec`.
- `AmmoCatalog`: blob of `AmmoSpec`.
- `ProjectileEntity`: active projectile state.
- `ProjectileSpawnRequest`: buffered spawn requests.
- `ProjectilePoolConfig`: pool prefab + capacity.
- `Health`, `DamageEvent`, `DeathEvent`: damage resolution.
- `AmmoStockpile`, `WeaponMagazine`, `AmmoConsumptionRequest`: ammo primitives.
- `DeflectionProfile`, `DeflectionBudget`, `DeflectionRequest`: deflection primitives.
- `ProjectileControlRequest`, `ProjectileControlState`: redirect/hijack/disrupt hooks.
- `FriendlyFireIncident`, `FriendlyFirePenalty`: relation/morale hooks for accidents.
- `EngagementEnvelope`, `PositioningIntent`, `StrafeProfile`: positioning primitives.
- `WeaponPoolConfig`, `WeaponPoolEntry`: shipyard/pool selection.
- `WeaponInstallRequest`, `ShipyardEquipRequest`: install + shipyard equip queue.

## Defaults (v0)
Weapons:
- `weapon.basic.railgun` (MassDriver)
- `weapon.basic.launcher` (Missile)

Projectiles:
- `projectile.simple.ballistic`
- `projectile.simple.homing`

Ammo:
- `ammo.standard` (baseline, no modifiers)
- `ammo.kinetic` (pierce bonus, +damage, +knockback)
- `ammo.he` (AoE effect, larger blast)
- `ammo.emp` (status effect, lightning + ignore shield)
- `ammo.arc` (chain effect, lightning + chain flag)

## Ammo + Effects (Primitives)
**AmmoSpec** modifies projectile behavior and OnHit effects:
- Multipliers: `DamageMultiplier`, `SpeedMultiplier`, `LifetimeMultiplier`, `TurnRateMultiplier`, `SeekRadiusMultiplier`, `AoERadiusMultiplier`, `ChainRangeMultiplier`, `KnockbackMultiplier`
- Additive: `PierceBonus`
- Damage overrides: `DamageTypeOverride`, `DamageFlags`
- `OnHitAdd`: appended EffectOps (AoE, Chain, Status) layered on projectile OnHit.

**EffectOp** (OnHit / OnHitAdd):
- `Damage`: direct damage
- `AoE`: area damage (Aux = radius)
- `Chain`: chain damage (Aux = range)
- `Status`: buff/debuff request (StatusId)
- `Knockback`, `SpawnSub`: stub hooks (data-only; behavior added later)

**Where ammo applies**
- Spawn: speed + pierce bonus (ProjectilePoolSpawnSystem)
- Flight: lifetime/turn rate/seek radius (ProjectileFlightSystem)
- Hit: damage type/flags + OnHitAdd (ProjectileEffectExecutionSystem)

## Weapon Spawner (Data-only)
`WeaponSpawner` lets entities fire without a `WeaponMount`:
- Aim modes: target entity, target position, fixed direction.
- Uses ammo from `WeaponSpawner.AmmoId` or `WeaponMagazine` + `AmmoStockpile`.
- Emits `InterruptType.OutOfAmmo` when magazines/stockpiles are empty.
- Writes `ProjectileSpawnRequest` like mounts; same downstream pipeline.

## Ammo Metrics (Headless)
- `ammo.shots_total`, `ammo.shots.<ammoId>`
- `ammo.used_total`, `ammo.used.<ammoId>`
- `ammo.out_of_ammo_total`, `ammo.out_of_ammo.<ammoId>`
- `ammo.stockpile.count`, `ammo.stockpile.current_total`, `ammo.stockpile.capacity_total`
- `ammo.magazine.count`, `ammo.magazine.current_total`, `ammo.magazine.capacity_total`

## Projectile Tracking (Audit)
Tracking is optional and data-only. When enabled, events are aggregated into counters.

Metrics (from tracking hub counters):
- `projectile.tracking.spawned_total`
- `projectile.tracking.hits_total`
- `projectile.tracking.deflect_total`
- `projectile.tracking.redirect_total`
- `projectile.tracking.control_total`
- `projectile.tracking.retire_total`
- `projectile.tracking.expire_total`
- `projectile.tracking.recycle_total`
- `projectile.tracking.events_count` (buffer length, if retained)

Per-ammo counters (audit detail):
- `projectile.tracking.spawned.<ammoId>`
- `projectile.tracking.hits.<ammoId>`
- `projectile.tracking.deflect.<ammoId>`
- `projectile.tracking.redirect.<ammoId>`
- `projectile.tracking.control.<ammoId>`
- `projectile.tracking.retire.<ammoId>`
- `projectile.tracking.expire.<ammoId>`
- `projectile.tracking.recycle.<ammoId>`

## Notes
- Presentation systems are optional; this pipeline runs headless.
- Physics colliders are optional; damage system can run with target-only data.
- The catalog design is intentionally minimal; games expand as needed.

## Next Extensions
- Ammo depletion behaviors (batch reloads, mixed magazines, quality variance).
- Deflection resolvers (shield hits, limb deflect, drone screens).
- Projectile control systems (ECM or mana sway, countermeasure suites).
- Projectile tracking/audit trail (events + metrics).
- Weapon profile biases (lawful/chaotic/materialist) for generator pipelines.
