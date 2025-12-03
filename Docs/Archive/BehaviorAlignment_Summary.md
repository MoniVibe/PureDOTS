# Entity Behaviors, Alignments, Outlooks & Aggregate Dynamics - Summary

**Last Updated**: 2025-11-29
**Source**: [Dual Leadership Pattern](../Packages/com.moni.puredots/Documentation/DesignNotes/DualLeadershipPattern.md)

This document summarizes how entities behave, how their moral/ideological positions work, and how aggregates (bands, guilds, fleets) form and function in PureDOTS.

---

## Core Behavioral Systems

### 1. Alignment (Moral/Ideological Position)

**Tri-axis system** with values from -100 to +100:

```csharp
public struct VillagerAlignment : IComponentData
{
    public sbyte MoralAxis;   // -100 (Evil) ↔ +100 (Good)
    public sbyte OrderAxis;   // -100 (Chaotic) ↔ +100 (Lawful)
    public sbyte PurityAxis;  // -100 (Corrupt) ↔ +100 (Pure)
    public float AlignmentStrength;  // 0-1, conviction level
}
```

**What it represents**: WHAT they believe morally/ideologically

**Aggregate alignment**: Weighted average of member alignments (higher Weight = more influence)

**Space4X variant**: Continuous normalized values (-1 to +1) for Law, Good, Integrity

### 2. Outlooks (Behavioral Perspectives)

**Distinct from alignment** - represents philosophical approaches:

- Derived from alignment combinations
- Up to **3 primary outlooks** per entity/aggregate
- Examples:
  - **Heroic/Righteous**: Moral + Lawful
  - **Ruthless**: Evil
  - **Scholarly/Methodical**: Orderly
  - **Pragmatic**: Neutral
  - **Rebellious**: Chaotic
  - **Devout**: Pure

**Types**:
```csharp
public enum Outlook : byte
{
    Neutral = 0,
    Loyalist = 1,      // Strongly aligned with faction
    Opportunist = 2,   // Pursues self-interest
    Fanatic = 3,       // Zealously committed
    Mutinous = 4       // Opposes authority
}
```

**Space4X ethical axes** (distinct from alignment):
- War, Materialist, Authoritarian, Xenophobia, Expansionist

### 3. Personality Traits (VillagerBehavior)

**Independent of alignment** - defines emotional responses:

```csharp
public struct VillagerBehavior : IComponentData
{
    public sbyte VengefulScore;      // -100 (forgiving) ↔ +100 (vengeful)
    public sbyte BoldScore;          // -100 (craven) ↔ +100 (bold)
    public float InitiativeModifier;
    public byte ActiveGrudgeCount;
    public uint LastMajorActionTick;
}
```

**Key distinction**:
| Aspect | Behavior | Alignment |
|--------|----------|-----------|
| **What** | HOW they respond emotionally | WHAT they believe morally |
| **Example** | "Vengeful person holds grudges" | "Good person opposes killing" |
| **Change** | Slow (trait-based) | Moderate (can shift with aggregate) |

**Aggregate behavior**: Majority vote + cohesion modulation
- If >50% vengeful → aggregate vengeful
- Low cohesion → pushed toward neutral
- High cohesion → held at majority

---

## Aggregates: Formation & Structure

### Types

```csharp
public enum AggregateCategory : byte
{
    Village,   // Settlement
    Band,      // Combat/work group (2+ members)
    Guild,     // Professional association (5+ members)
    Dynasty,   // Family lineage
    Colony,    // Space settlement
    Fleet      // Spacefaring force
}
```

### Formation Mechanics

#### Band Formation (Combat/Work Groups)

**Requirements**:
- 2+ entities within **10m proximity**
- **Compatible goals** (fight with fight, work with work)

**Formation probability**:
```
baseProbability = 10%
+ memberBonus (5% per member, cap 30%)
+ desperationBonus (5% per desperate member, cap 20%)
+ alignmentBonus (2% per aligned member, cap 15%)
```

**Result**:
- Band name based on purpose (Defenders, Hunters, Miners)
- Alignment = weighted average of members
- Leadership elected by: Wisdom + Mood + AlignmentStrength

#### Guild Formation (Professional Associations)

**Requirements**:
- **5+ founders minimum**
- Within **25m proximity**
- **Alignment tolerance ±40** on all axes
- Compatible goals

**Guild type** (from aggregate alignment):
- **Heroes**: Moral >50, Order >30
- **Assassins**: Moral <-50, Order <-30
- **Holy Order**: Moral >50, Purity >50
- **Scholars**: Order >50, neutral moral
- **Rebels**: Order <-50, Moral >30
- **Merchants**: Default

**Governance type** (from order axis):
- **Meritocratic**: Order >50
- **Democratic**: Moral >50
- **Authoritarian**: Order <-50
- **Oligarchic**: Default

### Core Components

```csharp
public struct AggregateEntity : IComponentData
{
    public AggregateCategory Category;
    public Entity Owner;
    public Entity Parent;
    public float Wealth, Reputation, Cohesion, Morale, Stress;
    public int MemberCount;
}

public struct AggregateMember : IBufferElementData
{
    public Entity Member;
    public float Weight;  // Influence weighting
}
```

### Cohesion System

**Cohesion** (0-1) = internal unity:

```
cohesion =
    alignmentVariance * 0.3 +    // Similar alignments = +0.3
    loyaltyAverage * 0.3 +       // Loyal members = +0.3
    leadershipStrength * 0.2 +   // Strong leader = +0.2
    -dissent * 0.4               // Each dissenter = -0.4 * ratio
```

**Effects**:
| Cohesion | Consensus | Task Speed | Notes |
|----------|-----------|------------|-------|
| High (>0.8) | Unanimous | +20% | Strong unity |
| Medium (0.4-0.8) | Majority | Normal | Functional |
| Low (<0.4) | Anarchy | -40% | Splintering risk |

---

## Decision-Making & AI

### Initiative System

Controls when entities act autonomously:

```csharp
baseInitiative =
    0.4 +
    morale * 0.2 +
    cohesion * 0.2 +
    (members / 100) * 0.1 +
    -stress * 0.15

boldModifier = BoldScore * 0.0015
grudgeBoost = ActiveGrudgeCount * 0.02
alignmentModifier =
    chaotic ? +0.05 :
    lawful ? -0.05 :
    pure ? +0.02 :
    corrupt ? -0.02 : 1.0

finalInitiative = clamp(
    (baseInitiative + boldModifier + grudgeBoost) * alignmentModifier,
    0, 1
)
```

**High initiative** = frequent autonomous actions
**Low initiative** = passive, reactive

### Moral Conflict System

When orders conflict with alignment/behavior, entities **hesitate**:

| Conflict Level | Delay | Morale Hit | Example |
|----------------|-------|------------|---------|
| Minor (0.2-0.4) | 10 ticks | -5 | Bend rules slightly |
| Moderate (0.4-0.6) | 50 ticks | -15 | Attack neutrals |
| Major (0.6-0.8) | 150 ticks | -30 | Execute prisoners |
| Severe (0.8+) | 300 ticks | -50 | May refuse if low loyalty |

**Calculation**:
```csharp
// Example: AttackDefenseless
conflictLevel = (goodness + forgiveness) / 2

// Example: ExecutePrisoners
conflictLevel = lawfulness * 0.6 + goodness * 0.4
```

**Cohesion impact**: Each conflicted member = -0.05 cohesion; leadership conflict = -0.15

### Consensus Voting

Aggregates make decisions via member voting:

| Governance | Decision Process | Leader Influence |
|------------|------------------|------------------|
| **Democratic** | Leader proposes, members vote | 30% |
| **Authoritarian** | Leader decides | 90% |
| **Oligarchic** | Council votes | 60% |
| **Anarchy** | No consensus | 0% |

Vote weights based on:
- Alignment fit to decision type
- Behavior fit (bold vs craven)
- Loyalty to aggregate

---

## Social Dynamics

### Grudge System

```csharp
public struct EntityGrudge : IBufferElementData
{
    public Entity OffenderEntity;
    public GrudgeType Type;          // Personal, Professional, Factional
    public byte Intensity;           // 0-100
    public GrudgeSeverity Severity;  // Forgotten, Minor, Moderate, Serious, Vendetta
    public uint OriginTick;
    public uint LastRenewedTick;
    public bool IsInherited;         // Passed from family/faction
    public bool IsPublic;            // Affects reputation
}
```

**Grudge Types**:
- **Personal (1-9)**: Insult, Theft, Assault, Betrayal, Murder, Humiliation
- **Professional (10-19)**: Demotion, Sabotage, Credit Stolen, Exploitation
- **Factional (20-29)**: War Crime, Territory Loss, Economic Harm, Genocide

**Decay rates**:
- **Vengeful** (score -70): DecayRate = 0.3/day
- **Forgiving** (score +60): DecayRate = 2.0/day
- **Vendetta threshold**: Intensity >75 (permanent until resolved)
- **Action threshold**: Intensity >50 triggers hostile action

**Inheritance**:
- Families inherit parent grudges at scaled intensity
- Factions consolidate member grudges into collective memory

### Loyalty & Belonging

Members track up to **5 aggregate memberships** ranked by loyalty:

```csharp
public struct VillagerBelonging : IBufferElementData
{
    public Entity AggregateEntity;
    public VillagerAggregateKind Kind;  // Family, Village, Band, Guild
    public short Loyalty;               // -200 to +200
    public ushort InfluenceOrder;       // Ranking by importance
}
```

**Loyalty shifts**:
- High loyalty + alignment match → follow orders
- Low loyalty + conflict → dissent/desertion
- Based on treatment, success, moral alignment

---

## Splintering & Merging

### Splintering (Fracturing)

**Conditions**:
- Cohesion <0.2 for 100+ ticks
- Dissent >50% members
- Multiple competing leaders
- Alignment conflict (factions too different)

**Process**:
1. Breakaway leader takes aligned members
2. Remaining entity shrinks but survives
3. Both recalculate alignment/cohesion

**Example**: "High Steward leads reformers to found gentler sect after refusing Prophet's sacrifice order"

### Merging (Consolidation)

**Conditions**:
- Nearby aggregates with >0.9 alignment similarity
- Compatible governance types

**Process**:
1. Larger aggregate absorbs smaller
2. Alignment recalculated (weighted average)
3. Members gain dual loyalty temporarily

---

## Practical Examples

### Example 1: Guild Formation from Compatible Founders

**Setup**: 5 entities within 25m, alignments:
- Entity A: Moral 60, Order 40, Purity 20
- Entity B: Moral 55, Order 35, Purity 15
- Entity C: Moral 70, Order 45, Purity 25
- Entity D: Moral 50, Order 30, Purity 10
- Entity E: Moral 65, Order 50, Purity 30

**Process**:
1. Alignment averaging: Moral 60, Order 40, Purity 20
2. Guild type: **Heroes** (Moral >50, Order >30)
3. Governance: **Meritocratic** (Order >30)
4. Outlooks: [Heroic, Righteous, Scholarly]
5. Initial cohesion: 0.85 (tight alignment variance)
6. Guild master: Entity with highest Wisdom + Mood + AlignmentStrength

### Example 2: Moral Conflict → Dissent

**Setup**: Good officer (Moral +60, Forgiving 0.2) ordered to attack defenseless refugees

**Process**:
1. Conflict = (goodness + forgiveness) / 2 = (0.8 + 0.8) / 2 = 0.8 (Major)
2. Effects:
   - 150 tick hesitation
   - -30 morale
   - Loyalty to commander -10
   - Aggregate cohesion -0.05
