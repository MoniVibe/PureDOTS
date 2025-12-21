# AI Telemetry Schema (Headless v0)

Purpose: shared, compact telemetry keys/events for headless AI validation across Space4X + Godgame.

## Naming Conventions
- Prefix with game or loop domain: `space4x.*`, `godgame.*`, `loop.*`.
- Metrics: lower_snake_case segments, keep <= 64 chars.
- Events: `TelemetryEvent.EventType` uses stable PascalCase tokens.
- Loop proofs: use `TelemetryLoopProofUtility` (`eventType=loop_proof`).

## Loop Proof Events
Event type: `loop_proof`
Payload (compact JSON):
- `l` = loop id (e.g., `combat`, `logistics`, `exploration`, `rewind`, `time`)
- `k` = step id (optional, e.g., `attack_run`, `cap_to_attack`)
- `s` = success (1/0)
- `o` = observed (float)
- `e` = expected (string)
- `w` = timeout ticks (uint)

## Space4X Strike Craft
Events (`eventType`):
- `BehaviorProfileAssigned`
- `RoleStateChanged`
- `AttackRunStart`
- `AttackRunEnd`
- `CombatAirPatrolStart`
- `CombatAirPatrolEnd`

Metrics:
- `space4x.strikecraft.total` (count)
- `space4x.strikecraft.cap.active` (count)
- `space4x.strikecraft.cap.noncombat` (count)
- `space4x.strikecraft.cap.ratio` (ratio)
- `space4x.strikecraft.attack.active` (count)
- `space4x.strikecraft.attack.ratio` (ratio)
- `space4x.strikecraft.wing.members` (count)
- `space4x.strikecraft.wing.avgDistance` (custom)
- `space4x.strikecraft.wing.cohesion` (ratio)

Loop proof steps:
- `attack_run`
- `cap_to_attack`

## Space4X Mining/Logistics (existing keys)
- `space4x.mining.oreInHold` (custom)
- `space4x.mining.oreInHold.lastTick` (custom)
- `loop.extract.buffer` (custom)
- `loop.extract.outputPerTick` (custom)
- `loop.extract.activeWorkers` (count)
- `loop.extract.nodes.active` (count)
- `space4x.docking.*` (counts + ratio)

## Godgame Villager/Needs (placeholders for parity)
Events:
- `VillagerNeedStateChanged` (targeted need + satisfaction)
- `VillagerJobAssigned` (job + target)
- `VillagerLoopProof` (when not using `loop_proof`)

Metrics:
- `godgame.villagers.total`
- `godgame.needs.hunger.avg`
- `godgame.needs.energy.avg`
- `godgame.needs.morale.avg`

## Guidance
- Prefer metrics for continuous values (ratios, counts).
- Prefer events for state transitions and proof signals.
- Keep payloads <= `FixedString128Bytes` and avoid large JSON blobs.
