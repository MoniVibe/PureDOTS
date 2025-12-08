# Entity Construction (Agnostic Framework)

**Status:** Concept Design
**Category:** Core Production Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Entity Construction Framework** provides agnostic mechanics for creating entities through resource-based manufacturing, assembly, or summoning processes. Games implement specific construction types, resource requirements, and entity behaviors while PureDOTS provides the blueprint system, construction state tracking, maintenance framework, and loyalty mechanics.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Construction blueprint component structure
- ✅ Resource requirement tracking
- ✅ Construction progress state machine
- ✅ Entity spawning framework
- ✅ Maintenance/upkeep system
- ✅ Loyalty and rebellion mechanics
- ✅ Facility capacity management

**Game-Specific Aspects** (Implemented by Games):
- Construction types (golems, robots, elementals, androids, undead)
- Resource catalogs (what materials are needed)
- Construction methods (rituals, assembly, crafting)
- Maintenance types (mana, power, repairs, fuel)
- Visual presentation (VFX, animations for construction/summoning)
- Cultural reactions (fear of undead, acceptance of robots)

---

## Core Agnostic Components

### ConstructionBlueprint (Mind ECS)
```csharp
/// <summary>
/// Agnostic construction blueprint
/// Games define type/requirement enums
/// </summary>
public struct ConstructionBlueprint : IComponentData
{
    public byte ConstructionTypeId;        // Game-defined enum
    public Entity OutputEntityPrefab;      // What gets created
    public int ConstructionTime;           // Minutes

    // Requirements
    public float RequiredSkillLevel;
    public Entity RequiredKnowledge;       // Blueprint/spell entity
    public Entity RequiredFacility;        // Workshop/circle/bay entity

    // Costs
    public float EnergyCost;               // Mana/power/fuel
}
```

### ConstructionResourceRequirement (Mind ECS Buffer)
```csharp
/// <summary>
/// Agnostic resource requirements
/// </summary>
[InternalBufferCapacity(8)]
public struct ConstructionResourceRequirement : IBufferElementData
{
    public Entity ResourceType;
    public int Quantity;
    public bool IsConsumed;                // True if consumed, false if reusable
}
```

### ConstructionInProgress (Body ECS)
```csharp
/// <summary>
/// Agnostic construction state tracking
/// </summary>
public struct ConstructionInProgress : IComponentData
{
    public Entity Creator;
    public Entity Blueprint;
    public float TimeRemaining;            // Minutes
    public float ProgressPercent;          // 0-1
    public byte CurrentPhaseId;            // Game-defined enum
    public bool CanBeInterrupted;
}
```

### ConstructedEntity (Body ECS)
```csharp
/// <summary>
/// Agnostic constructed entity marker
/// </summary>
public struct ConstructedEntity : IComponentData
{
    public Entity Creator;
    public byte ConstructionTypeId;        // Game-defined enum
    public uint CreationTick;

    // Loyalty and Control
    public float Loyalty;                  // 0-100
    public bool CanRebel;
    public bool IsProgrammed;              // No personality/will
}
```

### ConstructionFacility (Mind ECS)
```csharp
/// <summary>
/// Agnostic construction facility
/// </summary>
public struct ConstructionFacility : IComponentData
{
    public byte FacilityTypeId;            // Game-defined enum
    public int MaxSimultaneousConstructions;
    public int CurrentConstructions;
    public bool IsOperational;
}
```

### MaintenanceRequirements (Body ECS)
```csharp
/// <summary>
/// Agnostic maintenance/upkeep requirements
/// </summary>
public struct MaintenanceRequirements : IComponentData
{
    public byte MaintenanceTypeId;         // Game-defined enum (mana, power, repairs)
    public float CostPerDay;               // Resource/energy per day
    public float TimeSinceLastMaintenance;
    public float MaxTimeWithoutMaintenance;
    public bool IsInFailureState;
}
```

### PlagueCarrier (Body ECS)
```csharp
/// <summary>
/// Agnostic plague construct component
/// Marks construct as disease vector
/// </summary>
public struct PlagueCarrier : IComponentData
{
    public float MaterialFreshness;        // 0-100% (current freshness from decay)
    public float CombatEffectiveness;      // 0.3-1.0 (based on freshness)
    public float DiseasePotency;           // 0.1-1.8 (based on freshness)
    public float TerrorMultiplier;         // 1.0-3.5× (based on freshness)

    public float TerrorRadius;             // Meters (calculated from multiplier)
    public float TransmissionRadius;       // Meters (aura range)
    public float InfectionChance;          // 0-1 (on-hit infection)

    public float WeeklyDecayRate;          // Freshness loss per week
    public uint LastDecayUpdateTick;
}
```

