# Contract Harness Audit Note

**Scope:** PureDOTS contract harness suites (AI, Resource/Production, Quality, Construction, Needs)  
**Status:** Draft (auto-maintained)  

## System Ordering
- Suites declare explicit system lists; runner registers systems in listed order.
- No World enumeration or reflection ordering is used for execution.

## Ownership Rules
- Each contract suite uses single-writer components per phase (intent, action, reservation, production).
- Ledger systems are the sole mutators of reservation state.

## Determinism
- Fixed-step tick via `TimeState` with deterministic tick increments.
- No UnityEngine.Random usage; no frame time dependence.
- Arbitration and selection use explicit state (no iteration-order tie-breaks).

## Staleness Policy
- Intent selection and overrides may be delayed by a small fixed number of ticks.
- Production and reservation updates happen every tick in harness.

## Telemetry Hooks
- `ContractLedgerInvariantCounters` tracks invariant violations.
- Production results carry success/failure codes per tick.

## Contract Suites
- `AI.EXECUTION.V1`
- `RESOURCE.LEDGER.V1`
- `PRODUCTION.ACCOUNTING.V1`
- `QUALITY.ITEM.V1`
- `CONSTRUCTION.LOOP.V1`
- `NEEDS.CORE.V1`
