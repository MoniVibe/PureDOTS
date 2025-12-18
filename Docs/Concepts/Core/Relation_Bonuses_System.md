# Relation Bonuses System - Strategic Diplomacy

**Status:** Design Document
**Category:** Core Social Dynamics - Strategic Depth
**Scope:** PureDOTS Foundation Layer
**Created:** 2025-12-18

---

## Purpose

Defines how **relations provide different bonuses** based on alignment/outlook combinations. The value of a relationship depends on WHO you have it with and what THEY offer based on their personality and capabilities.

**Core Concept:** A peaceful village allied with warlike neighbors gets mercenary discounts. A warlike village hated by xenophobes gains favor with other cultures. Intelligent colonies gain production bonuses when threatened by hostile neighbors. **Strategic relationship management** - the RIGHT relations with the RIGHT entities matter more than just high relations with everyone.

**Key Principle:** Bonuses are **computed from alignment/outlook/stats combinations**, creating emergent strategic depth where diplomacy becomes a resource optimization puzzle.

---

## Architecture Overview

```
RELATION + ALIGNMENT COMBO → CONTEXTUAL BONUS

Entity A (Peaceful, Materialist) + Entity B (Warlike, Spiritualist)
├─ High Relation (+70)
├─ Alignment Analysis:
│  ├─ A is Peaceful → Values B's military protection
│  ├─ A is Materialist → Doesn't care about B's spiritualism
│  └─ B is Warlike → Values A's resources for war
├─ Computed Bonuses:
│  ├─ A gets: +20% mercenary discount from B
│  ├─ A gets: +10% military defense from B
│  └─ B gets: +15% resource discount from A
└─ Result: Mutually beneficial despite different values
```

---

## Component Architecture

### 1. Relation Bonus Components

```csharp
/// <summary>
/// Active bonuses derived from a specific relation
/// </summary>
public struct RelationBonus : IBufferElementData
{
    /// <summary>
    /// Entity this bonus comes from (relation target)
    /// </summary>
    public Entity SourceEntity;

    /// <summary>
    /// Type of bonus granted
    /// </summary>
    public RelationBonusTypeId BonusType;

    /// <summary>
    /// Bonus magnitude (0-1 normalized)
    /// </summary>
    public float Magnitude;

    /// <summary>
    /// Conditions that must be met for bonus to apply
    /// </summary>
    public RelationBonusConditions Conditions;
}

public enum RelationBonusTypeId : ushort
{
    // Economic
    TradePriceDiscount,
    ResourceProductionBonus,
    ResourceGatheringBonus,
    TaxRevenueBonus,

    // Military
    MercenaryDiscount,
    MilitaryDefenseBonus,
    MilitaryProductionBonus,
    CombatEffectivenessBonus,
    RecruitmentBonus,

    // Diplomatic
    DiplomaticInfluenceBonus,
    ReputationGainBonus,
    AllianceStrengthBonus,
    NegotiationBonus,

    // Cultural
    CulturalSpreadBonus,
    IdeologicalInfluenceBonus,
    ConversionResistanceBonus,

    // Research/Tech (Space4X)
    TechSharingBonus,
    ResearchSpeedBonus,
    InnovationBonus,

    // Intelligence
    IntelligenceGatheringBonus,
    CounterIntelligenceBonus,
    EspionageEffectivenessBonus,

    // Special
    UniqueAbilityAccess,
    SharedVisionBonus,
    JointMilitaryOperations
}

[Flags]
public enum RelationBonusConditions : uint
{
    None = 0,
    RequiresAlliance = 1 << 0,
    RequiresSharedEnemy = 1 << 1,
    RequiresTradeRoute = 1 << 2,
    RequiresProximity = 1 << 3,
    RequiresWar = 1 << 4,
    RequiresPeace = 1 << 5,
    RequiresMutualBenefit = 1 << 6
}

/// <summary>
/// Aggregate stats that influence bonus calculations
/// </summary>
public struct AggregateStats : IComponentData
{
    /// <summary>
    /// Average intelligence of population
    /// </summary>
    public float IntelligenceAverage;

    /// <summary>
    /// Military capability rating
    /// </summary>
    public float MilitaryStrength;

    /// <summary>
    /// Economic output
    /// </summary>
    public float EconomicPower;

    /// <summary>
    /// Cultural influence
    /// </summary>
    public float CulturalInfluence;

    /// <summary>
    /// Technology level (Space4X)
    /// </summary>
    public float TechLevel;

    /// <summary>
    /// Population count
    /// </summary>
    public int PopulationCount;
}

/// <summary>
/// Outlook/ideology component (extends alignment)
/// </summary>
public struct EntityOutlook : IComponentData
{
    /// <summary>
    /// Economic outlook
    /// </summary>
    public OutlookAxis Economic;  // Materialist <-> Spiritualist

    /// <summary>
    /// Expansion outlook
    /// </summary>
    public OutlookAxis Expansion;  // Isolationist <-> Expansionist

    /// <summary>
    /// Tolerance outlook
    /// </summary>
    public OutlookAxis Tolerance;  // Xenophobic <-> Xenophilic

    /// <summary>
    /// Military outlook
    /// </summary>
    public OutlookAxis Military;  // Pacifist <-> Militarist
}

public struct OutlookAxis
{
    /// <summary>
    /// -1 to +1 scale
    /// Economic: -1 = Spiritualist, +1 = Materialist
    /// Expansion: -1 = Isolationist, +1 = Expansionist
    /// Tolerance: -1 = Xenophobic, +1 = Xenophilic
    /// Military: -1 = Pacifist, +1 = Militarist
    /// </summary>
    public float Value;
}
```

