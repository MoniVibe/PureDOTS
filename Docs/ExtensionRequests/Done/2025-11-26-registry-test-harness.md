# Extension Request: Registry Mock/Test Harness Utilities

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Space4X  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

`Docs/TODO/4xdotsrequest.md` ends with “Add focused integration tests for fleet spawning, trade route updates, miracle execution, colony supply; provide mock registry utilities.”  
Space4X is expanding its test coverage (`Space4XRegistryBridgeSystemTests`, `Space4XRegistryContinuityTests`, `Space4XColonySupplyTests`), but every test currently has to bootstrap PureDOTS singletons manually: create a `World`, invoke `CoreSingletonBootstrapSystem`, fake `TimeState/RewindState`, create registry entities, and seed telemetry buffers. This boilerplate is brittle (it must mirror PureDOTS internal bootstrap order) and makes it hard to author narrow tests for registries, trade routes, miracles, etc. We need official mock utilities from PureDOTS that spin up deterministic registry worlds with minimal code so gameplay tests can focus on assertions rather than setup.

---

## Proposed Solution

**Extension Type**: Test utility package / helper APIs

**Details:**
- Add a new assembly `PureDOTS.Runtime.Tests` (or `PureDOTS.TestUtilities`) containing helpers for DOTS edit/playmode tests.
- Provide factory methods such as:
  - `RegistryTestWorld CreateRegistryTestWorld(RegistryTestConfig config)` – boots `CoreSingletonBootstrapSystem`, `TimeState`, `RewindState`, `TelemetryStream`, and returns strongly-typed handles for frequently used systems.
  - `Entity CreateColony(World world, in ColonyTestParams parameters)` – spawns an entity with the components required for registry population (includes `SpatialIndexedTag`, `LocalTransform`, etc.).
  - `Entity CreateFleet(...)`, `Entity CreateLogisticsRoute(...)`, `Entity CreateMiracle(...)` helpers mirroring the shared schemas.
- Ship mock registries (`MockRegistryDirectory`, `MockTelemetryStream`) that behave like the real ones but run entirely in-memory so tests can assert against `DynamicBuffer<TRegistryEntry>` without needing Scene conversion.
- Include disposal helpers and deterministic random seed injection so tests remain leak-free and reproducible.
- Document usage in `Docs/Guides/Testing.md` and provide sample NUnit fixtures demonstrating colony, fleet, miracle, and trade route coverage.

---

## Impact Assessment

**Files/Systems Affected:**
- New project under `Packages/com.moni.puredots/Tests/TestUtilities/` (assembly definition + source)
- Potential updates to `CoreSingletonBootstrapSystem` or bootstrap helpers to expose hooks needed by the test harness
- `Docs/Guides/Testing.md`
- CI configuration to include the new utility assembly (even if excluded from runtime builds)

**Breaking Changes:**
- None; utilities live in a test-only assembly and are referenced by game tests via asmdef dependencies.

---

## Example Usage

```csharp
[Test]
public void FleetRegistry_Rebuilds_WhenFleetMoves()
{
    using var harness = RegistryTestWorld.CreateDefault();
    var fleet = harness.CreateFleet(new FleetTestParams { ShipCount = 6, Posture = Space4XFleetPosture.Patrol });

    harness.Tick(); // populates registry

    var registry = harness.GetRegistryBuffer<FleetRegistryEntry>();
    Assert.AreEqual(1, registry.Length);
    Assert.AreEqual(6, registry[0].ShipCount);
}
```

Space4X tests would simply depend on `PureDOTS.TestUtilities` rather than duplicating bootstrap logic in every fixture.

---

## Alternative Approaches Considered

- **Copy/paste bootstrap code per test**: already in use but increases maintenance load and has caused flaky tests whenever PureDOTS changes singleton setups.
- **Scene-based tests**: standing up SubScenes for each test dramatically slows edit-mode execution and still doesn’t expose low-level registry buffers.

---

## Implementation Notes

- Provide overloads for custom registry components so projects can inject their own component types (e.g., Space4X-specific buffers) while still reusing the base harness.
- Consider offering both synchronous (`harness.Tick()`) and asynchronous (`harness.StepSystems<TGroup>()`) helpers so tests can drive just the systems they care about.
- Utilities should guard against missing Burst/Systems packages in editor-only builds to keep them lightweight for CI usage.

---

## Review Notes

**Reviewer**:  
**Review Date**:  
**Decision**:  
**Notes**:
