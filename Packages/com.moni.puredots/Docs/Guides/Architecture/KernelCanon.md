# Kernel Canon (Living Contract)

This is the canonical kernel list for TRI simulation architecture.

Purpose:
- define shared single sources of truth
- reduce drift across `space4x` and `godgame`
- let iterator lanes ship UI/features without rewriting core contracts

Status values:
- `Proposed` = agreed concept
- `Scaffolded` = contracts/systems exist
- `Migrating` = dual path active
- `Canonical` = only path allowed

## Kernel List

| Kernel | Scope | Primary Responsibility | Status |
|---|---|---|---|
| TimeKernel | shared | tick, fixed-step, pause/speed/rewind, catch-up policy | Migrating |
| InputKernel | shared | per-tick input snapshot, bindings, action normalization | Scaffolded |
| OrderKernel | shared | all player/AI intents as deterministic command stream | Proposed |
| MovementKernel | shared | locomotion intent -> movement state and steering constraints | Scaffolded |
| PhysicsKernel | shared facade | physics queries/config/contact event normalization | Proposed |
| EventKernel | shared | domain event bus/stream with ordering and idempotency rules | Proposed |
| ProjectionKernel | shared | read models for UI/HUD/tooltips/debug | Proposed |
| UIKernel | shared | panel/tooltip/tab state + UI intents | Scaffolded |
| EconomyKernel | shared | inventory/reservation/transfer/ledger mutation rules | Migrating |
| IdentityKernel | shared | stable IDs, ownership, reference lifetimes | Migrating |
| RulesKernel | shared | tunables, scenario overrides, versioned config provenance | Proposed |
| SaveReplayKernel | shared | snapshot/replay format and deterministic restore path | Proposed |
| TelemetryKernel | shared | metrics/event schema and export contract | Migrating |
| RenderingKernel | shared facade | semantic render keys, presentation registry/apply flow | Migrating |

## Current References

- UI kernel contracts and systems:
  - `Packages/com.moni.puredots/Runtime/Runtime/UI/UiKernelComponents.cs`
  - `Packages/com.moni.puredots/Runtime/Systems/UI/UiKernelBootstrapSystem.cs`
  - `Packages/com.moni.puredots/Runtime/Systems/UI/UiKernelIntentSystem.cs`
- Input kernel contracts and systems:
  - `Packages/com.moni.puredots/Runtime/Input/InputKernelComponents.cs`
  - `Packages/com.moni.puredots/Runtime/Systems/Input/InputKernelBootstrapSystem.cs`
  - `Packages/com.moni.puredots/Runtime/Systems/Input/InputKernelSanitizationSystem.cs`
- Movement kernel contracts and systems:
  - `Packages/com.moni.puredots/Runtime/Runtime/Movement/MovementKernelComponents.cs`
  - `Packages/com.moni.puredots/Runtime/Systems/Movement/MovementKernelBootstrapSystem.cs`
  - `Packages/com.moni.puredots/Runtime/Systems/Movement/MovementKernelPoseCaptureSystem.cs`
  - `Packages/com.moni.puredots/Runtime/Systems/Movement/MovementKernelGuardSystem.cs`

## Kernel Admission Rule

A domain becomes a kernel only if all are true:
1. Cross-game reuse pressure exists.
2. Ordering/determinism matters.
3. Multiple systems currently duplicate behavior.
4. Replay/debug/validation benefits from a single contract.

## Implementation Rule

For each kernel:
1. Define data contracts first (components/buffers/enums).
2. Add one bootstrap + one processor path.
3. Add adapter layer for old callers.
4. Migrate writes first, reads second.
5. Remove legacy path after validator signoff.

## Migration Playbook

1. `Charter`
   - Fill `KernelCharter_TEMPLATE.md`.
   - Name boundaries and non-goals.
2. `Scaffold`
   - Add contracts and minimal systems in `puredots`.
3. `Bridge`
   - Patch game-side callers to emit/read kernel contracts.
4. `Dual-Run`
   - Keep legacy and kernel path in parallel behind feature flag.
5. `Flip`
   - Switch reads to kernel state.
6. `Retire`
   - delete legacy route, update docs/tests.

## Sequencing Recommendation

Near-term order:
1. OrderKernel
2. ProjectionKernel
3. PhysicsKernel (facade)
4. EventKernel

Rationale:
- this sequence unlocks faster UI/menu iteration while keeping simulation deterministic.

## Definition Of Done (Per Kernel)

- Single write path in shared contracts.
- Deterministic behavior under fixed seed.
- Replay/restore compatibility documented.
- Smoke coverage in both `space4x` and `godgame` where relevant.
- Legacy path removed or explicitly marked temporary with expiry.

## Governance

- Canon owner: `puredots` maintainers + validator role.
- Iterator lanes can extend only through chartered contract changes.
- Breaking kernel changes require migration notes in this file.
