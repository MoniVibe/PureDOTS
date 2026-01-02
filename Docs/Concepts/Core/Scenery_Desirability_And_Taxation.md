# Scenery, Desirability, and Taxation Systems

**Status:** Concept Design
**Category:** Core / Social / Economic
**Complexity:** High
**Applies To:** Godgame, Space4X, shared PureDOTS
**Created:** 2025-01-21
**Last Updated:** 2025-01-21

---

## Overview

**Core Concept:** Entities prefer to live in high-desirability areas based on natural scenery, luxury availability, and social status. Aggregate entities (villages, factions, empires, guilds) construct housing and tax their populations according to their behavior profiles, creating emergent social stratification and economic dynamics.

**Key Systems:**
1. **Scenery Stats:** Flora/fauna density determines natural beauty of locations
2. **Desirability Calculation:** Combines naturalness, luxury goods, height, line of sight, and social factors
3. **Housing Construction:** Behavior-driven decisions on segregated vs. egalitarian neighborhoods
4. **Taxation System:** Profile-driven tax policies that reflect alignment, outlook, and behavior values

---

## Scenery System

### Core Concept

**Scenery** represents the natural beauty and ecological richness of a location. It is derived from the presence and health of native flora and fauna.

### Scenery Calculation

```csharp
/// <summary>
/// Scenery value for a location (0-100, where 100 = pristine wilderness).
/// </summary>
public struct SceneryValue : IComponentData
{
    public float BaseScenery;           // 0-100 (flora/fauna density)
    public float IndustrializationPenalty; // 0-1 (how much industry has reduced scenery)
    public float CurrentScenery;         // BaseScenery * (1 - IndustrializationPenalty)
    public uint LastCalculatedTick;
}

/// <summary>
/// System-level scenery (aggregate of all planets/regions).
/// </summary>
public struct SystemScenery : IComponentData
{
    public float TotalScenery;          // Weighted average of child regions
    public float FloraDensity;          // 0-1 (native plant coverage)
    public float FaunaDensity;          // 0-1 (native animal population)
    public float IndustrializationLevel; // 0-1 (how much industry has replaced nature)
    public uint LastCalculatedTick;
}
```

### Flora and Fauna Tracking

```csharp
/// <summary>
/// Tracks native flora in a region.
/// </summary>
public struct FloraPopulation : IComponentData
{
    public int SpeciesCount;            // Number of unique species
    public float TotalBiomass;           // Total plant mass
    public float CoverageRatio;          // 0-1 (how much of region is covered)
    public float Health;                 // 0-1 (disease, pollution, overharvesting)
    public DynamicBuffer<FloraSpecies> Species;
}

/// <summary>
/// Tracks native fauna in a region.
/// </summary>
public struct FaunaPopulation : IComponentData
{
    public int SpeciesCount;            // Number of unique species
    public float TotalBiomass;           // Total animal mass
    public float PopulationDensity;     // 0-1 (animals per unit area)
    public float Health;                 // 0-1 (disease, hunting, habitat loss)
    public DynamicBuffer<FaunaSpecies> Species;
}

/// <summary>
/// Flora species entry.
/// </summary>
public struct FloraSpecies : IBufferElementData
{
    public FixedString32Bytes SpeciesName;
    public float Biomass;                // Mass of this species
    public float SceneryContribution;    // How much this species adds to scenery (0-1)
    public float Health;                 // 0-1
}

/// <summary>
/// Fauna species entry.
/// </summary>
public struct FaunaSpecies : IBufferElementData
{
    public FixedString32Bytes SpeciesName;
    public float Population;            // Number of individuals
    public float SceneryContribution;    // How much this species adds to scenery (0-1)
    public float Health;                 // 0-1
}
```

### Industrialization Impact

```csharp
/// <summary>
/// Industrial activity that reduces scenery.
/// </summary>
public struct IndustrialActivity : IComponentData
{
    public float PollutionLevel;         // 0-1 (air/water/soil contamination)
    public float Deforestation;          // 0-1 (trees cleared for industry)
    public float HabitatDestruction;     // 0-1 (wildlife displaced/killed)
    public float NoiseLevel;             // 0-1 (industrial noise pollution)
    public float SceneryPenalty;         // Combined penalty (0-1)
}

/// <summary>
/// Calculates scenery reduction from industrialization.
/// </summary>
public static float CalculateIndustrializationPenalty(IndustrialActivity activity)
{
    // Weighted combination of all factors
    float penalty = 
        activity.PollutionLevel * 0.3f +
        activity.Deforestation * 0.25f +
        activity.HabitatDestruction * 0.3f +
        activity.NoiseLevel * 0.15f;
    
    return math.clamp(penalty, 0f, 1f);
}
```

### Scenery Decay and Recovery

**Decay:**
- Industrialization immediately reduces scenery
- Overharvesting (logging, hunting) gradually reduces flora/fauna
- Pollution kills species over time
- Habitat destruction permanently reduces capacity

**Recovery:**
- If industrialization stops, flora/fauna can regrow (slow, 1-5% per year)
- Reforestation projects can accelerate recovery
- Wildlife reintroduction programs restore fauna
- Pollution cleanup allows natural recovery

**Example:**
- **Pristine Forest:** FloraDensity = 0.95, FaunaDensity = 0.80 → Scenery = 87.5
- **Industrialized City:** FloraDensity = 0.10, FaunaDensity = 0.05, IndustrializationPenalty = 0.85 → Scenery = 2.25
- **Recovering Region:** FloraDensity = 0.40, FaunaDensity = 0.30, IndustrializationPenalty = 0.40 → Scenery = 42.0

---

## Desirability System

### Core Concept

**Desirability** represents how attractive a location is for living. Entities prefer high-desirability areas, leading to natural migration patterns and housing market dynamics.

### Desirability Calculation

```csharp
/// <summary>
/// Desirability value for a location (0-100, where 100 = most desirable).
/// </summary>
public struct DesirabilityValue : IComponentData
{
    public float BaseDesirability;      // 0-100 (calculated from factors)
    public float NaturalnessFactor;      // 0-1 (how natural vs. developed)
    public float LuxuryAvailability;     // 0-1 (luxury goods/services nearby)
    public float HeightBonus;           // 0-1 (elevation preference)
    public float ViewQuality;           // 0-1 (line of sight, scenic views)
    public float SocialStatus;          // 0-1 (prestige of living here)
    public float Accessibility;         // 0-1 (ease of reaching amenities)
    public uint LastCalculatedTick;
}

/// <summary>
/// Calculates desirability from multiple factors.
/// </summary>
public static float CalculateDesirability(
    float scenery,
    float naturalness,
    float luxuryAvailability,
    float height,
    float viewQuality,
    float socialStatus,
    float accessibility)
{
    // Base desirability from scenery (40% weight)
    float base = scenery * 0.4f;
    
    // Naturalness bonus (20% weight) - people prefer natural areas
    float naturalBonus = naturalness * 20f;
    
    // Luxury offset (15% weight) - luxury goods can offset low naturalness
    float luxuryOffset = luxuryAvailability * 15f;
    
    // Height bonus (10% weight) - elevation provides views and status
    float heightBonus = height * 10f;
    
    // View quality (10% weight) - scenic views increase desirability
    float viewBonus = viewQuality * 10f;
    
    // Social status (3% weight) - prestige of neighborhood
    float statusBonus = socialStatus * 3f;
    
    // Accessibility (2% weight) - ease of reaching amenities
    float accessBonus = accessibility * 2f;
    
    // Combine all factors
    float desirability = base + naturalBonus + luxuryOffset + heightBonus + viewBonus + statusBonus + accessBonus;
    
    return math.clamp(desirability, 0f, 100f);
}
```

### Naturalness Factor

**Naturalness** measures how untouched by development an area is:

```csharp
/// <summary>
/// Naturalness calculation (0-1, where 1 = pristine wilderness).
/// </summary>
public static float CalculateNaturalness(
    float scenery,
    float buildingDensity,
    float populationDensity,
    float roadDensity)
{
    // Base naturalness from scenery
    float baseNaturalness = scenery / 100f;
    
    // Penalties for development
    float developmentPenalty = 
        buildingDensity * 0.3f +
        populationDensity * 0.3f +
        roadDensity * 0.2f;
    
    float naturalness = baseNaturalness * (1f - developmentPenalty);
    
    return math.clamp(naturalness, 0f, 1f);
}
```

### Luxury Availability

**Luxury Availability** measures proximity to luxury goods and services:

```csharp
/// <summary>
/// Luxury goods/services within proximity.
/// </summary>
public struct LuxuryAvailability : IComponentData
{
    public float ProximityScore;        // 0-1 (how close luxury amenities are)
    public float VarietyScore;          // 0-1 (diversity of luxury options)
    public float QualityScore;          // 0-1 (quality of luxury goods)
    public float PriceAccessibility;     // 0-1 (how affordable luxury is)
    public float CombinedScore;          // Weighted average
}

/// <summary>
/// Luxury goods that offset low naturalness.
/// </summary>
public enum LuxuryType : byte
{
    FineDining = 0,
    ArtisanGoods = 1,
    Entertainment = 2,
    Spas = 3,
    CulturalVenues = 4,
    ExclusiveServices = 5,
    HighQualityHousing = 6,
    Transportation = 7
}
```

**Luxury Offset Formula:**
- High naturalness (0.8+) → Luxury has minimal impact (already desirable)
- Medium naturalness (0.4-0.8) → Luxury can offset up to 30% of naturalness penalty
- Low naturalness (<0.4) → Luxury can offset up to 50% of naturalness penalty, but area remains less desirable than natural areas

### Height and View Quality

**Height Bonus:**
- Elevation provides better views, status, and air quality
- Higher locations have lower population density (more exclusive)
- Formula: `HeightBonus = math.clamp((elevation - seaLevel) / maxElevation, 0f, 1f)`

**View Quality:**
- Line of sight to scenic features (mountains, oceans, forests)
- Unobstructed views (no buildings blocking)
- Formula: `ViewQuality = (scenicFeaturesVisible * 0.6f) + (unobstructedRatio * 0.4f)`

### Social Status Factor

**Social Status** represents the prestige of living in a location:

```csharp
/// <summary>
/// Social status of a location (0-1, where 1 = most prestigious).
/// </summary>
public struct SocialStatusValue : IComponentData
{
    public float AverageWealth;         // 0-1 (average wealth of residents)
    public float ElitePresence;          // 0-1 (how many elites live here)
    public float SegregationLevel;       // 0-1 (how segregated from lower classes)
    public float GuardPresence;          // 0-1 (security/guards protecting area)
    public float CombinedStatus;        // Weighted average
}
```

**Status Calculation:**
- High-wealth residents → Higher status
- Elite presence (nobles, rulers, guild masters) → Higher status
- Segregated neighborhoods (guarded, exclusive) → Higher status
- Guard presence (security, peacekeepers) → Higher status

---

## Housing Construction and Segregation

### Core Concept

**Aggregate entities** (villages, factions, empires, guilds) construct housing based on their behavior profiles. Leadership, elites, and rulers make decisions about:
- **Segregated neighborhoods** for higher-income individuals (authoritarian, materialistic)
- **Egalitarian housing** for poorer parts of society (egalitarian outlook)
- **Guarded communities** for elites (authoritarian, high wealth)

### Behavior Profile Influence

```csharp
/// <summary>
/// Housing construction decision based on behavior profile.
/// </summary>
public struct HousingConstructionPolicy : IComponentData
{
    public float SegregationPreference;  // 0-1 (how much to segregate by wealth)
    public float EliteHousingPriority;   // 0-1 (priority for elite housing)
    public float PoorHousingPriority;     // 0-1 (priority for poor housing)
    public float GuardPresenceLevel;     // 0-1 (how much to guard neighborhoods)
    public float IntegrationLevel;        // 0-1 (how much to integrate classes)
}

/// <summary>
/// Calculates housing policy from aggregate profile.
/// </summary>
public static HousingConstructionPolicy CalculateHousingPolicy(
    EntityAlignment alignment,
    EntityOutlook outlook,
    PersonalityAxes behavior)
{
    var policy = new HousingConstructionPolicy();
    
    // Egalitarian outlook → Low segregation, high poor housing priority
    if (outlook.Primary == OutlookType.Egalitarian || 
        outlook.Secondary == OutlookType.Egalitarian)
    {
        policy.SegregationPreference = 0.2f;  // Low segregation
        policy.PoorHousingPriority = 0.8f;    // High priority for poor
        policy.EliteHousingPriority = 0.3f;   // Low priority for elites
        policy.IntegrationLevel = 0.9f;        // High integration
    }
    
    // Authoritarian outlook → High segregation, high elite housing priority
    if (outlook.Primary == OutlookType.Authoritarian || 
        outlook.Secondary == OutlookType.Authoritarian)
    {
        policy.SegregationPreference = 0.8f;  // High segregation
        policy.EliteHousingPriority = 0.9f;   // High priority for elites
        policy.PoorHousingPriority = 0.2f;     // Low priority for poor
        policy.GuardPresenceLevel = 0.7f;      // Guards protect elites
        policy.IntegrationLevel = 0.2f;        // Low integration
    }
    
    // Materialistic outlook → Luxury housing, status-based segregation
    if (outlook.Primary == OutlookType.Materialistic || 
        outlook.Secondary == OutlookType.Materialistic)
    {
        policy.SegregationPreference = 0.7f;  // High segregation by wealth
        policy.EliteHousingPriority = 0.8f;    // High priority for wealthy
        policy.GuardPresenceLevel = 0.6f;      // Guards protect wealth
    }
    
    // Pure Authoritarian (Good or Evil) → Care for underlings, but maintain hierarchy
    if (alignment.Purity > 50f && 
        (outlook.Primary == OutlookType.Authoritarian || 
         outlook.Secondary == OutlookType.Authoritarian))
    {
        // Pure authoritarians care for their subjects
        policy.PoorHousingPriority = 0.6f;    // Moderate priority for poor
        policy.SegregationPreference = 0.5f;  // Moderate segregation (hierarchy but care)
        policy.GuardPresenceLevel = 0.4f;      // Guards protect all, not just elites
    }
    
    // Corrupt Authoritarian (Good or Evil) → Exploit lower strata
    if (alignment.Purity < -50f && 
        (outlook.Primary == OutlookType.Authoritarian || 
         outlook.Secondary == OutlookType.Authoritarian))
    {
        // Corrupt authoritarians exploit subjects
        policy.PoorHousingPriority = 0.1f;    // Very low priority for poor
        policy.SegregationPreference = 0.9f;  // Extreme segregation
        policy.GuardPresenceLevel = 0.8f;      // Guards suppress lower classes
        policy.IntegrationLevel = 0.1f;        // Minimal integration
    }
    
    return policy;
}
```

### Housing Construction Decisions

```csharp
/// <summary>
/// Housing construction request from aggregate leadership.
/// </summary>
public struct HousingConstructionRequest : IComponentData
{
    public Entity Aggregate;            // Village/faction/empire
    public Entity Leader;               // Who made the decision
    public HousingType Type;            // Elite, Poor, Mixed, Segregated
    public float3 Location;              // Where to build
    public float Desirability;          // Target desirability level
    public int Capacity;                // How many entities can live here
    public float Budget;                 // Available funds
    public uint RequestTick;
}

public enum HousingType : byte
{
    EliteSegregated = 0,    // Guarded, high-desirability, exclusive
    PoorIntegrated = 1,     // Low-cost, accessible, mixed with other classes
    MixedClass = 2,          // All classes together
    GuardedCommunity = 3,    // High security, elite-only
    PublicHousing = 4,       // Government-funded, for poor
    LuxuryDistrict = 5      // High-end, materialistic focus
}
```

### Segregated Neighborhood Construction

**Authoritarian/Materialistic Aggregates:**
1. **Identify High-Desirability Areas:** Scenic locations, elevated terrain, good views
2. **Construct Guarded Communities:** Walls, gates, peacekeeper presence
3. **Reserve for Elites:** Only high-wealth individuals allowed
4. **Luxury Amenities:** Fine dining, artisan goods, exclusive services
5. **Tax Benefits:** Lower taxes for elite residents (or tax breaks)

**Example:**
- **Lawful Evil Authoritarian Materialistic Village:**
  - Constructs walled elite district on hilltop (HeightBonus = 0.8, ViewQuality = 0.9)
  - Guards patrol perimeter (GuardPresence = 0.8)
  - Only entities with wealth > 1000 gold allowed
  - Luxury shops, fine dining, cultural venues nearby
  - Desirability = 85 (high naturalness + luxury + height + status)

### Egalitarian Housing Construction

**Egalitarian Aggregates:**
1. **Prioritize Poor Housing:** Construct affordable housing in accessible areas
2. **Mixed-Class Neighborhoods:** Integrate all wealth levels
3. **Public Services:** Schools, clinics, markets accessible to all
4. **No Segregation:** No guarded communities, no exclusive districts
5. **Redistributive Policies:** Tax rich to fund poor housing

**Example:**
- **Lawful Good Egalitarian Peaceful Village:**
  - Constructs public housing in central location (Accessibility = 0.9)
  - Mixed-class neighborhoods (IntegrationLevel = 0.9)
  - Public services (schools, clinics) accessible to all
  - Desirability = 60 (moderate naturalness, good accessibility, no luxury offset)

---

## Taxation System

### Core Concept

**Taxation** is how aggregate entities extract resources from their populations. Tax policies reflect the aggregate's alignment, outlook, and behavior profiles, creating distinct economic systems.

### Tax Structure

```csharp
/// <summary>
/// Tax policy for an aggregate entity.
/// </summary>
public struct TaxPolicy : IComponentData
{
    public float BaseTaxRate;           // 0-1 (percentage of income/wealth)
    public float WealthTaxRate;          // 0-1 (tax on accumulated wealth)
    public float IncomeTaxRate;          // 0-1 (tax on income/earnings)
    public float DesirabilityTax;        // 0-1 (tax based on location desirability)
    public float LuxuryTax;              // 0-1 (tax on luxury goods)
    public TaxBracket[] Brackets;        // Progressive tax brackets
    public float EnforcementLevel;       // 0-1 (how strictly taxes are collected)
    public float UseOfForce;             // 0-1 (peacekeeper violence for collection)
}

/// <summary>
/// Tax bracket for progressive taxation.
/// </summary>
public struct TaxBracket : IBufferElementData
{
    public float MinWealth;             // Minimum wealth for this bracket
    public float MaxWealth;             // Maximum wealth (or infinity)
    public float TaxRate;                // Tax rate for this bracket
}
```

### Behavior Profile Influence on Taxation

