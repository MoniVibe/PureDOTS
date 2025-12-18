# Bay and Platform Combat System

**Last Updated:** 2025-12-18
**Status:** Design Document - Tactical Combat Positioning
**Burst Compatible:** Yes
**Deterministic:** Yes
**Entity-Agnostic:** Yes (works for carriers, ships, vehicles, fortifications)

---

## Overview

The **Bay and Platform Combat System** enables parent entities (carriers, ships, fortifications) to provide **combat positions** for child entities (mechs, titans, crew, turrets) that can attack from protected or advantageous positions. This creates tactical depth through:

- **Space4X**: Carriers open hangar bays to deploy mechs/titans that attack from the carrier's hull
- **Godgame**: Ships have broadside platforms where crews man cannons with specific firing arcs
- **Both**: Fortifications, mobile fortresses, and other vehicles gain combat effectiveness through positioned attackers

---

## Core Concepts

### 1. Combat Positions (Bays/Platforms)

Parent entities have **combat positions** that child entities can occupy:

```
CARRIER (Space4X)
├─ Hangar Bay 1 (Ventral): 4 Mech slots, firing arc 270° downward
├─ Hangar Bay 2 (Port): 2 Titan slots, firing arc 180° left
└─ Hangar Bay 3 (Starboard): 2 Titan slots, firing arc 180° right

GALLEON (Godgame)
├─ Port Broadside: 8 Cannon positions, firing arc 120° left
├─ Starboard Broadside: 8 Cannon positions, firing arc 120° right
├─ Bow Chaser: 2 Cannon positions, firing arc 90° forward
└─ Stern Chaser: 2 Cannon positions, firing arc 90° backward

FORTRESS (Godgame)
├─ North Wall: 12 Archer positions, firing arc 180° north
├─ East Wall: 12 Archer positions, firing arc 180° east
├─ South Wall: 12 Archer positions, firing arc 180° south
└─ West Wall: 12 Archer positions, firing arc 180° west
```

### 2. Firing Arcs

Each position has a **firing arc** that defines valid target angles:

```csharp
public struct FiringArc
{
    /// <summary>
    /// Center direction of the arc (relative to parent entity's forward)
    /// </summary>
    public float3 CenterDirection;

    /// <summary>
    /// Arc width in radians (e.g., PI = 180°, PI/2 = 90°)
    /// </summary>
    public float ArcWidthRadians;

    /// <summary>
    /// Minimum range (dead zone)
    /// </summary>
    public float MinRange;

    /// <summary>
    /// Maximum effective range
    /// </summary>
    public float MaxRange;

    /// <summary>
    /// Whether this arc has elevation limits (important for ground combat)
    /// </summary>
    public float MinElevation;  // -PI/2 to PI/2
    public float MaxElevation;
}
```

### 3. Bay States

Bays/platforms can be in different operational states:

```csharp
public enum BayState : byte
{
    Closed = 0,      // Bays sealed, no attacks possible
    Opening = 1,     // Transitioning (X seconds to open)
    Open = 2,        // Ready for combat
    Closing = 3,     // Transitioning (X seconds to close)
    Damaged = 4,     // Cannot open/close
    Destroyed = 5    // Inoperable
}
```

---

## Component Architecture

### Combat Position Definition

```csharp
/// <summary>
/// Defines a combat position (bay/platform/mount) on a parent entity
/// </summary>
public struct CombatPosition : IBufferElementData
{
    /// <summary>
    /// Unique ID for this position on the parent entity
    /// </summary>
    public FixedString32Bytes PositionId;

    /// <summary>
    /// Local offset from parent entity center
    /// </summary>
    public float3 LocalOffset;

    /// <summary>
    /// Firing arc definition
    /// </summary>
    public FiringArc Arc;

    /// <summary>
    /// Current operational state
    /// </summary>
    public BayState State;

    /// <summary>
    /// Transition progress (0-1) for opening/closing
    /// </summary>
    public float TransitionProgress;

    /// <summary>
    /// Time required to open/close (seconds)
    /// </summary>
    public float TransitionDuration;

    /// <summary>
    /// Maximum occupants this position can hold
    /// </summary>
    public byte MaxOccupants;

    /// <summary>
    /// Current occupant count
    /// </summary>
    public byte CurrentOccupants;

    /// <summary>
    /// Structural health of this position (0-1)
    /// </summary>
    public float Health;

    /// <summary>
    /// Layer mask for what this position can attack
    /// (e.g., anti-fighter bays vs anti-capital bays)
    /// </summary>
    public uint TargetLayerMask;
}
```

