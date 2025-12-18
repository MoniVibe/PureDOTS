# General Forces System - PureDOTS Foundation

**Status:** Design Document
**Category:** Core Physics & Simulation
**Scope:** PureDOTS Foundation Layer
**Created:** 2025-12-18
**Last Updated:** 2025-12-18

---

## Purpose

PureDOTS provides a **unified forces framework** that can manipulate spatial position, mass, time flow, and other physical properties of entities. The system is designed to be:
- **Entity-agnostic**: Works on any entity with force-receptive components
- **Runtime-flexible**: Force fields, physics parameters, and time scaling can change during gameplay
- **Burst-compatible**: All hot paths use `IJobEntity` with no managed allocations
- **Composable**: Multiple force types can stack and interact
- **Deterministic**: Same inputs always produce same outputs for rewind support

---

## Core Concept: Forces as Modifiers

A **force** is any influence that modifies entity properties over time:

- **Spatial Forces**: Modify position/velocity (gravity, wind, magnetism, repulsion)
- **Temporal Forces**: Modify local time flow rate (time dilation, temporal stasis)
- **Mass Forces**: Modify effective mass (density shifts, phase changes)
- **Resistance Forces**: Modify drag/friction coefficients
- **Grid Forces**: Modify spatial grid cell properties (traversal cost, visibility)

**Key Principle:** Forces are data, not code. Force behavior is defined through blob assets and component data, not through inheritance hierarchies.

---

## Component Architecture

### 1. Force Receiver Components

Entities that can be affected by forces have receiver components:

```csharp
/// <summary>
/// Marks an entity as receptive to spatial forces (gravity, wind, etc.)
/// </summary>
public struct SpatialForceReceiver : IComponentData
{
    /// <summary>
    /// Mass for force calculations (F = ma)
    /// </summary>
    public float Mass;

    /// <summary>
    /// Current velocity vector
    /// </summary>
    public float3 Velocity;

    /// <summary>
    /// Accumulated force this frame (cleared after integration)
    /// </summary>
    public float3 AccumulatedForce;

    /// <summary>
    /// Drag coefficient (0 = no drag, 1 = full drag)
    /// </summary>
    public float DragCoefficient;

    /// <summary>
    /// Mask for which force layers affect this entity
    /// </summary>
    public uint ForceLayerMask;
}

/// <summary>
/// Marks an entity as receptive to temporal forces (time dilation)
/// </summary>
public struct TemporalForceReceiver : IComponentData
{
    /// <summary>
    /// Local time scale multiplier (1.0 = normal, 0.5 = half speed, 2.0 = double speed)
    /// </summary>
    public float LocalTimeScale;

    /// <summary>
    /// Accumulated time scale delta this frame
    /// </summary>
    public float AccumulatedTimeScaleDelta;

    /// <summary>
    /// Min/max clamps for time scale
    /// </summary>
    public float2 TimeScaleClamp; // x = min, y = max

    /// <summary>
    /// Mask for which temporal force layers affect this entity
    /// </summary>
    public uint TemporalLayerMask;
}

/// <summary>
/// Marks an entity as receptive to mass forces (density changes)
/// </summary>
public struct MassForceReceiver : IComponentData
{
    /// <summary>
    /// Base mass (unmodified)
    /// </summary>
    public float BaseMass;

    /// <summary>
    /// Current effective mass (after force modifications)
    /// </summary>
    public float EffectiveMass;

    /// <summary>
    /// Accumulated mass delta this frame
    /// </summary>
    public float AccumulatedMassDelta;

    /// <summary>
    /// Mask for which mass force layers affect this entity
    /// </summary>
    public uint MassLayerMask;
}

/// <summary>
/// Grid-based movement resistance (terrain, fluid resistance)
/// </summary>
public struct GridForceReceiver : IComponentData
{
    /// <summary>
    /// Base movement cost multiplier (1.0 = normal, 2.0 = half speed)
    /// </summary>
    public float BaseMovementCost;

    /// <summary>
    /// Current movement cost (after terrain/fluid forces)
    /// </summary>
    public float EffectiveMovementCost;

    /// <summary>
    /// Height offset from terrain surface
    /// </summary>
    public float HeightOffset;

    /// <summary>
    /// Mask for which grid layers affect this entity
    /// </summary>
    public uint GridLayerMask;
}
```

### 2. Force Field Components

Force fields define regions of space that apply forces:

```csharp
/// <summary>
/// Defines a force field that affects entities within range
/// </summary>
public struct ForceFieldEmitter : IComponentData
{
    /// <summary>
    /// Force field type (references blob asset)
    /// </summary>
    public ForceFieldTypeId TypeId;

    /// <summary>
    /// World position of field center
    /// </summary>
    public float3 Position;

    /// <summary>
    /// Force strength/magnitude
    /// </summary>
    public float Strength;

    /// <summary>
    /// Effective radius (distance falloff starts here)
    /// </summary>
    public float Radius;

    /// <summary>
    /// Falloff curve exponent (1 = linear, 2 = quadratic, etc.)
    /// </summary>
    public float FalloffExponent;

    /// <summary>
    /// Force layer this emitter affects
    /// </summary>
    public uint ForceLayer;

    /// <summary>
    /// Enabled state (can be toggled at runtime)
    /// </summary>
    public bool IsActive;
}

/// <summary>
/// Directional force field (wind, current, conveyor)
/// </summary>
public struct DirectionalForceField : IComponentData
{
    /// <summary>
    /// Force direction (normalized)
    /// </summary>
    public float3 Direction;

    /// <summary>
    /// Strength magnitude
    /// </summary>
    public float Strength;

    /// <summary>
    /// AABB bounds for this field
    /// </summary>
    public AABB Bounds;

    /// <summary>
    /// Force layer
    /// </summary>
    public uint ForceLayer;

    public bool IsActive;
}

/// <summary>
/// Radial force field (gravity well, explosion, implosion)
/// </summary>
public struct RadialForceField : IComponentData
{
    /// <summary>
    /// Center point
    /// </summary>
    public float3 Center;

    /// <summary>
    /// Positive = attraction (gravity), negative = repulsion (explosion)
    /// </summary>
    public float Strength;

    /// <summary>
    /// Radius of effect
    /// </summary>
    public float Radius;

    /// <summary>
    /// Falloff type (inverse square, linear, etc.)
    /// </summary>
    public FalloffType Falloff;

    /// <summary>
    /// Force layer
    /// </summary>
    public uint ForceLayer;

    public bool IsActive;
}

/// <summary>
/// Vortex force field (tornado, whirlpool, orbital)
/// </summary>
public struct VortexForceField : IComponentData
{
    /// <summary>
    /// Vortex axis center point
    /// </summary>
    public float3 AxisCenter;

    /// <summary>
    /// Vortex axis direction (normalized)
    /// </summary>
    public float3 AxisDirection;

    /// <summary>
    /// Tangential strength (rotation speed)
    /// </summary>
    public float TangentialStrength;

    /// <summary>
    /// Radial strength (pull toward/away from axis)
    /// </summary>
    public float RadialStrength;

    /// <summary>
    /// Axial strength (pull along axis)
    /// </summary>
    public float AxialStrength;

    /// <summary>
    /// Radius of effect
    /// </summary>
    public float Radius;

    /// <summary>
    /// Force layer
    /// </summary>
    public uint ForceLayer;

    public bool IsActive;
}

/// <summary>
/// Temporal force field (time dilation zone)
/// </summary>
public struct TemporalForceField : IComponentData
{
    /// <summary>
    /// Center of temporal distortion
    /// </summary>
    public float3 Center;

    /// <summary>
    /// Time scale at center (0 = frozen, 0.5 = half speed, 2.0 = double speed)
    /// </summary>
    public float TimeScale;

    /// <summary>
    /// Radius of full effect
    /// </summary>
    public float InnerRadius;

    /// <summary>
    /// Radius where effect fades to zero
    /// </summary>
    public float OuterRadius;

    /// <summary>
    /// Temporal layer
    /// </summary>
    public uint TemporalLayer;

    public bool IsActive;
}

/// <summary>
/// Height-based force field (buoyancy, atmospheric pressure)
/// </summary>
public struct HeightForceField : IComponentData
{
    /// <summary>
    /// Reference height (sea level, ground level, etc.)
    /// </summary>
    public float ReferenceHeight;

    /// <summary>
    /// Force strength per unit height difference
    /// </summary>
    public float StrengthPerHeight;

    /// <summary>
    /// Direction of force (up = buoyancy, down = gravity)
    /// </summary>
    public float3 Direction;

    /// <summary>
    /// AABB bounds for this field
    /// </summary>
    public AABB Bounds;

    /// <summary>
    /// Force layer
    /// </summary>
    public uint ForceLayer;

    public bool IsActive;
}
```

### 3. Force Configuration Data

Force behavior defined through blob assets:

```csharp
/// <summary>
/// Blob asset defining force field behavior
/// </summary>
public struct ForceFieldTypeDef
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public ForceFieldTypeId TypeId;

    /// <summary>
    /// Falloff curve samples (distance -> strength multiplier)
    /// </summary>
    public BlobArray<float2> FalloffCurve; // x = normalized distance, y = multiplier

    /// <summary>
    /// Force application mode
    /// </summary>
    public ForceApplicationMode ApplicationMode;

    /// <summary>
    /// Whether this force affects velocity or position directly
    /// </summary>
    public ForceIntegrationMode IntegrationMode;

    /// <summary>
    /// Maximum force magnitude (safety clamp)
    /// </summary>
    public float MaxMagnitude;
}

public enum ForceApplicationMode : byte
{
    Continuous,      // Applied every frame
    Impulse,         // Applied once then disabled
    Periodic,        // Applied at intervals
    Conditional      // Applied when conditions met
}

public enum ForceIntegrationMode : byte
{
    Acceleration,    // F = ma (affects velocity)
    VelocityDirect,  // Directly modify velocity
    PositionDirect   // Directly modify position (telekinesis)
}

public enum FalloffType : byte
{
    None,           // No falloff (constant strength)
    Linear,         // Linear distance falloff
    InverseSquare,  // Physically accurate gravity
    Exponential,    // Rapid dropoff
    Custom          // Use blob curve
}
```

---

## System Architecture

### 1. Force Accumulation Systems

```csharp
/// <summary>
/// Spatial force accumulation (gravity, wind, etc.)
/// Runs in parallel per entity, accumulates forces from all active fields
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(ForceAccumulationSystemGroup))]
public partial struct SpatialForceAccumulationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Clear accumulated forces
        new ClearAccumulatedForcesJob().ScheduleParallel();

        // Query all active force fields
        var radialFields = SystemAPI.Query<RefRO<RadialForceField>>()
            .Where(f => f.ValueRO.IsActive);
        var directionalFields = SystemAPI.Query<RefRO<DirectionalForceField>>()
            .Where(f => f.ValueRO.IsActive);
        var vortexFields = SystemAPI.Query<RefRO<VortexForceField>>()
            .Where(f => f.ValueRO.IsActive);
        var heightFields = SystemAPI.Query<RefRO<HeightForceField>>()
            .Where(f => f.ValueRO.IsActive);

        // Apply each force type
        foreach (var field in radialFields)
        {
            new ApplyRadialForceJob { Field = field.ValueRO }.ScheduleParallel();
        }

        foreach (var field in directionalFields)
        {
            new ApplyDirectionalForceJob { Field = field.ValueRO }.ScheduleParallel();
        }

        foreach (var field in vortexFields)
        {
            new ApplyVortexForceJob { Field = field.ValueRO }.ScheduleParallel();
        }

        foreach (var field in heightFields)
        {
            new ApplyHeightForceJob { Field = field.ValueRO }.ScheduleParallel();
        }
    }
}

[BurstCompile]
partial struct ClearAccumulatedForcesJob : IJobEntity
{
    void Execute(ref SpatialForceReceiver receiver)
    {
        receiver.AccumulatedForce = float3.zero;
    }
}

[BurstCompile]
partial struct ApplyRadialForceJob : IJobEntity
{
    [ReadOnly] public RadialForceField Field;

    void Execute(
        ref SpatialForceReceiver receiver,
        in LocalTransform transform)
    {
        // Check layer mask
        if ((receiver.ForceLayerMask & Field.ForceLayer) == 0)
            return;

        // Calculate distance
        float3 toEntity = transform.Position - Field.Center;
        float distance = math.length(toEntity);

        // Early out if outside radius
        if (distance > Field.Radius)
            return;

        // Calculate falloff
        float normalizedDist = distance / Field.Radius;
        float falloffMultiplier = CalculateFalloff(normalizedDist, Field.Falloff);

        // Calculate force direction
        float3 forceDir = Field.Strength >= 0
            ? -math.normalize(toEntity)  // Attraction
            : math.normalize(toEntity);   // Repulsion

        // Calculate force magnitude
        float magnitude = math.abs(Field.Strength) * falloffMultiplier;

        // Accumulate force
        receiver.AccumulatedForce += forceDir * magnitude;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float CalculateFalloff(float normalizedDist, FalloffType type)
    {
        return type switch
        {
            FalloffType.None => 1f,
            FalloffType.Linear => 1f - normalizedDist,
            FalloffType.InverseSquare => 1f / math.max(0.01f, normalizedDist * normalizedDist),
            FalloffType.Exponential => math.exp(-normalizedDist * 3f),
            _ => 1f
        };
    }
}

[BurstCompile]
partial struct ApplyDirectionalForceJob : IJobEntity
{
    [ReadOnly] public DirectionalForceField Field;

    void Execute(
        ref SpatialForceReceiver receiver,
        in LocalTransform transform)
    {
        // Check layer mask
        if ((receiver.ForceLayerMask & Field.ForceLayer) == 0)
            return;

        // Check if entity is within bounds
        if (!Field.Bounds.Contains(transform.Position))
            return;

        // Apply force
        receiver.AccumulatedForce += Field.Direction * Field.Strength;
    }
}

[BurstCompile]
partial struct ApplyVortexForceJob : IJobEntity
{
    [ReadOnly] public VortexForceField Field;

    void Execute(
        ref SpatialForceReceiver receiver,
        in LocalTransform transform)
    {
        // Check layer mask
        if ((receiver.ForceLayerMask & Field.ForceLayer) == 0)
            return;

        // Vector from entity to axis
        float3 toEntity = transform.Position - Field.AxisCenter;

        // Project onto axis to get radial component
        float axisProjection = math.dot(toEntity, Field.AxisDirection);
        float3 axisPoint = Field.AxisCenter + Field.AxisDirection * axisProjection;
        float3 radialVector = transform.Position - axisPoint;
        float radialDistance = math.length(radialVector);

        // Early out if outside radius
        if (radialDistance > Field.Radius)
            return;

        // Calculate falloff
        float falloff = 1f - (radialDistance / Field.Radius);

        // Tangential force (rotation)
        float3 tangentialDir = math.cross(Field.AxisDirection, radialVector);
        if (math.lengthsq(tangentialDir) > 0.0001f)
        {
            tangentialDir = math.normalize(tangentialDir);
            receiver.AccumulatedForce += tangentialDir * Field.TangentialStrength * falloff;
        }

        // Radial force (pull toward/away from axis)
        if (radialDistance > 0.0001f)
        {
            float3 radialDir = math.normalize(radialVector);
            receiver.AccumulatedForce += radialDir * Field.RadialStrength * falloff;
        }

        // Axial force (pull along axis)
        receiver.AccumulatedForce += Field.AxisDirection * Field.AxialStrength * falloff;
    }
}

[BurstCompile]
partial struct ApplyHeightForceJob : IJobEntity
{
    [ReadOnly] public HeightForceField Field;

    void Execute(
        ref SpatialForceReceiver receiver,
        in LocalTransform transform)
    {
        // Check layer mask
        if ((receiver.ForceLayerMask & Field.ForceLayer) == 0)
            return;

        // Check bounds
        if (!Field.Bounds.Contains(transform.Position))
            return;

        // Calculate height difference
        float heightDiff = transform.Position.y - Field.ReferenceHeight;

        // Calculate force
        float magnitude = heightDiff * Field.StrengthPerHeight;
        receiver.AccumulatedForce += Field.Direction * magnitude;
    }
}
```

