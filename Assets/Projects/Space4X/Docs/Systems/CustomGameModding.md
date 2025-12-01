# Space4X Custom Game Modding (UGC System)

**Game**: Space4X (4X Space Strategy)
**Framework**: Uses [PureDOTS ModdingAndEditorFramework](../../../../Packages/com.moni.puredots/Documentation/DesignNotes/ModdingAndEditorFramework.md)
**Created**: 2025-11-27

---

## Overview

Players can create custom Space4X scenarios using the in-game editor - from fleet battles to diplomatic crises to exploration missions.

**Example Custom Modes**:
- **The Gauntlet** - Run a gauntlet of enemy fleets
- **Diplomacy Crisis** - Prevent war through careful negotiation
- **Exploration Mission** - Find artifacts while avoiding hazards
- **Fleet Defense** - Tower defense with space fleets
- **Battle Royale** - Last fleet standing wins

---

## Space4X-Specific Entities

### Base Templates Provided

**Ships**:
```csharp
// Players can customize these
BaseCarrier - Large capital ship (player sets modules)
BaseDestroyer - Medium combat ship
BaseCorvette - Fast scout ship
BaseFighter - Small, nimble ship
BaseBomber - High damage, fragile
BaseStation - Stationary defense platform
```

**Modules** (for carriers/destroyers):
```csharp
BaseWeaponModule - Guns, lasers, missiles
BaseShieldModule - Energy shields
BaseEngineModule - Propulsion, maneuverability
BaseHangarModule - Fighter/bomber bay
BaseRepairModule - Self-repair systems
BaseSensorModule - Detection range
```

**Hazards**:
```csharp
BaseAsteroid - Static obstacle
BaseDebrisField - Slows movement
BaseSolarStorm - Damage over time (avoid shadow)
BaseBlackHole - Gravitational pull
BaseMine - Stationary explosive
```

**Stations & Structures**:
```csharp
BaseSpaceStation - Refuel/repair point
BaseJumpGate - Fast travel
BaseArtifact - Collectible objective
BaseBeacon - Waypoint marker
```

### Custom Entity Example

**Creating "Stealth Bomber"** in editor:

```json
{
  "EntityId": "StealthBomber",
  "DisplayName": "Shadow Strike Bomber",
  "BaseTemplate": "BaseBomber",
  "ComponentOverrides": [
    { "ComponentType": "HealthComponent", "FieldName": "MaxHealth", "Value": "80" },
    { "ComponentType": "CombatStats", "FieldName": "AttackDamage", "Value": "300" },
    { "ComponentType": "MovementModel", "FieldName": "MaxSpeed", "Value": "15" },
    { "ComponentType": "SensorComponent", "FieldName": "DetectionRadius", "Value": "5" },
    { "ComponentType": "StealthComponent", "FieldName": "StealthRating", "Value": "0.9" }
  ],
  "VisualData": {
    "MeshRef": "models/bomber_stealth.fbx",
    "TextureRef": "textures/black_hull.png",
    "VFXRef": "vfx/cloak_shimmer.vfx"
  }
}
```

**Result**: Low-health bomber with massive damage, hard to detect (stealth 0.9)

---

## Space4X-Specific Triggers

### Custom Events

**Space4X adds these events** to the framework:

```csharp
// Fleet events
FleetEntersSystem = 300,
FleetLeavesSystem = 301,
FleetEngagesEnemy = 302,
FleetRetreats = 303,
FleetDestroyed = 304,

// Diplomacy events
DiplomaticIncident = 310,
BorderViolation = 311,
TradeAgreementSigned = 312,
WarDeclared = 313,
PeaceTreaty = 314,
SanctionsApplied = 315,

// Technology events
TechResearched = 320,
TechUnlocked = 321,

// Exploration events
ArtifactDiscovered = 330,
JumpGateActivated = 331,
BeaconFound = 332,

// Combat events
CarrierModuleDestroyed = 340,
FightersLaunched = 341,
ShipCloaked = 342,
ShipDecloaked = 343,

// Hazards
EnteredSolarStorm = 350,
EnteredAsteroidField = 351,
HitByMine = 352,
```

### Custom Conditions

```csharp
// Space4X-specific condition checks
FleetSize(fleet, min, max)
FleetOwner(fleet, owner)
CarrierModuleIntegrity(carrier, moduleType, min, max)
DiplomaticRelation(faction1, faction2, comparison, value)
TechLevel(faction, techId, comparison, value)
InSolarStormShadow(fleet)
WithinJumpRange(fleet, target)
```

### Custom Actions

```csharp
// Space4X-specific actions
CreateFleet(fleetTemplate, position, owner)
MoveFleet(fleet, destination)
JumpFleet(fleet, destination)      // Micro-jump
AttackTarget(fleet, target)
LaunchFighters(carrier, count)
RecallFighters(carrier)
CloakFleet(fleet)
DecloakFleet(fleet)
RepairShip(ship, amount)
AddDiplomaticRelation(faction1, faction2, modifier)
ApplySanctions(faction)
UnlockTech(faction, techId)
ActivateJumpGate(gate)
TriggerSolarStorm(region)
```

---

## Example Custom Games

### 1. The Gauntlet

**Concept**: Navigate through enemy fleet ambushes to reach extraction point

**Map Setup**:
- Long corridor with 5 checkpoints
- Enemy fleets at each checkpoint (increasing difficulty)
- Asteroid fields (provide cover)
- Solar storm zones (must use shadows)
- Final extraction point (jump gate)

**Starting Conditions**:
```
- Player fleet: 1 Carrier, 3 Destroyers, 5 Corvettes
- All ships at 100% health
- No resupply (damaged ships stay damaged)
- No tech unlocks (use what you have)
```

**Triggers**:

```
Trigger: "Checkpoint 1 - Scout Ambush"
  Event: FleetEntersRegion "Checkpoint1"
  Condition: Fleet.Owner == LocalPlayer
  Actions:
    - Create 5 EnemyCorvettes at "Ambush1Point"
    - Create text "Ambush! Scout fighters detected!"
    - Play sound "alarm.ogg"

Trigger: "Checkpoint 2 - Destroyer Gauntlet"
  Event: FleetEntersRegion "Checkpoint2"
  Condition: Fleet.Owner == LocalPlayer
  Actions:
    - Create 3 EnemyDestroyers at "Ambush2PointA"
    - Create 3 EnemyDestroyers at "Ambush2PointB"
    - Create text "Destroyers flanking! Use jump drive!"

Trigger: "Checkpoint 3 - Solar Storm"
  Event: FleetEntersRegion "Checkpoint3"
  Actions:
    - TriggerSolarStorm "Checkpoint3Zone"
    - Create text "Solar storm! Hide behind asteroids!"
    - Wait 30 seconds
    - Create 2 EnemyCarriers at "Ambush3Point"

Trigger: "Checkpoint 4 - Minefield"
  Event: FleetEntersRegion "Checkpoint4"
  Actions:
    - Create 50 mines in "Checkpoint4Zone" (scattered)
    - Create text "Minefield! Navigate carefully!"

Trigger: "Checkpoint 5 - Boss Fleet"
  Event: FleetEntersRegion "Checkpoint5"
  Actions:
    - Create 1 BossCarrier at "BossSpawn"
    - Create 5 EnemyDestroyers at "BossSpawn"
    - Create 10 EnemyFighters at "BossSpawn"
    - Create text "BOSS FLEET! Final obstacle!"
    - Play sound "boss_music.ogg"

Trigger: "Extraction - Victory"
  Event: FleetEntersRegion "ExtractionPoint"
  Condition: Fleet.Owner == LocalPlayer && AllEnemiesDefeated
  Actions:
    - ActivateJumpGate "ExtractionGate"
    - Create text "Jump gate activated! You survived the gauntlet!"
    - DeclareVictory

Trigger: "Fleet Destroyed - Defeat"
  Event: FleetDestroyed
  Condition: Fleet.Owner == LocalPlayer
  Actions:
    - Create text "Your fleet was destroyed. Mission failed."
    - DeclareDefeat

Trigger: "Shadow Bonus"
  Event: EnteredSolarStorm
  Condition: InSolarStormShadow(PlayerFleet)
  Actions:
    - Create text "In shadow! Storm damage nullified."
    - RepairShip "All" 10  (bonus for smart play)
```

