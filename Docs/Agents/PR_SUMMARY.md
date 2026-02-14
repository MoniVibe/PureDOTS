# PR Summary: Role-First AGENTS.md Sweep

## What changed

### Roles

| Role | Purpose | Slot |
|------|---------|------|
| **Validator** | Only role that runs canonical validations and declares "super green." Bunker-only. | VALIDATION |
| **Builder** | Implements features, local smoke; may NOT declare validated. | DEMO_SLICE |
| **Ops** | Task wiring, headless tooling; does NOT run validations. | TASK_WIRING |
| **Perf/Harness** | Perf gates, scale harness, report schemas; does NOT implement large gameplay. | SCALE_HARNESS |
| **Sherpa/Bridge** | Coordination only; forbidden from validations. CodexBridge must NOT "helpfully validate." | — |
| **Docs** | Documentation only; never touches CI, harness, runtime. | DOCS |

### Slot locking

- **Location:** `Docs/Agents/SLOTS.md` (identical in puredots, space4x, godgame)
- **Slots:** VALIDATION (Validator only), SCALE_HARNESS, TASK_WIRING, DEMO_SLICE, DOCS
- **Rule:** Must claim a slot before touching files in that scope.

### Files modified

| Repo | Files |
|------|-------|
| puredots | `AGENTS.md` (role-first), `Docs/Agents/SLOTS.md` (new), `Docs/Agents/SWEEP_REPORT.md` (new), `Docs/INDEX.md` |
| space4x | `AGENTS.md` (role-first), `Docs/Agents/SLOTS.md` (new), `Docs/AGENTS.md`, `Docs/INDEX.md` |
| godgame | `AGENTS.md` (role-first), `Docs/Agents/SLOTS.md` (new), `Docs/AGENTS.md`, `Docs/INDEX.md` |

### Preserved

- TRI multi-track philosophy, cross-OS boundaries (WSL/Windows, headless/presentation)
- Space4X carrier-as-village, command ladder, demo slice priorities
- Headless run discipline: canonical paths, ops-bus, headless_runbook, headlesstasks
- Recurring pitfalls (branch pin, telemetry bloat) referenced as known signatures
- DOTS 1.4 + Burst rules, hard rules, error patterns

### Sweep report

- **Link:** `Docs/Agents/SWEEP_REPORT.md`
- Desktop vs laptop doc drift documented (directive, recurring at laptop root; desktop lacks these at unity_clean root)

---

## Statement

**No validations were run. Bunker owns green.**
