# Catalytic Crystal System (Unstable Energy Material)

## Overview

Self-propagating crystalline material (inspired by Tiberium) that grows on worlds, providing powerful energy resources but corrupting flora, fauna, and terrain unless actively excised. Crystals spread through soil/rock, emit radiation, mutate organisms, and destabilize ecosystems. High-risk, high-reward resource management where extraction yields massive energy but unchecked growth causes environmental collapse.

**Key Principles**:
- **Self-propagating**: Crystals spread autonomously, consuming nutrients from soil/rock
- **Energy source**: Extremely high energy density (10× normal resources)
- **Environmental corruption**: Mutates flora/fauna, poisons water, destabilizes terrain
- **Exponential growth**: Spreads faster as crystal mass increases
- **Active management required**: Must be harvested/excised or it overtakes ecosystems
- **Mutation mechanics**: Creates dangerous mutant creatures and corrupted plants
- **Containment strategies**: Barriers, radiation shielding, controlled burn zones
- **Cross-game**: Crystal fields (Godgame), asteroid infestations (Space4X)
- **Moddable**: All growth rates, mutation chances, energy yields runtime-adjustable
- **Deterministic**: Same seed + same conditions = same crystal spread patterns

---

## Crystal Core Components

### Crystal Definition

```csharp
public struct CatalyticCrystal : IComponentData
{
    public int2 GridPosition;              // World grid location
    public CrystalType Type;
    public CrystalGrowthStage Stage;
    public float CrystalMass;               // kg of crystal at this location
    public float GrowthRate;                // kg per hour
    public float RadiationLevel;            // 0.0 to 10.0+ (dangerous at 5.0+)
    public float EnergyDensity;             // kWh per kg
    public float ToxicityLevel;             // Soil/water contamination
    public uint Age;                        // Ticks since formation
    public bool IsStable;                   // False = high mutation/explosion risk
}

public enum CrystalType : byte
{
    // Basic types
    Green = 0,              // Standard, moderate growth, 10× energy
    Blue = 1,               // Slow growth, high energy (15×), low toxicity
    Red = 2,                // Fast growth, explosive, 8× energy
    Purple = 3,             // Rare, very high energy (20×), extreme mutations

    // Advanced/mutated types
    Veined = 10,            // Spreads through underground veins
    Blossom = 11,           // Above-ground flowering crystals
    Nexus = 12,             // Hub that accelerates nearby crystal growth
    Reactive = 13,          // Explodes when damaged
    Psionic = 14            // Affects minds, creates hallucinations (Space4X)
}

public enum CrystalGrowthStage : byte
{
    Seedling = 0,           // Just formed, <10kg
    Sprouting = 1,          // Growing, 10-100kg
    Mature = 2,             // Stable growth, 100-1000kg
    Blooming = 3,           // Peak growth, 1000-10000kg
    Overgrown = 4,          // Dangerous, >10000kg, spreading rapidly
    Critical = 5            // Unstable, imminent explosion/mutation event
}

public struct CrystalField : IComponentData
{
    public int2 CenterPosition;
    public float FieldRadius;               // Meters
    public uint CrystalCount;               // Number of crystal nodes
    public float TotalMass;                 // Total kg in field
    public float CombinedRadiation;         // Overlapping radiation zones
    public float SpreadRate;                // m² per hour
}
```

### Crystal Growth Mechanics

