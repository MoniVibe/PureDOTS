# Canonicity Pack

This folder defines how TRI treats "canonical" artifacts across `puredots`, `space4x`, and `godgame`.

Use this pack when:
- deciding where a definition should live,
- deciding which scenario/data file is authoritative,
- preventing duplicate semantics under different names.

## Canonical Files

- `Docs/Canonicity/Canonicity_Contract_v0.md`
- `Docs/Canonicity/Data_Contract_Canon_Sprint_v0.md`
- `Docs/Canonicity/Canonical_Scenario_Set_v0.md`
- `Docs/Canonicity/canonical_scenarios.v0.json`
- `Docs/Canonicity/canonical_contracts.v0.json`
- `Docs/Canonicity/canonical_contract_payloads.v0.json`
- `Docs/Canonicity/Combat_Mining_DataContracts_v0.md`
- `Docs/Canonicity/Schemas/contract.mining.v0.schema.json`
- `Docs/Canonicity/Schemas/contract.combat.v0.schema.json`
- `Docs/Canonicity/Schemas/contract.room_profile.v0.schema.json`
- `Docs/Canonicity/Schemas/contract.scenario_envelope.v0.schema.json`
- `Docs/Canonicity/Schemas/contract.mission_objective.v0.schema.json`
- `Docs/Canonicity/Schemas/contract.loot_cache.v0.schema.json`
- `Docs/Canonicity/Schemas/contract.encounter_profile.v0.schema.json`
- `Docs/Canonicity/Payloads/*`

## Operating Rule

1. Define or update meaning/ownership first (contract docs).
2. Update canonical contract/payload/scenario registries (`canonical_contracts.v0.json`, `canonical_contract_payloads.v0.json`, `canonical_scenarios.v0.json`).
3. Update project assets/adapters.
4. Keep headless gates mapped to canonical scenario IDs.

## Validation Gate

- CI/local validator script: `puredots/CI/validate_canonicity_contracts.ps1`
- Scope:
  - canonical contract registry integrity checks,
  - canonical payload registry integrity checks,
  - schema JSON parse checks,
  - canonical registry integrity checks,
  - canonical path + `scenarioId` consistency checks,
  - missing/duplicate `scenarioId` checks in active scenario folders.
