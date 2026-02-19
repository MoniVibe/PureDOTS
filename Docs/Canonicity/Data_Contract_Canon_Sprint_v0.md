# Data Contract Canon Sprint v0

Status: active  
Date: 2026-02-19  
Owner: PureDOTS canonicity lane

This sprint codifies one truth source for data contracts across TRI projects so `space4x` and `godgame` stay data-driven while `puredots` remains game-agnostic.

## Sprint Outcome

1. Canonical contract registry exists and is validated: `Docs/Canonicity/canonical_contracts.v0.json`.
2. Canonical payload registry exists and is validated: `Docs/Canonicity/canonical_contract_payloads.v0.json`.
3. Shared contract schemas are source of truth, not game-local JSON conventions.
4. Game projects only adapt/present contract meaning; they do not redefine it.
5. Validation runs locally and in CI warning mode to expose drift early.

## Operating Rule

1. Add/modify contract semantics in `puredots` docs first.
2. Register contract in `canonical_contracts.v0.json`.
3. Add/update schema in `Docs/Canonicity/Schemas`.
4. Add/update payloads in `Docs/Canonicity/Payloads/*` and register them in `canonical_contract_payloads.v0.json`.
5. Update adapters in `space4x` and `godgame`.
6. Run `CI/validate_canonicity_contracts.ps1`.

## Contract Scope (Sprint v0)

Active:
- `contract.mining.v0`
- `contract.combat.v0`
- `contract.room_profile.v0`
- `contract.scenario_envelope.v0`

Scaffolded for next passes:
- `contract.mission_objective.v0`
- `contract.loot_cache.v0`
- `contract.encounter_profile.v0`

## PureDOTS vs Game-Side Canon Rule

Put it in `puredots` if it changes deterministic outcomes:
- objective success/failure semantics,
- damage/mining/resource outcome rules,
- spawn/encounter composition semantics,
- boss phase and escalation semantics.

Put it in game-side adapters if it is projection only:
- art/prefab binding,
- UI labels, lore text, flavor pacing copy,
- optional VFX/audio cues,
- local debug legend conventions.

## Sprint Lanes

1. Canon Registry Lane  
Single contract registry + validator checks for ID/path/schema/doc integrity.

2. Contract Payload Lane  
Single payload registry + validator checks for payload ID/path/type and cross-contract references.

3. Contract Schema Lane  
Schema-first definitions for mission, cache, and encounter slices.

4. Scenario Composition Lane  
Scenario envelope and room composition reference canonical contract IDs only.

5. Adapter Lane  
`space4x`/`godgame` map local entity sets to shared contract IDs without changing semantics.

6. Validation Lane  
Warning-only CI gate for drift visibility (`HARSH WARNING` policy) and local preflight parity.

## Definition of Done (v0 Sprint)

1. No contract-bearing scenario/entity data exists without a canonical contract ID.
2. No duplicate contract meaning under multiple IDs.
3. Every canonical contract entry has an existing schema and doc path.
4. Reusable payloads are indexed and cross-reference valid.
5. Both games can consume shared contracts through adapters without changing PureDOTS meaning.
6. Validator passes locally at workspace root and `puredots` repo root.

## Near-Term Backlog (v1)

1. Add contract relationship checks (room refs -> existing contract IDs).
2. Add schema validation against actual contract payload files once payload registries are added.
3. Add mission/cache/encounter micro scenarios to canonical scenario set.