```csharp
public struct CrystalGrowth : IComponentData
{
    public float BaseGrowthRate;            // kg per hour per crystal
    public float NutrientConsumptionRate;   // Depletes soil/rock
    public float SpreadChance;              // % chance to create new node per hour
    public float SpreadRadius;              // Max distance for new nodes
    public GrowthCurveType CurveType;

    // Environmental requirements
    public float OptimalTemperature;        // Grows fastest at this temp
    public float MinimumMoisture;           // Needs some water
    public bool RequiresSunlight;           // Some types photosynthetic
}

public enum GrowthCurveType : byte
{
    Linear = 0,         // Constant growth rate
    Exponential = 1,    // Accelerating growth (Tiberium-like)
    Logistic = 2,       // S-curve (rapid then plateau)
    Cyclic = 3          // Grows in pulses
}

[BurstCompile]
public partial struct CrystalGrowthSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (crystal, growth) in SystemAPI.Query<
            RefRW<CatalyticCrystal>,
            RefRO<CrystalGrowth>>())
        {
            // Check environmental conditions
            var tile = GetTileAt(crystal.ValueRO.GridPosition);
            float envMultiplier = CalculateEnvironmentalMultiplier(growth.ValueRO, tile);

            // Calculate growth based on curve type
            float growthAmount = growth.ValueRO.CurveType switch
            {
                GrowthCurveType.Linear =>
                    growth.ValueRO.BaseGrowthRate * deltaTime,

                GrowthCurveType.Exponential =>
                    growth.ValueRO.BaseGrowthRate * math.pow(1.1f, crystal.ValueRO.Age / 3600f) * deltaTime,

                GrowthCurveType.Logistic =>
                    growth.ValueRO.BaseGrowthRate * (1f - crystal.ValueRO.CrystalMass / 10000f) * deltaTime,

                GrowthCurveType.Cyclic =>
                    growth.ValueRO.BaseGrowthRate * math.sin(crystal.ValueRO.Age / 1800f) * deltaTime,

                _ => 0f
            };

            // Apply environmental modifier
            growthAmount *= envMultiplier;

            // Consume nutrients from tile
            float nutrientsNeeded = growthAmount * growth.ValueRO.NutrientConsumptionRate;
            if (tile.Nutrients >= nutrientsNeeded)
            {
                // Grow crystal
                crystal.ValueRW.CrystalMass += growthAmount;
                tile.Nutrients -= nutrientsNeeded;

                // Update growth stage
                crystal.ValueRW.Stage = GetGrowthStage(crystal.ValueRO.CrystalMass);

                // Increase radiation
                crystal.ValueRW.RadiationLevel = CalculateRadiation(crystal.ValueRO.CrystalMass);

                // Check for spreading
                if (UnityEngine.Random.value < growth.ValueRO.SpreadChance * deltaTime)
                {
                    SpreadCrystal(crystal.ValueRO, growth.ValueRO.SpreadRadius);
                }
            }
            else
            {
                // Insufficient nutrients, slower growth or dormancy
                crystal.ValueRW.GrowthRate *= 0.5f;
            }

            // Check stability
            crystal.ValueRW.IsStable = CheckStability(crystal.ValueRO);
        }
    }

    private void SpreadCrystal(in CatalyticCrystal source, float spreadRadius)
    {
        // Find valid adjacent tile
        float2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
        int2 targetPos = source.GridPosition + new int2((int)offset.x, (int)offset.y);

        var targetTile = GetTileAt(targetPos);
        if (targetTile.IsValidForCrystal && !HasCrystal(targetPos))
        {
            // Create new crystal seedling
            CreateCrystalSeedling(targetPos, source.Type);
        }
    }
}
```

### Crystal Spread Patterns

```csharp
public struct CrystalSpreadPattern : IComponentData
{
    public SpreadPatternType PatternType;
    public float SpreadSpeed;               // m/hour
    public float PreferredDirection;        // Radians (some crystals spread directionally)
    public bool PreferWater;                // Spreads faster near water
    public bool PreferRichSoil;             // Spreads faster in fertile areas
}

public enum SpreadPatternType : byte
{
    Radial = 0,             // Spreads evenly in all directions
    Directional = 1,        // Spreads in dominant direction (wind, water flow)
    Vein = 2,               // Follows underground mineral veins
    Bloom = 3,              // Explosive spread from nexus points
    Creeping = 4,           // Slow, methodical spread
    Invasive = 5            // Opportunistic, targets weak ecosystems
}

// Example spread rates:
// Green crystal (radial): 5 m²/hour
// Red crystal (bloom): 20 m²/hour (explosive)
// Veined crystal: 15 m²/hour along mineral veins
```

---

## Environmental Corruption

