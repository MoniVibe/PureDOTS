# Hierarchical Tooltip System

**Last Updated:** 2025-12-18
**Status:** Design Document - UI/UX Architecture
**Cross-Game:** Yes (Godgame and Space4X)
**Entity-Agnostic:** Yes
**Performance Conscious:** Yes

---

## Overview

The **Hierarchical Tooltip System** provides contextual, layered information about all entities in the game world. Inspired by Paradox Interactive's approach, this system allows players to **hover over any stat or term** in a tooltip to see a **sub-tooltip** explaining it in detail, creating an intuitive, self-documenting game experience.

**Core Design Philosophy:**
- **Show, don't hide** - All information accessible, but layered
- **Learn by exploring** - Hovering teaches mechanics organically
- **No wiki required** - Game explains itself through tooltips
- **Consistent across games** - Same patterns in Godgame and Space4X
- **Performance aware** - Efficient for hundreds/thousands of entities

---

## Tooltip Architecture

### Three-Tier Information Hierarchy

```
Tier 1: Primary Tooltip (hover on entity)
├─ Entity name, type, basic stats
├─ Current state, health, status effects
├─ Highlighted terms (blue/orange text) = Tier 2 trigger
└─ Summary of immediate concerns

    Tier 2: Sub-Tooltip (hover on highlighted term)
    ├─ Detailed explanation of stat/mechanic
    ├─ Current value, modifiers breakdown
    ├─ Further highlighted terms = Tier 3 trigger
    └─ Related mechanics cross-reference

        Tier 3: Deep Tooltip (hover on Tier 2 terms)
        ├─ Formula breakdown
        ├─ Historical context (if applicable)
        ├─ Tech unlock info
        └─ Design intent explanation
```

**Example (Space4X Ship):**

```
[TIER 1: Hover on ship]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Battlecruiser "Resolute"
Heavy Capital Ship (12,000 tons)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Hull: 45,000 / 50,000 (90%)
Shields: 8,200 / 11,900 (69%)
Crew: 452 / 500 (90% effectiveness)

Power: 8,328 MW available
├─ Reactor output: 8,855 MW
├─ Distribution loss: 527 MW
└─ Battery: 34,130 / 50,000 MW·s (68%)

Weapons: 4× Particle Cannons [READY]
├─ Damage per volley: 4,340
├─ Power per shot: 10,870 MW·s
└─ Cooldown: 2.1 sec remaining

Combat Status: Engaging 3 targets
Morale: High (85%)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Terms highlighted in BLUE can be hovered for details]
```

**[TIER 2: Hover on "Distribution loss"]**

```
┌─────────────────────────────┐
│ Distribution Loss           │
├─────────────────────────────┤
│ Power lost during           │
│ transmission through        │
│ ship's conduits             │
│                             │
│ Base Efficiency: 95%        │
│ Conduit Damage: -1%         │
│ ═══════════════════         │
│ Actual Efficiency: 94.05%   │
│                             │
│ Lost: 527 MW (becomes heat) │
│                             │
│ Upgrade with [Tech Level 10]│
│ to reduce loss to 3%        │
└─────────────────────────────┘
```

**[TIER 3: Hover on "Tech Level 10"]**

```
┌──────────────────────┐
│ Tech Level 10        │
├──────────────────────┤
│ Advanced Fusion Era  │
│                      │
│ Unlocks:             │
│ • Low-temp super-    │
│   conductors (97%    │
│   distribution)      │
│ • Antimatter reactors│
│ • Ultra-capacitors   │
│                      │
│ Research Cost:       │
│ 15,000 RP            │
│                      │
│ Current: Tech 8      │
│ Progress: 8,450 RP   │
└──────────────────────┘
```

---

## Tooltip Interaction Methods

### Method 1: Hover with Timeout (Paradox Style)

**Primary Method:**
1. Hover over entity → Primary tooltip appears instantly
2. Hover over highlighted term → Sub-tooltip appears after 0.3s
3. Move mouse away → 3-second grace period to return
4. If mouse returns within 3s → Tooltip persists
5. If not → Tooltip fades out

**Benefits:**
- Natural exploration (no accidental triggers)
- Forgiving (grace period prevents frustration)
- Doesn't interfere with fast mouse movement

### Method 2: Middle Mouse Button Pin

**Alternative:**
1. Middle-click highlighted term → Pin sub-tooltip open
2. Tooltip stays open indefinitely
3. Can open multiple pinned tooltips
4. Click outside or press ESC to close
5. Pinned tooltips stack with slight offset

**Benefits:**
- Study complex interactions
- Compare multiple stats
- Reference while making decisions

### Method 3: Shift+Hover (Advanced)

**Expert Mode:**
1. Hold Shift while hovering → Skip Tier 1, jump to Tier 2
2. Hold Shift+Ctrl → Jump directly to Tier 3 (formula view)
3. Useful for experienced players who know what they want

---

## Component Architecture

### Tooltip Data Provider

```csharp
/// <summary>
/// Provides tooltip data for any entity
/// </summary>
public struct TooltipDataProvider : IComponentData
{
    /// <summary>
    /// Primary tooltip template ID
    /// </summary>
    public TooltipTemplateId PrimaryTemplate;

    /// <summary>
    /// Entity's display name
    /// </summary>
    public FixedString64Bytes DisplayName;

    /// <summary>
    /// Entity type for icon/styling
    /// </summary>
    public EntityTypeId EntityType;

    /// <summary>
    /// Whether tooltips are enabled for this entity
    /// </summary>
    public bool TooltipsEnabled;

    /// <summary>
    /// Custom tooltip flags (show health, show stats, etc.)
    /// </summary>
    public TooltipFlags Flags;
}

/// <summary>
/// Buffer of tooltip sections to display
/// </summary>
public struct TooltipSection : IBufferElementData
{
    public FixedString32Bytes SectionId;      // "Health", "Power", "Weapons"
    public TooltipSectionType SectionType;
    public int DisplayPriority;               // Lower = shown first
    public bool IsCollapsible;                // Can user hide this section?
}

/// <summary>
/// Buffer of highlighted terms that trigger sub-tooltips
/// </summary>
public struct TooltipHighlight : IBufferElementData
{
    /// <summary>
    /// Term text (e.g., "Distribution Loss")
    /// </summary>
    public FixedString64Bytes TermText;

    /// <summary>
    /// Sub-tooltip template to show when hovered
    /// </summary>
    public TooltipTemplateId SubTooltipTemplate;

    /// <summary>
    /// Highlight color (blue = stat, orange = mechanic, green = bonus, red = penalty)
    /// </summary>
    public TooltipHighlightColor HighlightColor;

    /// <summary>
    /// Optional entity reference (e.g., for per-entity stat details)
    /// </summary>
    public Entity ContextEntity;
}
```

### Tooltip Builder System

```csharp
/// <summary>
/// Builds tooltip data for an entity on demand
/// </summary>
[BurstCompile]
public partial struct TooltipBuilderSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only build tooltips when requested (by hover)
        foreach (var request in SystemAPI.Query<RefRO<TooltipBuildRequest>>())
        {
            Entity targetEntity = request.ValueRO.TargetEntity;

            // Get entity data
            var tooltipData = state.EntityManager.GetComponentData<TooltipDataProvider>(targetEntity);

            // Build tooltip sections based on entity type
            var sections = BuildTooltipSections(state, targetEntity, tooltipData);

            // Generate highlight terms
            var highlights = GenerateHighlights(state, targetEntity, sections);

            // Send to presentation layer
            SendTooltipToUI(state, targetEntity, sections, highlights);
        }
    }

    static NativeList<TooltipSectionData> BuildTooltipSections(
        SystemState state,
        Entity entity,
        TooltipDataProvider tooltipData)
    {
        var sections = new NativeList<TooltipSectionData>(16, Allocator.Temp);

        // Header section (always first)
        sections.Add(BuildHeaderSection(state, entity, tooltipData));

        // Health section (if entity has health)
        if (state.EntityManager.HasComponent<Health>(entity))
        {
            sections.Add(BuildHealthSection(state, entity));
        }

        // Power section (if entity has power)
        if (state.EntityManager.HasComponent<PowerGenerator>(entity) ||
            state.EntityManager.HasComponent<PowerDistribution>(entity))
        {
            sections.Add(BuildPowerSection(state, entity));
        }

        // Combat section (if entity can fight)
        if (state.EntityManager.HasBuffer<WeaponPowerDemand>(entity) ||
            state.EntityManager.HasBuffer<CombatPosition>(entity))
        {
            sections.Add(BuildCombatSection(state, entity));
        }

        // Relations section (if entity has relations)
        if (state.EntityManager.HasBuffer<RelationEntry>(entity))
        {
            sections.Add(BuildRelationsSection(state, entity));
        }

        // Status effects section
        if (state.EntityManager.HasBuffer<StatusEffect>(entity))
        {
            sections.Add(BuildStatusEffectsSection(state, entity));
        }

        // Sort by priority
        sections.Sort(new TooltipPrioritySorter());

        return sections;
    }
}
```

