# Extension Request: Formation Positioning System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X primary, Godgame party formations)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need formation-based positioning for coordinated groups:

- **Space4X**: Fleet formations (wedge, line, screen, echelon) for carrier battle groups, escort positioning, strike craft wings
- **Godgame**: Party formations for adventuring groups, guard formations, work crews

Formations need:
- Slot position generation from templates
- Leader-relative world position calculation
- Dynamic rebalancing when members are destroyed/removed
- Role-based slot assignment (leader, escort, rear guard)

---

## Proposed Solution

**Extension Type**: New Components + Helpers

### Components (`Packages/com.moni.puredots/Runtime/Runtime/AI/Formation/`)

```csharp
public enum FormationType : byte
{
    None = 0,
    Wedge = 1,      // V-shape, leader at point
    Line = 2,       // Horizontal line
    Column = 3,     // Vertical column (follow the leader)
    Circle = 4,     // Ring around center
    Screen = 5,     // Wide defensive line
    Echelon = 6,    // Diagonal offset
    Box = 7,        // Square perimeter
    Diamond = 8     // Diamond shape
}

public enum FormationRole : byte
{
    Leader = 0,
    Escort = 1,     // Close protection
    Wing = 2,       // Flanking position
    Rear = 3,       // Trailing position
    Screen = 4,     // Forward scouts
    Reserve = 5     // Backup position
}

/// <summary>
/// Defines a slot in a formation template.
/// </summary>
public struct FormationSlot
{
    public float3 LocalOffset;      // Position relative to leader
    public FormationRole Role;       // Slot role
    public byte Priority;            // Assignment priority (lower = assigned first)
}

/// <summary>
/// Template defining formation shape.
/// </summary>
public struct FormationTemplate : IComponentData
{
    public FormationType Type;
    public float Spacing;           // Base distance between slots
    public float Depth;             // Formation depth multiplier
    public byte MaxSlots;           // Maximum supported members
}

/// <summary>
/// Buffer of slot definitions for a formation.
/// </summary>
[InternalBufferCapacity(16)]
public struct FormationSlotDefinition : IBufferElementData
{
    public FormationSlot Slot;
}

/// <summary>
/// Links an entity to its assigned formation slot.
/// </summary>
public struct FormationAssignment : IComponentData
{
    public Entity FormationLeader;   // Leader entity
    public byte SlotIndex;           // Assigned slot index
    public FormationRole Role;       // Current role
    public half Cohesion;            // How well entity maintains position [0,1]
}

/// <summary>
/// Calculated target position from formation.
/// </summary>
public struct FormationTargetPosition : IComponentData
{
    public float3 WorldPosition;     // Target world position
    public float3 Velocity;          // Leader velocity for prediction
    public byte IsValid;             // Whether position is valid
}
```

### Static Helpers