**Gameplay**:
1. Fight through 5 increasingly difficult checkpoints
2. Use jump drive to flank enemies
3. Hide in asteroid shadows during solar storm
4. Navigate minefield carefully
5. Defeat boss fleet
6. Reach extraction → win

---

### 2. Fleet Defense (Tower Defense)

**Concept**: Defend space station from waves of attackers

**Map Setup**:
- Central space station (player's base)
- Multiple approach lanes (3 paths)
- Build zones (deploy defense platforms)
- Resource nodes (capture for credits)

**Starting Conditions**:
```
- 1 Space Station (must protect)
- 1,000 Credits (buy defense platforms)
- No starting fleet (must build)
```

**Triggers**:

```
Trigger: "Wave 1 - Scouts"
  Event: TimeElapsed 10 seconds
  Actions:
    - Create 10 EnemyCorvettes at "Lane1Spawn"
    - Create text "Wave 1: Scout fighters approaching!"
    - Set variable "CurrentWave" = 1

Trigger: "Wave 2 - Destroyers"
  Event: AllEnemiesDefeated
  Condition: CurrentWave == 1
  Actions:
    - Wait 20 seconds
    - Create 5 EnemyDestroyers at "Lane2Spawn"
    - Create 5 EnemyDestroyers at "Lane3Spawn"
    - Create text "Wave 2: Destroyers from multiple lanes!"
    - Increment variable "CurrentWave"

Trigger: "Wave 10 - Boss"
  Event: AllEnemiesDefeated
  Condition: CurrentWave == 9
  Actions:
    - Wait 30 seconds
    - Create 1 BossCarrier at "Lane1Spawn"
    - Create 3 BossCarrier at "Lane2Spawn"
    - Create 3 BossCarrier at "Lane3Spawn"
    - Create text "FINAL WAVE! Defend the station!"

Trigger: "Build Defense Platform"
  Event: UnitSpawned
  Condition: Unit.Type == "DefensePlatform" && Unit.InBuildZone
  Actions:
    - AddResource "Credits" -200
    - Create text "Defense platform built! Credits: {Credits}"

Trigger: "Capture Resource Node"
  Event: FleetEntersRegion "ResourceNode"
  Condition: Fleet.Owner == LocalPlayer
  Actions:
    - Set "ResourceNode.Owner" = LocalPlayer
    - Create text "Resource node captured! +50 Credits/wave"

Trigger: "Resource Income"
  Event: AllEnemiesDefeated
  Condition: AnyResourceNodeOwned
  Actions:
    - AddResource "Credits" (50 × OwnedNodeCount)
    - Create text "Income: +{Income} Credits"

Trigger: "Station Destroyed - Defeat"
  Event: UnitDies
  Condition: Unit.Type == "SpaceStation"
  Actions:
    - Create text "The station was destroyed! Mission failed!"
    - DeclareDefeat

Trigger: "Victory"
  Event: AllEnemiesDefeated
  Condition: CurrentWave == 10
  Actions:
    - Create text "All waves defeated! The station is safe!"
    - DeclareVictory
```

**Gameplay**:
1. Enemies approach from 3 lanes
2. Player builds defense platforms (cost credits)
3. Capture resource nodes for income
4. Upgrade platforms between waves
5. Defend station for 10 waves → win

---

### 3. Diplomacy Crisis

**Concept**: Prevent war between 3 factions through negotiation

**Map Setup**:
- 3 factions: Alliance, Empire, Federation
- Border regions (potential flashpoints)
- Trade routes (economic incentive for peace)
- Military bases (escalation risk)

**Starting Conditions**:
```
- Player is neutral mediator
- All factions at neutral relations (0.5)
- Random border incidents occur
- Player has no military (diplomacy only)
```

**Triggers**:

```
Trigger: "Border Incident 1"
  Event: TimeElapsed 30 seconds
  Actions:
    - Create text "Alliance ship crossed Empire border! Tensions rise!"
    - AddDiplomaticRelation "Alliance" "Empire" -0.2
    - Set variable "IncidentCount" += 1

Trigger: "Border Incident 2"
  Event: TimeElapsed 60 seconds
  Condition: DiplomaticRelation("Empire", "Federation") > 0.3
  Actions:
    - Create text "Empire fleet near Federation space! Federation protests!"
    - AddDiplomaticRelation "Empire" "Federation" -0.3

Trigger: "Player Mediates"
  Event: CustomEvent "PlayerMediatesIncident"
  Condition: PlayerInfluence > 50
  Actions:
    - AddDiplomaticRelation "Alliance" "Empire" +0.3
    - Create text "Successful mediation! Tensions ease."
    - AddResource "Influence" -50

Trigger: "Trade Agreement"
  Event: TradeAgreementSigned
  Condition: Between any 2 factions
  Actions:
    - AddDiplomaticRelation (Signatories) +0.2
    - Create text "{Faction1} and {Faction2} sign trade deal!"

Trigger: "War Declared"
  Event: WarDeclared
  Actions:
    - Create text "WAR! Diplomacy has failed!"
    - DeclareDefeat

Trigger: "Sanctions Escalation"
  Event: SanctionsApplied
  Actions:
    - AddDiplomaticRelation (Target, Sanctioner) -0.4
    - Create text "Sanctions applied! War risk increases!"
    - If DiplomaticRelation < 0.1:
        Trigger "WarDeclared" in 20 seconds

Trigger: "Peace Treaty"
  Event: PeaceTreaty
  Condition: AllFactionsRelations > 0.7
  Actions:
    - Create text "All factions sign peace treaty! Crisis averted!"
    - DeclareVictory

Trigger: "Influence Depleted"
  Event: ResourceChanged "Influence"
  Condition: Influence <= 0 && AnyRelation < 0.3
  Actions:
    - Create text "No influence left to mediate! War is inevitable."
    - DeclareDefeat
```

**Gameplay**:
1. Random border incidents occur
2. Player spends influence to mediate
3. Encourage trade agreements (boost relations)
4. Prevent sanctions (escalation risk)
5. Get all factions to peace treaty → win
6. Any war declaration → lose

---

## Space4X Editor Features

### Fleet Designer

```
┌──────────────────────────────────────┐
│ Fleet Designer: Elite Strike Force   │
├──────────────────────────────────────┤
│ Fleet Composition:                   │
│                                      │
│ [+] Carriers:     [1]  Type: [Heavy▼] │
│     Modules:                         │
│       - Weapon Module × 3            │
│       - Shield Module × 2            │
│       - Hangar Module × 1            │
│                                      │
│ [+] Destroyers:   [3]  Type: [Fast▼]  │
│     Loadout: [Balanced▼]            │
│                                      │
│ [+] Corvettes:    [5]  Type: [Scout▼] │
│     Loadout: [Speed▼]               │
│                                      │
│ Total Fleet Value: [2,500 Credits]   │
│ Estimated Combat Power: [Medium]     │
│                                      │
│ [Save as Template] [Test in Battle]  │
└──────────────────────────────────────┘
```

### Carrier Module Editor

```
┌──────────────────────────────────────┐
│ Carrier Module Configuration         │
├──────────────────────────────────────┤
│ Carrier: Heavy Battle Carrier        │
│                                      │
│ Module Slots: [6/6 used]            │
│                                      │
│ Slot 1: [Weapon Module ▼]           │
│   Type: Railgun Battery             │
│   Damage: 50   Range: 20            │
│                                      │
│ Slot 2: [Weapon Module ▼]           │
│   Type: Missile Launcher            │
│   Damage: 80   Range: 30            │
│                                      │
│ Slot 3: [Weapon Module ▼]           │
│   Type: Point Defense               │
│   Damage: 10   Range: 5             │
│                                      │
│ Slot 4: [Shield Module ▼]           │
│   Shield HP: 500   Regen: 10/sec    │
│                                      │
│ Slot 5: [Shield Module ▼]           │
│   Shield HP: 500   Regen: 10/sec    │
│                                      │
│ Slot 6: [Hangar Module ▼]           │
│   Capacity: 20 fighters             │
│                                      │
│ Total Stats:                         │
│   HP: 2000   Shields: 1000          │
│   DPS: 140   Hangar: 20             │
└──────────────────────────────────────┘
```

### Diplomacy Scenario Designer

```
┌──────────────────────────────────────┐
│ Diplomacy Scenario: Border Crisis    │
├──────────────────────────────────────┤
│ Factions:                            │
│   [+] Alliance     Relations: [0.5] │
│   [+] Empire       Relations: [0.5] │
│   [+] Federation   Relations: [0.5] │
│                                      │
│ Starting Incidents:                  │
│   [ ] Border violation (random)      │
│   [ ] Trade dispute                  │
│   [ ] Military buildup               │
│   Frequency: [Every 30-60 seconds]  │
│                                      │
│ Player Resources:                    │
│   Influence Points: [100]           │
│   Income Rate: [+5 per minute]      │
│                                      │
│ Victory Conditions:                  │
│   [✓] All relations > 0.7 for 60s   │
│   [ ] Trade agreement between all   │
│                                      │
│ Defeat Conditions:                   │
│   [✓] Any war declared              │
│   [✓] Influence depleted + crisis   │
│                                      │
│ [Test Scenario] [Save]              │
└──────────────────────────────────────┘
```

---

## Distribution & Community

### In-Game Mod Browser (Space4X)

```
┌──────────────────────────────────────────┐
│ Space4X Community Scenarios              │
├──────────────────────────────────────────┤
│ [Featured] [Most Popular] [New] [Search] │
├──────────────────────────────────────────┤
│                                          │
│ ┌────────────────────────────────────┐  │
│ │ The Gauntlet                       │  │
│ │ By: SpaceAce42                     │  │
│ │ ★★★★★ (4.9 / 3,821 ratings)       │  │
│ │                                    │  │
│ │ Survive 5 ambushes with one fleet.│  │
│ │ High difficulty, strategic play!   │  │
│ │                                    │  │
│ │ [Play] [Download] [Rate]          │  │
│ └────────────────────────────────────┘  │
│                                          │
│ ┌────────────────────────────────────┐  │
│ │ Fleet Defense                      │  │
│ │ By: TowerDefMaster                 │  │
│ │ ★★★★☆ (4.6 / 2,104 ratings)       │  │
│ │                                    │  │
│ │ Tower defense in space! 10 waves. │  │
│ │ Build platforms, defend station!  │  │
│ │                                    │  │
│ │ [Play] [Download] [Rate]          │  │
│ └────────────────────────────────────┘  │
│                                          │
└──────────────────────────────────────────┘
```

---

## Technical Integration

### Space4X-Specific Systems

```csharp
// Space4X provides these extensions to framework

[UpdateInGroup(typeof(ModRuntimeGroup))]
public partial class Space4XModEventSystem : SystemBase
{
    // Emit Space4X-specific events
    protected override void OnUpdate()
    {
        // Listen for border violations
        Entities
            .WithAll<FleetComponent>()
            .ForEach((Entity fleet, in LocalTransform transform, in OwnerComponent owner) =>
            {
                if (IsInEnemyTerritory(transform.Position, owner.FactionId))
                {
                    EmitEvent(new ModEvent
                    {
                        EventType = (TriggerEventType)311, // BorderViolation
                        TriggeringEntity = fleet
                    });
                }
            }).Run();

        // Other Space4X events...
    }
}

[UpdateInGroup(typeof(ModRuntimeGroup))]
public partial class Space4XTriggerActionSystem : SystemBase
{
    // Execute Space4X-specific actions
    public void ExecuteAction(TriggerAction action)
    {
        switch ((Space4XActionType)action.ActionType)
        {
            case Space4XActionType.JumpFleet:
                // Parse fleet entity, destination
                // Execute micro-jump
                break;

            case Space4XActionType.AddDiplomaticRelation:
                // Modify diplomatic relation between factions
                break;

            // Other Space4X actions...
        }
    }
}
```

---

## Summary

**Space4X modding enables**:
- ✅ Custom fleet battles
- ✅ Tower defense with fleets
- ✅ Diplomacy scenarios
- ✅ Exploration missions
- ✅ Battle royale / competitive modes

**Players can customize**:
- Fleet compositions and loadouts
- Carrier modules and abilities
- Diplomatic starting conditions
- Hazard placement (storms, asteroids, mines)
- Victory/defeat conditions

**Distribution**:
- In-game scenario browser
- Steam Workshop integration
- Community ratings & featured scenarios

---

**See Also**:
- [PureDOTS ModdingAndEditorFramework](../../../../Packages/com.moni.puredots/Documentation/DesignNotes/ModdingAndEditorFramework.md)
- [Godgame Custom Game Modding](../../Godgame/Docs/Systems/CustomGameModding.md)
