# Forces System Integration Guide

**Status:** Integration Guide
**Category:** Core Physics
**Scope:** PureDOTS Foundation + Game Projects
**Created:** 2025-12-18

---

## Purpose

Quick reference for integrating the General Forces System into Godgame and Space4X projects.

See [General_Forces_System.md](General_Forces_System.md) for full technical specification.

---

## Quick Start

### 1. Enable Forces on an Entity

```csharp
// Make an entity receptive to spatial forces (gravity, wind, etc.)
em.AddComponentData(entity, new SpatialForceReceiver
{
    Mass = 1f,
    Velocity = float3.zero,
    DragCoefficient = 0.1f,
    ForceLayerMask = ForceLayers.Physical  // Gravity + Wind + Terrain + Fluid
});

// Make an entity receptive to temporal forces (time dilation)
em.AddComponentData(entity, new TemporalForceReceiver
{
    LocalTimeScale = 1f,
    TimeScaleClamp = new float2(0.1f, 10f),  // 10% to 1000% speed
    TemporalLayerMask = ForceLayers.Temporal
});

// Make an entity receptive to mass changes
em.AddComponentData(entity, new MassForceReceiver
{
    BaseMass = 1f,
    EffectiveMass = 1f,
    MassLayerMask = ForceLayers.Magical
});
```

### 2. Create a Force Field

```csharp
// Gravity well (attracts entities)
Entity gravityWell = em.CreateEntity();
em.AddComponentData(gravityWell, new RadialForceField
{
    Center = new float3(0, 0, 0),
    Strength = 100f,  // Positive = attraction
    Radius = 50f,
    Falloff = FalloffType.InverseSquare,
    ForceLayer = ForceLayers.Gravity,
    IsActive = true
});

// Wind zone (pushes entities in direction)
Entity windZone = em.CreateEntity();
em.AddComponentData(windZone, new DirectionalForceField
{
    Direction = new float3(1, 0, 0),  // Eastward
    Strength = 10f,
    Bounds = new AABB { Center = float3.zero, Extents = new float3(100, 100, 100) },
    ForceLayer = ForceLayers.Wind,
    IsActive = true
});

// Temporal slow zone (slows time)
Entity slowZone = em.CreateEntity();
em.AddComponentData(slowZone, new TemporalForceField
{
    Center = new float3(0, 0, 0),
    TimeScale = 0.5f,  // Half speed
    InnerRadius = 10f,
    OuterRadius = 20f,
    TemporalLayer = ForceLayers.Temporal,
    IsActive = true
});
```

### 3. Toggle Forces at Runtime

```csharp
// Disable a force field
var field = em.GetComponentData<RadialForceField>(fieldEntity);
field.IsActive = false;
em.SetComponentData(fieldEntity, field);

// Change force strength
field.Strength = 200f;
em.SetComponentData(fieldEntity, field);

// Move force field to new position
field.Center = newPosition;
em.SetComponentData(fieldEntity, field);
```

---

## Godgame Integration Examples

### Example 1: Divine Intervention - Gravity Miracle

```csharp
/// <summary>
/// Player casts gravity miracle to pull villagers toward a point
/// </summary>
public void CastGravityMiracle(float3 targetPosition, float strength, float duration)
{
    Entity miracle = em.CreateEntity();

    // Create attractive force
    em.AddComponentData(miracle, new RadialForceField
    {
        Center = targetPosition,
        Strength = strength,
        Radius = 20f,
        Falloff = FalloffType.InverseSquare,
        ForceLayer = ForceLayers.Divine,
        IsActive = true
    });

    // Add timer to auto-disable
    em.AddComponentData(miracle, new ForceFieldTimer
    {
        RemainingTime = duration
    });

    // Visual effect marker
    em.AddComponentData(miracle, new GodgameMiraclePresentationMarker
    {
        EffectType = MiracleFXType.GravityWell
    });
}

/// <summary>
/// System to cleanup timed force fields
/// </summary>
[BurstCompile]
public partial struct ForceFieldTimerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (timer, entity) in
            SystemAPI.Query<RefRW<ForceFieldTimer>>()
                .WithEntityAccess())
        {
            timer.ValueRW.RemainingTime -= deltaTime;

            if (timer.ValueRO.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

public struct ForceFieldTimer : IComponentData
{
    public float RemainingTime;
}
```