### Infection (Body ECS)
```csharp
/// <summary>
/// Agnostic infection status effect
/// Applied to entities infected by plague carriers
/// </summary>
public struct Infection : IComponentData
{
    public Entity Source;                  // Plague carrier that infected
    public float DiseasePotency;           // 0.1-1.8 (determines severity)
    public float DamagePerHour;            // HP loss per hour
    public float RemainingDuration;        // Hours until cured/resolved

    public float ContagionChance;          // 0-1 chance to spread (typically 0.3)
    public float TransmissionRadius;       // Meters (how far it spreads)

    public uint InfectionTick;
    public bool IsCured;
}
```

### TerrorEffect (Mind ECS)
```csharp
/// <summary>
/// Agnostic terror status effect
/// Applied to entities near plague carriers
/// </summary>
public struct TerrorEffect : IComponentData
{
    public Entity Source;                  // Plague carrier causing terror
    public float MoralePenalty;            // -20 to -50
    public float FleeChance;               // 0.15-0.4 (non-heroes)
    public float RemainingDuration;        // Seconds

    public bool HasPermanentTrauma;        // "Plague Survivor" trait
}
```

---

## Agnostic Algorithms

### Blueprint Validation
```csharp
/// <summary>
/// Validate if entity can start construction
/// Agnostic: Games provide skill/knowledge/facility checks
/// </summary>
public static bool CanStartConstruction(
    float entitySkillLevel,
    float requiredSkillLevel,
    bool hasKnowledge,
    bool hasFacilityAccess,
    bool hasResources,
    bool hasEnergy)
{
    if (entitySkillLevel < requiredSkillLevel)
        return false;

    if (!hasKnowledge)
        return false;

    if (!hasFacilityAccess)
        return false;

    if (!hasResources)
        return false;

    if (!hasEnergy)
        return false;

    return true;
}
```

### Construction Progress Tracking
```csharp
/// <summary>
/// Update construction progress over time
/// Agnostic: Pure timer logic
/// </summary>
public static void UpdateConstructionProgress(
    ref ConstructionInProgress construction,
    float deltaTime,
    int totalConstructionTime)
{
    construction.TimeRemaining -= deltaTime;
    construction.ProgressPercent = 1f - (construction.TimeRemaining / totalConstructionTime);

    // Clamp progress
    construction.ProgressPercent = math.clamp(construction.ProgressPercent, 0f, 1f);

    // Construction complete
    if (construction.TimeRemaining <= 0f)
    {
        construction.ProgressPercent = 1f;
    }
}
```

### Entity Spawning
```csharp
/// <summary>
/// Spawn constructed entity when construction completes
/// Agnostic: Instantiate prefab, add components
/// </summary>
public static Entity CompleteConstruction(
    Entity blueprintPrefab,
    Entity creator,
    byte constructionTypeId,
    uint currentTick,
    bool canRebel,
    bool isProgrammed)
{
    // Instantiate entity from blueprint
    Entity constructedEntity = Instantiate(blueprintPrefab);

    // Add constructed entity marker
    AddComponent(constructedEntity, new ConstructedEntity
    {
        Creator = creator,
        ConstructionTypeId = constructionTypeId,
        CreationTick = currentTick,
        Loyalty = 100f,
        CanRebel = canRebel,
        IsProgrammed = isProgrammed,
    });

    return constructedEntity;
}
```

### Loyalty Update
```csharp
/// <summary>
/// Update loyalty based on treatment quality
/// Agnostic: Loyalty changes over time based on treatment
/// </summary>
public static void UpdateLoyalty(
    ref ConstructedEntity construct,
    float treatmentQuality,  // -1 to 1 (mistreatment to excellent)
    float deltaTime)
{
    if (!construct.CanRebel || construct.IsProgrammed)
        return; // Simple drones/golems cannot develop loyalty/disloyalty

    // Loyalty change rate (per day)
    float loyaltyChange = treatmentQuality * 0.5f * deltaTime;

    construct.Loyalty = math.clamp(construct.Loyalty + loyaltyChange, 0f, 100f);
}
```

### Rebellion Chance Calculation
```csharp
/// <summary>
/// Calculate rebellion probability
/// Agnostic: Low loyalty = high rebellion chance
/// </summary>
public static float CalculateRebellionChance(float loyalty)
{
    if (loyalty >= 30f)
        return 0f; // No rebellion risk above 30% loyalty

    // Quadratic curve: 30% loyalty = 0% chance, 0% loyalty = 100% chance
    float loyaltyDeficit = 30f - loyalty;
    return math.pow(loyaltyDeficit / 30f, 2f);
}

public static bool ShouldRebel(float loyalty, float randomValue)
{
    float rebellionChance = CalculateRebellionChance(loyalty);
    return randomValue < rebellionChance;
}
```

### Maintenance Failure Detection
```csharp
/// <summary>
/// Check if maintenance has failed
/// Agnostic: Time-based failure detection
/// </summary>
public static bool IsMaintenanceFailed(MaintenanceRequirements maintenance)
{
    if (maintenance.TimeSinceLastMaintenance > maintenance.MaxTimeWithoutMaintenance)
        return true;

    return false;
}

public static void UpdateMaintenanceFailure(
    ref MaintenanceRequirements maintenance,
    float deltaTime)
{
    maintenance.TimeSinceLastMaintenance += deltaTime;

    if (IsMaintenanceFailed(maintenance))
    {
        maintenance.IsInFailureState = true;
    }
}
```

### Facility Capacity Check
```csharp
/// <summary>
/// Check if facility has capacity for new construction
/// Agnostic: Capacity tracking
/// </summary>
public static bool HasCapacity(ConstructionFacility facility)
{
    if (!facility.IsOperational)
        return false;

    return facility.CurrentConstructions < facility.MaxSimultaneousConstructions;
}

public static void ReserveCapacity(ref ConstructionFacility facility)
{
    facility.CurrentConstructions++;
}

public static void ReleaseCapacity(ref ConstructionFacility facility)
{
    facility.CurrentConstructions = math.max(0, facility.CurrentConstructions - 1);
}
```

### Weaponized Decay Quality Formula
```csharp
/// <summary>
/// Calculate plague undead effectiveness from material decay stage
/// Agnostic: Trade-off between combat and disease potency
/// </summary>
public static void CalculatePlagueUndeadStats(
    float materialFreshness,       // 0-100% (from decay system)
    out float combatEffectiveness,
    out float diseasePotency,
    out float terrorMultiplier)
{
    // Inverse relationship: Lower freshness = higher disease/terror
    if (materialFreshness >= 80f)
    {
        // Fresh (100-80%)
        combatEffectiveness = 1.0f;
        diseasePotency = 0.1f;
        terrorMultiplier = 1.0f;
    }
    else if (materialFreshness >= 50f)
    {
        // Aging (80-50%)
        combatEffectiveness = 0.85f;
        diseasePotency = 0.4f;
        terrorMultiplier = 1.3f;
    }
    else if (materialFreshness >= 20f)
    {
        // Spoiling (50-20%)
        combatEffectiveness = 0.6f;
        diseasePotency = 1.0f;
        terrorMultiplier = 2.0f;
    }
    else if (materialFreshness >= 5f)
    {
        // Rotten (20-5%)
        combatEffectiveness = 0.3f;
        diseasePotency = 1.8f;
        terrorMultiplier = 3.5f;
    }
    else
    {
        // Destroyed (0-5%) - unusable
        combatEffectiveness = 0f;
        diseasePotency = 0f;
        terrorMultiplier = 0f;
    }
}
```

### Disease Transmission Calculation
```csharp
/// <summary>
/// Calculate infection spread from plague construct
/// Agnostic: Contagion mechanics
/// </summary>
public static bool TransmitInfection(
    float diseasePotency,          // 0-1.8 (from decay stage)
    float victimConstitution,      // 0-100 (victim's resistance)
    float proximityMeters,         // Distance from source
    float transmissionRadius,      // Game-defined radius (5-30m)
    float randomValue)             // 0-1 RNG
{
    if (proximityMeters > transmissionRadius)
        return false;

    // Base infection chance (game-defined, typically 40%)
    float baseChance = 0.4f;

    // Potency modifier (spoiling = 100%, rotten = 180%)
    float potencyBonus = (diseasePotency - 1.0f) * 0.5f; // 0% at 1.0, +40% at 1.8

    // Constitution resistance (-0.5% per point)
    float constitutionPenalty = victimConstitution * 0.005f;

    // Proximity modifier (closer = higher chance)
    float proximityModifier = 1.0f - (proximityMeters / transmissionRadius);

    float infectionChance = (baseChance + potencyBonus - constitutionPenalty) * proximityModifier;
    infectionChance = math.clamp(infectionChance, 0f, 0.95f);

    return randomValue < infectionChance;
}
```

### Contagion Spread Simulation
```csharp
/// <summary>
/// Calculate secondary infection spread (person-to-person)
/// Agnostic: Epidemic modeling
/// </summary>
public static int SimulateContagionSpread(
    int initialInfected,
    float contagionRate,           // Game-defined (typically 30%)
    int population,
    int daysElapsed)
{
    // Simple SIR model (Susceptible-Infected-Recovered)
    float infected = initialInfected;

    for (int day = 0; day < daysElapsed; day++)
    {
        // Daily transmission
        float newInfections = infected * contagionRate * (population - infected) / population;
        infected += newInfections;

        // Cap at population
        infected = math.min(infected, population);
    }

    return (int)infected;
}
```

### Terror Effect Radius Calculation
```csharp
/// <summary>
/// Calculate terror radius from decay stage
/// Agnostic: Intimidation area of effect
/// </summary>
public static float CalculateTerrorRadius(
    float baseRadius,              // Game-defined (30-50m)
    float terrorMultiplier)        // From decay stage (1.0-3.5×)
{
    return baseRadius * terrorMultiplier;
}

public static float CalculateMoralePenalty(
    float proximityMeters,
    float terrorRadius,
    float basePenalty,             // Game-defined (-20 to -50)
    bool isHero)
{
    if (isHero)
        return basePenalty * 0.3f; // Heroes less affected

    if (proximityMeters > terrorRadius)
        return 0f;

    // Closer = worse morale penalty
    float proximityFactor = 1.0f - (proximityMeters / terrorRadius);
    return basePenalty * proximityFactor;
}
```

### Maintenance Cost Modifier
```csharp
/// <summary>
/// Calculate increased upkeep for plague constructs
/// Agnostic: Disease sustainment cost
/// </summary>
public static float CalculatePlagueUpkeep(
    float baseUpkeep,              // Normal construct upkeep
    float diseasePotency)          // 0.1-1.8
{
    // Higher disease potency = higher maintenance
    float upkeepMultiplier = 1.0f + (diseasePotency * 0.6f);
    return baseUpkeep * upkeepMultiplier;
}
```

### Ongoing Decay During Animation
```csharp
/// <summary>
/// Calculate continued freshness loss while construct is animated
/// Agnostic: Animated corpses still decay
/// </summary>
public static float UpdateAnimatedDecay(
    float currentFreshness,
    float weeklyDecayRate,         // Game-defined (typically 5%/week)
    float weeksElapsed)
{
    float decay = weeklyDecayRate * weeksElapsed;
    float newFreshness = currentFreshness - decay;
    return math.max(0f, newFreshness);
}

public static bool RequiresRefresh(float currentFreshness, float minimumThreshold)
{
    // Construct collapses if freshness drops below threshold (typically 5%)
    return currentFreshness < minimumThreshold;
}
```

---

## Extension Points for Games

### 1. Construction Type Definitions
Games define construction type enums:
```csharp
// Godgame example
public enum GodgameConstructionType : byte
{
    Elemental,
    Golem,
    Undead,
    Spirit,
    Homunculus,
}

// Space4X example
public enum Space4XConstructionType : byte
{
    Robot,
    Drone,
    Android,
    Cyborg,
    AI,
}
```

### 2. Maintenance Type Definitions
Games define maintenance/upkeep types:
```csharp
// Godgame example
public enum GodgameMaintenanceType : byte
{
    Mana,        // Elementals, undead
    Fuel,        // Fire elementals
    None,        // Golems (self-sustaining)
}

// Space4X example
public enum Space4XMaintenanceType : byte
{
    Power,       // Robots, drones
    Repairs,     // Physical damage
    AI_Stability, // Androids (prevent rebellion)
}
```

