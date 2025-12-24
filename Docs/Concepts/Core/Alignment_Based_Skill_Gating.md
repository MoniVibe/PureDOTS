# Alignment & Outlook-Based Skill Gating

## Overview

**Skills and passives are soft-gated behind logical alignments and outlooks** - entities can access any skill/passive, but matching alignment/outlook reduces cost and difficulty, while mismatched alignments increase cost.

**Core Concept:**
- **Soft Gating:** No hard blocks - all skills/passives are accessible
- **Cost Modulation:** Alignment/outlook match affects unlock cost and difficulty
- **Archetype Affinity:** Certain archetypes pay less for flavor-appropriate skills
- **Vibes, Not Rules:** Even the most moral character can learn cruel talents (at higher cost)

---

## Skill Alignment Affinity System

### Alignment Matching

**Skills and passives have alignment affinities** that determine how well they match an entity's alignment profile.

**Alignment Components:**
- **Moral Axis:** Good (+100) ↔ Evil (-100)
- **Order Axis:** Lawful (+100) ↔ Chaotic (-100)
- **Purity Axis:** Pure (+100) ↔ Corrupt (-100)

**Skill Affinity Definition:**
```
SkillAlignmentAffinity = {
    MoralAffinity: -100 to +100 (negative = evil-aligned, positive = good-aligned, 0 = neutral)
    OrderAffinity: -100 to +100 (negative = chaotic-aligned, positive = lawful-aligned, 0 = neutral)
    PurityAffinity: -100 to +100 (negative = corrupt-aligned, positive = pure-aligned, 0 = neutral)
    Strength: 0.0-1.0 (how strongly the skill requires alignment match, 0 = no requirement)
}
```

**Example Skill Affinities:**
```
Skill: Precise Criticals
  MoralAffinity: -30 (slightly evil-aligned, cruel precision)
  OrderAffinity: +20 (slightly lawful-aligned, disciplined)
  PurityAffinity: -40 (corrupt-aligned, brutal)
  Strength: 0.6 (moderate alignment requirement)
  
Skill: Beneficial Aura
  MoralAffinity: +50 (strongly good-aligned)
  OrderAffinity: +30 (lawful-aligned, ordered)
  PurityAffinity: +40 (pure-aligned, altruistic)
  Strength: 0.7 (strong alignment requirement)
  
Skill: Brutal Finisher
  MoralAffinity: -60 (strongly evil-aligned)
  OrderAffinity: -40 (chaotic-aligned, uncontrolled)
  PurityAffinity: -50 (corrupt-aligned, vicious)
  Strength: 0.8 (very strong alignment requirement)
```

---

## Cost Modulation Formula

### Base Cost with Alignment Modifier

**Skill/Passive Cost Calculation:**
```
FinalCost = BaseCost × (1.0 + AlignmentCostPenalty - AlignmentCostBonus)

Where:
  BaseCost = Base skill/passive unlock cost (experience, skill points, resources)
  AlignmentCostPenalty = Mismatch penalty (0.0-2.0, increases cost for mismatched alignment)
  AlignmentCostBonus = Match bonus (0.0-0.5, reduces cost for matched alignment)
```

**Alignment Match Calculation:**
```
AlignmentMatch = CalculateAlignmentMatch(EntityAlignment, SkillAffinity)

Where:
  EntityAlignment = Entity's alignment values (Moral, Order, Purity)
  SkillAffinity = Skill's alignment affinity requirements
  
AlignmentMatch = (
    (1.0 - |EntityMoral - SkillMoralAffinity| / 200) × SkillStrength × MoralWeight +
    (1.0 - |EntityOrder - SkillOrderAffinity| / 200) × SkillStrength × OrderWeight +
    (1.0 - |EntityPurity - SkillPurityAffinity| / 200) × SkillStrength × PurityWeight
) / (MoralWeight + OrderWeight + PurityWeight)

Where:
  SkillStrength = How strongly skill requires alignment (0.0-1.0)
  MoralWeight = 0.4 (moral axis weight)
  OrderWeight = 0.3 (order axis weight)
  PurityWeight = 0.3 (purity axis weight)
  
AlignmentMatch ranges from -1.0 (perfect opposite) to +1.0 (perfect match)
```

