# Extension Request: Reinforcement & Arrival Positioning System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/AI/Reinforcement/ReinforcementComponents.cs` - ArrivalPattern, ArrivalFormation, ArrivalTiming, ArrivalPrecision, RallyPoint, ArrivalGroup, ArrivalState
- `Packages/com.moni.puredots/Runtime/Runtime/AI/Reinforcement/ReinforcementHelpers.cs` - Static helpers for formation positioning, staggered delays, rally point calculation

---

## Use Case

Both games need systems for reinforcement arrival and unit positioning:

**Space4X:**
- Fleet warp-in positioning with scatter based on navigation precision
- Staggered reinforcement waves (vanguard, main force, rear guard)
- Rally points for retreating ships
- Ambush positioning (flanking arrival)
- Emergency jump scatter (high imprecision when damaged)

**Godgame:**
- Band reinforcement rally points
- Militia gathering at alarm locations
- Hunter pack rendezvous positioning
- Retreat and regroup mechanics
- Caravan arrival at trading posts

Shared needs:
- Position calculation with controlled scatter
- Timing coordination (simultaneous vs staggered)
- Formation-aware positioning
- Optimal rally point selection

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components (`Packages/com.moni.puredots/Runtime/Runtime/AI/`)

```csharp
/// <summary>
/// Timing pattern for arrivals.
/// </summary>
public enum ArrivalPattern : byte
{
    Simultaneous = 0,    // All arrive at once
    Staggered = 1,       // Sequential with delays
    Wave = 2,            // Groups arrive in waves
    Random = 3           // Random timing within window
}

/// <summary>
/// How units position relative to rally point.
/// </summary>
public enum ArrivalFormation : byte
{
    Scatter = 0,         // Random positions in radius
    Circle = 1,          // Arranged in circle
    Line = 2,            // Arranged in line facing target
    Wedge = 3,           // V-formation
    Flanking = 4         // Split to sides of target
}

/// <summary>
/// Arrival timing configuration.
/// </summary>
public struct ArrivalTiming : IComponentData
{
    public ArrivalPattern Pattern;
    public float BaseDelay;            // Base time before arrival
    public float DelayVariance;        // Random variance +/-
    public float WaveInterval;         // Time between waves
    public byte WaveCount;             // Number of waves
    public uint ScheduledTick;         // When arrival was scheduled
}

/// <summary>
/// Positional precision for arrival.
/// </summary>
public struct ArrivalPrecision : IComponentData
{
    public float BaseScatter;          // Base scatter radius
    public float PrecisionModifier;    // 0-1, higher = tighter grouping
    public float MaxScatter;           // Maximum scatter limit
    public float MinDistance;          // Minimum distance from rally point
    public float PreferredDistance;    // Ideal distance from rally point
    public uint Seed;                  // Random seed for reproducible scatter
}

/// <summary>
/// Target location for arriving units.
/// </summary>
public struct RallyPoint : IComponentData
{
    public float3 Position;
    public float3 FacingDirection;     // Direction to face on arrival
    public Entity TargetEntity;        // Optional entity to rally near
    public float Radius;               // Radius of rally area
    public byte IsActive;
    public uint CreatedTick;
}

/// <summary>
/// Group of units arriving together.
/// </summary>
public struct ArrivalGroup : IComponentData
{
    public Entity LeaderEntity;        // First to arrive / commander
    public ushort GroupSize;           // Total units in group
    public ushort ArrivedCount;        // Units that have arrived
    public ArrivalFormation Formation;
    public float FormationSpacing;     // Distance between units
    public uint ArrivalTick;           // When group arrives
    public byte IsComplete;            // All units have arrived
}

/// <summary>
/// Per-unit arrival state.
/// </summary>
public struct ArrivalState : IComponentData
{
    public Entity GroupEntity;         // Which arrival group this belongs to
    public float3 AssignedPosition;    // Where this unit should arrive
    public byte SlotIndex;             // Position in formation
    public float ArrivalDelay;         // Individual delay offset
    public byte HasArrived;
    public uint ArrivedTick;
}

/// <summary>
/// Request to find optimal rally point.
/// </summary>
public struct RallyPointRequest : IComponentData
{
    public float3 FriendlyCentroid;    // Center of friendly forces
    public float3 EnemyCentroid;       // Center of enemy forces
    public float3 ObjectivePosition;   // What we're trying to reach/defend
    public float PreferredDistance;    // Distance from objective
    public byte AvoidEnemies;          // Stay away from enemies
    public byte FlankObjective;        // Try to arrive on flank
}
```

### Static Helpers

```csharp
public static class ReinforcementHelpers
{
    /// <summary>
    /// Calculates arrival position with scatter.
    /// </summary>
    public static float3 CalculateArrivalPosition(
        float3 rallyPoint,
        float3 facingDirection,
        in ArrivalPrecision precision,
        int unitIndex,
        int totalUnits)
    {
        // Use deterministic random based on seed + index
        uint hash = math.hash(new uint2(precision.Seed, (uint)unitIndex));
        Random rng = new Random(hash);
        
        // Calculate scatter based on precision
        float scatter = precision.BaseScatter * (1f - precision.PrecisionModifier);
        scatter = math.clamp(scatter, 0, precision.MaxScatter);
        
        // Random angle
        float angle = rng.NextFloat(0, math.PI * 2f);
        
        // Random distance within scatter radius
        float distance = precision.MinDistance + rng.NextFloat(0, scatter);
        
        // Calculate offset
        float3 offset = new float3(
            math.cos(angle) * distance,
            0,
            math.sin(angle) * distance
        );
        
        return rallyPoint + offset;
    }

    /// <summary>
    /// Calculates formation-based arrival positions.
    /// </summary>
    public static void CalculateFormationPositions(
        float3 rallyPoint,
        float3 facingDirection,
        ArrivalFormation formation,
        float spacing,
        int unitCount,
        NativeArray<float3> positions)
    {
        float3 right = math.cross(facingDirection, new float3(0, 1, 0));
        float3 forward = facingDirection;
        
        switch (formation)
        {
            case ArrivalFormation.Circle:
                CalculateCirclePositions(rallyPoint, spacing, unitCount, positions);
                break;
            case ArrivalFormation.Line:
                CalculateLinePositions(rallyPoint, right, spacing, unitCount, positions);
                break;
            case ArrivalFormation.Wedge:
                CalculateWedgePositions(rallyPoint, forward, right, spacing, unitCount, positions);
                break;
            case ArrivalFormation.Flanking:
                CalculateFlankingPositions(rallyPoint, right, spacing, unitCount, positions);
                break;
            default:
                // Scatter handled by CalculateArrivalPosition
                for (int i = 0; i < unitCount; i++)
                {
                    positions[i] = rallyPoint;
                }
                break;
        }
    }

    private static void CalculateCirclePositions(
        float3 center,
        float spacing,
        int count,
        NativeArray<float3> positions)
    {
        float radius = spacing * count / (2f * math.PI);
        radius = math.max(radius, spacing);
        
        for (int i = 0; i < count; i++)
        {
            float angle = (float)i / count * math.PI * 2f;
            positions[i] = center + new float3(
                math.cos(angle) * radius,
                0,
                math.sin(angle) * radius
            );
        }
    }

    private static void CalculateLinePositions(
        float3 center,
        float3 lineDirection,
        float spacing,
        int count,
        NativeArray<float3> positions)
    {
        float halfWidth = (count - 1) * spacing * 0.5f;
        
        for (int i = 0; i < count; i++)
        {
            float offset = i * spacing - halfWidth;
            positions[i] = center + lineDirection * offset;
        }
    }

    private static void CalculateWedgePositions(
        float3 tip,
        float3 forward,
        float3 right,
        float spacing,
        int count,
        NativeArray<float3> positions)
    {
        if (count == 0) return;
        
        // Leader at tip
        positions[0] = tip;
        
        int row = 1;
        int placed = 1;
        
        while (placed < count)
        {
            int unitsInRow = row + 1;
            float rowBack = -row * spacing;
            float halfWidth = row * spacing * 0.5f;
            
            for (int i = 0; i < unitsInRow && placed < count; i++)
            {
                float sideOffset = i * spacing - halfWidth;
                positions[placed] = tip + forward * rowBack + right * sideOffset;
                placed++;
            }
            row++;
        }
    }

    private static void CalculateFlankingPositions(
        float3 center,
        float3 right,
        float spacing,
        int count,
        NativeArray<float3> positions)
    {
        int leftCount = count / 2;
        int rightCount = count - leftCount;
        
        float flankDistance = spacing * 3f; // Distance to flank
        
        // Left flank
        float3 leftCenter = center - right * flankDistance;
        for (int i = 0; i < leftCount; i++)
        {
            float offset = (i - leftCount / 2f) * spacing;
            positions[i] = leftCenter + new float3(0, 0, offset);
        }
        
        // Right flank
        float3 rightCenter = center + right * flankDistance;
        for (int i = 0; i < rightCount; i++)
        {
            float offset = (i - rightCount / 2f) * spacing;
            positions[leftCount + i] = rightCenter + new float3(0, 0, offset);
        }
    }

    /// <summary>
    /// Calculates staggered arrival delay for a unit.
    /// </summary>
    public static float GetStaggeredDelay(
        int unitIndex,
        int totalUnits,
        in ArrivalTiming timing)
    {
        switch (timing.Pattern)
        {
            case ArrivalPattern.Simultaneous:
                return timing.BaseDelay;
                
            case ArrivalPattern.Staggered:
                float interval = timing.WaveInterval / math.max(1, totalUnits - 1);
                return timing.BaseDelay + unitIndex * interval;
                
            case ArrivalPattern.Wave:
                int waveSize = math.max(1, totalUnits / timing.WaveCount);
                int waveIndex = unitIndex / waveSize;
                return timing.BaseDelay + waveIndex * timing.WaveInterval;
                
            case ArrivalPattern.Random:
                Random rng = new Random((uint)(timing.ScheduledTick + unitIndex));
                return timing.BaseDelay + rng.NextFloat(-timing.DelayVariance, timing.DelayVariance);
                
            default:
                return timing.BaseDelay;
        }
    }

    /// <summary>
    /// Finds optimal rally point based on tactical situation.
    /// </summary>
    public static float3 FindOptimalRallyPoint(
        in RallyPointRequest request,
        float safetyMargin)
    {
        // Vector from enemy to objective
        float3 enemyToObjective = request.ObjectivePosition - request.EnemyCentroid;
        float3 friendlyToObjective = request.ObjectivePosition - request.FriendlyCentroid;
        
        float3 rallyPoint;
        
        if (request.AvoidEnemies != 0)
        {
            // Rally on opposite side from enemies
            float3 awayFromEnemy = math.normalizesafe(enemyToObjective);
            rallyPoint = request.ObjectivePosition + awayFromEnemy * request.PreferredDistance;
        }
        else if (request.FlankObjective != 0)
        {
            // Rally to the side (flanking position)
            float3 toEnemy = math.normalizesafe(-enemyToObjective);
            float3 perpendicular = math.cross(toEnemy, new float3(0, 1, 0));
            
            // Choose side closer to friendly forces
            float3 candidate1 = request.ObjectivePosition + perpendicular * request.PreferredDistance;
            float3 candidate2 = request.ObjectivePosition - perpendicular * request.PreferredDistance;
            
            float dist1 = math.distance(candidate1, request.FriendlyCentroid);
            float dist2 = math.distance(candidate2, request.FriendlyCentroid);
            
            rallyPoint = dist1 < dist2 ? candidate1 : candidate2;
        }
        else
        {
            // Rally between friendlies and objective
            float3 direction = math.normalizesafe(friendlyToObjective);
            rallyPoint = request.ObjectivePosition - direction * request.PreferredDistance;
        }
        
        return rallyPoint;
    }

    /// <summary>
    /// Checks if arrival group is complete.
    /// </summary>
    public static bool IsGroupComplete(in ArrivalGroup group)
    {
        return group.ArrivedCount >= group.GroupSize;
    }

    /// <summary>
    /// Calculates arrival tick for a unit.
    /// </summary>
    public static uint CalculateArrivalTick(
        uint currentTick,
        float delay,
        float ticksPerSecond)
    {
        return currentTick + (uint)(delay * ticksPerSecond);
    }

    /// <summary>
    /// Applies scatter to precision based on damage/emergency.
    /// </summary>
    public static ArrivalPrecision ApplyEmergencyScatter(
        in ArrivalPrecision basePrecision,
        float damageRatio)
    {
        // More damage = worse precision
        var result = basePrecision;
        result.PrecisionModifier *= (1f - damageRatio);
        result.BaseScatter *= (1f + damageRatio * 2f);
        return result;
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/ReinforcementComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/ReinforcementHelpers.cs`
- Integration: Game-specific reinforcement systems consume these utilities

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Fleet warp-in ===
var timing = new ArrivalTiming
{
    Pattern = ArrivalPattern.Wave,
    BaseDelay = 2f,                  // 2 seconds before first arrival
    WaveInterval = 1.5f,             // 1.5 seconds between waves
    WaveCount = 3,                   // Vanguard, main force, rear guard
    ScheduledTick = currentTick
};

