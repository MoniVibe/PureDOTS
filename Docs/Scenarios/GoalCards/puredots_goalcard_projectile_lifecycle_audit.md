# Goal Card: Projectile Lifecycle Audit
ID: projectile_lifecycle_audit_v0
Date: 2026-02-09
Owner: shonh
Status: draft

## Goal
Validate that projectile lifecycle events (retire/expire/recycle) are emitted and aggregated in headless runs.

## Hypotheses
- Tracking hub records retire/expire/recycle events for a short-lived projectile.
- Lifecycle counters are deterministic for a fixed seed.

## Scenario Frame
Theme: lifecycle instrumentation
Why this scenario matters: lifecycle audit is needed to reason about pool health, cleanup, and telemetry integrity

## Setup
Map/Scene: headless
Actors: 1 spawner, 1 target hull
Equipment/Loadouts: launcher + ammo.standard
Rules/Constraints: short lifetime, pool recycle enabled, no presentation
Duration: 120 seconds

## Roles and Experience
- Seats or roles: none (data-only)
- Experience tiers: none
- Skill effects per seat: none

## Behavior Profile
Cooperation: none
Target sharing: none
Discipline: fire continuously
Failure modes: expiry never fires, recycle never fires

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
Ammo and heat: ammo.standard, generic defaults

## Nuance Prompts (fill what applies)
Perception: none
Coordination: none
Reaction timing: fixed cadence
Skill/stat modifiers: none
Morale/discipline: ignore
Environment/interference: none
Failure cases: retire/expire events missing
Determinism cues: seed + scenarioId

## Script
1. Spawn a target hull with Health + collider.
2. Spawn a data-only WeaponSpawner targeting the hull.
3. Fire one spawner at a target (retire) and one into empty space (expire), both using pooled projectiles.

## Metrics
- projectile.tracking.retire_total: total retire events
- projectile.tracking.expire_total: total expire events
- projectile.tracking.recycle_total: total recycle events
- puredots.q.projectile_lifecycle.audit: headless audit pass flag

## Scoring
- audit_pass = (retire_total >= 1 && expire_total >= 1 && recycle_total >= 1)

## Acceptance
- audit_pass == true

## Regression Guardrails
- lifecycle counters must not stay zero
- recycle count must not drift when pool capacity is stable

## Nightly Focus
Scenario ID: scenario.puredots.projectile_lifecycle.audit
Run budget: 2 minutes
Pass gates: audit_pass == 1
Do not regress: lifecycle counters
Priority work: add lifetime overrides; add pool capacity stress variant
Telemetry IDs: puredots.q.projectile_lifecycle.audit

## Branch Plan
Branch name: scenarios/goal-cards/projectile-lifecycle-audit
Merge criteria: pass gates + review
Owner/Reviewer: shonh

## Variants
- ballistic instead of homing
- zero target (expire-only path)

## Telemetry/Outputs
- scenario run report

## Dependencies
- ProjectilePoolRecycleSystem
- ProjectileTrackingAggregationSystem

## Risks/Notes
- Requires a way to force short lifetimes or explicit retire events.
- May need pool config knobs to ensure recycle triggers.

## Scenario JSON
Path: Assets/Scenarios/puredots_projectile_lifecycle_micro.json
Version: v0
