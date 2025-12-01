# Village Response Strategies to Radicals

This document details how villages can respond to radical threats, with trade-offs and outcomes for each approach.

## Response Policies

### 1. Tolerance (Permissive)

**Philosophy**: "Dissent is healthy. Let them speak their minds."

```
Policy Configuration:
  CurrentPolicy: Tolerance
  AllowsExile: false
  AllowsExecution: false
  ToleratesProtest: true
  AttemptsReform: false
  CounterRadicalBudget: 0
```

**Effects**:
- ✅ Preserves civil liberties
- ✅ Low enforcement cost
- ✅ No martyrdom creation
- ✅ Some radicals de-radicalize naturally over time
- ❌ Cells grow unchecked
- ❌ Operations increase in frequency
- ❌ Public may lose confidence in government
- ❌ Can escalate to revolution if not monitored

**Best Used When**:
- Grievances are minor and localized
- Village has strong legitimacy
- Economy is stable
- Radical cells are small and non-violent

**Example Outcome**:
```
Village: Freedomhold
Radicals: 5 cells, 47 members total
Activity: Propaganda + peaceful protests
Response: Tolerance

Result after 6 months:
  → Cells grew to 8 cells, 89 members
  → No violence occurred
  → 15 members de-radicalized naturally
  → 3 cells split due to ideological differences
  → Village stability: -10% (minor decline)
  → Public opinion: Mixed (50% approve tolerance, 50% want action)
```

---

### 2. Surveillance (Passive Monitoring)

**Philosophy**: "Know your enemy before acting."

```
Policy Configuration:
  CurrentPolicy: Surveillance
  AllowsExile: false
  AllowsExecution: false
  ToleratesProtest: true
  AttemptsReform: false
  CounterRadicalBudget: 500 gold/month
  CounterRadicalEffectiveness: 60
```

**Effects**:
- ✅ Early detection of operations
- ✅ Intelligence gathering (know who is involved)
- ✅ Can prevent worst attacks
- ✅ Doesn't create martyrs
- ❌ Expensive (pays for spies, informants)
- ❌ Privacy violations anger citizens
- ❌ Cells can detect surveillance and go deeper underground
- ❌ Doesn't address root grievances

**Surveillance Methods**:
1. **Informant Networks**: Pay citizens to report on neighbors
   - Cost: 10-50 gold/informant/month
   - Effectiveness: 40-70% (depends on loyalty)
   - Risk: Informants can be exposed, creating grievances

2. **Magical Scrying**: Use divination magic to observe cells
   - Cost: 200-500 mana/day
   - Effectiveness: 70-90%
   - Risk: Anti-magic radicals see this as oppression

3. **Guard Patrols**: Increase guard presence in radical areas
   - Cost: 100 gold/guard/month
   - Effectiveness: 30-50%
   - Risk: Guards can be corrupted or attacked

**Best Used When**:
- Cells are forming but not yet violent
- Village has resources for intelligence operations
- Leadership wants to avoid overreach

**Example Outcome**:
```
Village: Watchful Vale
Radicals: 3 cells, 28 members
Activity: Sabotage planning detected via scrying
Response: Surveillance + preemptive guard deployment

Result:
  → Sabotage operation foiled before execution
  → 2 cell members arrested (evidence gathered)
  → Cell leader fled to nearby village
  → Remaining cells became more cautious (Secrecy: 70 → 85)
  → Village stability: -5%
  → Cost: 1,500 gold over 3 months
  → Public opinion: 60% approve (feel safer)
```

---

### 3. Infiltration (Active Disruption)

**Philosophy**: "Destroy them from within."

```
Policy Configuration:
  CurrentPolicy: Infiltration
  AllowsExile: true (for caught infiltrators)
  AllowsExecution: false
  ToleratesProtest: true
  AttemptsReform: false
  CounterRadicalBudget: 1,000 gold/month
  CounterRadicalEffectiveness: 75
```

**Effects**:
- ✅ Can disrupt operations before they happen
- ✅ Sows distrust within cells
- ✅ Gathers actionable intelligence
- ✅ Can turn radicals into double agents
- ❌ Very risky (infiltrators can be discovered and killed)
- ❌ Expensive (training, support, extraction)
- ❌ Ethical concerns (entrapment)
- ❌ If exposed, massive martyrdom effect

**Infiltration Methods**:
1. **Fake Recruits**: Send agents posing as sympathizers
   - Success Rate: 50-70%
   - Discovery Risk: Medium
   - Impact: Can sabotage operations

2. **Turned Members**: Flip captured radicals with deals
   - Success Rate: 30-60% (depends on commitment level)
   - Discovery Risk: High if radical is tested
   - Impact: Inside knowledge of leadership

3. **Provocateurs**: Agents who encourage cells to act prematurely
   - Success Rate: 60-80%
   - Discovery Risk: Low
   - Impact: Cells make mistakes, get caught

**Best Used When**:
- Cells are planning major attacks
- Intelligence is critical
- Village has skilled spies
- Willing to take ethical gray area

**Example Outcome**:
```
Village: Shadowmark
Radicals: "The Iron Fist" cell, 15 members
Activity: Planning riot + arson
Response: Infiltration - agent "Marcus" joins cell

Timeline:
  Month 1: Marcus gains trust (low-level member)
  Month 2: Marcus promoted to combatant role
  Month 3: Marcus learns of arson plan
  Month 4: Marcus tips off guards, raid arrests 12 members

Result:
  → Cell destroyed
  → 3 members escaped (including 1 leader)
  → Remaining cells paranoid, distrust new recruits
  → Martyrdom effect: +20% public support for radicals
  → Village stability: +15% (threat neutralized)
  → 2 new cells formed (splinters from paranoia)
```

---

### 4. Suppression (Direct Force)

**Philosophy**: "Crush the radicals before they grow."

```
Policy Configuration:
  CurrentPolicy: Suppression
  AllowsExile: true
  AllowsExecution: true
  AllowsTorture: false
  ToleratesProtest: false
  AttemptsReform: false
  CounterRadicalBudget: 2,000 gold/month
```

**Effects**:
- ✅ Removes immediate threats
- ✅ Demonstrates government strength
- ✅ Deters fence-sitters
- ❌ Creates martyrs
- ❌ Increases grievances
- ❌ Can radicalize moderates
- ❌ Cells go underground, harder to track

**Suppression Actions**:
1. **Mass Arrests**: Round up suspected radicals
   - Effectiveness: 60-80% (catch many, miss some)
   - Side Effect: +10 grievance to arrested families
   - Side Effect: False arrests radicalize innocents

2. **Exile**: Banish radicals from village
   - Effectiveness: 100% (they're gone)
   - Side Effect: Exiles form new cells elsewhere
   - Side Effect: Families left behind radicalize

3. **Execution**: Kill cell leaders
   - Effectiveness: 100% (permanent)
   - Side Effect: +30 grievance to sympathizers
   - Side Effect: Creates legendary martyrs
   - Side Effect: Successor leaders often more extreme

**Best Used When**:
- Cells are already violent
- Government legitimacy is strong
- Quick action needed to prevent coup
- Willing to accept martyrdom cost

**Example Outcome (Exile)**:
```
Village: Ironhold
Radicals: "Shadow Liberation" cell, 22 members
Activity: Sabotaged 3 farms, killed 1 guard
Response: Suppression - mass arrests + exile

Result:
  → 18 members arrested
  → 15 exiled to wilderness
  → 3 executed (leaders)
  → 4 escaped
  → Cell destroyed in village
  → Families gained +15 grievance
  → 8 new radicals created from families
  → Exiles formed new cell in neighboring village
  → Village stability: +20% short-term, -10% long-term
```

---

### 5. Reform (Address Root Causes)

**Philosophy**: "Fix the problems, and the radicals will disappear."

```
Policy Configuration:
  CurrentPolicy: Reform
  AllowsExile: false
  AllowsExecution: false
  ToleratesProtest: true
  AttemptsReform: true
  CounterRadicalBudget: 3,000 gold/month (invested in reforms)
```

**Effects**:
- ✅ Addresses root grievances
- ✅ De-radicalizes moderates
- ✅ Increases government legitimacy
- ✅ Long-term stability
- ❌ Expensive
- ❌ Slow (takes months to see results)
- ❌ Doesn't work on extremists
- ❌ Can be seen as weakness by hardliners

**Reform Actions**:
1. **Economic Reforms**:
   - Lower taxes
   - Create jobs
   - Redistribute wealth
   - Food assistance

   Effect: Reduces Poverty, Unemployment, Hunger grievances

2. **Political Reforms**:
   - Elections/councils
   - Transparency laws
   - Anti-corruption measures
   - Freedom of speech

   Effect: Reduces Authoritarianism, Corruption, LackOfVoice grievances

3. **Social Reforms**:
   - End discrimination
   - Cultural recognition
   - Equal rights laws

   Effect: Reduces Discrimination, Marginalization, CulturalSuppression grievances

4. **Justice Reforms**:
   - Fair trials
   - Amnesty programs
   - Prison improvements

   Effect: Reduces Injustice, Humiliation grievances

**Best Used When**:
- Grievances are systemic, not individual
- Village has resources for reform
- Leadership is willing to change
- Long-term stability is the goal

**Example Outcome**:
```
Village: New Hope
Radicals: 4 cells, 67 members total
Primary Grievances: Poverty (40%), Taxation (35%), Inequality (25%)
Response: Reform

Reforms Implemented:
  → Tax rate reduced from 50% to 30%
  → Public works program creates 50 new jobs
  → Food assistance for lowest 20% income
  → Elected village council formed

Timeline:
  Month 1-2: Skepticism, no change in radicalization
  Month 3: First job placements, -5 grievance average
  Month 4-5: Food aid delivered, -10 grievance average
  Month 6: Council elected, -15 grievance average
  Month 8: Tax relief felt, -25 grievance average

Result after 8 months:
  → 45 members de-radicalized (dropped below threshold)
  → 2 cells dissolved (insufficient members)
  → 22 hardcore members remain (extremists)
  → Village stability: +35%
  → Cost: 24,000 gold total
  → Public opinion: 80% approve
```

---

### 6. Negotiation (Compromise)

**Philosophy**: "Talk to them, find common ground."

```
Policy Configuration:
  CurrentPolicy: Negotiation
  AllowsExile: false
  AllowsExecution: false
  ToleratesProtest: true
  AttemptsReform: true (selective, based on demands)
  CounterRadicalBudget: 500 gold/month (mediation costs)
```

**Effects**:
- ✅ Peaceful resolution possible
- ✅ Gives radicals a voice
- ✅ Can split cells (moderates leave)
- ✅ Demonstrates government flexibility
- ❌ Legitimizes radical demands
- ❌ Can embolden extremists
- ❌ Hardliners may see as weakness
- ❌ Negotiations can fail spectacularly

**Negotiation Outcomes**:
1. **Success** (30% chance):
   - Radicals get partial demands
   - Cell disbands or becomes legal group
   - Stability increases

2. **Partial Success** (40% chance):
   - Moderates accept deal
   - Extremists reject, split off
   - Stability neutral

3. **Failure** (30% chance):
   - Radicals reject all offers
   - See government as weak
   - Escalate violence
   - Stability decreases

**Best Used When**:
- Radical demands are reasonable
- Cell has moderate members
- Violence has not yet escalated
- Government has flexibility to compromise

**Example Outcome (Success)**:
```
Village: Bargainford
Radicals: "Workers Union" cell, 34 members
Ideology: WorkersRights, AntiTax
Primary Grievances: Exploitation, Taxation
Response: Negotiation

Demands:
  1. Reduce work hours from 12/day to 10/day
  2. Increase minimum wage by 20%
  3. Right to form unions
  4. Lower taxes on poorest 30%

Government Counter-Offer:
  1. Reduce to 11 hours/day (phased over 6 months)
  2. Increase wage by 10%
  3. Allow unions, but must register
  4. Tax exemption for poorest 20%

Result:
  → Moderates (24 members) accept deal
  → Cell becomes legal union
  → Extremists (10 members) reject, splinter off
  → Village stability: +10%
  → Economic cost: 1,500 gold/month (wage increase)
  → Public opinion: 70% approve (workers happy, merchants concerned)
```

---

### 7. Crackdown (Martial Law)

**Philosophy**: "Eliminate the threat at any cost."

```
Policy Configuration:
  CurrentPolicy: Crackdown
  AllowsExile: true
  AllowsExecution: true
  AllowsTorture: true
  ToleratesProtest: false
  AttemptsReform: false
  CounterRadicalBudget: 5,000 gold/month
  CounterRadicalEffectiveness: 95
```

**Effects**:
- ✅ Maximum short-term effectiveness
- ✅ Total control over population
- ✅ Cells destroyed or driven deep underground
- ❌ Extreme grievance creation
- ❌ Mass radicalization of moderates
- ❌ Long-term instability
- ❌ Can trigger civil war
- ❌ Loss of legitimacy

**Crackdown Measures**:
1. **Curfews**: Restrict movement
2. **Mass Surveillance**: Monitor all citizens
3. **Collective Punishment**: Punish families of radicals
4. **Secret Police**: Arbitrary arrests
5. **Torture**: Extract intelligence
6. **Public Executions**: Maximum deterrent + martyrdom

**Best Used When**:
- Village is on brink of collapse
- Cells have massive support
- Revolution is imminent
- Last resort only

**Example Outcome**:
```
Village: Steelhold
Radicals: 12 cells, 340 members (30% of population!)
Activity: Attempted coup, killed village elder
Response: Crackdown (martial law declared)

Measures:
  → All gatherings banned
  → House-to-house searches
  → 87 arrests in first week
  → 15 public executions
  → Torture used to find cell leaders
  → 200 citizens exiled as "sympathizers"

Result after 3 months:
  → All 12 cells destroyed or driven underground
  → 95% of known radicals captured/killed/exiled
  → Village stability: +50% (immediate)
  → Entire population gained +40 grievance (Authoritarianism)
  → 120 new radicals created (from crackdown victims' families)
  → 5 new cells formed in secret (Secrecy: 95)
  → Long-term: Village becomes police state, constant repression needed
```

---

## Choosing the Right Response

### Decision Matrix

| Grievance Level | Cell Size | Violence | Best Response |
|----------------|-----------|----------|---------------|
| Low (0-30) | Small | None | Tolerance or Reform |
| Medium (31-60) | Medium | Minor | Surveillance or Negotiation |
| High (61-80) | Large | Significant | Reform + Suppression |
| Extreme (81-100) | Massive | Widespread | Negotiation or Crackdown |

### Response Escalation Path

```
Tolerance
  ↓ (if cells grow)
Surveillance
  ↓ (if operations increase)
Infiltration
  ↓ (if violence starts)
Suppression
  ↓ (if violence spreads)
Reform + Suppression (combined)
  ↓ (if civil war imminent)
Crackdown (last resort)
```

### Combining Responses

**Most Effective**: Reform + Suppression
- Reform addresses grievances (long-term)
- Suppression removes violent extremists (short-term)
- Balanced approach

**Example**:
```
Village: Balanced Vale
Radicals: 6 cells, mix of moderates and extremists
Response: Combined Reform + Suppression

Actions:
  → Economic reforms (jobs, tax relief) for moderates
  → Targeted arrests of violent cell leaders only
  → Negotiation with moderate cells
  → Public transparency about reforms

Result:
  → Moderates de-radicalize (reforms work)
  → Extremists isolated (lose public support)
  → Suppression has legitimacy (only targeting violent)
  → Village stability: +40%
  → Cost: High but sustainable
```

---

## Summary

| Policy | Cost | Speed | Grievance Impact | Best For |
|--------|------|-------|------------------|----------|
| **Tolerance** | Low | N/A | Neutral | Minor threats |
| **Surveillance** | Medium | Slow | Slightly negative | Intelligence gathering |
| **Infiltration** | High | Medium | Negative if exposed | Major threats |
| **Suppression** | High | Fast | Very negative | Active violence |
| **Reform** | Very High | Very Slow | Very positive | Root causes |
| **Negotiation** | Low | Medium | Positive or negative | Reasonable demands |
| **Crackdown** | Extreme | Very Fast | Extremely negative | Last resort |

**Golden Rule**: Address grievances when possible, suppress violence when necessary, never use crackdown unless facing collapse.
