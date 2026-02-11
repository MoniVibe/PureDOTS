# Goal Card: Projectile Tracking Audit
ID: projectile_tracking_audit_v0
Date: 2026-02-09
Owner: shonh
Status: draft

## Goal
Validate that projectile tracking emits auditable events and aggregates per-ammo counters in headless runs.

## Hypotheses
- Tracking hub records spawn + hit events for a basic spawner scenario.
- Per-ammo counters increment deterministically for the configured ammo id.

## Scenario Frame
Theme: range instrumentation
Why this scenario matters: tracking is the backbone for deflection audit, replay, and telemetry

## Setup
Map/Scene: headless
Actors: 1 spawner, 1 target hull
Equipment/Loadouts: launcher + ammo.arc
Rules/Constraints: generic defaults, no presentation
Duration: 120 seconds

## Roles and Experience
- Seats or roles: none (data-only)
- Experience tiers: none
- Skill effects per seat: none

## Behavior Profile
Cooperation: none
Target sharing: none
Discipline: fire continuously
Failure modes: tracking hub missing, events dropped

## Targeting and Fire Control
Detection: direct
Target selection: fixed target entity
Lock time: none
Track loss: none
Firing solution: homing

## Movement and Orientation
Formation: static
Rotation limits: none
Facing rules: face target
Speed profile: static

## Weapons and Arcs
Weapon types: homing projectile
Firing arcs: forward
Ammo and heat: ammo.arc, generic defaults

## Nuance Prompts (fill what applies)
Perception: none
Coordination: none
Reaction timing: fixed cadence
Skill/stat modifiers: none
Morale/discipline: ignore
Environment/interference: none
Failure cases: tracking buffer overflow, clear-each-frame loss
Determinism cues: seed + scenarioId

## Script
1. Spawn a target hull with Health + collider.
2. Spawn a data-only WeaponSpawner targeting the hull.
3. Run until tracking counters register spawn + hit.

## Metrics
- projectile.tracking.spawned_total: total tracked spawns
- projectile.tracking.hits_total: total tracked hits
- projectile.tracking.spawned.<ammoId>: per-ammo spawn count
- projectile.tracking.hits.<ammoId>: per-ammo hit count
- puredots.q.projectile_tracking.audit: headless audit pass flag

## Scoring
- audit_pass = (spawned_total >= 1 && hits_total >= 1)

## Acceptance
- audit_pass == true
- per-ammo counters increment for ammo.arc

## Regression Guardrails
- tracking counters must not stay zero
- per-ammo counters must not be missing

## Nightly Focus
Scenario ID: scenario.puredots.projectile_tracking.audit
Run budget: 2 minutes
Pass gates: audit_pass == 1
Do not regress: per-ammo counters
Priority work: add deflection tracking audit; add recycle/expire coverage
Telemetry IDs: puredots.q.projectile_tracking.audit

## Branch Plan
Branch name: scenarios/goal-cards/projectile-tracking-audit
Merge criteria: pass gates + review
Owner/Reviewer: shonh

## Variants
- ballistic instead of homing
- tracking hub ClearEachFrame = 0

## Telemetry/Outputs
- scenario run report

## Dependencies
- ProjectileTrackingBootstrapSystem
- ProjectileTrackingAggregationSystem
- WeaponSpawnerSystem + ProjectilePool systems

## Risks/Notes
- Tracking is data-only; no presentation required.
- Per-ammo counters are cumulative across ticks.

## Scenario JSON
Path: Assets/Scenarios/puredots_projectile_tracking_audit_micro.json
Version: v0
