# Netplay: Not Now

Netplay is intentionally deferred. Single-player deterministic headless proof is the current gate.

## Why Deferred
- The active risk is simulation correctness and scale reliability, not transport.
- Adding multiplayer surface area early increases failure modes and slows core proof loops.
- Space4X and Godgame both still benefit more from stronger shared runtime contracts.

## Constraints To Preserve Future Netplay
- Keep simulation tick-authoritative and deterministic.
- Keep command ingestion explicit and replayable (buffered command streams, no hidden side channels).
- Avoid wall-clock coupling and presentation-authoritative state changes.
- Keep stable ids/keys for entities and scenario actions where practical.
- Preserve rewind-safe state transitions and phase ordering discipline.

## Explicit "Do Not"
- Do not couple gameplay correctness to rendering/UI timing.
- Do not add NetCode-specific branching into core simulation systems.
- Do not weaken determinism rules to "make multiplayer easier."

## Entry Criteria To Start Netplay Work
- Determinism gates green across representative Space4X and Godgame scenarios.
- Headless perf gates stable at target tiers.
- Rewind/time contracts and command routing validated under stress.
- Operational triage burden is under control (no recurring red signatures from the current ledger).

## Version Advisory
- Keep compatibility with current runtime baseline (Entities 1.4.x family in this workspace).
- Treat NetCode adoption as an explicit milestone, not incidental package drift.
