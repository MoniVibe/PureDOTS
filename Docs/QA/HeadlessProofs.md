# Headless Proofs (Canonical Loops + Expectations)

**Scope**: Shared headless validation for PureDOTS, Godgame, and Space4X.
**Goal**: Define the canonical loop proofs, required subjects, and expected telemetry/log outputs.

## Telemetry defaults (headless)
- Use thin telemetry by default to avoid massive NDJSON outputs:
  - `PUREDOTS_TELEMETRY_LEVEL=summary`
  - `PUREDOTS_TELEMETRY_MAX_BYTES=524288000` (500 MB cap)
- For deep dives, set `PUREDOTS_TELEMETRY_LEVEL=full` or raise the byte cap.

## Shared proofs (PureDOTS)

### Time control proof (global + local)
- System: `puredots/Packages/com.moni.puredots/Runtime/Systems/HeadlessTimeControlProofSystem.cs`
- Enable: `PUREDOTS_HEADLESS_TIME_PROOF=1` (defaults on in headless)
- Expected log: `[HeadlessTimeControlProof] PASS ...`
- Telemetry: `loop=time` with step sequence:
  - `global.pause` expected `paused`
  - `global.step` expected `+2`
  - `global.resume` expected `play`
  - `global.speed_up` expected `2`
  - `global.speed_reset` expected `1`
  - `local.pause` expected `0`
  - `local.scale` expected `0.5`
  - `local.rewind` expected `<0`
- Rewind subject: `time.control`

### Rewind core proof
- System: `puredots/Packages/com.moni.puredots/Runtime/Systems/HeadlessRewindProofSystem.cs`
- Auto-enabled in headless (unless `PUREDOTS_HEADLESS_REWIND_PROOF=0`)
- Default trigger: tick 120, rewind back 60 ticks
- Expected log: `[HeadlessRewindProof] PASS ...`
- Telemetry: `loop=rewind` step `core`, expected `>0`
- Requires at least one **subject** (registered by game loop proofs below). No subjects => proof cannot pass.

## Godgame loop proofs

### Logistics: gather -> deliver -> store
- System: `godgame/Assets/Scripts/Godgame/Headless/GodgameHeadlessVillagerProofSystem.cs`
- Enable: auto-enabled in headless (set `GODGAME_HEADLESS_VILLAGER_PROOF=0` to disable; `=1` forces in non-headless)
- Expected log: `[GodgameHeadlessVillagerProof] PASS ...`
- Telemetry: `loop=logistics`, step `gather_deliver`, expected `>0` (storehouse delta)
- Rewind subject: `godgame.villager` (required mask: RecordReturn)
- Scenario: `godgame/Assets/Scenarios/Godgame/villager_loop_small.json`

### Needs satisfaction
- System: `godgame/Assets/Scripts/Godgame/Headless/GodgameHeadlessNeedsSystems.cs`
- Enable: auto-enabled in headless (set `GODGAME_HEADLESS_NEEDS_PROOF=0` to disable; `=1` forces in non-headless)
- Expected log: `[GodgameHeadlessNeedsProof] PASS ...`
- Telemetry: `loop=needs`, step `hunger/rest/faith/safety/social`, expected `satisfied`
- Rewind subject: `godgame.needs`

### Combat engagement + resolution
- System: `godgame/Assets/Scripts/Godgame/Headless/GodgameHeadlessCombatSystems.cs`
- Enable: auto-enabled in headless (set `GODGAME_HEADLESS_COMBAT_PROOF=0` to disable; `=1` forces in non-headless)
- Expected log: `[GodgameHeadlessCombatProof] PASS ...`
- Telemetry: `loop=combat`, step `engagement`, expected `engaged+resolved`
- Rewind subject: `godgame.combat`

### Village build loop
- System: `godgame/Assets/Scripts/Godgame/Headless/GodgameHeadlessVillageBuildProofSystem.cs`
- Enable: auto-enabled in headless (set `GODGAME_HEADLESS_VILLAGE_BUILD_PROOF=0` to disable; `=1` forces in non-headless)
- Expected log: `[GodgameHeadlessVillageBuildProof] PASS ...`
- Telemetry: `loop=construction`, step `village_build`, expected `>=1` (completed sites)

## Space4X loop proofs

### Mining: extract -> pickup -> dropoff
- System: `space4x/Assets/Scripts/Space4x/Headless/Space4XHeadlessMiningProofSystem.cs`
- Enable: `SPACE4X_HEADLESS_MINING_PROOF=1`
- Expected log: `[Space4XHeadlessMiningProof] PASS ...`
- Telemetry: `loop=extract`, step `gather_dropoff`, expected `>0` (ore delta)
- Rewind subject: `space4x.mining`
- Scenario: `space4x/Assets/Scenarios/space4x_demo_mining.json`

### Patrol loop
- System: `space4x/Assets/Scripts/Space4x/Headless/Space4XHeadlessBehaviorProofSystem.cs`
- Expected log: `[Space4XHeadlessLoopProof] PASS Patrol ...`
- Telemetry: `loop=exploration`, step `patrol`, expected `complete`
- Rewind subject: `space4x.patrol` (requires PatrolBehavior to exist)

### Escort loop
- System: `space4x/Assets/Scripts/Space4x/Headless/Space4XHeadlessBehaviorProofSystem.cs`
- Expected log: `[Space4XHeadlessLoopProof] PASS Escort ...`
- Telemetry: `loop=combat`, step `escort`, expected `complete`
- Rewind subject: `space4x.escort` (requires EscortAssignment to exist)

### Attack run loop
- System: `space4x/Assets/Scripts/Space4x/Headless/Space4XHeadlessBehaviorProofSystem.cs`
- Expected log: `[Space4XHeadlessLoopProof] PASS AttackRun ...`
- Telemetry: `loop=combat`, step `attack_run`, expected `complete`
- Rewind subject: `space4x.attack` (requires StrikeCraftProfile to exist)

### Docking loop
- System: `space4x/Assets/Scripts/Space4x/Headless/Space4XHeadlessBehaviorProofSystem.cs`
- Expected log: `[Space4XHeadlessLoopProof] PASS Docking ...`
- Telemetry: `loop=logistics`, step `docking`, expected `complete`
- Rewind subject: `space4x.docking` (requires MiningVessel to exist)

## Scenario-driven time/rewind commands

Scenario input commands are only used when running via the ScenarioRunner.
- Example: `puredots/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/headless_time_rewind_short.json`.
- Commands: `time.pause`, `time.play`, `time.setspeed`, `time.rewind`, `time.stoprewind`, `time.step`.

## Failure triage checklist (headless)

1) Proof enabled? (env flags)
2) Scenario spawns required subjects? (mining vessels, strike craft, storehouse, villagers)
3) Rewind enters playback + returns to record? (HeadlessRewindProof logs)
4) Telemetry NDJSON is fresh (truncate between runs)
5) Headless build is rebuilt after authoring/baker changes
