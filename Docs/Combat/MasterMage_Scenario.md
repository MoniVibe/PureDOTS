# Master Mage Scenario (PureDOTS v0)
Date: 2026-02-09
Owner: shonh
Status: draft

## Intent
A high-capability entity (master mage / apex operator) stands against apprentices or peers.
Validates deflection, spell arsenal, mana/focus budgets, and ally-protection behaviors.

## Core Loop
- Master mage alternates between offense, defense, and protection.
- Apprentices pressure with varied spell types and timing.
- The environment enforces resource constraints (mana/focus, stamina).

## Entities
- **Master Mage**: apex caster, barrier projection, projectile control.
- **Apprentices**: mixed precision/volume spells.
- **Allies**: protected by master barriers or drone shields.
- **Spectators/Injured** (optional): morale and ethics hooks.

## Data Primitives (Draft)
### Spell Arsenal
- `SpellCatalog` + `SpellSignatureCatalog` (spell ID, archetype, signatures)
- `SpellCaster`, `SpellCastState`, `SpellCastRequest`, `SpellCooldown`
- `SpellLoadout`, `SpellSlot` (prepared spells)
- `ProjectileCatalog` + `ProjectileSpec` (mana/psi projectiles)

### Resources
- `SpellMana` (regen + burst)
- `FocusState` (precision + timing window)
- `ResourcePools` (stamina + physical constraints)

### Defense + Deflection
- `DeflectionProfile`, `DeflectionBudget`, `DeflectionRequest`
- `ProjectileControlRequest` (control/sway/hijack)
- `ProjectileSignature` (ECM/mana targeting)

### Relations + Learning
- `EntityRelation` + `RelationEvent` (mentor/apprentice)
- `ExtendedSpellMastery` + lesson mastery (learning rate, skill growth)

### Scheduling
- `EntityRoutine` + `RoutineSchedule` (lesson cadence)
- `ScheduleAdherence` (discipline vs improvisation)

## Behavior Notes
- Master prefers conserving resources early, heavy deflect in late windows.
- Apprentices diversify spell archetypes (pressure + unpredictability).
- Alliances shift based on morale and perceived favoritism.

## Scenario Variants
1) **Duel**: master vs single elite apprentice.
2) **Swarm**: master vs many low-tier apprentices.
3) **Defense**: master protects allied students from external threats.
4) **Attrition**: extended battle to test mana/focus regen.

## PureDOTS Mapping
- Use Weapon/Projectile primitives for spell delivery.
- Deflection model shared with firing range.
- Presentation deferred; data-only execution.

## Notes
- This is a pillar scenario to validate deflection + resource budgets.
- Keeps the "master mage" concept generic for cross-game reuse.