---

## Bonus Computation System

### Core Formula

```csharp
[BurstCompile]
public static RelationBonus ComputeRelationBonus(
    Entity sourceEntity,
    Entity targetEntity,
    int relationValue,
    VillagerAlignment sourceAlignment,
    VillagerAlignment targetAlignment,
    EntityOutlook sourceOutlook,
    EntityOutlook targetOutlook,
    AggregateStats sourceStats,
    AggregateStats targetStats,
    DynamicBuffer<EntityRelationBuffer> sourceRelations,
    DynamicBuffer<EntityRelationBuffer> targetRelations)
{
    // 1. Determine what target can OFFER based on their strengths
    var targetOffers = DetermineOffers(targetAlignment, targetOutlook, targetStats);

    // 2. Determine what source NEEDS based on their weaknesses
    var sourceNeeds = DetermineNeeds(sourceAlignment, sourceOutlook, sourceStats);

    // 3. Match offers to needs
    var matchedBonuses = MatchOffersToNeeds(targetOffers, sourceNeeds);

    // 4. Scale by relation value
    foreach (var bonus in matchedBonuses)
    {
        bonus.Magnitude *= RelationToMultiplier(relationValue);
    }

    // 5. Apply alignment synergy/conflict modifiers
    ApplyAlignmentModifiers(matchedBonuses, sourceAlignment, targetAlignment);

    // 6. Apply outlook combo bonuses
    ApplyOutlookCombos(matchedBonuses, sourceOutlook, targetOutlook);

    // 7. Check for "enemy of my enemy" bonuses
    ApplySharedEnemyBonuses(matchedBonuses, sourceRelations, targetRelations);

    // 8. Apply hate-driven bonuses (gain strength from opposition)
    ApplyHateDrivenBonuses(matchedBonuses, sourceRelations, sourceStats);

    return matchedBonuses;
}

[BurstCompile]
static NativeList<BonusOffer> DetermineOffers(
    VillagerAlignment alignment,
    EntityOutlook outlook,
    AggregateStats stats)
{
    var offers = new NativeList<BonusOffer>(Allocator.Temp);

    // Warlike entities offer military bonuses
    if (outlook.Military.Value > 0.5f && stats.MilitaryStrength > 50f)
    {
        offers.Add(new BonusOffer
        {
            BonusType = RelationBonusTypeId.MercenaryDiscount,
            BaseMagnitude = stats.MilitaryStrength / 100f,
            RequiredRelation = 50  // Must be friendly
        });

        offers.Add(new BonusOffer
        {
            BonusType = RelationBonusTypeId.MilitaryDefenseBonus,
            BaseMagnitude = stats.MilitaryStrength / 200f,
            RequiredRelation = 70  // Must be allied
        });
    }

    // Materialist entities offer economic bonuses
    if (outlook.Economic.Value > 0.5f && stats.EconomicPower > 50f)
    {
        offers.Add(new BonusOffer
        {
            BonusType = RelationBonusTypeId.TradePriceDiscount,
            BaseMagnitude = stats.EconomicPower / 100f,
            RequiredRelation = 30  // Just need to be friendly
        });

        offers.Add(new BonusOffer
        {
            BonusType = RelationBonusTypeId.ResourceProductionBonus,
            BaseMagnitude = stats.EconomicPower / 150f,
            RequiredRelation = 60
        });
    }

    // Intelligent entities offer research bonuses
    if (stats.IntelligenceAverage > 0.7f)
    {
        offers.Add(new BonusOffer
        {
            BonusType = RelationBonusTypeId.TechSharingBonus,
            BaseMagnitude = stats.IntelligenceAverage * 0.5f,
            RequiredRelation = 80  // Requires very high trust
        });
    }

    // Culturally influential entities offer cultural bonuses
    if (stats.CulturalInfluence > 50f)
    {
        offers.Add(new BonusOffer
        {
            BonusType = RelationBonusTypeId.DiplomaticInfluenceBonus,
            BaseMagnitude = stats.CulturalInfluence / 100f,
            RequiredRelation = 40
        });
    }

    return offers;
}

[BurstCompile]
static NativeList<BonusNeed> DetermineNeeds(
    VillagerAlignment alignment,
    EntityOutlook outlook,
    AggregateStats stats)
{
    var needs = new NativeList<BonusNeed>(Allocator.Temp);

    // Peaceful entities need military protection
    if (outlook.Military.Value < -0.3f && stats.MilitaryStrength < 30f)
    {
        needs.Add(new BonusNeed
        {
            BonusType = RelationBonusTypeId.MilitaryDefenseBonus,
            Urgency = 1f - (stats.MilitaryStrength / 30f)  // More urgent if weaker
        });

        needs.Add(new BonusNeed
        {
            BonusType = RelationBonusTypeId.MercenaryDiscount,
            Urgency = 0.7f
        });
    }

    // Low economic power needs trade bonuses
    if (stats.EconomicPower < 30f)
    {
        needs.Add(new BonusNeed
        {
            BonusType = RelationBonusTypeId.TradePriceDiscount,
            Urgency = 1f - (stats.EconomicPower / 30f)
        });
    }

    // Low tech needs research bonuses
    if (stats.TechLevel < 30f)
    {
        needs.Add(new BonusNeed
        {
            BonusType = RelationBonusTypeId.TechSharingBonus,
            Urgency = 0.8f
        });
    }

    // Warlike entities need resources for war
    if (outlook.Military.Value > 0.5f)
    {
        needs.Add(new BonusNeed
        {
            BonusType = RelationBonusTypeId.ResourceProductionBonus,
            Urgency = outlook.Military.Value
        });
    }

    return needs;
}

[BurstCompile]
static void ApplyAlignmentModifiers(
    NativeList<RelationBonus> bonuses,
    VillagerAlignment sourceAlignment,
    VillagerAlignment targetAlignment)
{
    // Alignment difference affects bonus effectiveness

    float moralDiff = math.abs(sourceAlignment.MoralAxis - targetAlignment.MoralAxis);
    float orderDiff = math.abs(sourceAlignment.OrderAxis - targetAlignment.OrderAxis);
    float purityDiff = math.abs(sourceAlignment.PurityAxis - targetAlignment.PurityAxis);

    float alignmentCompatibility = 1f - ((moralDiff + orderDiff + purityDiff) / 3f);

    // Similar alignments get bonus multiplier
    float alignmentMultiplier = math.lerp(0.7f, 1.3f, alignmentCompatibility);

    for (int i = 0; i < bonuses.Length; i++)
    {
        var bonus = bonuses[i];
        bonus.Magnitude *= alignmentMultiplier;
        bonuses[i] = bonus;
    }
}

[BurstCompile]
static void ApplyOutlookCombos(
    NativeList<RelationBonus> bonuses,
    EntityOutlook sourceOutlook,
    EntityOutlook targetOutlook)
{
    // Specific outlook combinations grant special bonuses

    // Peaceful + Warlike combo: Mercenary discount amplified
    if (sourceOutlook.Military.Value < -0.3f && targetOutlook.Military.Value > 0.5f)
    {
        for (int i = 0; i < bonuses.Length; i++)
        {
            if (bonuses[i].BonusType == RelationBonusTypeId.MercenaryDiscount)
            {
                var bonus = bonuses[i];
                bonus.Magnitude *= 1.5f;  // 50% boost to mercenary discount
                bonuses[i] = bonus;
            }
        }
    }

    // Materialist + Spiritualist combo: Cultural exchange bonus
    if (sourceOutlook.Economic.Value > 0.5f && targetOutlook.Economic.Value < -0.5f)
    {
        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.CulturalSpreadBonus,
            Magnitude = 0.3f,
            Conditions = RelationBonusConditions.RequiresMutualBenefit
        });
    }

    // Xenophobic + Xenophilic combo: Intelligence gathering bonus
    if (sourceOutlook.Tolerance.Value < -0.5f && targetOutlook.Tolerance.Value > 0.5f)
    {
        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.IntelligenceGatheringBonus,
            Magnitude = 0.4f,
            Conditions = RelationBonusConditions.RequiresProximity
        });
    }
}

[BurstCompile]
static void ApplySharedEnemyBonuses(
    NativeList<RelationBonus> bonuses,
    DynamicBuffer<EntityRelationBuffer> sourceRelations,
    DynamicBuffer<EntityRelationBuffer> targetRelations)
{
    // "Enemy of my enemy is my friend" bonuses

    int sharedEnemyCount = 0;

    // Find entities both source and target have negative relations with
    for (int i = 0; i < sourceRelations.Length; i++)
    {
        if (sourceRelations[i].RelationValue < -30)  // Source dislikes this entity
        {
            for (int j = 0; j < targetRelations.Length; j++)
            {
                if (targetRelations[j].TargetEntity == sourceRelations[i].TargetEntity &&
                    targetRelations[j].RelationValue < -30)  // Target also dislikes this entity
                {
                    sharedEnemyCount++;
                }
            }
        }
    }

    if (sharedEnemyCount > 0)
    {
        // Grant alliance strength bonus based on number of shared enemies
        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.AllianceStrengthBonus,
            Magnitude = math.min(sharedEnemyCount * 0.15f, 0.6f),  // Cap at 60%
            Conditions = RelationBonusConditions.RequiresSharedEnemy
        });

        // Grant joint military operations bonus
        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.JointMilitaryOperations,
            Magnitude = math.min(sharedEnemyCount * 0.1f, 0.4f),
            Conditions = RelationBonusConditions.RequiresSharedEnemy | RelationBonusConditions.RequiresAlliance
        });
    }
}

[BurstCompile]
static void ApplyHateDrivenBonuses(
    NativeList<RelationBonus> bonuses,
    DynamicBuffer<EntityRelationBuffer> sourceRelations,
    AggregateStats sourceStats)
{
    // Gain bonuses from being hated (intelligent/adaptive entities compensate)

    // Count hostile relations
    int hostileCount = 0;
    int enemyCount = 0;

    for (int i = 0; i < sourceRelations.Length; i++)
    {
        if (sourceRelations[i].RelationValue < -50)
            enemyCount++;
        else if (sourceRelations[i].RelationValue < -10)
            hostileCount++;
    }

    // Intelligent entities mobilize when threatened
    if (sourceStats.IntelligenceAverage > 0.7f && (enemyCount > 0 || hostileCount > 2))
    {
        // Production bonus from compensating directives
        float threatLevel = (enemyCount * 0.3f + hostileCount * 0.1f);

        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.ResourceProductionBonus,
            Magnitude = math.min(threatLevel * sourceStats.IntelligenceAverage, 0.5f),
            Conditions = RelationBonusConditions.None
        });

        // Military production bonus
        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.MilitaryProductionBonus,
            Magnitude = math.min(threatLevel * sourceStats.IntelligenceAverage * 0.8f, 0.6f),
            Conditions = RelationBonusConditions.None
        });

        // Counter-intelligence bonus (paranoia helps)
        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.CounterIntelligenceBonus,
            Magnitude = math.min(threatLevel * 0.5f, 0.4f),
            Conditions = RelationBonusConditions.None
        });
    }

    // Warlike entities gain combat effectiveness from having enemies
    if (sourceStats.MilitaryStrength > 50f && enemyCount > 0)
    {
        bonuses.Add(new RelationBonus
        {
            BonusType = RelationBonusTypeId.CombatEffectivenessBonus,
            Magnitude = math.min(enemyCount * 0.1f, 0.3f),
            Conditions = RelationBonusConditions.RequiresWar
        });
    }
}
```