### 2. Force Integration Systems

```csharp
/// <summary>
/// Integrates accumulated spatial forces into velocity and position
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(ForceIntegrationSystemGroup))]
[UpdateAfter(typeof(ForceAccumulationSystemGroup))]
public partial struct SpatialForceIntegrationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        new IntegrateSpatialForcesJob
        {
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
partial struct IntegrateSpatialForcesJob : IJobEntity
{
    public float DeltaTime;

    void Execute(
        ref SpatialForceReceiver receiver,
        ref LocalTransform transform)
    {
        // F = ma -> a = F/m
        float3 acceleration = receiver.AccumulatedForce / math.max(0.001f, receiver.Mass);

        // Integrate acceleration into velocity
        receiver.Velocity += acceleration * DeltaTime;

        // Apply drag
        receiver.Velocity *= (1f - receiver.DragCoefficient * DeltaTime);

        // Integrate velocity into position
        transform.Position += receiver.Velocity * DeltaTime;
    }
}

/// <summary>
/// Integrates temporal forces into local time scale
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(ForceIntegrationSystemGroup))]
public partial struct TemporalForceIntegrationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Clear accumulated deltas
        new ClearTemporalDeltasJob().ScheduleParallel();

        // Apply temporal fields
        var temporalFields = SystemAPI.Query<RefRO<TemporalForceField>>()
            .Where(f => f.ValueRO.IsActive);

        foreach (var field in temporalFields)
        {
            new ApplyTemporalFieldJob { Field = field.ValueRO }.ScheduleParallel();
        }

        // Integrate accumulated deltas
        new IntegrateTemporalForcesJob().ScheduleParallel();
    }
}

[BurstCompile]
partial struct ClearTemporalDeltasJob : IJobEntity
{
    void Execute(ref TemporalForceReceiver receiver)
    {
        receiver.AccumulatedTimeScaleDelta = 0f;
    }
}

[BurstCompile]
partial struct ApplyTemporalFieldJob : IJobEntity
{
    [ReadOnly] public TemporalForceField Field;

    void Execute(
        ref TemporalForceReceiver receiver,
        in LocalTransform transform)
    {
        // Check layer mask
        if ((receiver.TemporalLayerMask & Field.TemporalLayer) == 0)
            return;

        // Calculate distance from field center
        float distance = math.distance(transform.Position, Field.Center);

        // Early out if outside outer radius
        if (distance > Field.OuterRadius)
            return;

        // Calculate blend factor
        float blend = 1f;
        if (distance > Field.InnerRadius)
        {
            float fadeRange = Field.OuterRadius - Field.InnerRadius;
            float fadeDistance = distance - Field.InnerRadius;
            blend = 1f - (fadeDistance / fadeRange);
        }

        // Calculate time scale delta
        float targetScale = Field.TimeScale;
        float currentScale = receiver.LocalTimeScale;
        float delta = (targetScale - currentScale) * blend;

        receiver.AccumulatedTimeScaleDelta += delta;
    }
}

[BurstCompile]
partial struct IntegrateTemporalForcesJob : IJobEntity
{
    void Execute(ref TemporalForceReceiver receiver)
    {
        // Apply accumulated delta
        receiver.LocalTimeScale += receiver.AccumulatedTimeScaleDelta;

        // Clamp to min/max
        receiver.LocalTimeScale = math.clamp(
            receiver.LocalTimeScale,
            receiver.TimeScaleClamp.x,
            receiver.TimeScaleClamp.y);
    }
}

/// <summary>
/// Integrates mass forces into effective mass
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(ForceIntegrationSystemGroup))]
public partial struct MassForceIntegrationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new IntegrateMassForcesJob().ScheduleParallel();
    }
}

[BurstCompile]
partial struct IntegrateMassForcesJob : IJobEntity
{
    void Execute(ref MassForceReceiver receiver)
    {
        // Apply accumulated mass delta
        receiver.EffectiveMass = receiver.BaseMass + receiver.AccumulatedMassDelta;

        // Clamp to positive values
        receiver.EffectiveMass = math.max(0.001f, receiver.EffectiveMass);

        // Reset accumulator
        receiver.AccumulatedMassDelta = 0f;
    }
}
```

---

## Runtime Flexibility

### 1. Dynamic Force Field Manipulation

```csharp
/// <summary>
/// Example: Create gravity well at runtime
/// </summary>
public void CreateGravityWell(EntityManager em, float3 position, float strength, float radius)
{
    Entity gravityWell = em.CreateEntity();

    em.AddComponentData(gravityWell, new RadialForceField
    {
        Center = position,
        Strength = strength,  // Positive = attraction
        Radius = radius,
        Falloff = FalloffType.InverseSquare,
        ForceLayer = 1u << 0,  // Layer 0 = gravity
        IsActive = true
    });

    // Optional: Add spatial indexing for optimization
    em.AddComponentData(gravityWell, new SpatialIndexedTag
    {
        CellIndex = SpatialGridUtility.GetCellIndex(position)
    });
}

/// <summary>
/// Example: Create temporal slow zone at runtime
/// </summary>
public void CreateSlowTimeZone(EntityManager em, float3 center, float timeScale, float radius)
{
    Entity timeZone = em.CreateEntity();

    em.AddComponentData(timeZone, new TemporalForceField
    {
        Center = center,
        TimeScale = timeScale,  // 0.5 = half speed
        InnerRadius = radius * 0.5f,
        OuterRadius = radius,
        TemporalLayer = 1u << 0,
        IsActive = true
    });
}

/// <summary>
/// Example: Toggle force field on/off at runtime
/// </summary>
public void ToggleForceField(EntityManager em, Entity fieldEntity, bool active)
{
    if (em.HasComponent<RadialForceField>(fieldEntity))
    {
        var field = em.GetComponentData<RadialForceField>(fieldEntity);
        field.IsActive = active;
        em.SetComponentData(fieldEntity, field);
    }
}

/// <summary>
/// Example: Modify force strength at runtime
/// </summary>
public void ModifyForceStrength(EntityManager em, Entity fieldEntity, float newStrength)
{
    if (em.HasComponent<RadialForceField>(fieldEntity))
    {
        var field = em.GetComponentData<RadialForceField>(fieldEntity);
        field.Strength = newStrength;
        em.SetComponentData(fieldEntity, field);
    }
}
```