var precision = new ArrivalPrecision
{
    BaseScatter = 50f,               // Base 50 unit scatter
    PrecisionModifier = 0.8f,        // 80% precision (good nav)
    MaxScatter = 200f,
    MinDistance = 10f,
    PreferredDistance = 30f,
    Seed = randomSeed
};

// Calculate arrival positions for fleet
var positions = new NativeArray<float3>(fleetSize, Allocator.Temp);
ReinforcementHelpers.CalculateFormationPositions(
    rallyPoint.Position,
    rallyPoint.FacingDirection,
    ArrivalFormation.Wedge,
    20f,  // 20 unit spacing
    fleetSize,
    positions);

// Apply scatter to each position
for (int i = 0; i < fleetSize; i++)
{
    positions[i] = ReinforcementHelpers.CalculateArrivalPosition(
        positions[i],
        rallyPoint.FacingDirection,
        precision,
        i,
        fleetSize);
        
    float delay = ReinforcementHelpers.GetStaggeredDelay(i, fleetSize, timing);
    ScheduleWarpIn(ships[i], positions[i], delay);
}

// Emergency jump (damaged ship) - worse precision
var emergencyPrecision = ReinforcementHelpers.ApplyEmergencyScatter(precision, 0.6f);
float3 emergencyPosition = ReinforcementHelpers.CalculateArrivalPosition(
    rallyPoint.Position, rallyPoint.FacingDirection, emergencyPrecision, 0, 1);