---

## Example Scenarios

### Example 1: Peaceful Village + Warlike Neighbor (Godgame)

```csharp
// PEACEFUL VILLAGE
PeacefulVillage = {
    Alignment: { MoralAxis = 0.9f, OrderAxis = 0.8f },
    Outlook: { Military = -0.7f },  // Very pacifist
    Stats: { MilitaryStrength = 15f, EconomicPower = 65f }
};

// WARLIKE VILLAGE
WarlikeVillage = {
    Alignment: { MoralAxis = 0.3f, OrderAxis = 0.4f },
    Outlook: { Military = 0.9f },  // Very militarist
    Stats: { MilitaryStrength = 85f, EconomicPower = 35f }
};

// Relation: +75 (Allied)

// BONUSES FOR PEACEFUL VILLAGE:
1. MercenaryDiscount
   - Base: 85 / 100 = 0.85
   - Relation multiplier: 0.75
   - Outlook combo (Peaceful + Warlike): ×1.5
   - Final: 0.85 × 0.75 × 1.5 = 0.956 ≈ 96% discount!
   → "Our warlike allies will fight for almost nothing"

2. MilitaryDefenseBonus
   - Base: 85 / 200 = 0.425
   - Relation multiplier: 0.75
   - Final: 0.425 × 0.75 = 0.319 ≈ 32% defense boost
   → "They will defend us in times of need"

// BONUSES FOR WARLIKE VILLAGE:
1. ResourceProductionBonus
   - Base: 65 / 150 = 0.433
   - Relation multiplier: 0.75
   - Final: 0.433 × 0.75 = 0.325 ≈ 33% resource boost
   → "Peaceful neighbors supply our war machine"

2. TradePriceDiscount
   - Base: 65 / 100 = 0.65
   - Relation multiplier: 0.75
   - Final: 0.65 × 0.75 = 0.488 ≈ 49% discount
   → "They sell us resources cheaply"

RESULT: Mutually beneficial relationship despite opposite values
→ Peaceful gets protection, Warlike gets resources ✨
```

---

### Example 2: Xenophobic Colony + Shared Enemies (Space4X)

```csharp
// XENOPHOBIC COLONY A
XenophobicColonyA = {
    Outlook: { Tolerance = -0.9f },  // Very xenophobic
    Relations: {
        Entity_XenoEmpire: -85,  // Hates aliens
        Entity_HumanColony: +60  // Likes similar humans
    }
};

// HUMAN COLONY B
HumanColonyB = {
    Outlook: { Tolerance = -0.4f },  // Mildly xenophobic
    Relations: {
        Entity_XenoEmpire: -70,  // Also hates same aliens
        Entity_XenophobicColonyA: +60
    }
};

// SHARED ENEMY: Xeno Empire (both hate it)

// BONUSES FOR BOTH COLONIES:
1. AllianceStrengthBonus
   - Shared enemies: 1
   - Magnitude: 1 × 0.15 = 0.15 ≈ 15% alliance strength
   → "United against the xeno threat"

2. JointMilitaryOperations
   - Shared enemies: 1
   - Magnitude: 1 × 0.1 = 0.10 ≈ 10% joint military bonus
   → "Coordinated attacks against aliens"

3. DiplomaticInfluenceBonus (with other anti-xeno factions)
   - Magnitude: 0.25
   → "Other human colonies see us as strong alliance"

MEANWHILE: Xeno Empire hates them back...

// XENO EMPIRE (intelligent, hated by 2+ colonies)
XenoEmpire = {
    Stats: { IntelligenceAverage = 0.85 },  // Very intelligent
    HostileRelations: 2 (ColonyA, ColonyB)
};

// HATE-DRIVEN BONUSES FOR XENO EMPIRE:
1. ResourceProductionBonus
   - Threat level: 2 × 0.3 = 0.6
   - Intelligence modifier: 0.85
   - Magnitude: 0.6 × 0.85 = 0.51 ≈ 51% production boost!
   → "Mobilizing economy to counter human aggression"

2. MilitaryProductionBonus
   - Magnitude: 0.6 × 0.85 × 0.8 = 0.408 ≈ 41% military production
   → "Building fleet to crush human colonies"

3. CounterIntelligenceBonus
   - Magnitude: 0.6 × 0.5 = 0.3 ≈ 30% counter-intel
   → "Paranoia makes us vigilant"

RESULT: Arms race!
→ Humans unite against aliens
→ Aliens mobilize from being hated
→ Emergent galactic conflict ✨
```

---

### Example 3: Corrupt Warlike vs Pure Warlike (Godgame)

```csharp
// CORRUPT WARLIKE VILLAGE
CorruptWarlike = {
    Alignment: { MoralAxis = 0.1f, PurityAxis = 0.2f },  // Corrupt
    Outlook: { Military = 0.8f }
};

// PURE WARLIKE VILLAGE
PureWarlike = {
    Alignment: { MoralAxis = 0.4f, PurityAxis = 0.9f },  // Honorable
    Outlook: { Military = 0.9f }
};

// PEACEFUL VILLAGE (weak target)
PeacefulTarget = {
    Alignment: { MoralAxis = 0.9f },
    Stats: { MilitaryStrength = 10f }
};

// HOW EACH WARLIKE VILLAGE VIEWS PEACEFUL TARGET:

// CORRUPT WARLIKE:
ReactionToWeak = {
    Interpretation: "Opportunity for easy conquest",
    BonusGained: ResourceGatheringBonus (raiding),
    Magnitude: 0.4,
    Condition: "Will attack if relation < 0"
};
→ "Weak villages are TARGETS"

// PURE WARLIKE:
ReactionToWeak = {
    Interpretation: "No honor in crushing weaklings",
    BonusGained: None,
    Magnitude: 0,
    Condition: "Will ignore unless provoked"
};
→ "Weak villages are BENEATH US"

// If Corrupt Warlike attacks Peaceful:
Relations = {
    PureWarlike → CorruptWarlike: -25,  // "Dishonorable cowards"
    PeacefulTarget → CorruptWarlike: -95,  // "We hate you!"
    OtherPeaceful → CorruptWarlike: -40   // "Bullies!"
};

// HATE-DRIVEN BONUS FOR CORRUPT WARLIKE:
1. CombatEffectivenessBonus
   - Enemies: 3
   - Magnitude: 3 × 0.1 = 0.3 ≈ 30% combat boost
   → "We thrive on being hated"

2. ConversionResistanceBonus
   - Magnitude: 0.2
   → "Their hatred makes us stronger in our ways"

RESULT: Alignment determines WHAT you value in relations
→ Corrupt sees weakness as opportunity
→ Pure seeks worthy opponents
→ Different strategic playstyles ✨
```

---

### Example 4: Materialist + Spiritualist Mutual Benefits (Space4X)

```csharp
// MATERIALIST COLONY A
MaterialistA = {
    Outlook: { Economic = 0.9f },  // Very materialist
    Stats: { EconomicPower = 85f, TechLevel = 75f },
    Relations: {
        SpiritualistB: +55,
        MutualSpiritualistC: +40
    }
};

// SPIRITUALIST COLONY B
SpiritualistB = {
    Outlook: { Economic = -0.8f },  // Very spiritualist
    Stats: { CulturalInfluence = 90f, EconomicPower = 35f },
    Relations: {
        MaterialistA: +55,
        MutualSpiritualistC: +70
    }
};

// MUTUAL SPIRITUALIST COLONY C (friend of both)
MutualSpiritualistC = {
    Outlook: { Economic = -0.7f },
    Relations: {
        MaterialistA: +40,
        SpiritualistB: +70
    }
};

// BONUSES FOR MATERIALIST A:

1. TradePriceDiscount (from Spiritualist B)
   - Base: 35 / 100 = 0.35
   - Relation: +55 → 0.55 multiplier
   - Final: 0.35 × 0.55 = 0.1925 ≈ 19% discount
   → "Spiritualists value our goods despite ideological differences"

2. DiplomaticInfluenceBonus (through mutual friend C)
   - Magnitude: 0.30
   - Condition: MaterialistA + SpiritualistB both have +40 with C
   → "Our spiritualist ally vouches for us with Colony C"
   → MaterialistA gains +10 relations with ALL of SpiritualistB's friends!

3. CulturalExchangeBonus
   - From outlook combo (Materialist + Spiritualist)
   - Magnitude: 0.3
   → "Learning from each other's perspectives"

// BONUSES FOR SPIRITUALIST B:

1. TechSharingBonus (from Materialist A)
   - Base: 0.75 × 0.5 = 0.375
   - Relation: +55 → 0.55 multiplier
   - Final: 0.375 × 0.55 = 0.206 ≈ 21% tech sharing
   → "Materialists share their advanced technology"

2. ResourceProductionBonus
   - Base: 85 / 150 = 0.567
   - Relation: +55 → 0.55 multiplier
   - Final: 0.567 × 0.55 = 0.312 ≈ 31% production boost
   → "Material goods flow from our materialist partners"

RESULT: Cross-ideology cooperation works!
→ Each provides what the other lacks
→ Mutual friend amplifies bonuses
→ Ideology differences don't prevent mutual benefit ✨
```

---

### Example 5: Intelligent Colony Under Siege (Space4X)

