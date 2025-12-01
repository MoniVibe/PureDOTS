# Radical Aggregates System - Quick Reference

**Concept**: Disgruntled villagers form secret cells to undermine authority through riots, sabotage, and violence. Villages must balance suppression vs reform.

## Core Mechanics

### 1. Radicalization (Individual Level)

```csharp
RadicalizationState:
  GrievanceLevel: 0-100 (accumulates from events)
  RadicalizationThreshold: 40-80 (personality-dependent)
  Stage: Stable → Discontented → Agitated → Radicalized → Extremist
  IsRadical: Joined cell?
  Commitment: 0-100 (how hard to de-radicalize)
```

**Grievance Sources**:
- **Economic**: Poverty, Unemployment, Taxation, Inequality
- **Social**: Discrimination, Marginalization, No political voice
- **Authority**: Corruption, Injustice, Authoritarianism
- **Material**: Hunger, Homelessness, Illness
- **Psychological**: Humiliation, Betrayal, Despair

**Example**:
```
Villager loses job (-10 grievance)
  → Can't afford food (-15 grievance)
  → Publicly shamed for debt (-20 grievance)
  → Total: 45 grievance
  → Threshold: 40
  → Becomes Radicalized, joins cell
```

### 2. Radical Cells (Aggregate Level)

```csharp
RadicalCell:
  Type: Agitators, Saboteurs, Rioters, Revolutionaries, etc.
  Ideology: AntiTax, Religious, Anarchist, etc.
  MemberCount: 5-200
  SecrecyLevel: 0-100 (how hidden)
  AggressionLevel: 0-100 (how violent)
  PublicSupport: 0-100 (sympathizers)
```

**Cell Types**:
| Type | Activity | Violence |
|------|----------|----------|
| **Agitators** | Propaganda, recruitment | None |
| **Saboteurs** | Destroy infrastructure | Low |
| **Rioters** | Street violence | Medium |
| **Revolutionaries** | Overthrow government | High |
| **Terrorists** | Maximum fear/damage | Very High |

### 3. Operations (Actions)

```csharp
RadicalOperation:
  Type: Propaganda, Riot, Sabotage, Assassination, Strike, Arson, etc.
  ParticipantCount: How many cell members
  ExpectedImpact: 0-100
  DetectionRisk: 0-100
  Succeeded: Yes/No
  Detected: Yes/No
```

**Operation Examples**:
- **Propaganda**: Recruit sympathizers (low risk)
- **Strike**: Economic disruption (medium risk)
- **Riot**: Street violence (high risk)
- **Sabotage**: Destroy buildings/farms (high risk)
- **Assassination**: Kill leaders (very high risk)

### 4. Village Responses

```csharp
RadicalResponsePolicy:
  CurrentPolicy: Tolerance, Surveillance, Suppression, Reform, Crackdown
  AllowsExile: true/false
  AllowsExecution: true/false
  ToleratesProtest: true/false
  AttemptsReform: true/false
```

**Response Policies**:

| Policy | Effect | Cost | Martyrdom Risk |
|--------|--------|------|----------------|
| **Tolerance** | Cells grow unchecked | Low | None |
| **Surveillance** | Early detection | Medium | Low |
| **Infiltration** | Disrupt from within | High | Medium |
| **Suppression** | Remove threats | High | High |
| **Reform** | Address grievances | Very High | None |
| **Negotiation** | Peaceful resolution | Low | Variable |
| **Crackdown** | Total control | Extreme | Extreme |

## Example Scenario: Economic Radicals

### Background
```
Village: Ironforge
Issue: Wealthy nobles exploit miners
Miners work 14 hours/day for 2 gold/week
Mine collapse kills 8, no compensation
```

### Radicalization
```
Marcus (miner, lost brother):
  Grievances:
    - FamilyHarm: +25
    - Injustice: +15
    - Exploitation: +1.0/day
    - Inequality: +1.5/day

  Total: 78/100 grievance
  Threshold: 50
  → Radicalized!
```

### Cell Formation
```
Name: "The Red Fist"
Type: Rioters + Saboteurs
Ideology: AntiCapitalist + WorkersRights
Members: 18
Secrecy: 75
PublicSupport: 35%
```

### Operations
```
Month 1: Strike (legal, 0% detection risk)
  → Mine stops, nobles lose 5,000 gold
  → Nobles refuse safety improvements

Month 2: Sabotage noble warehouse
  → Burns down, 10,000 gold loss
  → 40% detection risk → caught

Month 3: Village responds with Suppression
  → 12 arrested, 3 executed, 9 exiled
  → Martyrdom effect: +20 grievance to all miners
  → Public Support: 35% → 55%
  → 22 new radicals created
```

### Outcome Paths

**Path A: Continued Suppression**
```
Result:
  → Cells driven underground
  → Violence escalates
  → Civil war risk
  → Long-term instability
```

