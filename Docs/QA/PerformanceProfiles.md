# Platform Performance Profiles (Scaffold)

Updated: 2025-10-25

This document will catalogue profiling runs for key platform targets once instrumentation (job scheduling metrics, grid update timers, replay harness) is enabled. Use it to record configuration, frame timings, and optimisation notes.

## Template (Fill When Data Available)

### Platform / Build
- Target Platform: e.g., Windows IL2CPP, Xbox Series, Quest
- Build Type: Development / Release / Headless
- Burst: Enabled / Disabled
- Worker Count: e.g., Default (cores-1)
- Scene: e.g., IntegrationPerformance.unity

### Environment Settings
- Environment grid cell size / resolution
- Spatial grid provider & cell size
- Entity counts (villagers, resources, miracles, vegetation)

### Metrics (60s Capture)
- Average frame time (ms)
- `EnvironmentSystemGroup` budget (ms)
- `SpatialSystemGroup` budget (ms)
- `VillagerSystemGroup` budget (ms)
- Native memory footprint (MB)
- Burst compilation warnings/errors

### Observations & Actions
- Bottlenecks identified
- Tunables adjusted (cell size, worker count, group throttling)
- Follow-up tasks / TODO references

### Replay/Determinism Check
- Test name (from CI harness)
- Result (Pass/Fail)
- Notes (diff snapshots, event mismatches)

---

Update this file once instrumentation (debug overlay metrics, job profiling hooks) is merged. Reference from `Docs/TruthSources/PlatformPerformance_TruthSource.md` and `Docs/TODO/Utilities_TODO.md` for ongoing work.
