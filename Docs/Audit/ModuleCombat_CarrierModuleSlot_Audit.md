# CarrierModuleSlot Namespace Audit

## Summary

Two different `CarrierModuleSlot` definitions exist in the codebase:
1. **PureDOTS.Runtime.Ships.CarrierModuleSlot** - Used by combat systems
   - Fields: `MountType Type`, `MountSize Size`, `Entity InstalledModule`, `byte Priority`
2. **Space4X.Registry.CarrierModuleSlot** - Used by Space4X refit/repair systems
   - Fields: `int SlotIndex`, `ModuleSlotSize SlotSize`, `Entity CurrentModule`, `Entity TargetModule`, `float RefitProgress`, `ModuleSlotState State`

## Files Using PureDOTS.Runtime.Ships.CarrierModuleSlot

- `puredots/Packages/com.moni.puredots/Runtime/Authoring/Space/CarrierModuleLoadoutAuthoring.cs` ✓
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Ships/CarrierModuleBootstrapSystem.cs` ✓
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/ModuleTargetingService.cs` ✓
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/ModuleDamageRouterService.cs` ✓
- `space4x/Assets/Scripts/Space4x/Registry/Space4XCombatSystem.cs` ✓ (uses `using PureDOTS.Runtime.Ships;`)

## Files Using Space4X.Registry.CarrierModuleSlot

- `space4x/Assets/Scripts/Space4x/Authoring/Space4XCarrierModuleSlotsAuthoring.cs` ✗
- `space4x/Assets/Scripts/Space4x/Registry/Space4XFieldRepairSystem.cs` ✗
- `space4x/Assets/Scripts/Space4x/Registry/Space4XCarrierModuleRefitSystem.cs` ✗
- `space4x/Assets/Scripts/Space4x/Scenario/Space4XRefitScenarioSystem.cs` ✗
- `space4x/Assets/Scripts/Space4x/Scenario/Space4XRefitScenarioActionProcessor.cs` ✗
- `space4x/Assets/Scripts/Space4x/Registry/Space4XModuleRatingAggregationSystem.cs` ✗
- `space4x/Assets/Scripts/Space4x/Registry/Space4XModuleStatAggregationSystem.cs` ✗
- `space4x/Assets/Scripts/Space4x/Registry/Space4XStationOverhaulSystem.cs` ✗
- `space4x/Assets/Scripts/Space4x/Tests/Space4XModuleSystemsTests.cs` ✗

## Impact

**Combat systems will fail** if ships are created using `Space4XCarrierModuleSlotsAuthoring` because:
- Combat systems look for `InstalledModule` field
- Space4X version has `CurrentModule` field instead
- Module targeting/damage routing won't find modules

## Resolution Strategy

**Option A: Migrate Space4X to PureDOTS version (Recommended)**
- Pros: Single source of truth, combat systems work immediately
- Cons: Requires updating all Space4X systems, may break existing refit logic

**Option B: Create adapter system**
- Pros: No breaking changes to existing Space4X systems
- Cons: Technical debt, maintenance overhead, potential performance impact

**Option C: Runtime validation + warnings**
- Pros: Non-breaking, detects issues early
- Cons: Doesn't fix the problem, just warns about it

**Recommended:** Option A - Migrate Space4X systems to use PureDOTS version for consistency and combat compatibility.

