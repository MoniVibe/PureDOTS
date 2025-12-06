# Mass, Density, and Resource System Guide

**Last Updated**: 2025-01-27  
**Purpose**: Complete reference for interfacing with the hierarchical mass/density/resource system

---

## Overview

The Mass, Density, and Resource System provides a fully data-driven, physically consistent architecture for simulating mass, density, and resource properties across millions-billions of entities. It uses hierarchical aggregation, material property blobs, adaptive precision physics, and deterministic parallel updates.

### Key Features

- **Hierarchical Mass Aggregation**: Mass propagates from child entities (cargo, inventory items) to parent entities (ships, containers)
- **Data-Driven Materials**: Material properties stored in BlobAssets for efficient lookup
- **Adaptive Precision Physics**: Entities update at different frequencies based on mass (60Hz/6Hz/0.6Hz)
- **Deterministic Updates**: Burst-compiled parallel jobs with sparse updates (only when dirty)
- **Physics-Driven Damage**: Weapon damage calculated from material properties and kinetic energy

---

## Architecture

### System Execution Order

```
SimulationSystemGroup
├── InventoryMassSystem (calculates inventory mass)
├── CargoAggregationSystem (aggregates cargo mass)
├── MassAggregationSystem (hierarchical reduction)
├── MassUpdateSystem (validates/cleans mass data)
├── AdaptivePhysicsSystem (routes to tier groups)
├── LightMassPhysicsGroup (< 10⁴ kg, 60 Hz)
├── MediumMassPhysicsGroup (10⁴–10⁸ kg, 6 Hz)
├── HeavyMassPhysicsGroup (> 10⁸ kg, 0.6 Hz)
├── InertiaCalculationSystem (composite inertia)
├── StructuralIntegritySystem (stress/strain)
├── PhysicsIntegrationSystem (movement/fuel)
├── LODAggregationSystem (mass proxies)
└── WeaponDamageSystem (material-based damage)
```

### Component Flow

```
InventoryItem/CargoItem
    ↓ (InventoryMassSystem / CargoAggregationSystem)
MassComponent + MassDirtyTag
    ↓ (MassAggregationSystem)
Aggregated MassComponent (parent entities)
    ↓ (AdaptivePhysicsSystem)
MassTierComponent (routing to tier groups)
    ↓ (PhysicsIntegrationSystem)
PhysicsVelocity + FuelConsumption
```

---

## Core Components

### MassComponent

Stores hierarchical mass properties for an entity.

```csharp
public struct MassComponent : IComponentData
{
    public float Mass;              // Total mass in kg
    public float3 CenterOfMass;      // Center of mass in local space
    public float3 InertiaTensor;    // Diagonalized inertia (Ixx, Iyy, Izz)
}
```

**Usage**:
- Automatically created/updated by `InventoryMassSystem` and `CargoAggregationSystem`
- Read by `MassAggregationSystem` for hierarchical reduction
- Used by `PhysicsIntegrationSystem` for movement calculations

**When to Add Manually**:
- For entities that don't use inventory/cargo but need mass (e.g., hulls, structures)
- Set initial mass, then let aggregation systems update it

### MassDirtyTag

Tag indicating mass needs recalculation. Automatically removed by `MassUpdateSystem`.

**When Added**:
- Automatically by `InventoryMassSystem` when inventory changes
- Automatically by `CargoAggregationSystem` when cargo changes
- Manually when you modify `MassComponent` directly

**Usage**:
```csharp
// Mark mass as dirty after manual modification
SystemAPI.SetComponent(entity, newMass);
SystemAPI.AddComponent<MassDirtyTag>(entity);
```

### CargoChangedTag

Tag indicating cargo has changed. Used to trigger mass recalculation for cargo-dependent entities.

**When Added**:
- Automatically by `CargoAggregationSystem` when cargo items change

### MaterialId

Component storing material identifier for an entity. Used to look up material properties from `MaterialCatalog`.

```csharp
public struct MaterialId : IComponentData
{
    public FixedString64Bytes Value;  // e.g., "iron", "steel", "wood"
}
```

**Usage**:
```csharp
// Add material ID to entity
SystemAPI.AddComponent(entity, new MaterialId { Value = new FixedString64Bytes("iron") });
```

**Lookup Material Properties**:
```csharp
ref var catalog = ref SystemAPI.GetSingleton<MaterialCatalog>().Catalog.Value;
for (int i = 0; i < catalog.Materials.Length; i++)
{
    if (catalog.Materials[i].MaterialId.Equals(materialId.Value))
    {
        var material = catalog.Materials[i];
        // Use material.Density, material.YoungsModulus, etc.
    }
}
```

### MaterialSpec

Blob struct containing physical constants per material.

```csharp
public struct MaterialSpec
{
    public FixedString64Bytes MaterialId;
    public MaterialCategory Category;  // Metal, Alloy, Organic, Composite
    public float Density;              // kg/m³
    public float YoungsModulus;        // Pa (elastic modulus)
    public float YieldStrength;        // Pa (yield threshold)
    public float Flexibility;          // 0-1 (higher = more flexible)
    public float HeatCapacity;         // J/(kg·K)
}
```

**Predefined Materials** (via `MaterialCatalogBootstrapSystem`):
- `iron`, `steel`, `aluminum` (Metals)
- `titanium_alloy` (Alloy)
- `wood`, `organic_tissue` (Organic)
- `carbon_fiber`, `ceramic` (Composite)

### ResourceState

Component storing resource mass, density, and dimensions.

```csharp
public struct ResourceState : IComponentData
{
    public float Mass;           // Total mass in kg
    public float Density;        // Density in kg/m³
    public float3 Dimensions;   // Bounding box or radius
    public uint LastUpdateTick;
}
```

**Usage**:
- For resource piles and cargo bays
- Volume = Mass / Density
- Occupancy = Volume / ContainerCapacity

### StructuralState

Component tracking structural integrity.

```csharp
public struct StructuralState : IComponentData
{
    public float Stress;              // Current stress in Pa
    public float Strain;               // Current strain
    public float YieldThreshold;       // Yield strength in Pa
    public float Integrity;            // 0-1 (1 = perfect, 0 = destroyed)
    public float CrossSectionalArea;   // m² for stress calculations
    public uint LastUpdateTick;
}
```

**Usage**:
- Added to entities that can take structural damage
- Updated by `StructuralIntegritySystem` based on forces and material properties
- Integrity reduces when Stress > YieldThreshold

---

## System Integration

### Adding Mass to an Entity

**Option 1: Via Inventory** (Recommended)
```csharp
// Add Inventory component
SystemAPI.AddComponent(entity, new Inventory 
{ 
    MaxMass = 1000f, 
    MaxVolume = 10f 
});

// Add InventoryItem buffer
var items = SystemAPI.GetBuffer<InventoryItem>(entity);
items.Add(new InventoryItem 
{ 
    ItemId = new FixedString64Bytes("iron_ingot"),
    Quantity = 10f 
});

// InventoryMassSystem will automatically:
// - Calculate total mass from ItemSpec catalog
// - Add/update MassComponent
// - Add MassDirtyTag for aggregation
```

**Option 2: Via Cargo**
```csharp
// Add HaulerTag and CargoItem buffer
SystemAPI.AddComponent<HaulerTag>(entity);
var cargo = SystemAPI.GetBuffer<CargoItem>(entity);
cargo.Add(new CargoItem 
{ 
    ResourceId = new FixedString64Bytes("iron_ore"),
    Amount = 50f 
});

// CargoAggregationSystem will automatically:
// - Calculate total mass from ItemSpec catalog
// - Add/update MassComponent
// - Add MassDirtyTag and CargoChangedTag
```

**Option 3: Manual**
```csharp
// For entities without inventory/cargo
SystemAPI.AddComponent(entity, new MassComponent
{
    Mass = 5000f,
    CenterOfMass = float3.zero,
    InertiaTensor = new float3(1000f, 1000f, 1000f)
});
SystemAPI.AddComponent<MassDirtyTag>(entity);
```