### Example Section Builders

#### Health Section

```csharp
static TooltipSectionData BuildHealthSection(SystemState state, Entity entity)
{
    var health = state.EntityManager.GetComponentData<Health>(entity);
    var maxHealth = state.EntityManager.GetComponentData<MaxHealth>(entity);

    var section = new TooltipSectionData
    {
        SectionId = "Health",
        SectionType = TooltipSectionType.StatBlock,
        DisplayPriority = 1
    };

    // Main line
    section.AddLine($"Hull: {health.Current:F0} / {maxHealth.Value:F0} ({(health.Current/maxHealth.Value)*100:F0}%)");

    // Damage reduction (if applicable)
    if (state.EntityManager.HasComponent<DamageReduction>(entity))
    {
        var dr = state.EntityManager.GetComponentData<DamageReduction>(entity);
        section.AddLine($"Armor: {dr.Value:F0}% damage reduction", TooltipHighlightColor.Green);
    }

    // Regeneration (if applicable)
    if (state.EntityManager.HasComponent<HealthRegeneration>(entity))
    {
        var regen = state.EntityManager.GetComponentData<HealthRegeneration>(entity);
        section.AddLine($"Regen: +{regen.Rate:F1}/sec", TooltipHighlightColor.Green);
    }

    return section;
}
```

#### Power Section

```csharp
static TooltipSectionData BuildPowerSection(SystemState state, Entity entity)
{
    var section = new TooltipSectionData
    {
        SectionId = "Power",
        SectionType = TooltipSectionType.StatBlock,
        DisplayPriority = 2
    };

    // Generator output
    if (state.EntityManager.HasComponent<PowerGenerator>(entity))
    {
        var generator = state.EntityManager.GetComponentData<PowerGenerator>(entity);
        float actualOutput = CalculateActualOutput(generator);

        section.AddLine($"Power: {actualOutput:F0} MW available");

        // Breakdown (expandable sub-section)
        section.AddExpandableGroup("Power Breakdown", new[]
        {
            $"Reactor output: {generator.TheoreticalMaxOutput * generator.CurrentOutputPercent:F0} MW",
            $"Efficiency: {generator.Efficiency*100:F1}%",  // Highlighted → sub-tooltip
            $"Distribution loss: {CalculateDistributionLoss(state, entity):F0} MW",  // Highlighted
            $"Available: {actualOutput:F0} MW"
        });
    }

    // Battery status
    if (state.EntityManager.HasComponent<PowerBattery>(entity))
    {
        var battery = state.EntityManager.GetComponentData<PowerBattery>(entity);
        float percent = (battery.CurrentStored / battery.MaxCapacity) * 100f;

        section.AddLine($"Battery: {battery.CurrentStored:F0} / {battery.MaxCapacity:F0} MW·s ({percent:F0}%)");

        // Low battery warning
        if (percent < 30f)
        {
            section.AddLine("⚠ Battery Low", TooltipHighlightColor.Red);
        }
    }

    return section;
}
```

#### Combat Section (with Bay and Platform integration)

