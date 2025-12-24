# Module Combat Testing Checklist

## Pre-Testing Verification

Before running tests, verify:

- [ ] All files compile without errors
- [ ] `ModuleCombatScenarioBootstrapSystem` exists and handles `scenario.space4x.module.combat.smoke`
- [ ] Unit tests (`ModuleCombatServiceTests`) compile
- [ ] Validation system (`ModuleCombatValidationSystem`) compiles (development builds only)
- [ ] Smoke test JSON (`space4x_module_combat_smoke.json`) exists in Samples folder

## Unit Tests

### Running Unit Tests

Run the NUnit test suite:
```bash
# Unity Test Runner (EditMode)
# Or via CI: CI/run_playmode_tests.sh (editmode)
```

### Expected Test Results

All tests in `ModuleCombatServiceTests` should pass:

1. **ModuleTargetingService_SelectModuleTarget_SelectsHighestPriorityModule**
   - Verifies targeting selects highest priority module (Engine = 200 > BeamCannon = 150 > Cargo = 50)

2. **ModuleTargetingService_SelectModuleTarget_IgnoresDestroyedModules**
   - Verifies destroyed modules are not selected (destroyed Engine → selects BeamCannon)

3. **ModuleDamageRouterService_RouteDamageToModule_ReducesModuleHealth**
   - Verifies damage reduces module health correctly

4. **ModuleDamageRouterService_RouteDamageToModule_MarksModuleAsDestroyedWhenHealthZero**
   - Verifies modules are marked destroyed when health reaches 0

5. **CapabilityDisableService_UpdateCapabilitiesFromModules_DisablesMovementWhenEnginesDestroyed**
   - Verifies Movement capability disabled when all engines destroyed

## Smoke Test Scenario

### Running Smoke Test

**Headless:**
```bash
# Via ScenarioRunnerEntryPoints
Unity -batchmode -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioFromArgs --scenario "puredots/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/space4x_module_combat_smoke.json"
```

**Editor:**
- Use PureDOTS menu: `PureDOTS/Run Scenario` → Select `space4x_module_combat_smoke.json`

### Expected Behavior

1. **Entity Spawning (T=0)**
   - Two carriers spawn:
     - `carrier.attacker` at position (0, 0, 0)
     - `carrier.target` at position (50, 0, 0)
   - Each carrier has:
     - 1 BeamCannon module (weapon)
     - 1 Engine module (movement)
   - All modules have `ModuleHealth`, `ModulePosition`, `ModuleTargetPriority` components

2. **Combat Engagement (T=10+)**
   - Carriers engage in combat (handled by game-specific systems)
   - Module targeting selects modules based on priority
   - Damage routes to modules (not just hull)

3. **Module Destruction Effects**
   - Destroying engine module → Movement capability disabled
   - Destroying weapon module → Firing capability disabled
   - Destroyed modules marked with `ModuleHealthState.Destroyed` and `ModuleState.Destroyed`

### Verification Steps

1. **Check Entity Spawning:**
   - Verify two carriers exist in world
   - Verify each carrier has `CarrierModuleSlot` buffer with 2 modules
   - Verify modules have required components (`ShipModule`, `ModuleHealth`, `ModulePosition`, `ModuleTargetPriority`)

2. **Check Combat Systems:**
   - Verify `ModuleTargetingSystem` selects modules correctly
   - Verify `ModuleDamageRouterSystem` routes damage to modules
   - Verify `CapabilityDisableSystem` updates capabilities when modules destroyed

3. **Check Capability Disable:**
   - Manually destroy engine module → Verify `CapabilityState.EnabledCapabilities` no longer includes `Movement`
   - Manually destroy weapon module → Verify `CapabilityState.EnabledCapabilities` no longer includes `Firing`

## Manual Testing (Editor)

### Setup

1. Open Space4X scene with module combat systems enabled
2. Create test carriers with modules via `CarrierModuleLoadoutAuthoring` or bootstrap system
3. Enable visualization/debugging tools

### Test Cases

#### Test 1: Module Targeting
- **Setup**: Ship with Engine (priority 200), BeamCannon (150), Cargo (50)
- **Action**: Initiate combat targeting
- **Expected**: Engine module selected (highest priority)

#### Test 2: Module Damage Routing
- **Setup**: Ship with modules at full health
- **Action**: Apply damage to ship
- **Expected**: Damage routes to targeted module, module health decreases

#### Test 3: Module Destruction
- **Setup**: Ship with Engine module at 10 HP
- **Action**: Apply 20 damage to engine module
- **Expected**: 
  - Engine module health = 0
  - `ModuleHealth.State` = `Destroyed`
  - `ShipModule.State` = `Destroyed`
  - Movement capability disabled

#### Test 4: Capability Disable
- **Setup**: Ship with Engine module destroyed
- **Action**: Attempt to move ship
- **Expected**: Movement system checks `CapabilityState`, movement blocked

#### Test 5: 3D Formation
- **Setup**: Two ships at different vertical positions
- **Action**: Calculate 3D advantage
- **Expected**: Higher ship gets advantage multiplier

## Validation System Testing

### Development Build Only

