# Platform & Performance Truth-Source (PureDOTS)

## Purpose
- Capture project-wide policies for IL2CPP/AOT, Burst compilation, and job scheduling.
- Define hot vs. cold execution paths to keep core loops responsive.
- Serve as the companion reference to `RuntimeLifecycle_TruthSource.md` for platform-level considerations.

## AOT / IL2CPP Compliance
- Avoid reflection/dynamic type usage inside Burst jobs (`GetType()`, `Activator`, etc.).
- Use static registration helpers and `[Preserve]` attributes for types accessed via reflection or dynamic dispatch.
- Maintain `link.xml` when Unity linking strips assemblies; document required entries.
- Provide an IL2CPP build checklist (Scripting Backend, API compatibility level, `Managed Stripping Level`).
- Ensure testing pipeline includes an IL2CPP build (e.g., Windows or DOTS Runtime headless) to catch AOT regressions.

**Audit Status**: See `Docs/QA/IL2CPP_AOT_Audit.md` for detailed reflection usage analysis and preservation requirements.

### IL2CPP Build Checklist
1. **Player Settings → Other Settings**
   - `Scripting Backend`: IL2CPP
   - `Api Compatibility Level`: `.NET Standard 2.1`
   - `Managed Stripping Level`: Low (default) during development; increase only after validating `link.xml` coverage.
   - Enable `Allow 'Unsafe' Code` for the DOTS assemblies that require it.
2. **Burst Settings**
   - Ensure Burst is enabled for standalone builds (`Jobs → Burst AOT Compilation`).
   - Set `CompileSynchronously` for development builds to surface compile issues immediately.
3. **Linker Configuration**
   - Place project-wide `link.xml` under `Assets/Config/Linker/` (example below).
   - Preserve DOTS reflection usage (e.g., `Unity.Entities`, any dynamic type creation).
4. **Build Scripts**
   - Update CI build step to pass `-enableBurstCompilation` and `-burst-compile-assembly=PureDOTS.Runtime` flags as needed.
   - Record build output (`build/Logs/BurstCompilation.txt`) for troubleshooting.

#### `link.xml` Configuration

**Location**: `PureDOTS/Assets/Config/Linker/link.xml`

**Current Entries** (see audit for full list):
- `SystemRegistry` and `BootstrapWorldProfile` (bootstrap system discovery)
- `VillagerJob` enum (debug console enum reflection)
- `ResourceTypeIndex`, `DivineHandConfig` (runtime configs)
- Visual manifest types (presentation bridge)

**See**: `Docs/QA/IL2CPP_AOT_Audit.md` for complete preservation requirements and rationale.

#### Burst Troubleshooting Tips
- If Burst compilation fails in IL2CPP, inspect `Library/Bee/tmp/il2cppOutput/BurstDebugInformation_DoNotShip/` for generated C++.
- Use `BurstCompilerOptions.EnableBurstCompilation = false` temporarily to isolate managed vs. Burst issues.
- Ensure all jobs/components referenced by Burst are located in assemblies marked as `Entities.ForEach` compatible (no references to editor-only code).
- When encountering AOT generic instantiation errors, add explicit static constructors or dummy usage to force code generation.

#### Job Worker Tuning
- Default worker count is `JobsUtility.JobWorkerCount = JobsUtility.JobWorkerCountHint`. Override via `World.GetOrCreateSystemManaged<JobWorkerBootstrapSystem>()` during bootstrap.
- Scenarios:
  - **High AI density**: Increase worker count by 2 (but keep ≤ logical cores) to improve villager pathfinding throughput.
  - **Physics-heavy scenes**: Keep worker count equal to physical cores minus one to leave headroom for Burst-compiled physics jobs.
  - **Low-end hardware**: Expose setting in config to reduce workers (prevents contention and thermal throttling).
- Always profile after changes; document tuning in `Docs/QA/PerformanceProfiles.md` (TODO).

## Burst Compilation Guidance
- Enforce `BurstCompilerOptions.CompileSynchronously` in development; fail fast on Burst errors.
- Use `BurstCompile` on hot-path systems/jobs; avoid for cold/editor-only code.
- Keep job signatures Burst-safe (no managed types, pass structs via `in`/`ref`/`out`).
- Validate deterministic behaviour by running Burst-enabled tests on build machines.
- `EnvironmentEffectUpdateSystem` evaluates scalar/vector/pulse effects via catalog indices; keep effect parameter structs blittable and avoid delegates/reflection so the dispatcher remains Burst/AOT compliant.

## Job Scheduling & Thread Policy
- Define default job worker count (`JobsUtility.JobWorkerCount`) and document when it changes (e.g., heavy AI scenes).
- Keep main thread workloads minimal; offload deterministic logic to jobs inside relevant system groups.
- Use `Allocator.TempJob` only inside short-lived jobs; prefer persistent NativeContainers with explicit disposal.
- Add optional instrumentation (job scheduling logs, worker utilisation) for diagnosing bottlenecks.

## Hot vs. Cold Execution Paths
- Identify hot systems (physics, environment, spatial, gameplay) vs. cold/background systems (history logging, analytics).
- Use separate system groups or toggles to throttle cold paths during heavy load.
- Document double-buffering, event queue patterns, and caching strategies in design notes.
- Environment channel contributions are stored in contiguous buffers (scalar/vector) so jobs can stream writes coalesced; maintain this layout when adding new effect types to avoid cache misses.

## Testing & Validation
- Maintain automated Burst/IL2CPP build tests in CI.
- Run deterministic replay/soak tests with Burst enabled.
- Track Burst/IL2CPP issues in `PlatformPerformance_TODO` (to be authored) and ensure fixes land before template releases.

## References
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`
- `Docs/QA/IL2CPP_AOT_Audit.md` - Detailed reflection usage audit and preservation requirements
- `Docs/TODO/SystemIntegration_TODO.md` (Platform/AOT tasks)
- `Docs/TODO/Utilities_TODO.md` (debug, testing, CI infrastructure)
- `Docs/QA/PerformanceProfiles.md` (profiling results once instrumentation lands)

Keep this document updated as Unity DOTS/Burst releases evolve and as new platform targets are added.