```csharp
public static class FormationHelpers
{
    /// <summary>
    /// Generates slot positions for a formation type.
    /// </summary>
    public static void GenerateSlots(
        FormationType type,
        int slotCount,
        float spacing,
        NativeArray<FormationSlot> outSlots)
    {
        switch (type)
        {
            case FormationType.Wedge:
                GenerateWedgeSlots(slotCount, spacing, outSlots);
                break;
            case FormationType.Line:
                GenerateLineSlots(slotCount, spacing, outSlots);
                break;
            case FormationType.Circle:
                GenerateCircleSlots(slotCount, spacing, outSlots);
                break;
            case FormationType.Column:
                GenerateColumnSlots(slotCount, spacing, outSlots);
                break;
            case FormationType.Screen:
                GenerateScreenSlots(slotCount, spacing, outSlots);
                break;
            case FormationType.Echelon:
                GenerateEchelonSlots(slotCount, spacing, outSlots);
                break;
            // ... other formations
        }
    }

    /// <summary>
    /// Wedge (V) formation - leader at point.
    /// </summary>
    private static void GenerateWedgeSlots(int count, float spacing, NativeArray<FormationSlot> slots)
    {
        slots[0] = new FormationSlot { LocalOffset = float3.zero, Role = FormationRole.Leader, Priority = 0 };
        
        for (int i = 1; i < count; i++)
        {
            int row = (i + 1) / 2;
            int side = (i % 2 == 1) ? 1 : -1;
            
            slots[i] = new FormationSlot
            {
                LocalOffset = new float3(side * row * spacing, 0, -row * spacing),
                Role = row == 1 ? FormationRole.Wing : FormationRole.Rear,
                Priority = (byte)i
            };
        }
    }

    /// <summary>
    /// Line formation - horizontal spread.
    /// </summary>
    private static void GenerateLineSlots(int count, float spacing, NativeArray<FormationSlot> slots)
    {
        float halfWidth = (count - 1) * spacing * 0.5f;
        
        for (int i = 0; i < count; i++)
        {
            slots[i] = new FormationSlot
            {
                LocalOffset = new float3(i * spacing - halfWidth, 0, 0),
                Role = i == count / 2 ? FormationRole.Leader : FormationRole.Escort,
                Priority = (byte)math.abs(i - count / 2)
            };
        }
    }

    /// <summary>
    /// Circle formation - ring around center.
    /// </summary>
    private static void GenerateCircleSlots(int count, float spacing, NativeArray<FormationSlot> slots)
    {
        float radius = spacing * count / (2f * math.PI);
        
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * 2f * math.PI;
            slots[i] = new FormationSlot
            {
                LocalOffset = new float3(math.cos(angle) * radius, 0, math.sin(angle) * radius),
                Role = FormationRole.Escort,
                Priority = (byte)i
            };
        }
    }

    /// <summary>
    /// Calculates world position from local offset and leader transform.
    /// </summary>
    public static float3 CalculateWorldPosition(
        float3 leaderPosition,
        quaternion leaderRotation,
        float3 slotOffset)
    {
        return leaderPosition + math.mul(leaderRotation, slotOffset);
    }

    /// <summary>
    /// Calculates world position with velocity prediction.
    /// </summary>
    public static float3 CalculateWorldPositionPredicted(
        float3 leaderPosition,
        quaternion leaderRotation,
        float3 leaderVelocity,
        float3 slotOffset,
        float predictionTime)
    {
        float3 predictedLeaderPos = leaderPosition + leaderVelocity * predictionTime;
        return predictedLeaderPos + math.mul(leaderRotation, slotOffset);
    }

    /// <summary>
    /// Finds best slot for new member based on role preference.
    /// </summary>
    public static int FindBestSlot(
        NativeArray<FormationSlot> slots,
        NativeArray<bool> occupied,
        FormationRole preferredRole)
    {
        int bestSlot = -1;
        byte bestPriority = 255;

        for (int i = 0; i < slots.Length; i++)
        {
            if (occupied[i]) continue;
            
            if (slots[i].Role == preferredRole && slots[i].Priority < bestPriority)
            {
                bestSlot = i;
                bestPriority = slots[i].Priority;
            }
        }

        // Fallback to any available slot
        if (bestSlot < 0)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!occupied[i] && slots[i].Priority < bestPriority)
                {
                    bestSlot = i;
                    bestPriority = slots[i].Priority;
                }
            }
        }

        return bestSlot;
    }

    /// <summary>
    /// Rebalances formation after a member is removed.
    /// </summary>
    public static void RebalanceAfterRemoval(
        NativeArray<Entity> members,
        NativeArray<byte> slotAssignments,
        NativeArray<FormationSlot> slots,
        int removedIndex)
    {
        // Shift members to fill higher-priority slots
        // Implementation depends on desired behavior (maintain roles vs fill gaps)
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/Formation/FormationComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/Formation/FormationHelpers.cs`

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Create fleet formation ===
var template = new FormationTemplate
{
    Type = FormationType.Wedge,
    Spacing = 50f,
    MaxSlots = 9
};

// Generate slots
var slots = new NativeArray<FormationSlot>(9, Allocator.Temp);
FormationHelpers.GenerateSlots(template.Type, 9, template.Spacing, slots);

// Assign carriers to slots
for (int i = 0; i < fleetMembers.Length; i++)
{
    EntityManager.AddComponentData(fleetMembers[i], new FormationAssignment
    {
        FormationLeader = flagshipEntity,
        SlotIndex = (byte)i,
        Role = slots[i].Role,
        Cohesion = (half)1f
    });
}

// === Update loop: Calculate world positions ===
var leaderPos = EntityManager.GetComponentData<LocalTransform>(flagshipEntity).Position;
var leaderRot = EntityManager.GetComponentData<LocalTransform>(flagshipEntity).Rotation;

foreach (var (assignment, targetPos) in SystemAPI.Query<RefRO<FormationAssignment>, RefRW<FormationTargetPosition>>())
{
    var slot = slots[assignment.ValueRO.SlotIndex];
    targetPos.ValueRW.WorldPosition = FormationHelpers.CalculateWorldPosition(
        leaderPos, leaderRot, slot.LocalOffset);
}

// === Godgame: Party formation ===
// Same pattern but with smaller spacing and different formation types
var partyTemplate = new FormationTemplate
{
    Type = FormationType.Box,
    Spacing = 2f,
    MaxSlots = 4
};
```

---

## Alternative Approaches Considered

- **Alternative 1**: Game-specific formation systems
  - **Rejected**: Both games need similar functionality, avoid duplication

- **Alternative 2**: Use Unity NavMesh formations
  - **Rejected**: NavMesh is 2D pathfinding, doesn't work for 3D space combat or abstract formations

---

## Implementation Notes

**Dependencies:**
- Spatial grid for member queries
- Movement system integration (formations feed target positions)

**Performance Considerations:**
- Slot generation is done once on formation creation
- World position calculation is simple matrix math, very fast
- Rebalancing only needed when members join/leave

**Related Requests:**
- `2025-11-26-combat-utility-systems.md` - Formations affect targeting and engagement

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

