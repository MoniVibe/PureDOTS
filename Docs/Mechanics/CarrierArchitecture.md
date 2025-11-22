# Ship Architecture (Space4X)

## Overview

Ships in Space4X are modular entities with customizable loadouts, facilities, and crew assignments. This document defines the ship classification system (Capital Ships, Regular Ships, Vessels/Crafts), mount-based module framework, weapon research/manufacturing system, and crew specialization mechanics.

---

## Core Concept

**Ships are classified by independence and customizability:**
- **Capital Ships**: Highly customizable, independent operations, from personal yachts to super-massive titans
- **Regular Ships**: Moderate customizability, standard military classifications (Shuttle → Dreadnaught)
- **Vessels/Crafts**: Carried by larger ships, rarely independent, customizable loadouts

All ships use a **mount-based module system** where modules have:
- **Mount Type** (where it goes on the ship: Main Gun, Missile Rack, PD Slot, Utility Bay, Spinal)
- **Mass** (affects ship performance)
- **Power Requirements** (budget constraint)
- **Weapon Class** (how it behaves: Beam Cannon, Mass Driver, Missile, etc.)
- **Target Profile** (what it's good at: shields, armor, small/large targets, swarms)
- **Mods** (research-unlocked upgrades: MIRV, Shotgun, EMP, etc.)

---

## Ship Classifications

### 1. Capital Ships

**Definition**: Independent, highly customizable ships ranging from ultra-light personal yachts to super-massive titans.

**Customizability**:
- Full control over module loadouts (weapons, utilities, facilities)
- Custom facility installations (fabrication bays, research labs, colony modules)
- No fixed hull templates (player designs from scratch or heavily modifies base hulls)

**Examples**:
- **Personal Yacht**: Ultra-light, 1-10 crew, luxury/exploration focused
- **Merchant Freighter**: Medium capital, 50-200 crew, massive cargo + fabrication
- **Fleet Carrier**: Heavy capital, 500-2000 crew, hangar bays for hundreds of crafts
- **Titan**: Super-massive, 5000-20000 crew, mobile fortress with all facilities

```csharp
public struct CapitalShip : IComponentData
{
    public CapitalClass Class;
    public ushort CrewCapacity;
    public float MassLimit;                      // Max module mass before penalties
    public ushort PowerGeneration;               // Base power output
    public bool IsIndependent;                   // Can operate without mothership
}

public enum CapitalClass : byte
{
    PersonalYacht,      // Ultra-light
    MerchantFreighter,  // Medium
    MilitaryCruiser,    // Heavy
    FleetCarrier,       // Super-heavy
    Titan,              // Super-massive
    MobileStation       // Gargantuan (planet-killer scale)
}
```

---

### 2. Regular Ships

**Definition**: Standardized military/civilian ships with moderate customizability. Fixed hull templates with weapon/module variation.

**Classification** (ascending order):
1. **Shuttle**: 1-5 crew, transport/utility
2. **Corvette**: 5-20 crew, patrol/escort
3. **Frigate**: 20-50 crew, anti-fighter/screening
4. **Destroyer**: 50-150 crew, anti-ship/versatile
5. **Cruiser**: 150-500 crew, fleet anchor/multi-role
6. **Battleship**: 500-2000 crew, heavy firepower/tank
7. **Dreadnaught**: 2000-10000 crew, flagship/terror weapon

**Customizability**:
- Fixed hull templates (e.g., "Destroyer Hull Mk3" has predetermined slot layout)
- Variable weapon loadouts (player chooses which weapons in fixed slots)
- Module variations (shield type, engine type, sensor suite)
- Less flexible than Capital Ships, but cheaper/faster to produce

```csharp
public struct RegularShip : IComponentData
{
    public RegularShipClass Class;
    public FixedString64Bytes HullTemplate;     // "Frigate Hull Mk2", "Destroyer Hull Vanguard"
    public ushort CrewCapacity;
    public float MassLimit;
    public ushort PowerGeneration;
}

public enum RegularShipClass : byte
{
    Shuttle,
    Corvette,
    Frigate,
    Destroyer,
    Cruiser,
    Battleship,
    Dreadnaught
}
```

---

### 3. Vessels & Crafts

**Definition**: Small crafts carried by Capital Ships or Regular Ships. Rarely independent (orphaned, privately owned, mercenary).

**Types**:
- **Fighters**: Anti-craft combat
- **Bombers**: Anti-ship torpedoes
- **Interceptors**: Fast pursuit/patrol
- **Shuttles**: Transport/boarding
- **Mining Rigs**: Autonomous extractors
- **Repair Drones**: Field maintenance

**Customizability**:
- Loadout variations (weapons, equipment)
- Improvable via research (prototype crafts)
- Manufacturable mods (engine boost, armor plates, targeting AI)

**Independence**:
- **Docked** (normal): Stored in mothership hangar
- **Deployed** (active): Operating independently
- **Orphaned** (rare): Mothership destroyed, operating solo (limited endurance)
- **Mercenary** (rare): Privately owned, hired by factions

```csharp
public struct VesselCraft : IComponentData
{
    public CraftType Type;
    public Entity Mothership;                    // Entity.Null if orphaned/independent
    public CraftStatus Status;
    public ushort FuelRemaining;                 // Ticks until must return to mothership
}

public enum CraftType : byte
{
    Fighter,
    Bomber,
    Interceptor,
    Shuttle,
    MiningRig,
    RepairDrone,
    ScoutProbe
}

public enum CraftStatus : byte
{
    Docked,         // Stored in hangar
    Deploying,      // Launch sequence
    Deployed,       // Active operations
    Returning,      // RTB (return to base)
    Orphaned,       // Mothership lost, operating solo
    Mercenary       // Independent contractor
}
```

---

## Mount-Based Module System

### Mount Types (Slots)

Ships have **mount points** where modules are installed. Each mount type accepts specific module categories.

```csharp
public struct ShipMount : IBufferElementData
{
    public MountType Type;
    public MountSize Size;                       // Small/Medium/Large/Spinal
    public Entity InstalledModule;               // Entity.Null if empty
    public float3 MountPosition;                 // Relative to ship hull (for VFX/targeting)
    public quaternion MountRotation;             // Turret orientation
    public byte MountHealth;                     // 0-100 (damaged mounts reduce effectiveness)
}

public enum MountType : byte
{
    MainGun,        // Big forward/broadside weapons (heavy beams, railguns, plasma cannons)
    MissileRack,    // Internal/external launch cells (missiles, torpedoes, drones, decoys)
    PointDefense,   // Small hull hardpoints, auto-firing (PD lasers, flak, interceptors)
    UtilityBay,     // Internal non-weapon slots (hangars, sensors, reactors, shields, fabrication)
    Spinal          // One super-weapon down the middle (superlaser, mega railgun) - rare, specific hulls only
}

public enum MountSize : byte
{
    Small,          // Light weapons, basic utilities
    Medium,         // Standard weapons, moderate utilities
    Large,          // Heavy weapons, major facilities
    Spinal          // Super-weapon (only for Spinal mount type)
}
```

**Example Hull Layout**:
```
Cruiser Hull "Vanguard"
  Main Guns: 2 (Medium)
  Missile Racks: 3 (2 Medium, 1 Large)
  PD Slots: 6 (All Small)
  Utility Bays: 3 (1 Large, 2 Medium)
  Spinal: None

Battleship Hull "Dominion"
  Main Guns: 4 (2 Large, 2 Medium)
  Missile Racks: 4 (All Large)
  PD Slots: 12 (All Small)
  Utility Bays: 6 (2 Large, 4 Medium)
  Spinal: 1 (Spinal - optional mega railgun)
```

---

## Module Categories & Components

### Core Module Component

```csharp
public struct ShipModule : IComponentData
{
    public ModuleFamily Family;                  // Weapon, Defense, Utility, Facility, Colony
    public ModuleClass Class;                    // Specific behavior (BeamCannon, MassDriver, etc.)
    public FixedString64Bytes ModuleName;        // "Storm Lance Battery", "Guardian Missile System"

    // Physical properties
    public float Mass;                           // Affects ship performance
    public ushort PowerRequired;                 // Power budget consumption
    public MountType RequiredMount;              // Where it can be installed

    // Performance
    public byte EfficiencyPercent;               // 0-100 (degradation + crew skill + tech level)
    public ModuleState State;                    // Offline/Standby/Active/Damaged/Destroyed

    // Targeting (weapons only)
    public TargetProfile TargetProfile;          // What it's good at

    // Mods
    public DynamicBuffer<InstalledMod> Mods;     // Research-unlocked upgrades
}

public enum ModuleFamily : byte
{
    Weapon,         // Combat modules
    Defense,        // Shields, armor, PD
    Utility,        // Engines, sensors, cargo
    Facility,       // Fabrication, research, medical
    Colony          // Colony infrastructure (NEW CATEGORY)
}

public enum ModuleClass : byte
{
    // Weapons
    BeamCannon,         // Continuous beam (lasers, particle beams)
    MassDriver,         // Kinetic projectiles (railguns, coilguns)
    Missile,            // Guided munitions (torpedoes, missiles, drones)
    PointDefense,       // Anti-missile/fighter (PD lasers, flak)

    // Defense
    Shield,             // Energy shields
    Armor,              // Physical plating

    // Utility
    Engine,             // Thrusters, FTL drives
    Sensor,             // Scanners, targeting
    Cargo,              // Storage, fuel tanks

    // Facility
    Fabrication,        // Manufacturing
    Research,           // Tech development
    Medical,            // Crew health
    Hangar,             // Craft storage/launch

    // Colony (NEW)
    Habitation,         // Population housing
    Agriculture,        // Food production
    Mining,             // Resource extraction
    Terraforming,       // Planetary modification
    Administration      // Governance infrastructure
}

public enum TargetProfile : byte
{
    ShieldBreaker,      // +damage vs shields, -damage vs armor
    ArmorPiercer,       // +damage vs armor, -damage vs shields
    AntiSmall,          // +accuracy vs fighters/corvettes, -damage vs capitals
    AntiBig,            // +damage vs capitals, -accuracy vs small/fast
    AntiSwarm,          // Area effect, +damage vs grouped targets
    Balanced            // No bonuses/penalties
}

public enum ModuleState : byte
{
    Offline,        // Unpowered
    Standby,        // Powered but inactive
    Active,         // Operational
    Damaged,        // Reduced efficiency
    Destroyed       // Non-functional
}
```

---

## Weapon System Design

### Weapon Families & Cross-Tech Mods

**Step 1: Base Weapon Families** (Early Tech Tree)

```csharp
// Tech unlocks that provide base weapon types
public enum WeaponFamilyTech : ushort
{
    BeamCannonTech_I,       // Basic laser turrets
    MassDriverTech_I,       // Basic railguns
    MissileTech_I,          // Basic missiles
    PointDefenseTech_I      // Basic PD grid
}
```

**Step 2: Cross-Family Mods** (Research-Unlocked Tweaks)

These are **universal tech unlocks** that apply to compatible weapon families.

```csharp
public enum WeaponModTech : ushort
{
    // Ammo/Warhead Mods
    MIRV_Warheads,          // Applies to: Missiles, Mass Drivers (cluster shells)
    EMP_Warheads,           // Applies to: Missiles, Beam Cannons (ion beams)
    Armor_Piercing,         // Applies to: Mass Drivers, Missiles (shaped charges)
    Incendiary,             // Applies to: Mass Drivers, Missiles (burn damage over time)

    // Focusing/Pattern Mods
    Shotgun_Focusing,       // Applies to: Beam Cannons (laser shotguns), PD Lasers (wide arc)
    Narrow_Beam,            // Applies to: Beam Cannons (long range, high accuracy, low spread)
    Spread_Pattern,         // Applies to: Mass Drivers (flechette rounds)

    // Performance Mods
    Overcharger_Arrays,     // Applies to: Beam Cannons, Mass Drivers (alpha strike variants, cooldown penalty)
    Rapid_Fire,             // Applies to: Mass Drivers, PD (high RoF, lower damage per shot)
    Extended_Range,         // Applies to: All weapons (range +50%, accuracy -20%)

    // Targeting/AI Mods
    AI_Targeting_Cores,     // Applies to: All weapons (tracking +30% vs evasive targets)
    Predictive_Tracking,    // Applies to: Missiles, Beam Cannons (lead targets better)
    Proximity_Fuse,         // Applies to: Missiles (explode near target, good vs swarms)

    // Exotic Mods (late-game)
    Quantum_Entanglement,   // Applies to: Beam Cannons (ignores shields, passes through)
    Graviton_Lensing,       // Applies to: Mass Drivers (bends trajectory mid-flight)
    Nano_Swarm,             // Applies to: Missiles (releases nanobots that eat armor)
}
```

**Step 3: Doctrine-Level Techs** (Fleet-Wide Buffs)

```csharp
public enum DoctrineTech : ushort
{
    Shield_Overload_Doctrine,       // +20% beam damage vs shields, beams cause shield failures
    Armor_Warfare_Doctrine,         // Mass drivers pierce deeper, armor modules cheaper
    Missile_Saturation_Doctrine,    // Missile racks reload +30% faster, -10% damage per missile
    Point_Defense_Network,          // All PD in fleet shares targeting data, +25% effectiveness
    Alpha_Strike_Doctrine,          // First volley +50% damage, reload time +100%
    Sustained_Fire_Doctrine         // Continuous fire +20% DPS, overheating reduced
}
```

### Installed Mods on Modules

```csharp
public struct InstalledMod : IBufferElementData
{
    public WeaponModTech ModType;
    public byte ModTier;                         // 1-5 (researched tier)
    public float EffectMultiplier;               // 1.0 = base, 1.5 = tier 5
    public ushort ManufacturingQuality;          // 0-100 (how well-made this mod is)
    public ushort UsageExperience;               // XP gained from using this mod (improves over time)
}
```

**Example Weapon with Mods**:
```
Module: "Storm Lance Battery Mk3"
  Family: Weapon
  Class: BeamCannon
  Target Profile: ShieldBreaker / AntiSmall
  Mount: Main Gun (Medium)
  Mass: 150 tons
  Power: 80 units

  Installed Mods:
    - AI Targeting Cores (Tier 3, +25% tracking vs evasive)
    - Overcharger Arrays (Tier 2, +40% alpha damage, 2x cooldown)
    - Extended Range (Tier 1, +50% range, -20% accuracy)

  Manufacturing Quality: 87% (high-quality build)
  Usage Experience: 4500 XP (crew proficient with this exact loadout)
```

---

## Module Manufacturing & Proficiency

### Manufacturing System

Modules are **manufactured** in facilities (not just unlocked by research).

```csharp
public struct ModuleManufacturing : IComponentData
{
    public ModuleClass ModuleType;
    public Entity ManufacturingFacility;         // Which facility is building this
    public ushort ProductionProgress;            // 0-100%
    public byte QualityRoll;                     // 0-100 (random + facility tier + worker skill)
    public TechTier TechTier;                    // Higher tier = better base stats
}

public enum TechTier : byte
{
    Primitive,      // Tier 0-1 tech
    Industrial,     // Tier 2-3 tech
    Advanced,       // Tier 4-5 tech
    Exotic,         // Tier 6-7 tech
    Transcendent    // Tier 8+ tech
}
```

**Manufacturing Quality**:
- **Base Quality** = Facility Tier (0-100) + Worker Skill (0-50) + Random (-10 to +10)
- **Higher Quality** = +Efficiency, +Durability, -Degradation Rate
- **Prototype Bonus**: First-time manufacture of new mod combo gets +20% XP gain when used

**Manufacturing Proficiency**:
- Facilities gain XP for each module type manufactured
- Higher proficiency = faster production, higher quality, lower cost
- Encourages specialization (this shipyard makes great beam cannons, that one makes missiles)

### Module Usage Proficiency (Crew & Manufacturer XP)

**Modules do NOT gain experience themselves.** Instead, their **operating crews** and **manufacturing facilities** gain proficiency.

**Crew Proficiency** (already tracked in CrewMember.RoleXP):
- Crew gain XP when operating modules in combat/operations
- Weapon operator fires beam cannon: +10 XP to WeaponsSkill + BeamWeaponSpec (if specialized)
- Engineering crew operates reactor under load: +5 XP per 100 ticks
- Crew XP translates to module efficiency bonus (see Crew Assignment section)

**Manufacturer Proficiency** (tracked in facility):
```csharp
public struct FacilityManufacturingExperience : IComponentData
{
    public Entity FacilityEntity;
    public DynamicBuffer<ModuleTypeExperience> ModuleTypeXP;
}

public struct ModuleTypeExperience : IBufferElementData
{
    public ModuleClass ModuleType;               // BeamCannon, MassDriver, etc.
    public ushort TotalUnitsProduced;            // How many built
    public ushort AccumulatedXP;                 // XP from production
    public byte ProficiencyLevel;                // 0-10 (derived from XP)
}
```

**Manufacturer Proficiency Effects**:
- **Level 0-2**: Base production (standard quality, cost, time)
- **Level 3-5**: +10-20% quality, -10% cost, -15% production time
- **Level 6-8**: +25-35% quality, -20% cost, -30% production time
- **Level 9-10**: +40-50% quality, -30% cost, -50% production time (legendary shipyard)

**Proficiency XP Sources**:
- Module manufactured: +XP based on module complexity (beam cannon = 100 XP, spinal weapon = 500 XP)
- Prototype manufactured (first of mod combo): +100% XP bonus
- Challenging production (low resources, time pressure): +50% XP

---

## Crew Specialization

### Crew Roles & Specialization

Crew can be **generalists** or **specialists**.

```csharp
public struct CrewMember : IComponentData
{
    public Entity AssignedShip;
    public CrewRole PrimaryRole;                 // What they're trained for
    public CrewSpecialization Specialization;    // Narrow expertise (optional)

    // Skills (0-100 each)
    public byte WeaponsSkill;                    // Operating weapons
    public byte EngineeringSkill;                // Engines, power, repairs
    public byte SensorsSkill;                    // Scanners, targeting
    public byte PilotingSkill;                   // Helm control
    public byte CommandSkill;                    // Leadership, tactics

    // Experience
    public ushort TotalXP;
    public DynamicBuffer<RoleExperience> RoleXP; // XP per role (tracks cross-training)
}

public enum CrewRole : byte
{
    Generalist,         // Can operate anything (penalty to all)
    Weapons,            // Guns, missiles, PD
    Engineering,        // Engines, power, damage control
    Sensors,            // Scanning, targeting, intel
    Pilot,              // Helm, navigation
    Command,            // Captain, tactics, morale
    Medical,            // Crew health, recovery
    Science             // Research, tech analysis
}

public enum CrewSpecialization : byte
{
    None,               // Generalist within role

    // Weapon Specializations
    BeamWeaponSpec,     // +30% efficiency with beam cannons
    MissileSpec,        // +30% efficiency with missiles
    PDSpec,             // +30% efficiency with point defense
    HeavyWeaponSpec,    // +30% efficiency with spinal/super-weapons

    // Engineering Specializations
    ReactorSpec,        // +Power generation, +reactor safety
    FTLSpec,            // +FTL jump range/speed
    DamageControlSpec,  // +Repair speed, +emergency power

    // Sensor Specializations
    TacticalSensorSpec, // +Targeting accuracy for weapons
    LongRangeSpec,      // +Sensor range, +intel quality
    ECMSpec,            // Electronic warfare, jamming

    // Other Specializations
    CombatPilotSpec,    // +Evasion, +maneuverability
    StrategicNavSpec    // +FTL efficiency, +route planning
}

public struct RoleExperience : IBufferElementData
{
    public CrewRole Role;
    public ushort XP;                            // XP accumulated in this role
    public byte EffectivePenalty;                // 0-50% (penalty when operating non-primary role)
}
```

### Crew Assignment & Penalties

**Generalist Operation**:
- Crew with `PrimaryRole = Generalist` can operate ANY module
- **Penalty**: -30% efficiency on all modules (jack of all trades, master of none)
- **Benefit**: Flexibility (can fill any role shortage)

**Specialist Operation**:
- Crew with `PrimaryRole = Weapons` operating weapon modules: **Full efficiency**
- Crew with `PrimaryRole = Weapons` operating engine modules: **-40% efficiency** (out of specialty)
- **Cross-Training**: Crew gain XP in non-primary roles over time, reducing penalty

**Specialization Operation**:
- Crew with `Specialization = BeamWeaponSpec` operating beam cannons: **+30% efficiency**
- Crew with `Specialization = BeamWeaponSpec` operating missiles: **-20% efficiency** (narrow specialty)
- **Extreme Weapons**: Some weapons REQUIRE specialists (e.g., Spinal super-weapons need `HeavyWeaponSpec`)

**Training System**:
```csharp
// Crew operating non-primary role gains XP slowly
if (crewRole != moduleRole)
{
    float xpGain = baseXP * 0.3f; // 30% normal XP
    roleXP[moduleRole] += xpGain;

    // Reduce penalty over time
    if (roleXP[moduleRole] > 1000)
        penalty -= 10%; // -40% → -30%
    if (roleXP[moduleRole] > 5000)
        penalty -= 10%; // -30% → -20%
    // Max cross-training: -20% penalty (never as good as primary role)
}
```

**Extreme Weapon Specialist Training**:
- Weapons with `RequiresSpecialist = true` (e.g., Quantum Disruptor, Graviton Lance)
- **Option 1**: Hire specialist crew (rare, expensive)
- **Option 2**: Train weapon specialists on extreme weapons (1000 ticks training time)
- Training requires: Weapon installed, training facility, dedicated instructor (another specialist)

---

## Power Budget System

### Power Management

Ships have **finite power generation**. Modules compete for power.

```csharp
public struct ShipPowerGrid : IComponentData
{
    public ushort PowerGeneration;               // Total available power
    public ushort PowerConsumed;                 // Currently used power
    public ushort PowerReserve;                  // Emergency backup (shield/damage control)
    public PowerPriority ActivePriority;         // Which systems get power first
}

public enum PowerPriority : byte
{
    Balanced,       // All systems share equally (default)
    Weapons,        // Weapons get priority (shields may drop)
    Shields,        // Shields get priority (weapon efficiency drops)
    Engines,        // Engines get priority (combat/escape focus)
    LifeSupport     // Life support + critical systems only (emergency)
}
```

**Power Consumption**:
- Each module has `PowerRequired` value
- If `PowerConsumed > PowerGeneration`:
  - **Brownout**: Lower priority modules operate at reduced efficiency
  - **Blackout**: Lowest priority modules shut down completely

**Priority System Example**:
```
Ship Power: 1000 units
Weapons: 600 units required
Shields: 400 units required
Engines: 200 units required
Total Required: 1200 units (OVER BUDGET by 200)

Priority = Weapons:
  Weapons: 600 units (100% efficiency) ✓
  Shields: 200 units (50% efficiency, brownout)
  Engines: 200 units (100% efficiency)
  Result: Shields weakened, weapons at full power

Priority = Shields:
  Weapons: 400 units (67% efficiency, brownout)
  Shields: 400 units (100% efficiency) ✓
  Engines: 200 units (100% efficiency)
  Result: Weapons weakened, shields at full power
```

**Tactical Choice**: Player must choose power priority based on situation.

**Power Shortage Penalties**:
```csharp
float CalculateModuleEfficiency(ShipModule module, ShipPowerGrid powerGrid)
{
    float basePower = module.PowerRequired;
    float allocatedPower = CalculateAllocatedPower(module, powerGrid);

    float powerRatio = allocatedPower / basePower;

    if (powerRatio >= 1.0f)
        return 1.0f; // Full power
    else if (powerRatio >= 0.5f)
        return powerRatio; // Brownout (proportional efficiency)
    else
        return 0f; // Blackout (shutdown)
}
```

---

## Carrier Docking (Nested Hierarchy)

### Docking Rules

**Carriers can dock other carriers** if they have the required hangar modules.

```csharp
public struct HangarBay : IComponentData
{
    public ModuleClass Class = ModuleClass.Hangar; // Utility Bay module
    public HangarSize Size;
    public ushort Capacity;                      // How many ships can dock
    public DynamicBuffer<DockedShip> DockedShips;
}

public enum HangarSize : byte
{
    CraftBay,       // Fighters, shuttles, drones (small crafts only)
    ShipBay,        // Corvettes, frigates (Regular Ships up to Frigate)
    CapitalBay      // Any capital ship smaller than parent carrier
}

public struct DockedShip : IBufferElementData
{
    public Entity ShipEntity;
    public DockState State;                      // Docked/Launching/Recovering
    public ushort DockingBaySlot;
}
```

**Docking Size Rules**:
- **Craft Bay**: Can dock Vessels/Crafts (fighters, shuttles, etc.)
- **Ship Bay**: Can dock Regular Ships up to Frigate class
- **Capital Bay**: Can dock ANY Capital Ship **smaller** than parent carrier

**Example Nested Hierarchy**:
```
Titan "Leviathan" (Super-Massive Capital Ship)
  ├─ Capital Bay 1: Docked Capital Ship "Merchant Freighter" (Medium Capital)
  │   ├─ Ship Bay 1: Docked Frigate "Scout Alpha"
  │   └─ Craft Bay 1: 5x Fighters
  ├─ Capital Bay 2: Docked Capital Ship "Fleet Carrier" (Heavy Capital)
  │   ├─ Craft Bay 1: 20x Fighters
  │   ├─ Craft Bay 2: 10x Bombers
  │   └─ Craft Bay 3: 5x Shuttles
  └─ Ship Bay 1: Docked Destroyer "Vanguard"
```

**Mothership Loss**:
- If mothership destroyed, all docked ships become **Orphaned**
- Orphaned ships must find new mothership OR operate independently (limited endurance)

---

## Role Switching

### Dynamic Role Assignment

Ships switch roles based on **faction/empire needs** and **equipped modules**.

```csharp
public struct ShipRole : IComponentData
{
    public RoleType ActiveRole;
    public RoleType AssignedRole;                // What faction wants this ship to do
    public ushort RoleSwitchCooldown;            // Ticks remaining before can switch
    public DynamicBuffer<RoleCapability> Capabilities; // What roles this ship CAN perform
}

public enum RoleType : byte
{
    Idle,
    Mining,
    Hauling,
    Combat,
    Exploration,
    Construction,
    Defense,        // Patrol/guard duty
    Support,        // Repair/resupply other ships
    Colonization    // Deliver/support colonies
}

public struct RoleCapability : IBufferElementData
{
    public RoleType Role;
    public byte Effectiveness;                   // 0-100 (how good is ship at this role)
}
```

**Role Switch Triggers**:
- **Faction AI**: Empire needs more miners → reassign combat ships to mining (if capable)
- **Player Order**: Player manually switches ship role
- **Emergency**: Ship under attack → auto-switch to Combat role
- **Resource Depletion**: Mining ship finishes deposit → auto-switch to Idle, await new orders

**Role Switch Timing**:
- **Interval**: Dynamic, handled by PureDOTS time systems
- **Cooldown**: 0 ticks (instant) to 100 ticks (depending on ship size and role change magnitude)
- Large role changes (Mining → Combat) = longer prep time
- Small role changes (Combat → Defense) = instant

**Effectiveness Calculation**:
```csharp
byte CalculateRoleEffectiveness(Entity ship, RoleType role)
{
    var modules = GetBuffer<ShipMount>(ship);
    byte score = 0;

    switch (role)
    {
        case RoleType.Mining:
            if (HasModule(modules, ModuleClass.Mining))
                score += 40;
            if (HasModule(modules, ModuleClass.Cargo))
                score += 30;
            if (HasModule(modules, ModuleClass.Sensor))
                score += 20;
            if (HasModule(modules, ModuleClass.Engine))
                score += 10;
            break;

        case RoleType.Combat:
            if (HasModule(modules, ModuleClass.BeamCannon) || HasModule(modules, ModuleClass.MassDriver))
                score += 50;
            if (HasModule(modules, ModuleClass.Shield))
                score += 30;
            if (HasModule(modules, ModuleClass.Sensor))
                score += 20;
            break;

        // ... other roles
    }

    return math.min(score, 100);
}
```

---

## Implementation Notes

- **Ship** entity = single ECS entity with `CapitalShip`, `RegularShip`, or `VesselCraft` component
- **ShipMount** buffer = dynamic buffer of mount points per ship
- **ShipModule** entity = child entity with parent reference to ship
- **CrewMember** entity = child entity with `AssignedShip` reference
- **HangarBay** component = on ships with hangar modules, tracks docked ships
- **ShipPowerGrid** component = singleton per ship, tracks power budget
- **ModuleExperience** component = on modules, tracks usage XP
- **InstalledMod** buffer = on modules, tracks research-unlocked upgrades
- **ShipRole** component = on ships, tracks current/assigned role and capabilities

---

## Open Questions / Design Decisions Needed

1. **Capital Ship Size Limits**: Should there be a maximum titan size, or allow arbitrarily large custom capitals?
   - *Suggestion*: Soft cap via power/crew requirements (bigger = harder to crew/power efficiently)

2. **Module Swapping in Field**: Can ships refit modules while deployed, or only at stations?
   - *Suggestion*: Only at stations with refit facilities (prevents mid-combat loadout changes)

3. **Prototype Limit**: How many prototype modules can exist simultaneously (prevent spam)?
   - *Suggestion*: 1 prototype per module class per faction (forces specialization)

4. **Crew Death**: Do crew die permanently in combat, or just injured (recoverable)?
   - *Suggestion*: Both - injured (recoverable) vs. killed (permanent, affects morale)

5. **Orphaned Craft Endurance**: How long can orphaned fighters operate before fuel runs out?
   - *Suggestion*: 500 ticks (~8 minutes) before must find mothership or perish

6. **Power Priority AI**: Should AI ships auto-manage power priority, or player-controlled only?
   - *Suggestion*: AI uses doctrine-based defaults (aggressive = weapons priority), player can override

7. **Nested Docking Depth**: Should there be a max nesting depth (Titan → Carrier → Frigate → Fighter = 4 levels)?
   - *Suggestion*: No hard limit, but practical limit via capacity/mass constraints

8. **Module Degradation**: Do modules degrade faster in combat vs. normal operations?
   - *Suggestion*: Yes - combat usage = 5x degradation rate, incentivizes repairs

---

## References

- **Mining Loop**: [MiningLoop.md](MiningLoop.md) - Mining role integration
- **Combat Loop**: [CombatLoop.md](CombatLoop.md) - Combat role, weapon usage
- **Haul Loop**: [HaulLoop.md](HaulLoop.md) - Hauling role, cargo modules
- **Exploration Loop**: [ExplorationLoop.md](ExplorationLoop.md) - Exploration role, sensor modules
- **Construction Loop**: [ConstructionLoop.md](ConstructionLoop.md) - Construction role, fabrication modules
- **Intel System**: [IntelVisibilitySystem.md](IntelVisibilitySystem.md) - Sensor integration
- **AI Behavior**: [AIBehaviorModules.md](AIBehaviorModules.md) - AI role switching decisions
- **Crew Aggregation**: Phase 3 TODO - Crew as individuals vs. ship-level aggregates
