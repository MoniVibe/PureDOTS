# Godgame Custom Game Modding (UGC System)

**Game**: Godgame (Divine Intervention)
**Framework**: Uses [PureDOTS ModdingAndEditorFramework](../../../../Packages/com.moni.puredots/Documentation/DesignNotes/ModdingAndEditorFramework.md)
**Created**: 2025-11-27

---

## Overview

Players can create custom Godgame scenarios using the in-game editor - from simple village challenges to complex radical management simulations.

**Example Custom Modes**:
- **Demon Siege** - Waves of demons attack, use miracles to defend
- **Radical Revolution** - Manage a village on the brink of rebellion
- **Miracle Puzzle** - Limited miracles, solve village problems
- **Survival Mode** - Keep villagers alive through harsh winters
- **Hero Quest** - Single hero villager completes objectives

---

## Godgame-Specific Entities

### Base Templates Provided

**Villagers**:
```csharp
// Players can customize these
BaseVillager - Generic villager (player sets stats, jobs)
BaseFarmer - Farming specialist
BaseGuard - Combat specialist
BasePriest - Faith specialist
BaseSmith - Crafting specialist
BaseRadical - Pre-radicalized villager
```

**Enemies**:
```csharp
BaseDemon - Generic demon (player customizes appearance)
WeakDemon - Low health, fast
TankDemon - High health, slow
FlyingDemon - Can cross water
BossDemon - Powerful, unique abilities
```

**Buildings**:
```csharp
BaseHouse - Villager housing
BaseFarm - Food production
BaseBarracks - Guard training
BaseTemple - Faith production
BaseWorkshop - Item crafting
```

**Items**:
```csharp
BaseWeapon - Combat item
BaseTool - Gathering tool
BaseFood - Consumable
BaseBuff - Temporary effect
```

### Custom Entity Example

**Creating "Elite Guard"** in editor:

```json
{
  "EntityId": "EliteGuard",
  "DisplayName": "Elite Temple Guardian",
  "BaseTemplate": "BaseGuard",
  "ComponentOverrides": [
    { "ComponentType": "HealthComponent", "FieldName": "MaxHealth", "Value": "200" },
    { "ComponentType": "CombatStats", "FieldName": "AttackDamage", "Value": "50" },
    { "ComponentType": "CombatStats", "FieldName": "Armor", "Value": "30" },
    { "ComponentType": "SkillLevel", "FieldName": "CombatSkill", "Value": "80" },
    { "ComponentType": "VillagerFaith", "FieldName": "FaithLevel", "Value": "1.0" }
  ],
  "VisualData": {
    "MeshRef": "models/guard_elite.fbx",
    "TextureRef": "textures/guard_gold_armor.png"
  }
}
```

**Result**: Guard with 200 HP, 50 damage, 30 armor, expert combat skill, maximum faith

---

## Godgame-Specific Triggers

### Custom Events

**Godgame adds these events** to the framework:

```csharp
// Villager events
VillagerBecameRadical = 200,
VillagerLostFaith = 201,
VillagerGainedFaith = 202,
VillagerDied = 203,
VillagerBorn = 204,
VillagerPromoted = 205,      // Job promotion

// Miracle events
MiracleUsed = 210,
MiracleFailed = 211,          // Not enough miracle points
RainStarted = 212,
RainEnded = 213,

// Faith events
FaithReached = 220,
FaithDepleted = 221,
TempleBuilt = 222,

// Radical events
RadicalGroupFormed = 230,
RadicalAttack = 231,
RadicalSuppressed = 232,

// Resource events
FoodDepleted = 240,
HarvestCompleted = 241,
```

### Custom Conditions

```csharp
// Godgame-specific condition checks
VillagerCountInRole(role, min, max)
FaithLevel(comparison, value)
RadicalCount(comparison, value)
MiraclePointsAvailable(comparison, value)
SeasonIs(season)               // Spring, Summer, Fall, Winter
WeatherIs(weather)             // Clear, Rain, Snow
```

### Custom Actions

```csharp
// Godgame-specific actions
SetVillagerJob(villager, job)
SetVillagerFaith(villager, faithLevel)
RadicalizeVillager(villager)
DeradicalizeVillager(villager)
CastMiracle(miracleType, position)
ModifyFaith(amount)
ModifyMiraclePoints(amount)
SetSeason(season)
SetWeather(weather)
TriggerHarvest()
SpawnVillagerBaby(parents)
```

---

## Example Custom Games

### 1. Demon Siege

**Concept**: Waves of demons attack village, use miracles to defend

**Map Setup**:
- Village center with 20 houses, 1 temple
- Demon spawn point at north edge
- Defensive wall (weak) around village
- Forest to west (can use rain miracle to create barrier)