### 2. Dynamic Spatial Grid Configuration

```csharp
/// <summary>
/// Runtime-configurable spatial grid settings
/// </summary>
public struct SpatialGridConfig : IComponentData
{
    /// <summary>
    /// Cell size in world units
    /// </summary>
    public float CellSize;

    /// <summary>
    /// Grid dimensions (cells)
    /// </summary>
    public int3 GridDimensions;

    /// <summary>
    /// World space origin
    /// </summary>
    public float3 Origin;

    /// <summary>
    /// Whether to use octree instead of uniform grid
    /// </summary>
    public bool UseOctree;

    /// <summary>
    /// Maximum octree depth
    /// </summary>
    public int OctreeMaxDepth;

    /// <summary>
    /// Can be modified at runtime (triggers rebuild)
    /// </summary>
    public bool IsDirty;
}

/// <summary>
/// Example: Reconfigure spatial grid at runtime
/// </summary>
[BurstCompile]
public partial struct ReconfigureSpatialGridSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var config in SystemAPI.Query<RefRW<SpatialGridConfig>>())
        {
            if (config.ValueRO.IsDirty)
            {
                // Rebuild spatial grid with new configuration
                RebuildSpatialGrid(ref state, config.ValueRO);

                config.ValueRW.IsDirty = false;
            }
        }
    }

    void RebuildSpatialGrid(ref SystemState state, SpatialGridConfig config)
    {
        // Implementation: Rebuild spatial index with new parameters
        // This is called when grid configuration changes at runtime
    }
}
```

### 3. Dynamic Physics Configuration

```csharp
/// <summary>
/// Runtime-configurable physics settings
/// </summary>
public struct PhysicsConfig : IComponentData
{
    /// <summary>
    /// Global gravity direction
    /// </summary>
    public float3 GravityDirection;

    /// <summary>
    /// Global gravity magnitude
    /// </summary>
    public float GravityMagnitude;

    /// <summary>
    /// Heightmap resolution
    /// </summary>
    public int2 HeightmapResolution;

    /// <summary>
    /// Heightmap world bounds
    /// </summary>
    public AABB HeightmapBounds;

    /// <summary>
    /// Physics backend (Entities Physics, Havok, Custom)
    /// </summary>
    public PhysicsBackendType Backend;

    /// <summary>
    /// Whether to use heightmap collision
    /// </summary>
    public bool UseHeightmapCollision;

    /// <summary>
    /// Can be modified at runtime (triggers rebuild)
    /// </summary>
    public bool IsDirty;
}

public enum PhysicsBackendType : byte
{
    EntitiesPhysics,
    Havok,
    Custom,
    None
}

/// <summary>
/// Heightmap-based terrain collision force
/// </summary>
public struct HeightmapCollisionForce : IComponentData
{
    /// <summary>
    /// Reference to heightmap blob
    /// </summary>
    public BlobAssetReference<HeightmapBlob> Heightmap;

    /// <summary>
    /// Collision response strength
    /// </summary>
    public float ResponseStrength;

    /// <summary>
    /// Whether collision is enabled
    /// </summary>
    public bool IsEnabled;
}

public struct HeightmapBlob
{
    /// <summary>
    /// Height samples (row-major order)
    /// </summary>
    public BlobArray<float> Heights;

    /// <summary>
    /// Resolution (width, height)
    /// </summary>
    public int2 Resolution;

    /// <summary>
    /// World bounds
    /// </summary>
    public AABB Bounds;
}

/// <summary>
/// System to apply heightmap collision forces
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(ForceAccumulationSystemGroup))]
public partial struct HeightmapCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var heightmapForce in SystemAPI.Query<RefRO<HeightmapCollisionForce>>())
        {
            if (!heightmapForce.ValueRO.IsEnabled)
                continue;

            new ApplyHeightmapCollisionJob
            {
                Heightmap = heightmapForce.ValueRO.Heightmap,
                ResponseStrength = heightmapForce.ValueRO.ResponseStrength
            }.ScheduleParallel();
        }
    }
}

[BurstCompile]
partial struct ApplyHeightmapCollisionJob : IJobEntity
{
    [ReadOnly] public BlobAssetReference<HeightmapBlob> Heightmap;
    public float ResponseStrength;

    void Execute(
        ref SpatialForceReceiver receiver,
        in LocalTransform transform)
    {
        ref var heightData = ref Heightmap.Value;

        // Check if position is within heightmap bounds
        if (!heightData.Bounds.Contains(transform.Position))
            return;

        // Sample heightmap
        float terrainHeight = SampleHeightmap(
            ref heightData,
            transform.Position.xz);

        // Calculate penetration
        float penetration = terrainHeight - transform.Position.y;

        // Apply collision response if penetrating
        if (penetration > 0f)
        {
            float3 responseForce = new float3(0, penetration * ResponseStrength, 0);
            receiver.AccumulatedForce += responseForce;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float SampleHeightmap(ref HeightmapBlob heightmap, float2 worldPos)
    {
        // Convert world position to heightmap UV
        float2 localPos = worldPos - heightmap.Bounds.Min.xz;
        float2 size = heightmap.Bounds.Max.xz - heightmap.Bounds.Min.xz;
        float2 uv = localPos / size;

        // Clamp to valid range
        uv = math.clamp(uv, float2.zero, new float2(1f, 1f));

        // Convert to texel coordinates
        float2 texel = uv * new float2(heightmap.Resolution.x - 1, heightmap.Resolution.y - 1);
        int2 texelInt = (int2)texel;
        float2 frac = math.frac(texel);

        // Bilinear interpolation
        int idx00 = texelInt.y * heightmap.Resolution.x + texelInt.x;
        int idx10 = texelInt.y * heightmap.Resolution.x + math.min(texelInt.x + 1, heightmap.Resolution.x - 1);
        int idx01 = math.min(texelInt.y + 1, heightmap.Resolution.y - 1) * heightmap.Resolution.x + texelInt.x;
        int idx11 = math.min(texelInt.y + 1, heightmap.Resolution.y - 1) * heightmap.Resolution.x + math.min(texelInt.x + 1, heightmap.Resolution.x - 1);

        float h00 = heightmap.Heights[idx00];
        float h10 = heightmap.Heights[idx10];
        float h01 = heightmap.Heights[idx01];
        float h11 = heightmap.Heights[idx11];

        float h0 = math.lerp(h00, h10, frac.x);
        float h1 = math.lerp(h01, h11, frac.x);

        return math.lerp(h0, h1, frac.y);
    }
}
```