### Flora Corruption

Crystals mutate and corrupt plant life:

```csharp
public struct FloraCorruption : IComponentData
{
    public Entity Plant;
    public float CorruptionLevel;           // 0.0 to 1.0
    public CorruptionStage Stage;
    public float ExposureTime;              // Seconds near crystals
    public MutationType Mutation;
}

public enum CorruptionStage : byte
{
    Healthy = 0,            // No corruption
    Wilting = 1,            // Early stage, 10-30% corruption
    Infested = 2,           // Crystals growing on plant, 30-60%
    Mutated = 3,            // Plant transformed, 60-90%
    Crystalline = 4         // Fully converted to crystal, 90-100%
}

public enum MutationType : byte
{
    None = 0,
    ToxicSpores = 1,        // Releases toxic spores
    CrystallineBark = 2,    // Bark becomes crystal, plant dies
    GlowingFruit = 3,       // Fruit becomes radioactive
    ThornGrowth = 4,        // Aggressive thorns grow
    RapidGrowth = 5,        // Grows uncontrollably
    Carnivorous = 6         // Plant becomes predatory
}

[BurstCompile]
public partial struct FloraCorruptionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var corruption in SystemAPI.Query<RefRW<FloraCorruption>>())
        {
            // Check proximity to crystals
            var nearestCrystal = FindNearestCrystal(corruption.ValueRO.Plant);
            if (nearestCrystal != Entity.Null)
            {
                float distance = GetDistance(corruption.ValueRO.Plant, nearestCrystal);
                float radiation = GetRadiation(nearestCrystal);

                // Corruption increases with radiation exposure
                float corruptionRate = radiation / (distance + 1f) * 0.01f; // Per second
                corruption.ValueRW.CorruptionLevel += corruptionRate * deltaTime;
                corruption.ValueRW.ExposureTime += deltaTime;

                // Update corruption stage
                corruption.ValueRW.Stage = corruption.ValueRO.CorruptionLevel switch
                {
                    < 0.1f => CorruptionStage.Healthy,
                    < 0.3f => CorruptionStage.Wilting,
                    < 0.6f => CorruptionStage.Infested,
                    < 0.9f => CorruptionStage.Mutated,
                    _ => CorruptionStage.Crystalline
                };

                // Check for mutation
                if (corruption.ValueRO.CorruptionLevel > 0.5f &&
                    corruption.ValueRO.Mutation == MutationType.None)
                {
                    // Roll for mutation
                    if (UnityEngine.Random.value < 0.1f) // 10% chance
                    {
                        corruption.ValueRW.Mutation = RollMutation();
                        ApplyMutation(corruption.ValueRO.Plant, corruption.ValueRO.Mutation);
                    }
                }

                // Full crystallization = plant death
                if (corruption.ValueRO.Stage == CorruptionStage.Crystalline)
                {
                    ConvertPlantToCrystal(corruption.ValueRO.Plant);
                }
            }
            else
            {
                // No nearby crystals, corruption slowly fades
                corruption.ValueRW.CorruptionLevel = math.max(0f,
                    corruption.ValueRO.CorruptionLevel - (0.01f * deltaTime));
            }
        }
    }
}
```

### Fauna Corruption and Mutation

Animals exposed to crystal radiation mutate:

```csharp
public struct FaunaCorruption : IComponentData
{
    public Entity Creature;
    public float CorruptionLevel;
    public float RadiationDose;             // Accumulated radiation (Sieverts)
    public MutationSeverity Severity;
    public uint MutationCount;              // Number of mutations
}

public enum MutationSeverity : byte
{
    None = 0,
    Minor = 1,              // Cosmetic changes, minor stat changes
    Moderate = 2,           // Significant stat changes, behavior changes
    Major = 3,              // Dramatic transformation, hostile
    Abomination = 4         // Completely transformed, extremely dangerous
}

[InternalBufferCapacity(4)]
public struct CreatureMutation : IBufferElementData
{
    public CreatureMutationType Type;
    public float Magnitude;                 // Strength of mutation
    public uint AcquiredTick;
}

public enum CreatureMutationType : byte
{
    // Physical mutations
    CrystallineHide = 0,        // +defense, -speed
    EnlargedLimbs = 1,          // +strength, +size
    ExtraLimbs = 2,             // +attack speed, grotesque
    GlowingEyes = 3,            // +vision, emits light
    RadioactiveBite = 4,        // Attacks deal radiation damage

    // Behavioral mutations
    Aggression = 10,            // Becomes hostile
    Fearlessness = 11,          // Ignores danger
    PackHunting = 12,           // Coordinates with other mutants
    Territorial = 13,           // Defends crystal fields

    // Ability mutations
    CrystalGrowth = 20,         // Grows crystals on body
    RadiationEmission = 21,     // Emits radiation (corrupts nearby)
    ToxicSpit = 22,             // Ranged toxic attack
    RegenerativeHealing = 23,   // Heals near crystals
    Burrowing = 24              // Can tunnel underground
}

[BurstCompile]
public partial struct FaunaMutationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (corruption, mutations) in SystemAPI.Query<
            RefRW<FaunaCorruption>,
            DynamicBuffer<CreatureMutation>>())
        {
            // Accumulate radiation dose
            float nearbyRadiation = GetNearbyRadiation(corruption.ValueRO.Creature);
            corruption.ValueRW.RadiationDose += nearbyRadiation * SystemAPI.Time.DeltaTime;

            // Calculate corruption level from dose
            corruption.ValueRW.CorruptionLevel = math.min(1f,
                corruption.ValueRO.RadiationDose / 100f); // 100 Sv = full corruption

            // Determine severity
            corruption.ValueRW.Severity = corruption.ValueRO.CorruptionLevel switch
            {
                < 0.1f => MutationSeverity.None,
                < 0.3f => MutationSeverity.Minor,
                < 0.6f => MutationSeverity.Moderate,
                < 0.9f => MutationSeverity.Major,
                _ => MutationSeverity.Abomination
            };

            // Mutation checks at thresholds
            if (ShouldGainMutation(corruption.ValueRO))
            {
                var newMutation = GenerateMutation(corruption.ValueRO.Severity);
                mutations.Add(newMutation);
                corruption.ValueRW.MutationCount++;

                ApplyMutationEffects(corruption.ValueRO.Creature, newMutation);
            }

            // Abominations are extremely dangerous
            if (corruption.ValueRO.Severity == MutationSeverity.Abomination)
            {
                MakeAbomination(corruption.ValueRO.Creature, mutations);
            }
        }
    }

    private void MakeAbomination(Entity creature, DynamicBuffer<CreatureMutation> mutations)
    {
        // Apply abomination template:
        // - +200% health
        // - +150% damage
        // - +100% size
        // - Emits radiation (corrupts nearby entities)
        // - Regenerates health near crystals
        // - Extremely aggressive
        // - Visually horrifying
    }
}
```

### Terrain Corruption

Crystals poison soil and destabilize terrain:

```csharp
public struct TerrainCorruption : IComponentData
{
    public int2 GridPosition;
    public float SoilToxicity;              // 0.0 to 1.0
    public float WaterContamination;        // 0.0 to 1.0
    public float StructuralIntegrity;       // 1.0 to 0.0 (weakening)
    public bool IsBarren;                   // Nothing can grow
    public bool IsUnstable;                 // Collapse risk
}

public struct CrystalizedTerrain : IComponentData
{
    public float CrystallizationLevel;      // % of tile converted to crystal
    public bool IsTraversable;              // Can entities walk on it?
    public float CollapseDanger;            // % chance to collapse per hour
}

// Effects of terrain corruption:
// - Soil toxicity kills non-mutated plants
// - Water contamination spreads to rivers/lakes
// - Structural integrity loss causes cave-ins, landslides
// - Fully crystallized terrain becomes impassable (unless harvested)
```

---

## Harvesting and Extraction

### Crystal Harvesting