### Using Material Properties

**Step 1: Add MaterialId**
```csharp
SystemAPI.AddComponent(entity, new MaterialId 
{ 
    Value = new FixedString64Bytes("steel") 
});
```

**Step 2: Lookup in Systems**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    if (!SystemAPI.TryGetSingleton<MaterialCatalog>(out var catalog))
        return;
    
    ref var catalogBlob = ref catalog.Catalog.Value;
    
    foreach (var (materialId, entity) in SystemAPI.Query<RefRO<MaterialId>>().WithEntityAccess())
    {
        // Find material spec
        MaterialSpec material = default;
        for (int i = 0; i < catalogBlob.Materials.Length; i++)
        {
            if (catalogBlob.Materials[i].MaterialId.Equals(materialId.ValueRO.Value))
            {
                material = catalogBlob.Materials[i];
                break;
            }
        }
        
        // Use material properties
        var density = material.Density;
        var strength = material.YieldStrength;
        // ...
    }
}
```

### Integrating with Movement Systems

**Using PhysicsIntegrationSystem**:
```csharp
// Add required components
SystemAPI.AddComponent(entity, new PhysicsVelocity());
SystemAPI.AddComponent(entity, new AppliedForces());
SystemAPI.AddComponent(entity, new FuelConsumption());
SystemAPI.AddComponent(entity, new EngineReference 
{ 
    EngineId = new FixedString64Bytes("thruster_small") 
});

// Apply forces (your movement system)
var forces = SystemAPI.GetComponent<AppliedForces>(entity);
forces.Force = new float3(0, 0, 1000f);  // Thrust in N
forces.Torque = new float3(0, 100f, 0);  // Torque in N·m
SystemAPI.SetComponent(entity, forces);

// PhysicsIntegrationSystem will automatically:
// - Calculate acceleration = thrust / mass
// - Calculate turnRate = torque / totalInertia
// - Update PhysicsVelocity
// - Calculate fuel consumption
```

### Using Adaptive Physics Tiers

**Automatic Routing**:
- Entities with `MassComponent` automatically get `MassTierComponent` via `AdaptivePhysicsSystem`
- Tier determined by mass thresholds:
  - Light: < 10⁴ kg → `LightMassPhysicsGroup` (60 Hz)
  - Medium: 10⁴–10⁸ kg → `MediumMassPhysicsGroup` (6 Hz)
  - Heavy: > 10⁸ kg → `HeavyMassPhysicsGroup` (0.6 Hz)

**Custom Tier Systems**:
```csharp
[UpdateInGroup(typeof(LightMassPhysicsGroup))]
public partial struct MyLightMassSystem : ISystem
{
    // Only processes entities in LightMassPhysicsGroup
    // Runs at 60 Hz
}
```

**Configuring Thresholds**:
```csharp
// Modify PhysicsTierConfig singleton
var config = SystemAPI.GetSingleton<PhysicsTierConfig>();
config.MediumTierThreshold = 5000f;  // Custom threshold
config.HeavyTierThreshold = 50000000f;
SystemAPI.SetSingleton(config);
```

### Calculating Weapon Damage

**Using WeaponDamageSystem**:
```csharp
// WeaponDamageSystem automatically calculates damage for entities with:
// - StructuralState
// - MaterialId
// - MassComponent

// Damage formula: Damage = (KineticEnergy / TargetYieldStrength) * MaterialFlexibility
// Material penetration modifiers applied from WeaponSpec.DamageModel

// To apply weapon damage manually:
var damage = WeaponDamageSystem.CalculateDamage(
    kineticEnergy: 1000000f,  // J
    targetMaterial: materialSpec,
    damageModel: weaponSpec.Damage,
    targetCategory: MaterialCategory.Metal
);
```

---

## Common Patterns

### Pattern 1: Adding Mass to a Ship with Cargo

```csharp
// 1. Create ship entity
var ship = entityManager.CreateEntity();