**Starting Conditions**:
```
- 30 villagers (10 farmers, 10 guards, 5 priests, 5 smiths)
- 100 food
- 50 miracle points
- Faith level: 0.8
```

**Triggers**:

```
Trigger: "Spawn Wave 1"
  Event: GameStart + 10 seconds
  Actions:
    - Create 5 WeakDemons at "DemonGate"
    - Create text "Wave 1: Weak demons approach!"
    - Set variable "CurrentWave" = 1

Trigger: "Spawn Wave 2"
  Event: AllEnemiesDefeated
  Condition: CurrentWave == 1
  Actions:
    - Wait 20 seconds
    - Create 10 WeakDemons at "DemonGate"
    - Create 2 TankDemons at "DemonGate"
    - Create text "Wave 2: Stronger demons!"
    - Increment variable "CurrentWave"

Trigger: "Boss Wave"
  Event: AllEnemiesDefeated
  Condition: CurrentWave == 5
  Actions:
    - Wait 30 seconds
    - Create 1 BossDemon at "DemonGate"
    - Create 20 WeakDemons at "DemonGate"
    - Create text "BOSS WAVE! Defend the temple!"
    - Play sound "boss_music.ogg"

Trigger: "Temple Destroyed"
  Event: UnitDies
  Condition: Unit.Type == "Temple"
  Actions:
    - Create text "The temple fell! Faith is lost!"
    - DeclareDefeat

Trigger: "Victory"
  Event: AllEnemiesDefeated
  Condition: CurrentWave == 6
  Actions:
    - Create text "You defended the village! The demons retreat!"
    - AddResource "MiraclePoints" 100
    - DeclareVictory

Trigger: "Low Faith Warning"
  Event: FaithDepleted
  Condition: FaithLevel < 0.3
  Actions:
    - Create text "Faith is low! Build more temples or use blessings!"
    - Play sound "warning.ogg"

Trigger: "Miracle Bonus"
  Event: MiracleUsed
  Condition: MiracleType == "Rain"
  Actions:
    - Create text "Rain miracle! Forest barrier activated!"
    - Create 10 terrain tiles "Water" at "ForestEdge"
    - Play VFX "water_barrier.vfx" at "ForestEdge"
```

**Gameplay**:
1. Demons spawn in waves
2. Player uses miracles (rain, blessings) to help defenders
3. Guards fight demons, priests maintain faith
4. If temple destroyed → lose
5. Survive 6 waves → win

---

### 2. Radical Revolution

**Concept**: Prevent villagers from overthrowing you (the god)