### Combat Position Occupancy

```csharp
/// <summary>
/// Marks an entity as occupying a combat position on a parent
/// </summary>
public struct CombatPositionOccupant : IComponentData
{
    /// <summary>
    /// Parent entity providing the combat position
    /// </summary>
    public Entity ParentEntity;

    /// <summary>
    /// Which position on the parent this entity occupies
    /// </summary>
    public FixedString32Bytes PositionId;

    /// <summary>
    /// Index within that position (for multi-occupant positions)
    /// </summary>
    public byte SlotIndex;

    /// <summary>
    /// Whether this occupant is ready to fire
    /// </summary>
    public bool IsReady;

    /// <summary>
    /// Current target (if any)
    /// </summary>
    public Entity CurrentTarget;

    /// <summary>
    /// Attack cooldown timer
    /// </summary>
    public float CooldownRemaining;
}
```

### Combat Position Provider

```csharp
/// <summary>
/// Tag component indicating this entity provides combat positions
/// </summary>
public struct CombatPositionProvider : IComponentData
{
    /// <summary>
    /// Total positions available
    /// </summary>
    public byte TotalPositions;

    /// <summary>
    /// Total positions currently occupied
    /// </summary>
    public byte OccupiedPositions;

    /// <summary>
    /// Whether automatic bay/platform management is enabled
    /// </summary>
    public bool AutoManageBays;

    /// <summary>
    /// Auto-open bays when hostiles detected within range
    /// </summary>
    public bool AutoOpenOnThreat;

    /// <summary>
    /// Auto-close bays when no hostiles within range
    /// </summary>
    public bool AutoCloseOnSafe;
}
```

---

## System Implementation

### 1. Bay State Management System

```csharp
/// <summary>
/// Manages bay/platform state transitions (opening/closing)
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(CombatSystemsGroup))]
public partial struct BayStateManagementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (positionBuffer, provider) in
            SystemAPI.Query<
                DynamicBuffer<CombatPosition>,
                RefRO<CombatPositionProvider>>())
        {
            for (int i = 0; i < positionBuffer.Length; i++)
            {
                var position = positionBuffer[i];

                // Update transition progress
                if (position.State == BayState.Opening || position.State == BayState.Closing)
                {
                    position.TransitionProgress += deltaTime / position.TransitionDuration;

                    // Complete transition
                    if (position.TransitionProgress >= 1.0f)
                    {
                        position.TransitionProgress = 1.0f;
                        position.State = position.State == BayState.Opening
                            ? BayState.Open
                            : BayState.Closed;
                    }

                    positionBuffer[i] = position;
                }
            }
        }
    }
}
```

### 2. Auto Bay Management System

