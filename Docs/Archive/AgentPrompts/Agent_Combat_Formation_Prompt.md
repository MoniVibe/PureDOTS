# Agent: Combat Formation & Tactics (Godgame) - UPDATED

## Status: 🟡 PARTIAL IMPLEMENTATION

**Completed**:
- ✅ Formation Combat (Service, Components, Systems) - IMPLEMENTED
- ✅ Morale Wave (Service, Components, Systems) - IMPLEMENTED

**Remaining Work**:
- ❌ Cohesion Effects (3 files) - Still stubbed
- ❌ Formation Tactics (3 files) - Still stubbed  
- ❌ Combat State Extensions (1 file) - Still stubbed

## Remaining Stub Files to Implement

### Cohesion Effects (3 files)
- `Runtime/Stubs/CohesionEffectStub.cs` → `Runtime/Combat/CohesionEffectService.cs`
- `Runtime/Stubs/CohesionEffectStubComponents.cs` → `Runtime/Combat/CohesionEffectComponents.cs`
- `Runtime/Stubs/CohesionEffectStubSystems.cs` → `Systems/Combat/CohesionEffectSystem.cs`

**Requirements:**
- Squad cohesion affects combat effectiveness (accuracy, damage, defense)
- Cohesion thresholds: Broken (< 0.3), Fragmented (0.3-0.6), Cohesive (0.6-0.8), Elite (> 0.8)
- Cohesion degrades under fire, recovers over time
- Apply cohesion multipliers to combat stats

### Formation Tactics (3 files)
- `Runtime/Stubs/FormationTacticStub.cs` → `Runtime/Combat/FormationTacticService.cs`
- `Runtime/Stubs/FormationTacticStubComponents.cs` → `Runtime/Combat/FormationTacticComponents.cs`
- `Runtime/Stubs/FormationTacticStubSystems.cs` → `Systems/Combat/FormationTacticSystem.cs`

**Requirements:**
- Tactics: Charge, Hold, Flank, Encircle, Feint, Retreat
- Tactic states: Idle, Preparing, Executing, Completing, Failed
- Execute movement patterns based on tactic type
- Tactic effectiveness vs different formation types

### Combat State Extensions (1 file)
- `Runtime/Stubs/CombatStateFormationStub.cs` → Extend `Runtime/Combat/State/CombatStateComponents.cs`

**Requirements:**
- Add formation combat states to existing `CombatState` enum:
  - `FormationEngaged` (100)
  - `FormationBroken` (101)
  - `FormationRouted` (102)
  - `FormationReforming` (103)

## Reference Documentation
- `Docs/Audit/Combat_System_Audit.md` - Section 1.1-1.4 (Godgame Vision)
- `Runtime/Combat/FormationCombatService.cs` - Existing implementation reference
- `Runtime/Combat/MoraleWaveService.cs` - Existing implementation reference

## Implementation Notes
- Use existing `FormationCombatService` and `MoraleWaveService` as patterns
- Integrate with `CombatStats` and `CombatState` systems
- Cohesion updates should be throttled (not every tick)
- Formation tactics should integrate with existing formation system

## Dependencies
- `CombatStats` component
- `CombatState` enum
- `FormationCombatComponents` - Already implemented
- `MoraleWaveComponents` - Already implemented
