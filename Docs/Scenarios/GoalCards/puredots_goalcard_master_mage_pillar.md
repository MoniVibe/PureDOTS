# Goal Card: Master Mage Pillar
ID: master_mage_pillar_v0
Date: 2026-02-09
Owner: shonh
Status: draft

## Goal
Validate deflection, mana/focus budgets, and ally-protection behaviors for an apex operator.
Ensure projectile control and barrier projection share the same PureDOTS primitives.

## Hypotheses
- Master mage survives sustained apprentice pressure with proper resource use.
- Deflection choices track threat severity and cost constraints.

## Scenario Frame
Theme: master mage training duel
Why this scenario matters: pillar test for deflection + resource modeling

## Setup
Map/Scene: headless
Actors: master mage, 3 apprentices
Equipment/Loadouts: mixed spell arsenals, barrier + projectile control
Rules/Constraints: generic defaults, no presentation
Duration: 180 seconds

## Roles and Experience
- Seats or roles: master, apprentice
- Experience tiers: master, novice
- Skill effects per seat: reduced reaction time for master

## Behavior Profile
Cooperation: apprentices coordinate lightly
Target sharing: local
Discipline: staggered pressure
Failure modes: resource starvation, missed timing windows

## Targeting and Fire Control
Detection: line of sight + basic sensor
Target selection: master prioritizes highest threat
Lock time: short
Track loss: when target breaks line
Firing solution: direct and controlled projectiles

## Movement and Orientation
Formation: loose ring around master
Rotation limits: standard
Facing rules: keep master in arc
Speed profile: moderate

## Weapons and Arcs
Weapon types: spell projectiles + barriers
Firing arcs: forward + radial barrier
Ammo and heat: mana and focus budgets

## Nuance Prompts (fill what applies)
Perception: minor occlusion
Coordination: basic callouts
Reaction timing: master bias for narrow windows
Skill/stat modifiers: focus improves timing
Morale/discipline: apprentices may panic when losing
Environment/interference: none
Failure cases: deflection overuse, barrier collapse
Determinism cues: seed and scenarioId

## Script
1. Spawn master with barrier + deflection capabilities.
2. Spawn apprentices with mixed spell projectiles.
3. Run sustained pressure cycle.

## Metrics
- master.mana_remaining
- master.deflect_success_rate
- apprentice.hit_rate

## Scoring
- master_survival = 1 if alive at end else 0

## Acceptance
- master_survival == 1 with generic defaults

## Regression Guardrails
- no determinism regressions

## Nightly Focus
Scenario ID: scenario.puredots.master_mage.smoke
Run budget: 3 minutes
Pass gates: master_survival == 1
Do not regress: deflect_success_rate
Priority work: add mana/focus costs, add control/sway
Telemetry IDs: puredots.q.master_mage.survival

## Branch Plan
Branch name: scenarios/goal-cards/master-mage
Merge criteria: pass gates + review
Owner/Reviewer: shonh

## Variants
- master protects allies
- attrition focus

## Telemetry/Outputs
- scenario run report

## Dependencies
- deflection model
- spell arsenal catalogs

## Risks/Notes
- Spell arsenal not yet implemented; stub until catalogs exist.

## Scenario JSON
Path: Assets/Scenarios/puredots_master_mage_micro.json
Version: v0
