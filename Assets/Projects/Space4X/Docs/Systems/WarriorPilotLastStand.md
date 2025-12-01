# Warrior Pilot's Last Stand: Kamikaze Boarding

**Status**: Concept Design
**Last Updated**: 2025-11-29
**Game**: Space4X
**Category**: Cultural Mechanic + Combat System

---

## Overview

**Warrior Pilot's Last Stand** is a cultural trait system where pilots from warlike/honorable factions perform desperate kamikaze boarding actions when their strike craft is critically damaged. Instead of ejecting or retreating, these pilots crash their dying craft into enemy capital ships, becoming impromptu boarding parties.

**Core Fantasy**: "I may die, but I will die **inside** the enemy ship, sowing chaos until my last breath."

---

## Cultural Foundation

### Faction Cultures with Last Stand

Not all factions have this trait—it's alignment/outlook-dependent:

| Alignment Profile | Culture Name | Last Stand Variant |
|-------------------|--------------|-------------------|
| **Good + Lawful + War** | Honorbound Knights | "Death Before Dishonor" (seek glorious end) |
| **Evil + Chaotic + War** | Berserker Clans | "Blood for Blood" (maximum carnage) |
| **Neutral + Authoritarian + War** | Imperial Legions | "Service Unto Death" (obey to the end) |
| **Good + Fanatic + Purity** | Zealot Crusaders | "Martyrdom's Embrace" (die for the cause) |

**Cultures WITHOUT Last Stand**:
- Pragmatic/Opportunist: Eject and live to fight another day
- Scholarly/Methodical: Value their lives, strategic retreat
- Corrupt/Materialist: Prefer survival over honor

### Cultural Trait Component

```csharp
public struct CulturalLastStandTrait : IComponentData
{
    public LastStandVariant Variant;   // How they perform the action
    public float Probability;          // 0-1, chance of triggering
    public float GlorySeekingLevel;    // 0-1, how much they seek dramatic death
    public float SurvivalInstinct;     // 0-1, competing with glory (tension)
    public byte MinimumDamageThreshold; // % hull damage to trigger (default: 75%)
}

public enum LastStandVariant : byte
{
    DeathBeforeDishonor,    // Seek honorable combat death
    BloodForBlood,          // Maximum enemy casualties
    ServiceUntoDeath,       // Complete mission objective before dying
    MartyrdomsEmbrace       // Sacrifice for greater cause
}
```

---

## Trigger Conditions

### Critical Damage Threshold

**Activation criteria**:
1. Strike craft hull <25% (critically damaged)
2. Pilot has `CulturalLastStandTrait` component
3. Enemy capital ship within ramming range (500m)
4. Pilot passes decision roll (based on alignment + culture)

**Decision Logic**:
```csharp
float decisionThreshold =
    culturalTrait.Probability * 0.5f +
    (1.0f - culturalTrait.SurvivalInstinct) * 0.3f +
    pilot.AlignmentStrength * 0.2f;

// Modifiers:
if (pilot.Outlook == Fanatic) decisionThreshold += 0.3f;
if (pilot.BoldScore > 60) decisionThreshold += 0.2f;
if (pilot.Loyalty > 150) decisionThreshold += 0.1f;  // Loyal to faction/cause

// Competing factors:
if (pilot.Morale < 30) decisionThreshold -= 0.2f;  // Too demoralized
if (pilot.HasFamily) decisionThreshold -= 0.15f;   // Hesitation (loved ones)
if (pilot.Craven) decisionThreshold -= 0.4f;       // Fear overrides honor

// Roll:
if (random.NextFloat() < decisionThreshold) {
    InitiateLastStand();
}
```

---

## The Crash: Entry Mechanics

### Ramming Calculation

**Pilot maneuvers craft toward capital ship**:
- **Target selection**: Player-directed (manual aim) or AI-chooses (priority systems)
- **Impact location**: Determined by approach vector + directional damage system
- **Crash damage**: Explosive impact on capital ship hull

**Damage calculation**:
```csharp
float crashDamage =
    strikeCraft.RemainingMass * strikeCraft.Velocity * 0.5f +  // Kinetic energy
    strikeCraft.FuelRemaining * 10f +                          // Explosion from fuel
    strikeCraft.OrdnanceRemaining * 20f;                       // Munitions detonation

// Apply to capital ship's directional armor at impact point
ApplyDirectionalDamage(capitalShip, impactLocation, crashDamage);
```

**Impact locations** (directional damage system):
| Location | Systems Accessible | Narrative |
|----------|-------------------|-----------|
| **Bridge** | Command, sensors, communications | "Pilot crashes into bridge, chaos ensues" |
| **Engine Bay** | Propulsion, power generation | "Saboteur in the engine room" |
| **Weapons Battery** | Turrets, missile launchers | "Disabling our guns from within" |
| **Hangar** | Strike craft, crew quarters | "Loose in the hangars, slaughtering pilots" |
| **Hull Breach** | Random section, external access | "Sealed in damaged section, isolated" |

### Pilot Survival Roll

**Does pilot survive the crash?**
```csharp
float survivalProbability =
    pilot.Constitution * 0.4f +           // Physical durability
    strikeCraft.ArmorRemaining * 0.3f +   // Craft protection
    impactAngle * 0.2f +                  // Glancing vs. head-on
    luck * 0.1f;

if (random.NextFloat() < survivalProbability) {
    pilot.Health = math.max(5, pilot.Health - crashDamage * 0.5f);  // Injured but alive
    SpawnPilotInside(capitalShip, impactLocation);
} else {
    KillPilot(pilot);  // Died on impact
    NotifyFaction(pilot.Faction, "martyr", pilot.Name);
}
```

---

## Inside the Enemy Ship: Boarding Actions

### Pilot Behavior States

Once inside, pilot can choose (AI-driven or player-directed):

#### 1. Join Boarding Party
**Prerequisites**: Existing boarding party in ship
**Behavior**:
- Navigate to nearest allied boarding squad
- Join as member (adds combat power)
- Follow squad orders
- Share benefits of organized assault

**Benefits**:
- Higher survival chance (strength in numbers)
- Coordinated sabotage
- Shared loot/glory

**Drawbacks**:
- May not reach ideal sabotage targets (squad priorities differ)
- Share glory (individual reputation gain reduced)

#### 2. Solo Sabotage (Stealth)
**Prerequisites**: None
**Behavior**:
- Avoid detection (stealth movement)
- Sabotage critical systems (engines, weapons, life support)
- Lay low in maintenance shafts, cargo holds
- Wait for ship capture or escape opportunity

**Benefits**:
- Maximum system disruption
- Lower detection risk (if skilled)
- Can sabotage high-value targets (engine core, reactor)

**Drawbacks**:
- Vulnerable if discovered (no backup)
- May be trapped if ship destroys/escapes
- Requires high skill (Stealth, Engineering)

#### 3. Rampage (Glory Death)
**Prerequisites**: Fanatic/Berserker outlook, GlorySeekingLevel >0.7
**Behavior**:
- Aggressive combat against all crew encountered
- Seek bridge/command to kill officers
- Maximize casualties before death
- No self-preservation

**Benefits**:
- Massive crew casualties (demoralizes enemy)
- May kill high-value targets (captain, officers)
- Legendary reputation (posthumous)
- Inspires allies ("Witness the warrior's glory!")

**Drawbacks**:
- Almost guaranteed death
- Quick detection and response
- May trigger ship-wide alert (hurts other boarding parties)

#### 4. Lay Low (Survival)
**Prerequisites**: High SurvivalInstinct, Pragmatic outlook
**Behavior**:
- Hide in cargo/maintenance areas
- Avoid all contact
- Wait for ship capture or rescue
- Scavenge supplies to survive

