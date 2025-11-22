# Intel & Visibility System (Space4X)

## Overview

The Intel & Visibility System governs what information players and AI factions can perceive about the game world. It implements fog-of-war, intel decay, sensor mechanics, and information warfare. This system is critical for the **Exploration Loop** and affects all strategic decisions.

---

## Core Concept

**Information is a strategic resource.**

- **Visibility** determines what you can see RIGHT NOW (sensor range)
- **Intel** is historical knowledge that DECAYS over time (last known position, fleet composition)
- **Rumor** is unreliable information from third parties (diplomatic channels, intercepted transmissions)
- **Certainty** measures confidence in intel (confirmed sighting vs. outdated report vs. rumor)

Unlike Godgame where the player sees everything (god perspective), Space4X operates on **limited information warfare**.

---

## Spatial Visibility Model

### Sector-Based Fog of War

The galaxy is divided into **sectors** (spatial grid cells). Each faction tracks visibility per sector.

```csharp
// Singleton per faction tracking fog-of-war state
public struct FactionIntelState : IComponentData
{
    public Entity OwnerFaction;
    public ushort SectorCount;                   // Total sectors in galaxy
}

// Buffer of sector visibility states
public struct SectorVisibility : IBufferElementData
{
    public ushort SectorId;                      // Spatial grid cell index
    public VisibilityLevel Level;
    public ushort LastObservedTick;              // When we last had sensor coverage here
    public ushort DecayRate;                     // Ticks until intel drops a level
}

public enum VisibilityLevel : byte
{
    Unknown,        // Never visited, no data
    Rumored,        // Heard about from diplomacy/intercept (low certainty)
    Stale,          // Visited >1000 ticks ago, intel decayed
    Recent,         // Visited 100-1000 ticks ago
    Current         // Active sensor coverage RIGHT NOW
}
```

**Visibility Mechanics**:
- **Unknown**: Sector appears blank/void (no data)
- **Rumored**: Sector marked with "?" (possible deposit/threat, unconfirmed)
- **Stale**: Last known data shown with timestamp, grayed out
- **Recent**: Data from <1000 ticks ago, yellow tint (caution)
- **Current**: Real-time sensor data, white/normal appearance

---

## Entity Detection System (Supreme Commander Style)

Detection operates on **two separate layers**: **Radar** (object existence) and **Perception** (entity details).

### Layer 1: Radar Detection (Object Existence)

**Radar reveals that SOMETHING exists**, but not what it is.

```csharp
public struct RadarSignature : IComponentData
{
    public float SignatureStrength;              // 0.0-10.0 (how easy to detect)
    public RadarProfile Profile;                 // Size category for radar detection
    public bool EmitsRadarSignature;             // False if powered down/stealthed
}

public enum RadarProfile : byte
{
    Tiny,           // Shuttle, probe (signature 0.5)
    Small,          // Corvette, asteroid (signature 1.0)
    Medium,         // Frigate, small station (signature 2.0)
    Large,          // Cruiser, large deposit (signature 4.0)
    Huge,           // Carrier, planet (signature 6.0)
    Massive,        // Titan, moon (signature 8.0)
    Stellar         // Station, star (signature 10.0, always visible at extreme range)
}
```

**Radar Detection Formula**:
```
Radar Detection = (Sensor Radar Range / Distance) * (Signature Strength) * Sensor Quality * Tech Level Modifier
If Radar Detection >= 1.0: Object detected (blip on radar, approximate position)
If Radar Detection < 1.0: No detection (invisible)
```

**What Radar Provides**:
- Object exists at approximate coordinates (±50 unit error at long range)
- Size category (Tiny/Small/Medium/Large/Huge/Massive/Stellar)
- Movement vector (if object moving)
- **NO entity details** (unknown type, allegiance, composition)

### Layer 2: Perception Detection (Entity Identification)

**Perception reveals WHO the entity is and WHAT it contains.**

```csharp
public struct PerceptionProfile : IComponentData
{
    public float PerceptionResistance;           // 0.0-10.0 (how hard to identify)
    public PerceptionMethod VisibleBy;           // Bitmask of sensor types that can perceive this
    public bool IsCloaked;                       // True if active cloaking engaged
    public byte CloakTechLevel;                  // 0-10 (higher = harder to penetrate)
}

[Flags]
public enum PerceptionMethod : byte
{
    None = 0,
    Visual = 1 << 0,        // Optical telescopes (long range, basic ID)
    Passive = 1 << 1,       // Heat/emissions analysis (ship class, power signature)
    Active = 1 << 2,        // Active scanning (full composition, allegiance)
    Gravimetric = 1 << 3,   // Mass analysis (bypasses some cloaking)
    Quantum = 1 << 4        // Exotic sensors (penetrates all cloaking)
}
```

**Perception Detection Formula**:
```
Perception Power = Sensor Perception Strength * (Sensor Tech Level - Cloak Tech Level) * Proximity Modifier
Proximity Modifier = 2.0 if distance < 50 units, 1.0 if 50-200 units, 0.5 if >200 units

If Cloaked:
    If Sensor has Gravimetric: Perception Power *= 0.5 (partial penetration)
    If Sensor has Quantum: Perception Power *= 2.0 (full penetration)
    Else: Perception Power *= 0.1 (cloaking defeats perception)

If Perception Power >= PerceptionResistance: Entity identified (full details)
If Perception Power < PerceptionResistance: Entity hidden (radar blip only)
```

**What Perception Provides**:
- Entity type (Capital Ship, Regular Ship, Vessel, Station, Deposit)
- Allegiance (which faction owns it)
- Ship class (if ship: Corvette, Frigate, Carrier, etc.)
- Approximate loadout (weapon types, shield status) - detail increases with perception strength
- Crew count estimate (±20% accuracy)
- Current activity (mining, combat, hauling, idle)

### Combined Detection States

```csharp
public enum DetectionLevel : byte
{
    Undetected,         // No radar, no perception (invisible)
    RadarOnly,          // Radar detected, perception failed (unknown blip)
    PerceptionPartial,  // Radar + weak perception (type known, details fuzzy)
    PerceptionFull      // Radar + strong perception (complete intel)
}
```

