# Physics vs Spatial Grid Guidelines

This guide explains when to use Unity Physics vs PureDOTS spatial grid for entity interactions, with specific recommendations for Space4X and Godgame workloads.

## Overview

PureDOTS provides two primary mechanisms for entity interactions:

| Mechanism | Performance | Use Case |
|-----------|-------------|----------|
| **Spatial Grid** | ~0.01ms per query | Default for all interactions |
| **Unity Physics** | ~0.1ms per body | Spectacle moments only |

**Rule of Thumb**: Use physics for < 1% of entities (spectacle only).

## Default Strategy

1. **Start with spatial grid / distance checks** - Fast, deterministic, scalable
2. **Only add physics when**:
   - Player-visible spectacle requires realistic motion
   - Gameplay mechanics depend on physics (pushing, momentum, rotation)
   - Visual feedback needs realistic collision response

## Component Usage

### Spatial Grid (Default)

```csharp
// Most entities use spatial grid implicitly
// Add UsesSpatialGrid for explicit configuration
public struct UsesSpatialGrid : IComponentData
{
    public float QueryRadius;
    public SpatialQueryFlags Flags;
}
```

### Physics (Opt-In)

```csharp
// Only add when physics is truly needed
public struct RequiresPhysics : IComponentData
{
    public byte Priority;
    public PhysicsInteractionFlags Flags;
}
```

### Ballistic Motion (No Physics)

```csharp
// For thrown objects without full physics
public struct BallisticMotion : IComponentData
{
    public float3 Velocity;
    public float3 Gravity;
    public float FlightTime;
    // ...
}
```

## Space4X Guidelines

### Projectiles

| Scenario | Recommendation | Reason |
|----------|----------------|--------|
| Standard weapons | Spatial grid raycast | Fast, deterministic |
| Player-fired missiles | Ballistic motion | Visual trajectory without physics |
| Interceptable torpedoes | RequiresPhysics | Needs collision response |
| Beam weapons | Spatial grid raycast | Instant hit, no travel |

**Implementation**:
```csharp
// Default projectile: spatial grid raycast (hitscan)
// Use SpatialGridSystem.QueryRaycast() for hit detection

// Spectacle projectile: ballistic motion
// Calculate trajectory on fire, update position each tick
float3 velocity = PhysicsInteractionHelpers.CalculateBallisticArc(
    startPos, targetPos, gravity, flightTime);
```

### Ship/Asteroid Interactions

| Scenario | Recommendation | Reason |
|----------|----------------|--------|
| Combat range checks | Spatial grid overlap | Fast broadphase |
| Mining proximity | Distance check | Simple, deterministic |
| Collision avoidance | Spatial grid | Steering, not physics |
| Ship ramming | RequiresPhysics | Needs momentum transfer |

**Implementation**:
```csharp
// Range check: spatial grid
var nearbyShips = SpatialGridSystem.QueryOverlap(position, range);

// Collision avoidance: steering behaviors
var avoidanceVector = CalculateAvoidance(nearbyShips);
```

### Carrier Docking

| Scenario | Recommendation | Reason |
|----------|----------------|--------|
| Docking approach | Distance check | Logical state change |
| Docking alignment | Angle check | No physics needed |
| Docking collision | RequiresPhysics (rare) | Only if visual precision needed |

## Godgame Guidelines

### Villager Hand Logic

| Action | Recommendation | Reason |
|--------|----------------|--------|
| Picking up | Spatial grid query | Distance check only |
| Holding | Logical state | No physics needed |
| Throwing | Ballistic motion | Pre-computed arc |
| Catching | Spatial grid + state | No physics collision |

**Implementation**:
```csharp
// Picking up: spatial grid distance check
bool canPickup = distance < pickupRange;

// Throwing: calculate ballistic arc
float3 velocity = PhysicsInteractionHelpers.CalculateBallisticArc(
    handPosition, targetPosition, -9.81f, flightTime);
chunk.Velocity = velocity;
chunk.Flags |= ResourceChunkFlags.Thrown;

// Update each tick (no physics body)
PhysicsInteractionHelpers.UpdateBallisticPosition(
    ref position, ref velocity, gravity, deltaTime);
```