```csharp
static TooltipSectionData BuildCombatSection(SystemState state, Entity entity)
{
    var section = new TooltipSectionData
    {
        SectionId = "Combat",
        SectionType = TooltipSectionType.StatBlock,
        DisplayPriority = 3
    };

    // Weapons
    if (state.EntityManager.HasBuffer<WeaponPowerDemand>(entity))
    {
        var weapons = state.EntityManager.GetBuffer<WeaponPowerDemand>(entity);

        section.AddLine($"Weapons: {weapons.Length}× equipped");

        foreach (var weapon in weapons)
        {
            string status = weapon.BankRequirement.PowerBank != Entity.Null ? "[READY]" : "[POWER STARVED]";
            section.AddLine($"  ├─ {weapon.WeaponName}: {status}");
            section.AddLine($"  │  Damage: {weapon.BaseDamage:F0}");
            section.AddLine($"  │  Power: {weapon.PowerPerShot:F0} MW·s per shot");  // Highlighted
        }
    }

    // Combat positions (bays/platforms)
    if (state.EntityManager.HasBuffer<CombatPosition>(entity))
    {
        var positions = state.EntityManager.GetBuffer<CombatPosition>(entity);

        int openBays = 0;
        int occupiedSlots = 0;
        int totalSlots = 0;

        foreach (var pos in positions)
        {
            if (pos.State == BayState.Open)
                openBays++;
            occupiedSlots += pos.CurrentOccupants;
            totalSlots += pos.MaxOccupants;
        }

        section.AddLine($"Combat Positions: {openBays} / {positions.Length} open");
        section.AddLine($"Occupants: {occupiedSlots} / {totalSlots}");  // Highlighted → shows occupant details
    }

    // Shields
    if (state.EntityManager.HasComponent<ShieldPowerDemand>(entity))
    {
        var shield = state.EntityManager.GetComponentData<ShieldPowerDemand>(entity);
        var shieldHP = state.EntityManager.GetComponentData<ShieldHP>(entity);

        section.AddLine($"Shields: {shieldHP.Current:F0} / {shieldHP.Max:F0} ({(shieldHP.Current/shieldHP.Max)*100:F0}%)");
        section.AddLine($"Recharge: {shield.RechargeDraw:F0} MW");  // Highlighted
    }

    return section;
}
```

#### Relations Section (with Reaction system integration)

```csharp
static TooltipSectionData BuildRelationsSection(SystemState state, Entity entity)
{
    var section = new TooltipSectionData
    {
        SectionId = "Relations",
        SectionType = TooltipSectionType.RelationList,
        DisplayPriority = 5
    };

    var relations = state.EntityManager.GetBuffer<RelationEntry>(entity);

    if (relations.Length == 0)
    {
        section.AddLine("No diplomatic relations");
        return section;
    }

    // Sort by relation value (highest first)
    var sortedRelations = new NativeList<RelationEntry>(relations.Length, Allocator.Temp);
    for (int i = 0; i < relations.Length; i++)
        sortedRelations.Add(relations[i]);

    sortedRelations.Sort(new RelationValueSorter());

    // Show top 5 relations
    int displayCount = math.min(5, sortedRelations.Length);
    section.AddLine($"Diplomatic Relations: (showing {displayCount} of {sortedRelations.Length})");

    for (int i = 0; i < displayCount; i++)
    {
        var relation = sortedRelations[i];
        string targetName = GetEntityName(state, relation.TargetEntity);
        string relationLevel = GetRelationLevelText(relation.RelationValue);
        var color = GetRelationColor(relation.RelationValue);

        // Main relation line (highlighted → shows detailed relation breakdown)
        section.AddLine($"  {targetName}: {relationLevel} ({relation.RelationValue:+0;-0})", color);

        // Show active bonuses from this relation
        if (state.EntityManager.HasBuffer<RelationBonus>(entity))
        {
            var bonuses = state.EntityManager.GetBuffer<RelationBonus>(entity);
            foreach (var bonus in bonuses)
            {
                if (bonus.SourceEntity == relation.TargetEntity)
                {
                    section.AddLine($"    └─ {bonus.BonusType}: {bonus.Magnitude:+0.0%;-0.0%}", TooltipHighlightColor.Green);
                }
            }
        }
    }

    if (sortedRelations.Length > displayCount)
    {
        section.AddLine($"  ... and {sortedRelations.Length - displayCount} more");
    }

    sortedRelations.Dispose();
    return section;
}
```

---

## Game-Specific Implementations

### Godgame: Villager Tooltips