```csharp
/// <summary>
/// Calculates tax policy from aggregate profile.
/// </summary>
public static TaxPolicy CalculateTaxPolicy(
    EntityAlignment alignment,
    EntityOutlook outlook,
    PersonalityAxes behavior)
{
    var policy = new TaxPolicy();
    
    // Egalitarian → Tax the rich, progressive taxation
    if (outlook.Primary == OutlookType.Egalitarian || 
        outlook.Secondary == OutlookType.Egalitarian)
    {
        policy.BaseTaxRate = 0.15f;      // Moderate base rate
        policy.WealthTaxRate = 0.3f;     // High wealth tax (redistribute)
        policy.IncomeTaxRate = 0.2f;     // Moderate income tax
        
        // Progressive brackets: rich pay more
        policy.Brackets = new TaxBracket[]
        {
            new TaxBracket { MinWealth = 0f, MaxWealth = 500f, TaxRate = 0.05f },      // Poor: 5%
            new TaxBracket { MinWealth = 500f, MaxWealth = 2000f, TaxRate = 0.15f },   // Middle: 15%
            new TaxBracket { MinWealth = 2000f, MaxWealth = 10000f, TaxRate = 0.35f }, // Rich: 35%
            new TaxBracket { MinWealth = 10000f, MaxWealth = float.MaxValue, TaxRate = 0.50f } // Elite: 50%
        };
        
        policy.EnforcementLevel = 0.6f;   // Moderate enforcement
        policy.UseOfForce = 0.2f;         // Low use of force (egalitarian values)
    }
    
    // Authoritarian → Tax all, but favor elites
    if (outlook.Primary == OutlookType.Authoritarian || 
        outlook.Secondary == OutlookType.Authoritarian)
    {
        policy.BaseTaxRate = 0.25f;      // Higher base rate
        policy.WealthTaxRate = 0.15f;    // Lower wealth tax (protect elites)
        policy.IncomeTaxRate = 0.3f;      // Higher income tax
        
        // Regressive brackets: poor pay more, rich pay less
        policy.Brackets = new TaxBracket[]
        {
            new TaxBracket { MinWealth = 0f, MaxWealth = 500f, TaxRate = 0.30f },      // Poor: 30%
            new TaxBracket { MinWealth = 500f, MaxWealth = 2000f, TaxRate = 0.25f },   // Middle: 25%
            new TaxBracket { MinWealth = 2000f, MaxWealth = 10000f, TaxRate = 0.20f }, // Rich: 20%
            new TaxBracket { MinWealth = 10000f, MaxWealth = float.MaxValue, TaxRate = 0.15f } // Elite: 15%
        };
        
        policy.EnforcementLevel = 0.9f;   // High enforcement
        policy.UseOfForce = 0.7f;         // High use of force (authoritarian control)
    }
    
    // Pure Authoritarian (Good or Evil) → Care for subjects, but maintain hierarchy
    if (alignment.Purity > 50f && 
        (outlook.Primary == OutlookType.Authoritarian || 
         outlook.Secondary == OutlookType.Authoritarian))
    {
        // Pure authoritarians care for their subjects
        policy.BaseTaxRate = 0.20f;      // Moderate base rate
        policy.WealthTaxRate = 0.20f;    // Moderate wealth tax
        policy.IncomeTaxRate = 0.25f;    // Moderate income tax
        
        // Balanced brackets: all pay fairly, but hierarchy maintained
        policy.Brackets = new TaxBracket[]
        {
            new TaxBracket { MinWealth = 0f, MaxWealth = 500f, TaxRate = 0.15f },      // Poor: 15%
            new TaxBracket { MinWealth = 500f, MaxWealth = 2000f, TaxRate = 0.20f },   // Middle: 20%
            new TaxBracket { MinWealth = 2000f, MaxWealth = 10000f, TaxRate = 0.25f }, // Rich: 25%
            new TaxBracket { MinWealth = 10000f, MaxWealth = float.MaxValue, TaxRate = 0.30f } // Elite: 30%
        };
        
        policy.EnforcementLevel = 0.7f;   // Moderate enforcement
        policy.UseOfForce = 0.3f;         // Low use of force (care for subjects)
    }
    
    // Corrupt Authoritarian (Good or Evil) → Exploit lower strata
    if (alignment.Purity < -50f && 
        (outlook.Primary == OutlookType.Authoritarian || 
         outlook.Secondary == OutlookType.Authoritarian))
    {
        // Corrupt authoritarians exploit subjects
        policy.BaseTaxRate = 0.35f;      // Very high base rate
        policy.WealthTaxRate = 0.10f;    // Very low wealth tax (protect elites)
        policy.IncomeTaxRate = 0.40f;     // Very high income tax
        
        // Extreme regressive brackets: poor pay most, rich pay least
        policy.Brackets = new TaxBracket[]
        {
            new TaxBracket { MinWealth = 0f, MaxWealth = 500f, TaxRate = 0.45f },      // Poor: 45%
            new TaxBracket { MinWealth = 500f, MaxWealth = 2000f, TaxRate = 0.35f },   // Middle: 35%
            new TaxBracket { MinWealth = 2000f, MaxWealth = 10000f, TaxRate = 0.25f }, // Rich: 25%
            new TaxBracket { MinWealth = 10000f, MaxWealth = float.MaxValue, TaxRate = 0.10f } // Elite: 10%
        };
        
        policy.EnforcementLevel = 1.0f;   // Maximum enforcement
        policy.UseOfForce = 0.9f;         // Maximum use of force (oppression)
    }
    
    // Materialistic → Tax luxury goods, protect wealth
    if (outlook.Primary == OutlookType.Materialistic || 
        outlook.Secondary == OutlookType.Materialistic)
    {
        policy.LuxuryTax = 0.25f;         // High luxury tax
        policy.WealthTaxRate = 0.10f;     // Low wealth tax (protect accumulated wealth)
        policy.DesirabilityTax = 0.15f;    // Tax high-desirability locations
    }
    
    // Spiritual → Lower taxes, focus on faith contributions
    if (outlook.Primary == OutlookType.Spiritual || 
        outlook.Secondary == OutlookType.Spiritual)
    {
        policy.BaseTaxRate = 0.10f;       // Lower base rate
        policy.WealthTaxRate = 0.15f;     // Moderate wealth tax
        // Faith contributions replace some taxes
    }
    
    return policy;
}
```

### Desirability-Based Taxation

**Luxury Location Tax:**
- High-desirability areas can be taxed more (premium location tax)
- Formula: `DesirabilityTax = (desirability / 100f) * baseDesirabilityTaxRate`
- Example: Desirability = 85 → Tax multiplier = 0.85

**Wealth Concentration Tax:**
- Areas with high average wealth can be taxed more
- Formula: `WealthConcentrationTax = (averageWealth / maxWealth) * baseWealthTaxRate`
- Example: Average wealth = 5000, max = 10000 → Tax multiplier = 0.5

**Segregation Tax:**
- Segregated neighborhoods may pay different rates
- Elite districts: Lower taxes (reward for status)
- Poor districts: Higher taxes (exploitation) OR lower taxes (redistribution)

### Peacekeeper Jurisdiction and Use of Force

**Egalitarian Aggregates:**
- **Limited Peacekeeper Jurisdiction:** Peacekeepers only enforce laws, not tax collection
- **Low Use of Force:** Minimal violence, focus on de-escalation
- **Community Policing:** Peacekeepers serve community, not elites

**Authoritarian Aggregates:**
- **Expanded Peacekeeper Jurisdiction:** Peacekeepers enforce taxes, suppress dissent
- **High Use of Force:** Violence for tax collection, maintaining order
- **Elite Protection:** Peacekeepers protect elites, suppress lower classes

**Pure Authoritarians:**
- **Moderate Jurisdiction:** Peacekeepers enforce laws and taxes, but with restraint
- **Moderate Use of Force:** Force used when necessary, but not excessive
- **Protective Role:** Peacekeepers protect all subjects, maintain hierarchy

**Corrupt Authoritarians:**
- **Maximum Jurisdiction:** Peacekeepers are tax collectors, enforcers, oppressors
- **Maximum Use of Force:** Violence is primary tool for control
- **Elite Enforcers:** Peacekeepers serve elites, oppress lower classes

### Tax Collection and Enforcement

```csharp
/// <summary>
/// Tax collection event.
/// </summary>
public struct TaxCollectionEvent : IComponentData
{
    public Entity Taxpayer;              // Who is being taxed
    public Entity TaxCollector;          // Aggregate collecting tax
    public float TaxAmount;               // Amount collected
    public float EnforcementLevel;        // How strictly enforced
    public bool UsedForce;                // Whether force was used
    public uint CollectionTick;
}

/// <summary>
/// Tax evasion attempt.
/// </summary
public struct TaxEvasionEvent : IComponentData
{
    public Entity Taxpayer;              // Who evaded tax
    public Entity TaxCollector;          // Aggregate trying to collect
    public float EvadedAmount;           // Amount evaded
    public bool Caught;                  // Whether evasion was detected
    public uint EvasionTick;
}
```

**Enforcement Mechanics:**
- **High Enforcement:** Tax collectors actively pursue evaders, audits, penalties
- **Low Enforcement:** Lax collection, easy to evade, minimal penalties
- **Use of Force:** Peacekeepers can use violence to collect taxes (authoritarian)
- **Community Pressure:** Social pressure to pay taxes (egalitarian)

---

## Integration and Emergent Dynamics

### Migration Patterns

**High-Desirability Areas:**
- Entities migrate toward high-desirability locations
- Creates population pressure and competition
- Drives up housing costs and taxes
- Leads to natural segregation (wealthy can afford desirable areas)

**Low-Desirability Areas:**
- Entities avoid low-desirability locations
- Creates population decline and abandonment
- Lowers housing costs and taxes
- Can become slums or industrial zones

### Social Stratification

**Segregated Neighborhoods:**
- Elites live in high-desirability, guarded communities
- Poor live in low-desirability, accessible areas
- Middle class in moderate-desirability, mixed areas
- Creates visible social hierarchy

**Egalitarian Integration:**
- All classes live in mixed neighborhoods
- Public services accessible to all
- Redistributive policies reduce wealth gaps
- Creates more homogeneous society

### Economic Dynamics

**Tax Revenue:**
- High taxes → More revenue, but lower population satisfaction
- Low taxes → Less revenue, but higher population satisfaction
- Progressive taxes → Redistribute wealth, reduce inequality
- Regressive taxes → Concentrate wealth, increase inequality

**Housing Market:**
- High desirability → High demand → High prices → Elite-only
- Low desirability → Low demand → Low prices → Poor-only
- Luxury amenities → Offset low naturalness → Attract wealthy
- Public housing → Redistribute access to desirable areas

### Political Dynamics

**Egalitarian Policies:**
- Tax rich to fund poor housing
- Limit peacekeeper violence
- Integrate neighborhoods
- Redistribute wealth

**Authoritarian Policies:**
- Tax poor to fund elite housing
- Expand peacekeeper jurisdiction
- Segregate neighborhoods
- Concentrate wealth

**Pure Authoritarian Policies:**
- Balanced taxation (all pay fairly)
- Moderate peacekeeper use
- Hierarchical but caring structure
- Maintain order while protecting subjects

**Corrupt Authoritarian Policies:**
- Extreme regressive taxation
- Maximum peacekeeper violence
- Extreme segregation
- Oppression and exploitation

---

## Cross-Game Applications

### Godgame (Villagers)

**Scenery:**
- Flora/fauna density in regions
- Industrialization from villages (logging, mining, farming)
- Natural beauty affects villager happiness and migration

