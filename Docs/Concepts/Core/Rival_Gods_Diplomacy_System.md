# Rival Gods Diplomacy System

**Last Updated:** 2025-12-18
**Status:** Design Document - Divine Politics & Power Acquisition
**Game:** Godgame
**Entity-Agnostic:** Yes (gods are entities with relations)
**Integrates With:** Miracles, Reactions, Relations, Forces, Alignment

---

## Overview

The **Rival Gods Diplomacy System** introduces a **meta-layer** where natural phenomena (vegetation, time, wind, fire, luck) are controlled by **rival deities**. The player, as a new **interloper god**, must manage relations with the established pantheon. Using miracles that affect another god's domain **costs mana** and **affects relations**. By improving relations from **Interloper** → **Tolerated** → **Accepted** → **Allied** → **Conjoined**, the player permanently unlocks that god's powers.

**Core Design Philosophy:**
- **Nature has agency** - Every miracle affects a god's domain
- **Diplomacy > brute force** - Strategic relations unlock powers
- **Competing interests** - Gods have conflicting agendas
- **Permanent rewards** - Conjoined status grants lasting abilities
- **Strategic choices** - Which gods to befriend first matters

---

## The Pantheon of Nature Gods

### Primary Deities (Core Systems)

```
CHRONOS - God of Time
├─ Domain: Time flow, day/night cycles, seasons, aging
├─ Personality: Lawful (+80), Neutral (0), Pure (+60), Patient
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Time acceleration/deceleration
    ├─ Age manipulation (youth, aging)
    ├─ Season control
    └─ Historical rewind (most intrusive!)

VERDARA - Goddess of Growth
├─ Domain: Vegetation, crops, forests, plant life
├─ Personality: Chaotic (+40), Good (+70), Pure (+80), Nurturing
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Plant growth acceleration
    ├─ Harvest bounty
    ├─ Forest summoning
    └─ Blight removal

AEOLUS - God of Wind
├─ Domain: Wind, storms, air currents, weather patterns
├─ Personality: Chaotic (+90), Neutral (0), Neutral (0), Capricious
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Wind direction/strength
    ├─ Storm summoning/dispersal
    ├─ Tornado/hurricane
    └─ Calm air

PYROS - God of Fire
├─ Domain: Fire, heat, combustion, destruction
├─ Personality: Chaotic (+60), Evil (-40), Impure (-30), Wrathful
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Ignition/extinguishing
    ├─ Firestorm
    ├─ Volcanic eruption
    └─ Wildfire control

HYDRIA - Goddess of Water
├─ Domain: Water, rain, rivers, oceans, ice
├─ Personality: Lawful (+40), Good (+50), Pure (+70), Tranquil
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Rain/drought
    ├─ Flood/drought
    ├─ Water purification
    └─ Ice/thaw

FORTUNA - Goddess of Fortune
├─ Domain: Luck, randomness, probability, production efficiency
├─ Personality: Chaotic (+100), Neutral (0), Neutral (0), Whimsical
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Blessing (increase luck)
    ├─ Curse (decrease luck)
    ├─ Production efficiency
    └─ Combat outcomes

TERRA - Goddess of Earth
├─ Domain: Soil, stone, mountains, earthquakes, minerals
├─ Personality: Lawful (+90), Neutral (0), Pure (+40), Steadfast
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Earthquake
    ├─ Mountain summoning
    ├─ Fertile soil
    └─ Mineral vein creation

VITALIS - God of Life
├─ Domain: Health, healing, birth, vitality, disease
├─ Personality: Lawful (+50), Good (+90), Pure (+90), Compassionate
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Healing
    ├─ Plague/cure
    ├─ Birth rate
    └─ Lifespan extension

MORTA - Goddess of Death
├─ Domain: Death, decay, undead, necromancy, endings
├─ Personality: Lawful (+70), Evil (-60), Impure (-70), Grim
├─ Starting Relation: Interloper (-50)
└─ Miracles that affect domain:
    ├─ Death dealing
    ├─ Necromancy
    ├─ Decay acceleration
    └─ Soul manipulation
```

### Secondary Deities (Biome-Specific)

```
SYLVANUS - God of Forests (allied with Verdara)
GLACIUS - God of Ice (allied with Hydria)
FULGOR - God of Lightning (allied with Aeolus, rival of Pyros)
UMBRA - Goddess of Shadows (allied with Morta, rival of Vitalis)
CAELUM - God of Sky (allied with Aeolus)
OCEANUS - God of Seas (allied with Hydria)
```

---

## Mana as Debt/Credit System

### Core Concept

**Mana is NOT a single pool** - it's **debt/credit with individual gods**. Each god has a separate balance representing how much "credit" the player has earned through worship directed to that god.

```csharp
/// <summary>
/// Player's mana balance with a specific god
/// </summary>
public struct GodManaBalance : IBufferElementData
{
    /// <summary>
    /// Which god this balance is with
    /// </summary>
    public Entity GodEntity;

    /// <summary>
    /// Current mana credit (positive = can cast miracles, negative = in debt)
    /// </summary>
    public float CurrentMana;

    /// <summary>
    /// Maximum mana capacity with this god (increases with relations)
    /// </summary>
    public float MaxMana;

    /// <summary>
    /// Mana regeneration rate (worship points per second)
    /// </summary>
    public float RegenerationRate;

    /// <summary>
    /// Whether this god has been absorbed (Conjoined status)
    /// </summary>
    public bool IsAbsorbed;
}
```

**Example:**

