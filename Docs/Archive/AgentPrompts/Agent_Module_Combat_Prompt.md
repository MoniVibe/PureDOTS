# Agent: Module Combat (Space4X) - UPDATED

## Status: 🟡 PARTIAL IMPLEMENTATION

**Completed**:
- ✅ Module Targeting (Service, System) - IMPLEMENTED
- ✅ Module Damage Router (Service) - IMPLEMENTED

**Remaining Work**:
- ❌ Module Targeting Components (1 file) - Verify if exists
- ❌ Module Damage Router Components/Systems (2 files) - Verify if exists
- ❌ Capability Disable (3 files) - Still stubbed
- ❌ 3D Formation (3 files) - Still stubbed
- ❌ Combat State Extensions (1 file) - Still stubbed

## Remaining Stub Files to Implement

### Capability Disable (3 files)
- `Runtime/Stubs/CapabilityDisableStub.cs` → `Runtime/Combat/CapabilityDisableService.cs`
- `Runtime/Stubs/CapabilityDisableStubComponents.cs` → `Runtime/Combat/CapabilityDisableComponents.cs`
- `Runtime/Stubs/CapabilityDisableStubSystems.cs` → `Systems/Combat/CapabilityDisableSystem.cs`

**Requirements:**
- Map modules to capabilities: Engine → Movement, Weapon → Firing, Shield → Shields, etc.
- Disable capabilities when modules destroyed
- Partial capability: damaged modules reduce capability effectiveness
- Capability recovery: repair module → restore capability

**Note**: `CapabilityState` component already exists in Space4X - verify integration

### 3D Formation (3 files)
- `Runtime/Stubs/Formation3DStub.cs` → `Runtime/Combat/Formation3DService.cs`
- `Runtime/Stubs/Formation3DStubComponents.cs` → `Runtime/Combat/Formation3DComponents.cs`
- `Runtime/Stubs/Formation3DStubSystems.cs` → `Systems/Combat/Formation3DSystem.cs`

**Requirements:**
- 3D combat positioning: ships above/below leader
- Vertical engagement range separate from horizontal
- 3D advantage calculations: high ground bonus, flanking from below
- Vertical movement mechanics: ascend, descend, dive, climb

### Combat State Extensions (1 file)
- `Runtime/Stubs/CombatStateModuleStub.cs` → Extend `Runtime/Combat/State/CombatStateComponents.cs`

**Requirements:**
- Add module operational states to existing `ModuleState` enum:
  - `ModuleDestroyed` (100)
  - `ModuleDamaged` (101)
  - `ModuleOffline` (102)
  - `ModuleRepairing` (103)

## Reference Documentation
- `Docs/Audit/Combat_System_Audit.md` - Section 2.1-2.4 (Space4X Vision)
- `Runtime/Combat/ModuleTargetingService.cs` - Existing implementation reference
- `Runtime/Combat/ModuleDamageRouterService.cs` - Existing implementation reference
- `Runtime/Runtime/Space/ModuleComponents.cs` - Existing module components

## Implementation Notes
- Use existing `ModuleTargetingService` as pattern
- `CapabilityState` component already exists - integrate with it
- 3D positioning uses existing `LocalTransform` (already 3D)
- Module positions can be stubbed (canned offsets) until geometric hit detection ready

## Dependencies
- `ShipModule` component
- `ModuleHealth` component
- `ModuleState` enum
- `CapabilityState` component (already exists)
- `HitEvent` buffer
- `LocalTransform` for 3D positioning