```csharp
/// <summary>
/// Automatically opens/closes bays based on threat detection
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(CombatSystemsGroup))]
public partial struct AutoBayManagementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var spatialGrid = SystemAPI.GetSingleton<SpatialGridState>();

        foreach (var (positionBuffer, provider, transform, entity) in
            SystemAPI.Query<
                DynamicBuffer<CombatPosition>,
                RefRO<CombatPositionProvider>,
                RefRO<LocalTransform>>()
            .WithEntityAccess())
        {
            if (!provider.ValueRO.AutoManageBays)
                continue;

            // Check for threats within range
            bool threatsDetected = false;

            for (int i = 0; i < positionBuffer.Length; i++)
            {
                var position = positionBuffer[i];

                // Query spatial grid for hostiles within arc
                var hostiles = QueryHostilesInArc(
                    spatialGrid,
                    transform.ValueRO.Position,
                    transform.ValueRO.Rotation,
                    position);

                if (hostiles.Length > 0)
                {
                    threatsDetected = true;

                    // Open this bay if closed
                    if (provider.ValueRO.AutoOpenOnThreat &&
                        position.State == BayState.Closed)
                    {
                        position.State = BayState.Opening;
                        position.TransitionProgress = 0f;
                        positionBuffer[i] = position;
                    }
                }

                hostiles.Dispose();
            }

            // Close all bays if no threats and auto-close enabled
            if (!threatsDetected && provider.ValueRO.AutoCloseOnSafe)
            {
                for (int i = 0; i < positionBuffer.Length; i++)
                {
                    var position = positionBuffer[i];
                    if (position.State == BayState.Open)
                    {
                        position.State = BayState.Closing;
                        position.TransitionProgress = 0f;
                        positionBuffer[i] = position;
                    }
                }
            }
        }
    }

    [BurstCompile]
    static NativeList<Entity> QueryHostilesInArc(
        SpatialGridState spatialGrid,
        float3 parentPosition,
        quaternion parentRotation,
        CombatPosition position)
    {
        var hostiles = new NativeList<Entity>(16, Allocator.Temp);

        // Get world-space arc direction
        float3 arcWorldDir = math.mul(parentRotation, position.Arc.CenterDirection);

        // Query spatial grid for entities in range
        var nearby = spatialGrid.QueryRadius(parentPosition, position.Arc.MaxRange);

        foreach (var candidate in nearby)
        {
            // Check if in arc
            float3 toCandidate = candidate.Position - parentPosition;
            float distance = math.length(toCandidate);

            if (distance < position.Arc.MinRange || distance > position.Arc.MaxRange)
                continue;

            // Check angle
            float3 dirToCandidate = toCandidate / distance;
            float angle = math.acos(math.dot(arcWorldDir, dirToCandidate));

            if (angle <= position.Arc.ArcWidthRadians * 0.5f)
            {
                hostiles.Add(candidate.Entity);
            }
        }

        nearby.Dispose();
        return hostiles;
    }
}
```

### 3. Combat Position Attack System