```
Player's Mana Balances:
├─ Pyros (Fire): 450 / 1,000 mana (45% credit)
├─ Hydria (Water): 850 / 1,200 mana (71% credit)
├─ Verdara (Growth): 120 / 800 mana (15% credit)
├─ Chronos (Time): -50 / 600 mana (IN DEBT! Can't use time miracles)
└─ Fortuna (Fortune): [ABSORBED] - Infinite mana, FREE miracles
```

### Worship Points Economy

#### Worship Generation

Villagers generate **worship points** through various activities:

```csharp
public struct WorshipGenerator : IComponentData
{
    /// <summary>
    /// Base worship points generated per second
    /// </summary>
    public float BaseWorshipRate;

    /// <summary>
    /// Multiplier based on villager's faith/happiness
    /// </summary>
    public float WorshipMultiplier;

    /// <summary>
    /// Which temple/shrine this villager worships at (determines god)
    /// </summary>
    public Entity WorshipSite;
}
```

**Worship Sources:**

```
TEMPLES & SHRINES (primary):
├─ Temple of Pyros: Generates 10 worship/sec for Pyros
├─ Shrine to Hydria: Generates 5 worship/sec for Hydria
└─ Grand Cathedral: Generates 20 worship/sec (split among gods)

VILLAGER ACTIVITIES (passive):
├─ Praying at temple: +5 worship/sec
├─ Working happily: +0.5 worship/sec (to locally dominant god)
├─ Eating feast: +2 worship/sec burst
└─ Witnessing miracle: +10 worship burst

SPECIAL EVENTS (burst):
├─ Festival day: +50 worship burst (to target god)
├─ Sacrifice ritual: +100 worship burst
├─ Miracle success: +20 worship burst (to miracle's god)
└─ Answered prayer: +30 worship burst
```

#### Worship Direction System

The player **directs worship** to specific gods through temple placement and policies:

```csharp
public struct WorshipDirector : IComponentData
{
    /// <summary>
    /// Entity directing worship (player)
    /// </summary>
    public Entity DirectorEntity;

    /// <summary>
    /// How worship points are distributed
    /// </summary>
    public WorshipDistributionMode Mode;

    /// <summary>
    /// Manual distribution weights (if Mode = Manual)
    /// </summary>
    public FixedList512Bytes<GodWorshipWeight> ManualWeights;
}

public enum WorshipDistributionMode : byte
{
    /// <summary>
    /// Worship goes to nearest temple's god
    /// </summary>
    TempleBased = 0,

    /// <summary>
    /// Player manually sets percentages per god
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Automatically prioritizes gods with low relations
    /// </summary>
    AutoBalance = 2,

    /// <summary>
    /// Focus all worship on single god (fastest progression)
    /// </summary>
    Focused = 3
}

public struct GodWorshipWeight : IBufferElementData
{
    public Entity GodEntity;
    public float Weight;  // 0-1
}
```

**Example Worship Distribution:**

```
TEMPLE-BASED MODE:
Village has:
├─ Temple of Pyros (center) → Attracts 60% of worship
├─ Shrine to Hydria (north) → Attracts 25% of worship
└─ Shrine to Verdara (south) → Attracts 15% of worship

Result: Pyros gets most worship, fastest mana regeneration

MANUAL MODE:
Player sets:
├─ Pyros: 0% (already Conjoined, waste of worship)
├─ Hydria: 40% (pushing toward Allied)
├─ Verdara: 30% (maintain relations)
├─ Chronos: 30% (pay off debt!)
└─ Others: 0%

Result: Strategic worship allocation

FOCUSED MODE:
Player targets Hydria:
├─ Hydria: 100% (all worship goes here)
└─ Others: 0%

Result: Fastest path to Conjoined with Hydria, but neglects others
```

### Mana Transaction Flow

```
WORSHIP → MANA → MIRACLES

Step 1: Worship Generation
├─ Villagers generate 100 worship points/sec total
├─ 60% directed to Pyros = 60 worship/sec
└─ Converted to Pyros mana at 1:1 ratio

Step 2: Mana Accumulation
├─ Pyros mana: 450 → 510 (+60 this second)
├─ Max capacity: 1,000 mana
└─ Can store 490 more before capped

Step 3: Miracle Casting
├─ Player casts "Firestorm" (Pyros domain)
├─ Base cost: 500 mana
├─ Relation mod: 0.7× (Allied with Pyros)
├─ Final cost: 350 mana
└─ Pyros mana: 510 → 160 (-350 spent)

Step 4: Regeneration
├─ Worship continues: +60 mana/sec
├─ In 6 seconds: 160 → 520 mana
└─ Can cast Firestorm again
```

### Max Mana Capacity (Relation-Based)

The **maximum mana** you can store with each god increases with relations:

```csharp
[BurstCompile]
public static float CalculateMaxMana(float relationValue)
{
    // Base capacity at Interloper
    float baseCapacity = 500f;

    // Relation bonus (0-2× multiplier)
    float relationMult = 1f + (relationValue / 100f);  // -50 = 0.5×, +100 = 2×

    // Tech unlocks (temples, cathedrals increase capacity)
    float techMult = GetTempleCapacityMultiplier();

    float maxMana = baseCapacity * relationMult * techMult;

    return math.max(maxMana, 100f);  // Minimum 100 even at Nemesis
}
```

**Example Progression:**

```
CHRONOS (Time God):

Interloper (-50):
├─ Base: 500 mana
├─ Relation: 0.5× multiplier
└─ Max: 250 mana (very limited)

Tolerated (+10):
├─ Base: 500 mana
├─ Relation: 1.1× multiplier
└─ Max: 550 mana (slightly better)

Allied (+60):
├─ Base: 500 mana
├─ Relation: 1.6× multiplier
└─ Max: 800 mana (comfortable)

Conjoined (+90):
├─ Base: 500 mana
├─ Relation: 1.9× multiplier
├─ Max: 950 mana (huge capacity)
└─ BUT: Mana cost is 0, so capacity irrelevant!
```

### God Absorption (Conjoined Status)

When relations reach **Conjoined (+76)**, the player **absorbs** that god's essence:

```csharp
/// <summary>
/// Mark god as absorbed (player has conjoined with them)
/// </summary>
public struct AbsorbedGod : IComponentData
{
    public Entity GodEntity;
    public GodDomain Domain;
    public double AbsorptionTime;
    public bool WorshipRedirected;  // Has player redirected worship elsewhere?
}
```

**Absorption Effects:**

```
BEFORE Absorption (Pyros, Allied +60):
├─ Pyros mana: 720 / 800
├─ Worship to Pyros: 60/sec
├─ Firestorm cost: 350 mana (0.7× modifier)
└─ Can cast ~2 Firestorms before depleting

AFTER Absorption (Pyros, Conjoined +90):
├─ Pyros mana: ∞ (irrelevant, all miracles FREE)
├─ Worship to Pyros: WASTED (god absorbed)
├─ Firestorm cost: 0 mana (FREE FOREVER!)
├─ Player MUST redirect worship to other gods
└─ Can cast infinite Firestorms

Worship Redirection:
├─ 60/sec previously going to Pyros now available
├─ Player redirects to Hydria (pushing toward Conjoined)
├─ Hydria mana regeneration: 40/sec → 100/sec
└─ Faster progression with remaining gods
```

### Strategic Worship Management

#### Problem: Limited Worship Points

```
RESOURCE SCARCITY:
Village generates: 100 worship points/sec
9 gods to befriend
Average needed per god: 11.1 worship/sec

CHOICES:
Option A: Spread evenly (11/sec each)
├─ Pro: All gods progress slowly
├─ Con: Takes forever to reach Conjoined with anyone
└─ Result: Jack-of-all-trades, slow endgame

Option B: Focus one god at a time (100/sec to one)
├─ Pro: Reach Conjoined quickly with priority god
├─ Con: Other gods neglected (may become Nemesis)
└─ Result: Specialized, vulnerable to rival gods

Option C: Focus clusters (Life Cluster gets 80%, others 20%)
├─ Pro: Allied gods progress together, synergy bonuses
├─ Con: Rival clusters become Nemesis
└─ Result: Strong in one area, weak in others

Option D: Dynamic allocation (respond to threats)
├─ Pro: Flexible, adapts to game state
├─ Con: No god reaches Conjoined quickly
└─ Result: Reactive playstyle
```

#### Temple Placement Strategy

**CENTRALIZED TEMPLES (concentrated worship):**
```
Village layout:
        [Pyros Temple]
              ↓
    ← Villagers work here →
         (60% worship)

Pros:
├─ Fast progression with Pyros
├─ Easy to manage
└─ Clear focus

Cons:
├─ Other gods neglected
├─ Vulnerable if Pyros becomes Nemesis
└─ Wastes potential worship diversity
```

**DISTRIBUTED TEMPLES (diversified worship):**
```
Village layout:
[Verdara]    [Village]    [Hydria]
   Shrine      Center       Shrine
    ↓            ↓            ↓
  20% worship  40% to      20% worship
   to Verdara  nearest     to Hydria
               temple

Pros:
├─ Balanced progression
├─ Less vulnerable to single god Nemesis
└─ Flexibility

Cons:
├─ Slower to reach Conjoined
├─ More complex management
└─ Opportunity cost (worship split)
```

### Mana Debt & Overdraft

You CAN cast miracles even with **negative mana** (debt):

```csharp
/// <summary>
/// Calculate if player can cast miracle (even in debt)
/// </summary>
[BurstCompile]
public static bool CanCastMiracle(
    float currentMana,
    float miracleCost,
    float relationValue,
    out float debtPenalty)
{
    // Always allow casting if relation is not Nemesis
    if (relationValue <= -75f)
    {
        debtPenalty = 0f;
        return false;  // Nemesis blocks all miracles
    }

    // Calculate debt after casting
    float manaAfter = currentMana - miracleCost;

    if (manaAfter >= 0f)
    {
        debtPenalty = 0f;
        return true;  // Normal casting
    }

    // Going into debt
    float debtAmount = math.abs(manaAfter);

    // Debt penalty: -1 relation per 100 mana debt
    debtPenalty = debtAmount / 100f;

    return true;  // Allow, but with penalty
}
```

**Example:**

```
DESPERATE CASTING:

Pyros mana: 50 / 1,000
Firestorm cost: 350 mana
Relation: Allied (+60)

Player casts anyway:
├─ Mana after: 50 - 350 = -300 (IN DEBT!)
├─ Debt penalty: 300 / 100 = -3 relations
├─ New relation: +60 → +57
└─ Must repay debt with worship before casting again

Debt Repayment:
├─ Worship regeneration: 60 mana/sec
├─ Time to repay: 300 / 60 = 5 seconds
├─ After 5 sec: -300 → 0 mana (debt cleared)
└─ After 10 sec: 0 → +300 mana (back in credit)
```

### UI: Mana Balances Display

```
[TOP-LEFT HUD]
━━━━━━━━━━━━━━━━━━━━━━━━━━
DIVINE MANA
━━━━━━━━━━━━━━━━━━━━━━━━━━
🔥 Pyros:    720/800  [+60/s]
💧 Hydria:   450/1200 [+40/s]
🌿 Verdara:  120/800  [+15/s]
⏰ Chronos:  -50/600  [+30/s] ⚠️ DEBT
🎲 Fortuna:  [∞] ABSORBED ✨

Total Worship: 145/sec
Available: 145/sec (redirect!)
━━━━━━━━━━━━━━━━━━━━━━━━━━

[CLICK TO MANAGE WORSHIP]
```

**Tooltip on Pyros mana:**

```
┌──────────────────────────┐
│ Pyros - Fire God         │
├──────────────────────────┤
│ Mana: 720 / 800          │
│ Regeneration: +60/sec    │
│                          │
│ Sources:                 │
│ • Temple worship: +50/sec│
│ • Villager prayers: +10  │
│                          │
│ Relation: Allied (+60)   │
│ Miracle discount: 40%    │
│                          │
│ Next threshold:          │
│ Conjoined at +76         │
│ (+16 relations needed)   │
│                          │
│ At Conjoined:            │
│ → FREE fire miracles     │
│ → Redirect 60 worship/sec│
└──────────────────────────┘
```

---

## Relation Mechanics

### Relation Scale

```
Relation Value    Status          Effects
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
-100 to -75       Nemesis         Actively opposes player, sabotages miracles
-74 to -50        Interloper      Starting status, neutral interference
-49 to -25        Suspicious      Watches player, occasional interference
-24 to 0          Wary            Minimal interference
+1 to +25         Tolerated       Allows basic miracles
+26 to +50        Accepted        Reduced mana costs (-20%)
+51 to +75        Allied          Significant mana reduction (-40%), occasional aid
+76 to +100       Conjoined       PERMANENT power unlock, no mana cost for domain
```

### How Relations Change

```csharp
/// <summary>
/// Calculate relation change when player uses a miracle in a god's domain
/// </summary>
[BurstCompile]
public static float CalculateGodReactionToMiracle(
    MiracleType miracle,
    Entity godEntity,
    float miracleIntensity,
    VillagerAlignment godAlignment)
{
    float baseReaction = 0f;

    // 1. Domain intrusion penalty (base)
    baseReaction = -10f * miracleIntensity;  // Higher intensity = more intrusive

    // 2. Alignment compatibility
    float alignmentMod = CalculateAlignmentCompatibility(miracle, godAlignment);
    baseReaction *= (1f + alignmentMod);

    // 3. Intent detection (helpful vs harmful)
    if (IsMiracleHelpful(miracle))
    {
        baseReaction *= 0.5f;  // Less negative if helping mortals
    }
    else if (IsMiracleDestructive(miracle))
    {
        baseReaction *= 1.5f;  // More negative if destructive
    }

    // 4. Rival god conflicts
    if (HasRivalGodConflict(miracle, godEntity))
    {
        baseReaction *= 0.3f;  // Much less negative if harming rival's interests
        // e.g., Rain (Hydria) vs Fire (Pyros) → Hydria approves of extinguishing fires
    }

    return baseReaction;
}
```

**Example:**

```
Player casts "Rain" miracle (intensity: 0.8)
Affected gods:
├─ Hydria (Water): -8 (domain intrusion, but player helps crops)
│   └─ Alignment compat: +0.3 (player Good +40, Hydria Good +50)
│   └─ Intent: Helpful (watering crops) → ×0.5
│   └─ Final: -8 × 1.3 × 0.5 = -5.2 (minimal negative)
│
└─ Pyros (Fire): +6 (rival god, rain extinguishes fires!)
    └─ Rival conflict: Player helps Hydria's domain, harms Pyros
    └─ Final: +6 (Pyros APPROVES because he hates Hydria)
```

---

## Competing God Interests

### Alliance Clusters

```
LIFE CLUSTER (allied):
├─ Verdara (Growth)
├─ Vitalis (Life)
├─ Hydria (Water)
└─ Terra (Earth - nurturing soil)

DESTRUCTION CLUSTER (allied):
├─ Pyros (Fire)
├─ Morta (Death)
├─ Aeolus (Wind - storms)
└─ Umbra (Shadows)

CHAOS CLUSTER (allied):
├─ Fortuna (Luck)
├─ Aeolus (Wind)
└─ Pyros (Fire)

ORDER CLUSTER (allied):
├─ Chronos (Time)
├─ Terra (Earth - steady)
└─ Vitalis (Life - structured)
```

### Rival Pairs (Mutually Exclusive?)

```
OPPOSING PAIRS:
├─ Pyros (Fire) ↔ Hydria (Water)
├─ Vitalis (Life) ↔ Morta (Death)
├─ Verdara (Growth) ↔ Morta (Decay)
└─ Chronos (Order) ↔ Fortuna (Chaos)

IMPLICATION:
Befriending one may harm relations with the other.
BUT: Reaching Conjoined with BOTH opposing gods unlocks ULTIMATE power!
```

**Example:**

