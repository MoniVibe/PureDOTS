# Extension Request: Entity Needs System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Both games have entities with needs that decay and must be replenished:

**Godgame:**
- Villagers need food (Hunger), rest (Fatigue), social interaction (Social)
- Unmet needs cause morale penalties, health decay, breakdown risk

**Space4X:**
- Crew needs food, rest, entertainment
- Ship systems need fuel, maintenance
- Colonies need supplies, housing

Needs drive entity behavior (seek food when hungry, rest when tired).

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/AI/`)

```csharp
public enum NeedType : byte
{
    None = 0,
    // Biological
    Hunger = 1,         // Food requirement
    Thirst = 2,         // Water requirement
    Fatigue = 3,        // Rest requirement
    Health = 4,         // Physical wellbeing
    // Psychological
    Social = 10,        // Interaction need
    Entertainment = 11, // Leisure need
    Safety = 12,        // Security feeling
    Purpose = 13,       // Meaningful work
    // Operational (systems)
    Fuel = 20,
    Power = 21,
    Maintenance = 22,
    Supplies = 23
}

public enum NeedUrgency : byte
{
    Satisfied = 0,      // 80-100%
    Normal = 1,         // 50-79%
    Concerned = 2,      // 25-49%
    Urgent = 3,         // 10-24%
    Critical = 4        // 0-9%
}

public struct EntityNeeds : IComponentData
{
    // Core needs (0-1000 scale)
    public float Health;
    public float MaxHealth;
    public float Energy;           // Combines hunger/fatigue
    public float MaxEnergy;
    public float Morale;           // Combines social/purpose
    public float MaxMorale;
    
    // Urgency flags
    public NeedUrgency HealthUrgency;
    public NeedUrgency EnergyUrgency;
    public NeedUrgency MoraleUrgency;
    
    // Decay rates (per second)
    public float EnergyDecayRate;
    public float MoraleDecayRate;
}

[InternalBufferCapacity(6)]
public struct NeedEntry : IBufferElementData
{
    public NeedType Type;
    public float Current;          // 0-100
    public float Max;              // Maximum (usually 100)
    public float DecayRate;        // Per second
    public float RegenRate;        // Per second when replenishing
    public NeedUrgency Urgency;
    public uint LastUpdateTick;
}

public struct NeedsConfig : IComponentData
{
    public float UrgentThreshold;      // 25%
    public float CriticalThreshold;    // 10%
    public float WorkingDecayMult;     // 2.5x decay when working
    public float IdleDecayMult;        // 0.5x decay when idle
    public float SleepRegenMult;       // 5x regen when sleeping
}
```

### System

```csharp
// NeedsDecaySystem - Decays needs over time based on activity
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct NeedsDecaySystem : ISystem { }

// NeedsUrgencySystem - Updates urgency levels, triggers seeking behavior
public partial struct NeedsUrgencySystem : ISystem { }

public static class NeedsHelpers
{
    public static NeedUrgency GetUrgency(float current, float max, in NeedsConfig config);
    public static float GetDecayRate(float baseRate, ActivityState activity, in NeedsConfig config);
    public static bool ShouldSeekNeed(NeedUrgency urgency);
    public static float GetPerformancePenalty(in EntityNeeds needs); // From low needs
}
```

---

## Example Usage

```csharp
// === Godgame: Decay energy while working ===
var needs = EntityManager.GetComponentData<EntityNeeds>(villagerEntity);
float decay = NeedsHelpers.GetDecayRate(needs.EnergyDecayRate, ActivityState.Working, config);
needs.Energy = math.max(0, needs.Energy - decay * deltaTime);
needs.EnergyUrgency = NeedsHelpers.GetUrgency(needs.Energy, needs.MaxEnergy, config);

// === Space4X: Check if crew should eat ===
var needsBuffer = EntityManager.GetBuffer<NeedEntry>(crewEntity);
var hunger = NeedsHelpers.GetNeed(needsBuffer, NeedType.Hunger);
if (NeedsHelpers.ShouldSeekNeed(hunger.Urgency))
{
    // Queue "find food" behavior
}

// === Performance penalty from unmet needs ===
float penalty = NeedsHelpers.GetPerformancePenalty(needs);
workEfficiency *= (1f - penalty);
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/Villagers/`
- `VillagerNeedsSystem.cs`
- `VillagerNeedsAuthoring.cs`

---

## Review Notes

*(PureDOTS team use)*