**Desirability:**
- Villagers prefer scenic locations
- Luxury goods (fine dining, artisan crafts) offset low naturalness
- Height and views matter (hilltop villages more desirable)

**Housing:**
- Villages construct housing based on leader's outlook
- Egalitarian villages build public housing
- Authoritarian villages build elite districts

**Taxation:**
- Villages tax villagers based on leader's profile
- Egalitarian leaders tax rich, fund poor
- Authoritarian leaders tax poor, protect elites

### Space4X (Colonists)

**Scenery:**
- Flora/fauna on planets
- Industrialization from colonies (mining, manufacturing)
- Terraforming can restore or destroy scenery

**Desirability:**
- Colonists prefer planets with high scenery
- Luxury amenities (entertainment, cultural venues) offset low naturalness
- Orbital stations and space habitats have different desirability factors

**Housing:**
- Colonies construct housing based on faction profile
- Egalitarian factions build integrated districts
- Authoritarian factions build elite sectors

**Taxation:**
- Factions tax colonists based on profile
- Egalitarian factions use progressive taxation
- Authoritarian factions use regressive taxation

---

## Example Scenarios

### Scenario 1: Egalitarian Village

**Profile:** Lawful Good Egalitarian Peaceful
- **Housing:** Constructs public housing in accessible areas, mixed-class neighborhoods
- **Taxation:** Progressive (rich pay 50%, poor pay 5%), low use of force
- **Desirability:** Moderate (good accessibility, moderate naturalness, no luxury offset)
- **Result:** Integrated society, reduced inequality, moderate desirability for all

### Scenario 2: Authoritarian Materialistic Empire

**Profile:** Lawful Evil Authoritarian Materialistic
- **Housing:** Constructs guarded elite districts on hilltops, poor in low-desirability areas
- **Taxation:** Regressive (poor pay 45%, elite pay 10%), high use of force
- **Desirability:** Elite areas = 85 (high naturalness + luxury + height), Poor areas = 20 (low naturalness, no luxury)
- **Result:** Extreme segregation, high inequality, elite live in luxury, poor in slums

### Scenario 3: Pure Authoritarian Kingdom

**Profile:** Lawful Good Pure Authoritarian
- **Housing:** Constructs hierarchical but caring structure, moderate segregation
- **Taxation:** Balanced progressive (all pay 15-30% based on wealth), moderate use of force
- **Desirability:** Moderate-high for all (good naturalness, accessible, some luxury)
- **Result:** Maintained hierarchy but caring leadership, moderate inequality, all subjects protected

### Scenario 4: Corrupt Authoritarian Regime

**Profile:** Chaotic Evil Corrupt Authoritarian
- **Housing:** Constructs extreme segregation, elite in guarded communities, poor in slums
- **Taxation:** Extreme regressive (poor pay 50%, elite pay 5%), maximum use of force
- **Desirability:** Elite = 90 (pristine naturalness + luxury), Poor = 10 (industrial wasteland)
- **Result:** Extreme oppression, maximum inequality, elite exploit poor, peacekeepers oppress

---

## Summary

The **Scenery, Desirability, and Taxation** systems create emergent social and economic dynamics:

1. **Scenery:** Flora/fauna density determines natural beauty, reduced by industrialization
2. **Desirability:** Combines naturalness, luxury, height, views, and social status
3. **Housing:** Behavior-driven construction of segregated vs. egalitarian neighborhoods
4. **Taxation:** Profile-driven tax policies reflecting alignment, outlook, and behavior
5. **Integration:** All systems interact to create migration patterns, social stratification, and economic dynamics

**Key Principles:**
- **Egalitarians:** Tax rich, fund poor, integrate neighborhoods, limit violence
- **Authoritarians:** Tax poor, protect elites, segregate neighborhoods, expand violence
- **Pure Authoritarians:** Balanced approach, care for subjects, maintain hierarchy
- **Corrupt Authoritarians:** Extreme exploitation, maximum oppression, extreme inequality

**Cross-Game:** Same systems apply to both Godgame (villagers) and Space4X (colonists) with contextual differences.

---

**Related Documentation:**
- `Archetypical_Theme_Mapping.md` - Alignment, outlook, and behavior profiles
- `Entity_Relations_And_Interactions.md` - Social dynamics and relations
- `Village_Spatial_Growth.md` - Village construction and growth
- `Aggregate_Decision_Making.md` - How aggregates make decisions

---

**Last Updated:** 2025-01-21
**Status:** Concept Design - Ready for implementation
