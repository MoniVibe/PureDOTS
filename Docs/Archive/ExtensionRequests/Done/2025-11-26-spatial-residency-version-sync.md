# Extension Request: Spatial Residency Version Sync

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Godgame  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Godgame’s registry bridge emits `RegistryContinuitySnapshot` data using `SpatialGridResidency.CellId` and `Version` so spatial-aware registries can surface continuity/resim metadata. After the spatial grid rebuilds, `SpatialGridResidency.Version` remains zero because the grid build pipeline only increments `SpatialGridState.Version`. Registries therefore fall back to non-spatial continuity and cannot validate rewind continuity for mining/haul/storehouse registries or related telemetry.

---

## Proposed Solution

**Extension Type**: New system / behavior change

**Details:**
- After a spatial rebuild, stamp each `SpatialGridResidency.Version` (and any other residency metadata) with the current `SpatialGridState.Version`.
- Consider exposing a helper on `RegistrySpatialSyncState` or a lightweight `SpatialResidencyVersionSystem` that runs after grid rebuilds and before registry sync systems.
- Keep Burst-friendly and allocation-free; operate on cached queries inside the spatial systems package.

---

## Impact Assessment

**Files/Systems Affected:**
- `Packages/com.moni.puredots/Runtime/Spatial/SpatialGridSystems.cs` (or equivalent residency update system)
- `Packages/com.moni.puredots/Runtime/Registry/RegistrySpatialSyncSystem.cs` (verify version propagation to registries)

**Breaking Changes:**
- No breaking changes expected; versions become populated instead of remaining zero.

---

## Example Usage

```csharp
// In Godgame registry bridge
var residency = SystemAPI.GetComponentRO<SpatialGridResidency>(entity).ValueRO;
var snapshot = RegistryContinuitySnapshot.WithSpatial(residency.CellId, residency.Version);
// Without the fix, Version stays 0 and the snapshot falls back to non-spatial continuity.
```

---

## Alternative Approaches Considered

- **Godgame-local residency bump system**
  - **Rejected:** duplicates PureDOTS spatial behavior and risks divergence across games.
- **Ignore spatial continuity and rely on non-spatial snapshots**
  - **Rejected:** loses determinism guarantees for rewind/resim and breaks spatial registry diagnostics.

---

## Implementation Notes

- Ensure the version stamping runs after any residency rebuild/move pass and before registry sync/telemetry systems.
- Add a regression test that rebuilds the grid, queries a resident entity’s `SpatialGridResidency.Version`, and asserts it matches `SpatialGridState.Version` for deterministic continuity snapshots.