// === Godgame: Band reinforcements ===
var request = new RallyPointRequest
{
    FriendlyCentroid = bandPosition,
    EnemyCentroid = enemyPosition,
    ObjectivePosition = defendTarget,
    PreferredDistance = 20f,
    AvoidEnemies = 1,
    FlankObjective = 0
};

float3 optimalRally = ReinforcementHelpers.FindOptimalRallyPoint(request, 10f);

// Calculate militia arrival positions
var militiaPositions = new NativeArray<float3>(militiaCount, Allocator.Temp);
ReinforcementHelpers.CalculateFormationPositions(
    optimalRally,
    math.normalizesafe(enemyPosition - optimalRally),
    ArrivalFormation.Line,
    2f,  // 2 unit spacing
    militiaCount,
    militiaPositions);

// Staggered arrival as militia runs in
for (int i = 0; i < militiaCount; i++)
{
    float delay = ReinforcementHelpers.GetStaggeredDelay(i, militiaCount, new ArrivalTiming
    {
        Pattern = ArrivalPattern.Staggered,
        BaseDelay = 0,
        WaveInterval = 5f  // 5 seconds for all to arrive
    });
    ScheduleMilitiaArrival(militia[i], militiaPositions[i], delay);
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Game-specific positioning
  - **Rejected**: Formation math and scatter calculations are identical

- **Alternative 2**: Pure random positioning
  - **Rejected**: Both games need tactical positioning (flanking, formation)

- **Alternative 3**: Hard-coded formations only
  - **Rejected**: Need flexibility for scatter and dynamic formations

---

## Implementation Notes

**Dependencies:**
- Unity.Mathematics for vector operations
- Random seed for deterministic scatter

**Performance Considerations:**
- Formation calculations are O(n) for n units
- All helpers are static and burst-compatible
- NativeArray used for position output

**Related Requests:**
- Formation system (formation-aware positioning)
- Spatial grid (finding rally points near structures)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