---

## Integration with Existing PureDOTS Systems

### 1. Time Spine Integration

```csharp
/// <summary>
/// Applies local time scale to entity update rates
/// </summary>
[BurstCompile]
public partial struct TimeScaledUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var timeState = SystemAPI.GetSingleton<TimeState>();
        float globalDeltaTime = timeState.DeltaTime;

        // Update entities with local time scale
        foreach (var (receiver, entity) in
            SystemAPI.Query<RefRO<TemporalForceReceiver>>()
                .WithEntityAccess())
        {
            // Calculate effective delta time for this entity
            float localDeltaTime = globalDeltaTime * receiver.ValueRO.LocalTimeScale;

            // Store in component for systems to consume
            if (!state.EntityManager.HasComponent<LocalDeltaTime>(entity))
            {
                state.EntityManager.AddComponentData(entity, new LocalDeltaTime());
            }

            state.EntityManager.SetComponentData(entity, new LocalDeltaTime
            {
                Value = localDeltaTime
            });
        }
    }
}

/// <summary>
/// Local delta time for time-scaled entities
/// </summary>
public struct LocalDeltaTime : IComponentData
{
    public float Value;
}
```

### 2. Spatial Grid Integration

```csharp
/// <summary>
/// Updates spatial grid residency when entities move
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(ForceIntegrationSystemGroup))]
[UpdateAfter(typeof(SpatialForceIntegrationSystem))]
public partial struct UpdateSpatialResidencySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gridConfig = SystemAPI.GetSingleton<SpatialGridConfig>();

        foreach (var (transform, residency) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<SpatialGridResidency>>())
        {
            // Calculate new cell index
            int3 newCell = CalculateCellIndex(transform.ValueRO.Position, gridConfig);

            // Update if changed
            if (!newCell.Equals(residency.ValueRO.CellIndex))
            {
                residency.ValueRW.CellIndex = newCell;
                residency.ValueRW.IsDirty = true;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int3 CalculateCellIndex(float3 position, SpatialGridConfig config)
    {
        float3 localPos = position - config.Origin;
        int3 cellIndex = (int3)(localPos / config.CellSize);
        return math.clamp(cellIndex, int3.zero, config.GridDimensions - 1);
    }
}
```

### 3. Registry Integration

```csharp
/// <summary>
/// Sync force-affected entities to registries
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(RegistrySyncSystemGroup))]
public partial struct ForceSyncToRegistrySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var registryDirectory = SystemAPI.GetSingleton<RegistryDirectory>();

        // Update villager registry with force data
        if (registryDirectory.TryGetRegistry(RegistryTypeId.Villager, out var villagerRegistry))
        {
            foreach (var (spatial, temporal, entityId) in
                SystemAPI.Query<RefRO<SpatialForceReceiver>, RefRO<TemporalForceReceiver>>()
                    .WithAll<VillagerTag>()
                    .WithEntityAccess())
            {
                // Find registry entry
                int index = villagerRegistry.FindEntryIndex(entityId);
                if (index >= 0)
                {
                    ref var entry = ref villagerRegistry.GetEntry(index);

                    // Store velocity for AI to consume
                    entry.Velocity = spatial.ValueRO.Velocity;

                    // Store time scale for scheduling adjustments
                    entry.LocalTimeScale = temporal.ValueRO.LocalTimeScale;
                }
            }
        }
    }
}
```

---

## Performance Considerations