```
[TIER 1: Hover on villager]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Olaf the Smith
Male, Age 34, Blacksmith
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Health: 85 / 100 (Bruised)
Hunger: 40 / 100 (Fed)
Energy: 60 / 100 (Tired)

Alignment:
├─ Moral: Neutral (+2)
├─ Order: Lawful (+45)
└─ Purity: Pure (+12)

Behavior:
├─ Bold / Craven: Bold (+30)
└─ Vengeful / Forgiving: Balanced (0)

Skills:
├─ Smithing: 85 (Expert)
├─ Combat: 45 (Competent)
└─ Trade: 30 (Novice)

Current Activity: Crafting iron sword
Location: Village Forge

Relations:
├─ Freya (Wife): Beloved (+95)
├─ Bjorn (Rival): Grudge (-30)
└─ Village: Loyal (+60)

Status Effects:
└─ Inspired (+20% crafting speed, 45m remaining)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**[TIER 2: Hover on "Bold / Craven"]**

```
┌────────────────────────────┐
│ Bold / Craven Behavior     │
├────────────────────────────┤
│ How this villager reacts   │
│ to danger and risk.        │
│                            │
│ Bold: +30                  │
│ ═════════════════          │
│                            │
│ Effects:                   │
│ • +15% combat initiative   │
│ • More likely to volunteer │
│   for dangerous tasks      │
│ • Reacts positively to     │
│   successful raids (+10)   │
│ • Less affected by fear    │
│   (-50% fear duration)     │
│                            │
│ Inherited from parents:    │
│ • Father (Erik): +40 Bold  │
│ • Mother (Astrid): +20 Bold│
│                            │
│ Modified by events:        │
│ • Survived bandit raid:    │
│   +10 Bold                 │
│ • Witnessed miracle:       │
│   +5 Bold                  │
└────────────────────────────┘
```

**[TIER 3: Hover on "combat initiative"]**

```
┌───────────────────────┐
│ Combat Initiative     │
├───────────────────────┤
│ How quickly a fighter │
│ engages in battle.    │
│                       │
│ Base: 50              │
│ Bold bonus: +15%      │
│ Equipment: +10        │
│ Morale: +5            │
│ ═══════════           │
│ Total: 88             │
│                       │
│ Higher initiative     │
│ strikes first, deals  │
│ +10% damage on opener.│
└───────────────────────┘
```

### Godgame: Building Tooltips

```
[TIER 1: Hover on building]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Village Forge
Tier 2 Workshop
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Durability: 820 / 1,000 (82%)
Workers: 2 / 3
Efficiency: 140% (boosted)

Production:
├─ Current: Iron Sword
├─ Progress: 65%
├─ Time remaining: 8 minutes
└─ Quality: Standard (expected)

Power Consumption: 0 (manual labor)

Upgrades:
├─ Advanced Anvil (installed)
└─ Master's Tools (available, 500 gold)

Bonuses:
└─ Nearby Temple: +40% quality
    (Divine Inspiration)

Workforce:
├─ Olaf the Smith (Smithing 85)
├─ Harald (Smithing 60)
└─ [Empty slot]

Storage:
├─ Iron Ingots: 45 / 100
├─ Coal: 120 / 200
└─ Wood: 30 / 50
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**[TIER 2: Hover on "Divine Inspiration"]**

```
┌────────────────────────────┐
│ Divine Inspiration         │
├────────────────────────────┤
│ Bonus from Nearby Temple   │
│                            │
│ Effect:                    │
│ • +40% item quality        │
│ • +10% production speed    │
│                            │
│ Source: Temple of Odin     │
│ Distance: 45m              │
│ Radius: 80m                │
│                            │
│ Temple Level: 3            │
│ Worship Power: High        │
│                            │
│ Alignment compatibility:   │
│ • Forge Owner: Lawful +45  │
│ • Temple Faith: Lawful +60 │
│ • Synergy bonus: +20%      │
└────────────────────────────┘
```

### Space4X: Ship Tooltips

