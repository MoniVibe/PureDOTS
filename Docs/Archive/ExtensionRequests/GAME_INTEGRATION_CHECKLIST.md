# Game Integration Checklist (PureDOTS)

Use this before integrating new mechanics or PureDOTS updates to keep game code stable and unblock teams.

## Pre-Flight
- Versions: confirm target PureDOTS package/version and Unity version; note required defines.
- Request filed: if you need new hooks, file a `Docs/ExtensionRequests` entry with an owner and ETA.
- Ownership: name a game-side owner and PureDOTS point of contact.

## API & Assemblies
- Assembly refs: confirm asmdef references are correct and game code only consumes the stable surfaces (no internal namespaces).
- Feature flags: wrap experimental hooks behind defines/config (e.g., `PUREDOTS_EXPERIMENTAL_*`).
- Shims: if migrating, ensure adapter/shim types are in place and mapped in game code.

## Data & Authoring
- Configs/Blobs: identify new blobs/config assets and default values; add authoring validators if applicable.
- IDs/Enums: reserve ID ranges or enum slots as needed; avoid game-specific IDs in shared enums.
- Serialization: check save/load impact and add versioning guards if data changes.

## Performance & Determinism
- Budgets: set Hz/alloc/burst expectations per system; validate in a sandbox scene.
- Determinism: ensure replay/rewind paths are exercised if the game uses them.
- Jobs: confirm Burst safety (no managed types, no unsupported params) and correct scheduling group.

## Testing
- Unit/Integration: add tests covering new mechanics and key edge cases; include a minimal PureDOTS harness scene if needed.
- Perf tests: capture baseline perf metrics (frame time, allocs) before/after.
- Validation: run with/without feature flags to ensure graceful fallback.

## Rollout & Support
- Changelog/Migration: document behavior changes and copy/paste snippets for game teams.
- Telemetry/Debug: expose minimal metrics/logs for live validation (and how to toggle them).
- SLA: note who to page for regressions and expected response window during milestone crunch.