### Object Landing

| Scenario | Recommendation | Reason |
|----------|----------------|--------|
| Ground collision | Terrain heightmap | No physics needed |
| Breaking on impact | Logical check | Velocity threshold |
| Bouncing | Ballistic motion | Simple reflection |
| Rolling | RequiresPhysics (rare) | Only for visual spectacle |

**Implementation**:
```csharp
// Ground collision: heightmap lookup
float groundHeight = GetTerrainHeight(position.xz);
if (position.y <= groundHeight)
{
    // Landed - stop motion
    velocity = float3.zero;
    chunk.Flags &= ~ResourceChunkFlags.Thrown;
    
    // Check if should break
    if (math.lengthsq(preImpactVelocity) > breakThreshold)
    {
        SpawnFragments(chunk);
    }
}
```

### Thrown Objects & Miracles

| Scenario | Recommendation | Reason |
|----------|----------------|--------|
| God hand throw | Ballistic motion | Dramatic but deterministic |
| Miracle effects | Spatial grid | Area-of-effect |
| Object stacking | Logical placement | No physics stacking |
| Spectacular destruction | RequiresPhysics | Visual payoff justifies cost |

## Performance Comparison

### At 100k Entities

| Operation | Spatial Grid | Physics |
|-----------|-------------|---------|
| Query/Update | ~1ms total | ~10ms total |
| Memory | ~10MB | ~50MB |
| Determinism | Fully deterministic | Requires fixed step |
| Scalability | Excellent | Limited |

### Budget Guidelines

| Entity Count | Max Physics Bodies | Reason |
|--------------|-------------------|--------|
| 10k | 100 | 1% budget |
| 100k | 500 | 0.5% budget |
| 1M+ | 1000 | 0.1% budget |

## Pooling Strategy

### Projectiles
- Pool entities, reuse (don't create/destroy per frame)
- Toggle `Active` flag instead of destroy
- Reset position and velocity on reuse

### Resource Chunks
- Use `PendingDestroy` flag instead of immediate destruction
- Batch destruction in cleanup system
- Pool chunk entities for frequent spawn/destroy

### Physics Bodies
- Add/remove `RequiresPhysics` component, don't destroy entity
- Pool physics-enabled entities separately
- Disable physics when not needed (remove component)

## Decision Flowchart

```
Is this interaction visible to the player?
├── No → Use Spatial Grid
└── Yes
    ├── Does it need realistic physics response?
    │   ├── No → Use Ballistic Motion or Spatial Grid
    │   └── Yes
    │       ├── Is it a rare/special moment?
    │       │   ├── Yes → Use RequiresPhysics
    │       │   └── No → Consider simplifying to Ballistic Motion
    │       └── Would simplified physics look acceptable?
    │           ├── Yes → Use Ballistic Motion
    │           └── No → Use RequiresPhysics (budget permitting)
```

## Components Reference

| Component | File | Purpose |
|-----------|------|---------|
| `UsesSpatialGrid` | `PhysicsInteractionComponents.cs` | Spatial grid configuration |
| `RequiresPhysics` | `PhysicsInteractionComponents.cs` | Physics opt-in |
| `BallisticMotion` | `PhysicsInteractionComponents.cs` | Simplified projectile motion |
| `GroundCollisionCheck` | `PhysicsInteractionComponents.cs` | Terrain collision |
| `PhysicsInteractionConfig` | `PhysicsInteractionComponents.cs` | Physics parameters |

## See Also

- `Docs/PERFORMANCE_PLAN.md` - Overall performance strategy
- `Docs/Guides/PerformanceIntegrationRoadmap.md` - Integration roadmap and contracts
- `Docs/Guides/SpatialQueryUsage.md` - Spatial grid API usage
- `Packages/com.moni.puredots/Runtime/Runtime/Physics/` - Component definitions