**Cost Modifiers:**
```
If AlignmentMatch >= 0.5 (Strong Match):
  AlignmentCostBonus = AlignmentMatch × 0.5 (up to 50% cost reduction)
  AlignmentCostPenalty = 0.0
  
Else If AlignmentMatch >= 0.0 (Weak Match):
  AlignmentCostBonus = AlignmentMatch × 0.2 (up to 20% cost reduction)
  AlignmentCostPenalty = 0.0
  
Else If AlignmentMatch >= -0.5 (Weak Mismatch):
  AlignmentCostBonus = 0.0
  AlignmentCostPenalty = |AlignmentMatch| × 1.0 (up to 50% cost increase)
  
Else (Strong Mismatch):
  AlignmentCostBonus = 0.0
  AlignmentCostPenalty = 0.5 + (|AlignmentMatch| - 0.5) × 3.0 (50% to 200% cost increase)
```

---

## Examples

### Example 1: Evil Entity Learning Cruel Talent

**Entity:**
```
EntityAlignment = {
    Moral: -70 (strongly evil)
    Order: -30 (slightly chaotic)
    Purity: -80 (strongly corrupt)
}
```

**Skill: Precise Criticals**
```
SkillAffinity = {
    MoralAffinity: -30 (slightly evil-aligned)
    OrderAffinity: +20 (slightly lawful-aligned)
    PurityAffinity: -40 (corrupt-aligned)
    Strength: 0.6
}
```

**Calculation:**
```
MoralMatch = (1.0 - |-70 - (-30)| / 200) × 0.6 × 0.4 = (1.0 - 40/200) × 0.24 = 0.80 × 0.24 = 0.192
OrderMatch = (1.0 - |-30 - 20| / 200) × 0.6 × 0.3 = (1.0 - 50/200) × 0.18 = 0.75 × 0.18 = 0.135
PurityMatch = (1.0 - |-80 - (-40)| / 200) × 0.6 × 0.3 = (1.0 - 40/200) × 0.18 = 0.80 × 0.18 = 0.144

AlignmentMatch = (0.192 + 0.135 + 0.144) / 1.0 = 0.471 (weak match, close to strong match)

Cost Modifier:
  AlignmentMatch = 0.471 (just below 0.5 threshold)
  AlignmentCostBonus = 0.471 × 0.2 = 0.094 (9.4% cost reduction)
  AlignmentCostPenalty = 0.0
  
FinalCost = BaseCost × (1.0 + 0.0 - 0.094) = BaseCost × 0.906 (10% discount)
```

**Result:** Evil entity gets a 10% discount on cruel talent (good match).

---

### Example 2: Good Entity Learning Cruel Talent

**Entity:**
```
EntityAlignment = {
    Moral: +90 (strongly good)
    Order: +80 (strongly lawful)
    Purity: +85 (strongly pure)
}
```

**Skill: Precise Criticals** (same as above)

**Calculation:**
```
MoralMatch = (1.0 - |90 - (-30)| / 200) × 0.6 × 0.4 = (1.0 - 120/200) × 0.24 = 0.40 × 0.24 = 0.096
OrderMatch = (1.0 - |80 - 20| / 200) × 0.6 × 0.3 = (1.0 - 60/200) × 0.18 = 0.70 × 0.18 = 0.126
PurityMatch = (1.0 - |85 - (-40)| / 200) × 0.6 × 0.3 = (1.0 - 125/200) × 0.18 = 0.375 × 0.18 = 0.0675

AlignmentMatch = (0.096 + 0.126 + 0.0675) / 1.0 = 0.29 (weak mismatch)

Cost Modifier:
  AlignmentMatch = 0.29 (above -0.5, weak mismatch)
  AlignmentCostBonus = 0.0
  AlignmentCostPenalty = 0.29 × 1.0 = 0.29 (29% cost increase)
  
FinalCost = BaseCost × (1.0 + 0.29 - 0.0) = BaseCost × 1.29 (29% premium)
```

