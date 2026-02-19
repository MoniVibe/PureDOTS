# Combat + Mining Data Contracts v0

Status: draft-active  
Date: 2026-02-19

This document codifies combat/mining as shared data contracts, with simulation logic owned by PureDOTS and game-side loaders acting as adapters.

## Goal

- Keep combat/mining meaning canonical and reusable across projects.
- Keep runtime logic deterministic and centralized in PureDOTS.
- Keep Space4X and Godgame free to project/present differently.

## Contract Set (v0)

- `contract.mining.v0`
  - Schema: `Docs/Canonicity/Schemas/contract.mining.v0.schema.json`
- `contract.combat.v0`
  - Schema: `Docs/Canonicity/Schemas/contract.combat.v0.schema.json`
- `contract.room_profile.v0`
  - Schema: `Docs/Canonicity/Schemas/contract.room_profile.v0.schema.json`
- `contract.scenario_envelope.v0`
  - Schema: `Docs/Canonicity/Schemas/contract.scenario_envelope.v0.schema.json`
- `contract.mission_objective.v0` (scaffold)
  - Schema: `Docs/Canonicity/Schemas/contract.mission_objective.v0.schema.json`
- `contract.loot_cache.v0` (scaffold)
  - Schema: `Docs/Canonicity/Schemas/contract.loot_cache.v0.schema.json`
- `contract.encounter_profile.v0` (scaffold)
  - Schema: `Docs/Canonicity/Schemas/contract.encounter_profile.v0.schema.json`

Canonical registry for all above IDs:
- `Docs/Canonicity/canonical_contracts.v0.json`
- `Docs/Canonicity/canonical_contract_payloads.v0.json`

## Ownership Boundary

PureDOTS owns:
- contract semantics,
- deterministic rules (resolution, timing, outcomes),
- contract validation rules and schema versioning.

Game projects own:
- JSON authoring convenience and projection fields,
- prefab/entity mappings,
- visuals, UI, pacing flavor.

## Mapping To Current Runtime (No Rewrite Required)

Current loaders can remain while contracts become canonical:

- Space4X loader: `space4x/Assets/Scripts/Space4x/Scenario/Space4XMiningScenarioSystem.cs`
  - Existing `scenarioConfig/spawn/actions` can be treated as adapter projection.
  - New contract IDs should be added as references first, then resolved by adapter logic.

- Godgame loader: `godgame/Assets/Scripts/Godgame/Scenario/GodgameScenarioLoaderSystem.cs`
  - Existing `entities/actions` shape can be retained as projection.
  - Contract references become source-of-truth inputs for spawn/action composition.

- PureDOTS scenario core: `puredots/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/ScenarioDefinition.cs`
  - Remains the deterministic scenario backbone.
  - Contract payload integration can be staged without breaking existing scenario files.

## Required Rules

1. Contract IDs are stable.
2. Contract data validates against versioned JSON schema.
3. Game-side projection fields must not change contract meaning.
4. Rooms compose contracts; rooms do not replace combat/mining contracts.
5. Any behavior that affects deterministic outcomes must be expressible via contract fields.

## Room Composition Rule

`room_profile` is a composition contract:
- It references mining/combat contract IDs.
- It sets budgets/weights/activation order.
- It does not redefine core mining/combat semantics.

## Migration Plan (MVP-safe)

1. Add contract schemas and docs (this pass).
2. Add contract references to canonical scenarios (non-breaking).
3. Add validation gate in CI for schema + canonical IDs.
   - Script: `puredots/CI/validate_canonicity_contracts.ps1`
4. Migrate loader internals from ad hoc fields to contract-driven resolution gradually.
5. Expand to mission/cache/encounter contracts via `Docs/Canonicity/Data_Contract_Canon_Sprint_v0.md`.
6. Register reusable contract payloads under `Docs/Canonicity/Payloads/*` and index in `canonical_contract_payloads.v0.json`.

## Non-Goals (v0)

- No mandatory runtime refactor in this pass.
- No balancing freeze.
- No presentation lock-in.
