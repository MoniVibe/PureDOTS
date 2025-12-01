# Extension Request: Knowledge & Tech Diffusion System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need knowledge/technology spreading between locations:

**Space4X:**
- Tech unlocks at capitals, then propagates to colonies via relay networks
- Diffusion speed depends on distance, infrastructure tier, and diplomatic ties
- Espionage can siphon tech from rivals prematurely
- Diffusion pauses/regresses when relations sour

**Godgame:**
- Crafting techniques spread between villages via caravans and migration
- Religious knowledge propagates from temples to outlying settlements
- Scholarly knowledge spreads faster with libraries and schools
- Isolation slows cultural/tech advancement

Shared needs:
- Propagation queue mechanics
- Distance-based diffusion timing
- Infrastructure quality affecting spread speed
- Decay when communication breaks down
- Shared research synchronization

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// State of knowledge propagation.
/// </summary>
public enum DiffusionState : byte
{
    Unknown = 0,        // Not yet reached
    Queued = 1,         // In transit
    Arriving = 2,       // Almost there
    Adopted = 3,        // Fully available
    Decaying = 4,       // Losing access
    Lost = 5            // No longer available
}

/// <summary>
/// Type of knowledge being diffused.
/// </summary>
public enum KnowledgeCategory : byte
{
    Technology = 0,
    Crafting = 1,
    Religion = 2,
    Culture = 3,
    Military = 4,
    Economic = 5,
    Scientific = 6,
    Medical = 7
}

/// <summary>
/// A piece of knowledge that can spread.
/// </summary>
public struct KnowledgeDefinition : IComponentData
{
    public FixedString64Bytes KnowledgeId;
    public KnowledgeCategory Category;
    public byte Tier;                  // Complexity tier (affects spread speed)
    public float BaseDiffusionRate;    // How fast it spreads naturally
    public byte RequiresInfrastructure; // Min infrastructure to receive
    public uint UnlockedTick;          // When first discovered
}

/// <summary>
/// Tracks diffusion state for a knowledge item at a location.
/// </summary>
public struct KnowledgeDiffusionState : IComponentData
{
    public FixedString64Bytes KnowledgeId;
    public Entity SourceEntity;        // Where it came from
    public Entity LocationEntity;      // Where it's spreading to
    public DiffusionState State;
    public float Progress;             // 0-1 progress to adoption
    public float DecayProgress;        // 0-1 decay toward lost
    public uint QueuedTick;
    public uint AdoptedTick;
    public uint EstimatedArrivalTick;
}

/// <summary>
/// Buffer of knowledge items in transit to a location.
/// </summary>
[InternalBufferCapacity(16)]
public struct DiffusionQueue : IBufferElementData
{
    public FixedString64Bytes KnowledgeId;
    public Entity SourceEntity;
    public float TravelProgress;       // 0-1 how far along
    public float TravelSpeed;          // Units per tick
    public uint QueuedTick;
}

/// <summary>
/// Infrastructure that affects diffusion.
/// </summary>
public struct DiffusionInfrastructure : IComponentData
{
    public byte InfrastructureTier;    // 0-10
    public float RelayQuality;         // 0-1 network quality
    public float ReceptionBonus;       // Bonus to incoming diffusion
    public float TransmissionBonus;    // Bonus to outgoing diffusion
    public byte HasLibrary;            // Archives accelerate adoption
    public byte HasAcademy;            // Training accelerates adoption
}

/// <summary>
/// Connection between two locations for knowledge transfer.
/// </summary>
public struct DiffusionLink : IComponentData
{
    public Entity SourceEntity;
    public Entity TargetEntity;
    public float Distance;             // Travel distance
    public float LinkQuality;          // 0-1 connection quality
    public float ThroughputLimit;      // Max knowledge per tick
    public byte IsActive;              // Currently transferring
    public byte IsDiplomatic;          // Requires positive relations
}

/// <summary>
/// Shared research agreement between entities.
/// </summary>
public struct ResearchPact : IComponentData
{
    public Entity PartnerEntity;
    public float SyncRate;             // How fast queues sync
    public float ShareRatio;           // What % of research shared
    public uint EstablishedTick;
    public uint ExpiresTick;
    public byte IsActive;
}

/// <summary>
/// Knowledge adoption status at a location.
/// </summary>
[InternalBufferCapacity(32)]
public struct AdoptedKnowledge : IBufferElementData
{
    public FixedString64Bytes KnowledgeId;
    public KnowledgeCategory Category;
    public byte Tier;
    public uint AdoptedTick;
    public float MasteryLevel;         // 0-1 how well understood
    public byte CanTransmit;           // Mastered enough to teach
}
```

### Static Helpers

```csharp
public static class DiffusionHelpers
{
    /// <summary>
    /// Calculates travel time for knowledge diffusion.
    /// </summary>
    public static float CalculateTravelTime(
        float distance,
        byte knowledgeTier,
        float linkQuality,
        float infrastructureTier)
    {
        // Base travel time from distance
        float baseTime = distance * 0.1f;
        
        // Higher tier = slower spread
        float tierPenalty = 1f + knowledgeTier * 0.3f;
        
        // Better infrastructure = faster
        float infraBonus = 1f / (1f + infrastructureTier * 0.1f);
        
        // Link quality affects speed
        float linkFactor = 2f - linkQuality;
        
        return baseTime * tierPenalty * infraBonus * linkFactor;
    }

    /// <summary>
    /// Calculates diffusion rate at destination.
    /// </summary>
    public static float CalculateDiffusionRate(
        float baseDiffusionRate,
        in DiffusionInfrastructure infra,
        float sourceRelation,
        bool hasDiplomaticTies)
    {
        // Base rate from knowledge
        float rate = baseDiffusionRate;
        
        // Infrastructure bonuses
        rate *= 1f + infra.ReceptionBonus;
        if (infra.HasLibrary != 0) rate *= 1.2f;
        if (infra.HasAcademy != 0) rate *= 1.15f;
        
        // Relations affect sharing
        if (hasDiplomaticTies)
        {
            rate *= 0.5f + sourceRelation * 0.5f;
        }
        
        return rate;
    }

    /// <summary>
    /// Calculates decay rate when cut off from source.
    /// </summary>
    public static float CalculateDecayRate(
        byte knowledgeTier,
        float masteryLevel,
        in DiffusionInfrastructure infra)
    {
        // Higher tier decays faster without reinforcement
        float tierDecay = 0.01f * knowledgeTier;
        
        // Better mastery resists decay
        float masteryResist = masteryLevel * 0.5f;
        
        // Libraries preserve knowledge
        float archiveResist = infra.HasLibrary != 0 ? 0.3f : 0;
        
        return math.max(0, tierDecay * (1f - masteryResist - archiveResist));
    }

    /// <summary>
    /// Checks if location meets requirements for knowledge.
    /// </summary>
    public static bool MeetsRequirements(
        in KnowledgeDefinition knowledge,
        in DiffusionInfrastructure infra,
        in DynamicBuffer<AdoptedKnowledge> adopted)
    {
        // Check infrastructure tier
        if (infra.InfrastructureTier < knowledge.RequiresInfrastructure)
            return false;
        
        // Check prerequisites (simplified - just tier check)
        if (knowledge.Tier > 0)
        {
            bool hasPrereq = false;
            for (int i = 0; i < adopted.Length; i++)
            {
                if (adopted[i].Category == knowledge.Category &&
                    adopted[i].Tier >= knowledge.Tier - 1)
                {
                    hasPrereq = true;
                    break;
                }
            }
            if (!hasPrereq) return false;
        }
        
        return true;
    }

    /// <summary>
    /// Updates diffusion queue progress.
    /// </summary>
    public static void UpdateDiffusionQueue(
        ref DynamicBuffer<DiffusionQueue> queue,
        float deltaTime,
        float linkQuality)
    {
        for (int i = 0; i < queue.Length; i++)
        {
            var entry = queue[i];
            entry.TravelProgress += entry.TravelSpeed * deltaTime * linkQuality;
            queue[i] = entry;
        }
    }

    /// <summary>
    /// Checks for completed diffusions in queue.
    /// </summary>
    public static int CheckCompletedDiffusions(
        in DynamicBuffer<DiffusionQueue> queue,
        NativeList<FixedString64Bytes> completed)
    {
        int count = 0;
        for (int i = 0; i < queue.Length; i++)
        {
            if (queue[i].TravelProgress >= 1f)
            {
                completed.Add(queue[i].KnowledgeId);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Calculates research pact sync progress.
    /// </summary>
    public static float CalculatePactSyncProgress(
        in ResearchPact pact,
        float partnerProgress,
        float myProgress)
    {
        if (pact.IsActive == 0) return 0;
        
        float gap = partnerProgress - myProgress;
        if (gap <= 0) return 0;
        
        return gap * pact.SyncRate * pact.ShareRatio;
    }

    /// <summary>
    /// Estimates arrival tick for queued knowledge.
    /// </summary>
    public static uint EstimateArrivalTick(
        uint currentTick,
        float remainingDistance,
        float travelSpeed)
    {
        if (travelSpeed <= 0) return uint.MaxValue;
        
        float remainingTime = remainingDistance / travelSpeed;
        return currentTick + (uint)remainingTime;
    }

    /// <summary>
    /// Calculates mastery gain rate.
    /// </summary>
    public static float CalculateMasteryGain(
        float currentMastery,
        in DiffusionInfrastructure infra,
        float usageRate)
    {
        // Mastery grows with use
        float baseGain = 0.01f * usageRate;
        
        // Academy accelerates mastery
        float academyBonus = infra.HasAcademy != 0 ? 1.5f : 1f;
        
        // Diminishing returns at high mastery
        float diminishing = 1f - currentMastery * 0.5f;
        
        return baseGain * academyBonus * diminishing;
    }

    /// <summary>
    /// Checks if entity can transmit knowledge.
    /// </summary>
    public static bool CanTransmitKnowledge(
        in AdoptedKnowledge knowledge,
        in DiffusionInfrastructure infra)
    {
        // Need sufficient mastery
        if (knowledge.MasteryLevel < 0.5f) return false;
        
        // Need transmission capability
        if (infra.TransmissionBonus <= 0) return false;
        
        return true;
    }

    /// <summary>
    /// Gets effective link throughput.
    /// </summary>
    public static float GetEffectiveThroughput(
        in DiffusionLink link,
        in DiffusionInfrastructure sourceInfra,
        in DiffusionInfrastructure targetInfra)
    {
        float base_throughput = link.ThroughputLimit;
        float sourceBonus = 1f + sourceInfra.TransmissionBonus;
        float targetBonus = 1f + targetInfra.ReceptionBonus;
        float qualityFactor = link.LinkQuality;
        
        return base_throughput * sourceBonus * targetBonus * qualityFactor;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Tech diffusion from capital ===
var tech = new KnowledgeDefinition
{
    KnowledgeId = "advanced_shields",
    Category = KnowledgeCategory.Technology,
    Tier = 3,
    BaseDiffusionRate = 0.05f,
    RequiresInfrastructure = 2
};

var link = EntityManager.GetComponentData<DiffusionLink>(relayEntity);
var targetInfra = EntityManager.GetComponentData<DiffusionInfrastructure>(colonyEntity);

// Calculate travel time
float travelTime = DiffusionHelpers.CalculateTravelTime(
    link.Distance,
    tech.Tier,
    link.LinkQuality,
    targetInfra.InfrastructureTier);

// Add to queue
var queue = EntityManager.GetBuffer<DiffusionQueue>(colonyEntity);
queue.Add(new DiffusionQueue
{
    KnowledgeId = tech.KnowledgeId,
    SourceEntity = capitalEntity,
    TravelProgress = 0,
    TravelSpeed = 1f / travelTime,
    QueuedTick = currentTick
});

// Each tick, update progress
DiffusionHelpers.UpdateDiffusionQueue(ref queue, deltaTime, link.LinkQuality);

// Check for completed arrivals
var completed = new NativeList<FixedString64Bytes>(Allocator.Temp);
int count = DiffusionHelpers.CheckCompletedDiffusions(queue, completed);

// === Godgame: Crafting technique spreading ===
var craftKnowledge = new KnowledgeDefinition
{
    KnowledgeId = "steel_forging",
    Category = KnowledgeCategory.Crafting,
    Tier = 2,
    BaseDiffusionRate = 0.08f,
    RequiresInfrastructure = 1
};

var villageInfra = EntityManager.GetComponentData<DiffusionInfrastructure>(targetVillage);
var adopted = EntityManager.GetBuffer<AdoptedKnowledge>(targetVillage);

// Check if village can receive
bool canReceive = DiffusionHelpers.MeetsRequirements(craftKnowledge, villageInfra, adopted);

// Calculate diffusion rate (affected by trade relations)
float rate = DiffusionHelpers.CalculateDiffusionRate(
    craftKnowledge.BaseDiffusionRate,
    villageInfra,
    0.7f, // trade relation
    true); // has caravan link

// Update mastery of adopted knowledge
for (int i = 0; i < adopted.Length; i++)
{
    var knowledge = adopted[i];
    float masteryGain = DiffusionHelpers.CalculateMasteryGain(
        knowledge.MasteryLevel,
        villageInfra,
        GetUsageRate(knowledge.KnowledgeId));
    knowledge.MasteryLevel = math.saturate(knowledge.MasteryLevel + masteryGain * deltaTime);
    adopted[i] = knowledge;
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Instant tech unlocks everywhere
  - **Rejected**: Both games want spread delay for strategic depth

- **Alternative 2**: Game-specific diffusion
  - **Rejected**: Core mechanics (queue, travel time, decay) are identical

---

## Implementation Notes

**Dependencies:**
- Entity references for source/target locations
- Spatial data for distance calculations

**Performance Considerations:**
- Queue processing is O(n) per location
- Can batch updates across locations
- Fixed-size buffers avoid allocations

**Related Requests:**
- Supply chain utilities (similar route-based logic)
- Diplomacy system (relations affect diffusion)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