**Result:** Good entity pays 29% more, but can still learn the cruel talent.

---

### Example 3: Good Entity Learning Beneficial Aura

**Entity:** (same good entity as above)

**Skill: Beneficial Aura**
```
SkillAffinity = {
    MoralAffinity: +50 (strongly good-aligned)
    OrderAffinity: +30 (lawful-aligned)
    PurityAffinity: +40 (pure-aligned)
    Strength: 0.7
}
```

**Calculation:**
```
MoralMatch = (1.0 - |90 - 50| / 200) × 0.7 × 0.4 = (1.0 - 40/200) × 0.28 = 0.80 × 0.28 = 0.224
OrderMatch = (1.0 - |80 - 30| / 200) × 0.7 × 0.3 = (1.0 - 50/200) × 0.21 = 0.75 × 0.21 = 0.1575
PurityMatch = (1.0 - |85 - 40| / 200) × 0.7 × 0.3 = (1.0 - 45/200) × 0.21 = 0.775 × 0.21 = 0.16275

AlignmentMatch = (0.224 + 0.1575 + 0.16275) / 1.0 = 0.544 (strong match)

Cost Modifier:
  AlignmentMatch = 0.544 (above 0.5 threshold, strong match)
  AlignmentCostBonus = 0.544 × 0.5 = 0.272 (27.2% cost reduction)
  AlignmentCostPenalty = 0.0
  
FinalCost = BaseCost × (1.0 + 0.0 - 0.272) = BaseCost × 0.728 (27% discount)
```

**Result:** Good entity gets a 27% discount on beneficial skill (perfect match).

---

### Example 4: Strong Mismatch (Good Entity Learning Brutal Finisher)

**Entity:** (same good entity)

**Skill: Brutal Finisher**
```
SkillAffinity = {
    MoralAffinity: -60 (strongly evil-aligned)
    OrderAffinity: -40 (chaotic-aligned)
    PurityAffinity: -50 (corrupt-aligned)
    Strength: 0.8 (very strong requirement)
}
```

**Calculation:**
```
MoralMatch = (1.0 - |90 - (-60)| / 200) × 0.8 × 0.4 = (1.0 - 150/200) × 0.32 = 0.25 × 0.32 = 0.08
OrderMatch = (1.0 - |80 - (-40)| / 200) × 0.8 × 0.3 = (1.0 - 120/200) × 0.24 = 0.40 × 0.24 = 0.096
PurityMatch = (1.0 - |85 - (-50)| / 200) × 0.8 × 0.3 = (1.0 - 135/200) × 0.24 = 0.325 × 0.24 = 0.078

AlignmentMatch = (0.08 + 0.096 + 0.078) / 1.0 = 0.254 (weak mismatch, but high strength amplifies)
```

**Adjusted for High Strength:**
```
EffectiveAlignmentMatch = AlignmentMatch - (SkillStrength - 0.5) × 0.3
EffectiveAlignmentMatch = 0.254 - (0.8 - 0.5) × 0.3 = 0.254 - 0.09 = 0.164
```

**Cost Modifier:**
```
AlignmentMatch = 0.164 (weak mismatch)
AlignmentCostBonus = 0.0
AlignmentCostPenalty = 0.164 × 1.0 = 0.164 (16.4% cost increase)

But with high strength penalty:
FinalCost = BaseCost × (1.0 + 0.164 - 0.0) = BaseCost × 1.164 (16.4% premium)
```

**Result:** Good entity can still learn brutal finisher, but pays 16% more (vibes, not set in stone).

---

## Outlook-Based Affinity

### Outlook Tags and Skill Flavors

**Skills can also have outlook affinities** that match behavioral/cultural expressions.

**Outlook Types:**
- Warlike, Peaceful
- Spiritual, Materialistic
- Scholarly, Pragmatic
- Xenophobic, Egalitarian
- Authoritarian

