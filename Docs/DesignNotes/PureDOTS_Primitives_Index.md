# PureDOTS Primitives Index (v0)
Date: 2026-02-09
Owner: shonh
Status: draft

## Purpose
Single map of data-only primitives used to express scenarios without presentation.
This is a reference for sprint planning and scenario design.

## Combat + Projectiles
- Weapon data: `WeaponMount`, `WeaponCatalog`, `WeaponSpec`, `WeaponComponent`
- Projectile data: `ProjectileCatalog`, `ProjectileSpec`, `ProjectileEntity`, `ProjectileSpawnRequest`
- Pooling: `ProjectilePoolConfig`, `ProjectilePoolState`, `ProjectilePoolEntry`
- Targeting: `TargetingComputer`, `TargetingSolution`, `ProjectileFlightSpec`
- Damage: `Health`, `Damageable`, `DamageEvent`, `DeathEvent`, `DamageProfile`

## Deflection + Interception
- Deflection: `DeflectionProfile`, `DeflectionBudget`, `DeflectionIntent`
- Deflection queue: `DeflectionRequest`, `DeflectionEvent`
- Control: `ProjectileControlRequest`, `ProjectileControlState`, `ProjectileSignature`
- Intercept stubs: `InterceptTicket`, `InterceptTarget`, `InterceptSolution`

## Ammo + Resource Use
- Ammo: `AmmoStockpile`, `WeaponMagazine`, `AmmoConsumptionRequest`
- Energy/heat: `WeaponMount.EnergyReserve`, `WeaponMount.HeatLevel`
- Mana/focus: `SpellMana`, `FocusState`
- Generic pools: `ResourcePools`

## Positioning + Tactics
- Engagement range: `EngagementEnvelope`
- Tactical intent: `PositioningIntent`
- Maneuvers: `StrafeProfile`, `OrbitProfile`
- Formations: `FormationAnchor`, `FormationCombatComponents`, `FormationTacticComponents`

## Relations + Morale
- Social relations: `EntityRelation`, `RelationEvent`, `RelationChangedEvent`
- Factions: `FactionRelationships`, `OrgRelation`
- Friendly fire: `FriendlyFireIncident`, `FriendlyFirePenalty`, `FriendlyFireTolerance`
- Morale: `MoraleState`, `SquadCohesion`, `CohesionCombatMultipliers`

## Scheduling + Adherence
- Routines: `EntityRoutine`, `RoutineSchedule`, `RoutineConfig`
- Adherence: `ScheduleAdherence`, `ScheduleDeviation`, `ScheduleDeviationEvent`

## Spells + Mastery
- Catalogs: `SpellCatalog`, `SpellSignatureCatalog`
- Casting: `SpellCaster`, `SpellCastState`, `SpellCastRequest`, `SpellCooldown`
- Arsenal: `SpellLoadout`, `SpellSlot`
- Mastery: `ExtendedSpellMastery`, `HybridSpell`

## Needs + Intent
- Needs: `NeedCategory`, `NeedSatisfaction`, `NeedRequestElement`
- Intent: `EntityIntentQueue`, `IntentState`, `CombatIntent`

## Notes
- All primitives are data-only and safe for headless runs.
- Game layers tune parameters and add presentation only.
- Scenario design should reuse these primitives rather than inventing new ones.
