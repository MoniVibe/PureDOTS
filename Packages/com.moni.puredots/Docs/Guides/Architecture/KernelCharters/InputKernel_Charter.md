# InputKernel Charter

## 1) Identity

- Kernel name: `InputKernel`
- Owner: `puredots` runtime
- Date: 2026-02-25
- Status: scaffolded
- Scope: shared

## 2) Problem Statement

- Current pain:
  - input values are sampled in multiple adapters and consumed with inconsistent sanitization
  - intent payloads drift between games and feature slices
  - one-shot toggles can replay unexpectedly during fixed-step catch-up
- Why kernel:
  - a single per-tick input contract gives deterministic handoff into movement/order/UI kernels

## 3) Responsibilities

- In scope:
  - singleton input-kernel tick/revision state
  - shared locomotion intent contract
  - ownership marker for player-controlled entities
  - per-tick sanitization/clamping/normalization
- Out of scope (for now):
  - action rebinding UX
  - device discovery/profile persistence
  - replacing existing game-side input adapters in one pass

## 4) Contracts

- `InputKernelRootTag` (`IComponentData`)
- `InputKernelState` (`IComponentData`)
- `InputKernelOwnership` (`IComponentData`)
- `InputKernelLocomotionIntent` (`IComponentData`)
- `InputKernelDiagnostics` (`IComponentData`)

## 5) Ordering + Determinism

- Bootstrap phase:
  - `InputKernelBootstrapSystem` runs in `InitializationSystemGroup` (`OrderLast`).
- Sanitization phase:
  - `InputKernelSanitizationSystem` runs in `FixedStepSimulationSystemGroup` (`OrderFirst`).
- Authority:
  - downstream gameplay systems should consume sanitized `InputKernelLocomotionIntent` values.

## 6) Migration Plan

- Legacy path:
  - game-specific input components continue to exist during migration.
- Adapter strategy:
  - bridges map game payloads to/from `InputKernelLocomotionIntent`.
- Dual-run:
  - maintain legacy + kernel writes until validators confirm parity.
- Future hardening:
  - enforce kernel-only reads for locomotion/order command producers.

## 7) Validation

- Validate in play mode:
  - ensure `InputKernelRootTag` singleton exists
  - confirm controlled entities have `InputKernelOwnership`
  - verify locomotion intents are clamped and direction vectors are normalized
- Observe:
  - `InputKernelState.Revision`
  - `InputKernelDiagnostics`

## 8) Risks

- Adapters that bypass kernel writeback can reintroduce one-shot toggle replay.
- Over-broad auto-tagging could attach locomotion intent to entities that never consume it.

## 9) Signoff

- Iterator ready: yes (scaffold + bridge path)
- Validator approved: pending
- Canon updated: yes (`KernelCanon.md`)