```csharp
/// <summary>
/// Routes attacks from occupants through their combat positions
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(CombatSystemsGroup))]
public partial struct CombatPositionAttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (occupant, weapon, transform, entity) in
            SystemAPI.Query<
                RefRW<CombatPositionOccupant>,
                RefRO<WeaponStats>,
                RefRO<LocalTransform>>()
            .WithEntityAccess())
        {
            // Update cooldown
            if (occupant.ValueRO.CooldownRemaining > 0)
            {
                occupant.ValueRW.CooldownRemaining -= deltaTime;
                continue;
            }

            // Get parent entity's position buffer
            if (!state.EntityManager.Exists(occupant.ValueRO.ParentEntity))
                continue;

            var parentPositions = state.EntityManager.GetBuffer<CombatPosition>(
                occupant.ValueRO.ParentEntity);
            var parentTransform = state.EntityManager.GetComponentData<LocalTransform>(
                occupant.ValueRO.ParentEntity);

            // Find this occupant's position
            CombatPosition myPosition = default;
            bool found = false;
            for (int i = 0; i < parentPositions.Length; i++)
            {
                if (parentPositions[i].PositionId.Equals(occupant.ValueRO.PositionId))
                {
                    myPosition = parentPositions[i];
                    found = true;
                    break;
                }
            }

            if (!found || myPosition.State != BayState.Open)
            {
                occupant.ValueRW.IsReady = false;
                continue;
            }

            occupant.ValueRW.IsReady = true;

            // Find valid target in firing arc
            Entity target = FindTargetInArc(
                state,
                parentTransform.Position,
                parentTransform.Rotation,
                myPosition,
                occupant.ValueRO.CurrentTarget);

            if (target != Entity.Null)
            {
                occupant.ValueRW.CurrentTarget = target;

                // Fire weapon
                ecb.AddComponent(entity, new AttackCommand
                {
                    Target = target,
                    Damage = weapon.ValueRO.Damage,
                    AttackType = weapon.ValueRO.AttackType
                });

                // Reset cooldown
                occupant.ValueRW.CooldownRemaining = weapon.ValueRO.AttackCooldown;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    static Entity FindTargetInArc(
        SystemState state,
        float3 parentPosition,
        quaternion parentRotation,
        CombatPosition position,
        Entity currentTarget)
    {
        // Prefer current target if still valid
        if (currentTarget != Entity.Null &&
            state.EntityManager.Exists(currentTarget))
        {
            var targetPos = state.EntityManager.GetComponentData<LocalTransform>(currentTarget).Position;
            if (IsInArc(parentPosition, parentRotation, position, targetPos))
            {
                return currentTarget;
            }
        }

        // Find new target (simplified - use spatial grid in production)
        // ... (target acquisition logic)

        return Entity.Null;
    }

    [BurstCompile]
    static bool IsInArc(
        float3 parentPosition,
        quaternion parentRotation,
        CombatPosition position,
        float3 targetPosition)
    {
        float3 arcWorldDir = math.mul(parentRotation, position.Arc.CenterDirection);
        float3 toTarget = targetPosition - parentPosition;
        float distance = math.length(toTarget);

        // Range check
        if (distance < position.Arc.MinRange || distance > position.Arc.MaxRange)
            return false;

        // Angle check
        float3 dirToTarget = toTarget / distance;
        float angle = math.acos(math.dot(arcWorldDir, dirToTarget));

        return angle <= position.Arc.ArcWidthRadians * 0.5f;
    }
}
```

### 4. Combat Position Assignment System

```csharp
/// <summary>
/// Assigns occupants to optimal combat positions based on current threats
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(CombatSystemsGroup))]
public partial struct CombatPositionAssignmentSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Find unassigned occupants (mechs in carrier, crew on ship)
        foreach (var (occupant, parent, entity) in
            SystemAPI.Query<
                RefRW<CombatPositionOccupant>,
                RefRO<Parent>>()
            .WithEntityAccess()
            .WithNone<AssignedPosition>())  // Tag for assigned occupants
        {
            // Get parent's position buffer
            if (!state.EntityManager.HasBuffer<CombatPosition>(parent.ValueRO.Value))
                continue;

            var positions = state.EntityManager.GetBuffer<CombatPosition>(parent.ValueRO.Value);

            // Find best available position
            int bestPositionIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0; i < positions.Length; i++)
            {
                var pos = positions[i];

                // Skip if full or not operational
                if (pos.CurrentOccupants >= pos.MaxOccupants ||
                    pos.State == BayState.Damaged ||
                    pos.State == BayState.Destroyed)
                    continue;

                // Score this position based on:
                // - Threat coverage (how many hostiles in arc)
                // - Position health
                // - Arc width (prefer wider arcs for flexibility)
                float score = ScorePosition(state, parent.ValueRO.Value, pos);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPositionIndex = i;
                }
            }

            // Assign to best position
            if (bestPositionIndex >= 0)
            {
                var bestPos = positions[bestPositionIndex];

                occupant.ValueRW.ParentEntity = parent.ValueRO.Value;
                occupant.ValueRW.PositionId = bestPos.PositionId;
                occupant.ValueRW.SlotIndex = bestPos.CurrentOccupants;

                // Update position occupancy
                bestPos.CurrentOccupants++;
                positions[bestPositionIndex] = bestPos;

                // Mark as assigned
                ecb.AddComponent<AssignedPosition>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    static float ScorePosition(
        SystemState state,
        Entity parentEntity,
        CombatPosition position)
    {
        float score = 0f;

        // Health factor (0-1)
        score += position.Health * 20f;

        // Arc width factor (wider is better for flexibility)
        score += (position.Arc.ArcWidthRadians / math.PI) * 10f;

        // Threat coverage (how many hostiles in arc)
        // ... (count hostiles in arc using spatial grid)

        return score;
    }
}

/// <summary>
/// Tag component for assigned occupants
/// </summary>
public struct AssignedPosition : IComponentData { }
```

