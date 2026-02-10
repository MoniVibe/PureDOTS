# Nightly Commandments (Headless)

Purpose: define the non-negotiable behaviors for nightly headless agents. This is outcome-driven guidance, not quota-driven. We do not require time-on-task or lines-of-code targets.

Sources of truth: `headless_runbook.md`, `headlesstasks.md`, `OPS_BUS_PROTOCOL.md`, and `NIGHTLY_TARGET_BANK.md`. If this file conflicts with any of those, the source documents win.

1. Honor the canonical entrypoints and procedures. Use `headlessctl` and `nightly_runner` as the standard path. Do not invent alternate pipelines.
2. Keep the ops bus correct. Requests live under `TRI_ROOT/.tri/state/ops/requests`. Results and locks are written once and cleaned up last.
3. Never run Unity Editor builds. Use the license-free pipeline only. If a build would require licensing, mark it failed with a clear note and unlock.
4. Avoid mixed modes. Use the correct headless env vars, and do not bleed presentation or editor settings into nightly headless runs.
5. Prove success explicitly. A cycle is only successful when logs, telemetry, and results JSON agree. Never infer success from a `done` print.
6. Fail loudly and specifically. If a run fails, record the failure reason, preserve the evidence, and file the proper request or queue entry.
7. Protect telemetry health. If telemetry is truncated or missing required signals, treat the run as failed and resolve the budget or signal gap.
8. Respect OS boundaries. WSL agents do not edit `Assets/` or `.meta`. Asset fixes happen in Windows/presentation mode or are queued.
9. Update the written record. If you change expectations, toggles, or bank outcomes, update the relevant headless docs before the cycle ends.
10. Reject quota mandates. Do not add time-based or LOC-based constraints. Choose scope based on outcomes, risk, and documentation, and justify any skipped validation.

These commandments are the minimal standard. Agents are expected to exercise judgment and raise the bar when the work demands it.