```csharp
// INTELLIGENT COLONY (surrounded by hostiles)
IntelligentColony = {
    Stats: {
        IntelligenceAverage = 0.92,  // Genius population
        MilitaryStrength = 45f,
        EconomicPower = 60f
    },
    Relations: {
        HostileEmpire1: -85,
        HostileEmpire2: -70,
        HostileEmpire3: -60,
        HostilePirates: -55
    }
};

// HATE-DRIVEN BONUSES (mobilization from threat):

// Calculate threat level
ThreatLevel = {
    Enemies (-50 or worse): 4
    Total: 4 × 0.3 = 1.2 threat
};

// 1. ResourceProductionBonus
Magnitude = math.min(1.2 × 0.92, 0.5) = 0.5 (capped)
→ "Emergency economic mobilization: +50% production!"

// 2. MilitaryProductionBonus
Magnitude = math.min(1.2 × 0.92 × 0.8, 0.6) = 0.6 (capped)
→ "War economy activated: +60% military production!"

// 3. CounterIntelligenceBonus
Magnitude = math.min(1.2 × 0.5, 0.4) = 0.4
→ "Paranoia pays off: +40% counter-intelligence!"

// 4. ResearchSpeedBonus (desperation innovation)
Magnitude = 0.35
→ "Necessity breeds invention: +35% research speed!"

// 5. RecruitmentBonus (total war)
Magnitude = 0.45
→ "Population mobilizes: +45% recruitment efficiency!"

// COMBINED EFFECT:
BaseProduction = 60
BonusedProduction = 60 × 1.5 = 90 (+50%)

BaseMilitary = 45
MilitaryProduction = 45 × 1.6 = 72 (+60%)
TotalWarEffort = 72 × 1.45 (recruitment) = 104.4!

RESULT: Intelligent colony STRONGER when threatened
→ Turns siege into competitive advantage
→ Hostiles created a monster ✨
```

---

## Aggregate Stat Thresholds

Different aggregate stat levels unlock different bonus types:

```csharp
[BurstCompile]
static bool CanGrantBonus(
    RelationBonusTypeId bonusType,
    AggregateStats stats)
{
    return bonusType switch
    {
        // Military bonuses require military strength
        RelationBonusTypeId.MercenaryDiscount =>
            stats.MilitaryStrength > 50f,

        RelationBonusTypeId.MilitaryDefenseBonus =>
            stats.MilitaryStrength > 60f,

        RelationBonusTypeId.JointMilitaryOperations =>
            stats.MilitaryStrength > 70f,

        // Economic bonuses require economic power
        RelationBonusTypeId.TradePriceDiscount =>
            stats.EconomicPower > 30f,

        RelationBonusTypeId.ResourceProductionBonus =>
            stats.EconomicPower > 50f,

        // Intelligence bonuses require high average intelligence
        RelationBonusTypeId.TechSharingBonus =>
            stats.IntelligenceAverage > 0.7f,

        RelationBonusTypeId.CounterIntelligenceBonus =>
            stats.IntelligenceAverage > 0.6f,

        RelationBonusTypeId.IntelligenceGatheringBonus =>
            stats.IntelligenceAverage > 0.5f,

        // Cultural bonuses require cultural influence
        RelationBonusTypeId.DiplomaticInfluenceBonus =>
            stats.CulturalInfluence > 40f,

        RelationBonusTypeId.IdeologicalInfluenceBonus =>
            stats.CulturalInfluence > 60f,

        // Population-based bonuses
        RelationBonusTypeId.RecruitmentBonus =>
            stats.PopulationCount > 100,

        _ => true  // Other bonuses have no stat requirements
    };
}
```

---

## Bonus Application System

```csharp
/// <summary>
/// System that computes and applies relation bonuses
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SocialSystemsGroup))]
[UpdateAfter(typeof(ApplyReactionToRelationsSystem))]
public partial struct ComputeRelationBonusesSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // For each entity with relations
        foreach (var (relationBuffer, alignment, outlook, stats, entity) in
            SystemAPI.Query<
                DynamicBuffer<EntityRelationBuffer>,
                RefRO<VillagerAlignment>,
                RefRO<EntityOutlook>,
                RefRO<AggregateStats>>()
            .WithEntityAccess())
        {
            // Clear existing bonuses
            if (state.EntityManager.HasBuffer<RelationBonus>(entity))
            {
                var bonusBuffer = state.EntityManager.GetBuffer<RelationBonus>(entity);
                bonusBuffer.Clear();
            }
            else
            {
                ecb.AddBuffer<RelationBonus>(entity);
            }

            // Compute bonuses from each relation
            foreach (var relation in relationBuffer)
            {
                if (relation.RelationValue < 10)
                    continue;  // Only friendly+ relations grant bonuses

                // Get target entity's data
                if (!state.EntityManager.Exists(relation.TargetEntity))
                    continue;

                var targetAlignment = state.EntityManager.GetComponentData<VillagerAlignment>(relation.TargetEntity);
                var targetOutlook = state.EntityManager.GetComponentData<EntityOutlook>(relation.TargetEntity);
                var targetStats = state.EntityManager.GetComponentData<AggregateStats>(relation.TargetEntity);
                var targetRelations = state.EntityManager.GetBuffer<EntityRelationBuffer>(relation.TargetEntity);

                // Compute bonuses
                var bonuses = ComputeRelationBonus(
                    entity,
                    relation.TargetEntity,
                    relation.RelationValue,
                    alignment.ValueRO,
                    targetAlignment,
                    outlook.ValueRO,
                    targetOutlook,
                    stats.ValueRO,
                    targetStats,
                    relationBuffer,
                    targetRelations);

                // Add to bonus buffer
                var bonusBuffer = state.EntityManager.GetBuffer<RelationBonus>(entity);
                foreach (var bonus in bonuses)
                {
                    bonusBuffer.Add(bonus);
                }

                bonuses.Dispose();
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## Bonus Consumption Examples

### Economy System

```csharp
[BurstCompile]
partial struct ApplyTradeBonusesSystem : IJobEntity
{
    void Execute(
        ref TradeIncome income,
        in DynamicBuffer<RelationBonus> bonuses)
    {
        float totalDiscount = 0f;
        float totalProductionBonus = 0f;

        foreach (var bonus in bonuses)
        {
            switch (bonus.BonusType)
            {
                case RelationBonusTypeId.TradePriceDiscount:
                    totalDiscount += bonus.Magnitude;
                    break;

                case RelationBonusTypeId.ResourceProductionBonus:
                    totalProductionBonus += bonus.Magnitude;
                    break;
            }
        }

        // Apply bonuses
        income.PurchaseCosts *= (1f - totalDiscount);
        income.ProductionRate *= (1f + totalProductionBonus);
    }
}
```

### Military System

```csharp
[BurstCompile]
partial struct ApplyMilitaryBonusesSystem : IJobEntity
{
    void Execute(
        ref MilitaryCapability military,
        in DynamicBuffer<RelationBonus> bonuses)
    {
        float defenseBonu = 0f;
        float recruitmentBonus = 0f;
        float productionBonus = 0f;
        float combatBonus = 0f;

        foreach (var bonus in bonuses)
        {
            switch (bonus.BonusType)
            {
                case RelationBonusTypeId.MilitaryDefenseBonus:
                    defenseBonus += bonus.Magnitude;
                    break;

                case RelationBonusTypeId.RecruitmentBonus:
                    recruitmentBonus += bonus.Magnitude;
                    break;

                case RelationBonusTypeId.MilitaryProductionBonus:
                    productionBonus += bonus.Magnitude;
                    break;

                case RelationBonusTypeId.CombatEffectivenessBonus:
                    combatBonus += bonus.Magnitude;
                    break;
            }
        }

        // Apply bonuses
        military.DefenseRating *= (1f + defenseBonus);
        military.RecruitmentSpeed *= (1f + recruitmentBonus);
        military.ProductionRate *= (1f + productionBonus);
        military.CombatEffectiveness *= (1f + combatBonus);
    }
}
```

### Research System

```csharp
[BurstCompile]
partial struct ApplyResearchBonusesSystem : IJobEntity
{
    void Execute(
        ref ResearchProgress research,
        in DynamicBuffer<RelationBonus> bonuses)
    {
        float techSharingBonus = 0f;
        float researchSpeedBonus = 0f;

        foreach (var bonus in bonuses)
        {
            switch (bonus.BonusType)
            {
                case RelationBonusTypeId.TechSharingBonus:
                    techSharingBonus += bonus.Magnitude;
                    break;

                case RelationBonusTypeId.ResearchSpeedBonus:
                    researchSpeedBonus += bonus.Magnitude;
                    break;
            }
        }

        // Tech sharing reduces research cost
        research.CostReduction = techSharingBonus;

        // Research speed increases progress per tick
        research.ProgressPerTick *= (1f + researchSpeedBonus);
    }
}
```

---

## Strategic Implications

### 1. Diverse Alliance Portfolios

```
OPTIMAL STRATEGY: Ally with different specializations