### 1. Spatial Partitioning for Force Fields

```csharp
/// <summary>
/// Spatially partition force fields to avoid checking all entities against all fields
/// </summary>
public struct ForceFieldSpatialIndex : IComponentData
{
    /// <summary>
    /// Grid cells this force field affects
    /// </summary>
    public BlobAssetReference<BlobArray<int3>> AffectedCells;
}

/// <summary>
/// Build spatial index for force fields
/// </summary>
[BurstCompile]
public partial struct BuildForceFieldSpatialIndexSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gridConfig = SystemAPI.GetSingleton<SpatialGridConfig>();

        // Rebuild index when force fields change
        foreach (var (field, entity) in
            SystemAPI.Query<RefRO<RadialForceField>>()
                .WithNone<ForceFieldSpatialIndex>()
                .WithEntityAccess())
        {
            var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobArray<int3>>();

            // Calculate affected cells
            var affectedCells = CalculateAffectedCells(
                field.ValueRO.Center,
                field.ValueRO.Radius,
                gridConfig);

            var cellArray = builder.Allocate(ref root, affectedCells.Length);
            for (int i = 0; i < affectedCells.Length; i++)
            {
                cellArray[i] = affectedCells[i];
            }

            var blobAsset = builder.CreateBlobAssetReference<BlobArray<int3>>(Unity.Collections.Allocator.Persistent);
            builder.Dispose();

            state.EntityManager.AddComponentData(entity, new ForceFieldSpatialIndex
            {
                AffectedCells = blobAsset
            });
        }
    }

    static NativeList<int3> CalculateAffectedCells(float3 center, float radius, SpatialGridConfig config)
    {
        var cells = new NativeList<int3>(Unity.Collections.Allocator.Temp);

        // Calculate bounding box in cell space
        int3 minCell = (int3)math.floor((center - radius - config.Origin) / config.CellSize);
        int3 maxCell = (int3)math.ceil((center + radius - config.Origin) / config.CellSize);

        // Clamp to grid bounds
        minCell = math.clamp(minCell, int3.zero, config.GridDimensions - 1);
        maxCell = math.clamp(maxCell, int3.zero, config.GridDimensions - 1);

        // Add all cells in range
        for (int z = minCell.z; z <= maxCell.z; z++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    cells.Add(new int3(x, y, z));
                }
            }
        }

        return cells;
    }
}
```

### 2. Layer Masking for Selective Forces

```csharp
/// <summary>
/// Example force layer definitions
/// </summary>
public static class ForceLayers
{
    public const uint Gravity = 1u << 0;
    public const uint Wind = 1u << 1;
    public const uint Magnetism = 1u << 2;
    public const uint Temporal = 1u << 3;
    public const uint Divine = 1u << 4;
    public const uint Terrain = 1u << 5;
    public const uint Fluid = 1u << 6;
    public const uint Cosmic = 1u << 7;

    // Presets
    public const uint Physical = Gravity | Wind | Terrain | Fluid;
    public const uint Magical = Magnetism | Divine | Cosmic;
    public const uint All = 0xFFFFFFFF;
}

/// <summary>
/// Example: Make entity immune to gravity but affected by wind
/// </summary>
public void ConfigureForceReception(EntityManager em, Entity entity)
{
    em.SetComponentData(entity, new SpatialForceReceiver
    {
        Mass = 1f,
        ForceLayerMask = ForceLayers.Wind | ForceLayers.Magnetism,  // No gravity
        DragCoefficient = 0.1f
    });
}
```

### 3. LOD for Force Calculations

```csharp
/// <summary>
/// Force LOD component - reduces update frequency for distant entities
/// </summary>
public struct ForceLOD : IComponentData
{
    /// <summary>
    /// LOD level (0 = full update, 1 = half rate, 2 = quarter rate)
    /// </summary>
    public byte LODLevel;

    /// <summary>
    /// Frame counter for staggered updates
    /// </summary>
    public ushort FrameCounter;
}

/// <summary>
/// Update LOD levels based on distance to camera
/// </summary>
[BurstCompile]
public partial struct UpdateForceLODSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get camera position (simplified - actual implementation would query camera entity)
        float3 cameraPos = float3.zero;

        foreach (var (transform, lod) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<ForceLOD>>())
        {
            float distance = math.distance(transform.ValueRO.Position, cameraPos);

            // Assign LOD level
            lod.ValueRW.LODLevel = distance switch
            {
                < 50f => 0,   // Full rate
                < 100f => 1,  // Half rate
                < 200f => 2,  // Quarter rate
                _ => 3        // Very slow rate
            };
        }
    }
}

/// <summary>
/// Skip force accumulation for entities not on their update frame
/// </summary>
[BurstCompile]
partial struct ApplyRadialForceLODJob : IJobEntity
{
    [ReadOnly] public RadialForceField Field;
    public uint FrameCount;

    void Execute(
        ref SpatialForceReceiver receiver,
        ref ForceLOD lod,
        in LocalTransform transform)
    {
        // Check if this entity should update this frame
        uint updateInterval = 1u << lod.LODLevel;  // 1, 2, 4, 8
        if ((FrameCount + lod.FrameCounter) % updateInterval != 0)
            return;

        // ... normal force application
    }
}
```

---

## Example Use Cases

### 1. Black Hole (Godgame)

