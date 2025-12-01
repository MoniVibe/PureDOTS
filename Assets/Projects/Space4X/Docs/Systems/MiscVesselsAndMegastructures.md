# Miscellaneous Vessels, Infrastructure & Pocket Universes

**Status**: Concept Design
**Last Updated**: 2025-11-29
**Game**: Space4X
**Category**: Vessel Types + Megastructures + Dimensional Mechanics

---

## Overview

This document conceptualizes the **full ecosystem** of Space4X vessels and structures beyond combat ships:
- **Utility Vessels**: Boarding craft, drop pods, shuttles, lifeboats, probes
- **Infrastructure**: Warp relays, beacons, gateways
- **Megastructures**: Dyson spheres, ring worlds, star forges
- **Dark Fleet**: Slaveships, prison barges (cultural/alignment-gated)
- **Pocket Universes**: Dungeon-like instances, alternate galactic maps, dimensional rifts

---

# UTILITY VESSELS

## Boarding Craft

**Purpose**: Deliver boarding parties to enemy capital ships (complement to Warrior Pilot Last Stand)

### Component Structure

```csharp
public struct BoardingCraft : IComponentData
{
    public Entity ParentCarrier;        // Launched from which ship
    public int CrewCapacity;            // Max marines (5-20)
    public float ArmorPenetration;      // Can breach armored sections
    public float StealthRating;         // Detection difficulty (0-1)
    public BoardingTactic Tactic;       // Breach, Stealth, Negotiation
}

public enum BoardingTactic : byte
{
    ForcedBreach,      // Explosive entry, loud, fast
    StealthInfiltration, // Docking clamps, quiet, slow
    Negotiation,       // Hail and request permission (for surrendered ships)
    Parasite           // Attach and drill through hull over time
}
```

### Gameplay Mechanics

**Launch Conditions**:
- Parent carrier within 2000m of target
- Crew allocated (marines/specialists)
- Tactic selected (player choice or AI doctrine)

**Flight Phase**:
- Small, fast, maneuverable (like strike craft)
- Vulnerable to point defense
- Can be manually aimed (Divine Guidance integration)

**Docking Phase**:
```csharp
// Breach location determined by approach vector
var breachLocation = DetermineBreachLocation(approach, targetShip);

// Success chance:
float breachChance =
    boardingCraft.ArmorPenetration * 0.5f +
    (1.0f - targetShip.Armor[breachLocation]) * 0.3f +
    crew.EngineeringSkillAverage * 0.2f;

if (random.NextFloat() < breachChance) {
    // Success: Deploy crew inside ship
    DeployCrew(crew, targetShip, breachLocation);
} else {
    // Failure: Repelled, craft damaged or destroyed
    ApplyDamage(boardingCraft, repulsionDamage);
}
```

**Interior Phase**:
- Crew navigates ship sections (same as Warrior Pilot Last Stand)
- Objective-based (capture bridge, disable engines, free prisoners)
- Security response escalates
- Can join with Last Stand pilots inside

**Cultural Variants**:
| Culture | Boarding Preference | Tactics |
|---------|-------------------|---------|
| **Honorbound** | Challenge and duel | ForcedBreach, seek captain |
| **Pragmatic** | Efficient capture | StealthInfiltration, sabotage |
| **Zealot** | Purge the unclean | ForcedBreach, rampage |
| **Merchant** | Minimize damage | Negotiation, bribery |

---

## Drop Pods

**Purpose**: Rapid planetary assault, station boarding, emergency escape

### Component Structure

```csharp
public struct DropPod : IComponentData
{
    public Entity LaunchShip;           // Orbital vessel
    public Entity TargetPlanet;         // Or station
    public float3 TargetCoordinates;    // Precision landing
    public int OccupantCapacity;        // 1-4 troops
    public float HeatShielding;         // Atmospheric entry survival
    public bool HasRetroRockets;        // Soft landing vs. crash
}

public struct DropPodOccupant : IBufferElementData
{
    public Entity CrewMember;
    public DropRole Role;               // Assault, Engineer, Medic, Commander
}

public enum DropRole : byte
{
    Assault,        // Combat specialist
    Engineer,       // Sabotage/hacking
    Medic,          // Sustain casualties
    Commander,      // Coordinate squads
    Demolition      // Destroy infrastructure
}
```

### Gameplay Mechanics

**Orbital Deployment**:
- Parent ship in orbit (altitude 100-500km)
- Target designated on planet/station surface
- Multiple pods launched simultaneously (coordinated assault)