3. If repeated: Loyalty <50 → Officer refuses → Task fails

### Example 3: Band Initiative Calculation

**Setup**:
- Morale: 45
- Cohesion: 0.6
- Members: 8
- Stress: 0.2
- Average BoldScore: -20
- Active grudges: 2 members

**Calculation**:
```
baseInitiative = 0.4 + 0.45*0.2 + 0.6*0.2 + 0.08*0.1 - 0.2*0.15
               = 0.4 + 0.09 + 0.12 + 0.008 - 0.03
               = 0.588

boldModifier = -20 * 0.0015 = -0.03
grudgeBoost = 2 * 0.02 = 0.04
alignmentModifier = 1.0 (neutral)

finalInitiative = clamp((0.588 - 0.03 + 0.04) * 1.0)
                = 0.598
```

**Result**:
- Interval = 30 days / (sqrt(8) * scale) ≈ 3 days between major actions
- PendingAction = utility evaluation decides (seek_conflict, seek_resources, etc.)

---

## Key Architectural Patterns

### Data-Oriented Design

- **Components**: Pure data, no behavior
- **Systems**: Operate on component queries (Burst-compiled)
- **Buffers**: Dynamic collections (members, relationships, grudges)
- **BlobAssets**: Serialized reference data (catalogs, profiles)

### Rewindable State

All systems check `RewindState.Mode`:
- **Record**: Execute normally
- **Playback**: Skip (deterministic replay)
- **Pause**: Pause but don't reset

### Burst Compilation

Personality/alignment systems are `[BurstCompile]` for:
- Band/Guild formation (spatial queries)
- Cohesion calculations
- Initiative computation
- Voting system

---

## Integration with Dual Leadership Pattern

The [Dual Leadership Pattern](../Packages/com.moni.puredots/Documentation/DesignNotes/DualLeadershipPattern.md) leverages all these systems to create Captain/Shipmaster, Prophet/Steward dynamics:

- **Alignment distance** between leaders → CommandFriction
- **Moral conflict** when leaders disagree on orders
- **Grudges** accumulate between leaders
- **Cohesion** reduced by CommandFriction
- **Splintering** triggered by high friction + low cohesion

See [DualLeadershipPattern.md](../Packages/com.moni.puredots/Documentation/DesignNotes/DualLeadershipPattern.md) for details.

---

## Quick Reference Table

| Aspect | Individual | Aggregate |
|--------|-----------|-----------|
| **Alignment** | Tri-axis (-100 to +100) | Weighted average of members |
| **Behavior** | VengefulScore + BoldScore | Majority vote + cohesion modulation |
| **Outlook** | Top 3 perspectives | Top 3 from members |
| **Initiative** | Personal cadence | Scaled by member count |
| **Decision** | Autonomous AI evaluation | Consensus voting + leader influence |
| **Loyalty** | N/A | Per-member tracked (top 5) |
| **Cohesion** | N/A | Alignment variance + loyalty + leadership |
| **Grudges** | Personal, decay individually | Aggregated for faction memory |

---

## See Also

- [Dual Leadership Pattern](../Packages/com.moni.puredots/Documentation/DesignNotes/DualLeadershipPattern.md) - Symbolic/operational role dynamics
- [Faction & Guild System](../Packages/com.moni.puredots/Documentation/DesignNotes/FactionAndGuildSystem.md) - Organization framework
- [Guild Curriculum System](../Packages/com.moni.puredots/Documentation/DesignNotes/GuildCurriculumSystem.md) - Teaching and knowledge transmission
- [Villager Decision Making](../Packages/com.moni.puredots/Documentation/DesignNotes/VillagerDecisionMaking.md) - Comprehensive AI architecture

---

**This emergent system creates** rich social behavior without hardcoded "politics" logic:
- Entities with different outlooks clash
- Moral conflicts cause hesitation and dissent
- Grudges drive vendettas
- Aggregates splinter or merge based on alignment dynamics