**Benefits**:
- Highest survival chance
- May gather intel on ship layout
- Can ambush later if opportunity arises

**Drawbacks**:
- No immediate impact on battle
- Risk of starvation/detection over time
- Dishonor (if culture values glory)

### Sabotage Targets

Based on impact location and pilot skills:

```csharp
public struct SabotageTarget : IComponentData
{
    public SabotageType Type;
    public float Difficulty;           // 0-1, skill check required
    public float TimeToSabotage;       // Seconds to complete
    public float ImpactSeverity;       // How much damage it does
    public bool RequiresTools;         // Needs engineering kit
}

public enum SabotageType : byte
{
    DisableWeapon,       // Turret/missile launcher
    OverloadReactor,     // Explosive chain reaction (high risk)
    VentAtmosphere,      // Depressurize sections (kills crew)
    JamSensors,          // Blind ship to incoming attacks
    SabotageEngines,     // Immobilize ship
    DisableComms,        // Prevent calling for help
    PoisonLifeSupport,   // Slow crew kill
    OpenHangars,         // Decompress hangar (destroy fighters)
}
```

**Skill checks**:
```csharp
float sabotageChance =
    pilot.EngineeringSkill * 0.5f +
    pilot.Intelligence * 0.3f +
    (hasTools ? 0.2f : 0.0f);

// Penalties:
if (isDetected) sabotageChance *= 0.5f;  // Working under fire
if (isInjured) sabotageChance *= 0.7f;   // Wounded, slower

if (random.NextFloat() < sabotageChance) {
    ApplySabotage(capitalShip, target);
    GainReputation(pilot, "Saboteur", +15);
} else {
    AlertSecurity(pilot.Location);  // Failed, detected
}
```

---

## Detection & Response

### Ship Security Response

**Detection methods**:
1. **Crash alert**: All crew aware of breach location
2. **Visual contact**: Crew/cameras spot intruder
3. **Sabotage detected**: System failures trigger manhunt
4. **Life sign scanners**: Ship scans for unauthorized biosignatures

**Security response tiers**:

| Tier | Description | Forces Deployed |
|------|-------------|-----------------|
| **1: Breach Alert** | Crash detected, sector sealed | 2-3 security personnel |
| **2: Intruder Hunt** | Pilot spotted, active pursuit | 5-10 security, squad tactics |
| **3: Ship-Wide Lockdown** | Multiple sabotages, serious threat | 20+ marines, armored squads |
| **4: Desperate Measures** | Venting sections to kill intruder | Sacrifice crew to eliminate threat |

**Pilot survival chances**:
```
Tier 1: 80% (can hide/evade easily)
Tier 2: 50% (active hunt, harder to hide)
Tier 3: 20% (overwhelming force)
Tier 4: 5% (environmental hazards + enemies)
```

### Combat Inside Ship

**Pilot vs. Security**:
```csharp
// Pilot advantages:
- Desperation (morale immune to fear)
- Combat training (strike craft pilots are elite)
- Element of surprise (ambush tactics)

// Security advantages:
- Numbers (outnumber pilot 5-1 or more)
- Home ground (know ship layout)
- Support (medics, reinforcements nearby)

// Combat resolution:
float pilotCombatPower =
    pilot.CombatSkill * 0.4f +
    pilot.Weapon.Effectiveness * 0.3f +
    pilot.Desperation * 0.2f +           // Fights to the death
    pilot.Armor * 0.1f;

float securityCombatPower =
    security.Count * security.SkillAverage * 0.5f +
    security.Coordination * 0.3f +
    security.Equipment * 0.2f;

// Modified by:
- Terrain: Narrow corridors favor pilot (can't be surrounded)
- Pilot injury: -50% effectiveness if critically wounded
- Reinforcements: Security gets +2 members every 30 seconds
```

---

## Resolution Outcomes

### 1. Ship Captured (Allied Victory)