**Entry Phase**:
```csharp
// Atmospheric entry challenge
float survivalChance =
    dropPod.HeatShielding * 0.6f +
    crew.Constitution * 0.2f +
    random.NextFloat() * 0.2f;

// Hazards:
if (planet.HasStorm) survivalChance *= 0.7f;
if (planet.HasDefenseGrid) survivalChance *= 0.5f;

if (random.NextFloat() < survivalChance) {
    // Survived entry
    LandPod(dropPod, targetCoordinates);
} else {
    // Destroyed in atmosphere
    DestroyPod(dropPod, crew);
}
```

**Landing Accuracy**:
```csharp
// Landing scatter
float scatterRadius =
    baseScatter * (1.0f - crew.PilotSkill) *
    (planet.AtmosphereDensity + planet.GravityModifier);

var actualLanding = targetCoordinates + RandomPointInCircle(scatterRadius);
```

**Deployment**:
- Crew exits pod
- Begin ground combat or sabotage mission
- Can call for extraction (shuttle pickup)

**One-Way Missions**:
- Pods without retro rockets = crash landing (high damage)
- Crew stranded until extraction or victory
- "Do or die" missions (Warrior Pilot Last Stand equivalent for ground troops)

---

## Shuttles

**Purpose**: Personnel/cargo transfer, non-combat logistics

### Component Structure

```csharp
public struct Shuttle : IComponentData
{
    public Entity HomeBase;             // Station or carrier
    public Entity Destination;          // Where it's headed
    public int PassengerCapacity;       // 2-20 people
    public float CargoCapacity;         // Tons of goods
    public bool IsArmed;                // Defensive weapons only
    public ShuttleType Type;
}

public enum ShuttleType : byte
{
    Personnel,      // Crew transfer
    Cargo,          // Resource hauling
    VIP,            // High-value passengers (captains, diplomats)
    Medical,        // Ambulance, triage
    Repair,         // Engineering team deployment
}
```

### Gameplay Mechanics

**Automated Routes**:
- Station ↔ Station: Trade/migration
- Fleet ↔ Station: Resupply
- Carrier ↔ Planet: Colony shuttles

**Player-Directed Missions**:
- Extract VIP from damaged ship
- Transfer critical supplies mid-battle
- Evacuate civilians from war zone

**Vulnerability**:
- Unarmed or lightly armed
- Slow (civilian speed)
- High-value targets (destroying shuttle = war crime if carrying civilians)

**Moral Conflict Integration**:
```csharp
// Enemy shuttle detected
if (shuttle.Type == ShuttleType.Medical || shuttle.PassengerType == Civilian) {
    var attackDecision = EvaluateMoralConflict(pilot, OrderType.AttackCivilians);

    if (pilot.Alignment.Good > 50) {
        // Refuse or hesitate
        attackDecision.Delay = 300;  // Major moral conflict
        attackDecision.Probability = 0.2f;  // 80% refuse
    }
}
```

---

## Lifeboats

**Purpose**: Emergency escape, last-ditch survival

### Component Structure

```csharp
public struct Lifeboat : IComponentData
{
    public Entity ParentShip;           // Ship being abandoned
    public int Occupants;               // Crew aboard
    public float FuelRange;             // Limited (100-500km)
    public bool HasDistressBeacon;      // Broadcast SOS
    public uint LaunchTick;             // When ejected
    public LifeboatStatus Status;
}

public enum LifeboatStatus : byte
{
    Docked,         // Still attached to parent ship
    Launched,       // Ejected, drifting
    Rescued,        // Picked up by ally
    Captured,       // Picked up by enemy (prisoners)
    Lost,           // Out of fuel, dead in space
}
```

### Gameplay Mechanics

**Automatic Launch**:
```csharp
// Ship critical (hull <10%)
if (ship.Hull < ship.MaxHull * 0.1f && ship.LifeboatsRemaining > 0) {
    // Crew decision: abandon ship?
    float abandonProbability =
        (1.0f - crew.Loyalty / 200) * 0.4f +
        (1.0f - crew.Morale / 100) * 0.3f +
        crew.SurvivalInstinct * 0.3f;

    // Competing with cultural traits:
    if (crew.HasCulturalTrait("DeathBeforeDishonor")) {
        abandonProbability *= 0.3f;  // Honor = stay and fight
    }

    if (random.NextFloat() < abandonProbability) {
        LaunchLifeboats(ship, crew);
    }
}
```

