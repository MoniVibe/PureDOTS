# Canonical Scenario Set v0

Status: draft-active  
Date: 2026-02-19

This file declares the minimum canonical scenario set used for smoke, slice intent, and scale gates.

Machine-readable source: `Docs/Canonicity/canonical_scenarios.v0.json`

## Canonical Set

| Canon key | Project | Scenario ID | Path | Tier | Purpose |
|---|---|---|---|---|---|
| `canon.space4x.smoke` | space4x | `space4x_smoke` | `space4x/Assets/Scenarios/space4x_smoke.json` | smoke | Primary Space4X smoke scenario. |
| `canon.space4x.supergreen` | space4x | `space4x_collision_micro` | `space4x/Assets/Scenarios/space4x_collision_micro.json` | smoke | Fast headless super-green gate. |
| `canon.space4x.mining_micro` | space4x | `space4x_mining_micro` | `space4x/Assets/Scenarios/space4x_mining_micro.json` | micro | Mining behavior slice. |
| `canon.space4x.combat_micro` | space4x | `space4x_combat_micro` | `space4x/Assets/Scenarios/space4x_combat_micro.json` | micro | Combat behavior slice. |
| `canon.space4x.economy_micro` | space4x | `scenario.space4x.economy_loop_micro` | `space4x/Assets/Scenarios/space4x_economy_loop_micro.json` | micro | Economy throughput slice. |
| `canon.space4x.mining_combat` | space4x | `space4x_mining_combat` | `space4x/Assets/Scenarios/space4x_mining_combat.json` | slice | Mixed mining/combat slice. |
| `canon.space4x.fleetcrawl_survivors_v1` | space4x | `space4x_fleetcrawl_survivors_v1` | `space4x/Assets/Scenarios/space4x_fleetcrawl_survivors_v1.json` | slice | FleetCrawl Survivors intent slice. |
| `canon.space4x.scale_100k` | space4x | `scenario_space_01_scale_100k` | `space4x/Assets/Scenarios/scenario_space_01_scale_100k.json` | scale | Space4X scale baseline. |
| `canon.godgame.smoke` | godgame | `godgame_smoke` | `godgame/Assets/Scenarios/Godgame/godgame_smoke.json` | smoke | Primary Godgame smoke scenario. |
| `canon.godgame.smoke_determinism` | godgame | `scenario.godgame.smoke_determinism` | `godgame/Assets/Scenarios/Godgame/godgame_smoke_determinism.json` | smoke | Determinism smoke gate. |
| `canon.godgame.storehouse_throughput` | godgame | `godgame_storehouse_throughput_micro` | `godgame/Assets/Scenarios/Godgame/godgame_storehouse_throughput_micro.json` | micro | Gather/deliver throughput slice. |
| `canon.godgame.villager_crowd_flow` | godgame | `godgame_villager_crowd_flow_micro` | `godgame/Assets/Scenarios/Godgame/godgame_villager_crowd_flow_micro.json` | micro | Movement/collision crowd slice. |
| `canon.godgame.combat_micro` | godgame | `godgame_combat_micro` | `godgame/Assets/Scenarios/Godgame/godgame_combat_micro.json` | micro | Combat slice. |
| `canon.puredots.firing_range_micro` | puredots | `puredots_firing_range_micro` | `puredots/Assets/Scenarios/puredots_firing_range_micro.json` | micro | Core projectile/combat harness. |
| `canon.puredots.projectile_lifecycle_micro` | puredots | `puredots_projectile_lifecycle_micro` | `puredots/Assets/Scenarios/puredots_projectile_lifecycle_micro.json` | micro | Projectile lifecycle verification. |
| `canon.puredots.shipyard_equip_micro` | puredots | `puredots_shipyard_equip_micro` | `puredots/Assets/Scenarios/puredots_shipyard_equip_micro.json` | micro | Module equip pipeline slice. |

## Legacy and Optional Scenarios

Scenarios not listed here are not deleted; they are optional, experimental, or legacy until promoted.

Promotion requires:
1. Clear purpose.
2. Stable `scenarioId`.
3. Linked gate/goalcard/use-case.
4. Registry update in `canonical_scenarios.v0.json`.

## Normalization Applied in This Pass

- Added missing `scenarioId` fields to active scenario files in:
  - `space4x/Assets/Scenarios` (scale variants + FTL micro)
  - `godgame/Assets/Scenarios/Godgame` (multiple smoke/micro/loop scenarios)