**Outlook Affinity Definition:**
```
SkillOutlookAffinity = {
    PrimaryOutlook: OutlookType (primary outlook match)
    SecondaryOutlook: OutlookType (optional secondary match)
    AffinityStrength: 0.0-1.0 (how much outlook matters)
}
```

**Outlook Cost Modifier:**
```
OutlookMatch = CalculateOutlookMatch(EntityOutlook, SkillOutlookAffinity)

Where:
  EntityOutlook = Entity's outlook tags (Primary, Secondary, Tertiary)
  SkillOutlookAffinity = Skill's outlook requirements
  
OutlookMatch = 1.0 if entity has matching outlook (Primary match = 1.0, Secondary match = 0.7, Tertiary match = 0.4)
OutlookMatch = 0.0 if no match

OutlookCostModifier = OutlookMatch × AffinityStrength × 0.3 (up to 30% cost reduction)
```

**Example:**
```
Skill: War Chant
  OutlookAffinity: Warlike (Primary), Authoritarian (Secondary)
  AffinityStrength: 0.6
  
Entity: Warlike (Primary), Authoritarian (Secondary)
  OutlookMatch = 1.0 (perfect primary match)
  OutlookCostModifier = 1.0 × 0.6 × 0.3 = 0.18 (18% additional cost reduction)
  
Combined with alignment match, could total up to 50% cost reduction
```

---

## Combined Cost Formula

**Final Skill/Passive Cost:**
```
FinalCost = BaseCost × (1.0 + AlignmentCostPenalty - AlignmentCostBonus - OutlookCostModifier)

Where:
  BaseCost = Base unlock cost
  AlignmentCostPenalty = 0.0-2.0 (mismatch penalty)
  AlignmentCostBonus = 0.0-0.5 (alignment match bonus, max 50%)
  OutlookCostModifier = 0.0-0.3 (outlook match bonus, max 30%)
  
Maximum discount: 80% (50% alignment + 30% outlook)
Maximum premium: 200% (strong mismatch)
```

---

## Skill Categories and Typical Affinities

### Combat Skills

**Cruel/Evil-Aligned:**
- **Precise Criticals:** Moral -30, Purity -40, Strength 0.6
- **Brutal Finisher:** Moral -60, Order -40, Purity -50, Strength 0.8
- **Torture Techniques:** Moral -80, Order -60, Purity -70, Strength 0.9
- **Ruthless Strike:** Moral -50, Purity -40, Strength 0.7
- **Vicious Mockery:** Moral -40, Order -30, Strength 0.5

**Beneficial/Good-Aligned:**
- **Beneficial Aura:** Moral +50, Order +30, Purity +40, Strength 0.7
- **Lay on Hands:** Moral +60, Purity +50, Strength 0.8
- **Inspiring Presence:** Moral +40, Order +30, Strength 0.6
- **Protective Barrier:** Moral +45, Order +35, Purity +30, Strength 0.7
- **Cleansing Light:** Moral +55, Purity +60, Strength 0.8

**Neutral/Order-Aligned:**
- **Precise Strike:** Order +40, Strength 0.5
- **Defensive Stance:** Order +50, Moral +20, Strength 0.6
- **Tactical Awareness:** Order +45, Strength 0.5
- **Disciplined Focus:** Order +60, Strength 0.7

**Neutral/Chaos-Aligned:**
- **Wild Strike:** Order -40, Strength 0.5
- **Unpredictable Dodge:** Order -50, Strength 0.6
- **Chaotic Flurry:** Order -45, Moral -20, Strength 0.6

---

### Support Skills

**Healing/Buffing (Good/Pure-Aligned):**
- **Greater Heal:** Moral +50, Purity +55, Strength 0.8
- **Regeneration Aura:** Moral +40, Purity +45, Strength 0.7
- **Benevolent Blessing:** Moral +60, Purity +50, Order +30, Strength 0.8

**Debuffing/Corruption (Evil/Corrupt-Aligned):**
- **Corruption Wave:** Purity -60, Moral -40, Strength 0.7
- **Draining Touch:** Purity -50, Moral -45, Strength 0.7
- **Malefic Curse:** Moral -70, Purity -65, Strength 0.9

---

### Passive Abilities

**Passives follow same affinity rules:**
- **Cruel Strikes (Passive):** Moral -30, Purity -35, Strength 0.6
- **Compassionate Aura (Passive):** Moral +45, Purity +40, Strength 0.7
- **Lawful Precision (Passive):** Order +50, Strength 0.6
- **Chaotic Momentum (Passive):** Order -45, Strength 0.6

---

## System Design Philosophy

### Vibes, Not Rules

**Core Principle:** Alignment gates are soft, not hard.

**Why:**
- **Player Agency:** Players should be able to build any character concept
- **Narrative Flexibility:** Even good characters can learn cruel techniques (at cost)
- **Emergent Stories:** Unexpected combinations create interesting narratives
- **Accessibility:** All skills remain accessible, just easier/harder based on match

**Example Narrative:**
```
A lawful good paladin could learn "Brutal Finisher" to stop a recurring villain
who keeps escaping. The cost is high (alignment mismatch), but the narrative
justification ("ends justify means") makes it compelling. The skill is harder
to master, requires more practice, but remains accessible.
```

---

### Cost as Narrative Barrier

**Higher costs represent:**
- **Moral Conflict:** Learning skills that conflict with your nature is difficult
- **Training Difficulty:** Finding teachers/mentors for mismatched skills is harder
- **Practice Time:** Skills that don't come naturally require more effort
- **Cultural Barriers:** Your community may disapprove of certain skills

**Lower costs represent:**
- **Natural Aptitude:** Skills matching your nature come easily
- **Easy Access:** Teachers and resources are readily available
- **Cultural Support:** Your community encourages these skills
- **Intuitive Understanding:** Skills align with your worldview

---

## Component Structures

```csharp
// Skill alignment affinity
public struct SkillAlignmentAffinity : IComponentData
{
    public sbyte MoralAffinity;      // -100 to +100 (evil to good)
    public sbyte OrderAffinity;      // -100 to +100 (chaotic to lawful)
    public sbyte PurityAffinity;     // -100 to +100 (corrupt to pure)
    public half Strength;            // 0.0-1.0 (how strongly alignment matters)
}

// Skill outlook affinity
public struct SkillOutlookAffinity : IComponentData
{
    public OutlookType PrimaryOutlook;     // Primary outlook match
    public OutlookType SecondaryOutlook;   // Optional secondary match
    public half AffinityStrength;          // 0.0-1.0 (how much outlook matters)
}

// Skill cost modifier (calculated at unlock time)
public struct SkillCostModifier : IComponentData
{
    public float AlignmentMatch;        // -1.0 to +1.0 (alignment match score)
    public float OutlookMatch;          // 0.0 to +1.0 (outlook match score)
    public float AlignmentCostPenalty;  // 0.0-2.0 (cost increase for mismatch)
    public float AlignmentCostBonus;    // 0.0-0.5 (cost reduction for match)
    public float OutlookCostModifier;   // 0.0-0.3 (outlook match bonus)
    public float FinalCostMultiplier;   // Combined cost multiplier
}
```

---

## Open Questions

- **Progression Gates:** Should some skills require prerequisites that are easier/harder based on alignment?
- **Skill Decay:** Should mismatched skills decay faster or be harder to maintain?
- **Alignment Shift:** If entity alignment shifts after learning a skill, should skill effectiveness change?
- **Outlook Evolution:** Should outlook changes affect existing skill costs or only new unlocks?
- **Hybrid Skills:** How to handle skills with mixed alignment affinities (e.g., lawful evil)?

---

## Related Documentation

- **Alignment System**: `puredots/Packages/com.moni.puredots/Runtime/Identity/Components.cs`
- **Outlook System**: `puredots/Packages/com.moni.puredots/Runtime/Identity/Components.cs`
- **Skill Progression**: (To be referenced when skill system is documented)
- **Generalized Alignment Framework**: `puredots/Docs/Concepts/Core/Generalized_Alignment_Framework.md`