---

## Example Scenarios

### Space4X: Carrier Mech Deployment

```csharp
// CARRIER ENTITY
Entity carrier = em.CreateEntity();

// Add combat positions (hangar bays)
var bayBuffer = em.AddBuffer<CombatPosition>(carrier);

// Ventral bay (bottom-facing)
bayBuffer.Add(new CombatPosition
{
    PositionId = "VentralBay",
    LocalOffset = new float3(0, -10, 0),
    Arc = new FiringArc
    {
        CenterDirection = new float3(0, -1, 0),  // Downward
        ArcWidthRadians = math.PI * 1.5f,        // 270° arc
        MinRange = 10f,
        MaxRange = 500f
    },
    State = BayState.Closed,
    TransitionDuration = 3.0f,  // 3 seconds to open
    MaxOccupants = 4,
    Health = 1.0f,
    TargetLayerMask = LayerMask.Fighters | LayerMask.Corvettes
});

// Port bay (left-facing)
bayBuffer.Add(new CombatPosition
{
    PositionId = "PortBay",
    LocalOffset = new float3(-15, 0, 0),
    Arc = new FiringArc
    {
        CenterDirection = new float3(-1, 0, 0),  // Left
        ArcWidthRadians = math.PI,               // 180° arc
        MinRange = 20f,
        MaxRange = 800f
    },
    State = BayState.Closed,
    TransitionDuration = 5.0f,  // 5 seconds to open (larger bay)
    MaxOccupants = 2,
    Health = 1.0f,
    TargetLayerMask = LayerMask.Capitals
});

em.AddComponentData(carrier, new CombatPositionProvider
{
    TotalPositions = 2,
    OccupiedPositions = 0,
    AutoManageBays = true,
    AutoOpenOnThreat = true,
    AutoCloseOnSafe = true
});

// MECH ENTITIES (children of carrier)
Entity mech1 = em.CreateEntity();
em.AddComponentData(mech1, new CombatPositionOccupant
{
    ParentEntity = carrier,
    PositionId = default,  // Will be assigned by AssignmentSystem
    IsReady = false
});
em.AddComponentData(mech1, new Parent { Value = carrier });
em.AddComponentData(mech1, new WeaponStats
{
    Damage = 150f,
    AttackCooldown = 2.0f,
    AttackType = AttackTypeId.Kinetic
});

// COMBAT FLOW:
// 1. Hostile fighters approach carrier
// 2. AutoBayManagementSystem detects threats in VentralBay arc
// 3. VentralBay.State = Opening
// 4. After 3 seconds, VentralBay.State = Open
// 5. CombatPositionAssignmentSystem assigns mechs to VentralBay
// 6. CombatPositionAttackSystem enables mechs to fire at fighters
// 7. When fighters destroyed, bays auto-close after delay
```

### Godgame: Galleon Broadside

