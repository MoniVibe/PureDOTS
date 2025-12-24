# Module Combat System Requirements

## Overview

The module combat system enables Homeworld-style combat where ships have modular components (engines, weapons, shields) that can be individually targeted, damaged, and destroyed. When modules are destroyed, ship capabilities (movement, firing, etc.) are disabled.

## Component Requirements

### Ship Components (Carrier)

Ships participating in module combat must have:

- `CarrierModuleSlot` buffer (`PureDOTS.Runtime.Ships.CarrierModuleSlot`) - **REQUIRED**
  - Must use `PureDOTS.Runtime.Ships.CarrierModuleSlot` (has `InstalledModule` field)
  - `Space4X.Registry.CarrierModuleSlot` (has `CurrentModule`) is **INCOMPATIBLE**
- `HitEvent` buffer - Added automatically by `CarrierModuleBootstrapSystem`
- `VerticalEngagementRange` component - Added automatically by bootstrap
- `Advantage3D` component - Added automatically by bootstrap
- `CapabilityState` component - Added automatically by `CapabilityDisableSystem`
- `CapabilityEffectiveness` component - Added automatically by `CapabilityDisableSystem`

### Module Components

Each module entity must have:

- `ShipModule` component (`PureDOTS.Runtime.Ships.ShipModule`) - **REQUIRED**
  - Defines module class, family, mount requirements
- `ModuleHealth` component (`PureDOTS.Runtime.Ships.ModuleHealth`) - **REQUIRED**
  - Must use `PureDOTS.Runtime.Ships.ModuleHealth` (float `Health`/`MaxHealth` fields)
  - `PureDOTS.Runtime.Space.ModuleHealth` (byte `Integrity`) is **INCOMPATIBLE**
- `ModulePosition` component - **REQUIRED** for hit detection
  - Added automatically by baker or bootstrap system
- `ModuleTargetPriority` component - **REQUIRED** for targeting
  - Added automatically by baker or bootstrap system
- `CarrierOwner` component - Added automatically by bootstrap
- `ModuleOperationalState` component - Added automatically by bootstrap

## Namespace Requirements

### CarrierModuleSlot

**CRITICAL**: Module combat systems require `PureDOTS.Runtime.Ships.CarrierModuleSlot`.

**Structure:**
```csharp
public struct CarrierModuleSlot : IBufferElementData
{
    public MountType Type;
    public MountSize Size;
    public Entity InstalledModule;  // ← Combat systems look for this field
    public byte Priority;
}
```

**Incompatible Structure:**
```csharp
// Space4X.Registry.CarrierModuleSlot - DO NOT USE for combat
public struct CarrierModuleSlot : IBufferElementData
{
    public int SlotIndex;
    public ModuleSlotSize SlotSize;
    public Entity CurrentModule;  // ← Wrong field name
    public Entity TargetModule;
    public float RefitProgress;
    public ModuleSlotState State;
}
```

**Migration**: Space4X systems using `Space4X.Registry.CarrierModuleSlot` must migrate to `PureDOTS.Runtime.Ships.CarrierModuleSlot` for combat compatibility.

### ModuleHealth

**CRITICAL**: Module combat systems require `PureDOTS.Runtime.Ships.ModuleHealth`.

**Structure:**
```csharp
public struct ModuleHealth : IComponentData
{
    public float MaxHealth;      // ← Float field
    public float Health;          // ← Float field
    public float DegradationPerTick;
    public float FailureThreshold;
    public ModuleHealthState State;
    public ModuleHealthFlags Flags;
    public uint LastProcessedTick;
}
```

**Incompatible Structure:**
```csharp
// PureDOTS.Runtime.Space.ModuleHealth - DO NOT USE for combat
public struct ModuleHealth : IComponentData
{
    public byte Integrity;        // ← Byte field - WRONG
    public byte FailureThreshold;
    public byte RepairPriority;
    public byte Flags;
}
```

## System Ordering

Module combat systems must run in this order:

1. `HitDetectionSystem` / `ProjectileDamageSystem` → Creates `HitEvent`
2. `DamageResolutionSystem` → Converts `HitEvent` to `DamageEvent`
3. `ModuleDamageRouterSystem` → Routes damage to modules (`[UpdateAfter(typeof(DamageResolutionSystem))]`)
4. `CapabilityDisableSystem` → Updates capabilities (`[UpdateAfter(typeof(ModuleDamageRouterSystem))]`)
5. Movement/Firing systems → Check capabilities (run after `CapabilityDisableSystem`)

## Capability Mapping

Modules map to capabilities as follows:

- `ModuleClass.Engine` → `CapabilityType.Movement`
- `ModuleClass.BeamCannon` / `MassDriver` / `Missile` / `PointDefense` → `CapabilityType.Firing`
- `ModuleClass.Shield` → `CapabilityType.Shields`
- `ModuleClass.Sensor` → `CapabilityType.Sensors`

When all modules of a type are destroyed, the corresponding capability is disabled.

## Module Target Priority

Default priorities (higher = targeted first):

- `Engine` = 200 (critical - disables movement)
- `BeamCannon` / `MassDriver` / `Missile` = 150 (weapons)
- `PointDefense` = 140
- `Shield` = 120
- `Armor` = 100
- `Sensor` = 80
- `Hangar` = 60
- `Cargo` = 50
- `Fabrication` / `Research` = 40
- `Medical` = 30
- Others = 20

## Troubleshooting

### Combat Systems Don't Find Modules

**Symptom**: Module targeting/damage routing doesn't work.

**Causes**:
1. Ship uses `Space4X.Registry.CarrierModuleSlot` instead of `PureDOTS.Runtime.Ships.CarrierModuleSlot`
2. Module entities missing `ShipModule` component
3. Module entities missing `ModuleHealth` component

**Solution**:
- Check `ModuleCombatValidationSystem` logs (development builds only)
- Ensure ships use `PureDOTS.Runtime.Ships.CarrierModuleSlot`
- Verify modules have required components

### Module Health Not Updating

**Symptom**: Damage doesn't reduce module health.

**Causes**:
1. Using wrong `ModuleHealth` structure (`PureDOTS.Runtime.Space.ModuleHealth` instead of `PureDOTS.Runtime.Ships.ModuleHealth`)
2. `HitEvent` buffer missing on ship
3. `ModuleDamageRouterSystem` not running

**Solution**:
- Check `ModuleCombatValidationSystem` logs
- Verify `ModuleHealth` uses float `Health`/`MaxHealth` fields
- Ensure `HitEvent` buffer exists (added by bootstrap)

### Capabilities Not Disabling

**Symptom**: Ship still moves/fires after modules destroyed.

**Causes**:
1. `CapabilityDisableSystem` not running
2. Movement/firing systems not checking `CapabilityState`
3. `CapabilityState` component missing

**Solution**:
- Verify system ordering (`CapabilityDisableSystem` runs after `ModuleDamageRouterSystem`)
- Check movement/firing systems check `CapabilityState` before allowing actions
- Ensure `CapabilityDisableSystem` initializes `CapabilityState` component

### Module Targeting Always Returns Null

**Symptom**: `ModuleTargetingService.SelectModuleTarget()` always returns `Entity.Null`.

**Causes**:
1. Ship has no modules
2. All modules destroyed
3. Ship uses wrong `CarrierModuleSlot` namespace

**Solution**:
- Verify ship has `CarrierModuleSlot` buffer with `InstalledModule` entities
- Check modules have `ModuleHealth` with `Health > 0`
- Ensure using `PureDOTS.Runtime.Ships.CarrierModuleSlot`

## Validation

`ModuleCombatValidationSystem` (development builds only) validates:
- Module entities have required components
- `ModuleHealth` structure is correct (float fields)
- `ModulePosition` and `ModuleTargetPriority` exist
- Module health values are valid (> 0)

Check Unity console for validation warnings/errors.

## References

- `puredots/Docs/Audit/ModuleCombat_CarrierModuleSlot_Audit.md` - CarrierModuleSlot namespace audit
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/ModuleTargetingService.cs` - Targeting logic
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/ModuleDamageRouterService.cs` - Damage routing
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/CapabilityDisableService.cs` - Capability management