```
[TIER 1: Hover on ship]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Frigate "Storm Chaser"
Fast Attack Vessel
Captain: Elena Vasquez
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Hull: 12,000 / 15,000 (80%)
Shields: 0 / 0 (NO SHIELDS)
Crew: 85 / 90 (94% effectiveness)

Power: 2,450 MW available
├─ Reactor: 3,000 MW (Tech 6)
├─ Efficiency: 85%
├─ Distribution: -100 MW loss
└─ Battery: 8,500 / 12,000 MW·s (71%)

Weapons: 6× Railguns [2 READY, 4 RECHARGING]
├─ Forward Arc: 3 guns (2 ready)
├─ Aft Arc: 2 guns (0 ready)
├─ Port Broadside: 1 gun (0 ready)

Speed: 450 m/s (boosted)
Maneuverability: High

Mission: Patrol Sector Alpha-7
Status: Combat Alert (3 hostiles detected)
Morale: Confident (75%)

Fleet: 3rd Recon Squadron
Commander: Admiral Zhang
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**[TIER 2: Hover on "NO SHIELDS"]**

```
┌────────────────────────────┐
│ Shields Unavailable        │
├────────────────────────────┤
│ This ship has NO SHIELD    │
│ EMITTERS installed.        │
│                            │
│ Shields require:           │
│ • Shield Emitter Module    │
│   (2 slots)                │
│ • Power Bank (1 slot)      │
│ • 800+ MW sustained power  │
│                            │
│ Current hull slots:        │
│ • 6× Railguns (12 slots)   │
│ • 1× Advanced Sensors (2)  │
│ • 1× Large Battery (3)     │
│ • 1× Engine Booster (2)    │
│ • Empty: 1 slot            │
│                            │
│ Consider:                  │
│ • Remove 1 railgun → free  │
│   2 slots for shield       │
│ • Trade offense for defense│
│                            │
│ Doctrine: Speed Tank       │
│ (Rely on speed to avoid    │
│ damage rather than shields)│
└────────────────────────────┘
```

**[TIER 3: Hover on "Shield Emitter Module"]**

```
┌──────────────────────┐
│ Shield Emitter       │
├──────────────────────┤
│ Tech Level 4+ Module │
│                      │
│ Specs:               │
│ • Shield HP: 3,500   │
│ • Recharge: 2.0s     │
│ • Power: 600 MW base │
│ • Slots: 2           │
│                      │
│ Requires Power Bank: │
│ • Min: 4,000 MW·s    │
│ • For surge capacity │
│                      │
│ Cost: 180,000 credits│
│ Mass: 1,200 kg       │
│                      │
│ Research: Complete   │
│ (Tech 6 available)   │
└──────────────────────┘
```

### Space4X: Station/Colony Tooltips

```
[TIER 1: Hover on colony]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Mining Colony "New Prosperity"
Asteroid Belt Settlement
Governor: Marcus Chen
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Population: 12,450
Growth: +2.3% per month
Morale: Content (68%)

Production:
├─ Iron Ore: 850 t/day
├─ Rare Metals: 120 t/day
└─ Ice (Water): 450 t/day

Infrastructure:
├─ Mining Facilities: 8 / 10
├─ Habitation Domes: 6 / 6 (full)
├─ Power Plants: 4 (120 GW)
└─ Defense Grid: Level 2

Power Grid:
├─ Generation: 120 GW
├─ Consumption: 105 GW
├─ Coverage: 100%
└─ Blackout Risk: None

Trade Balance: +45,000 cr/day
├─ Exports: 95,000 cr/day
└─ Imports: 50,000 cr/day

Outlook:
├─ Economic: Materialist (+60)
├─ Military: Pacifist (-30)
└─ Tolerance: Xenophilic (+40)

Defensive Fleet: 2 frigates stationed
Garrison: 500 marines

Situation: None
Alerts: Food shortage warning (3 days)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**[TIER 2: Hover on "Food shortage warning"]**

```
┌────────────────────────────┐
│ Food Shortage Warning      │
├────────────────────────────┤
│ Current food stocks will   │
│ run out in 3 days.         │
│                            │
│ Current stock: 1,200 tons  │
│ Consumption: 400 t/day     │
│ Production: 0 t/day        │
│ ═══════════                │
│ Deficit: -400 t/day        │
│                            │
│ Reasons:                   │
│ • No agricultural domes    │
│ • Supply convoy delayed    │
│   (pirate activity)        │
│                            │
│ Solutions:                 │
│ • Build Hydroponic Dome    │
│   (500k cr, 30 days)       │
│ • Emergency import from    │
│   nearby colonies (80k cr) │
│ • Request military escort  │
│   for supply convoy        │
│                            │
│ If unresolved:             │
│ • Morale: -20% per day     │
│ • Riots after 5 days       │
│ • Production halted        │
└────────────────────────────┘
```

---

## Performance Optimizations

### Tooltip Pooling

```csharp
/// <summary>
/// Pool of pre-allocated tooltip UI elements
/// </summary>
public class TooltipPool
{
    private Stack<TooltipUI> availableTooltips;
    private List<TooltipUI> activeTooltips;

    private const int POOL_SIZE = 10;  // Max 10 tooltips visible at once

    public TooltipUI Acquire()
    {
        if (availableTooltips.Count > 0)
        {
            var tooltip = availableTooltips.Pop();
            activeTooltips.Add(tooltip);
            return tooltip;
        }

        // Pool exhausted, reuse oldest tooltip
        var oldest = activeTooltips[0];
        activeTooltips.RemoveAt(0);
        activeTooltips.Add(oldest);
        return oldest;
    }

    public void Release(TooltipUI tooltip)
    {
        activeTooltips.Remove(tooltip);
        tooltip.Clear();
        availableTooltips.Push(tooltip);
    }
}
```