### Example 2: Weather System - Wind and Rain

```csharp
/// <summary>
/// Create weather system with directional wind
/// </summary>
public void CreateStormWeather(AABB stormBounds, float3 windDirection, float windStrength)
{
    Entity storm = em.CreateEntity();

    // Main wind force
    em.AddComponentData(storm, new DirectionalForceField
    {
        Direction = math.normalize(windDirection),
        Strength = windStrength,
        Bounds = stormBounds,
        ForceLayer = ForceLayers.Wind,
        IsActive = true
    });

    // Add turbulence with vortices
    int vortexCount = 5;
    for (int i = 0; i < vortexCount; i++)
    {
        float3 vortexPos = stormBounds.Center +
            UnityEngine.Random.insideUnitSphere * stormBounds.Extents.x * 0.5f;

        Entity vortex = em.CreateEntity();
        em.AddComponentData(vortex, new VortexForceField
        {
            AxisCenter = vortexPos,
            AxisDirection = new float3(0, 1, 0),
            TangentialStrength = windStrength * 0.5f,
            RadialStrength = -windStrength * 0.2f,
            AxialStrength = 0f,
            Radius = 8f,
            ForceLayer = ForceLayers.Wind,
            IsActive = true
        });

        // Make vortex follow main storm
        em.AddComponentData(vortex, new Parent { Value = storm });
    }

    // Presentation marker
    em.AddComponentData(storm, new GodgameWeatherPresentationMarker
    {
        WeatherType = WeatherType.Storm
    });
}
```

### Example 3: Terrain Physics - Water Buoyancy

```csharp
/// <summary>
/// Configure water physics for swimming/boats
/// </summary>
public void SetupWaterPhysics(float waterLevel, AABB waterBounds)
{
    Entity waterPhysics = em.CreateEntity();

    // Buoyancy (upward force when submerged)
    em.AddComponentData(waterPhysics, new HeightForceField
    {
        ReferenceHeight = waterLevel,
        StrengthPerHeight = 20f,  // Strong upward force
        Direction = new float3(0, 1, 0),
        Bounds = waterBounds,
        ForceLayer = ForceLayers.Fluid,
        IsActive = true
    });

    // Tag for special handling in movement systems
    em.AddComponentData(waterPhysics, new GodgameFluidZoneTag
    {
        FluidType = FluidType.Water,
        DragMultiplier = 3f  // Movement is slower in water
    });
}

/// <summary>
/// Configure villager for water interaction
/// </summary>
public void MakeVillagerSwimmable(Entity villager)
{
    // Enable fluid forces
    var receiver = em.GetComponentData<SpatialForceReceiver>(villager);
    receiver.ForceLayerMask |= ForceLayers.Fluid;
    em.SetComponentData(villager, receiver);

    // Add swimming ability
    em.AddComponentData(villager, new GodgameSwimmingAbility
    {
        SwimSpeed = 2f,
        IsSwimming = false
    });
}
```

---

## Space4X Integration Examples

### Example 1: Gravity Wells - Stars and Planets

```csharp
/// <summary>
/// Create gravitational field around a star
/// </summary>
public void CreateStarGravity(Entity star, float3 position, float stellarMass)
{
    // Main gravity well
    Entity gravityField = em.CreateEntity();

    em.AddComponentData(gravityField, new RadialForceField
    {
        Center = position,
        Strength = stellarMass * 100f,  // Scale with mass
        Radius = 1000f,  // Large gravity influence
        Falloff = FalloffType.InverseSquare,  // Realistic physics
        ForceLayer = ForceLayers.Gravity,
        IsActive = true
    });

    // Make gravity field follow star
    em.AddComponentData(gravityField, new Parent { Value = star });

    // Spatial index for optimization
    em.AddComponentData(gravityField, new SpatialIndexedTag
    {
        CellIndex = SpatialGridUtility.GetCellIndex(position)
    });
}

/// <summary>
/// Configure ship to be affected by gravity
/// </summary>
public void EnableGravityForShip(Entity ship, float shipMass)
{
    em.AddComponentData(ship, new SpatialForceReceiver
    {
        Mass = shipMass,
        Velocity = float3.zero,
        DragCoefficient = 0.01f,  // Space has minimal drag
        ForceLayerMask = ForceLayers.Gravity | ForceLayers.Cosmic
    });

    // Add grid receiver for movement cost
    em.AddComponentData(ship, new GridForceReceiver
    {
        BaseMovementCost = 1f,
        GridLayerMask = ForceLayers.Terrain  // Nebulae, asteroid fields
    });
}
```