```csharp
public struct CrystalHarvester : IComponentData
{
    public Entity HarvesterEntity;
    public float HarvestRate;               // kg per hour
    public float ProcessingEfficiency;      // % of energy extracted
    public float RadiationShielding;        // % radiation blocked
    public bool IsOperational;
}

public struct CrystalExtraction : IComponentData
{
    public Entity TargetCrystal;
    public float ExtractionProgress;        // 0.0 to 1.0
    public float EnergyYield;               // kWh extracted so far
    public float WasteGenerated;            // Toxic byproduct (kg)
    public bool IsContained;                // Safe extraction vs exposed
}

// Harvesting risks:
// - Radiation exposure to workers (requires shielding)
// - Crystal can explode if damaged (reactive types)
// - Waste products are toxic (must be disposed)
// - Harvesting too fast destabilizes remaining crystals
```

### Energy Yield

```csharp
public struct CrystalEnergyData
{
    // Energy yields per kg:
    // Normal resource (coal): 10 kWh/kg
    // Green crystal: 100 kWh/kg (10×)
    // Blue crystal: 150 kWh/kg (15×)
    // Red crystal: 80 kWh/kg (8×, but explosive)
    // Purple crystal: 200 kWh/kg (20×, extremely rare)

    public static float GetEnergyYield(CrystalType type, float mass)
    {
        float energyPerKg = type switch
        {
            CrystalType.Green => 100f,
            CrystalType.Blue => 150f,
            CrystalType.Red => 80f,
            CrystalType.Purple => 200f,
            CrystalType.Veined => 110f,
            CrystalType.Blossom => 95f,
            CrystalType.Nexus => 130f,
            CrystalType.Reactive => 75f, // Lower due to waste from explosions
            CrystalType.Psionic => 180f,
            _ => 100f
        };

        return energyPerKg * mass;
    }
}
```

---

## Containment and Excision

### Containment Strategies

```csharp
public struct CrystalContainment : IComponentData
{
    public ContainmentType Type;
    public float Effectiveness;             // % growth rate reduction
    public float MaintenanceCost;           // Resources per hour
    public bool RequiresPower;
}

public enum ContainmentType : byte
{
    // Physical barriers
    SonicFence = 0,         // Disrupts crystal growth with sound
    RadiationShield = 1,    // Blocks radiation spread
    ConcreteWall = 2,       // Physical barrier (crystals can break through)

    // Chemical methods
    IonSpray = 10,          // Inhibits crystal growth temporarily
    AcidWash = 11,          // Dissolves crystals slowly
    Herbicide = 12,         // Kills mutated plants

    // Advanced tech
    StasisField = 20,       // Freezes crystal growth (high power cost)
    QuantumBarrier = 21,    // Perfect containment (extremely expensive)
    NaniteSwarm = 22        // Actively disassembles crystals
}

// Containment costs vs benefits:
// Sonic fence: 10 power/hour, 60% reduction, 90% effective
// Stasis field: 100 power/hour, 100% reduction, perfect containment
// Nanite swarm: 50 power/hour + 10 metal/hour, actively reduces crystal mass
```

### Excision and Removal

```csharp
public struct CrystalExcision : IComponentData
{
    public ExcisionMethod Method;
    public float RemovalRate;               // kg per hour
    public float EnergyRecovery;            // % of energy salvaged
    public float CollateralDamage;          // % damage to nearby entities
}

public enum ExcisionMethod : byte
{
    ManualHarvest = 0,      // Slow, safe, 90% recovery
    Explosives = 1,         // Fast, dangerous, 20% recovery, high collateral
    LaserCutting = 2,       // Moderate speed, 70% recovery, precise
    ControlledBurn = 3,     // Destroys crystals, 0% recovery, prevents spread
    Dissolution = 4         // Chemical breakdown, 50% recovery, slow
}

// Example: Explosive excision
// - Removal rate: 10,000 kg/hour
// - Energy recovery: 20%
// - Collateral damage: 500m radius, 50% damage to structures/entities
// - Risk: Can trigger chain reaction in dense crystal fields
```

---

## Crystal Events and Instabilities

### Crystal Bloom Events

Massive explosive growth:

```csharp
public struct CrystalBloomEvent : IComponentData
{
    public int2 EpicenterPosition;
    public float BloomRadius;               // Affected area (km)
    public float GrowthMultiplier;          // Temporary growth boost
    public uint Duration;                   // Ticks
    public bool IsActive;
}

// Bloom event triggers:
// - Crystal field reaches critical mass (>100,000 kg)
// - Multiple nexus crystals in proximity
// - Environmental trigger (meteor impact, earthquake)
// - Random chance (1% per day in mature fields)

// Bloom effects:
// - 10× growth rate for duration
// - Spreads 50× faster
// - High mutation rate
// - Creates new nexus points
// - Can devastate entire biomes in hours
```

### Crystal Detonation

Reactive crystals can explode:

```csharp
public struct CrystalDetonation : IComponentData
{
    public int2 Position;
    public float ExplosiveYield;            // Equivalent TNT (kg)
    public float RadiationRelease;          // Sieverts at epicenter
    public float ShrapnelRadius;            // m
}

public enum DetonationTrigger : byte
{
    Damage = 0,             // Hit by weapon/explosion
    Overload = 1,           // Too much mass/energy
    ChainReaction = 2,      // Nearby crystal exploded
    Destabilization = 3,    // Harvested too aggressively
    Lightning = 4           // Environmental trigger
}

// Example: Red crystal detonation (1000 kg)
// - Explosive yield: 5,000 kg TNT equivalent
// - Radiation release: 50 Sv at epicenter (lethal)
// - Shrapnel radius: 200m
// - Creates 10-20 new crystal seedlings (spreads field)
// - Can trigger chain reaction if multiple crystals nearby
```

### Psionic Anomalies (Space4X)

Purple/psionic crystals affect minds:

```csharp
public struct PsionicCrystalEffect : IComponentData
{
    public PsionicEffectType Effect;
    public float Intensity;
    public float Range;
}

public enum PsionicEffectType : byte
{
    Hallucinations = 0,     // Entities see things that aren't there
    Paranoia = 1,           // Increased aggression, distrust
    Euphoria = 2,           // Drawn to crystals (addiction)
    Madness = 3,            // Complete mental breakdown
    Telepathy = 4,          // Unintended mind-reading
    PsychicScreams = 5      // Psychic damage to nearby entities
}
```

---

## Cross-Game Variants

### Godgame: Crystal Fields

```csharp
// Crystals appear on terrain, spread across land
// - Threatens villages, corrupts farms
// - Villagers can harvest for massive energy boost
// - Mutates wildlife into dangerous creatures
// - Player must choose: exploit or eradicate
// - Miracles can accelerate/suppress crystal growth
// - Advanced tech: sonic fences, harvesting refineries

// Strategic choice:
// Option A: Harvest crystals aggressively
//   - Massive energy income
//   - Faster tech progression
//   - High mutation/corruption risk
//   - Villagers suffer radiation sickness
//
// Option B: Eradicate crystals immediately
//   - Safe environment
//   - Lower energy income
//   - Prevents catastrophic bloom events
//   - Villagers healthier
```

### Space4X: Asteroid Infestations

```csharp
// Crystals infest asteroids, planets, space stations
// - Spreads across asteroid belts
// - Can infest ship hulls if not contained
// - Valuable but dangerous cargo
// - Creates mutant space creatures
// - Planetary infestations threaten colonies

// Infestation scenarios:
// 1. Crystal asteroid discovered
//    - Mine for energy (10× yield)
//    - Risk spreading to nearby asteroids
//    - Can infest mining ships
//
// 2. Planet surface infestation
//    - Threatens colony
//    - Must be excised or evacuate
//    - Orbital bombardment option (destroys colony)
//
// 3. Derelict ship with crystal growth
//    - High salvage value
//    - Crystals spread to salvage crew
//    - Creates infected crew (mutants)
```

---

## Integration with Other Systems

**Overlay System**: Crystal distribution overlay shows fields, radiation zones, spread predictions

**Forces System**: Radiation acts as area-of-effect force, damages entities over time

**Ritual System**: Crystals can be used in rituals for massive power (but dangerous backlash)

**Cooperation System**: Harvesting crews require high cohesion to manage danger

**Formations**: Mutant creatures use pack tactics, require coordinated response

**Sandbox Modding**: All growth rates, mutation chances, energy yields runtime-adjustable

---

## Example Scenarios

### Scenario 1: Early Crystal Discovery (Godgame)

```csharp
// Day 1: Scout discovers small green crystal field (500 kg)
var field = new CrystalField
{
    CenterPosition = new int2(100, 50),
    CrystalCount = 5,
    TotalMass = 500f,
    SpreadRate = 5f // 5 m²/hour
};

// Day 7: Field has grown to 3,500 kg (exponential growth)
// - Spreading at 35 m²/hour
// - Nearby forest showing corruption (10% of trees wilting)
// - 2 deer mutated (minor mutations, crystalline hide)

// Player decision:
// Option A: Build harvester, extract energy
//   - 350,000 kWh available (10× normal coal)
//   - Enables advanced tech 50% faster
//   - Risk: Field continues growing while harvesting
//
// Option B: Immediate excision with controlled burn
//   - Destroys all crystals
//   - 0% energy recovery
//   - Prevents spread to village (200m away)
//
// Option C: Contain with sonic fence, monitor
//   - 60% growth reduction
//   - 10 power/hour maintenance
//   - Harvest later when tech improves

// Player chooses Option A
// Day 14: Harvester operational, extracting 500 kg/day
// - Energy income: +50,000 kWh/day
// - Field growing faster than harvesting (now 8,000 kg)
// - Forest 40% corrupted
// - 10 mutant deer (moderate severity, aggressive)
// - 3 villagers showing radiation sickness

// Day 21: Crisis
// - Field reaches 20,000 kg (critical mass)
// - Crystal bloom event triggered
// - Growth rate 10×, spreading 350 m²/hour
// - Creates 3 new nexus crystals
// - Mutant deer abomination created (major threat)
// - Village evacuated

// Player must now choose:
// - Explosive excision (destroys field, village damaged)
// - Upgrade to stasis field (expensive, perfect containment)
// - Relocate village, exploit massive crystal field (high risk/reward)
```

### Scenario 2: Red Crystal Detonation Chain Reaction

```csharp
// Scenario: Player mining red crystal field aggressively
var field = new CrystalField
{
    Type = CrystalType.Red, // Explosive
    TotalMass = 15,000f,
    CrystalCount = 30
};

// Harvester damages one crystal during extraction
// Crystal 1 detonates:
// - 500 kg TNT equivalent explosion
// - Damages 5 nearby crystals
// - Creates shrapnel (kills 3 workers)

// Chain reaction begins:
// Crystal 2-6 detonate (damaged by Crystal 1)
// - Each 200-800 kg TNT equivalent
// - Damages 10 more crystals
// - Harvester destroyed

// Crystal 7-16 detonate
// - Combined yield: 5,000 kg TNT equivalent
// - Radiation release: 200 Sv (lethal to all nearby)
// - Creates crater 50m diameter

// Result:
// - Entire field destroyed
// - 0% energy recovery
// - 25 workers dead
// - Contaminated zone 500m radius (uninhabitable for months)
// - New crystal seedlings created (field will regrow)

// Lesson: Red crystals require careful, slow extraction
```

### Scenario 3: Psionic Crystal Madness (Space4X)