### Lazy Data Loading

```csharp
/// <summary>
/// Only build tooltip data when actually hovered
/// </summary>
public partial struct TooltipRequestSystem : ISystem
{
    // Only runs when user hovers over entity
    public void OnUpdate(ref SystemState state)
    {
        // Check if mouse hovering over entity (from input system)
        Entity hoveredEntity = GetHoveredEntity(state);

        if (hoveredEntity == Entity.Null)
            return;

        // Check if tooltip already built this frame
        if (HasTooltipCache(state, hoveredEntity))
        {
            DisplayCachedTooltip(state, hoveredEntity);
            return;
        }

        // Build tooltip data (expensive, only once per hover)
        BuildTooltipData(state, hoveredEntity);
    }
}
```

### Spatial Culling

```csharp
/// <summary>
/// Only show tooltips for entities within screen bounds
/// </summary>
public static bool ShouldShowTooltip(Entity entity, Camera camera)
{
    // Get entity position
    var position = EntityManager.GetComponentData<LocalTransform>(entity).Position;

    // Check if in view frustum
    if (!IsInViewFrustum(position, camera))
        return false;

    // Check if occluded by other entities/terrain
    if (IsOccluded(position, camera))
        return false;

    return true;
}
```

### Text Caching

```csharp
/// <summary>
/// Cache formatted strings to avoid string allocation spam
/// </summary>
public class TooltipTextCache
{
    private Dictionary<(Entity, string), string> cache;
    private int frameNumber;

    public string GetFormattedText(Entity entity, string key, Func<string> formatter)
    {
        // Clear cache every 60 frames (1 second at 60fps)
        if (frameNumber > 60)
        {
            cache.Clear();
            frameNumber = 0;
        }

        frameNumber++;

        var cacheKey = (entity, key);

        if (cache.TryGetValue(cacheKey, out string cachedText))
            return cachedText;

        // Generate text
        string text = formatter();
        cache[cacheKey] = text;

        return text;
    }
}
```

---

## Highlight Color Coding

**Consistent across both games:**

```csharp
public enum TooltipHighlightColor
{
    Blue = 0,    // Stats, numbers, mechanics
    Green = 1,   // Bonuses, positive effects
    Red = 2,     // Penalties, warnings, negative effects
    Orange = 3,  // Mechanics, game concepts
    Purple = 4,  // Rare/unique, special entities
    Yellow = 5,  // Important notices, alerts
    White = 6,   // Default, non-highlighted
    Gray = 7     // Disabled, unavailable
}
```

**Example usage:**

```
Power: 8,328 MW available            [BLUE - hoverable stat]
├─ Reactor output: 8,855 MW         [BLUE]
├─ Distribution loss: -527 MW       [RED - negative, hoverable]
└─ Battery: 34,130 MW·s            [BLUE]

Bonuses:                             [GREEN header]
├─ Divine Inspiration: +40% quality  [GREEN]
└─ Skilled Worker: +15% speed       [GREEN]

Warnings:                            [RED header]
├─ Low Battery (30%)                [YELLOW - alert]
└─ Hull Damage (critical)           [RED]

Tech Requirements:                   [ORANGE]
├─ Shield Emitter (Tech 4+)         [ORANGE]
└─ Antimatter Reactor (Tech 10+)    [ORANGE - hoverable]
```

---

## Cross-System Integration

### Forces System Integration

```
Force Receiver: Active
├─ Mass: 15,000 kg
├─ Velocity: 120 m/s northeast
└─ Active Forces: 3

Applied Forces:
├─ Gravity Well (Moon): -2.5 m/s² downward
├─ Solar Wind: +0.3 m/s² outward
└─ Engine Thrust: +8.0 m/s² forward

Net Acceleration: +5.8 m/s² (approx)
```

**[Hover on "Gravity Well"]** → Explains gravity force, radius, falloff

### Reactions System Integration

```
Recent Events:
├─ Trade deal accepted by Colony Prime
│   └─ Reaction: Positive (+15 relations)
│       Reason: Materialist outlook (+60) values trade
│
└─ Warning shot from Pirate Fleet
    └─ Reaction: Provoked (-30 relations)
        Reason: Warlike captain (+80) sees it as challenge
```

**[Hover on "Materialist outlook"]** → Explains outlook effects on reactions

### Combat Position Integration

