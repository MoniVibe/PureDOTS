# Module Recipe Template (Blank-by-Default)

Use this template when proposing or implementing a new **module** so features stay composable and “anything can be anything” (sentient buildings, living items, etc.).

Related:
- `Docs/Concepts/Core/Entity_Modularity_And_Capabilities.md`
- `Docs/ContractFirst_FeatureRecipe.md` (long-form example)
- `Docs/Contracts.md` (shared contracts)

---

## Context & Intent

**One-line description:**  
<What this module enables.>

**Problem solved:**  
<Why we need it; what it unlocks.>

**Applies to:**  
- [ ] PureDOTS (shared)
- [ ] Godgame
- [ ] Space4X

**Determinism/rewind:**  
- [ ] Required (Record-mode only; rewind-safe)
- [ ] Not required (explain why)

---

## Capability Adapter Contract (no “entity types”)

**Eligibility rule:** This module operates on any entity that has:
- Required components:
  - <ComponentA>
  - <ComponentB>
- Required buffers:
  - <BufferA>
- Optional components/buffers (change behavior, not eligibility):
  - <OptionalX>

**Representation / proxies (if needed):**
- If entity lacks agency, resolve via:
  - `OwnerRef` / `ProxyRef` / `RepresentativeRef` (pick one and define it)
- Define “who speaks for this thing” explicitly (do not assume buildings/items are inert).

**Attach/detach behavior:**
- What happens if components are added at runtime?
- What happens if components are removed at runtime?
- Which state must be reset when re-attaching?

**Optional: single-tag bootstrap**
- If this module should be “one tag to enable”, define a `*ModuleTag` and a bootstrap system that ensures the full contract exists.

---

## Data & Contracts

**Shared contracts (update `Docs/Contracts.md` if cross-project):**
- <ContractName v1> (producer/consumer/schema)

**Non-shared data (game-local OK):**
- <GameSpecificComponent>

---

## Systems (PureDOTS spine + game adapters)

### PureDOTS spine (if shared)
- `PureDOTS.Runtime.<Module>.Systems.<SystemA>`:
  - Update group + ordering:
  - Reads:
  - Writes:
  - Invariants:

### Game adapters (if needed)
- Godgame adapter(s):
  - What authoring writes/attaches:
  - What bridge/emitter writes:
- Space4X adapter(s):
  - What authoring writes/attaches:
  - What bridge/emitter writes:

**Ordering / groups:**
- Which system group owns this module?
- Which existing groups must it run before/after?

---

## Scenario & Runtime Knobs (data-driven)

**Scenario JSON fields (if any):**
- `scenario.<...>`:

**Env vars (if any):**
- `<ENV_VAR>`:

**Default behavior if knobs are missing:**
- <Default>

---

## Proof & Telemetry (headless-first)

**Headless proof line(s):**
- `[<ProofSystem>] PASS ...`

**Telemetry metrics/events (small payloads):**
- `metric.<...>`:
- `event.<...>`:

**Acceptance criteria (must be observable):**
- [ ] <Proof passes>
- [ ] <Metric nonzero / monotonic / within bounds>
- [ ] <No smoke exceptions from involved system groups>

---

## Failure Modes & Guards (prevent regressions)

- Common DOTS hazards (ECB remap, structural changes, aliasing, under-stepping):
  - <List any applicable, with guard strategy>
- Expected “safe no-op” behavior when capability is missing:
  - <e.g., system does not run; no logs>

---

## Implementation Checklist

- [ ] Add/define components + buffers
- [ ] Add/define systems and ordering
- [ ] Add/define adapters (if needed)
- [ ] Add scenario/env knobs (if needed)
- [ ] Add proof + telemetry
- [ ] Verify via `*Smoke.md` signals + proof lines