### Example 2: Warp Drive - Temporal Acceleration

```csharp
/// <summary>
/// Activate warp drive on ship
/// </summary>
public void ActivateWarpDrive(Entity ship, float warpFactor)
{
    float3 shipPosition = em.GetComponentData<LocalTransform>(ship).Position;

    // Create temporal bubble around ship
    Entity warpBubble = em.CreateEntity();

    em.AddComponentData(warpBubble, new TemporalForceField
    {
        Center = shipPosition,
        TimeScale = warpFactor,  // 2.0 = double speed, 10.0 = 10x speed
        InnerRadius = 5f,
        OuterRadius = 10f,
        TemporalLayer = ForceLayers.Cosmic,
        IsActive = true
    });

    // Make bubble follow ship
    em.AddComponentData(warpBubble, new Parent { Value = ship });
    em.AddComponentData(warpBubble, new LocalTransform
    {
        Position = float3.zero,  // Relative to parent
        Rotation = quaternion.identity,
        Scale = 1f
    });

    // Store warp bubble reference on ship
    em.AddComponentData(ship, new Space4XWarpBubbleRef
    {
        BubbleEntity = warpBubble,
        WarpFactor = warpFactor
    });

    // Telemetry
    em.AddComponentData(ship, new Space4XWarpActiveTag());
}

/// <summary>
/// Deactivate warp drive
/// </summary>
public void DeactivateWarpDrive(Entity ship)
{
    if (em.HasComponent<Space4XWarpBubbleRef>(ship))
    {
        var warpRef = em.GetComponentData<Space4XWarpBubbleRef>(ship);
        em.DestroyEntity(warpRef.BubbleEntity);
        em.RemoveComponent<Space4XWarpBubbleRef>(ship);
        em.RemoveComponent<Space4XWarpActiveTag>(ship);
    }
}

public struct Space4XWarpBubbleRef : IComponentData
{
    public Entity BubbleEntity;
    public float WarpFactor;
}

public struct Space4XWarpActiveTag : IComponentData { }
```

### Example 3: Nebula Physics - Movement Resistance

```csharp
/// <summary>
/// Create nebula region with movement resistance
/// </summary>
public void CreateNebula(AABB nebulaBounds, float density)
{
    Entity nebula = em.CreateEntity();

    // Directional resistance (slows movement)
    em.AddComponentData(nebula, new DirectionalForceField
    {
        Direction = float3.zero,  // No push, just drag
        Strength = 0f,
        Bounds = nebulaBounds,
        ForceLayer = ForceLayers.Terrain,
        IsActive = true
    });

    // Grid-based movement cost
    em.AddComponentData(nebula, new Space4XNebulaZone
    {
        Density = density,
        MovementCostMultiplier = 1f + density,  // Higher density = slower movement
        SensorRangeMultiplier = 1f - (density * 0.5f)  // Reduced sensors
    });
}

/// <summary>
/// System to apply nebula movement penalties
/// </summary>
[BurstCompile]
public partial struct Space4XNebulaMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Query all ships in nebulae
        foreach (var (gridReceiver, transform, velocity) in
            SystemAPI.Query<RefRW<GridForceReceiver>,
                          RefRO<LocalTransform>,
                          RefRW<Space4XVelocity>>())
        {
            // Check if ship is in a nebula
            foreach (var nebula in SystemAPI.Query<RefRO<Space4XNebulaZone>>())
            {
                // Apply movement penalty
                float penalty = nebula.ValueRO.MovementCostMultiplier;
                velocity.ValueRW.Value *= (1f / penalty);
            }
        }
    }
}

public struct Space4XNebulaZone : IComponentData
{
    public float Density;
    public float MovementCostMultiplier;
    public float SensorRangeMultiplier;
}

public struct Space4XVelocity : IComponentData
{
    public float3 Value;
}
```

### Example 4: Magnetic Storm - Ship Disruption