```csharp
// Research ship discovers purple psionic crystal asteroid
var asteroid = new CrystalField
{
    Type = CrystalType.Psionic,
    TotalMass = 2,000f,
    RadiationLevel = 8.0f // Extreme
};

// Week 1: Research team studying crystal
// - Energy readings off the charts (200 kWh/kg)
// - Team reports vivid dreams
// - 2 crew members claim telepathic contact

// Week 2: Psychological effects intensify
// - 5 crew members experiencing hallucinations
// - 1 crew member obsessed with crystal (euphoria effect)
// - Ship sensors detecting psionic emissions

// Week 3: Breakdown
// - 10 crew members (50%) affected
// - Obsessed crew member sabotages containment
// - Crystal exposed to ship atmosphere
// - Psionic screams broadcast ship-wide
// - 3 crew members suffer psychic damage (madness)
// - 1 crew member tries to protect crystal (paranoia)

// Week 4: Rescue
// - Distress signal sent
// - Rescue ship arrives
// - Must choose:
//   - Board ship, retrieve survivors (risk psionic exposure)
//   - Destroy ship with long-range weapons (kills crew, prevents spread)
//   - Quarantine ship, wait for specialized team (slow)

// Outcome: Psionic crystals extremely dangerous, require specialized handling
```

---

## Moddable Parameters

All crystal parameters runtime-adjustable for creative iteration:

```csharp
public struct CrystalModParameters : IComponentData
{
    // Growth parameters
    public float GlobalGrowthMultiplier;    // Scale all growth rates
    public float SpreadChanceMultiplier;    // Control spread speed
    public float ExponentialGrowthPower;    // Adjust exponential curve

    // Energy parameters
    public float EnergyYieldMultiplier;     // Scale energy value
    public float HarvestEfficiency;         // % energy recovered

    // Corruption parameters
    public float CorruptionRateMultiplier;  // Speed of mutation
    public float MutationChance;            // % chance per threshold
    public float RadiationIntensity;        // Radiation damage multiplier

    // Containment parameters
    public float ContainmentEffectiveness;  // % effectiveness multiplier
    public float ExcisionSpeed;             // Removal rate multiplier

    // Event parameters
    public float BloomEventChance;          // % chance per day
    public float DetonationSensitivity;     // How easily crystals explode
    public float PsionicIntensity;          // Psionic effect strength
}

// Sandbox profiles:
// "Tiberium Classic": Exponential growth, high energy, dangerous
// "Controlled Resource": Slow growth, moderate energy, safer
// "Apocalypse Mode": Explosive growth, chain detonations, extinction-level
// "Energy Paradise": High yield, low danger, easy harvesting
```

---

## Performance Targets

```
Crystal Growth Update:    <0.1ms per crystal
Corruption Calculation:   <0.05ms per entity
Spread Check:             <0.2ms per field
Harvesting:               <0.1ms per harvester
Mutation Roll:            <0.03ms per entity
─────────────────────────────────────────
Total (1000 crystals):    <500ms per frame
```

---

## Summary

**Material**: Self-propagating catalytic crystals, 10-20× energy density of normal resources

**Growth**: Exponential/logistic curves, spreads 5-350 m²/hour depending on type/stage

**Environmental Impact**:
- Flora: Wilting → Infested → Mutated → Crystallized (toxic spores, carnivorous plants)
- Fauna: Minor → Moderate → Major → Abomination mutations (extra limbs, radioactive bite, regeneration)
- Terrain: Soil toxicity, water contamination, structural collapse

**Energy Yield**: 100-200 kWh/kg (vs 10 kWh/kg for coal), massive power source

**Risks**:
- Radiation exposure (lethal at 50+ Sv)
- Explosive detonations (red crystals, 5,000 kg TNT equivalent)
- Catastrophic bloom events (10× growth, devastates biomes in hours)
- Psionic effects (hallucinations, madness, psychic damage)
- Ecosystem collapse (unchecked growth = extinction)

**Containment**: Sonic fences (60% reduction), stasis fields (100%), nanite swarms (active removal)

**Excision**: Manual harvest (safe, slow), explosives (fast, dangerous), controlled burn (prevents spread, 0% recovery)

**Strategic Dilemma**: Exploit for massive energy advantage vs protect environment from corruption

**Key Insight**: Crystals are a double-edged sword - incredible power at terrible cost. Early aggressive harvesting enables rapid tech progression but risks catastrophic bloom events and environmental collapse. Conservative players eradicate immediately and forgo energy advantage. Skilled players manage extraction/containment balance, harvesting near maximum sustainable rate without triggering crisis.
