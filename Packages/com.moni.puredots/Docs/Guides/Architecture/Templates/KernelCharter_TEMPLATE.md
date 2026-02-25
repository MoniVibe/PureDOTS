# Kernel Charter Template

Use this template before creating or upgrading any kernel.

## 1) Identity

- Kernel name:
- Owner:
- Date:
- Status: proposed / scaffolded / migrating / canonical
- Scope: shared / game-specific / facade

## 2) Problem Statement

- Current pain:
- Why this needs a kernel:
- Why now:

## 3) Responsibilities

- In scope:
- Out of scope:
- Explicit non-goals:

## 4) Contracts

- New components/buffers/enums:
- Existing contracts reused:
- Intent/event types:

## 5) Ordering + Determinism

- System groups and order:
- Determinism constraints:
- Seed/state ownership (if applicable):

## 6) Migration Plan

- Legacy path:
- Adapter strategy:
- Dual-run flag:
- Cutover conditions:
- Retirement plan:

## 7) Validation

- Unit tests:
- PlayMode/smoke checks:
- Replay/restore checks:
- Telemetry hooks:

## 8) Risks

- Technical risks:
- Operational risks:
- Rollback plan:

## 9) Signoff

- Iterator ready: yes/no
- Validator approved: yes/no
- Canon updated: yes/no