```csharp
/// <summary>
/// Create magnetic storm that disrupts systems
/// </summary>
public void CreateMagneticStorm(float3 center, float radius)
{
    Entity storm = em.CreateEntity();

    // Vortex force (chaotic movement)
    em.AddComponentData(storm, new VortexForceField
    {
        AxisCenter = center,
        AxisDirection = UnityEngine.Random.onUnitSphere,
        TangentialStrength = 50f,
        RadialStrength = 20f,
        AxialStrength = 10f,
        Radius = radius,
        ForceLayer = ForceLayers.Cosmic,
        IsActive = true
    });

    // Temporal disruption (random time scale changes)
    em.AddComponentData(storm, new TemporalForceField
    {
        Center = center,
        TimeScale = UnityEngine.Random.Range(0.5f, 1.5f),
        InnerRadius = radius * 0.5f,
        OuterRadius = radius,
        TemporalLayer = ForceLayers.Cosmic,
        IsActive = true
    });

    // Game-specific disruption marker
    em.AddComponentData(storm, new Space4XSystemDisruptionZone
    {
        DisruptionStrength = 0.8f,
        AffectsShields = true,
        AffectsSensors = true,
        AffectsWeapons = true
    });

    // Animate storm parameters
    em.AddComponentData(storm, new Space4XStormAnimation
    {
        TimeScale = UnityEngine.Random.Range(0.5f, 2f)
    });
}

public struct Space4XSystemDisruptionZone : IComponentData
{
    public float DisruptionStrength;
    public bool AffectsShields;
    public bool AffectsSensors;
    public bool AffectsWeapons;
}

public struct Space4XStormAnimation : IComponentData
{
    public float TimeScale;
}
```

---

## Performance Optimization

### 1. Layer Masking Strategy

```csharp
/// <summary>
/// Recommended layer assignments
/// </summary>
public static class ForceLayers
{
    // Universal forces (affect most entities)
    public const uint Gravity = 1u << 0;
    public const uint Terrain = 1u << 1;

    // Environmental forces (selective)
    public const uint Wind = 1u << 2;
    public const uint Fluid = 1u << 3;

    // Game-specific forces
    public const uint Divine = 1u << 4;      // Godgame miracles
    public const uint Cosmic = 1u << 5;      // Space4X warp/anomalies
    public const uint Magnetism = 1u << 6;   // Both games
    public const uint Temporal = 1u << 7;    // Time effects

    // Presets
    public const uint Physical = Gravity | Terrain | Fluid;
    public const uint Environmental = Wind | Fluid | Terrain;
    public const uint Supernatural = Divine | Cosmic | Temporal;
    public const uint All = 0xFFFFFFFF;
}

/// <summary>
/// Configure entity force reception based on entity type
/// </summary>
public void ConfigureForceReception(Entity entity, EntityType type)
{
    uint mask = type switch
    {
        EntityType.Villager => ForceLayers.Physical | ForceLayers.Divine,
        EntityType.Ship => ForceLayers.Gravity | ForceLayers.Cosmic,
        EntityType.Projectile => ForceLayers.Gravity | ForceLayers.Wind,
        EntityType.Spirit => ForceLayers.Divine | ForceLayers.Temporal,
        _ => ForceLayers.Physical
    };

    em.SetComponentData(entity, new SpatialForceReceiver
    {
        ForceLayerMask = mask,
        // ... other settings
    });
}
```

### 2. Spatial Partitioning