The `ModuleCombatValidationSystem` only runs in development builds (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`).

### Test Missing Components

1. **Create intentionally broken ship:**
   - Ship with `CarrierModuleSlot` buffer
   - Module entity missing `ModuleHealth` component

2. **Run validation:**
   - Check Unity console for error: `"[ModuleCombatValidation] Module X missing ModuleHealth component"`

3. **Verify bootstrap fixes:**
   - `CarrierModuleBootstrapSystem` should add missing components
   - Re-run validation → No errors

### Expected Validation Warnings

- **Missing ShipModule**: Error (combat systems will fail)
- **Missing ModuleHealth**: Error (damage routing will fail)
- **Missing ModulePosition**: Warning (hit detection may be inaccurate)
- **Missing ModuleTargetPriority**: Warning (targeting uses defaults)
- **Invalid MaxHealth**: Warning (MaxHealth <= 0)

## Common Issues and Solutions

### Issue: Module Targeting Returns Null

**Symptoms:**
- `ModuleTargetingService.SelectModuleTarget()` always returns `Entity.Null`

**Causes:**
- Ship uses wrong `CarrierModuleSlot` namespace (`Space4X.Registry.CarrierModuleSlot` instead of `PureDOTS.Runtime.Ships.CarrierModuleSlot`)
- All modules destroyed
- Ship has no modules

**Solution:**
- Check validation system logs
- Verify ship uses `PureDOTS.Runtime.Ships.CarrierModuleSlot` with `InstalledModule` field
- Ensure modules have `ModuleHealth.Health > 0`

### Issue: Capabilities Not Disabling

**Symptoms:**
- Ship still moves/fires after modules destroyed

**Causes:**
- `CapabilityDisableSystem` not running
- Movement/firing systems not checking `CapabilityState`
- `CapabilityState` component missing

**Solution:**
- Verify system ordering (`CapabilityDisableSystem` runs after `ModuleDamageRouterSystem`)
- Check movement/firing systems check `CapabilityState` before allowing actions
- Ensure `CapabilityDisableSystem` initializes `CapabilityState` component

### Issue: Module Health Not Updating

**Symptoms:**
- Damage doesn't reduce module health

**Causes:**
- Using wrong `ModuleHealth` structure (`PureDOTS.Runtime.Space.ModuleHealth` instead of `PureDOTS.Runtime.Ships.ModuleHealth`)
- `HitEvent` buffer missing on ship
- `ModuleDamageRouterSystem` not running

**Solution:**
- Check validation system logs
- Verify `ModuleHealth` uses float `Health`/`MaxHealth` fields (not byte `Integrity`)
- Ensure `HitEvent` buffer exists (added by bootstrap)

### Issue: Smoke Test Doesn't Spawn Entities

**Symptoms:**
- Running smoke test produces no entities

**Causes:**
- Wrong scenario ID in bootstrap system
- Bootstrap system not enabled
- Scenario runner not executing bootstrap systems

**Solution:**
- Verify scenario JSON has `"scenarioId": "scenario.space4x.module.combat.smoke"`
- Check `ModuleCombatScenarioBootstrapSystem` is enabled
- Verify bootstrap system checks for correct scenario ID

## Performance Testing

### Expected Performance

- **Module Targeting**: < 0.1ms per ship (with 10 modules)
- **Damage Routing**: < 0.05ms per damage event
- **Capability Update**: < 0.1ms per ship (with 10 modules)
- **Validation System**: < 1ms total (development builds only)

### Performance Testing Steps

1. Create 100 ships with 10 modules each
2. Run combat systems for 1000 ticks
3. Profile with Unity Profiler
4. Verify systems stay within budgets

## Integration Testing

### With Space4X Combat Systems

1. **Verify Integration:**
   - `Space4XCombatSystem` uses `ModuleTargetingService`
   - `Space4XWeaponSystem` checks `CapabilityState` before firing
   - `VesselMovementSystem` checks `CapabilityState` before moving

2. **Test Full Combat Loop:**
   - Ships engage in combat
   - Modules targeted and damaged
   - Capabilities disabled when modules destroyed
   - Movement/firing systems respect disabled capabilities

### With Other Game Systems

Module combat systems are game-agnostic and should work with any game that:
- Uses `PureDOTS.Runtime.Ships.CarrierModuleSlot`
- Uses `PureDOTS.Runtime.Ships.ModuleHealth` (float-based)
- Implements movement/firing systems that check `CapabilityState`

## Success Criteria

All tests pass when:

- ✅ Unit tests pass (5/5 tests)
- ✅ Smoke test spawns entities correctly
- ✅ Module targeting works (selects highest priority)
- ✅ Damage routing works (damage reduces module health)
- ✅ Capability disable works (destroyed modules disable capabilities)
- ✅ Validation system catches configuration errors (development builds)
- ✅ No compilation errors
- ✅ Performance within budgets

## References

- `puredots/Docs/Combat/ModuleCombat_Requirements.md` - Component requirements and troubleshooting
- `puredots/Packages/com.moni.puredots/Runtime/Tests/ModuleCombatServiceTests.cs` - Unit test source
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/space4x_module_combat_smoke.json` - Smoke test scenario