```csharp
// GALLEON ENTITY
Entity galleon = em.CreateEntity();

var platformBuffer = em.AddBuffer<CombatPosition>(galleon);

// Port broadside (left side cannons)
platformBuffer.Add(new CombatPosition
{
    PositionId = "PortBroadside",
    LocalOffset = new float3(-5, 2, 0),
    Arc = new FiringArc
    {
        CenterDirection = new float3(-1, 0, 0),  // Left
        ArcWidthRadians = math.PI * 0.66f,       // 120° arc
        MinRange = 5f,
        MaxRange = 200f,
        MinElevation = -0.1f,  // Slight downward angle
        MaxElevation = 0.3f    // Slight upward angle
    },
    State = BayState.Open,  // Broadsides always open
    MaxOccupants = 8,       // 8 cannon positions
    Health = 1.0f,
    TargetLayerMask = LayerMask.Ships | LayerMask.SeaMonsters
});

// Starboard broadside (right side cannons)
platformBuffer.Add(new CombatPosition
{
    PositionId = "StarboardBroadside",
    LocalOffset = new float3(5, 2, 0),
    Arc = new FiringArc
    {
        CenterDirection = new float3(1, 0, 0),   // Right
        ArcWidthRadians = math.PI * 0.66f,       // 120° arc
        MinRange = 5f,
        MaxRange = 200f,
        MinElevation = -0.1f,
        MaxElevation = 0.3f
    },
    State = BayState.Open,
    MaxOccupants = 8,
    Health = 1.0f,
    TargetLayerMask = LayerMask.Ships | LayerMask.SeaMonsters
});

em.AddComponentData(galleon, new CombatPositionProvider
{
    TotalPositions = 2,
    OccupiedPositions = 0,
    AutoManageBays = false  // Broadsides don't open/close
});

// CREW ENTITIES (manning cannons)
for (int i = 0; i < 8; i++)
{
    Entity crew = em.CreateEntity();
    em.AddComponentData(crew, new CombatPositionOccupant
    {
        ParentEntity = galleon,
        PositionId = default,  // Will be assigned
        IsReady = false
    });
    em.AddComponentData(crew, new Parent { Value = galleon });
    em.AddComponentData(crew, new WeaponStats
    {
        Damage = 50f,
        AttackCooldown = 8.0f,  // Reload time
        AttackType = AttackTypeId.Explosive
    });
}

// COMBAT FLOW:
// 1. Enemy ship approaches from port side
// 2. CombatPositionAssignmentSystem assigns crew to PortBroadside
// 3. Crew fire cannons when enemy in arc
// 4. Coordinated broadside volley (all cannons fire together)
// 5. If PortBroadside takes damage, Health decreases
// 6. At Health < 0.5, some cannon positions destroyed (MaxOccupants reduced)
```

### Godgame: Fortress Wall Defense

```csharp
// FORTRESS ENTITY
Entity fortress = em.CreateEntity();

var wallBuffer = em.AddBuffer<CombatPosition>(fortress);

// North wall archer positions
wallBuffer.Add(new CombatPosition
{
    PositionId = "NorthWall",
    LocalOffset = new float3(0, 5, 20),  // 5m height, 20m north
    Arc = new FiringArc
    {
        CenterDirection = new float3(0, 0, 1),   // North
        ArcWidthRadians = math.PI,               // 180° arc
        MinRange = 0f,
        MaxRange = 100f,
        MinElevation = -0.5f,  // Can shoot down at attackers
        MaxElevation = 0.2f
    },
    State = BayState.Open,
    MaxOccupants = 12,
    Health = 1.0f,
    TargetLayerMask = LayerMask.Infantry | LayerMask.Siege
});

// ... (East, South, West walls similar)

em.AddComponentData(fortress, new CombatPositionProvider
{
    TotalPositions = 4,
    OccupiedPositions = 0,
    AutoManageBays = false
});

// ARCHER ENTITIES
for (int i = 0; i < 12; i++)
{
    Entity archer = em.CreateEntity();
    em.AddComponentData(archer, new CombatPositionOccupant
    {
        ParentEntity = fortress,
        PositionId = default,
        IsReady = false
    });
    em.AddComponentData(archer, new Parent { Value = fortress });
    em.AddComponentData(archer, new WeaponStats
    {
        Damage = 25f,
        AttackCooldown = 3.0f,
        AttackType = AttackTypeId.Piercing
    });
}

// COMBAT FLOW:
// 1. Enemy band approaches from north
// 2. CombatPositionAssignmentSystem assigns archers to NorthWall
// 3. Archers fire at enemies in arc
// 4. If enemy attacks from east, some archers reassigned to EastWall
// 5. Dynamic defense based on threat direction
```

