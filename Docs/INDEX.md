# PureDOTS Docs Index

This index is the canonical entrypoint for direction and constraints.

## Start Here (Canonical)
- `Docs/NORTH_STAR.md` - project direction and non-negotiables.
- `Docs/DEMO_SLICE.md` - six-point "full demo vibe" acceptance.
- `Docs/ARCHITECTURE.md` - determinism/time/rewind/headless contracts.
- `Docs/PERF_GATES.md` - scale harness thresholds and pass/fail policy.
- `Docs/NETPLAY_NOT_NOW.md` - deferred netplay constraints.
- `Docs/PROGRESS_HUB.md` - active progress tracking rule.
- `Docs/ContentIntent/README.md` - shared content intent scope across games.
- `Docs/ContentIntent/PureDOTS_vs_GameSide_Ruleset.md` - boundary rules for simulation core vs project flavor.
- `Docs/ContentIntent/MVP_Content_Taxonomy_v0.md` - reusable MVP content taxonomy.
- `Docs/Canonicity/README.md` - canonicity operating pack.
- `Docs/Canonicity/Canonicity_Contract_v0.md` - canonical ownership contract.
- `Docs/Canonicity/Data_Contract_Canon_Sprint_v0.md` - sprint plan for contract canon unification.
- `Docs/Canonicity/Canonical_Scenario_Set_v0.md` - canonical scenario gates/slices.
- `Docs/Canonicity/canonical_scenarios.v0.json` - machine-readable canonical scenario registry.
- `Docs/Canonicity/canonical_contracts.v0.json` - machine-readable canonical contract registry.
- `Docs/Canonicity/canonical_contract_payloads.v0.json` - machine-readable canonical contract payload registry.
- `Docs/Canonicity/Combat_Mining_DataContracts_v0.md` - shared combat/mining/room data contract model.
- `Docs/Canonicity/Schemas/contract.mining.v0.schema.json` - mining contract schema.
- `Docs/Canonicity/Schemas/contract.combat.v0.schema.json` - combat contract schema.
- `Docs/Canonicity/Schemas/contract.room_profile.v0.schema.json` - room profile contract schema.
- `Docs/Canonicity/Schemas/contract.scenario_envelope.v0.schema.json` - scenario envelope schema.
- `Docs/Canonicity/Schemas/contract.mission_objective.v0.schema.json` - mission objective contract schema.
- `Docs/Canonicity/Schemas/contract.loot_cache.v0.schema.json` - loot cache contract schema.
- `Docs/Canonicity/Schemas/contract.encounter_profile.v0.schema.json` - encounter/boss profile contract schema.
- `Docs/Canonicity/Payloads/*` - canonical reusable payload definitions.
- `../CI/validate_canonicity_contracts.ps1` - canonicity/schema/scenario ID validation gate.

## Project Context
- Space4X orientation: `../../space4x/Docs/ORIENTATION.md`
- Godgame orientation/index: `../../godgame/Docs/INDEX.md`
- Recurring operational pitfalls: `Docs/Headless/recurring.md`
- Recurring error ledger: `Docs/Headless/recurringerrors.md`

## Archive Policy
- Stale progress/vision/directive docs should be tombstoned and point here.
- Historical material belongs under `Docs/Archive/`.

## Notes
- Headless evidence is authoritative.
- ECS tooling for human debugging (Archetypes, Systems, Journaling) is part of standard workflow.