```
Player improves relations with Hydria (+60, Allied)
Effect on Pyros:
├─ Rival penalty: -0.5 per +1 with Hydria
├─ Total penalty: -30
└─ Pyros relation: -50 (Interloper) → -80 (Nemesis)

Pyros now actively interferes:
├─ Spontaneous wildfires (10% chance per day)
├─ Player's fire miracles cost +50% mana
└─ Fire-based enemies spawned near player villages

BUT: If player reaches Conjoined with BOTH:
└─ Unlock "Steam" domain (Fire + Water synergy)
    └─ Steam explosions, scalding rain, superheated geysers
```

---

## Miracle Cost Modifiers

### Base Mana Cost

```csharp
public struct MiracleCost : IComponentData
{
    /// <summary>
    /// Base mana cost (before god relation modifiers)
    /// </summary>
    public float BaseCost;

    /// <summary>
    /// Intensity multiplier (0-1 for weak, 1+ for strong)
    /// </summary>
    public float IntensityMultiplier;

    /// <summary>
    /// Which god's domain this affects
    /// </summary>
    public Entity PrimaryGodEntity;

    /// <summary>
    /// Secondary gods affected (for multi-domain miracles)
    /// </summary>
    public FixedList64Bytes<Entity> SecondaryGods;
}
```

### Cost Calculation with God Relations

```csharp
[BurstCompile]
public static float CalculateMiracleCost(
    MiracleCost baseCost,
    NativeArray<GodRelation> godRelations)
{
    float totalCost = baseCost.BaseCost * baseCost.IntensityMultiplier;

    // Primary god relation modifier
    Entity primaryGod = baseCost.PrimaryGodEntity;
    float primaryRelation = GetGodRelation(godRelations, primaryGod);

    float costMod = GetCostModifier(primaryRelation);
    totalCost *= costMod;

    // Secondary god modifiers (additive)
    foreach (var secondaryGod in baseCost.SecondaryGods)
    {
        float secondaryRelation = GetGodRelation(godRelations, secondaryGod);
        float secondaryMod = GetCostModifier(secondaryRelation) - 1f;  // Offset from 1.0
        totalCost *= (1f + (secondaryMod * 0.3f));  // Secondary gods have 30% weight
    }

    return totalCost;
}

[BurstCompile]
static float GetCostModifier(float relationValue)
{
    // Nemesis: 2× cost
    if (relationValue < -75f)
        return 2.0f;

    // Interloper: 1× cost (baseline)
    if (relationValue < -50f)
        return 1.0f;

    // Suspicious: 0.9× cost
    if (relationValue < -25f)
        return 0.9f;

    // Wary: 0.8× cost
    if (relationValue < 0f)
        return 0.8f;

    // Tolerated: 0.7× cost
    if (relationValue < 25f)
        return 0.7f;

    // Accepted: 0.6× cost
    if (relationValue < 50f)
        return 0.6f;

    // Allied: 0.4× cost
    if (relationValue < 75f)
        return 0.4f;

    // Conjoined: 0× cost (FREE!)
    return 0f;
}
```

**Example:**

```
Miracle: "Firestorm" (Pyros domain)
Base cost: 500 mana
Intensity: 1.5× (very strong)

Scenario 1: Interloper with Pyros (-50)
Cost mod: 1.0×
Total: 500 × 1.5 × 1.0 = 750 mana

Scenario 2: Allied with Pyros (+60)
Cost mod: 0.4×
Total: 500 × 1.5 × 0.4 = 300 mana (60% discount!)

Scenario 3: Conjoined with Pyros (+90)
Cost mod: 0×
Total: 0 mana (FREE FOREVER!)

Scenario 4: Nemesis with Pyros (-85)
Cost mod: 2.0×
Total: 500 × 1.5 × 2.0 = 1,500 mana (DOUBLE cost!)
```

---

## Conjoined Powers (Permanent Unlocks)

### Power Acquisition

When a player reaches **Conjoined (+76 or higher)** with a god, they permanently unlock that god's domain:

```csharp
public struct ConjoinedPower : IComponentData
{
    /// <summary>
    /// Which god is conjoined with player
    /// </summary>
    public Entity GodEntity;

    /// <summary>
    /// Domain unlocked
    /// </summary>
    public GodDomain Domain;

    /// <summary>
    /// When conjoined status achieved
    /// </summary>
    public double ConjoinedTime;

    /// <summary>
    /// Whether passive bonuses are active
    /// </summary>
    public bool PassiveBonusesActive;
}

public enum GodDomain : byte
{
    Time = 0,
    Growth = 1,
    Wind = 2,
    Fire = 3,
    Water = 4,
    Fortune = 5,
    Earth = 6,
    Life = 7,
    Death = 8
}
```

### Conjoined Power Effects

**CHRONOS (Time) - Conjoined:**
```
Unlocked Powers:
├─ Time miracles: FREE (0 mana cost)
├─ Passive: Player can perceive future events (3 days ahead)
├─ Active: Rewind time (personal only, costs focus)
├─ Active: Age manipulation on demand
└─ Ultimate: "Temporal Freeze" - Pause time for entire map (1/day)
```

**VERDARA (Growth) - Conjoined:**
```
Unlocked Powers:
├─ Growth miracles: FREE
├─ Passive: All crops grow 50% faster in player villages
├─ Active: Instant forest summoning (no mana)
├─ Active: Plant-based healing (trees shed healing fruit)
└─ Ultimate: "Overgrowth" - Cover entire map in jungle (1/week)
```

**AEOLUS (Wind) - Conjoined:**
```
Unlocked Powers:
├─ Wind miracles: FREE
├─ Passive: Player villages have favorable winds (trade bonus)
├─ Active: Tornado summoning on demand
├─ Active: Flight grant (villagers can glide)
└─ Ultimate: "Hurricane Apocalypse" - Category 5 hurricane (1/month)
```

**PYROS (Fire) - Conjoined:**
```
Unlocked Powers:
├─ Fire miracles: FREE
├─ Passive: Player villages immune to fire damage
├─ Active: Volcanic eruption on demand
├─ Active: Permanent campfires (never extinguish)
└─ Ultimate: "Hellfire Cataclysm" - Burn half the map (1/month)
```

**HYDRIA (Water) - Conjoined:**
```
Unlocked Powers:
├─ Water miracles: FREE
├─ Passive: Player villages have perfect rainfall (crop bonus)
├─ Active: Flood summoning on demand
├─ Active: Water walking (villagers cross rivers)
└─ Ultimate: "Deluge" - Noah's Ark flood (1/month)
```

**FORTUNA (Fortune) - Conjoined:**
```
Unlocked Powers:
├─ Luck miracles: FREE
├─ Passive: Player villages have +30% production efficiency
├─ Active: Guaranteed critical success (next action always crits)
├─ Active: Probability manipulation (change event outcomes)
└─ Ultimate: "Chaos Cascade" - All random events this day favor player
```

**TERRA (Earth) - Conjoined:**
```
Unlocked Powers:
├─ Earth miracles: FREE
├─ Passive: Player villages built on unshakeable ground
├─ Active: Mountain summoning on demand
├─ Active: Mineral vein creation (resources appear)
└─ Ultimate: "Continental Shift" - Move tectonic plates (reshape map)
```

**VITALIS (Life) - Conjoined:**
```
Unlocked Powers:
├─ Life miracles: FREE
├─ Passive: Player villagers heal 2× faster
├─ Active: Resurrection (bring dead back to life, costs focus)
├─ Active: Immortality grant (1 villager becomes undying)
└─ Ultimate: "Genesis" - Create new life forms (1/year)
```

**MORTA (Death) - Conjoined:**
```
Unlocked Powers:
├─ Death miracles: FREE
├─ Passive: Player villages immune to disease
├─ Active: Instant death (kill any single target)
├─ Active: Necromancy (raise undead army, permanent)
└─ Ultimate: "Apocalypse" - Kill 50% of all living beings (1/year)
```

---

## Strategic Progression Paths

### Path 1: Life Cluster (Helpful God)

```
GOAL: Befriend life-affirming gods (Verdara, Vitalis, Hydria, Terra)

Strategy:
├─ Use helpful miracles (healing, growth, rain)
├─ Avoid destructive miracles (fire, death, decay)
├─ Prioritize villager well-being
└─ Focus on growth and prosperity

Pros:
├─ Villagers love you (high morale)
├─ High production (growth bonuses)
├─ Sustainable playstyle
└─ Allied gods synergize (Life Cluster)

Cons:
├─ Destruction gods become Nemesis (Pyros, Morta)
├─ Limited offensive miracles
└─ Vulnerable to enemy aggression

Conjoined Powers:
└─ "Garden of Eden" - Permanent perfect environment
    ├─ FREE growth, life, water, earth miracles
    ├─ +100% crop yield
    ├─ Villagers live 2× longer
    └─ Ultimate: "Paradise" - Transform region into utopia
```

### Path 2: Destruction Cluster (Wrathful God)

```
GOAL: Befriend destructive gods (Pyros, Morta, Aeolus)

Strategy:
├─ Use destructive miracles frequently
├─ Punish enemy villages harshly
├─ Embrace chaos and fire
└─ Show no mercy

Pros:
├─ Powerful offensive miracles
├─ Enemies fear you
├─ Fast military victories
└─ Allied gods synergize (Destruction Cluster)

Cons:
├─ Life gods become Nemesis (Verdara, Vitalis, Hydria)
├─ Player villagers have lower morale
└─ Sustainable growth difficult

Conjoined Powers:
└─ "Armageddon Arsenal" - Permanent destruction tools
    ├─ FREE fire, death, wind miracles
    ├─ Summon disasters on demand
    ├─ Villagers immune to player's destruction
    └─ Ultimate: "Ragnarok" - End the world (reset game)
```

### Path 3: Balance (Diplomatic God)

```
GOAL: Maintain neutral relations with ALL gods

Strategy:
├─ Use miracles sparingly
├─ Balance helpful and destructive actions
├─ Appease rival gods equally
└─ Focus on mana efficiency

Pros:
├─ No Nemesis gods (no interference)
├─ Flexible miracle access
├─ Good relations across pantheon
└─ Lower mana costs overall

Cons:
├─ Never reach Conjoined with anyone (no permanent powers)
├─ Slower progression
├─ No ultimate abilities
└─ Mediocre in all domains

Result:
└─ Jack-of-all-trades, master of none
    (Valid playstyle for balanced players)
```

### Path 4: Opposing Pair Mastery (Ultimate Power)

```
GOAL: Conjoin with RIVAL GODS (e.g., Pyros + Hydria)

Strategy:
├─ Alternate between rival domains
├─ Use fire miracles, then water miracles
├─ Accept one will become Nemesis initially
├─ Slowly repair relations with both
└─ Requires late-game resources and time

Difficulty: EXTREME (both gods resist, rivalry intensifies)

Reward: ULTIMATE SYNERGY POWERS

Example: Pyros + Hydria (Fire + Water)
└─ "Steam Mastery" - Combined domain unlock
    ├─ FREE fire and water miracles
    ├─ NEW miracles: Steam explosion, scalding rain, geyser
    ├─ Control temperature precisely
    ├─ Villagers thrive in extreme climates
    └─ Ultimate: "Thermodynamic Singularity" - Boil oceans or freeze sun
```

---

## God Interference & Sabotage

### Nemesis Status Effects

When a god reaches **Nemesis (-75 or worse)**, they actively oppose the player:

```csharp
public struct GodInterference : IComponentData
{
    public Entity InterferingGod;
    public InterferenceType Type;
    public float Severity;  // 0-1
    public double NextInterferencTime;
    public float FrequencyPerDay;  // How often god interferes
}

public enum InterferenceType : byte
{
    MiracleCostIncrease,     // +50% mana cost for domain miracles
    RandomDisaster,          // Spontaneous bad events (fire, flood, etc.)
    VillagerCurse,           // Villagers in player villages get debuffs
    MiracleFailure,          // Player miracles have chance to fail
    DivineSmite,             // Direct attack on player villages (rare)
    BlessingBlockade         // Player cannot use opposing god's miracles at all
}
```

**Example Interferences:**

**PYROS (Nemesis):**
```
Interference:
├─ Spontaneous wildfires (10% chance/day)
├─ Player fire miracles cost +50% mana
├─ Villagers suffer heat exhaustion (-20% work speed)
└─ Rare: "Divine Smite" - Meteor strike on player village
```

**CHRONOS (Nemesis):**
```
Interference:
├─ Time flows erratically (day/night cycles irregular)
├─ Villagers age 2× faster
├─ Player time miracles cost +50% mana
└─ Rare: "Temporal Prison" - Player frozen for 1 game day
```

**FORTUNA (Nemesis):**
```
Interference:
├─ Player villages have -30% production efficiency (bad luck)
├─ Critical failures more common (weapons break, crops fail)
├─ Player luck miracles cost +50% mana
└─ Rare: "Catastrophic Cascade" - Everything goes wrong for 1 day
```

---

## Reputation Events & Quests

### Divine Quests

Gods may offer **quests** to improve relations:

```csharp
public struct DivineQuest : IComponentData
{
    public Entity OfferingGod;
    public FixedString64Bytes QuestName;
    public QuestType Type;
    public float RelationReward;   // +10 to +50 depending on difficulty
    public float ManaCost;          // Some quests cost mana upfront
    public bool IsCompleted;
}

public enum QuestType : byte
{
    UseSpecificMiracle,    // "Cast rain 10 times"
    ProtectDomain,         // "Extinguish all fires for 3 days" (Hydria vs Pyros)
    EmpowerWorshippers,    // "Build temple to me"
    PunishRival,           // "Flood Pyros' sacred volcano"
    DemonstrateAlignment   // "Show you are Good/Evil/Lawful/Chaotic"
}
```

**Example Quests:**

**VERDARA (Growth):**
```
Quest: "Restore the Blighted Forest"
├─ Description: A forest has been burned by Pyros. Restore it.
├─ Task: Use "Forest Growth" miracle 5 times in blighted area
├─ Cost: 200 mana per cast × 5 = 1,000 mana total
├─ Reward: +30 relations with Verdara
└─ Side Effect: -15 relations with Pyros (rival punishment)
```

**CHRONOS (Time):**
```
Quest: "Restore Temporal Order"
├─ Description: Fortuna has caused chaos. Restore order.
├─ Task: Prevent all random events for 7 days
├─ Method: Use "Time Freeze" to lock RNG
├─ Cost: 500 mana (sustained miracle)
├─ Reward: +40 relations with Chronos
└─ Side Effect: -20 relations with Fortuna (rival oppression)
```

**PYROS (Fire):**
```
Quest: "Burn the Heretics"
├─ Description: A village worships Hydria. Burn them.
├─ Task: Destroy enemy village using only fire miracles
├─ Cost: 300 mana (firestorm)
├─ Reward: +25 relations with Pyros
└─ Side Effect: -50 relations with Hydria (direct attack on worshippers!)
```

---

## Integration with Existing Systems

### Miracle Framework Integration

```csharp
/// <summary>
/// Extended miracle component with god relation tracking
/// </summary>
public struct MiracleWithGodRelations : IComponentData
{
    // Existing miracle data
    public MiracleTypeId MiracleType;
    public float ManaCost;
    public float Intensity;

    // NEW: God relation tracking
    public Entity PrimaryGod;                      // Which god's domain
    public FixedList64Bytes<Entity> AffectedGods;  // Other gods impacted
    public float ExpectedRelationChange;           // Predicted impact

    // NEW: Interference resistance
    public float InterferenceResistance;  // 0-1, reduces god sabotage chance
}
```

### Reactions System Integration

Gods use the **Reactions and Relations System** we already documented:

```csharp
// Gods react to player miracles just like villagers react to events
foreach (var miracle in playerMiracles)
{
    foreach (var god in pantheon)
    {
        // Calculate god's reaction
        float reactionIntensity = CalculateGodReactionToMiracle(
            miracle,
            god,
            miracle.Intensity,
            god.Alignment);

        // Apply relation change
        UpdateGodRelation(god, reactionIntensity);

        // Check if god becomes Nemesis (trigger interference)
        if (GetGodRelation(god) < -75f)
        {
            TriggerGodInterference(god);
        }
    }
}
```

### Forces System Integration

Gods can use forces to interfere:

```
AEOLUS (Wind) - Nemesis Status:
├─ Applies "Strong Wind" force to player villages
│   ├─ Wind force: 5 m/s² westward
│   ├─ Affects: Villagers, buildings, projectiles
│   └─ Duration: Until player appeases Aeolus
│
└─ Result: Villagers struggle to work, arrows miss, buildings damaged
```

### Alignment System Integration

Gods have alignments that affect compatibility:

```
Player Alignment: Good (+60), Lawful (+40), Pure (+50)

Compatible Gods:
├─ Vitalis: Good (+90), Lawful (+50), Pure (+90) → HIGH SYNERGY
├─ Hydria: Good (+50), Lawful (+40), Pure (+70) → GOOD SYNERGY
└─ Verdara: Good (+70), Chaotic (+40), Pure (+80) → MIXED

Incompatible Gods:
├─ Pyros: Evil (-40), Chaotic (+60), Impure (-30) → CONFLICTS
├─ Morta: Evil (-60), Lawful (+70), Impure (-70) → HIGH CONFLICT
└─ Fortuna: Neutral (0), Chaotic (+100), Neutral (0) → OPPOSES ORDER

IMPLICATION:
Player's alignment affects starting relations and ease of befriending gods.
Lawful Good player has easier time with Vitalis, harder with Pyros.
```

---

## UI Integration (Tooltips)

### God Relation Tooltip

```
[TIER 1: Hover on god icon]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
PYROS - God of Fire
Chaotic Evil, Impure, Wrathful
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Relation: Nemesis (-85)
Status: ACTIVELY HOSTILE

Domain: Fire, heat, combustion
Rivals: Hydria (Water), Fulgor (Lightning)
Allies: Morta (Death), Aeolus (Wind)

Current Interference:
├─ Spontaneous wildfires (10%/day)
├─ Fire miracle cost: +50% mana
└─ Villagers: Heat exhaustion (-20% work)

Path to Conjoined: (+171 needed)
├─ Current: -85
├─ Target: +76 (Conjoined)
├─ Requirement: +161 improvement
└─ Estimate: 80+ fire miracles, or quests

Recent Events:
├─ Player cast "Rain" (Hydria domain) → -10
├─ Player extinguished wildfire → -15
└─ Player destroyed fire temple → -30

Unlock at Conjoined:
├─ Fire miracles: FREE (0 mana)
├─ Passive: Villages immune to fire
├─ Active: Volcanic eruption on demand
└─ Ultimate: "Hellfire Cataclysm"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**[TIER 2: Hover on "Nemesis"]**

```
┌────────────────────────────┐
│ Nemesis Status             │
├────────────────────────────┤
│ This god ACTIVELY OPPOSES  │
│ you and interferes with    │
│ your actions.              │
│                            │
│ Relation: -75 to -100      │
│                            │
│ Effects:                   │
│ • Domain miracles cost 2×  │
│ • Random disasters (10%/day│
│ • Villager curses          │
│ • Rare divine smites       │
│                            │
│ To escape Nemesis:         │
│ • Stop using opposing      │
│   god's miracles           │
│ • Complete divine quests   │
│ • Build temples            │
│ • Sacrifice resources      │
│                            │
│ WARNING: Some gods stay    │
│ Nemesis if you befriend    │
│ their rivals!              │
└────────────────────────────┘
```

---

## Summary

The **Rival Gods Diplomacy System** transforms miracle usage from simple mana expenditure into **strategic divine politics**. Every miracle affects god relations, every god has competing interests, and permanent power unlocks reward diplomatic mastery.

**Key Features:**

✅ **Nature has agency** - Gods control natural phenomena
✅ **Strategic choices** - Which gods to befriend matters
✅ **Competing interests** - Fire vs Water, Life vs Death, Order vs Chaos
✅ **Dynamic costs** - Relations affect mana costs (0× to 2×)
✅ **Permanent rewards** - Conjoined status = FREE miracles forever
✅ **Active opposition** - Nemesis gods sabotage player
✅ **Ultimate powers** - Conjoin rival gods for synergy abilities
✅ **Integrates everything** - Uses reactions, relations, forces, alignment

**Game Impact:**

**Early Game:**
- All gods Interloper (-50)
- Miracles cost baseline mana
- Choose strategic path (Life vs Destruction vs Balance)

**Mid Game:**
- Some gods Allied (+60), some Suspicious (-30)
- Mana costs vary wildly (0.4× to 1.2×)
- Nemesis gods start interfering
- Divine quests become available

**Late Game:**
- First Conjoined god (+80) → FREE miracles in that domain
- Nemesis gods require appeasement or acceptance
- Push for opposing pair (Fire + Water) → Ultimate synergy
- Shape pantheon relations permanently

**Result:** Miracles become **diplomatic tools** as much as **mechanical powers**. Players must navigate divine politics, manage competing loyalties, and earn the right to godhood through **strategic relationship building**. 🔥💧⚡🌿

---

**Related Documentation:**
- [Hierarchical_Tooltip_System.md](Hierarchical_Tooltip_System.md) - God relation tooltips
- [Reactions_And_Relations_System.md](Reactions_And_Relations_System.md) - God reaction mechanics
- [General_Forces_System.md](General_Forces_System.md) - Divine interference via forces
- [MiracleFramework.md](../../Mechanics/MiracleFramework.md) - Base miracle system

---

**Last Updated:** 2025-12-18
**Status:** Design Document - Divine Diplomacy
**Game:** Godgame
**Meta-Layer:** Nature as rival gods
**Diplomatic Depth:** DIVINE! ⚡👑✨