---

## Tactical Depth

### 1. Positioning Strategy

```
CARRIER TACTICS (Space4X):

Optimal Positioning:
├─ Face ventral bay toward fighter swarms
├─ Port/starboard bays toward capitals
├─ Rotate to bring fresh bays into combat
└─ Retreat when bay health critical

Counter-tactics:
├─ Target bay doors during transitions (vulnerable)
├─ Flank carrier to avoid primary bay arcs
└─ Focus fire on damaged bays
```

### 2. Arc Management

```
BROADSIDE TACTICS (Godgame):

Crossing the T:
├─ Position bow/stern toward enemy
├─ Enemy can only use chase guns (2 cannons)
├─ You can use full broadside (8 cannons)
└─ 4x firepower advantage!

Line of Battle:
├─ Ships form parallel lines
├─ Each ship fires full broadside
├─ Coordinated volleys maximize damage
└─ Classic naval warfare tactics
```

### 3. Bay Damage Progression

```
BAY HEALTH DEGRADATION:

Health 100%: All positions functional
Health 75%: Minor damage, -10% fire rate
Health 50%: Moderate damage, -25% positions available
Health 25%: Critical damage, -50% positions available
Health 0%: Bay destroyed, no combat capability

STRATEGIC IMPLICATIONS:
→ Protect damaged bays (don't expose to fire)
→ Focus fire on enemy bays (disable combat capability)
→ Repair bays between engagements
→ Backup bays provide redundancy
```

### 4. Coordinated Fire

```csharp
/// <summary>
/// System that coordinates simultaneous firing from all occupants in a position
/// </summary>
[BurstCompile]
public partial struct CoordinatedFireSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Group occupants by parent and position
        var occupantsByPosition = new NativeMultiHashMap<Entity, Entity>(128, Allocator.Temp);

        foreach (var (occupant, entity) in
            SystemAPI.Query<RefRO<CombatPositionOccupant>>()
            .WithEntityAccess())
        {
            occupantsByPosition.Add(occupant.ValueRO.ParentEntity, entity);
        }

        // For each parent with coordinated fire enabled
        foreach (var (provider, positionBuffer, entity) in
            SystemAPI.Query<
                RefRO<CombatPositionProvider>,
                DynamicBuffer<CombatPosition>>()
            .WithEntityAccess()
            .WithAll<CoordinatedFireEnabled>())
        {
            // Check if all occupants ready in each position
            foreach (var position in positionBuffer)
            {
                int readyCount = CountReadyOccupants(state, entity, position.PositionId, occupantsByPosition);

                // If all ready, trigger coordinated volley
                if (readyCount >= position.MaxOccupants)
                {
                    TriggerCoordinatedVolley(state, entity, position.PositionId, occupantsByPosition);
                }
            }
        }

        occupantsByPosition.Dispose();
    }
}

/// <summary>
/// Tag component for entities with coordinated fire (e.g., broadsides)
/// </summary>
public struct CoordinatedFireEnabled : IComponentData
{
    /// <summary>
    /// Bonus damage multiplier for coordinated volleys
    /// </summary>
    public float VolleyDamageMultiplier;  // e.g., 1.2x for coordinated fire
}
```

---

## Integration with Forces System

Bay attacks can use the **Forces System** for projectile physics:

```csharp
// When firing from a combat position, apply force to projectile

Entity projectile = em.CreateEntity();

// Initial force from weapon
em.AddComponentData(projectile, new SpatialForceReceiver
{
    Mass = 10f,
    Velocity = firingDirection * muzzleVelocity,
    AccumulatedForce = float3.zero
});

// Gravity affects projectile
// Wind affects projectile (in Godgame)
// Ship momentum affects projectile (firing from moving ship)

// Result: Realistic projectile physics from mounted weapons
```

---

## Performance Optimization

### 1. Spatial Partitioning