**Map Setup**:
- Large village with 50 villagers
- Central temple (player's power base)
- Outlying farms (discontent areas)
- Secret meeting spot for radicals (hidden cave)

**Starting Conditions**:
```
- 50 villagers (varying jobs)
- 20% villagers slightly radicalized (AlignmentLevel 0.2-0.4)
- 3 villagers highly radicalized (AlignmentLevel 0.7-0.9)
- Low food (40 units, need 100 for everyone)
- Faith level: 0.5 (neutral)
```

**Triggers**:

```
Trigger: "Radical Group Forms"
  Event: RadicalGroupFormed
  Condition: GroupSize >= 5
  Actions:
    - Create text "Radicals are organizing! Faith is at risk!"
    - ModifyFaith -10
    - Play sound "conspiracy.ogg"

Trigger: "Appease Radicals"
  Event: MiracleUsed
  Condition: MiracleType == "Harvest" && FoodLevel > 80
  Actions:
    - For each radical:
        DeradicalizeVillager by 0.2
    - Create text "Bountiful harvest! Radicals are satisfied... for now."
    - ModifyFaith +5

Trigger: "Suppress Radicals"
  Event: UnitDies
  Condition: Unit.IsRadical && Unit.KilledByGuard
  Actions:
    - DeradicalizeVillager all villagers by 0.1  (fear)
    - ModifyFaith -15  (heavy cost)
    - Create text "Harsh measures reduce dissent, but faith suffers."

Trigger: "Radical Attack"
  Event: RadicalAttack
  Condition: RadicalCount >= 10
  Actions:
    - Create text "UPRISING! Radicals attack the temple!"
    - Set all radicals to "AttackTarget" = "Temple"
    - Play sound "alarm.ogg"

Trigger: "Temple Destroyed"
  Event: UnitDies
  Condition: Unit.Type == "Temple"
  Actions:
    - Create text "The radicals won. You are overthrown!"
    - DeclareDefeat

Trigger: "Peaceful Resolution"
  Event: FaithReached
  Condition: FaithLevel >= 0.9 && RadicalCount == 0
  Actions:
    - Create text "The village is united in faith! You win!"
    - DeclareVictory

Trigger: "Total Suppression"
  Event: RadicalSuppressed
  Condition: RadicalCount == 0 && GuardCount >= 15
  Actions:
    - Create text "All radicals eliminated... but at what cost?"
    - If FaithLevel < 0.3:
        DeclareDefeat (pyrrhic victory)
    - Else:
        DeclareVictory
```

**Gameplay**:
- Balance appeasement (miracles, food) vs suppression (guards)
- Radicals spread dissent if not addressed
- Low faith → more radicalization
- High faith → radicals convert back
- Multiple win/lose paths (peaceful vs violent)

---

### 3. Miracle Puzzle

**Concept**: Limited miracles, solve village problems creatively

**Map Setup**:
- Small village (10 villagers)
- Drought (no water sources)
- Blocked paths (rocks, fallen trees)
- Hidden treasure (requires miracle to reveal)

**Starting Conditions**:
```
- 10 villagers (all farmers)
- 3 miracle points (very limited!)
- No food (need to farm, but drought)
- Paths blocked (need to clear)
```

**Puzzle**:
1. Use 1 miracle point: Rain (creates water source)
2. Use 1 miracle point: Lightning (clears rock blocking path)
3. Use 1 miracle point: Blessing (reveals hidden treasure = food)

**Triggers**:

```
Trigger: "Objective: Provide Water"
  Event: GameStart
  Actions:
    - Create text "Objective 1: Provide water for crops"
    - Set variable "Objective1Complete" = false

Trigger: "Water Provided"
  Event: MiracleUsed
  Condition: MiracleType == "Rain"
  Actions:
    - Create terrain "Water" at "DroughtArea"
    - Create text "Objective 1 complete! Now the path is blocked..."
    - Set variable "Objective1Complete" = true

Trigger: "Objective: Clear Path"
  Event: Variable "Objective1Complete"
  Condition: Objective1Complete == true
  Actions:
    - Create text "Objective 2: Clear the blocked path"
    - Set variable "Objective2Complete" = false

Trigger: "Path Cleared"
  Event: MiracleUsed
  Condition: MiracleType == "Lightning" && TargetNear "BlockedPath"
  Actions:
    - Remove terrain obstacle at "BlockedPath"
    - Create text "Path cleared! But no food yet..."
    - Set variable "Objective2Complete" = true

Trigger: "Objective: Find Food"
  Event: Variable "Objective2Complete"
  Condition: Objective2Complete == true
  Actions:
    - Create text "Objective 3: Provide food for villagers"

Trigger: "Food Revealed"
  Event: MiracleUsed
  Condition: MiracleType == "Blessing" && TargetNear "HiddenTreasure"
  Actions:
    - Create item "Food" × 100 at "HiddenTreasure"
    - Create text "Treasure revealed! The village is saved!"
    - DeclareVictory

Trigger: "Waste Miracle"
  Event: MiracleUsed
  Condition: MiraclePointsAvailable == 0 && AnyObjectiveIncomplete
  Actions:
    - Create text "Out of miracle points! Puzzle failed."
    - DeclareDefeat
```

**Challenge**: Use exactly 3 miracles in correct order to solve puzzle

---

## Godgame Editor Features

### Villager Customization Panel

```
┌──────────────────────────────────────┐
│ Villager Editor: Elite Guard         │
├──────────────────────────────────────┤
│ Base Template: [BaseGuard ▼]        │
│                                      │
│ Stats:                               │
│   Health:    [200]    (Base: 100)   │
│   Damage:    [50]     (Base: 25)    │
│   Armor:     [30]     (Base: 10)    │
│   Speed:     [5]      (Base: 5)     │
│                                      │
│ Skills:                              │
│   Combat:    [80]     (Base: 50)    │
│   Faith:     [100]    (Base: 50)    │
│   Crafting:  [20]     (Base: 20)    │
│                                      │
│ Personality:                         │
│   Alignment: [Loyal ▼]              │
│   Job:       [Guard ▼]              │
│                                      │
│ Visual:                              │
│   Mesh:      [guard_elite.fbx]      │
│   Texture:   [gold_armor.png]       │
│                                      │
│ [Save] [Cancel] [Test in Scene]     │
└──────────────────────────────────────┘
```

### Miracle Configuration

```
┌──────────────────────────────────────┐
│ Miracle Settings                     │
├──────────────────────────────────────┤
│ Starting Miracle Points: [50]        │
│ Miracle Regen Rate: [1 per 10 sec]  │
│                                      │
│ Available Miracles:                  │
│   [✓] Rain       Cost: [10]         │
│   [✓] Blessing   Cost: [15]         │
│   [✓] Lightning  Cost: [20]         │
│   [✓] Harvest    Cost: [25]         │
│   [ ] Custom...                      │
│                                      │
│ Miracle Cooldowns:                   │
│   Rain:       [30 seconds]          │
│   Blessing:   [60 seconds]          │
│   Lightning:  [45 seconds]          │
│   Harvest:    [120 seconds]         │
└──────────────────────────────────────┘
```

### Radical Mechanics Settings

```
┌──────────────────────────────────────┐
│ Radical System Settings              │
├──────────────────────────────────────┤
│ Radicalization Enabled: [✓]         │
│                                      │
│ Radicalization Factors:              │
│   Low Food:        [+0.1 per day]   │
│   Low Faith:       [+0.2 per day]   │
│   Guard Brutality: [+0.3 per event] │
│   High Taxes:      [+0.15 per day]  │
│                                      │
│ De-radicalization Factors:           │
│   Blessings:       [-0.2 per cast]  │
│   Good Harvest:    [-0.1 per day]   │
│   Temple Presence: [-0.05 per day]  │
│                                      │
│ Radical Threshold: [0.5]  (0-1)     │
│   (Villager becomes radical above)  │
│                                      │
│ [Preview Radicalization Rate]        │
└──────────────────────────────────────┘
```

---

## Distribution & Community

### In-Game Mod Browser

```
┌──────────────────────────────────────────┐
│ Godgame Community Maps                   │
├──────────────────────────────────────────┤
│ [Featured] [Most Popular] [New] [Search] │
├──────────────────────────────────────────┤
│                                          │
│ ┌────────────────────────────────────┐  │
│ │ Demon Siege                        │  │
│ │ By: PlayerName123                  │  │
│ │ ★★★★★ (4.8 / 2,451 ratings)       │  │
│ │                                    │  │
│ │ Defend your village from 10 waves │  │
│ │ of demons. Use miracles wisely!   │  │
│ │                                    │  │
│ │ [Play] [Download] [Rate]          │  │
│ └────────────────────────────────────┘  │
│                                          │
│ ┌────────────────────────────────────┐  │
│ │ Radical Revolution                 │  │
│ │ By: ModMaster99                    │  │
│ │ ★★★★☆ (4.2 / 1,102 ratings)       │  │
│ │                                    │  │
│ │ Can you prevent a rebellion?       │  │
│ │ Appease or suppress radicals!     │  │
│ │                                    │  │
│ │ [Play] [Download] [Rate]          │  │
│ └────────────────────────────────────┘  │
│                                          │
└──────────────────────────────────────────┘
```

### Steam Workshop Integration

- Players upload maps to Steam Workshop
- Auto-sync with in-game browser
- Version control (updates propagate automatically)
- Comments & ratings

---

## Technical Integration

### Godgame-Specific Systems

```csharp
// Godgame provides these extensions to framework

[UpdateInGroup(typeof(ModRuntimeGroup))]
public partial class GodgameModEventSystem : SystemBase
{
    // Emit Godgame-specific events
    protected override void OnUpdate()
    {
        // Listen for radicalization
        Entities
            .WithChangeFilter<RadicalAlignment>()
            .ForEach((Entity e, in RadicalAlignment alignment) =>
            {
                if (alignment.AlignmentLevel > 0.5f)
                {
                    EmitEvent(new ModEvent
                    {
                        EventType = (TriggerEventType)200, // VillagerBecameRadical
                        TriggeringEntity = e
                    });
                }
            }).Run();

        // Other Godgame events...
    }
}

[UpdateInGroup(typeof(ModRuntimeGroup))]
public partial class GodgameTriggerActionSystem : SystemBase
{
    // Execute Godgame-specific actions
    public void ExecuteAction(TriggerAction action)
    {
        switch ((GodgameActionType)action.ActionType)
        {
            case GodgameActionType.RadicalizeVillager:
                // Find villager, set RadicalAlignment = 1.0
                break;

            case GodgameActionType.CastMiracle:
                // Parse miracle type, position
                // Emit miracle event to MiracleSystem
                break;

            // Other Godgame actions...
        }
    }
}
```

---

## Summary

**Godgame modding enables**:
- ✅ Custom village scenarios
- ✅ Demon siege / tower defense
- ✅ Radical management challenges
- ✅ Miracle puzzles
- ✅ Story-driven campaigns

**Players can customize**:
- Villager stats, jobs, personalities
- Demon types and abilities
- Miracle availability and costs
- Radicalization mechanics
- Victory/defeat conditions

**Distribution**:
- In-game mod browser
- Steam Workshop integration
- Community ratings & featured mods

---

**See Also**:
- [PureDOTS ModdingAndEditorFramework](../../../../Packages/com.moni.puredots/Documentation/DesignNotes/ModdingAndEditorFramework.md)
- [Space4X Custom Game Modding](../../Space4X/Docs/Systems/CustomGameModding.md)
