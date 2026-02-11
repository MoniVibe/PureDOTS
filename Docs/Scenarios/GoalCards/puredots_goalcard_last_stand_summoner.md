# Goal Card: Last Stand Summoner Hunt
ID: last_stand_summoner_v0
Date: 2026-02-09
Owner: shonh
Status: draft

## Goal
Validate that a single elite melee combatant can intelligently cut through a sustained horde and eliminate a summoner target using focus‑driven survival skills.

## Hypotheses
- The warrior reaches and kills the summoner within the scenario duration while remaining alive.
- Focus usage (dodge/parry) prevents fatal damage even under sustained horde pressure.

## Scenario Frame
Theme: Last‑stand warrior vs. undead/demon horde with a summoner anchor.
Why this scenario matters: Stress‑tests melee survivability, focus budget, threat prioritization, and kill‑the‑source objectives in one loop.

## Setup
Map/Scene: Headless arena (no presentation).
Actors: 2 master warriors, 1 summoner, horde of undead + demons (target 1200 minions).
Equipment/Loadouts: Warrior w/ warhammer + light leather armor pieces.
Rules/Constraints: Warrior knows summoner location; summoner is priority target; summoner sustains horde; no external allies.
Duration: 180 seconds.

## Roles and Experience
- Seats or roles: Warrior (solo), Summoner (controller), Minions (swarm).
- Experience tiers: Warrior elite, Summoner elite, Minions veteran.
- Skill effects per seat: Warrior reaction time + parry efficiency; Summoner focus/mana sustain; Minion aggression.

## Behavior Profile
Cooperation: Horde acts as swarm; summoner keeps distance; warrior solo.
Target sharing: Horde converges on warrior; warrior locks summoner.
Discipline: Warrior stays on target; minions press; summoner repositions.
Failure modes: Warrior overcommits, focus starvation, summoner kites forever.

## Targeting and Fire Control
Detection: Warrior has summoner reveal/lock from start; minions aggro on warrior.
Target selection: Warrior prioritizes summoner; minions prioritize warrior.
Lock time: Immediate for warrior; normal for minions.
Track loss: Summoner can break line‑of‑sight; warrior attempts reacquire.
Firing solution: Melee arcs for warrior, ranged/minion attacks for horde.

## Movement and Orientation
Formation: Horde surrounds; summoner backs away from warrior.
Rotation limits: Warrior fast turn/strafe; summoner moderate.
Facing rules: Warrior faces summoner; minions face warrior.
Speed profile: Warrior fast, summoner moderate, minions mixed.

## Weapons and Arcs
Weapon types: Warrior melee (warhammer cleave); Summoner spells; Minion melee/ranged.
Firing arcs: Warrior frontal cleave; summoner line‑of‑sight.
Ammo and heat: Focus budget for dodge/parry; summoner mana sustain.

## Nuance Prompts (fill what applies)
Perception: Summoner location known to warrior; minions use proximity aggro.
Coordination: Horde pressure forces frequent survival actions.
Reaction timing: Warrior parry/dodge cadence uses focus intelligently.
Skill/stat modifiers: Light armor increases speed, reduces soak.
Failure cases: Warrior gets focus‑locked; summoner stalemates kiting.
Determinism cues: Fixed seed; reproducible spawn layout.

## Script
1. Spawn warrior, summoner, and horde; apply loadouts and stats.
2. Warrior beelines summoner while horde converges.
3. Victory when summoner dies with warrior alive.

## Metrics
- last_stand.summoner_killed: 1 if summoner dead.
- last_stand.hero_alive: 1 if warrior alive at exit.
- last_stand.time_to_kill_s: Time to summoner death.
- last_stand.focus_spent: Focus spent on survival actions.
- last_stand.minions_cleaved: Minions killed by cleave.

## Scoring
- Pass if summoner_killed == 1 AND hero_alive == 1.

## Acceptance
- summoner_killed == 1
- hero_alive == 1
- time_to_kill_s <= 180

## Regression Guardrails
- No determinism regressions on seed replay.
- No silent failure to spawn summoner or horde.

## Nightly Focus
Scenario ID: scenario.puredots.last_stand.summoner (micro: puredots_last_stand_summoner_micro)
Run budget: 3 mins
Pass gates: summoner_killed, hero_alive
Do not regress: focus usage, minion spawn counts
Priority work: seed branch, focus mechanics, summoner sustain
Telemetry IDs: puredots.q.last_stand.summoner_killed, puredots.q.last_stand.hero_survives

## Branch Plan
Branch name: scenarios/goal-cards/last-stand-summoner
Merge criteria: pass gates + review
Owner/Reviewer: shonh / tbd

## Variants
- heavier armor slower warrior
- summoner with burst teleport

## Telemetry/Outputs
- headless metrics bundle
- kill timeline logs

## Dependencies
- Melee combat + cleave damage
- Focus budget + dodge/parry actions
- Summoner sustain logic for horde
- Horde spawn/behavior

## Risks/Notes
- Many combat behaviors may be stubbed; seed branch should be minimal and deterministic.
- Current seed uses placeholder railgun/launcher weapon IDs until melee warhammer + summoner spell kits exist.

## Scenario JSON
Path: Assets/Scenarios/puredots_last_stand_summoner_micro.json
Version: v0