// 2. Add cargo capacity
entityManager.AddComponent(ship, new HaulerCapacity 
{ 
    MaxMass = 10000f, 
    MaxVolume = 100f 
});
entityManager.AddComponent<HaulerTag>(ship);

// 3. Add cargo items
var cargo = entityManager.AddBuffer<CargoItem>(ship);
cargo.Add(new CargoItem 
{ 
    ResourceId = new FixedString64Bytes("iron_ore"),
    Amount = 100f 
});

// 4. Systems automatically:
//    - CargoAggregationSystem calculates cargo mass
//    - Adds MassComponent with total mass
//    - Adds MassDirtyTag
//    - MassAggregationSystem aggregates into parent if ship has parent
```

### Pattern 2: Creating a Material-Based Structure

```csharp
// 1. Create structure entity
var structure = entityManager.CreateEntity();

// 2. Add material
entityManager.AddComponent(structure, new MaterialId 
{ 
    Value = new FixedString64Bytes("steel") 
});

// 3. Add structural state
entityManager.AddComponent(structure, new StructuralState
{
    CrossSectionalArea = 10f,  // m²
    Integrity = 1.0f,
    YieldThreshold = 400e6f  // Pa (from material, but can override)
});

// 4. Add mass (manual or via aggregation)
entityManager.AddComponent(structure, new MassComponent
{
    Mass = 5000f,
    CenterOfMass = float3.zero,
    InertiaTensor = new float3(1000f, 1000f, 1000f)
});

// 5. StructuralIntegritySystem will:
//    - Calculate stress from forces
//    - Calculate strain = stress / YoungsModulus
//    - Reduce integrity when stress > yield threshold
```

### Pattern 3: Hierarchical Mass (Parent-Child)

```csharp
// 1. Create parent entity (ship)
var ship = entityManager.CreateEntity();
entityManager.AddComponent(ship, new MassComponent 
{ 
    Mass = 10000f  // Base hull mass
});

// 2. Create child entity (cargo container)
var container = entityManager.CreateEntity();
entityManager.AddComponent(container, new Parent { Value = ship });
entityManager.AddComponent(container, new LocalTransform 
{ 
    Position = new float3(0, 0, 5)  // Offset from parent
});

// 3. Add cargo to container
var cargo = entityManager.AddBuffer<CargoItem>(container);
cargo.Add(new CargoItem { ResourceId = ..., Amount = 100f });

// 4. Systems automatically:
//    - CargoAggregationSystem calculates container mass
//    - MassAggregationSystem aggregates container mass into ship
//    - Ship's MassComponent updated with total mass
//    - CenterOfMass and InertiaTensor recalculated
```

---

## Performance Considerations

### Sparse Updates

- Systems only recalculate when `MassDirtyTag` or `CargoChangedTag` present
- Always add these tags when modifying mass-affecting components
- `MassUpdateSystem` removes tags after validation

### Blob Lookups

- Material catalogs are BlobAssets (read-only, shared)
- Use `ref var catalog = ref materialCatalog.Catalog.Value` for efficient access
- Lookups are O(n) linear search - consider caching indices for hot paths

### Parallel Jobs

- All systems use `IJobChunk` with `ScheduleParallel` for multi-threading
- Dependency chains ensure correct execution order
- Avoid `Complete()` mid-frame - let dependency system handle scheduling

### Chunk Layout

- Keep hot data (MassComponent, PhysicsVelocity) in same archetype
- Cold data (MaterialId, HullReference) can be in separate archetype
- Use `[InternalBufferCapacity]` to avoid heap allocations for buffers

### Adaptive Physics

- Heavy entities (> 10⁸ kg) update at 0.6 Hz instead of 60 Hz
- Reduces update cost by 100× for massive bodies
- Negligible drift over long runs due to analytic integration

---

## Troubleshooting

### Mass Not Updating

**Check**:
1. Is `MassDirtyTag` present? (should be added automatically)
2. Is `RewindState.Mode == RewindMode.Record`? (systems skip during playback)
3. Does entity have required components? (`Inventory` + `InventoryItem` buffer, or `CargoItem` buffer)

**Fix**:
```csharp
// Manually trigger update
SystemAPI.AddComponent<MassDirtyTag>(entity);
```

### Material Lookup Failing

**Check**:
1. Is `MaterialCatalog` singleton present? (created by `MaterialCatalogBootstrapSystem`)
2. Is `MaterialId.Value` matching catalog entry exactly? (case-sensitive)
3. Is catalog blob created? (check `Catalog.IsCreated`)

**Fix**:
```csharp
// Verify catalog exists
if (!SystemAPI.TryGetSingleton<MaterialCatalog>(out var catalog))
{
    Debug.LogError("MaterialCatalog singleton missing!");
    return;
}