**Rescue Mechanics**:
- Lifeboats broadcast distress signal (radius 5000km)
- Allied ships can rescue (crew recovered)
- Enemy ships can capture (crew become prisoners)
- Neutral ships may rescue (for ransom or goodwill)

**Prisoner of War**:
```csharp
// Enemy picks up lifeboat
if (rescuer.Faction != lifeboat.Faction) {
    foreach (var crewMember in lifeboat.Occupants) {
        // Same outcomes as Warrior Pilot Last Stand captures:
        // - Ransom
        // - Execution (if Evil faction)
        // - Recruitment (defection chance)
        // - Release (if Good faction)
        HandlePrisoner(crewMember, rescuer.Faction);
    }
}
```

**Drift Dynamics**:
- Lifeboats have limited fuel (can't travel far)
- Drift in space until rescue or fuel expires
- Players can manually guide rescue ships (Divine Guidance)
- Time pressure: crew dies if not rescued within X ticks

---

## Probes

**Purpose**: Reconnaissance, sensor extension, data gathering

### Component Structure

```csharp
public struct Probe : IComponentData
{
    public Entity LaunchShip;
    public ProbeType Type;
    public float SensorRange;           // Detection radius
    public float FuelRemaining;         // Limited lifespan
    public uint LaunchTick;
    public bool IsStealthed;            // Difficult to detect
}

public enum ProbeType : byte
{
    Scout,          // Basic sensors, high speed
    Science,        // Detailed scans (planets, anomalies)
    Spy,            // Passive listening (intercept comms)
    Minelayer,      // Deploy space mines
    Relay,          // Communication relay (extends network)
}
```

### Gameplay Mechanics

**Scout Probes**:
- Fast, cheap, disposable
- Reveal fog of war in radius
- Detect enemy fleets at range
- Self-destruct or expire after fuel runs out

**Science Probes**:
- Scan planets for resources
- Analyze anomalies (wormholes, nebulae, ancient ruins)
- Gather intel for tech research

**Spy Probes**:
- Passive sensors (don't emit, harder to detect)
- Intercept enemy communications
- Provide intel on fleet movements, diplomatic chatter
- High stealth rating (0.9)

**Minelayer Probes**:
- Deploy space mines at designated coordinates
- Mines detonate when enemy ships approach
- Defensive area denial
- War crime if used near civilian traffic

**Relay Probes**:
- Extend communication network across vast distances
- Enable FTL communication between distant fleets
- Critical for coordinating multi-front wars
- High-value targets (destroying relay = isolate fleet)

---

# INFRASTRUCTURE

## Warp Relays

**Purpose**: FTL travel network, strategic chokepoints

### Component Structure

```csharp
public struct WarpRelay : IComponentData
{
    public Entity OwnerFaction;
    public float3 Position;             // Fixed in space
    public DynamicBuffer<WarpConnection> Connections;  // Linked relays
    public float PowerLevel;            // 0-1, can be damaged
    public bool IsPublic;               // Open to all factions vs. restricted
}

public struct WarpConnection : IBufferElementData
{
    public Entity TargetRelay;
    public float Distance;              // Light-years
    public float TravelTime;            // Hours/days
    public bool RequiresKey;            // Encrypted jump (faction-locked)
}
```

### Gameplay Mechanics

**Travel Network**:
- Relays form nodes in FTL network
- Ships jump from relay to relay (can't jump arbitrary distances)
- Creates strategic geography (chokepoints, trade routes)

**Construction**:
- Expensive megastructure (requires colony support)
- Build time: 30-60 days (real-time or sim-time)
- Requires rare resources (exotic matter, quantum cores)

**Strategic Importance**:
- Control relay = control region
- Blockade relay = cut off enemy fleets
- Destroy relay = isolate systems (scorched earth)

**Factional Access**:
```csharp
// Ship attempts jump
if (relay.IsPublic || relay.OwnerFaction == ship.Faction) {
    // Allowed
    ExecuteJump(ship, relay, targetRelay);
} else {
    // Access denied
    if (ship.HasTech("RelayHacking")) {
        // Attempt hack (takes time, detectable)
        var hackSuccess = AttemptRelayHack(ship, relay);
    } else {
        // Must use conventional travel (slow)
    }
}
```

**Sabotage**:
- Enemy agents can sabotage relay (disable, reduce power)
- Pirates can extort tolls (pay or be blocked)
- Maintenance required (player choice: neglect = decay)

---

## Beacons

**Purpose**: Navigation aids, markers, warnings

### Component Structure

```csharp
public struct Beacon : IComponentData
{
    public Entity Placer;               // Who deployed it
    public BeaconType Type;
    public float BroadcastRadius;       // Detection range (km)
    public FixedString64Bytes Message; // Custom text
    public bool IsEncrypted;            // Faction-only readable
}

public enum BeaconType : byte
{
    Navigation,     // "Safe route marker"
    Warning,        // "Danger: minefield ahead"
    Claim,          // "Territory of [Faction]"
    Distress,       // "SOS: need rescue"
    Trade,          // "Market: buying/selling goods"
    Trap,           // "Fake distress, ambush waiting"
}
```

### Gameplay Mechanics

**Navigation Beacons**:
- Mark safe routes through hazards (asteroid fields, nebulae)
- AI pathfinding uses beacons for optimal routes
- Public service (Good factions deploy freely)

**Warning Beacons**:
- "Minefield ahead"
- "Solar flare zone"
- "Restricted space: turn back"

**Claim Beacons**:
- Territory markers (factional borders)
- Violating claim = border incident (Compliance system)
- Can trigger diplomatic penalties or war

**Distress Beacons**:
- Automated SOS (from lifeboats, damaged ships)
- Factions respond based on alignment:
  - Good: Rescue
  - Neutral: Ignore or ransom
  - Evil: Capture/enslave

**Trap Beacons**:
- Fake distress signal
- Pirates/opportunists lure victims
- Moral conflict for Good factions (must investigate distress)

---

## Gateways

**Purpose**: Instant travel portals, advanced tech

### Component Structure

```csharp
public struct Gateway : IComponentData
{
    public Entity PairedGateway;        // Two-way link
    public float3 Position;
    public float PowerConsumption;      // Energy drain
    public int MaxShipSize;             // Tonnage limit
    public bool IsStable;               // Unstable = random destination
    public GatewayOrigin Origin;
}

public enum GatewayOrigin : byte
{
    Ancient,        // Precursor tech (found, not built)
    Constructed,    // Player-built (late-game tech)
    Natural,        // Wormhole (naturally occurring, unstable)
    Experimental    // Prototype (may malfunction)
}
```

### Gameplay Mechanics

**Instant Travel**:
- Ship enters gateway → instantly exits paired gateway
- No travel time (unlike warp relays)
- Bypasses entire regions (strategic flanking)

**Limitations**:
- Fixed pairs (can't retarget)
- Size limit (capitals may not fit)
- Power requirement (can be disabled if starved)

**Discovery**:
- Ancient gateways = exploration reward
- Activate dormant gateway = major tech unlock
- Paired destination unknown until first use (risk/reward)

**Unstable Gateways**:
```csharp
if (!gateway.IsStable) {
    // Random destination within radius
    var destinationRadius = 10000;  // 10k km scatter
    var actualExit = gateway.PairedGateway.Position +
                     RandomPointInSphere(destinationRadius);

    // 10% chance of catastrophic failure
    if (random.NextFloat() < 0.1f) {
        DestroyShip(ship, "Lost in transit");
    }
}
```

**Strategic Uses**:
- Surprise attacks (flank via gateway)
- Rapid reinforcement
- Emergency evacuation (retreat through gateway)
- Blockade bypass

---

# MEGASTRUCTURES

## Dyson Spheres

**Purpose**: Harness entire star's energy, endgame resource generation

### Component Structure

```csharp
public struct DysonSphere : IComponentData
{
    public Entity Star;                 // Which star is encased
    public float CompletionPercentage;  // 0-1 (100 years to build)
    public float EnergyOutput;          // Petawatts
    public int MaintenanceCrew;         // Thousands of workers
    public DysonType Type;
}

public enum DysonType : byte
{
    Swarm,          // Orbital solar collectors (partial, faster to build)
    Shell,          // Complete enclosure (100% efficiency, slow)
    Ring,           // Equatorial band (compromise)
}
```

### Gameplay Mechanics

**Construction**:
- Multi-decade project (10-100 real-time hours or accelerated)
- Requires massive resources (trillions of tons of material)
- Planet-scale industry to supply construction
- Can be built incrementally (1% → 100%)

**Energy Generation**:
```csharp
float energyPerTick =
    star.Luminosity *
    dysonSphere.CompletionPercentage *
    dysonSphere.EfficiencyModifier;

// Energy can power:
// - Entire faction (unlimited)
// - Megastructure construction
// - Planet terraforming
// - Gateway network
// - Experimental weapons (Death Star-scale)
```

**Strategic Value**:
- Controlling faction = energy superpower
- Target for sabotage (destroy key sections = cascade failure)
- Diplomatic leverage (sell energy to other factions)

**Narrative Potential**:
- First faction to complete Dyson = galactic dominance
- Ethical debates (obscuring star = kill planets in system)
- Ancient Dyson discovery = precursor mystery

---

## Ring Worlds

**Purpose**: Massive habitable surface, post-scarcity living

### Component Structure

```csharp
public struct RingWorld : IComponentData
{
    public Entity Star;                 // Orbits which star
    public float Radius;                // Orbital radius (1 AU)
    public float SurfaceArea;           // Earth-equivalents (millions)
    public long Population;             // Trillions of inhabitants
    public float Stability;             // 0-1, structural integrity
}
```

### Gameplay Mechanics

**Scale**:
- Surface area = millions of Earths
- Population in trillions
- Self-sufficient (post-scarcity utopia or dystopia)

**Construction**:
- Even longer than Dyson (centuries)
- Requires Dyson-tier energy
- Player likely never completes (pass down generations)

**Gameplay Purpose**:
- Victory condition ("Build Ring World" = win)
- Narrative endpoint (civilization peak)
- Exploration (different regions = different cultures)

---

## Star Forges

**Purpose**: Stellar engineering, create/destroy stars

### Component Structure

```csharp
public struct StarForge : IComponentData
{
    public Entity TargetStar;
    public ForgeOperation Operation;
    public float Progress;              // 0-1
    public float EnergyRequired;        // Dyson-scale power
}

public enum ForgeOperation : byte
{
    Ignition,       // Create new star from gas cloud
    Stabilization,  // Prevent supernova
    Extraction,     // Mine star for exotic matter
    Detonation      // Weaponized supernova (war crime)
}
```

### Gameplay Mechanics

**Star Ignition**:
- Turn gas giant into brown dwarf
- Create new habitable zone (warm outer planets)
- 50-year project

**Star Detonation**:
- Trigger supernova
- Destroy entire system
- Ultimate weapon (galactic-scale genocide)
- All factions declare war on user (mutual defense)

---

# DARK FLEET (Cultural/Alignment-Gated)

## Slaveships

**Purpose**: Forced labor transport (Evil/Corrupt factions only)

### Component Structure

```csharp
public struct Slaveship : IComponentData
{
    public int SlaveCapacity;           // Thousands
    public float ConditionRating;       // 0-1 (determines death rate)
    public Entity OwnerFaction;
    public bool IsConcealed;            // Hidden cargo (smuggling)
}

public struct Slave : IBufferElementData
{
    public Entity CapturedEntity;       // Original person
    public uint CapturedTick;
    public float Morale;                // Always low
    public bool HasRebellionIntent;     // Uprising risk
}
```

### Cultural Gating

```csharp
// Only Evil or Corrupt factions can build/operate
if (faction.Alignment.Good > 0 || faction.Alignment.Integrity > 30) {
    // Cannot build slaveships (ethical opposition)
    // Tech tree locked
}

// Good factions encountering slaveship:
if (scanner.Faction.Alignment.Good > 50 && detected.Type == VesselType.Slaveship) {
    // Moral imperative to free slaves
    TriggerEvent("LibrateSlaves", scanner, detected);
}
```

### Gameplay Mechanics

**Capture**:
- Pirates/raiders capture civilians from colonies
- Load onto slaveship
- Sell at slave markets (forbidden zones)

**Rebellion**:
```csharp
float rebellionChance =
    (1.0f - slaveship.ConditionRating) * 0.4f +
    slaveLeader.Initiative * 0.3f +
    slaveCount / slaveCapacity * 0.3f;  // Overcrowding

if (random.NextFloat() < rebellionChance) {
    // Slaves revolt
    TakeoverShip(slaveship, slaves);
    // Becomes free crew, may join liberating faction
}
```

**Liberation**:
- Good factions can raid slaveships
- Free captives
- Return to home factions (diplomatic bonus)
- Slaveship crew executed or imprisoned

**Economic Model**:
- Slaves = cheap labor (colonies, mining, construction)
- Factions using slaves = higher production (−50% cost)
- Diplomatic penalty (all Good factions hostile)
- Risk of slave uprisings (internal instability)

---

## Prison Barges

**Purpose**: Transport captured criminals, POWs

### Component Structure

```csharp
public struct PrisonBarge : IComponentData
{
    public int PrisonerCapacity;
    public float SecurityRating;        // 0-1 (prevent escapes)
    public Entity Destination;          // Prison colony, penal system
}

public struct Prisoner : IBufferElementData
{
    public Entity CapturedEntity;
    public CrimeType Crime;             // War criminal, pirate, defector
    public int SentenceRemaining;       // Ticks until release
    public float EscapeAttemptRisk;     // 0-1
}
```

### Gameplay Mechanics

**Capture Sources**:
- Defeated enemy crews (captured lifeboat)
- Warrior Pilot Last Stand captures
- Boarding party prisoners

**Destination**:
- Prison colonies (forced labor)
- Execution facilities (war criminals)
- Rehabilitation centers (Good factions)
- Ransom exchanges (neutral ground)

**Jailbreak**:
- Pirates attack prison barge (free comrades)
- Prisoner uprising (security failure)
- Player rescue mission (extract captured ally)

---

# POCKET UNIVERSES

## Dungeon Instances

**Purpose**: Instanced combat/exploration zones, ancient ruins, derelicts

### Component Structure

```csharp
public struct PocketUniverse : IComponentData
{
    public PocketType Type;
    public Entity EntryGateway;         // Portal to enter
    public int MaxOccupants;            // Player limit (4-8 ships)
    public float TimeScale;             // Time flows differently (0.5x or 2x)
    public bool IsPersistent;           // Permanent or temporary
}

public enum PocketType : byte
{
    AncientRuin,        // Precursor dungeon (loot + lore)
    Derelict,           // Abandoned megaship (salvage)
    LabyrinthNebula,    // Maze-like nebula (navigation puzzle)
    TestingGrounds,     // Combat arena (PvP or PvE)
    DimensionalRift,    // Unstable space (danger + reward)
    Sanctuary,          // Safe zone (no combat allowed)
}
```

### Gameplay Mechanics

**Entry**:
- Discover gateway (exploration reward)
- Enter with fleet (limited size)
- Instance separate from main galaxy (pause main sim)

**Objectives**:
- **Ancient Ruins**: Solve puzzles, fight guardians, loot tech
- **Derelicts**: Salvage resources, discover logs (narrative)
- **Labyrinths**: Navigate maze, avoid hazards, escape
- **Arenas**: Survive waves of enemies, earn rewards
- **Rifts**: Risk/reward (high danger, rare loot)

**Time Dilation**:
```csharp
// Time flows differently inside
if (pocketUniverse.TimeScale < 1.0f) {
    // Slow time (2 hours outside = 1 hour inside)
    // Training grounds (skill up crew faster)
}
else if (pocketUniverse.TimeScale > 1.0f) {
    // Fast time (1 hour outside = 2 hours inside)
    // Danger zones (age rapidly, decay faster)
}
```

**Loot Tables**:
- Ancient tech (unique ship modules)
- Precursor knowledge (research boost)
- Rare resources (exotic matter, dark energy)
- Crew recruitment (survivors, AIs)

**Persistence**:
- Non-persistent: Reset after exit (repeatable farming)
- Persistent: Changes permanent (one-time loot)

---

## Alternate Galactic Maps

**Purpose**: Multiple galaxies, parallel dimensions, conquest variety

### Component Structure

```csharp
public struct GalacticMap : IComponentData
{
    public int MapID;                   // Unique identifier
    public MapType Type;
    public int StarSystemCount;         // 100-10000 systems
    public float DifficultyModifier;    // Enemy strength multiplier
}

public enum MapType : byte
{
    HomeGalaxy,         // Starting map
    DwarfGalaxy,        // Small (100 systems)
    SpiralGalaxy,       // Medium (1000 systems)
    EllipticalGalaxy,   // Large (5000 systems)
    IrregularGalaxy,    // Chaotic (random structure)
    VoidSpace,          // Empty void between galaxies
}
```

### Gameplay Mechanics

**Intergalactic Travel**:
- Build **Intergalactic Gateway** (ultra-endgame)
- Jump to new galaxy
- Start with scout fleet (establish foothold)

**Parallel Campaigns**:
- Main galaxy: Ongoing war with Faction A
- Secondary galaxy: Expand unopposed
- Tertiary galaxy: Ancient enemy awakens

**Map Generation**:
- Procedural (seed-based, deterministic)
- Handcrafted (designed encounters)
- Hybrid (procedural with fixed landmarks)

**Cross-Galaxy Resources**:
- Some resources only in specific galaxies
- Trade routes between galaxies (massive logistics)
- Gateway taxes/tolls (economic gameplay)

**Victory Conditions**:
- Conquer all galaxies (multi-galaxy empire)
- Control key galaxies (capital, tech, resource)
- Destroy all gateways (isolationist victory)

---

# INTEGRATION WITH EXISTING SYSTEMS

## Rewind & Determinism

All vessels/structures must be **deterministic**:
```csharp
// Probe launch example
uint seed = CombineHashes(launchTick, ship.Entity.Index);
var random = new Random(seed);
var scatterDirection = random.NextFloat3Direction();

// Always produces same scatter for same tick + entity
```

## Manual Aim (Divine Guidance)

**Compatible vessels**:
- Boarding craft: Aim at specific ship section
- Drop pods: Precise landing coordinates
- Probes: Trajectory to avoid detection

**Trajectory prediction**:
- Account for gravity wells (planets, stars)
- Avoid hazards (asteroid fields, mines)
- Optimal approach vector (stealth vs. speed)

## Alignment & Culture

**Factional Access**:
| Vessel/Structure | Good | Neutral | Evil |
|------------------|------|---------|------|
| Boarding Craft | ✅ (negotiation) | ✅ (all tactics) | ✅ (forced breach) |
| Lifeboats | ✅ (must rescue) | ⚠️ (may ransom) | ❌ (may execute) |
| Slaveships | ❌ (forbidden) | ⚠️ (illegal but smuggled) | ✅ (legal) |
| Prison Barges | ✅ (rehabilitation) | ✅ (standard) | ✅ (death camps) |
| Gateways | ✅ (public access) | ⚠️ (tolls) | ❌ (restricted) |

## Warrior Pilot Last Stand

**Complementary Systems**:
- Boarding craft deliver organized assaults
- Last Stand pilots are desperate individuals
- Can join forces inside enemy ship

**Lifeboat Interaction**:
- Pilots may eject to lifeboat instead of Last Stand
- Cultural trait determines priority (honor vs. survival)

---

# COMPONENT SUMMARY

## Utility Vessels

```csharp
// All inherit from base vessel
public struct Vessel : IComponentData
{
    public Entity Owner;
    public VesselType Type;
    public float3 Position;
    public float3 Velocity;
    public float Hull;
    public float Fuel;
}

// Specific types add components:
// - BoardingCraft
// - DropPod
// - Shuttle
// - Lifeboat
// - Probe
```

## Infrastructure

```csharp
// All inherit from base structure
public struct SpaceStructure : IComponentData
{
    public Entity OwnerFaction;
    public float3 Position;          // Fixed in space
    public float Integrity;          // 0-1 (damaged by attacks)
    public StructureType Type;
}

// Specific types:
// - WarpRelay
// - Beacon
// - Gateway
// - DysonSphere (megastructure)
// - RingWorld (megastructure)
// - StarForge (megastructure)
```

## Pocket Universes

```csharp
public struct PocketUniverse : IComponentData
{
    public int UniverseID;
    public PocketType Type;
    public Entity EntryGateway;
    public float TimeScale;
    public DynamicBuffer<OccupyingFleet> Occupants;
}

// Instances are separate World contexts (DOTS)
// Each pocket = new World with own EntityManager
```

---

# PATTERN BIBLE ENTRIES

## "The Desperate Rescue"

**Scope**: Cross-Vessel (Shuttle + Lifeboat)

**Preconditions**:
- Ally ship destroyed, lifeboats launched
- Player has rescue shuttle available
- Limited time (fuel depleting)
- Enemy ships nearby

**Gameplay Effects**:
- Player manually guides shuttle to lifeboat (Divine Guidance)
- Time pressure (must reach before fuel runs out)
- Enemy may intercept (moral conflict: risk shuttle to save crew?)
- Success: Crew rescued (+30 morale to faction)
- Failure: Crew lost in space (−20 morale, grudge against enemy)

**Narrative Hook**: "The pilot who risked everything to bring their comrades home."

**Priority**: Nice-to-have

---

## "The Slave Uprising"

**Scope**: Slaveship + Cultural Dynamics

**Preconditions**:
- Slaveship with poor conditions (<0.3 rating)
- High slave count (overcrowded)
- Slave leader with high initiative (>0.7)
- No security forces nearby

**Gameplay Effects**:
- Slaves revolt, kill crew
- Slaveship becomes free vessel
- Slaves offered choice:
  - Join liberating faction (if Good)
  - Form pirate band (if chaotic)
  - Return home (if lawful)
- Owner faction loses reputation (−50, "Slavers")
- May trigger "Abolitionist War" (Good factions unite against slavery)

**Narrative Hook**: "The enslaved who broke their chains and became legend."

**Priority**: Core (if slavery system implemented)

---

## "The Ancient Gateway"

**Scope**: Gateway + Exploration

**Preconditions**:
- Player discovers dormant ancient gateway
- Destination unknown
- Gateway unstable (10% ship loss risk)
- Tech level insufficient to analyze safely

**Gameplay Effects**:
- Player choice: Risk jump or study gateway first
- If jump: Random destination (treasure, danger, or nowhere)
  - 40% treasure (resource-rich system, tech)
  - 40% danger (hostile entities, dimensional anomaly)
  - 10% nowhere (empty space, wasted jump)
  - 10% catastrophic (ship destroyed)
- If study: Takes 500 ticks, reveals destination, but enemy may beat you there

**Narrative Hook**: "The explorer who gambled everything on an unknown portal."

**Priority**: Nice-to-have

---

## "The Dyson Dilemma"

**Scope**: Megastructure + Ethics

**Preconditions**:
- Faction completes Dyson Sphere around inhabited star
- Planets in system lose sunlight (mass starvation)
- Population pleads for mercy (tear down Dyson or evacuate them)

**Gameplay Effects**:
- Player choice:
  - **Dismantle Dyson**: Lose megastructure, save population (Good alignment +20)
  - **Evacuate planets**: Costly, slow, but saves lives and keeps Dyson (Neutral)
  - **Ignore pleas**: Billions die, keep Dyson (Evil alignment −30, galactic outcry)
- Galactic response:
  - Good factions may declare war (prevent genocide)
  - Evil factions respect power ("Might makes right")
- Long-term: Dyson = energy superpower, but moral stain

**Narrative Hook**: "The empire that built a utopia on a graveyard."

**Priority**: Wild experiment

---

# IMPLEMENTATION ROADMAP

## Phase 1: Utility Vessels (6 weeks)
- [ ] Boarding craft (complement to Last Stand)
- [ ] Drop pods (planetary assault)
- [ ] Shuttles (logistics, non-combat)
- [ ] Lifeboats (escape mechanics)
- [ ] Probes (reconnaissance)

## Phase 2: Infrastructure (4 weeks)
- [ ] Warp relays (FTL network)
- [ ] Beacons (navigation, warnings)
- [ ] Gateways (instant travel)

## Phase 3: Megastructures (8 weeks)
- [ ] Dyson spheres (energy generation)
- [ ] Ring worlds (massive habitats)
- [ ] Star forges (stellar engineering)

## Phase 4: Dark Fleet (3 weeks) [OPTIONAL]
- [ ] Slaveships (Evil factions only)
- [ ] Prison barges (POW transport)
- [ ] Ethical frameworks (moral conflict integration)

## Phase 5: Pocket Universes (6 weeks)
- [ ] Dungeon instances (ruins, derelicts)
- [ ] Alternate galactic maps (multi-galaxy)
- [ ] Intergalactic gateways

**Total**: ~27 weeks (6-7 months) for full implementation

---

## Success Metrics

**Vessel Diversity**:
- % of player fleets using utility vessels (target: 30%+)
- Boarding craft vs. Last Stand usage ratio (should be balanced)
- Lifeboat rescue frequency (measure player altruism)

**Infrastructure Impact**:
- Strategic value of warp relay control (should influence wars)
- Gateway discovery excitement (player feedback)
- Megastructure completion rate (aspirational, rarely finished)

**Narrative Richness**:
- Player stories involving utility vessels (rescue missions, slave liberations)
- Ethical dilemmas frequency (Dyson decision, slave encounters)
- Pocket universe exploration engagement

---

## Conclusion

This ecosystem of **miscellaneous vessels, infrastructure, and pocket universes** transforms Space4X from pure fleet combat into a **rich strategic tapestry**:

- **Utility vessels** add depth (boarding, rescue, reconnaissance)
- **Infrastructure** creates strategic geography (relay networks, gateways)
- **Megastructures** provide long-term aspirational goals (Dyson, ring worlds)
- **Dark fleet** introduces moral complexity (slavery, ethics)
- **Pocket universes** offer exploration variety (dungeons, alternate galaxies)

Every system integrates with **existing mechanics** (alignment, rewind, manual aim, cultural traits), creating **emergent gameplay** where player choices define their civilization's character.

**"Will you build a Dyson Sphere on a graveyard? Free slaves or profit from them? Risk the unknown gateway or play it safe?"**

These are the stories that make Space4X memorable.