```csharp
public void CreateBlackHole(EntityManager em, float3 position)
{
    Entity blackHole = em.CreateEntity();

    // Spatial gravity
    em.AddComponentData(blackHole, new RadialForceField
    {
        Center = position,
        Strength = 1000f,  // Strong attraction
        Radius = 50f,
        Falloff = FalloffType.InverseSquare,
        ForceLayer = ForceLayers.Gravity | ForceLayers.Divine,
        IsActive = true
    });

    // Temporal distortion (time slows near center)
    em.AddComponentData(blackHole, new TemporalForceField
    {
        Center = position,
        TimeScale = 0.1f,  // 10% speed near center
        InnerRadius = 10f,
        OuterRadius = 50f,
        TemporalLayer = ForceLayers.Temporal,
        IsActive = true
    });
}
```

### 2. Wind Storm (Godgame)

```csharp
public void CreateWindStorm(EntityManager em, AABB bounds, float3 windDirection)
{
    Entity storm = em.CreateEntity();

    em.AddComponentData(storm, new DirectionalForceField
    {
        Direction = math.normalize(windDirection),
        Strength = 20f,
        Bounds = bounds,
        ForceLayer = ForceLayers.Wind,
        IsActive = true
    });

    // Add turbulence with multiple vortices
    for (int i = 0; i < 5; i++)
    {
        Entity vortex = em.CreateEntity();
        float3 vortexPos = bounds.Center + UnityEngine.Random.insideUnitSphere * bounds.Extents.x;

        em.AddComponentData(vortex, new VortexForceField
        {
            AxisCenter = vortexPos,
            AxisDirection = new float3(0, 1, 0),
            TangentialStrength = 15f,
            RadialStrength = -5f,
            AxialStrength = 0f,
            Radius = 10f,
            ForceLayer = ForceLayers.Wind,
            IsActive = true
        });
    }
}
```

### 3. Warp Drive Bubble (Space4X)

```csharp
public void CreateWarpBubble(EntityManager em, Entity ship, float warpFactor)
{
    // Create temporal field that moves with ship
    Entity warpField = em.CreateEntity();

    em.AddComponentData(warpField, new TemporalForceField
    {
        Center = float3.zero,  // Will be updated to follow ship
        TimeScale = warpFactor,  // 2.0 = double speed
        InnerRadius = 5f,
        OuterRadius = 10f,
        TemporalLayer = ForceLayers.Cosmic,
        IsActive = true
    });

    // Add parent relationship so field follows ship
    em.AddComponentData(warpField, new Parent { Value = ship });
}
```

### 4. Buoyancy in Water (Both games)

```csharp
public void ConfigureWaterBuoyancy(EntityManager em, float waterLevel, AABB waterBounds)
{
    Entity buoyancyField = em.CreateEntity();

    em.AddComponentData(buoyancyField, new HeightForceField
    {
        ReferenceHeight = waterLevel,
        StrengthPerHeight = 50f,  // Strong upward force below water
        Direction = new float3(0, 1, 0),
        Bounds = waterBounds,
        ForceLayer = ForceLayers.Fluid,
        IsActive = true
    });

    // Add drag for entities in water
    Entity dragField = em.CreateEntity();
    em.AddComponentData(dragField, new DirectionalForceField
    {
        Direction = float3.zero,  // No specific direction (just drag)
        Strength = 0f,  // Drag handled via receiver component
        Bounds = waterBounds,
        ForceLayer = ForceLayers.Fluid,
        IsActive = true
    });
}
```

---

## Testing & Validation

### 1. Determinism Tests

```csharp
[Test]
public void ForceApplication_IsDeterministic()
{
    // Setup identical scenarios
    var world1 = CreateTestWorld();
    var world2 = CreateTestWorld();

    // Apply same forces
    ApplyTestForces(world1);
    ApplyTestForces(world2);

    // Run simulation
    for (int i = 0; i < 100; i++)
    {
        world1.Update();
        world2.Update();
    }

    // Verify identical results
    AssertWorldsIdentical(world1, world2);
}
```

### 2. Performance Benchmarks

```csharp
[Test]
[Performance]
public void ForceSystem_Performance_10KEntities_100Fields()
{
    var world = CreateTestWorld();

    // Spawn 10k entities
    for (int i = 0; i < 10000; i++)
    {
        CreateForceReceiver(world);
    }

    // Create 100 force fields
    for (int i = 0; i < 100; i++)
    {
        CreateRandomForceField(world);
    }

    // Measure update time
    Measure.Method(() => world.Update())
        .WarmupCount(10)
        .MeasurementCount(100)
        .Run();
}
```

---

## Related Documentation

- **Time Spine Integration**: `Docs/Architecture/TimeSystem.md`
- **Spatial Grid**: `Docs/Architecture/SpatialGrid.md`
- **Physics Backend**: `Docs/Architecture/PhysicsBackend.md`
- **Burst Optimization**: `Docs/BestPractices/BurstOptimization.md`
- **Entity Agnostic Design**: `Docs/Concepts/Core/Entity_Agnostic_Design.md`

---

## Future Enhancements

1. **GPU Acceleration**: Offload force calculations to compute shaders for 100k+ entities
2. **Force Field Blending**: Smooth transitions between overlapping fields
3. **Conditional Forces**: Triggers based on entity state (alignment, health, etc.)
4. **Force Recording**: Capture force field state for replay/rewind
5. **Visual Debugging**: Runtime visualization of force field influence

---

**Last Updated:** 2025-12-18
**Status:** Design Document - Core Architecture
**Burst Compatible:** Yes
**Deterministic:** Yes
**Runtime Flexible:** Yes
