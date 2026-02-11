# Goal Card: Firing Range Core Loop
ID: firing_range_core_v0
Date: 2026-02-09
Owner: shonh
Status: draft

## Goal
Validate the data-only weapon -> projectile -> damage pipeline in a controlled firing range.
Ensure ballistic and homing projectiles execute deterministically under generic defaults.

## Hypotheses
- Basic railgun rounds hit static targets within expected lifetime.
- Homing projectiles converge on target without presentation dependencies.

## Scenario Frame
Theme: training range for gunners/archers
Why this scenario matters: clean testbed for combat primitives before AI behaviors

## Setup
Map/Scene: headless
Actors: 2 shooters, 2 target drones
Equipment/Loadouts: railgun + launcher, ballistic + homing
Rules/Constraints: generic ammo/energy defaults
Duration: 120 seconds

## Roles and Experience
- Seats or roles: shooter
- Experience tiers: rookie and veteran
- Skill effects per seat: higher cadence and lower spread for veteran

## Behavior Profile
Cooperation: none
Target sharing: none
Discipline: hold fire outside range
Failure modes: friendly fire if lanes overlap

## Targeting and Fire Control
Detection: direct line of sight
Target selection: fixed targets
Lock time: none
Track loss: none
Firing solution: direct lead for ballistic, homing for launcher

## Movement and Orientation
Formation: fixed lanes
Rotation limits: none
Facing rules: face target
Speed profile: static

## Weapons and Arcs
Weapon types: ballistic, homing
Firing arcs: forward
Ammo and heat: generic defaults

## Nuance Prompts (fill what applies)
Perception: none
Coordination: none
Reaction timing: fixed cadence
Skill/stat modifiers: minimal
Morale/discipline: ignore
Environment/interference: none
Failure cases: pool exhaustion, dropped spawns
Determinism cues: seed and scenarioId

## Script
1. Spawn two shooters with weapon mounts.
2. Spawn two targets with health.
3. Let fire control tick for duration.

## Metrics
- firing_range.shots_fired: total shots
- firing_range.hits: total hits
- firing_range.projectiles_dropped: pool drop count

## Scoring
- accuracy = hits / shots_fired

## Acceptance
- accuracy > 0.2 for ballistic (generic defaults)
- homing projectiles hit at least once

## Regression Guardrails
- no determinism regressions
- no projectile pool underflow

## Nightly Focus
Scenario ID: scenario.puredots.firing_range.smoke
Run budget: 2 minutes
Pass gates: accuracy > 0.2
Do not regress: pool drop count
Priority work: improve spawn cadence, add target movement, add friendly fire
Telemetry IDs: puredots.q.firing_range.accuracy

## Branch Plan
Branch name: scenarios/goal-cards/firing-range
Merge criteria: pass gates + review
Owner/Reviewer: shonh

## Variants
- moving targets
- friendly fire lanes

## Telemetry/Outputs
- scenario run report

## Dependencies
- WeaponMount + Projectile catalogs
- Projectile pool

## Risks/Notes
- Damage system currently simplistic; refine later.

## Scenario JSON
Path: Assets/Scenarios/puredots_firing_range_micro.json
Version: v0