```csharp
// Only check positions that could possibly hit targets

foreach (position in positionBuffer)
{
    // Skip if no entities in position's range
    if (!spatialGrid.HasEntitiesInRadius(parentPos, position.Arc.MaxRange))
        continue;

    // Only query entities within arc bounds
    var candidates = spatialGrid.QueryArc(
        parentPos,
        arcDirection,
        position.Arc.ArcWidthRadians,
        position.Arc.MaxRange);
}
```

### 2. LOD System

```csharp
// Reduce update frequency for distant combat
public struct CombatPositionLOD : IComponentData
{
    public LODLevel CurrentLOD;

    public enum LODLevel : byte
    {
        High = 0,    // Update every frame (close combat)
        Medium = 1,  // Update every 3 frames
        Low = 2,     // Update every 10 frames
        Culled = 3   // Don't update (too far)
    }
}
```

### 3. Arc Caching

```csharp
// Cache arc calculations per-position

public struct CachedArcData : IBufferElementData
{
    public FixedString32Bytes PositionId;
    public float3 WorldArcDirection;  // Cached world-space direction
    public uint LastUpdateTick;       // When cache was updated
}

// Recalculate only when parent rotates
```

---

## Telemetry

```csharp
[BurstCompile]
partial struct CombatPositionTelemetrySystem : IJobEntity
{
    public TelemetryStream TelemetryStream;

    void Execute(
        in DynamicBuffer<CombatPosition> positions,
        in CombatPositionProvider provider)
    {
        int openBays = 0;
        int closedBays = 0;
        int damagedBays = 0;
        float avgHealth = 0f;

        foreach (var pos in positions)
        {
            switch (pos.State)
            {
                case BayState.Open: openBays++; break;
                case BayState.Closed: closedBays++; break;
                case BayState.Damaged: damagedBays++; break;
            }
            avgHealth += pos.Health;
        }

        avgHealth /= positions.Length;

        TelemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Combat,
            Name = "CombatPositions_Open",
            Value = openBays
        });

        TelemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Combat,
            Name = "CombatPositions_AverageHealth",
            Value = avgHealth
        });
    }
}
```

---

## Summary

The **Bay and Platform Combat System** creates tactical depth through:

✅ **Positional combat** - Arc management matters
✅ **Parent-child relationships** - Carriers/ships empower occupants
✅ **Firing arcs** - Angle and position determine effectiveness
✅ **Bay states** - Opening/closing creates vulnerability windows
✅ **Coordinated fire** - Volleys from multiple positions
✅ **Damage progression** - Bays degrade, affecting combat capability
✅ **Strategic positioning** - Flanking, crossing-the-T, arc coverage
✅ **Cross-game mechanics** - Works for space carriers and naval vessels
✅ **Burst-compatible** - Parallel processing, deterministic
✅ **Force integration** - Projectile physics from mounted weapons

**Game Impact:**

**Space4X:**
- Carrier tactics: bay management, rotation, targeting
- Mech/titan deployment from hangars
- Bay-specific damage (disable combat capability)
- Capital ships vulnerable when bays open

**Godgame:**
- Broadside tactics: line of battle, crossing-the-T
- Fortress defense: wall-based arc coverage
- Crew assignment to optimal firing positions
- Naval warfare depth

**Result:** Combat becomes about **positioning, arc management, and tactical positioning**, not just stat checks. Flanking matters. Bay damage matters. Coordinated fire matters. **Tactics > stats.**

---

**Related Documentation:**
- [General_Forces_System.md](General_Forces_System.md) - Projectile physics
- [Reactions_And_Relations_System.md](Reactions_And_Relations_System.md) - Crew morale
- [Relation_Bonuses_System.md](Relation_Bonuses_System.md) - Combat bonuses
- [Multi_Force_Interactions.md](Multi_Force_Interactions.md) - Projectile interactions

---

**Last Updated:** 2025-12-18
**Status:** Design Document - Tactical Combat
**Burst Compatible:** Yes
**Deterministic:** Yes
**Creates Tactical Depth:** ABSOLUTELY! 🎯