Peaceful Empire's Ideal Allies:
├─ Warlike Neighbor: Military protection
├─ Materialist Trader: Economic bonuses
├─ Intelligent Colony: Tech sharing
└─ Culturally Influential: Diplomatic leverage

AVOID: Allying only with similar entities
→ Redundant bonuses, missed opportunities
```

### 2. Enemy Management

```
STRATEGIC ENEMY SELECTION

Warlike Empire wants enemies that:
├─ Grant combat effectiveness bonuses (being hated)
├─ Share enemies with potential allies (common foe)
└─ Are weak enough to raid profitably

AVOID: Making enemies with:
├─ Potential trade partners (lost economic bonuses)
├─ Friends of your allies (complex diplomacy)
└─ Overwhelmingly powerful (existential threat)
```

### 3. Alignment-Based Min-Maxing

```
CORRUPT WARLIKE STRATEGY:
Goal: Maximize power from hatred
├─ Raid weak peaceful villages (easy targets)
├─ Accumulate enemies (+combat effectiveness)
├─ Don't care about reputation
└─ Thrive on being hated

PURE WARLIKE STRATEGY:
Goal: Gain respect through honorable combat
├─ Ignore weak targets (no honor)
├─ Challenge strong opponents
├─ Build reputation with similar warriors
└─ Alliance strength from shared values

SAME OUTLOOK, DIFFERENT STRATEGIES!
```

### 4. Hate-Driven Power Spikes

```
INTELLIGENT COLONY UNDER SIEGE:

T=0: Normal production
EconomicPower = 60
MilitaryStrength = 45

T=10: First hostile relation (-60)
Bonuses: +25% production, +30% military
EconomicPower = 75
MilitaryStrength = 58.5

T=20: Three hostile relations (-60, -70, -85)
Bonuses: +50% production, +60% military
EconomicPower = 90 (!)
MilitaryStrength = 72 (!!)

T=30: Four hostile relations
Bonuses: CAPPED at +50% prod, +60% mil
EconomicPower = 90
MilitaryStrength = 72
+ Counter-intel +40%
+ Research +35%
+ Recruitment +45%