**Path B: Reform**
```
Actions:
  → Improve mine safety
  → Raise wages
  → Arrest corrupt nobles
Result:
  → Moderates de-radicalize
  → Extremists isolated
  → Stability restored (6-12 months)
```

**Path C: Reform + Suppression (Best)**
```
Actions:
  → Reform: Safety + wages
  → Suppression: Only violent extremists
Result:
  → Moderates satisfied
  → Extremists removed
  → Stability + legitimacy
```

## Key Patterns

### Radicalization Stages

```
Stable (0-20 grievance)
  → Normal villager

Discontented (21-40)
  → Complains privately

Agitated (41-60)
  → Openly critical of authority

Radicalized (61-80)
  → Joins cell, willing to act

Extremist (81-100)
  → Willing to die for cause
```

### Cell Lifecycle

```
Formation
  ↓
Recruitment (propaganda)
  ↓
Testing (minor operations)
  ↓
Escalation (violence)
  ↓
Confrontation with authorities
  ↓
Resolution:
  - Destroyed (suppression)
  - Disbanded (reform/negotiation)
  - Success (overthrow/demands met)
  - Underground (driven into hiding)
```

### Martyrdom Cycle

```
Government suppresses radicals
  ↓
Creates martyrs (executions/torture)
  ↓
Sympathizers radicalize
  ↓
New cells form (larger than before)
  ↓
More suppression needed
  ↓
Spiral into civil war OR reform breaks cycle
```

## Integration with Existing Systems

### Mood System
```
if (radicalization.Stage >= Radicalized)
{
    mood.CurrentMood = Angry;
    mood.BaseHappiness -= 30;
}
```

### Guild System
```
// Guilds can counter radicalization
if (guildMembership.Rank >= 1)
{
    radicalization.GrievanceLevel -= 5; // Belonging reduces grievance
}

// OR entire guilds radicalize
if (guild.AverageGrievance > 60)
{
    ConvertGuildToRadicalCell(guildEntity);
}
```

### Village Stability
```
RadicalImpact:
  StabilityLoss: 0-100
  EconomicDamage: Gold value
  AuthorityDeaths: Count
  PublicSupport: 0-100

Village collapses when StabilityLoss > 80
```

## Example Ideologies

### Economic Radicals
```
Ideology: AntiCapitalist + WorkersRights
Grievances: Poverty, Exploitation, Inequality
Methods: Strikes, sabotage, occupation
Support: Workers, poor

Response: Reform (improve conditions) OR Suppression (violent cells)
```

### Religious Radicals
```
Ideology: Religious + Separatist
Grievances: ReligiousPersecution, CulturalSuppression
Methods: Underground worship, assassination, exodus
Support: Faithful

Response: Tolerance (allow worship) OR Crackdown (ban faith)
```

### Political Radicals
```
Ideology: Democratic + Revolutionary
Grievances: Authoritarianism, Corruption, Injustice
Methods: Demonstrations, occupation, overthrow
Support: Broad (if grievances legitimate)

Response: Reform (democracy) OR Suppression (maintain autocracy)
```

### Nihilist Radicals
```
Ideology: Nihilist + Anarchist
Grievances: Despair, none specific (just want destruction)
Methods: Random violence, maximum chaos
Support: None (fringe)

Response: Immediate Crackdown (no negotiation possible)
```

## Decision Guide for Players

### When to Tolerate
- Grievances are minor
- Cells are small and non-violent
- Village has legitimacy
- You value freedom

### When to Suppress
- Cells are violent
- Operations threaten stability
- You have strong military
- Quick action needed

### When to Reform
- Grievances are legitimate and systemic
- You have resources for change
- Long-term stability is goal
- Public supports change

### When to Negotiate
- Radical demands are reasonable
- Cells have moderate members
- Violence hasn't escalated yet
- Compromise is acceptable

### When to Crackdown (Last Resort)
- Village facing collapse
- Revolution imminent
- All other options failed
- Willing to accept long-term oppression

## Files Created

1. **[RadicalAggregatesSystem.md](RadicalAggregatesSystem.md)** - Full design specification
2. **[RadicalComponents.cs](../../Packages/com.moni.puredots/Runtime/Runtime/Aggregates/RadicalComponents.cs)** - Component definitions
3. **[RadicalResponseStrategies.md](RadicalResponseStrategies.md)** - Response policies in detail
4. **[RadicalMovementExamples.md](RadicalMovementExamples.md)** - 4 complete scenario examples

## Summary

✅ **Captures instability**: Villagers radicalize from real grievances
✅ **Emergent gameplay**: Player choices matter (suppress vs reform)
✅ **Political depth**: Ideologies, cells, operations, martyrdom
✅ **Consequences**: Every action has reactions
✅ **Integration**: Works with guilds, mood, economy, alignment

**Philosophy**: "Radicals are symptoms of deeper problems. Treat the disease (grievances) or fight the symptoms (cells) - but fighting symptoms creates more disease."