// Verify blob is created
if (!catalog.Catalog.IsCreated)
{
    Debug.LogError("MaterialCatalog blob not created!");
    return;
}
```

### Inertia Tensor Incorrect

**Check**:
1. Is `InertiaCalculationSystem` running after `MassAggregationSystem`?
2. Are child entities properly parented? (`Parent` component present)
3. Are positions relative to parent center of mass?

**Fix**:
- Ensure system order: `MassAggregationSystem` → `InertiaCalculationSystem`
- Verify parent-child hierarchy is correct
- Check that `LocalTransform.Position` is relative to parent

---

## API Reference

### Systems

| System | Purpose | Update Frequency |
|--------|---------|------------------|
| `InventoryMassSystem` | Calculates inventory mass from items | Every tick (sparse) |
| `CargoAggregationSystem` | Aggregates cargo mass | Every tick (sparse) |
| `MassAggregationSystem` | Hierarchical mass reduction | Every tick (sparse) |
| `MassUpdateSystem` | Validates/cleans mass data | Every tick (sparse) |
| `AdaptivePhysicsSystem` | Routes entities to tier groups | Every tick |
| `InertiaCalculationSystem` | Calculates composite inertia | Every tick (sparse) |
| `StructuralIntegritySystem` | Calculates stress/strain | Every tick |
| `PhysicsIntegrationSystem` | Mass-aware movement/fuel | Every tick |
| `LODAggregationSystem` | Creates mass proxies | Every tick (sparse) |
| `WeaponDamageSystem` | Material-based damage | Every tick |

### Bootstrap Systems

| System | Purpose | When Runs |
|--------|---------|-----------|
| `MaterialCatalogBootstrapSystem` | Creates default material catalog | Once at startup |
| `ItemSpecBootstrapSystem` | Creates default item catalog | Once at startup |

---

## Extension Points

### Adding Custom Materials

```csharp
// In MaterialCatalogBootstrapSystem.EnsureCatalog()
materials.Add(new MaterialSpec
{
    MaterialId = new FixedString64Bytes("custom_material"),
    Name = new FixedString64Bytes("Custom Material"),
    Category = MaterialCategory.Composite,
    Density = 2000f,
    YoungsModulus = 150e9f,
    YieldStrength = 500e6f,
    Flexibility = 0.15f,
    HeatCapacity = 600f
});
```

### Custom Mass Calculation

```csharp
// Override mass calculation in InventoryMassSystem
// Or create custom system that runs before MassAggregationSystem
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(MassAggregationSystem))]
public partial struct CustomMassSystem : ISystem
{
    // Your custom mass calculation logic
}
```

### Custom Damage Formula

```csharp
// Extend WeaponDamageSystem.CalculateDamage() or create custom system
public static float CalculateCustomDamage(
    float kineticEnergy,
    in MaterialSpec targetMaterial,
    in DamageModel damageModel)
{
    // Your custom formula
    return /* ... */;
}
```

---

## See Also

- [Movement Authoring Guide](MovementAuthoringGuide.md) - How to author movement systems
- [Threading/Parallelization Guide](ThreadingParallelizationGuide.md) - Performance patterns
- [Sanity Check Guide](sanity.md) - ECS design best practices

