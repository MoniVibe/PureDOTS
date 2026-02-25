# MovementKernel Charter

## 1) Identity

- Kernel name: `MovementKernel`
- Owner: `puredots` runtime
- Date: 2026-02-24
- Status: scaffolded
- Scope: shared

## 2) Problem Statement

- Current pain:
  - movement logic is distributed across systems and domains
  - transforms can be mutated outside movement pipelines
  - hard to validate deterministic movement authority
- Why kernel:
  - one movement authority contract enables predictable simulation and easier UI/debug tooling

## 3) Responsibilities

- In scope:
  - movement ownership marker (`MovementKernelOwned`)
  - canonical pose capture (`MovementKernelPose`)
  - external write detection and optional rollback (`MovementKernelGuardSystem`)
  - movement-mode/integration path in `Systems/Movement/*`
- Out of scope (for now):
  - global lockout for all `LocalTransform` writes in all systems
  - full physics solver ownership
  - nav/pathfinding redesign

## 4) Contracts

- `MovementKernelOwned` (`IComponentData`)
- `MovementKernelPose` (`IComponentData`)
- `MovementKernelGuardConfig` (`IComponentData`)
- `MovementKernelGuardStats` (`IComponentData`)
- `MovementKernelViolation` (`IBufferElementData`)

## 5) Ordering + Determinism

- Capture phase:
  - `MovementKernelPoseCaptureSystem` runs after movement integration in fixed-step.
- Guard phase:
  - `MovementKernelGuardSystem` runs in `LateSimulationSystemGroup` (`OrderLast`).
- Authority:
  - for `MovementKernelOwned` entities, captured pose is authoritative for the frame.

## 6) Migration Plan

- Legacy path:
  - existing systems may still read/write transforms directly.
- Adapter strategy:
  - opt entities in via `MovementKernelOwnershipAuthoring`.
- Dual-run:
  - enabled by default with guard config.
- Current enforcement:
  - detection + optional rollback for kernel-owned entities.
- Future hardening:
  - route all movement writes through kernel-owned command buffers and tighten guard policies.

## 7) Validation

- Validate in play mode:
  - add `MovementKernelOwnershipAuthoring` to target prefab/entity
  - ensure movement still updates
  - force an external transform write and confirm guard logs rollback
- Observe:
  - `MovementKernelGuardStats`
  - `MovementKernelViolation` buffer

## 8) Risks

- If too many entities are kernel-owned early, guard may expose existing drift/noise.
- Some intentional late-frame transform edits may be rolled back if not migrated.

## 9) Signoff

- Iterator ready: yes (opt-in ownership path)
- Validator approved: pending
- Canon updated: yes (`KernelCanon.md`)
