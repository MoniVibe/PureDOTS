# Canonicity Contract v0

Status: draft-active  
Date: 2026-02-19

This contract defines the minimum rules to stop drift and duplication.

## Core Terms

- Canonical artifact: the single authoritative source for a given meaning.
- Projection artifact: a game-side mapping/adaptation of canonical meaning.
- Legacy artifact: historical or duplicate content kept for reference/regression only.

## Artifact Classes and Ownership

| Artifact class | Canonical owner | Canonical location | Notes |
|---|---|---|---|
| Simulation semantics/contracts | PureDOTS | `puredots/Packages/com.moni.puredots` + `puredots/Docs/TruthSources/*` + contract tests | Deterministic behavior authority. |
| Domain data contracts (combat/mining/room) | PureDOTS | `puredots/Docs/Canonicity/Combat_Mining_DataContracts_v0.md` + `puredots/Docs/Canonicity/Schemas/*` | Shared contract semantics + schema validation baseline. |
| Canonical contract registry | PureDOTS | `puredots/Docs/Canonicity/canonical_contracts.v0.json` | Machine-readable source of truth for contract IDs and schema/doc ownership. |
| Canonical contract payload registry | PureDOTS | `puredots/Docs/Canonicity/canonical_contract_payloads.v0.json` + `puredots/Docs/Canonicity/Payloads/*` | Machine-readable source of truth for reusable payload IDs and cross-contract references. |
| Shared content intent | PureDOTS | `puredots/Docs/ContentIntent/*` | Cross-game meaning layer. |
| Scenario canonical set | Shared (owned in PureDOTS docs) | `puredots/Docs/Canonicity/canonical_scenarios.v0.json` | Registry of which scenario files are canonical gates/slices. |
| Scenario implementations | Game projects | `space4x/Assets/Scenarios/*`, `godgame/Assets/Scenarios/*`, `puredots/Assets/Scenarios/*` | Files live per project; canonical membership declared in shared registry. |
| Game-side adapters | Game projects | `space4x/Docs/ContentIntent/*`, `godgame/Docs/ContentIntent/*` | Must not redefine simulation meaning. |
| Goalcards and test intent docs | Game projects | `*/Docs/Scenarios/GoalCards/*` | Must reference canonical scenario IDs. |
| Presentation fallbacks/legends | Game projects | project docs/assets | Non-authoritative by design. |

## Hard Rules

1. One meaning -> one canonical ID.
2. Every scenario JSON must carry `scenarioId`.
3. Canonical IDs are stable; do not silently rename.
4. Game-side can project canonical meaning; it cannot fork it.
5. PureDOTS must not depend on project-specific presentation IDs or assets.
6. Legacy artifacts must be marked as legacy in docs and excluded from canonical registries.
7. Contract-bearing JSON must validate against the active schema version before merge.
8. New/changed contract IDs must be registered in `canonical_contracts.v0.json`.
9. Reusable contract payload IDs must be registered in `canonical_contract_payloads.v0.json`.

## Naming Rules (MVP)

- Scenario IDs: keep existing IDs stable; for new IDs prefer `scenario.<project>.<slice>`.
- Canonical registry keys: `canon.<project>.<purpose>`.
- Shared content intent IDs stay in `intent.<family>.<name>` form.

## Change Workflow

1. Update meaning docs first (`ContentIntent` and/or truth-source docs).
2. Update canonical registries (`canonical_contracts.v0.json`, `canonical_contract_payloads.v0.json`, `canonical_scenarios.v0.json`).
3. Update scenario JSON/data and project adapters.
4. Update goalcards/runbooks if gate scenarios changed.
5. Verify with headless smoke + required micro slices.

## PR Merge Checklist

1. Which artifact is canonical after this change?
2. Did any canonical ID change? If yes, where are aliases/migrations documented?
3. Did all touched scenarios keep `scenarioId`?
4. Did project-side docs/adapters reflect the change?
5. Is legacy content clearly marked and not accidentally treated as canonical?