**Example Scenarios**:
- **Long-range detection**: Radar detects Massive blip at 500 units, but perception too weak → DetectionLevel.RadarOnly
- **Mid-range detection**: Radar detects Large blip at 150 units, perception identifies "Enemy Cruiser" → DetectionLevel.PerceptionPartial
- **Close-range detection**: Radar detects Medium blip at 30 units, perception reveals "Hostile Frigate, Shield 75%, 3x Beam Cannons, 120 crew" → DetectionLevel.PerceptionFull
- **Cloaked ship**: Radar fails (no signature), perception fails (cloaking defeats sensors) → DetectionLevel.Undetected
- **Cloaked ship vs Gravimetric**: Radar fails, but gravimetric sensors detect mass distortion → DetectionLevel.RadarOnly (knows something is there, can't identify)
- **Cloaked ship vs Quantum**: Both radar and perception succeed (quantum penetrates cloaking) → DetectionLevel.PerceptionFull

---

## Sensor Module Integration

Carriers with Sensor modules (from [CarrierArchitecture.md](CarrierArchitecture.md)) provide visibility.

```csharp
// Component on carriers with active sensors
public struct SensorArray : IComponentData
{
    public float SensorRange;                    // Units from carrier position
    public DetectionMethod Methods;              // Bitmask of sensor types
    public float SensorQuality;                  // 0.5-2.0 multiplier (tech/crew)
    public SensorMode Mode;
    public ushort PowerDraw;                     // Active sensors consume more power
}

public enum SensorMode : byte
{
    Offline,        // No detection, zero power
    Passive,        // Listen-only (Passive detection), low power
    Active,         // Full scan (Active + Passive), high power, reveals carrier location
    Focused         // Narrow beam (2x range, 30° arc), very high power
}
```

**Sensor Mode Tradeoffs**:
- **Passive**: Stealthy but limited detection (only heat/radio signatures)
- **Active**: Full detection but broadcasts carrier position to enemies
- **Focused**: Long-range targeting for combat but narrow field of view

**Example Sensor Modules** (from CarrierArchitecture.md):
- **Sensor_PassiveScanArray**: 50 unit range, Passive only, 0.8 quality, 10 power
- **Sensor_ActiveRadar**: 100 unit range, Active + Passive, 1.2 quality, 50 power
- **Sensor_GravimetricDetector**: 75 unit range, Gravimetric (cloaked), 1.5 quality, 100 power
- **Sensor_ProbeDispenser**: Launches autonomous probes (see below)

---

## Survey Mechanics (Exploration Loop)

Carriers in Exploration role perform **sector surveys** to gather intel.

```csharp
public struct SurveyAction : IComponentData
{
    public ushort TargetSectorId;
    public SurveyDepth Depth;
    public ushort TicksRemaining;                // Survey duration countdown
    public ushort SurveyProgress;                // 0-100% completion
}

public enum SurveyDepth : byte
{
    Quick,          // 30 ticks, detects Large+ entities (deposits, stations)
    Standard,       // 100 ticks, detects Medium+ entities (carriers, asteroids)
    Deep,           // 300 ticks, detects Small+ entities (shuttles, probes)
    Exhaustive      // 1000 ticks, detects Tiny entities (debris, relics)
}
```

**Survey Process**:
1. Carrier enters sector with Exploration role active
2. Survey system checks sensor modules (if no Sensor module → cannot survey)
3. Survey begins automatically (or player can override depth)
4. Survey ticks down based on depth
5. On completion, sector visibility → Current, all entities detected added to intel database

**Survey Efficiency Modifiers**:
- Sensor quality (tech + crew proficiency)
- Multiple carriers in same sector stack survey speed (diminishing returns)
- Hazards slow surveys (radiation, debris fields)

---

## Intel Database (Multi-Layer System)

Intel is gathered from **multiple sources** and **exists separate from relations**. Intel is a strategic resource that can be **spent** for abilities.

### Intel Gathering Sources

```csharp
// Buffer on FactionIntelState tracking known entities
public struct IntelEntry : IBufferElementData
{
    public Entity TargetEntity;                  // What we know about
    public IntelData Data;                       // Last known state
    public ushort LastUpdatedTick;               // When we got this info
    public IntelCertainty Certainty;
    public ushort DecayTick;                     // When this intel expires
    public IntelSource Source;                   // How we got this intel
    public ushort IntelPoints;                   // Spendable intel resource (0-1000)
}

public struct IntelData
{
    public float3 Position;                      // Last known location
    public CarrierArchetype Archetype;           // Ship size (if carrier)
    public Entity OwnerFaction;                  // Allegiance
    public ushort ApproximateCrew;               // Crew count estimate
    public ModuleCategory DominantModule;        // Guessed role (mining/combat/haul)
    public DynamicBuffer<ModuleIntel> KnownModules; // Detected loadout (if high perception)
}

public enum IntelCertainty : byte
{
    Rumor,          // 0-25% confidence (diplomatic hearsay, ancient report)
    Low,            // 25-50% confidence (stale data, partial scan)
    Medium,         // 50-75% confidence (recent sighting, indirect detection)
    High,           // 75-95% confidence (direct sensor contact <100 ticks ago)
    Confirmed       // 95-100% confidence (current sensor lock)
}

public enum IntelSource : byte
{
    SensorDetection,    // Direct sensor contact
    SpyReport,          // Agent gathered intel
    BattleEncounter,    // Combat interaction
    DiplomaticExchange, // Allied faction shared intel
    Intercept,          // SIGINT intercepted transmission
    Deserter            // Enemy crew defected with intel
}
```

**Intel Gathering Methods**:

1. **Sensor Detection** (already covered):
   - Radar + Perception systems detect entities
   - Intel certainty = Confirmed while in sensor range
   - Decays over time after losing sensor contact

2. **Spy & Agent Networks**:

```csharp
public struct SpyAgent : IComponentData
{
    public Entity AssignedFaction;               // Who owns this spy
    public Entity TargetFaction;                 // Who they're spying on
    public SpyMission MissionType;
    public byte SpySkill;                        // 0-100 (affects success rate)
    public ushort MissionProgress;               // Ticks toward completion
    public float DetectionRisk;                  // 0.0-1.0 (chance of being caught)
}

public enum SpyMission : byte
{
    GatherFleetIntel,       // Learn ship locations, compositions
    StealTechBlueprints,    // Unlock enemy research
    Sabotage,               // Damage facilities, ships
    Subversion,             // Convert enemy pops/crew
    Assassination,          // Kill enemy captain/leader
    PlantMisinformation     // Feed false intel to enemy
}
```

**Spy Intel Mechanics**:
- Spies embedded in enemy faction gather intel over time
- Intel Points accumulate based on spy skill: `Points = SpySkill * MissionTicks / 100`
- Spy reports provide **High certainty intel** even without sensor contact
- Spies can be detected and eliminated (counterintelligence)
- High-skill spies can gather loadout details (module composition) without perception

3. **Battle Interactions**:

```csharp
public struct BattleIntelReward : IComponentData
{
    public Entity VictoriousFaction;
    public Entity DefeatedFaction;
    public ushort IntelPointsAwarded;            // Based on battle intensity
    public DynamicBuffer<CapturedIntel> CapturedData;
}

public struct CapturedIntel : IBufferElementData
{
    public Entity EnemyShip;
    public IntelCertainty NewCertainty;          // Upgraded to High/Confirmed
    public ushort IntelPoints;                   // Bonus points for this ship
}
```

**Battle Intel Rewards**:
- Winning battles grants intel on enemy ships involved
- Certainty upgraded to **High** (know exact composition after fighting them)
- Intel Points awarded: `Points = (Enemy Ship Value / 100) * (Victory Margin)`
- Capturing ships provides **perfect intel** (Confirmed certainty, full loadout details)
- Survivors/deserters provide intel on enemy fleet locations

### Intel as Spendable Resource

Intel Points can be **spent** for tactical/strategic abilities.

```csharp
public struct FactionIntelResources : IComponentData
{
    public ushort TotalIntelPoints;              // Accumulated from all sources
    public ushort IntelGenerationRate;           // Points per tick (from spies)
    public DynamicBuffer<IntelAbility> UnlockedAbilities;
}

public enum IntelAbility : byte
{
    TechTheft,              // Cost: 500 points - steal random enemy tech
    Sabotage,               // Cost: 300 points - damage enemy facility/ship
    Subversion,             // Cost: 400 points - convert enemy pops/crew
    CounterIntel,           // Cost: 200 points - reveal enemy spies
    PrecisionStrike,        // Cost: 250 points - reveal cloaked enemy for 100 ticks
    FalseFlag,              // Cost: 150 points - plant misinformation in enemy intel
    DeepScan                // Cost: 100 points - upgrade intel certainty (Low → High)
}
```

**Spending Intel**:
- Player/AI can spend Intel Points from pool
- Abilities have cooldowns (prevent spam)
- Spending intel does NOT delete intel entries (intel stays, points consumed)
- Intel generation rate depends on spy network size + battle frequency

### Intel Bonus Calculations

Intel provides **passive bonuses** in combat and diplomacy.

```csharp
public struct IntelCombatBonus : IComponentData
{
    public float CriticalStrikeChance;           // +0% to +25% based on intel certainty
    public float DodgeChance;                    // +0% to +20% (know enemy weapon timings)
    public float TargetingAccuracy;              // +0% to +30% (know enemy ship profiles)
}
```

**Intel Bonus Formula**:
```csharp
float CalculateCriticalStrikeBonus(IntelCertainty certainty)
{
    switch (certainty)
    {
        case IntelCertainty.Rumor: return 0.0f;
        case IntelCertainty.Low: return 0.05f;      // +5% crit
        case IntelCertainty.Medium: return 0.10f;   // +10% crit
        case IntelCertainty.High: return 0.18f;     // +18% crit
        case IntelCertainty.Confirmed: return 0.25f; // +25% crit (perfect intel)
    }
}
```

**Diplomatic Intel Bonuses**:
- High intel on faction improves agreement success rate
- Know faction's alignment/behavior → better negotiation tactics
- Intel on faction's resource shortages → leverage in trade deals

### Captain Refit Based on Intel

Captains use intel to **refit ships** for optimal counters.

```csharp
public struct CaptainRefitDecision : IComponentData
{
    public Entity Captain;
    public Entity TargetShip;                    // Ship to refit
    public DynamicBuffer<IntelEntry> EnemyIntel; // Known enemy compositions
    public RefitStrategy Strategy;
}

public enum RefitStrategy : byte
{
    AntiShield,     // Enemy uses shields → equip shield-breaker weapons
    AntiArmor,      // Enemy uses armor → equip armor-piercing weapons
    AntiSwarm,      // Enemy uses fighters → equip PD + area weapons
    LongRange,      // Enemy prefers close combat → equip long-range weapons
    Balanced        // Insufficient intel → balanced loadout
}
```

**Refit Decision Logic**:
1. Captain queries intel database for known enemy ships in region
2. Analyzes enemy loadouts (if intel certainty >= Medium)
3. Determines dominant enemy strategy (shield-heavy, missile-heavy, etc.)
4. Refits ship at station with counter-loadout (costs resources + time)
5. Low intel = Balanced loadout (no specific counter)

**Example**:
- Intel reveals enemy fleet uses 80% beam cannons (shield-heavy)
- Captain refits destroyer: Remove beam weapons, install armor + armor-piercing mass drivers
- Next battle: Captain has advantage (enemy beam cannons ineffective vs armor)

### Intel Decay & Culling

**Intel Decay**:
- **Confirmed** → **High**: 50 ticks (entities move, data becomes outdated)
- **High** → **Medium**: 200 ticks
- **Medium** → **Low**: 500 ticks
- **Low** → **Rumor**: 1000 ticks
- **Rumor** → **Culled**: 5000 ticks (deleted from database)

**Exception**: Immobile entities (stations, deposits) decay 10x slower (don't move)

**Intel Culling** (database limit):
- Faction intel database has capacity limit (500 entries by default, upgradeable via tech)
- When limit reached, **lowest priority intel culled first**:
  1. Rumor certainty (oldest first)
  2. Low certainty on non-threat entities (civilians, mining ships)
  3. Medium certainty on distant entities (outside operational range)
  4. High certainty retained (valuable intel)
  5. Confirmed certainty always retained (current sensor contacts)

**Manual Culling**:
- Player/AI can manually delete intel entries (free up space for new intel)
- Useful for ignoring irrelevant factions/sectors

---

## Probe System

Carriers with **Sensor_ProbeDispenser** modules can launch autonomous probes.

```csharp
public struct Probe : IComponentData
{
    public Entity LaunchedByCarrier;
    public Entity TargetSector;                  // Sector to survey
    public float FuelRemaining;                  // Ticks until probe dies
    public ProbeType Type;
}

public enum ProbeType : byte
{
    Disposable,     // 500 tick lifespan, cheap, single survey then expires
    Persistent,     // 5000 tick lifespan, maintains Current visibility in sector
    Stealth,        // 2000 tick lifespan, undetectable (Passive sensors only)
    Relay           // 10000 tick lifespan, extends sensor network range
}
```

**Probe Mechanics**:
1. Carrier launches probe (consumes 1 probe from Sensor_ProbeDispenser capacity)
2. Probe travels to target sector (slow, 0.1 speed of carrier)
3. On arrival, probe begins survey (Standard depth)
4. Probe maintains visibility while fuel lasts
5. Probe fuel expires → probe destroyed, sector visibility decays normally

**Probe Network**:
- **Relay probes** extend sensor range between carriers
- Formula: If Carrier A in sector X and Relay Probe in sector Y (adjacent), Carrier A's sensors extend to Y
- Allows monitoring distant sectors without dedicating carriers

---

## Stealth & Cloaking (Tech Level Considerations)

Entities can reduce signatures to avoid detection. **Cloaking defeats perception unless special sensors defeat it.**

```csharp
public struct StealthProfile : IComponentData
{
    public float RadarSignatureReduction;        // 0.0-0.95 (reduces radar detection)
    public float PerceptionResistanceBonus;      // Additive bonus to perception resistance
    public StealthMethod Method;
    public byte StealthTechLevel;                // 0-10 (higher = better stealth)
    public ushort PowerCost;                     // Cloaking drains power heavily
}

public enum StealthMethod : byte
{
    None,           // No stealth
    ColdRunning,    // Powered down (0.5x radar signature, 0% cloak, cannot move/attack)
    ECM,            // Electronic countermeasures (0.7x radar signature, +2 perception resistance, detectable by Gravimetric)
    Cloaking        // Active cloak (0.3x radar signature, +5 perception resistance, huge power cost)
}
```

**Cloaking vs Perception Mechanics**:
- **Cloaking defeats perception** unless sensor tech level > cloak tech level OR sensor has special counter (Gravimetric/Quantum)
- **Tech Level matters**: Tier 5 cloak vs Tier 3 sensors = perception fails (cloaked ship stays hidden)
- **Proximity matters**: Even cloaked ships become easier to perceive at <50 unit range (Proximity Modifier = 2.0)
- **Gravimetric sensors**: Reduce cloak effectiveness by 50% (detect mass distortion)
- **Quantum sensors**: Fully penetrate cloaking (exotic tech counter)

**Weapon Stealth Profiles**:

Weapons have their own stealth characteristics. **Firing weapons can reveal cloaked ships.**

```csharp
public struct WeaponStealthProfile : IComponentData
{
    public float FiringSignatureSpike;           // Temporary radar signature increase when firing
    public ushort SignatureDecayTicks;           // Ticks until signature returns to baseline
    public bool BreaksCloaking;                  // True = firing disables cloak for N ticks
    public ushort CloakBreakDuration;            // Ticks until cloak can re-engage
}
```

**Weapon Signature Examples**:
- **Beam Cannon**: High firing signature (+5.0 radar spike), breaks cloaking for 100 ticks (bright energy beam visible)
- **Mass Driver**: Medium firing signature (+2.0 radar spike), breaks cloaking for 50 ticks (muzzle flash detectable)
- **Missiles**: Low firing signature (+1.0 radar spike), breaks cloaking for 30 ticks (cold launch from tubes)
- **Point Defense**: Minimal signature (+0.5 radar spike), does NOT break cloaking (low-power lasers)

**Stealth Combat**:
- Cloaked ship approaches undetected (radar + perception both fail)
- Fires torpedoes (cold launch, +1.0 radar spike, cloaking breaks for 30 ticks)
- Enemy sensors detect radar blip during firing window
- If enemy perception strong enough + within 30 tick window: ship identified, loses surprise
- After 30 ticks: cloak re-engages, ship disappears again (if still out of strong perception range)

**Captain Intelligence for Blind Firing**:

Captains can **blind fire toward last known position** of cloaked targets.

```csharp
public struct CaptainIntelligence : IComponentData
{
    public byte TacticalSkill;                   // 0-100 (affects blind fire accuracy)
    public float3 LastKnownEnemyPosition;        // Where cloaked target was last detected
    public ushort LastDetectionTick;             // When target was last seen
    public float PositionUncertaintyRadius;      // Error margin (grows over time)
}
```

**Blind Fire Mechanics**:
- Captain with TacticalSkill 80 tracks cloaked ship's last position
- Every tick after detection, uncertainty radius grows: `Radius = (CurrentTick - LastDetectionTick) * 5.0 units`
- Captain orders weapons to fire at predicted position (last known + extrapolated velocity)
- Accuracy = `TacticalSkill / 100.0 * (1.0 / (1.0 + UncertaintyRadius / 50.0))`
- High-skill captains can land hits on cloaked targets by prediction

**Use Cases**:
- Stealth shuttles for scouting hostile sectors (passive sensors only, no radar signature)
- Cloaked bombers for alpha strikes (approach hidden, fire torpedoes, re-cloak)
- Cold-running haulers to avoid pirates (no radar signature, but vulnerable if detected)
- Frigate captains blind-firing at cloaked raiders (skill-based counterplay)

---

## Information Warfare

Factions can manipulate intel through deception and signals intelligence.

### Signals Intelligence (SIGINT)

```csharp
public struct SIGINTModule : IComponentData
{
    public float InterceptRange;                 // Detect enemy transmissions
    public float DecryptionSpeed;                // Ticks to decode message
}
```

**SIGINT Mechanics**:
- Carriers with SIGINT modules detect enemy Active sensor pings within range
- Intercepts reveal approximate enemy position + sensor strength
- Can triangulate enemy location if multiple SIGINT carriers detect same ping
- Does NOT reveal full carrier composition (partial intel)

### Deception & Misinformation

```csharp
public struct DecoyBeacon : IComponentData
{
    public DetectionProfile FakeSignature;       // Pretends to be a larger ship
    public ushort Duration;                      // Ticks before beacon expires
}
```

**Decoy Mechanics**:
- Carrier launches decoy beacon (fake signature)
- Enemy sensors detect beacon as if real carrier
- Decoy does not move (stationary signature)
- Deep survey or close inspection reveals decoy (certainty drops to Rumor)

**Use Cases**:
- Draw enemy fleet to empty sector (decoy battleship signature)
- Hide real mining operation behind decoy signatures
- Feint attack on station (decoys + real fleet elsewhere)

---

## Rumor & Diplomatic Intel

Factions share intel through diplomacy (see [DiplomacyDynamics.md](DiplomacyDynamics.md)).

```csharp
public struct DiplomaticIntelExchange : IComponentData
{
    public Entity SourceFaction;
    public Entity TargetFaction;
    public DynamicBuffer<IntelEntry> SharedIntel;
    public float TrustworthinessModifier;        // 0.0-1.0 (can source be trusted?)
}
```

**Rumor Mechanics**:
- Allied factions automatically share Current/High certainty intel
- Neutral factions can trade intel (treaty clause)
- Hostile factions provide false intel (misinformation warfare)

**Trustworthiness**:
- High trust (alliance) → intel certainty unchanged
- Medium trust (trade pact) → intel certainty reduced by 1 level (High → Medium)
- Low trust (neutral) → intel certainty reduced by 2 levels (High → Low)
- No trust (hostile) → intel may be fabricated (Rumor certainty, possibly fake data)

**AI Use**:
- AI factions assess trustworthiness based on alignment + past betrayals
- AI may intentionally share false intel to lure enemies into traps

---

## Exploration Loop Integration

The full Exploration loop ([ExplorationLoop.md](ExplorationLoop.md)) uses this system:

1. **Scout Carrier** with Sensor modules enters Unknown sector
2. **Survey Action** begins (Standard depth, 100 ticks)
3. Survey completes → sector visibility becomes Current
4. All detectable entities in sector added to **Intel Database** (Confirmed certainty)
5. Scout carrier moves to next sector OR deploys **Persistent Probe** to maintain visibility
6. Intel decays over time → **Rumor → Deleted** if not refreshed
7. Player reviews intel database to plan mining/combat/hauling operations

**Player Decisions**:
- Which sectors to survey first? (prioritize rumors of valuable deposits)
- Deploy expensive persistent probes or rely on recurring patrols?
- Use stealth scouts for hostile territory or fast scouts for speed?
- Trust allied faction intel or verify personally?

---

## Combat Integration

Intel affects combat engagement decisions (see [CombatLoop.md](CombatLoop.md)).

**Engagement with Current Intel**:
- Know enemy fleet composition, can plan counters
- Accurate range estimation for weapon deployment
- High confidence in outcome prediction

**Engagement with Stale/Low Intel**:
- Uncertain enemy strength (might be reinforced)
- Risk of ambush (enemy stealth units not detected in old survey)
- Must scout before committing battleships (send cheap frigate first)

**No Intel (Unknown Sector)**:
- Blind engagement, extreme risk
- Could be empty OR enemy titan waiting
- AI factions avoid unless desperate or scouting

---

## Hauling Integration

Haul routes ([HaulLoop.md](HaulLoop.md)) depend on route safety intel.

**Route Planning**:
- Hauler carriers check intel for each sector along route
- **Current visibility** → safe route (no detected hostiles)
- **Stale/Rumor visibility** → uncertain (request escort or avoid)
- **Unknown** → never use (too risky, divert around)

**Dynamic Rerouting**:
- If hauler detects hostile in Current sector → abort route, return to station
- If allied faction shares hostile intel → reroute all haulers proactively

---

## Mining Integration

Mining carriers ([MiningLoop.md](MiningLoop.md)) use intel to find deposits.

**Deposit Discovery**:
- Surveys reveal deposit entities with yield/richness data
- Intel database tracks deposit depletion (last known yield)
- Stale intel → deposit may be exhausted (risk wasted trip)

**Hazard Detection**:
- Surveys detect radiation/pirate presence
- Intel certainty affects risk assessment (Rumor of pirates → maybe false alarm)
- Mining carriers request combat escorts based on hazard intel

---

## Construction Integration

Station construction ([ConstructionLoop.md](ConstructionLoop.md)) requires site surveys.

**Site Selection**:
- Must have Current visibility to place station
- Survey reveals terrain suitability (asteroid field for mining station, open space for shipyard)
- Ongoing visibility required during construction (detect hostile approach)

**Construction Monitoring**:
- Construction carriers maintain Active sensors during build
- Detect hostile scouts approaching build site
- Can abort construction and evacuate if threatened

---

## Tech Progression

Sensor tech unlocks better detection capabilities.

**Primitive Tech**:
- Passive sensors only (50 unit range)
- Visual detection (requires line-of-sight)
- No intel decay mitigation (intel decays at base rate)

**Industrial Tech**:
- Active radar (100 unit range)
- Standard survey depth available
- Basic intel database (stores 100 entries)

**FTL Tech**:
- Gravimetric sensors (detect cloaked)
- Deep survey depth (detect Small entities)
- Persistent probes (maintain visibility)
- Advanced intel database (stores 500 entries, slower decay)

**Exotic Tech**:
- Quantum sensors (perfect detection, penetrates all stealth)
- Exhaustive survey (detect Tiny debris, relics)
- Relay probes (sensor network extension)
- Predictive analytics (AI estimates entity movement based on stale intel)

**Transcendent Tech**:
- Omniscient sensors (galaxy-wide visibility, no fog-of-war)
- Perfect intel (no decay, 100% certainty)
- Precognitive detection (detect entities before they arrive, sci-fi nonsense)

---

## Performance Considerations

**Sector Count**:
- Galaxy divided into ~10,000 sectors (100x100 grid)
- Each faction tracks visibility per sector = 10,000 * N factions
- Visibility updates only when carriers move or intel decays (not every tick)

**Intel Database Size**:
- Limit per faction: 500 entries (configurable by tech tier)
- Oldest Rumor entries deleted first when limit reached
- Immobile entities (stations) prioritized to stay in database

**Survey Optimization**:
- Survey does NOT scan every entity in sector (too slow)
- Spatial grid query for entities in sector radius
- Detection formula computed once per entity per survey tick
- Exhaustive surveys are intentionally slow (1000 ticks = strategic trade-off)

---

## Open Questions / Design Decisions Needed

1. **Sector Grid Resolution**: 100x100 sectors sufficient for typical galaxy? Or dynamic based on map size?
   - *Suggestion*: Scale with galaxy gen settings (small = 50x50, huge = 200x200)

2. **Probe Recovery**: Can carriers retrieve deployed probes for reuse, or always consumed?
   - *Suggestion*: Relay/Persistent probes recoverable if carrier returns, Disposable/Stealth consumed

3. **Intel Sharing Automation**: Should allied factions auto-share all intel, or require player approval?
   - *Suggestion*: Auto-share by default, player can toggle "intelligence blackout" for secrecy

4. **Survey Interruption**: Can hostile presence interrupt ongoing survey?
   - *Suggestion*: Yes - if hostile detected within 50 units, survey pauses until clear or aborted

5. **False Positives**: Should low-quality sensors generate fake detections (noise)?
   - *Suggestion*: Optional "hard mode" feature, off by default (frustrating for players)

6. **Intel AI Behavior**: How aggressively do AI factions scout? Spam cheap probes or careful targeted surveys?
   - *Suggestion*: Alignment-driven (Lawful = methodical surveys, Chaotic = spam probes everywhere)

---

## Implementation Notes

- **FactionIntelState** singleton per faction entity
- **SectorVisibility** buffer on FactionIntelState (one entry per sector)
- **IntelEntry** buffer on FactionIntelState (detected entities)
- **SensorArray** component on carriers with Sensor modules
- **SurveyAction** component added when carrier enters Exploration role
- **Probe** entities spawned by ProbeDispenser module
- **DetectionSystem** runs every 10 ticks (not every tick, performance)
- **IntelDecaySystem** runs every 100 ticks, decrements certainty
- **SurveyTickSystem** processes active surveys, updates visibility on completion
- **SIGINTInterceptSystem** listens for Active sensor pings, adds rumor intel

---

## References

- **Carrier Architecture**: [CarrierArchitecture.md](CarrierArchitecture.md) - Sensor modules
- **Exploration Loop**: [ExplorationLoop.md](ExplorationLoop.md) - Survey mechanics
- **Combat Loop**: [CombatLoop.md](CombatLoop.md) - Intel affects engagement decisions
- **Haul Loop**: [HaulLoop.md](HaulLoop.md) - Route safety intel
- **Mining Loop**: [MiningLoop.md](MiningLoop.md) - Deposit discovery
- **Diplomacy**: [DiplomacyDynamics.md](DiplomacyDynamics.md) - Intel sharing/misinformation
- **Spatial Registry**: Sector grid for visibility tracking
