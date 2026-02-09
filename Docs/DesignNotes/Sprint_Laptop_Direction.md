# Sprint Laptop Direction (PureDOTS Combat Primitives v0)
Date: 2026-02-09
Owner: shonh
Status: draft

## Target Direction
Establish PureDOTS-only combat primitives (weapon -> projectile -> damage) and a firing range scenario that exercises them without presentation dependencies.

## Scope (Generic Defaults)
- Weapon/Projectile data pipeline (catalogs + spawn + pool + flight).
- Simple ballistic + homing projectiles (data-only).
- Firing range scenario bootstrap for headless runs.
- Design grammar + scenario specs (firing range, master mage).
- Minimal metrics hooks to expand later (accuracy, cost, friendly fire, cohesion).

## Out of Scope (for this sprint)
- Visual presentation, VFX, or UI.
- Full AI targeting logic or combat behaviors.
- Detailed balance tuning or final numeric tuning.
- Full physics spectacle (we keep it data-first).

## Deliverables
- Default weapon + projectile catalogs.
- Weapon projectile spawner + pooling bootstraps.
- Scenario: `scenario.puredots.firing_range.smoke`.
- Docs: design grammar, firing range, master mage, deflection.

## Success Criteria (Qualitative)
- Headless run spawns shooters + targets and fires projectiles.
- Homing projectiles converge on target without presentation layer.
- Docs describe extensibility and hooks for later tuning.

## Immediate Follow-ups
- Build a proper sprint planner skill and macro-level scenario roadmap.
- Add detailed metrics for accuracy, cost efficiency, and friendly fire penalties.
- Expand projectile catalog with additional archetypes (piercing, AoE, chain).