**Pilot recovered**:
- Survives until ship subdued by allied forces
- Extracted by boarding parties
- Reputation +30 ("Warrior's Last Stand")
- Morale boost to faction (+20, "Our warriors are unstoppable!")

**Narrative**:
> "Pilot [Name] was found in the engine room, surrounded by corpses and disabled machinery. 'Took you long enough,' she muttered, covered in blood and soot. Her deeds this day will be sung in the halls of heroes."

### 2. Pilot Captured

**Captured by enemy crew**:
- Subdued or surrenders (if Pragmatic)
- Becomes prisoner

**Enemy faction decisions**:

#### Option A: Ransom
**Conditions**: Pilot is high-value (skilled, noble, high reputation)
**Process**:
- Enemy demands ransom (credits, resources, prisoner exchange)
- Pilot's faction decides to pay or refuse
- If paid: Pilot returned (loyalty −10, "Felt abandoned during capture")
- If refused: Pilot executed or recruited by enemy

**Economic model**:
```csharp
float ransomValue =
    pilot.Reputation * 100 +
    pilot.SkillAverage * 50 +
    pilot.Nobility * 200;  // Noble = high ransom

// Faction willingness to pay:
float payWillingness =
    faction.Wealth / ransomValue +
    pilot.Loyalty / 200 +
    faction.Alignment.Good * 0.3f;  // Good factions value lives more

if (payWillingness > 0.6f) {
    PayRansom();
} else {
    RefuseRansom();  // "We don't negotiate with terrorists"
}
```

#### Option B: Execution
**Conditions**: Enemy is Evil, pilot is hated enemy, committed atrocities
**Process**:
- Pilot executed (may be public, broadcast as propaganda)
- Pilot's faction receives news
- Martyrdom: Pilot becomes symbol (+50 reputation posthumously)
- May trigger vengeance grudge (intensity 90, Vendetta level)

**Narrative**:
> "Pilot [Name] was executed by firing squad, her last words: 'I regret nothing.' Her death sparked outrage across the fleet—vengeance will be swift."

#### Option C: Recruitment (Defection)
**Conditions**: Enemy is Opportunist, pilot has compatible alignment
**Process**:
- Enemy offers pilot choice: Join us or die
- Pilot evaluates based on loyalty, alignment match, treatment
- If accepts: Defects to enemy faction (traitor)
- If refuses: Executed or imprisoned

**Defection logic**:
```csharp
float defectionProbability =
    (1.0f - pilot.Loyalty / 200) * 0.4f +        // Low loyalty = more likely
    alignmentMatch * 0.3f +                       // Ideological fit
    enemyOffer.Quality * 0.2f +                   // Bribes, promises
    pilot.Opportunist * 0.1f;                     // Outlook modifier

// Competing factors:
if (pilot.Lawful > 50) defectionProbability -= 0.3f;  // Honor prevents betrayal
if (pilot.Vengeful) defectionProbability -= 0.2f;     // Grudges against enemy
if (pilot.Fanatic) defectionProbability = 0.0f;       // Never betray cause

if (random.NextFloat() < defectionProbability) {
    Defect(pilot, enemyFaction);
    NotifyOriginalFaction(pilot, "traitor");
} else {
    Refuse();  // Executed or imprisoned
}
```

#### Option D: Release (Mercy)
**Conditions**: Enemy is Good/Lawful, pilot is non-combatant status
**Process**:
- Enemy releases pilot (prisoner exchange, humanitarian gesture)
- Pilot returned to faction
- Loyalty to original faction +20 ("They fought to get me back")
- Enemy faction reputation +10 ("Honorable foe")

**Narrative**:
> "The enemy captain released Pilot [Name] as a gesture of honor. 'We are warriors, not butchers,' he said. This act of mercy may sow seeds of future peace."

### 3. Pilot Dies Gloriously

**Killed during rampage or sabotage**:
- Confirmed KIA inside enemy ship
- Body may be recovered (if ship captured) or lost (if ship escapes)

**Posthumous honors**:
```csharp
// Reputation boost (posthumous):
pilot.Reputation += 50;  // Martyr status

// Faction-wide morale boost:
faction.Morale += 15;  // "Witness the warrior's sacrifice!"

// Inspiration effect (cascade):
// Other pilots with similar culture gain temporary buff
foreach (var otherPilot in faction.Pilots) {
    if (otherPilot.Culture == pilot.Culture) {
        otherPilot.Morale += 10;
        otherPilot.CombatEffectiveness += 0.15f;  // Duration: 300 ticks
    }
}

// May trigger "Avenge the Fallen" event:
// Special mission to destroy ship that killed hero
```

**Narrative**:
> "Pilot [Name] died fighting in the enemy's bridge, taking the captain with her. Her sacrifice will be remembered. Songs will be sung. Statues will be raised. She is eternal."

### 4. Pilot Trapped (Limbo)

**Ship escapes before resolution**:
- Pilot still alive inside enemy ship
- Ship flees to allied territory or deep space
- Pilot in prolonged survival/sabotage mode

**Long-term outcomes**:
1. **Eventual rescue**: Allied forces intercept ship, rescue pilot (weeks/months later)
2. **Escape pod**: Pilot steals escape pod, ejects (random location)
3. **Ship destruction**: Pilot dies when ship is destroyed by other forces
4. **Integration**: Pilot becomes permanent stowaway/saboteur (months of disruption)

**Narrative**:
> "Pilot [Name]'s fate remains unknown. She was last seen inside the enemy dreadnought before it jumped to hyperspace. Some say she still lurks in its shadows, waiting for the right moment to strike."

---

## Component Structure

### Pilot Components

```csharp
// Marks pilot as having performed Last Stand
public struct LastStandActive : IComponentData
{
    public Entity TargetCapitalShip;
    public uint StartTick;
    public LastStandBehavior CurrentBehavior;
    public float DetectionLevel;        // 0-1, how aware enemy is
    public float InjurySeverity;        // 0-1, health loss from crash
}

public enum LastStandBehavior : byte
{
    JoiningBoardingParty,
    SoloSabotage,
    Rampage,
    LayingLow
}

// Tracks pilot location inside ship
public struct InteriorLocation : IComponentData
{
    public Entity HostShip;
    public ShipSection Section;         // Bridge, Engines, Hangar, etc.
    public float3 LocalPosition;        // Position within ship
    public bool IsDetected;
    public bool IsInCombat;
}

// Sabotage actions in progress
public struct SabotageInProgress : IComponentData
{
    public SabotageType Type;
    public float Progress;              // 0-1
    public float TimeRemaining;         // Seconds
    public float DetectionRisk;         // Increases over time
}
```

### Capital Ship Components

```csharp
// Tracks intruders inside ship
public struct ShipIntruders : IBufferElementData
{
    public Entity IntruderEntity;
    public ShipSection Location;
    public uint DetectedTick;           // When first detected (0 = unknown)
    public SecurityResponseTier ResponseTier;
}

// Ship security state
public struct ShipSecurity : IComponentData
{
    public SecurityResponseTier CurrentTier;
    public int SecurityForces;          // Number of security personnel
    public float AlertLevel;            // 0-1
    public bool IsLockdown;             // Ship-wide lockdown active
}
```

---

## Systems Workflow

### 1. Last Stand Decision System

```csharp
[BurstCompile]
public partial struct LastStandDecisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (health, trait, pilot, entity) in
            SystemAPI.Query<RefRO<HealthComponent>, RefRO<CulturalLastStandTrait>, RefRO<PilotComponent>>()
                .WithAll<StrikeCraft>()
                .WithNone<LastStandActive>())
        {
            // Check critical damage
            if (health.ValueRO.CurrentHealth / health.ValueRO.MaxHealth > 0.25f)
                continue;

            // Find nearby enemy capital ships
            var nearbyCapitalShips = FindNearbyCapitalShips(pilot.ValueRO.Position, 500f, enemyFaction);
            if (nearbyCapitalShips.Length == 0) continue;

            // Decision roll
            var decisionThreshold = CalculateLastStandProbability(pilot.ValueRO, trait.ValueRO);
            if (random.NextFloat() < decisionThreshold)
            {
                var targetShip = nearbyCapitalShips[0];  // Closest
                InitiateLastStand(state, entity, targetShip, trait.ValueRO.Variant);
            }
        }
    }
}
```

### 2. Crash Impact System

```csharp
[BurstCompile]
public partial struct LastStandCrashSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (lastStand, velocity, transform, pilot) in
            SystemAPI.Query<RefRW<LastStandActive>, RefRO<Velocity>, RefRO<LocalTransform>>()
                .WithAll<StrikeCraft>())
        {
            var targetShip = lastStand.ValueRO.TargetCapitalShip;

            // Check if reached target
            var distance = math.distance(transform.ValueRO.Position, GetPosition(targetShip));
            if (distance < 10f)  // Impact threshold
            {
                // Calculate crash damage
                var crashDamage = CalculateCrashDamage(velocity.ValueRO, pilot);

                // Determine impact location
                var impactLocation = DetermineImpactLocation(transform.ValueRO, targetShip);

                // Apply damage to capital ship
                ApplyDirectionalDamage(state, targetShip, impactLocation, crashDamage);

                // Pilot survival roll
                if (PilotSurvivesCrash(pilot, crashDamage))
                {
                    // Spawn pilot inside ship
                    SpawnPilotInside(state, pilot, targetShip, impactLocation);

                    // Destroy strike craft
                    state.EntityManager.DestroyEntity(pilot.Entity.StrikeCraft);
                }
                else
                {
                    // Pilot died on impact
                    KillPilot(state, pilot, martyrdom: true);
                }
            }
        }
    }
}
```

### 3. Interior Sabotage System

```csharp
[BurstCompile]
public partial struct InteriorSabotageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (sabotage, location, pilot) in
            SystemAPI.Query<RefRW<SabotageInProgress>, RefRO<InteriorLocation>, RefRO<PilotComponent>>())
        {
            // Progress sabotage
            sabotage.ValueRW.Progress += deltaTime / sabotage.ValueRO.TimeRemaining;

            // Check detection
            sabotage.ValueRW.DetectionRisk += deltaTime * 0.1f;  // Increases over time
            if (random.NextFloat() < sabotage.ValueRO.DetectionRisk)
            {
                // Detected! Alert security
                AlertSecurity(state, location.ValueRO.HostShip, pilot);
            }

            // Check completion
            if (sabotage.ValueRO.Progress >= 1.0f)
            {
                // Sabotage successful
                ApplySabotageEffect(state, location.ValueRO.HostShip, sabotage.ValueRO.Type);

                // Remove component
                state.EntityManager.RemoveComponent<SabotageInProgress>(pilot.Entity);

                // Grant reputation
                pilot.Reputation += 15;
            }
        }
    }
}
```

### 4. Security Response System

```csharp
[BurstCompile]
public partial struct ShipSecurityResponseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (intruders, security) in
            SystemAPI.Query<DynamicBuffer<ShipIntruders>, RefRW<ShipSecurity>>())
        {
            // Calculate appropriate response tier
            var intruderCount = intruders.Length;
            var sabotageCount = CountActiveSabotages(intruders);

            SecurityResponseTier newTier;
            if (sabotageCount >= 3) newTier = SecurityResponseTier.DesperateMeasures;
            else if (sabotageCount >= 2) newTier = SecurityResponseTier.ShipWideLockdown;
            else if (intruderCount >= 2) newTier = SecurityResponseTier.IntruderHunt;
            else newTier = SecurityResponseTier.BreachAlert;

            // Escalate response if needed
            if (newTier > security.ValueRO.CurrentTier)
            {
                EscalateResponse(state, security, newTier);
            }

            // Deploy security forces
            DeploySecurityForces(state, intruders, security.ValueRO);
        }
    }
}
```