```
Combat Positions: 3 bays
├─ Ventral Bay: OPEN (4 mechs deployed)
│   ├─ Firing Arc: 270° downward
│   ├─ Range: 500m
│   └─ Occupants: 4 / 4 (full)
│
├─ Port Bay: CLOSED
│   └─ Transition: Opening (2.3s remaining)
│
└─ Starboard Bay: DAMAGED (75% health)
    ├─ Capacity: 2 / 3 (1 slot destroyed)
    └─ Repair time: 45 minutes
```

**[Hover on "Firing Arc"]** → Shows visual arc overlay on screen

### Power Bank Integration

```
Weapon: Heavy Railgun
├─ Damage: 2,500
├─ Power per shot: 1,800 MW·s
├─ Rate of fire: 1 shot / 3 sec
└─ Power Bank: PRIMARY BANK
    ├─ Capacity: 5,000 MW·s
    ├─ Current: 3,200 MW·s (64%)
    ├─ Can fire: YES (1 shot available)
    └─ Recharge time: 2.1 seconds
```

**[Hover on "Power Bank"]** → Explains why power banks are required

---

## Accessibility Features

### Colorblind Support

```csharp
public enum ColorblindMode
{
    None,
    Protanopia,     // Red-blind
    Deuteranopia,   // Green-blind
    Tritanopia      // Blue-blind
}

// Alternative to color: use icons/symbols
static string GetHighlightSymbol(TooltipHighlightColor color)
{
    return color switch
    {
        TooltipHighlightColor.Green => "▲",  // Bonus
        TooltipHighlightColor.Red => "▼",    // Penalty
        TooltipHighlightColor.Blue => "●",   // Stat
        TooltipHighlightColor.Orange => "■", // Mechanic
        _ => ""
    };
}
```

### Font Scaling

```csharp
public enum TooltipFontSize
{
    Small = 0,     // 10pt
    Normal = 1,    // 12pt
    Large = 2,     // 14pt
    ExtraLarge = 3 // 16pt
}
```

### Screen Reader Support

```csharp
/// <summary>
/// Generate plain-text version of tooltip for screen readers
/// </summary>
public static string GenerateScreenReaderText(TooltipData tooltip)
{
    var sb = new StringBuilder();

    sb.AppendLine($"Entity: {tooltip.EntityName}");
    sb.AppendLine($"Type: {tooltip.EntityType}");

    foreach (var section in tooltip.Sections)
    {
        sb.AppendLine($"Section: {section.SectionId}");
        foreach (var line in section.Lines)
        {
            // Remove color codes, keep text
            string plainText = StripFormatting(line.Text);
            sb.AppendLine(plainText);
        }
    }

    return sb.ToString();
}
```

---

## Summary

The **Hierarchical Tooltip System** creates a **self-documenting game** where players learn mechanics organically through exploration. By hovering on any term, players can drill down to formulas, unlock requirements, and design rationale.

**Key Benefits:**

✅ **No wiki required** - Game explains itself
✅ **Consistent across games** - Same patterns in Godgame and Space4X
✅ **Performance conscious** - Pooling, caching, lazy loading
✅ **Integrates all systems** - Forces, reactions, combat, power, etc.
✅ **Accessible** - Colorblind support, font scaling, screen readers
✅ **Paradox-inspired** - Proven UX pattern from grand strategy games

**Game Impact:**

**Godgame:**
- Understand villager personalities at a glance
- See alignment effects on reactions
- Learn building bonuses and upgrades
- Track diplomatic relations

**Space4X:**
- Diagnose power issues instantly
- Optimize weapon/shield loadouts
- Understand fleet composition
- Make informed diplomatic choices

**Result:** Players feel **informed, not overwhelmed**. Complexity is **accessible, not hidden**. Learning is **exploration, not reading**.

---

**Related Documentation:**
- [Power_And_Battery_System.md](Power_And_Battery_System.md) - Power stats in tooltips
- [Reactions_And_Relations_System.md](Reactions_And_Relations_System.md) - Relation displays
- [Bay_And_Platform_Combat.md](Bay_And_Platform_Combat.md) - Combat position info
- [Relation_Bonuses_System.md](Relation_Bonuses_System.md) - Bonus breakdowns

---

**Last Updated:** 2025-12-18
**Status:** Design Document - UI/UX
**Cross-Game:** Both Godgame and Space4X
**Paradox-Inspired:** Hierarchical sub-tooltips
**Self-Documenting:** No wiki required! 📖✨
