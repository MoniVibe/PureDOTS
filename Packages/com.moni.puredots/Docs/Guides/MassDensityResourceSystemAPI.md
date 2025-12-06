# Mass/Density/Resource System - Quick API Reference

**Quick reference for common operations. See [MassDensityResourceSystemGuide.md](MassDensityResourceSystemGuide.md) for full documentation.**

## Quick Start

### Add Mass to Entity (Inventory)
```csharp
SystemAPI.AddComponent(entity, new Inventory { MaxMass = 1000f, MaxVolume = 10f });
var items = SystemAPI.GetBuffer<InventoryItem>(entity);
items.Add(new InventoryItem { ItemId = new FixedString64Bytes("iron_ingot"), Quantity = 10f });
// MassComponent automatically added/updated by InventoryMassSystem
```

### Add Mass to Entity (Cargo)
```csharp
SystemAPI.AddComponent<HaulerTag>(entity);
var cargo = SystemAPI.GetBuffer<CargoItem>(entity);
cargo.Add(new CargoItem { ResourceId = new FixedString64Bytes("iron_ore"), Amount = 50f });
// MassComponent automatically added/updated by CargoAggregationSystem
```

### Add Material to Entity
```csharp
SystemAPI.AddComponent(entity, new MaterialId { Value = new FixedString64Bytes("steel") });
```

### Lookup Material Properties
```csharp
ref var catalog = ref SystemAPI.GetSingleton<MaterialCatalog>().Catalog.Value;
for (int i = 0; i < catalog.Materials.Length; i++)
{
    if (catalog.Materials[i].MaterialId.Equals(materialId.Value))
    {
        var material = catalog.Materials[i];
        // Use material.Density, material.YoungsModulus, etc.
        break;
    }
}
```

### Apply Forces for Movement
```csharp
SystemAPI.AddComponent(entity, new PhysicsVelocity());
SystemAPI.AddComponent(entity, new AppliedForces());
SystemAPI.AddComponent(entity, new EngineReference { EngineId = new FixedString64Bytes("thruster_small") });

var forces = SystemAPI.GetComponent<AppliedForces>(entity);
forces.Force = new float3(0, 0, 1000f);  // Thrust in N
forces.Torque = new float3(0, 100f, 0);  // Torque in N·m
SystemAPI.SetComponent(entity, forces);
// PhysicsIntegrationSystem automatically updates PhysicsVelocity and FuelConsumption
```

### Add Structural Integrity
```csharp
SystemAPI.AddComponent(entity, new StructuralState
{
    CrossSectionalArea = 10f,  // m²
    Integrity = 1.0f,
    YieldThreshold = 400e6f  // Pa
});
// StructuralIntegritySystem automatically calculates stress/strain
```

## Component Checklist

| Component | When to Add | Auto-Added By |
|-----------|-------------|---------------|
| `MassComponent` | Entities with mass | `InventoryMassSystem`, `CargoAggregationSystem` |
| `MassDirtyTag` | After modifying mass | `InventoryMassSystem`, `CargoAggregationSystem` |
| `CargoChangedTag` | After cargo changes | `CargoAggregationSystem` |
| `MaterialId` | Entities with material properties | Manual |
| `MassTierComponent` | Entities for adaptive physics | `AdaptivePhysicsSystem` |
| `StructuralState` | Entities that can take damage | Manual |
| `PhysicsVelocity` | Entities with movement | Manual |
| `AppliedForces` | Entities applying forces | Manual |
| `FuelConsumption` | Entities consuming fuel | Manual |
| `EngineReference` | Entities with engines | Manual |

## System Dependencies

```
InventoryMassSystem
    ↓ (adds MassDirtyTag)
MassAggregationSystem
    ↓ (aggregates to parents)
MassUpdateSystem
    ↓ (validates/cleans)
AdaptivePhysicsSystem
    ↓ (routes to tiers)
[Light/Medium/Heavy]MassPhysicsGroup
    ↓
InertiaCalculationSystem
    ↓
StructuralIntegritySystem
    ↓
PhysicsIntegrationSystem
```

## Material Properties Reference

| Material | Density (kg/m³) | Young's Modulus (Pa) | Yield Strength (Pa) | Flexibility |
|----------|----------------|---------------------|-------------------|-------------|
| Iron | 7870 | 200×10⁹ | 250×10⁶ | 0.1 |
| Steel | 7850 | 210×10⁹ | 400×10⁶ | 0.05 |
| Aluminum | 2700 | 70×10⁹ | 275×10⁶ | 0.15 |
| Titanium Alloy | 4500 | 110×10⁹ | 900×10⁶ | 0.08 |
| Carbon Fiber | 1600 | 230×10⁹ | 600×10⁶ | 0.2 |
| Wood | 600 | 10×10⁹ | 40×10⁶ | 0.4 |
| Organic Tissue | 1000 | 1×10⁶ | 0.1×10⁶ | 0.8 |
| Ceramic | 2400 | 300×10⁹ | 300×10⁶ | 0.02 |

## Mass Tier Thresholds

| Tier | Mass Range | Update Frequency | System Group |
|------|------------|------------------|--------------|
| Light | < 10⁴ kg | 60 Hz | `LightMassPhysicsGroup` |
| Medium | 10⁴–10⁸ kg | 6 Hz | `MediumMassPhysicsGroup` |
| Heavy | > 10⁸ kg | 0.6 Hz | `HeavyMassPhysicsGroup` |

## Common Formulas

### Mass Aggregation
```
totalMass = Σ(child.Mass)
centerOfMass = Σ(child.Mass * child.Position) / totalMass
inertia = Σ(child.InertiaTensor + child.Mass * (r² * Identity - outer(r, r)))
```

### Physics Integration
```
acceleration = thrust / totalMass
turnRate = torque / (inertiaTensor.x + inertiaTensor.y + inertiaTensor.z)
fuelUse = thrust * Δv / engineEfficiency
```

### Structural Integrity
```
Stress = Force / Area
Strain = Stress / YoungsModulus
Integrity -= (Stress > YieldThreshold) ? damageRate : 0
```

### Weapon Damage
```
Damage = (KineticEnergy / TargetYieldStrength) * MaterialFlexibility * PenetrationModifier
```

## Troubleshooting Quick Fixes

**Mass not updating?**
```csharp
SystemAPI.AddComponent<MassDirtyTag>(entity);
```

**Material lookup failing?**
```csharp
// Check catalog exists
if (!SystemAPI.TryGetSingleton<MaterialCatalog>(out var catalog)) return;
if (!catalog.Catalog.IsCreated) return;
```

**Inertia incorrect?**
- Ensure `InertiaCalculationSystem` runs after `MassAggregationSystem`
- Verify parent-child hierarchy is correct
- Check `LocalTransform.Position` is relative to parent

