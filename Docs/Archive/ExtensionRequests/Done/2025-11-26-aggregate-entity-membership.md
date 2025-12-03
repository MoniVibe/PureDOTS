# Extension Request: Aggregate Entity & Membership System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Both games need composite/aggregate entities that pool members:

**Space4X:**
- Dynasties own capital ships, stations, and political capital
- Guilds pool specialists (engineers guild, spy cabals)
- Expeditionary fleets move as units with pooled upkeep
- Orders issued via aggregate propagate to individuals

**Godgame:**
- Families share resources and shelter
- Bands move together with averaged speed
- Guilds contract out members
- Work crews coordinate on projects

Shared needs:
- Aggregate stat calculation from members
- Member contribution tracking
- Order propagation to members
- Split/merge mechanics
- Aggregate-level resources

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Type of aggregate entity.
/// </summary>
public enum AggregateType : byte
{
    Family = 0,
    Dynasty = 1,
    Guild = 2,
    Corporation = 3,
    Band = 4,
    Army = 5,
    Fleet = 6,
    WorkCrew = 7,
    Expedition = 8,
    Cult = 9
}

/// <summary>
/// An aggregate entity that contains members.
/// </summary>
public struct AggregateEntity : IComponentData
{
    public AggregateType Type;
    public FixedString64Bytes Name;
    public Entity LeaderEntity;        // Who leads this aggregate
    public ushort MemberCount;
    public ushort MaxMembers;
    public float AverageSpeed;         // Aggregate movement speed
    public float TotalUpkeep;          // Combined resource cost
    public uint FormedTick;
    public byte IsActive;
}

/// <summary>
/// Membership in an aggregate entity.
/// </summary>
[InternalBufferCapacity(4)]
public struct AggregateMembership : IBufferElementData
{
    public Entity AggregateEntity;
    public AggregateType Type;
    public float ContributionWeight;   // How much this member contributes
    public float LoyaltyToAggregate;   // 0-1 loyalty to the group
    public byte Rank;                  // Position in hierarchy
    public byte IsFounder;             // Original member
    public uint JoinedTick;
}

/// <summary>
/// Members list for an aggregate.
/// </summary>
[InternalBufferCapacity(16)]
public struct AggregateMember : IBufferElementData
{
    public Entity MemberEntity;
    public float ContributionWeight;
    public byte Rank;
    public byte IsActive;
    public uint JoinedTick;
}

/// <summary>
/// Aggregate stats calculated from members.
/// </summary>
public struct AggregateStats : IComponentData
{
    public float AverageHealth;
    public float AverageMorale;
    public float AverageSkill;
    public float TotalStrength;        // Combat power
    public float TotalWealth;          // Economic power
    public float Cohesion;             // How unified the group is
    public uint LastCalculatedTick;
}

/// <summary>
/// Resources owned by aggregate.
/// </summary>
public struct AggregateResources : IComponentData
{
    public float Treasury;             // Money/credits
    public float Supplies;             // Consumables
    public float Influence;            // Political capital
    public float Prestige;             // Reputation value
}

/// <summary>
/// Order issued to aggregate.
/// </summary>
public struct AggregateOrder : IComponentData
{
    public FixedString32Bytes OrderType;
    public Entity TargetEntity;
    public float3 TargetPosition;
    public float Priority;
    public uint IssuedTick;
    public uint ExpiresTime;
    public byte PropagateToMembers;    // Should members receive this?
    public byte RequiresConsensus;     // Need member agreement?
}

/// <summary>
/// Split request for aggregate.
/// </summary>
public struct AggregateSplitRequest : IComponentData
{
    public Entity SourceAggregate;
    public FixedString64Bytes NewName;
    public Entity NewLeader;
    public float SplitRatio;           // What fraction goes to new group
    public uint RequestTick;
    public byte IsApproved;
}

/// <summary>
/// Merge request for aggregates.
/// </summary>
public struct AggregateMergeRequest : IComponentData
{
    public Entity SourceAggregate;
    public Entity TargetAggregate;
    public Entity NewLeader;
    public uint RequestTick;
    public byte IsApproved;
}
```

### Static Helpers

```csharp
public static class AggregateHelpers
{
    /// <summary>
    /// Calculates aggregate stats from members.
    /// </summary>
    public static AggregateStats CalculateAggregateStats(
        in DynamicBuffer<AggregateMember> members,
        NativeArray<float> memberHealths,
        NativeArray<float> memberMorales,
        NativeArray<float> memberSkills,
        NativeArray<float> memberStrengths,
        uint currentTick)
    {
        if (members.Length == 0)
        {
            return new AggregateStats { LastCalculatedTick = currentTick };
        }

        float totalWeight = 0;
        float weightedHealth = 0;
        float weightedMorale = 0;
        float weightedSkill = 0;
        float totalStrength = 0;
        int activeCount = 0;

        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].IsActive == 0) continue;
            
            float weight = members[i].ContributionWeight;
            totalWeight += weight;
            weightedHealth += memberHealths[i] * weight;
            weightedMorale += memberMorales[i] * weight;
            weightedSkill += memberSkills[i] * weight;
            totalStrength += memberStrengths[i];
            activeCount++;
        }

        float avgHealth = totalWeight > 0 ? weightedHealth / totalWeight : 0;
        float avgMorale = totalWeight > 0 ? weightedMorale / totalWeight : 0;
        float avgSkill = totalWeight > 0 ? weightedSkill / totalWeight : 0;

        // Calculate cohesion from morale variance
        float moraleVariance = 0;
        if (activeCount > 1)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].IsActive == 0) continue;
                float diff = memberMorales[i] - avgMorale;
                moraleVariance += diff * diff;
            }
            moraleVariance /= activeCount - 1;
        }
        float cohesion = math.saturate(1f - math.sqrt(moraleVariance));

        return new AggregateStats
        {
            AverageHealth = avgHealth,
            AverageMorale = avgMorale,
            AverageSkill = avgSkill,
            TotalStrength = totalStrength,
            Cohesion = cohesion,
            LastCalculatedTick = currentTick
        };
    }

    /// <summary>
    /// Calculates aggregate movement speed (slowest member).
    /// </summary>
    public static float CalculateAggregateSpeed(
        in DynamicBuffer<AggregateMember> members,
        NativeArray<float> memberSpeeds)
    {
        float minSpeed = float.MaxValue;
        
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].IsActive == 0) continue;
            minSpeed = math.min(minSpeed, memberSpeeds[i]);
        }
        
        return minSpeed == float.MaxValue ? 0 : minSpeed;
    }

    /// <summary>
    /// Calculates total upkeep for aggregate.
    /// </summary>
    public static float CalculateTotalUpkeep(
        in DynamicBuffer<AggregateMember> members,
        NativeArray<float> memberUpkeeps)
    {
        float total = 0;
        
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].IsActive == 0) continue;
            total += memberUpkeeps[i];
        }
        
        return total;
    }

    /// <summary>
    /// Propagates order to all members.
    /// </summary>
    public static int PropagateOrder(
        in AggregateOrder order,
        in DynamicBuffer<AggregateMember> members,
        NativeArray<float> memberLoyalties,
        float minLoyaltyToObey)
    {
        int obeying = 0;
        
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].IsActive == 0) continue;
            
            // Check if member will obey
            if (memberLoyalties[i] >= minLoyaltyToObey)
            {
                obeying++;
            }
        }
        
        return obeying;
    }

    /// <summary>
    /// Calculates consensus for order.
    /// </summary>
    public static float CalculateConsensus(
        in DynamicBuffer<AggregateMember> members,
        NativeArray<float> memberApproval)
    {
        if (members.Length == 0) return 0;
        
        float totalWeight = 0;
        float weightedApproval = 0;
        
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].IsActive == 0) continue;
            
            float weight = members[i].ContributionWeight;
            totalWeight += weight;
            weightedApproval += memberApproval[i] * weight;
        }
        
        return totalWeight > 0 ? weightedApproval / totalWeight : 0;
    }

    /// <summary>
    /// Adds member to aggregate.
    /// </summary>
    public static bool TryAddMember(
        ref DynamicBuffer<AggregateMember> members,
        ref AggregateEntity aggregate,
        Entity newMember,
        float contributionWeight,
        uint currentTick)
    {
        if (aggregate.MemberCount >= aggregate.MaxMembers)
            return false;
        
        members.Add(new AggregateMember
        {
            MemberEntity = newMember,
            ContributionWeight = contributionWeight,
            Rank = 0,
            IsActive = 1,
            JoinedTick = currentTick
        });
        
        aggregate.MemberCount++;
        return true;
    }

    /// <summary>
    /// Removes member from aggregate.
    /// </summary>
    public static bool TryRemoveMember(
        ref DynamicBuffer<AggregateMember> members,
        ref AggregateEntity aggregate,
        Entity memberToRemove)
    {
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].MemberEntity == memberToRemove)
            {
                members.RemoveAt(i);
                aggregate.MemberCount--;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Calculates split distribution.
    /// </summary>
    public static void CalculateSplitDistribution(
        in DynamicBuffer<AggregateMember> members,
        in AggregateResources resources,
        float splitRatio,
        out int membersToNew,
        out AggregateResources newResources,
        out AggregateResources remainingResources)
    {
        int activeMembers = 0;
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].IsActive != 0) activeMembers++;
        }
        
        membersToNew = (int)(activeMembers * splitRatio);
        
        newResources = new AggregateResources
        {
            Treasury = resources.Treasury * splitRatio,
            Supplies = resources.Supplies * splitRatio,
            Influence = resources.Influence * splitRatio * 0.5f, // Influence splits unequally
            Prestige = resources.Prestige * splitRatio * 0.3f    // Prestige stays with original
        };
        
        remainingResources = new AggregateResources
        {
            Treasury = resources.Treasury - newResources.Treasury,
            Supplies = resources.Supplies - newResources.Supplies,
            Influence = resources.Influence - newResources.Influence,
            Prestige = resources.Prestige - newResources.Prestige
        };
    }

    /// <summary>
    /// Calculates merge result.
    /// </summary>
    public static AggregateResources CalculateMergeResources(
        in AggregateResources sourceResources,
        in AggregateResources targetResources)
    {
        return new AggregateResources
        {
            Treasury = sourceResources.Treasury + targetResources.Treasury,
            Supplies = sourceResources.Supplies + targetResources.Supplies,
            Influence = math.max(sourceResources.Influence, targetResources.Influence) * 1.1f,
            Prestige = math.max(sourceResources.Prestige, targetResources.Prestige)
        };
    }

    /// <summary>
    /// Gets member with highest rank.
    /// </summary>
    public static Entity GetHighestRankMember(
        in DynamicBuffer<AggregateMember> members)
    {
        Entity best = Entity.Null;
        byte highestRank = 0;
        
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].IsActive != 0 && members[i].Rank > highestRank)
            {
                highestRank = members[i].Rank;
                best = members[i].MemberEntity;
            }
        }
        
        return best;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Dynasty managing assets ===
var dynasty = EntityManager.GetComponentData<AggregateEntity>(dynastyEntity);
var members = EntityManager.GetBuffer<AggregateMember>(dynastyEntity);
var resources = EntityManager.GetComponentData<AggregateResources>(dynastyEntity);

// Gather member data
var healths = new NativeArray<float>(members.Length, Allocator.Temp);
var morales = new NativeArray<float>(members.Length, Allocator.Temp);
// ... fill arrays from member components ...

// Calculate aggregate stats
var stats = AggregateHelpers.CalculateAggregateStats(
    members, healths, morales, skills, strengths, currentTick);

// Issue order to dynasty
var order = new AggregateOrder
{
    OrderType = "mobilize_fleet",
    TargetEntity = targetSystemEntity,
    Priority = 0.8f,
    PropagateToMembers = 1,
    RequiresConsensus = 0
};

// Check how many will obey
int obeyingCount = AggregateHelpers.PropagateOrder(
    order, members, loyalties, 0.5f);

// === Godgame: Band movement ===
var band = EntityManager.GetComponentData<AggregateEntity>(bandEntity);
var bandMembers = EntityManager.GetBuffer<AggregateMember>(bandEntity);

// Get member speeds
var speeds = new NativeArray<float>(bandMembers.Length, Allocator.Temp);
for (int i = 0; i < bandMembers.Length; i++)
{
    speeds[i] = GetMemberSpeed(bandMembers[i].MemberEntity);
}

// Band moves at slowest member's speed
float bandSpeed = AggregateHelpers.CalculateAggregateSpeed(bandMembers, speeds);
band.AverageSpeed = bandSpeed;

// Calculate upkeep
var upkeeps = new NativeArray<float>(bandMembers.Length, Allocator.Temp);
// ... fill upkeeps ...
band.TotalUpkeep = AggregateHelpers.CalculateTotalUpkeep(bandMembers, upkeeps);

// === Split aggregate ===
var splitRequest = new AggregateSplitRequest
{
    SourceAggregate = dynastyEntity,
    NewName = "Cadet Branch",
    NewLeader = cadetEntity,
    SplitRatio = 0.3f
};

AggregateHelpers.CalculateSplitDistribution(
    members, resources, splitRequest.SplitRatio,
    out int membersToNew, out var newResources, out var remainingResources);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Flat entity lists
  - **Rejected**: Need weighted contributions and hierarchical relationships

- **Alternative 2**: Game-specific groups
  - **Rejected**: Core mechanics (members, stats, orders) are identical

---

## Implementation Notes

**Dependencies:**
- Entity references for members
- NativeArray for batch stat collection

**Performance Considerations:**
- Stat calculation is O(n) per aggregate
- Can cache aggregate stats and recalculate on member changes
- Member buffers are fixed-size

**Related Requests:**
- Aggregate stats utilities (stat calculation)
- Patriotism system (loyalty to aggregates)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

