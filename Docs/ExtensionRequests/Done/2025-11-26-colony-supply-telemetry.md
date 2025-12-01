# Extension Request: Colony Supply & Bottleneck Telemetry Surfaces

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Space4X  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

`Docs/TODO/4xdotsrequest.md` lists “Add colony supply/bottleneck metrics to the registry snapshot and telemetry stream” as a blocking integration task. Space4X tracks per-colony demand/supply via `Space4XColony`, `Space4XColonySupply`, and emits aggregated values inside `Space4XRegistrySnapshot`, but these metrics never reach PureDOTS’ shared dashboards or telemetry consumers. Both Space4X and Godgame rely on the shared `TelemetryStream`, so we need a first-party extension point that standardizes colony supply metrics (demand totals, shortages, bottleneck counts, severity flags) and publishes them through the same dashboard/hud pipeline PureDOTS already owns. Without it, each game has to inject bespoke metrics and HUD widgets, making cross-project comparisons impossible.

---

## Proposed Solution

**Extension Type**: New Registry Snapshot fields + Telemetry metrics

**Details:**
- Extend `Packages/com.moni.puredots/Runtime/Registry/ColonyRegistrySnapshot.cs` (or introduce it if absent) with additive fields:
  - `float TotalSupplyDemand`
  - `float TotalSupplyShortage`
  - `float AverageSupplyRatio`
  - `int BottleneckColonyCount`
  - `int CriticalColonyCount`
- Update `ColonyRegistrySystem` (and `DeterministicRegistryBuilder` wiring) so it aggregates the above values whenever colony registries rebuild. Provide helper math utilities (mirroring `Space4XColonySupply.ComputeDemand(...)`) so game teams don’t duplicate thresholds.
- Publish canonical telemetry keys via `RegistryTelemetrySystem`:
  - `registry.colonies.supply.demand`
  - `registry.colonies.supply.shortage`
  - `registry.colonies.supply.avgRatio`
  - `registry.colonies.supply.bottleneck`
  - `registry.colonies.supply.critical`
  - optionally `registry.colonies.flags.supplyStrained` etc.
- Document the thresholds (e.g., `BottleneckThreshold = 0.6`, `CriticalThreshold = 0.3`) and allow override via authoring asset or registry config so each game can tune but still publish comparable metrics.
- Update the debug HUD panel to display the new values alongside existing population/storage counters and highlight bottleneck/critical states.

---

## Impact Assessment

**Files/Systems Affected:**
- `Packages/com.moni.puredots/Runtime/Registry/ColonyRegistrySystem.cs`
- `Packages/com.moni.puredots/Runtime/Registry/ColonyRegistrySnapshot.cs`
- `Packages/com.moni.puredots/Runtime/Registry/RegistryTelemetrySystem.cs`
- HUD binding scripts that read colony metrics (`DebugDisplaySystem`, `TelemetryHUD`)
- Docs: `Docs/DesignNotes/RegistryDomainPlan.md`, `Docs/Guides/Telemetry.md`

**Breaking Changes:**
- Additive only. Existing telemetry consumers keep working; new metrics simply show up in buffers. `ColonyRegistrySnapshot` gains extra fields but defaults to zero.

---

## Example Usage

```csharp
// Inside Space4XRegistryBridgeSystem once the PureDOTS snapshot is available
var colonySnapshot = SystemAPI.GetSingletonRW<ColonyRegistrySnapshot>();
colonySnapshot.ValueRW.TotalSupplyDemand += demand;
colonySnapshot.ValueRW.TotalSupplyShortage += shortage;

if (supplyRatio < ColonySupplyThresholds.Bottleneck)
{
    colonySnapshot.ValueRW.BottleneckColonyCount++;
}

if (supplyRatio < ColonySupplyThresholds.Critical)
{
    colonySnapshot.ValueRW.CriticalColonyCount++;
}
```

Telemetry stream would emit `registry.colonies.supply.*` metrics automatically after `DebugDisplaySystem` runs, allowing dashboards to show the same bottleneck gauges Space4X currently renders locally (`space4x.registry.colonies.supply.*`).

---

## Alternative Approaches Considered

- **Game-specific telemetry keys**: Space4X already writes `space4x.registry.colonies.supply.*`, but Godgame cannot reuse those and shared HUD tooling can’t display them without special cases.
- **Mirroring data via custom presentation layers**: Would duplicate effort and bypass shared instrumentation, defeating the goal of a unified PureDOTS telemetry surface.

---

## Implementation Notes

- Provide extension hooks so games can override the base thresholds per project through a `ColonySupplyTelemetryConfig` ScriptableObject.
- Ensure the aggregation works in Burst (all math should stay `float`/`half`).
- Consider exposing a `ColonySupplyTelemetryBuffer` for advanced analytics (min/max ratio per colony, IDs of the worst offenders) while keeping the summarized values lightweight for HUD display.

---

## Review Notes

**Reviewer**:  
**Review Date**:  
**Decision**:  
**Notes**:
