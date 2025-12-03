# Extension Request: Target Selection & Prioritization Utilities

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/AI/Targeting/TargetSelectionComponents.cs` - TargetScore, ThreatAssessment, DamageMemory, TargetCandidate, TargetSelectionConfig, CurrentTarget
- `Packages/com.moni.puredots/Runtime/Runtime/AI/Targeting/TargetSelectionHelpers.cs` - Static helpers for priority calculation, threat scoring, damage memory

---

## Use Case

Both games need target selection logic with weighted priority scoring:

**Space4X:**
- Weapon systems selecting targets based on threat, distance, and grudge history
- Strike craft prioritizing damaged enemies
- Captains directing fire at high-value targets
- Grudge targets receiving priority boost

**Godgame:**
- Hunters selecting prey based on distance, ease of kill, and meat yield
- Guards prioritizing threats by danger level and proximity
- Villagers choosing job targets (nearest resource, most urgent task)
- Band members targeting enemies who previously wounded them

Shared needs:
- Range-based filtering
- Multi-factor priority scoring
- Damage history tracking (who hurt me?)
- Threat assessment calculations

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components (`Packages/com.moni.puredots/Runtime/Runtime/AI/`)

```csharp
/// <summary>
/// Priority score for a potential target.
/// </summary>
public struct TargetScore : IComponentData
{
    public Entity TargetEntity;
    public float TotalScore;           // Combined weighted score
    public float ThreatScore;          // How dangerous is this target
    public float DistanceScore;        // Distance-based priority (closer = higher)
    public float HistoryScore;         // From damage memory / grudges
    public float ValueScore;           // Target value (high-value = priority)
    public uint EvaluatedTick;
}

/// <summary>
/// Threat level assessment for an entity.
/// </summary>
public struct ThreatAssessment : IComponentData
{
    public float BaseThreat;           // Innate danger (weapon damage, creature type)
    public float CurrentThreat;        // After modifiers (wounded = less threat)
    public float AggressionLevel;      // 0 = passive, 1 = attacking me
    public byte IsHostile;             // Currently hostile to evaluator
    public byte IsEngaged;             // Already in combat
    public uint LastAssessedTick;
}

/// <summary>
/// Memory of damage received from entities.
/// Used for revenge targeting and threat learning.
/// </summary>
[InternalBufferCapacity(8)]
public struct DamageMemory : IBufferElementData
{
    public Entity AttackerEntity;
    public float TotalDamageReceived;  // Cumulative damage from this attacker
    public float RecentDamage;         // Damage in last N ticks (decays)
    public ushort HitCount;            // Number of times hit by this entity
    public uint LastHitTick;
}

/// <summary>
/// Candidate target for evaluation.
/// </summary>
[InternalBufferCapacity(16)]
public struct TargetCandidate : IBufferElementData
{
    public Entity CandidateEntity;
    public float Distance;
    public float Priority;             // Calculated priority score
    public byte IsValid;               // Passes basic filters
    public byte WasSelected;           // Was chosen as target
}

/// <summary>
/// Configuration for target selection behavior.
/// </summary>
public struct TargetSelectionConfig : IComponentData
{
    public float MaxRange;             // Maximum targeting range
    public float OptimalRange;         // Preferred engagement range
    public float ThreatWeight;         // Weight for threat score
    public float DistanceWeight;       // Weight for distance score
    public float HistoryWeight;        // Weight for damage memory
    public float ValueWeight;          // Weight for target value
    public byte PreferWounded;         // Prioritize damaged targets
    public byte PreferEngaged;         // Prioritize targets already fighting
}
```

### Static Helpers

```csharp
public static class TargetSelectionHelpers
{
    /// <summary>
    /// Calculates overall target priority from weighted factors.
    /// </summary>
    public static float CalculatePriority(
        float threatScore,
        float distanceScore,
        float historyScore,
        float valueScore,
        in TargetSelectionConfig config)
    {
        return threatScore * config.ThreatWeight +
               distanceScore * config.DistanceWeight +
               historyScore * config.HistoryWeight +
               valueScore * config.ValueWeight;
    }

    /// <summary>
    /// Calculates distance-based score (closer = higher).
    /// </summary>
    public static float CalculateDistanceScore(
        float distance,
        float maxRange,
        float optimalRange)
    {
        if (distance > maxRange) return 0;
        if (distance <= optimalRange) return 1f;
        
        // Linear falloff from optimal to max range
        return 1f - (distance - optimalRange) / (maxRange - optimalRange);
    }

    /// <summary>
    /// Calculates threat score from assessment.
    /// </summary>
    public static float CalculateThreatScore(
        in ThreatAssessment threat,
        bool preferEngaged)
    {
        float score = threat.CurrentThreat * threat.AggressionLevel;
        if (threat.IsHostile != 0) score *= 1.5f;
        if (preferEngaged && threat.IsEngaged != 0) score *= 1.2f;
        return math.saturate(score);
    }

    /// <summary>
    /// Calculates revenge/history score from damage memory.
    /// </summary>
    public static float CalculateHistoryScore(
        in DynamicBuffer<DamageMemory> memory,
        Entity target)
    {
        for (int i = 0; i < memory.Length; i++)
        {
            if (memory[i].AttackerEntity == target)
            {
                // Recent damage and hit count both matter
                float recentFactor = math.min(1f, memory[i].RecentDamage * 0.01f);
                float countFactor = math.min(1f, memory[i].HitCount * 0.1f);
                return math.max(recentFactor, countFactor);
            }
        }
        return 0;
    }

    /// <summary>
    /// Filters candidates by range.
    /// </summary>
    public static void FilterByRange(
        ref DynamicBuffer<TargetCandidate> candidates,
        float3 position,
        float maxRange)
    {
        float maxRangeSq = maxRange * maxRange;
        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            candidate.IsValid = (byte)(candidate.Distance * candidate.Distance <= maxRangeSq ? 1 : 0);
            candidates[i] = candidate;
        }
    }

    /// <summary>
    /// Sorts candidates by priority (highest first).
    /// </summary>
    public static void SortByPriority(ref DynamicBuffer<TargetCandidate> candidates)
    {
        // Simple bubble sort for small buffers
        for (int i = 0; i < candidates.Length - 1; i++)
        {
            for (int j = 0; j < candidates.Length - i - 1; j++)
            {
                if (candidates[j].Priority < candidates[j + 1].Priority)
                {
                    var temp = candidates[j];
                    candidates[j] = candidates[j + 1];
                    candidates[j + 1] = temp;
                }
            }
        }
    }

    /// <summary>
    /// Adds damage to memory buffer, updating or creating entry.
    /// </summary>
    public static void RecordDamage(
        ref DynamicBuffer<DamageMemory> memory,
        Entity attacker,
        float damage,
        uint currentTick)
    {
        for (int i = 0; i < memory.Length; i++)
        {
            if (memory[i].AttackerEntity == attacker)
            {
                var entry = memory[i];
                entry.TotalDamageReceived += damage;
                entry.RecentDamage += damage;
                entry.HitCount++;
                entry.LastHitTick = currentTick;
                memory[i] = entry;
                return;
            }
        }

        // New attacker
        if (memory.Length < memory.Capacity)
        {
            memory.Add(new DamageMemory
            {
                AttackerEntity = attacker,
                TotalDamageReceived = damage,
                RecentDamage = damage,
                HitCount = 1,
                LastHitTick = currentTick
            });
        }
    }

    /// <summary>
    /// Decays recent damage over time.
    /// </summary>
    public static void DecayDamageMemory(
        ref DynamicBuffer<DamageMemory> memory,
        float decayRate)
    {
        for (int i = 0; i < memory.Length; i++)
        {
            var entry = memory[i];
            entry.RecentDamage *= (1f - decayRate);
            memory[i] = entry;
        }
    }

    /// <summary>
    /// Gets the highest priority valid target.
    /// </summary>
    public static Entity GetBestTarget(in DynamicBuffer<TargetCandidate> candidates)
    {
        Entity best = Entity.Null;
        float bestPriority = float.MinValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].IsValid != 0 && candidates[i].Priority > bestPriority)
            {
                bestPriority = candidates[i].Priority;
                best = candidates[i].CandidateEntity;
            }
        }

        return best;
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/TargetSelectionComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/TargetSelectionHelpers.cs`
- Integration: Existing combat systems can use these utilities

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Weapon targeting ===
var config = EntityManager.GetComponentData<TargetSelectionConfig>(weaponEntity);
var memory = EntityManager.GetBuffer<DamageMemory>(shipEntity);
var candidates = EntityManager.GetBuffer<TargetCandidate>(weaponEntity);

// Filter by range
TargetSelectionHelpers.FilterByRange(ref candidates, myPosition, config.MaxRange);

// Score each candidate
for (int i = 0; i < candidates.Length; i++)
{
    var candidate = candidates[i];
    if (candidate.IsValid == 0) continue;

    var threat = EntityManager.GetComponentData<ThreatAssessment>(candidate.CandidateEntity);
    
    float threatScore = TargetSelectionHelpers.CalculateThreatScore(threat, config.PreferEngaged != 0);
    float distanceScore = TargetSelectionHelpers.CalculateDistanceScore(
        candidate.Distance, config.MaxRange, config.OptimalRange);
    float historyScore = TargetSelectionHelpers.CalculateHistoryScore(memory, candidate.CandidateEntity);
    
    candidate.Priority = TargetSelectionHelpers.CalculatePriority(
        threatScore, distanceScore, historyScore, 0, config);
    candidates[i] = candidate;
}

// Get best target
Entity target = TargetSelectionHelpers.GetBestTarget(candidates);

// === Godgame: Hunter prey selection ===
var hunterConfig = new TargetSelectionConfig
{
    MaxRange = 50f,
    OptimalRange = 10f,
    ThreatWeight = 0.2f,      // Low - hunters aren't afraid
    DistanceWeight = 0.5f,    // High - prefer close prey
    HistoryWeight = 0.1f,     // Low - not vengeful
    ValueWeight = 0.2f        // Medium - prefer meaty prey
};

// Record damage when attacked
TargetSelectionHelpers.RecordDamage(ref memory, attackerEntity, damageAmount, currentTick);

// Later: prioritize that attacker for revenge
float revengeBonus = TargetSelectionHelpers.CalculateHistoryScore(memory, attackerEntity);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Game-specific targeting systems
  - **Rejected**: Both games need identical core logic (range, priority, memory)

- **Alternative 2**: Pure component-based (no helpers)
  - **Rejected**: Calculations are complex enough to warrant reusable helpers

---

## Implementation Notes

**Dependencies:**
- math library for distance calculations
- Entity system for target references

**Performance Considerations:**
- DamageMemory buffer is fixed-size to avoid allocations
- Sorting uses simple algorithm suitable for small candidate counts
- Helpers are static and burst-compatible

**Related Requests:**
- Grudge/Vendetta system (history score integration)
- Combat utility systems (damage events feed into memory)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