```csharp
/// <summary>
/// Only check force fields in nearby cells
/// </summary>
[BurstCompile]
public partial struct SpatiallyPartitionedForceSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gridConfig = SystemAPI.GetSingleton<SpatialGridConfig>();

        // Build cell -> force field lookup
        var cellToFields = new NativeMultiHashMap<int3, Entity>(1000, Unity.Collections.Allocator.Temp);

        foreach (var (field, entity) in
            SystemAPI.Query<RefRO<RadialForceField>>()
                .WithEntityAccess())
        {
            // Calculate which cells this field affects
            var affectedCells = GetAffectedCells(field.ValueRO, gridConfig);
            foreach (var cell in affectedCells)
            {
                cellToFields.Add(cell, entity);
            }
        }

        // Apply forces only from fields in same cell
        foreach (var (receiver, transform) in
            SystemAPI.Query<RefRW<SpatialForceReceiver>, RefRO<LocalTransform>>())
        {
            int3 entityCell = GetCellIndex(transform.ValueRO.Position, gridConfig);

            // Check fields in this cell and neighbors
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int3 neighborCell = entityCell + new int3(dx, dy, dz);

                        if (cellToFields.TryGetFirstValue(neighborCell, out var fieldEntity, out var iterator))
                        {
                            do
                            {
                                // Apply force from this field
                                var field = state.EntityManager.GetComponentData<RadialForceField>(fieldEntity);
                                ApplyRadialForce(ref receiver.ValueRW, transform.ValueRO, field);
                            }
                            while (cellToFields.TryGetNextValue(out fieldEntity, ref iterator));
                        }
                    }
                }
            }
        }

        cellToFields.Dispose();
    }

    static NativeList<int3> GetAffectedCells(RadialForceField field, SpatialGridConfig config)
    {
        // Calculate cells within field radius
        var cells = new NativeList<int3>(Unity.Collections.Allocator.Temp);
        // ... implementation
        return cells;
    }

    static int3 GetCellIndex(float3 position, SpatialGridConfig config)
    {
        return (int3)((position - config.Origin) / config.CellSize);
    }

    static void ApplyRadialForce(ref SpatialForceReceiver receiver, LocalTransform transform, RadialForceField field)
    {
        // ... force application logic
    }
}
```

### 3. LOD for Distant Entities

```csharp
/// <summary>
/// Reduce force update frequency for distant entities
/// </summary>
public void AssignForceLOD(Entity entity, float distanceToCamera)
{
    byte lodLevel = distanceToCamera switch
    {
        < 50f => 0,   // Full rate (every frame)
        < 100f => 1,  // Half rate (every 2 frames)
        < 200f => 2,  // Quarter rate (every 4 frames)
        _ => 3        // Eighth rate (every 8 frames)
    };

    em.AddComponentData(entity, new ForceLOD
    {
        LODLevel = lodLevel,
        FrameCounter = (ushort)UnityEngine.Random.Range(0, (1 << lodLevel))
    });
}

public struct ForceLOD : IComponentData
{
    public byte LODLevel;
    public ushort FrameCounter;
}
```

---

## Debugging & Visualization

### 1. Force Field Gizmos (Editor Only)

```csharp
#if UNITY_EDITOR
/// <summary>
/// Draw force field debug visualization
/// </summary>
public class ForceFieldDebugSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithoutBurst()
            .ForEach((in RadialForceField field) =>
            {
                if (!field.IsActive) return;

                // Draw sphere for force field
                UnityEngine.Gizmos.color = field.Strength >= 0
                    ? UnityEngine.Color.blue    // Attraction
                    : UnityEngine.Color.red;    // Repulsion

                UnityEngine.Gizmos.DrawWireSphere(field.Center, field.Radius);
            })
            .Run();

        Entities
            .WithoutBurst()
            .ForEach((in DirectionalForceField field) =>
            {
                if (!field.IsActive) return;

                // Draw arrow for directional force
                UnityEngine.Gizmos.color = UnityEngine.Color.cyan;
                UnityEngine.Debug.DrawRay(
                    field.Bounds.Center,
                    field.Direction * field.Strength,
                    UnityEngine.Color.cyan);
            })
            .Run();

        Entities
            .WithoutBurst()
            .ForEach((in TemporalForceField field) =>
            {
                if (!field.IsActive) return;

                // Draw temporal distortion
                UnityEngine.Gizmos.color = UnityEngine.Color.yellow;
                UnityEngine.Gizmos.DrawWireSphere(field.Center, field.InnerRadius);
                UnityEngine.Gizmos.color = new UnityEngine.Color(1f, 1f, 0f, 0.3f);
                UnityEngine.Gizmos.DrawWireSphere(field.Center, field.OuterRadius);
            })
            .Run();
    }
}
#endif
```

### 2. Force Telemetry

