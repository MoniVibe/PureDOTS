# Extension Request: Continuity Validation Toolkit for Custom Registries

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Space4X  
**Priority**: P0  
**Assigned To**: TBD

---

## Use Case

`Docs/TODO/4xdotsrequest.md` tracks an open Space4X item to “Align time/continuity (TimeState, rewind, continuity validation) with PureDOTS expectations; ensure deterministic fleet/colony updates.”  
Space4X builds several custom registries (`Space4XColonyRegistry`, `Space4XFleetRegistry`, `Space4XLogisticsRegistry`, `Space4XAnomalyRegistry`, `Space4XMiracleRegistry`) on top of `com.moni.puredots`. These registries are produced via `Space4XRegistryBridgeSystem` using `DeterministicRegistryBuilder<T>`, but today only first-party registries are inspected by `RegistryContinuityValidationSystem`. That means Space4X cannot enroll its registries in the same validation harness PureDOTS uses to guarantee deterministic ordering, rewind safety, and spatial version alignment. We need a sanctioned API to register custom registries with the continuity validator and to consume detailed reports so our edit-mode and batchmode test suites can gate on those results.

Without this toolkit we rely on bespoke assertions in `Space4XRegistryContinuityTests` and `Space4XRegistryBridgeSystemTests`, which quickly drift away from the canonical continuity contract defined in `Docs/DesignNotes/RegistryContinuityContracts.md`. We also lack access to the instrumentation that reports why `RegistryContinuitySnapshot` values fail, making deterministic rewind verification for fleets/colonies fragile.

---

## Proposed Solution

**Extension Type**: New System / API hooks

**Details:**
- Introduce a public `RegistryContinuityApi` (under `PureDOTS.Runtime.Registry`) that lets gameplay packages register `RegistryContinuityParticipant` structs containing the owning entity, registry label, spatial requirements, and the latest `RegistryContinuitySnapshot`.
- Extend `RegistryContinuityValidationSystem` so it consumes both built-in registries and registered participants. The system should:
  - Assert deterministic buffer ordering per rebuild.
  - Compare participant `SpatialVersion` values with `RegistrySpatialSyncState` when `SupportsSpatialQueries` is set.
  - Validate rewind hand-offs by ensuring registries skip writes when `RewindState.Mode != Record` and that buffers/metadata match once recording resumes.
  - Emit a `ContinuityValidationReport` buffer on each participant so downstream systems/tests can react without scraping logs.
- Provide an edit-mode helper `ContinuityValidationRunner.Validate(World world, NativeList<Entity> participants)` that exposes the same checks to NUnit/PlayMode tests (`Space4XRegistryContinuityTests` wants to run it after spawning colonies/fleets).
- Allow `DeterministicRegistryBuilder<T>.ApplyTo(...)` to optionally accept a `RegistryContinuityParticipantHandle` so registries can publish continuity metadata without duplicating boilerplate.
- Surface a lightweight `ContinuityValidationSettings` ScriptableObject authoring hook so scenes can opt-in to fail-fast behaviour before entering Play Mode.

```csharp
// Example registration inside Space4XRegistryBridgeSystem
var participant = RegistryContinuityApi.RegisterCustomRegistry(
    entity,
    new RegistryContinuityRegistration
    {
        Label = "Space4X.Colonies",
        RequiresSpatialSync = true,
        SupportsRewind = true,
        Snapshot = snapshot.Continuity,
        LastUpdateTick = tick
    });

RegistryContinuityApi.ReportUpdate(participant, resolvedCount, fallbackCount, unmappedCount);
```

---

## Impact Assessment

**Files/Systems Affected:**
- `Packages/com.moni.puredots/Runtime/Registry/RegistryContinuityValidationSystem.cs` – discover and validate custom participants.
- `Packages/com.moni.puredots/Runtime/Registry/DeterministicRegistryBuilder.cs` – plumb handles/snapshots.
- New helper `Packages/com.moni.puredots/Runtime/Registry/RegistryContinuityApi.cs`.
- `Packages/com.moni.puredots/Runtime/Registry/RegistryMetadata.cs` – expose participant handles and reports.
- Documentation updates in `Docs/DesignNotes/RegistryContinuityContracts.md` and README.
- Test suites (`RegistryContinuityTests`, `RegistrySpatialContinuityPlaymodeTests`) extended to exercise custom participant registration.

**Breaking Changes:**
- None; existing registries remain auto-discovered. The API is additive and only runs for participants that opt in.

---

## Example Usage

```csharp
// In Space4XRegistryBridgeSystemTests
var participant = RegistryContinuityApi.RegisterCustomRegistry(colonyRegistryEntity, registerParams);

// Advance TimeState / RewindState as usual...

// Invoke shared validation
ContinuityValidationRunner.Validate(world, participant);

// Assert report contents
var report = SystemAPI.GetBuffer<ContinuityValidationReport>(participant.ReportEntity);
Assert.AreEqual(0, report.First().Errors);
```

This keeps Space4X deterministic validation aligned with PureDOTS internals and surfaces the same error details the engine team sees in CI.

---

## Alternative Approaches Considered

- **Local assertions in Space4X tests**: Already in place but diverge whenever PureDOTS adjusts the contract (e.g., new spatial counters, metadata fields). This leads to false positives/negatives and duplicated maintenance.
- **Copy `RegistryContinuityValidationSystem` into Space4X**: Would force the game team to fork engine code, instantly stale once upstream evolves.

---

## Implementation Notes

- Ensure the new API is `Burst`-safe; validation can stay `[DisableBurst]` like today, but registration/reporting should avoid managed allocations so gameplay systems can call them during `OnUpdate`.
- Participants should be able to unregister automatically when their entity is destroyed to avoid dangling references during SubScene unloads.
- Consider gating expensive validation via scripting define or developer build flag so production builds just store snapshots without running diagnostics.

---

## Review Notes

**Reviewer**:  
**Review Date**:  
**Decision**:  
**Notes**:
