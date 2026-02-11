# Goal Card: Shipyard Equip Weapons
ID: shipyard_equip_weapons_v0
Date: 2026-02-09
Owner: shonh
Status: draft

## Goal
Validate shipyard-driven weapon installs using weapon pools so entities can be equipped deterministically before combat.
Confirm pool role biasing influences which weapon gets installed.

## Hypotheses
- Shipyard requests equip a target with a valid WeaponMount or WeaponSpawner.
- Pool selection respects role biasing and remains deterministic per seed.

## Scenario Frame
Theme: shipyard outfitting trial hulls
Why this scenario matters: proves build/equip loop without presentation dependencies

## Setup
Map/Scene: headless
Actors: 1 shipyard, 1 test hull
Equipment/Loadouts: weapon pool with railgun + launcher
Rules/Constraints: generic budgets, no presentation
Duration: 120 seconds

## Roles and Experience
- Seats or roles: outfitting crew, gunnery
- Experience tiers: basic
- Skill effects per seat: none (data-only)

## Behavior Profile
Cooperation: none
Target sharing: none
Discipline: equip once
Failure modes: out of range, invalid pool entries

## Targeting and Fire Control
Detection: none (equip only)
Target selection: pool selection
Lock time: none
Track loss: none
Firing solution: not required

## Movement and Orientation
Formation: static
Rotation limits: none
Facing rules: none
Speed profile: static

## Weapons and Arcs
Weapon types: ballistic, homing
Firing arcs: forward
Ammo and heat: generic defaults

## Nuance Prompts (fill what applies)
Perception: none
Coordination: none
Reaction timing: equip cooldown
Skill/stat modifiers: pool role biasing
Morale/discipline: none
Environment/interference: none
Failure cases: pool empty, invalid weaponId
Determinism cues: seed and scenarioId

## Script
1. Spawn shipyard and test hull within equip range.
2. Attach weapon pool to hull (auto-install disabled).
3. Shipyard queues equip using target pool.
4. Verify weapon mount installed on hull.

## Metrics
- shipyard.count: active shipyards
- shipyard.requests.pending: queued equip requests
- shipyard.install.queued_total: shipyard requests that queued an install
- shipyard.request.invalid_total: invalid install requests dropped
- weapon.install.completed_total: installs applied
- weapon.install.mount_total: mount installs applied
- weapon.install.spawner_total: spawner installs applied
- weapon.mount.count: entities with WeaponMount
- weapon.spawner.count: entities with WeaponSpawner
- puredots.q.shipyard.equip: headless question (1 = equip succeeded)

## Assertion Set (v0)
- shipyard.install.queued_total >= 1
- weapon.install.completed_total >= 1
- shipyard.equip.success >= 1
- puredots.q.shipyard.equip >= 1

## Scoring
- install_success = weapon.install.completed_total >= 1

## Acceptance
- install_success == true
- selection remains deterministic for fixed seed

## Regression Guardrails
- no missing WeaponMount install
- no invalid weaponId rejects

## Nightly Focus
Scenario ID: scenario.puredots.shipyard.equip
Run budget: 2 minutes
Pass gates: install_success == 1
Do not regress: pool selection determinism
Priority work: add module equip, add build budget spending, add multi-weapon mounts
Telemetry IDs: puredots.q.shipyard.equip

## Branch Plan
Branch name: scenarios/goal-cards/shipyard-equip
Merge criteria: pass gates + review
Owner/Reviewer: shonh

## Variants
- use shipyard pool instead of target pool
- add range/cooldown constraints

## Telemetry/Outputs
- scenario run report

## Dependencies
- WeaponInstallSystem
- WeaponPoolSelectionHelpers

## Risks/Notes
- Metrics wired in ScenarioMetricsCollectorSystem + install systems.
- Pool selection uses defaults unless biases are explicitly set.

## Scenario JSON
Path: Assets/Scenarios/puredots_shipyard_equip_micro.json
Version: v0