RESULT: Colony is now STRONGER than before siege
→ Hostiles created a powerhouse
→ "What doesn't kill us makes us stronger" ✨
```

---

## UI Presentation

### Godgame: Relation Bonus Tooltips

```csharp
// When hovering over village relation
void ShowRelationTooltip(Entity village, Entity targetVillage)
{
    var bonuses = em.GetBuffer<RelationBonus>(village);

    StringBuilder tooltip = new StringBuilder();
    tooltip.Append($"Relation with {targetVillage.Name}: +{relationValue}\n\n");

    tooltip.Append("Active Bonuses:\n");

    foreach (var bonus in bonuses)
    {
        if (bonus.SourceEntity == targetVillage)
        {
            string bonusName = BonusTypeToString(bonus.BonusType);
            string magnitude = $"+{bonus.Magnitude * 100:F0}%";

            tooltip.Append($"• {bonusName}: {magnitude}\n");

            // Show reason
            string reason = GetBonusReason(bonus, village, targetVillage);
            tooltip.Append($"  ({reason})\n");
        }
    }

    // Example output:
    // Relation with Stormhaven: +75
    //
    // Active Bonuses:
    // • Mercenary Discount: +96%
    //   (Warlike ally provides military services)
    // • Military Defense: +32%
    //   (Allied warriors will defend us)
}
```

### Space4X: Diplomatic Relations Screen

```csharp
// Faction diplomacy screen
void ShowDiplomaticBonuses(Entity faction, Entity targetFaction)
{
    var bonuses = em.GetBuffer<RelationBonus>(faction);

    UI.Header($"Relations with {targetFaction.Name}");
    UI.RelationBar(relationValue);  // Visual bar -100 to +100

    UI.Section("Economic Bonuses");
    foreach (var bonus in GetEconomicBonuses(bonuses, targetFaction))
    {
        UI.BonusRow(bonus.BonusType, bonus.Magnitude);
    }

    UI.Section("Military Bonuses");
    foreach (var bonus in GetMilitaryBonuses(bonuses, targetFaction))
    {
        UI.BonusRow(bonus.BonusType, bonus.Magnitude);
    }

    UI.Section("Shared Enemies");
    var sharedEnemies = GetSharedEnemies(faction, targetFaction);
    foreach (var enemy in sharedEnemies)
    {
        UI.EnemyRow(enemy, "Grants +15% Alliance Strength");
    }

    UI.Section("Strategic Value");
    float totalValue = CalculateStrategicValue(bonuses);
    UI.ValueBar(totalValue);  // Visual indicator of relationship value
}
```

---

## Telemetry

```csharp
[BurstCompile]
partial struct RelationBonusTelemetrySystem : IJobEntity
{
    public TelemetryStream TelemetryStream;

    void Execute(in DynamicBuffer<RelationBonus> bonuses)
    {
        // Count bonuses by type
        var bonusCounts = new NativeHashMap<RelationBonusTypeId, int>(16, Allocator.Temp);

        float totalMagnitude = 0f;

        foreach (var bonus in bonuses)
        {
            // Count
            if (bonusCounts.TryGetValue(bonus.BonusType, out int count))
                bonusCounts[bonus.BonusType] = count + 1;
            else
                bonusCounts.Add(bonus.BonusType, 1);

            // Sum magnitude
            totalMagnitude += bonus.Magnitude;
        }

        // Emit telemetry
        foreach (var (type, count) in bonusCounts)
        {
            TelemetryStream.Emit(new TelemetryMetric
            {
                Category = TelemetryCategory.Social,
                Name = $"RelationBonus_{type}",
                Value = count
            });
        }

        TelemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Social,
            Name = "RelationBonus_TotalMagnitude",
            Value = totalMagnitude
        });

        bonusCounts.Dispose();
    }
}
```

---

## Summary

The **Relation Bonuses System** creates strategic depth through context-dependent benefits:

✅ **Bonuses depend on WHO you ally with** - Not all friends are equal
✅ **Alignment/outlook combos** - Cross-ideology cooperation works
✅ **Aggregate stat thresholds** - Strong allies grant better bonuses
✅ **Shared enemy bonuses** - "Enemy of my enemy is my friend"
✅ **Hate-driven bonuses** - Intelligent entities gain power from threats
✅ **Strategic relationship management** - Optimize alliance portfolio
✅ **Burst-compatible** - Parallel computation, deterministic
✅ **Visual feedback** - UI shows bonus calculations

**Game Impact:**

**Godgame:**
- Peaceful villages ally with warlike for protection
- Warlike villages ally with peaceful for resources
- Corrupt warlikes raid weak targets (opportunity)
- Pure warlikes ignore weak targets (no honor)
- Hatred fuels power for intelligent/adaptive villages

**Space4X:**
- Cross-cultural alliances grant unique bonuses
- Shared enemies amplify alliance strength
- Intelligent colonies mobilize when threatened
- Xenophobic factions unite against common foes
- Materialist/spiritualist cooperation despite ideology

**Result:** Diplomacy becomes **resource optimization puzzle** where the RIGHT relationships matter more than just HIGH relationships.

**Strategic Depth:**
- Min-max alliance portfolios (diverse specialists)
- Strategic enemy selection (useful hatred)
- Alignment-based playstyles (corrupt vs pure)
- Hate-driven power spikes (threat → strength)
- Mutual benefit across ideologies

**Emergent Gameplay:**
- Arms races from mutual hatred
- Cross-cultural trading blocs
- Ideological cold wars
- Siege economies (besieged colonies mobilize)
- Diplomatic leverage from shared enemies

---

**Related Documentation:**
- [Reactions_And_Relations_System.md](Reactions_And_Relations_System.md) - How relations form
- [Entity_Agnostic_Design.md](Entity_Agnostic_Design.md) - Entities and aggregates
- [General_Forces_System.md](General_Forces_System.md) - Physical forces
- [Multi_Force_Interactions.md](Multi_Force_Interactions.md) - Emergent physics

---

**Last Updated:** 2025-12-18
**Status:** Design Document - Strategic Diplomacy
**Burst Compatible:** Yes
**Deterministic:** Yes
**Creates Strategic Depth:** Yes ✨
**Makes Diplomacy Interesting:** ABSOLUTELY! 🎯