---

## Balancing Considerations

### Risk vs. Reward

**High risk**:
- 80% pilot death rate
- Valuable pilot lost (training investment)
- May fail to inflict significant damage

**High reward**:
- Can disable capital ship systems
- Massive morale boost to faction
- Legendary reputation (martyrdom)
- Inspires other pilots

**Opportunity cost**:
- Pilot could have ejected and lived
- Could have retreated and repaired
- Sacrificing long-term asset for short-term chaos

### Cultural Balance

**Factions with Last Stand**:
- More aggressive combat style
- Higher pilot turnover (constant replacements needed)
- Lower retreat/surrender rates
- Terrifying reputation ("They never surrender")

**Factions without Last Stand**:
- Higher pilot survival rate
- More experienced veterans (longevity)
- Pragmatic tactics
- Less inspiring but more sustainable

### Counterplay

**Defending against Last Stand pilots**:
1. **Point Defense**: Shoot down ramming craft before impact
2. **Armored Bridge**: Harder to breach critical sections
3. **Security Protocols**: Fast response to intruders
4. **Venting Sections**: Sacrifice compromised areas
5. **Internal Sensors**: Detect intruders quickly

---

## Integration with Existing Systems

### Directional Damage

Last Stand crash uses **existing directional damage system**:
- Impact location determines which systems accessible
- Breach creates weak point for other attackers
- Armor degradation at impact site

### Boarding Mechanics

If **boarding parties already exist**:
- Last Stand pilot can join them (strength in numbers)
- Coordinated assault on capital ship
- Shared glory/loot

If **no boarding system yet**:
- Last Stand is **first implementation** of interior ship combat
- Establishes precedent for future boarding mechanics
- Solo pilot = simplified version, boarding squads = full version

### Crew Capture/Interrogation

Last Stand pilots can be:
- Captured → interrogation (intel extraction)
- Ransomed → economic/diplomatic gameplay
- Recruited → defection mechanics
- Executed → martyrdom/vengeance

### Alignment & Loyalty

**Alignment influences behavior**:
- Good: Avoid crew casualties (sabotage only)
- Evil: Maximize carnage (rampage)
- Lawful: Follow cultural code (honor duel)
- Chaotic: Unpredictable (random targets)

**Loyalty affects defection**:
- High loyalty: Never defect, die before betrayal
- Low loyalty: May accept recruitment offer

---

## Narrative Events

### Event: "The Last Flight"

**Trigger**: Pilot initiates Last Stand

**Text**:
> Pilot [Name]'s craft is crippled, flames licking the cockpit. Instead of ejecting, [he/she] grins through the pain and aims the dying vessel at the enemy dreadnought's bridge.
>
> "For [Faction Name]! For glory! For eternity!"
>
> The craft impacts with a thunderous explosion...