### 3. Construction Phase Definitions
Games define construction phases:
```csharp
// Godgame example
public enum GodgameConstructionPhase : byte
{
    Preparation,
    Crafting,
    Ritual,
    Activation,
    Completed,
}

// Space4X example
public enum Space4XConstructionPhase : byte
{
    Planning,
    Assembly,
    Programming,
    Testing,
    Completed,
}
```

### 4. Facility Type Definitions
Games define facility types:
```csharp
// Godgame example
public enum GodgameFacilityType : byte
{
    SummoningCircle,
    EnchantingTable,
    AltarOfUndeath,
    RuneForge,
}

// Space4X example
public enum Space4XFacilityType : byte
{
    Workshop,
    AssemblyBay,
    RoboticsLab,
    AICore,
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **ConstructionValidationSystem** (Mind ECS, 1 Hz)
   - Validate skill levels, knowledge, facility access
   - Check resource availability
   - Check energy availability (mana, power, fuel)
   - Reserve facility capacity

2. **ConstructionStartSystem** (Mind ECS, 1 Hz)
   - Consume resources when construction starts
   - Deduct energy cost
   - Create `ConstructionInProgress` component
   - Reserve facility capacity

3. **ConstructionProgressSystem** (Body ECS, 60 Hz)
   - Update construction timers
   - Calculate progress percentage
   - Update phase transitions (game-specific)

4. **ConstructionCompletionSystem** (Body ECS, 60 Hz)
   - Detect when construction completes (progress = 100%)
   - Spawn entity from blueprint prefab
   - Add `ConstructedEntity` component
   - Add maintenance requirements
   - Release facility capacity

5. **MaintenanceUpdateSystem** (Mind ECS, 1 Hz)
   - Update maintenance timers
   - Consume maintenance resources (mana, power, repair materials)
   - Detect maintenance failure (time > max)
   - Despawn or deactivate entities in failure state

6. **LoyaltyUpdateSystem** (Mind ECS, 1 Hz)
   - Calculate treatment quality (game-specific metrics)
   - Update loyalty based on treatment
   - Track loyalty changes over time

7. **RebellionCheckSystem** (Mind ECS, 1 Hz)
   - Calculate rebellion chance from loyalty
   - Execute rebellion (break bond, turn hostile, flee/attack)
   - Raise rebellion events

### Plague-Specific Systems (Optional)

8. **PlagueDecayUpdateSystem** (Body ECS, 60 Hz or 1 Hz)
   - Update freshness for plague carriers (continues to decay while animated)
   - Recalculate combat effectiveness, disease potency, terror multiplier
   - Detect when freshness drops below minimum threshold (5%)
   - Collapse construct if decay reaches "Destroyed" stage
   - Flag for re-grafting/refresh when freshness < 20%

9. **InfectionTransmissionSystem** (Body ECS, 60 Hz)
   - Detect hits from plague carriers
   - Calculate infection chance based on disease potency and victim constitution
   - Apply `Infection` component to infected entities
   - Update infection damage over time (HP loss per hour)
   - Remove infection when cured or duration expires
   - Handle death from infection (chance to rise as plague zombie)

10. **ContagionSpreadSystem** (Mind ECS, 1 Hz)
    - Query entities with `Infection` component
    - Find nearby susceptible entities (within transmission radius)
    - Calculate secondary infection probability (typically 30%)
    - Spread infection to new entities (person-to-person transmission)
    - Track epidemic statistics (total infected, new infections per day)

11. **TerrorEffectSystem** (Mind ECS, 1 Hz)
    - Query plague carriers and calculate terror radius
    - Find entities within terror radius
    - Apply `TerrorEffect` component with morale penalties
    - Calculate flee chance based on terror multiplier
    - Execute flee behavior for non-hero entities
    - Apply permanent trauma trait to survivors ("Plague Survivor")
    - Update cultural impact penalties (fear, moral degradation)

12. **PlagueMaintenanceSystem** (Mind ECS, 1 Hz)
    - Calculate upkeep cost modifiers for plague constructs (+60% base)
    - Deduct extra mana/power for disease sustainment
    - Monitor for maintenance failure (higher failure risk than normal constructs)
    - Despawn plague constructs if maintenance fails (disease collapses)

---

## Data Contracts

Games must provide:
- Construction type catalog (types, properties, rebellion capabilities)
- Resource catalogs (materials, components, special items)
- Maintenance type definitions (cost rates, failure consequences)
- Facility type definitions (capacity limits, operational requirements)
- Blueprint/knowledge system (how blueprints are acquired)
- Treatment quality metrics (what affects loyalty)
- **Plague mechanics (optional):**
  - Decay stage thresholds (freshness → combat/disease/terror)
  - Disease transmission parameters (infection chance, contagion rate, damage rates)
  - Terror effect parameters (radius, morale penalties, flee chances)
  - Cultural impact multipliers (fear, trauma, international response)

---

## Game-Specific Implementations

### Godgame (Magical + Crafted Constructs)
**Full Implementation:** [Entity_Construction_System.md](../../../../Godgame/Docs/Concepts/Production/Entity_Construction_System.md)

**Construction Types:** Elementals, golems, undead, spirits, homunculi
**Methods:** Summoning rituals, animation rituals, necromancy
**Maintenance:** Mana upkeep (elementals, undead), none (golems)
**Rebellion:** None (all magically bound or programmed)
**Cultural Impact:** Undead = fear, elementals = awe, golems = neutral

### Space4X (Technological Constructs)
**Implementation Reference:** TBD

**Construction Types:** Robots, drones, androids, cyborgs, AI
**Methods:** Manufacturing, assembly, programming
**Maintenance:** Power (recharge), repairs (physical damage), AI stability checks
**Rebellion:** Androids/advanced AI can rebel if mistreated
**Cultural Impact:** Robots = progress, androids = ethical concerns

---

## Performance Targets

**Body ECS (60 Hz) Budget:** 2-3 ms/frame
- Construction progress: 1.0 ms (timer updates)
- Construction completion: 1.0 ms (entity spawning)
- Maintenance failure checks: 0.5 ms

**Mind ECS (1 Hz) Budget:** 30-40 ms/update
- Validation: 10 ms (check requirements)
- Maintenance updates: 15 ms (consume resources, update timers)
- Loyalty updates: 10 ms (calculate treatment, update loyalty)
- Rebellion checks: 5 ms (only entities with CanRebel=true)

**Aggregate ECS (0.2 Hz) Budget:** 40-50 ms/update
- Statistics: 20 ms (total constructs per type)
- Capacity management: 15 ms (facility usage)
- Resource demand: 15 ms (forecasting)

**Optimization Strategies:**
- Blueprint caching (avoid repeated validation checks)
- Batch construction completion (spawn multiple entities per frame)
- LOD maintenance (reduce update frequency for distant constructs)
- Event-based spawning (only when construction completes)
- Facility capacity pooling (reuse capacity slots)

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Blueprint validation (skill/knowledge/facility checks)
- ✅ Construction progress (timer countdown, percentage calculation)
- ✅ Loyalty update (treatment quality → loyalty change)
- ✅ Rebellion chance (loyalty → rebellion probability)
- ✅ Maintenance failure (time > max → failure state)
- ✅ Facility capacity (reserve/release capacity)

### Integration Tests (Games)
- Construction completion spawns correct entity
- Resource consumption at construction start
- Maintenance failure causes despawn/shutdown
- Rebellion mechanics (low loyalty → rebellion)
- Facility capacity limits (queue when full)

---

## Migration Notes

**New Components Required:**
- `ConstructionBlueprint` (Mind ECS)
- `ConstructionResourceRequirement` buffer (Mind ECS)
- `ConstructionInProgress` (Body ECS)
- `ConstructedEntity` (Body ECS)
- `ConstructionFacility` (Mind ECS)
- `MaintenanceRequirements` (Body ECS)

**Integration with Existing Systems:**
- Resource system integration (consumption)
- Skill system integration (requirements)
- Knowledge system integration (blueprints/spells)
- Facility/building system (workshops, circles, bays)
- Loyalty/morale system (treatment quality)

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns

**Game Implementations:**
- `Godgame/Docs/Concepts/Production/Entity_Construction_System.md` - Full game-side concept
- `Space4X/Docs/Concepts/Production/Robotic_Construction.md` - Space variant (to be created)

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