```csharp
/// <summary>
/// Emit telemetry for force system
/// </summary>
[BurstCompile]
public partial struct ForceTelemetrySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var telemetryStream = SystemAPI.GetSingleton<TelemetryStream>();

        // Count active force fields by type
        int radialFields = 0;
        int directionalFields = 0;
        int vortexFields = 0;
        int temporalFields = 0;

        foreach (var field in SystemAPI.Query<RefRO<RadialForceField>>())
        {
            if (field.ValueRO.IsActive) radialFields++;
        }

        foreach (var field in SystemAPI.Query<RefRO<DirectionalForceField>>())
        {
            if (field.ValueRO.IsActive) directionalFields++;
        }

        foreach (var field in SystemAPI.Query<RefRO<VortexForceField>>())
        {
            if (field.ValueRO.IsActive) vortexFields++;
        }

        foreach (var field in SystemAPI.Query<RefRO<TemporalForceField>>())
        {
            if (field.ValueRO.IsActive) temporalFields++;
        }

        // Count force receivers
        int spatialReceivers = SystemAPI.Query<SpatialForceReceiver>().Count();
        int temporalReceivers = SystemAPI.Query<TemporalForceReceiver>().Count();

        // Emit metrics
        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Physics,
            Name = "ForceFields_Radial",
            Value = radialFields
        });

        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Physics,
            Name = "ForceFields_Directional",
            Value = directionalFields
        });

        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Physics,
            Name = "ForceFields_Vortex",
            Value = vortexFields
        });

        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Physics,
            Name = "ForceFields_Temporal",
            Value = temporalFields
        });

        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Physics,
            Name = "ForceReceivers_Spatial",
            Value = spatialReceivers
        });

        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Physics,
            Name = "ForceReceivers_Temporal",
            Value = temporalReceivers
        });
    }
}
```

---

## Common Patterns

### Pattern 1: Timed Force Field

```csharp
public struct ForceFieldTimer : IComponentData
{
    public float RemainingTime;
}

[BurstCompile]
public partial struct TimedForceFieldSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (timer, entity) in
            SystemAPI.Query<RefRW<ForceFieldTimer>>()
                .WithEntityAccess())
        {
            timer.ValueRW.RemainingTime -= deltaTime;

            if (timer.ValueRO.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

### Pattern 2: Following Force Field

```csharp
// Make force field follow an entity using Parent component
public void AttachForceFieldToEntity(Entity target, Entity forceField)
{
    em.AddComponentData(forceField, new Parent { Value = target });
    em.AddComponentData(forceField, new LocalTransform
    {
        Position = float3.zero,  // Offset from parent
        Rotation = quaternion.identity,
        Scale = 1f
    });
}
```

### Pattern 3: Conditional Force Field

```csharp
public struct ConditionalForceField : IComponentData
{
    public Entity ConditionEntity;
    public float ActivationThreshold;
}

[BurstCompile]
public partial struct ConditionalForceActivationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (conditional, radialField) in
            SystemAPI.Query<RefRO<ConditionalForceField>, RefRW<RadialForceField>>())
        {
            // Check condition (e.g., entity health)
            if (state.EntityManager.HasComponent<HealthComponent>(conditional.ValueRO.ConditionEntity))
            {
                var health = state.EntityManager.GetComponentData<HealthComponent>(conditional.ValueRO.ConditionEntity);
                bool shouldActivate = health.CurrentHealth < conditional.ValueRO.ActivationThreshold;

                radialField.ValueRW.IsActive = shouldActivate;
            }
        }
    }
}
```

---

## Testing Checklist

- [ ] Force fields toggle on/off correctly
- [ ] Multiple forces accumulate properly
- [ ] Layer masking filters entities correctly
- [ ] Temporal forces affect entity update rates
- [ ] Mass forces modify effective mass
- [ ] Spatial partitioning improves performance
- [ ] LOD reduces distant entity updates
- [ ] Telemetry reports accurate counts
- [ ] Determinism test passes (same input → same output)
- [ ] Performance test passes (10k entities, 100 fields < 16ms)

---

## Related Documentation

- **Technical Spec**: [General_Forces_System.md](General_Forces_System.md)
- **Entity Agnostic Design**: [Entity_Agnostic_Design.md](Entity_Agnostic_Design.md)
- **Spatial Grid**: `../../Architecture/SpatialGrid.md`
- **Time Spine**: `../../Architecture/TimeSystem.md`
- **Burst Optimization**: `../../BestPractices/BurstOptimization.md`

---

**Last Updated:** 2025-12-18
**Status:** Integration Guide
**For:** Godgame + Space4X developers