**Player choices**:
1. **Watch the chaos unfold** (observe pilot's actions)
2. **Rewind and save the pilot** (prevent Last Stand, eject instead)
3. **Coordinate with pilot** (direct sabotage targets via comms)

### Event: "Hero's Return"

**Trigger**: Last Stand pilot recovered alive

**Text**:
> Against all odds, Pilot [Name] survived. Found in the wreckage of the enemy ship's engine room, covered in blood and soot, [he/she] was extracted by boarding parties.
>
> "[He/She] disabled their reactor core from the inside. Without [his/her] sacrifice, we'd never have taken this ship."
>
> The crew erupts in cheers. [Name] will be sung about for generations.

**Outcome**:
- Pilot reputation +30
- Faction morale +20
- Cultural trait strengthened (more pilots likely to perform Last Stand)

### Event: "Martyr's Funeral"

**Trigger**: Last Stand pilot confirmed KIA

**Text**:
> Pilot [Name] did not survive. [His/Her] body was found clutching the enemy captain's throat, both dead.
>
> "In death, [he/she] achieved what we could not in life—[he/she] killed the tyrant."
>
> We burn [his/her] pyre. We sing [his/her] song. We remember.

**Outcome**:
- Pilot posthumous reputation +50
- Faction morale +15
- Inspiration buff (+15% combat effectiveness for 500 ticks)
- May trigger "Avenge [Name]" mission

---

## Pattern Bible Entry

### "The Glorious End"

**Scope**: Individual + Cultural Trait

**Preconditions**:
- Pilot with CulturalLastStandTrait (warrior culture)
- Strike craft critically damaged (<25% hull)
- Enemy capital ship within ramming range (500m)
- Pilot passes decision roll (alignment + culture)

**Gameplay Effects**:
- Pilot crashes craft into enemy ship (kamikaze boarding)
- Crash damage applied to capital ship (directional)
- Pilot survival roll (20-40% chance)
- If survives: Interior combat/sabotage begins
- If dies: Martyrdom (+50 reputation, faction morale boost)
- Outcomes: Recovered, captured (ransom/execution/recruitment), killed gloriously

**Narrative Hook**: "The warrior who chose death inside the enemy's heart over life in retreat."

**Priority**: Core (cultural identity pillar)

**Related Systems**: Last Stand, Directional Damage, Boarding, Crew Capture, Alignment, Loyalty, Reputation, Cultural Traits

---

## Implementation Roadmap

### Phase 1: Decision & Crash (3 weeks)
- [ ] CulturalLastStandTrait component
- [ ] LastStandDecisionSystem (trigger logic)
- [ ] Crash trajectory and impact calculation
- [ ] Pilot survival roll
- [ ] Basic crash damage to capital ship

### Phase 2: Interior Simulation (4 weeks)
- [ ] InteriorLocation component (track pilot inside ship)
- [ ] ShipSection enum (Bridge, Engines, Hangar, etc.)
- [ ] Sabotage system (disable systems)
- [ ] Security response system
- [ ] Combat inside ship (pilot vs. crew)

### Phase 3: Resolution Outcomes (2 weeks)
- [ ] Pilot recovery (if ship captured)
- [ ] Capture logic (ransom, execution, recruitment, release)
- [ ] Defection mechanics
- [ ] Martyrdom and reputation boost

### Phase 4: Narrative & Polish (2 weeks)
- [ ] Narrative events (The Last Flight, Hero's Return, Martyr's Funeral)
- [ ] UI notifications (pilot status, sabotage progress)
- [ ] Audio/VFX (crash explosion, interior combat sounds)
- [ ] Cultural trait tuning (balance risk/reward)

**Total**: ~11 weeks

---

## Success Metrics

**Cultural Identity**:
- % of players who embrace Last Stand cultures vs. pragmatic cultures
- Player feedback on "Do you feel like a warrior?"

**Gameplay Impact**:
- Average damage inflicted by Last Stand pilots (should be meaningful, not negligible)
- Pilot survival rate (target: 20-30%, high risk but not suicide)
- Frequency of Last Stand triggers (target: 5-10% of critically damaged pilots)

**Narrative Satisfaction**:
- Player reports of memorable Last Stand moments
- Social media sharing of "epic kamikaze boarding" clips

---

## Conclusion

**Warrior Pilot's Last Stand** transforms critical damage from a pure loss state into a dramatic opportunity for glory, cultural expression, and emergent storytelling. It rewards aggressive playstyles, creates asymmetric cultural identities, and generates unforgettable "I was there" moments.

This mechanic epitomizes the Space4X design philosophy: **deep simulation meets player-driven narrative**. Every pilot who crashes into an enemy ship becomes a story, whether they die a martyr, survive as a legend, or defect as a traitor.

**Cultural variance ensures** not all factions play the same—some value honor unto death, others pragmatism. This creates faction personality and strategic diversity.

**Integration with rewind** allows players to craft their perfect heroic moment or pragmatically choose survival. The choice is theirs.
