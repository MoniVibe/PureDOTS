# Modular Hull System

**Status**: Concept Design
**Last Updated**: 2025-11-29
**Related Systems**: [MiscVesselsAndMegastructures.md](MiscVesselsAndMegastructures.md), DirectionalDamage, Combat

---

## Overview

The **Modular Hull System** provides flexible vessel customization where every ship has a hull class with mass/power capacity and module slots. Players can configure vessels for different roles by trading off capabilities: a boarding craft can mount heavy guns with minimal crew space, or maximize troop capacity at the expense of firepower. A carrier can dedicate all slots to hangars or balance strike craft with defensive armament.

**Core Principles**:
- ✅ **Mass Budget**: Every hull has tonnage capacity, modules have mass requirements
- ✅ **Power Grid**: Modules consume power, hull has power generation limit
- ✅ **Slot Typing**: Weapons, utility, special slots with size restrictions
- ✅ **Trade-offs**: Meaningful choices between competing capabilities
- ✅ **Deterministic**: All configurations serializable, rewindable
- ✅ **Alignment-Gated**: Some modules require specific alignment (slavery modules, honor systems)

**Design Goals**:
- Allow radical specialization (pure fighter carrier vs. battlecarrier)
- Encourage asymmetric faction designs through module availability
- Support emergent tactics from unusual configurations
- Maintain balance through mass/power constraints
- Enable player creativity within physics constraints

---

## Core Components

### Hull Foundation

```csharp
public struct VesselHull : IComponentData
{
    public HullClass Class;               // Corvette, Frigate, Cruiser, etc.
    public float MassCapacity;            // Tonnage (50-10000)
    public float CurrentMass;             // Sum of all modules
    public float PowerGeneration;         // MW (100-5000)
    public float PowerConsumption;        // Current draw
    public byte WeaponSlots;              // 0-12 slots
    public byte UtilitySlots;             // 0-8 slots
    public byte SpecialSlots;             // 0-4 slots
    public float BaseSpeed;               // Unloaded speed
    public float CurrentSpeed;            // Speed after mass penalty
}

public enum HullClass : byte
{
    StrikeCraft,      // 50-100 tons, 2-4 slots
    Corvette,         // 100-300 tons, 4-6 slots
    Frigate,          // 300-800 tons, 6-10 slots
    Destroyer,        // 800-1500 tons, 8-12 slots
    Cruiser,          // 1500-3000 tons, 10-16 slots
    Battlecruiser,    // 3000-6000 tons, 12-20 slots
    Battleship,       // 6000-10000 tons, 14-24 slots
    Carrier,          // 5000-15000 tons, 8-16 slots (mostly special)
    Hauler,           // 2000-20000 tons, 2-8 slots (mostly utility)
    Utility,          // 10-500 tons, 1-4 slots (shuttles, lifeboats, etc.)
}
```

### Module System

```csharp
public struct ModuleSlot : IBufferElementData
{
    public SlotType Type;                 // Weapon, Utility, Special
    public SlotSize Size;                 // Small, Medium, Large, ExtraLarge
    public Entity InstalledModule;        // Entity.Null if empty
    public byte HardpointIndex;           // Position on hull (for visuals/damage)
    public bool IsDamaged;                // Module offline from damage
}

public enum SlotType : byte
{
    Weapon,           // Guns, missiles, beams
    Utility,          // Shields, armor, sensors, engines
    Special,          // Hangars, cargo, compartments, unique systems
}

public enum SlotSize : byte
{
    Small,            // 5-20 tons, 10-50 MW
    Medium,           // 20-100 tons, 50-200 MW
    Large,            // 100-500 tons, 200-800 MW
    ExtraLarge,       // 500-2000 tons, 800-2000 MW
}

public struct Module : IComponentData
{
    public ModuleType Type;
    public SlotSize RequiredSize;
    public SlotType RequiredSlot;
    public float Mass;                    // Tonnage
    public float PowerDraw;               // MW
    public byte RequiredCrew;             // Crew to operate
    public ModuleEfficiency Efficiency;   // 0-1 (damaged modules lose efficiency)
    public AlignmentGate AlignmentReq;    // Optional alignment lock
}

public enum ModuleType : byte
{
    // Weapons
    Autocannon,
    Railgun,
    MissileLauncher,
    TorpedoTube,
    BeamWeapon,
    PointDefense,

    // Utility
    Shield,
    Armor,
    Sensor,
    Engine,
    PowerPlant,
    LifeSupport,
    RepairBay,

    // Special
    Hangar,
    CargoBay,
    TroopCompartment,
    MedicalBay,
    SlavePen,          // Evil factions only
    HonorShrine,       // Warrior cultures only
    ScienceLab,
    WarpDrive,
    StealthSystem,
}

public struct AlignmentGate
{
    public bool RequiresEvil;             // Slave pens, torture chambers
    public bool RequiresGood;             // Medical bays, rescue systems
    public bool RequiresLawful;           // Compliance scanners, law enforcement
    public bool RequiresChaotic;          // Smuggling holds, black market
    public sbyte MinMoralAxis;            // -100 to +100
    public sbyte MinOrderAxis;
    public sbyte MinPurityAxis;
}
```

### Module Configurations

```csharp
// Example: Heavy Autocannon (Large Weapon)
public static readonly Module HeavyAutocannon = new Module
{
    Type = ModuleType.Autocannon,
    RequiredSize = SlotSize.Large,
    RequiredSlot = SlotType.Weapon,
    Mass = 300f,
    PowerDraw = 400f,
    RequiredCrew = 6,
    Efficiency = 1f,
};

// Example: Fighter Hangar (ExtraLarge Special)
public static readonly Module FighterHangar = new Module
{
    Type = ModuleType.Hangar,
    RequiredSize = SlotSize.ExtraLarge,
    RequiredSlot = SlotType.Special,
    Mass = 1500f,
    PowerDraw = 600f,
    RequiredCrew = 20,
    Efficiency = 1f,
};

// Example: Troop Compartment (Medium Special)
public static readonly Module TroopCompartment = new Module
{
    Type = ModuleType.TroopCompartment,
    RequiredSize = SlotSize.Medium,
    RequiredSlot = SlotType.Special,
    Mass = 80f,
    PowerDraw = 100f,
    RequiredCrew = 2,
    Efficiency = 1f,
};

// Example: Slave Pen (Medium Special, Evil Only)
public static readonly Module SlavePen = new Module
{
    Type = ModuleType.SlavePen,
    RequiredSize = SlotSize.Medium,
    RequiredSlot = SlotType.Special,
    Mass = 60f,
    PowerDraw = 50f,
    RequiredCrew = 4,
    Efficiency = 1f,
    AlignmentReq = new AlignmentGate { MinMoralAxis = -50 },  // Evil
};

// Example: Shield Generator (Large Utility)
public static readonly Module ShieldGenerator = new Module
{
    Type = ModuleType.Shield,
    RequiredSize = SlotSize.Large,
    RequiredSlot = SlotType.Utility,
    Mass = 250f,
    PowerDraw = 500f,
    RequiredCrew = 3,
    Efficiency = 1f,
};
```

---

## Hull Class Templates

### Strike Craft (50-100 tons)

**Default Configuration**:
- Mass: 80 tons
- Power: 150 MW
- Slots: 2 Small Weapon, 2 Small Utility

**Typical Loadouts**:

1. **Interceptor**: 2x Light Autocannon, 1x Engine Boost, 1x Shield
2. **Bomber**: 1x Torpedo Tube, 1x Light Autocannon, 1x Armor, 1x Stealth
3. **Kamikaze** (Warrior Cultures): 1x Ram, 1x Heavy Explosive, 1x Honor Shrine, 1x Engine Boost

### Boarding Craft (100-200 tons, Utility Hull)

**Default Configuration**:
- Mass: 150 tons
- Power: 200 MW
- Slots: 2 Medium Weapon, 2 Medium Utility, 2 Medium Special

**Example Loadouts**:

1. **Heavy Assault**:
   - Weapons: 2x Medium Railgun (600 tons, 400 MW)
   - Utility: 1x Armor, 1x Engine
   - Special: 1x Small Troop Compartment (12 marines)
   - **Result**: Heavy firepower, minimal troops, slow

2. **Troop Transport**:
   - Weapons: None
   - Utility: 1x Shield, 1x Engine, 1x Stealth
   - Special: 2x Large Troop Compartment (60 marines)
   - **Result**: Maximum troop capacity, defenseless, stealthy

3. **Balanced**:
   - Weapons: 1x Light Autocannon
   - Utility: 1x Shield, 1x Armor
   - Special: 1x Medium Troop Compartment (30 marines), 1x Medical Bay
   - **Result**: Moderate combat, reasonable capacity

### Lifeboat (10-50 tons, Utility Hull)

**Default Configuration**:
- Mass: 25 tons
- Power: 50 MW
- Slots: 1 Small Utility, 1 Small Special

**Example Loadouts**:

1. **Armored Lifeboat**:
   - Utility: 1x Heavy Armor (15 tons)
   - Special: 1x Small Crew Compartment (6 occupants)
   - **Result**: Survivable, minimal capacity

2. **Evacuation Pod**:
   - Utility: 1x Engine Boost
   - Special: 1x Large Crew Compartment (20 occupants)
   - **Result**: Fast escape, fragile

3. **Long-Range**:
   - Utility: 1x Extended Fuel Tank
   - Special: 1x Medium Crew Compartment (12 occupants)
   - **Result**: Extended range, moderate capacity

### Hauler (2000-20000 tons, Utility Hull)

**Default Configuration** (5000 ton example):
- Mass: 5000 tons
- Power: 800 MW
- Slots: 2 Medium Weapon, 4 Medium Utility, 6 Large Special

**Example Loadouts**:

1. **Pure Cargo**:
   - Weapons: None
   - Utility: 2x Engine, 2x Fuel Tank
   - Special: 6x ExtraLarge Cargo Bay (4500 tons cargo)
   - **Result**: Maximum cargo, defenseless, slow

2. **Armed Merchant**:
   - Weapons: 2x Medium Autocannon (200 tons, 200 MW)
   - Utility: 2x Shield, 2x Armor
   - Special: 4x Large Cargo Bay (2000 tons cargo)
   - **Result**: Half cargo, moderate defense

3. **Q-Ship** (Disguised Warship):
   - Weapons: 2x Heavy Railgun (concealed)
   - Utility: 2x Shield, 1x Armor, 1x Sensor
   - Special: 2x Cargo Bay (decoy), 2x Missile Launcher (concealed)
   - **Result**: Minimal cargo, heavy firepower, looks like hauler

### Carrier (5000-15000 tons, Capital Hull)

**Default Configuration** (10000 ton example):
- Mass: 10000 tons
- Power: 3000 MW
- Slots: 4 Large Weapon, 6 Medium Utility, 8 ExtraLarge Special

**Example Loadouts**:

1. **Pure Carrier**:
   - Weapons: None
   - Utility: 4x Shield, 2x Armor
   - Special: 6x Fighter Hangar (240 fighters), 2x Repair Bay
   - **Result**: Maximum strike craft, defenseless vs. capitals

2. **Battlecarrier**:
   - Weapons: 4x Heavy Beam Weapon (1200 tons, 1600 MW)
   - Utility: 4x Shield, 2x Armor
   - Special: 2x Fighter Hangar (80 fighters), 2x Torpedo Bay, 2x Repair Bay, 2x Point Defense
   - **Result**: Half strike craft, heavy capital-ship armament

3. **Escort Carrier**:
   - Weapons: 2x Point Defense
   - Utility: 3x Shield, 3x Engine
   - Special: 4x Fighter Hangar (160 fighters), 4x Rapid Launch System
   - **Result**: Fast deployment, moderate capacity, point defense only

---

## Mass and Power System

### Mass Calculations

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class HullMassCalculationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref VesselHull hull, in DynamicBuffer<ModuleSlot> slots) =>
        {
            float totalMass = 0f;
            float totalPower = 0f;
            int totalCrew = 0;

            foreach (var slot in slots)
            {
                if (slot.InstalledModule == Entity.Null) continue;

                var module = GetComponent<Module>(slot.InstalledModule);
                totalMass += module.Mass;
                totalPower += module.PowerDraw;
                totalCrew += module.RequiredCrew;

                // Damaged modules consume power but provide no benefit
                if (slot.IsDamaged)
                {
                    module.Efficiency = 0f;
                }
            }

            hull.CurrentMass = totalMass;
            hull.PowerConsumption = totalPower;

            // Mass penalty to speed
            float massRatio = totalMass / hull.MassCapacity;
            hull.CurrentSpeed = hull.BaseSpeed * math.lerp(1f, 0.5f, massRatio);

            // Overload penalties
            if (totalMass > hull.MassCapacity)
            {
                // Critical: Reduce speed to 25%
                hull.CurrentSpeed *= 0.25f;
            }

            if (totalPower > hull.PowerGeneration)
            {
                // Power shortage: Systems start shutting down
                // Priority: Life Support > Engines > Shields > Weapons
                ApplyPowerShortage(ref hull, slots);
            }

        }).ScheduleParallel();
    }
}
```

### Power Priority System

```csharp
public struct ModulePriority : IComponentData
{
    public byte Priority;                 // 0 (critical) to 255 (lowest)
}

// Priority assignments:
// 0-50:   Life Support, Medical
// 51-100: Engines, Navigation
// 101-150: Shields, Point Defense
// 151-200: Weapons
// 201-255: Luxury systems, non-essential

private void ApplyPowerShortage(ref VesselHull hull, DynamicBuffer<ModuleSlot> slots)
{
    float deficit = hull.PowerConsumption - hull.PowerGeneration;
    if (deficit <= 0f) return;

    // Sort modules by priority, disable lowest priority first
    var modulePriorities = new NativeList<(Entity, byte, float)>(Allocator.Temp);

    foreach (var slot in slots)
    {
        if (slot.InstalledModule == Entity.Null) continue;
        var module = GetComponent<Module>(slot.InstalledModule);
        var priority = GetComponent<ModulePriority>(slot.InstalledModule);

        modulePriorities.Add((slot.InstalledModule, priority.Priority, module.PowerDraw));
    }

    // Sort by priority (lowest first)
    modulePriorities.Sort(new PriorityComparer());

    float savedPower = 0f;
    foreach (var (moduleEntity, priority, powerDraw) in modulePriorities)
    {
        if (savedPower >= deficit) break;

        // Disable this module
        var module = GetComponent<Module>(moduleEntity);
        module.Efficiency = 0f;
        savedPower += powerDraw;
    }

    modulePriorities.Dispose();
}
```

---

## Slot Compatibility System

### Slot Validation

```csharp
public static bool CanInstallModule(Module module, ModuleSlot slot, VesselHull hull)
{
    // Type match
    if (module.RequiredSlot != slot.Type) return false;

    // Size match
    if ((byte)module.RequiredSize > (byte)slot.Size) return false;

    // Mass budget
    float projectedMass = hull.CurrentMass + module.Mass;
    if (projectedMass > hull.MassCapacity * 1.5f) return false;  // Allow 50% overload

    // Power budget
    float projectedPower = hull.PowerConsumption + module.PowerDraw;
    if (projectedPower > hull.PowerGeneration * 1.2f) return false;  // Allow 20% overload

    // Alignment gate
    if (!CheckAlignmentRequirement(module.AlignmentReq, hull)) return false;

    return true;
}

private static bool CheckAlignmentRequirement(AlignmentGate gate, VesselHull hull)
{
    // Get faction alignment from hull owner
    var ownerEntity = GetComponent<OwnerFaction>(hull).Entity;
    var alignment = GetComponent<FactionAlignment>(ownerEntity);

    if (gate.RequiresEvil && alignment.MoralAxis > -30) return false;
    if (gate.RequiresGood && alignment.MoralAxis < 30) return false;
    if (gate.RequiresLawful && alignment.OrderAxis < 30) return false;
    if (gate.RequiresChaotic && alignment.OrderAxis > -30) return false;

    if (alignment.MoralAxis < gate.MinMoralAxis) return false;
    if (alignment.OrderAxis < gate.MinOrderAxis) return false;
    if (alignment.PurityAxis < gate.MinPurityAxis) return false;

    return true;
}
```

### Module Effects by Type

```csharp
// Weapon modules: Add to weapon systems
public struct WeaponModule : IComponentData
{
    public ModuleType WeaponType;
    public float Damage;
    public float RateOfFire;              // Rounds per second
    public float Range;
    public float Accuracy;                // 0-1
    public float ArmorPenetration;        // 0-1
    public byte HardpointIndex;           // Which slot
}

// Hangar modules: Define strike craft capacity
public struct HangarModule : IComponentData
{
    public int FighterCapacity;           // ExtraLarge = 40, Large = 20, etc.
    public float LaunchRate;              // Craft per second
    public float RepairRate;              // HP per second for docked craft
    public DynamicBuffer<Entity> DockedCraft;
}

// Cargo modules: Storage capacity
public struct CargoModule : IComponentData
{
    public float CargoCapacity;           // Tons
    public float CurrentCargo;
    public CargoType AllowedType;         // Generic, Refrigerated, Hazmat, etc.
}

// Troop compartment: Marines/boarders
public struct TroopCompartment : IComponentData
{
    public int TroopCapacity;
    public int CurrentTroops;
    public float MoraleBonus;             // 0-1 (quality quarters improve morale)
    public bool HasMedicalBay;
}

// Slave pen (Evil factions only)
public struct SlavePen : IComponentData
{
    public int SlaveCapacity;
    public int CurrentSlaves;
    public float ConditionRating;         // 0-1 (affects value/revolt chance)
    public float RevoltRisk;              // 0-1
}
```

---

## Trade-off Examples

### Boarding Craft Configurations

#### Configuration A: "Gunship"
```
Hull: Boarding Craft (150 tons, 200 MW)
Modules:
  - 2x Medium Railgun (200 tons, 200 MW) [Weapons]
  - 1x Heavy Armor (40 tons, 0 MW) [Utility]
  - 1x Engine Boost (20 tons, 50 MW) [Utility]
  - 1x Small Troop Compartment (15 tons, 20 MW, 8 marines) [Special]

Total: 275 tons (overloaded by 125), 270 MW (overloaded by 70)
Speed: 25% of base (overload penalty)
Combat Power: High
Boarding Capacity: Minimal
Role: Breaching heavy defenses, limited interior combat
```

#### Configuration B: "Troopship"
```
Hull: Boarding Craft (150 tons, 200 MW)
Modules:
  - 0x Weapons
  - 1x Shield (30 tons, 80 MW) [Utility]
  - 1x Stealth System (25 tons, 60 MW) [Utility]
  - 2x Large Troop Compartment (120 tons, 120 MW, 50 marines) [Special]

Total: 175 tons (overloaded by 25), 260 MW (overloaded by 60)
Speed: 60% of base
Combat Power: None
Boarding Capacity: Maximum
Role: Stealth insertion, overwhelming interior force
```

#### Configuration C: "Balanced"
```
Hull: Boarding Craft (150 tons, 200 MW)
Modules:
  - 1x Light Autocannon (40 tons, 50 MW) [Weapon]
  - 1x Shield (30 tons, 80 MW) [Utility]
  - 1x Armor (20 tons, 0 MW) [Utility]
  - 1x Medical Bay (25 tons, 40 MW) [Special]
  - 1x Medium Troop Compartment (50 tons, 60 MW, 25 marines) [Special]

Total: 165 tons (overloaded by 15), 230 MW (overloaded by 30)
Speed: 75% of base
Combat Power: Low
Boarding Capacity: Moderate
Role: Self-sufficient assault team with medical support
```

### Lifeboat Configurations

#### Configuration A: "Armored Escape Pod"
```
Hull: Lifeboat (25 tons, 50 MW)
Modules:
  - 1x Heavy Armor (15 tons, 0 MW) [Utility]
  - 1x Small Crew Compartment (10 tons, 10 MW, 6 occupants) [Special]

Total: 25 tons, 10 MW
Speed: 100% (no overload)
Survivability: High
Capacity: Minimal
Role: VIP escape, high-value personnel
```

#### Configuration B: "Mass Evacuation"
```
Hull: Lifeboat (25 tons, 50 MW)
Modules:
  - 1x Engine Boost (5 tons, 30 MW) [Utility]
  - 1x Large Crew Compartment (20 tons, 20 MW, 20 occupants) [Special]

Total: 25 tons, 50 MW
Speed: 150% (engine boost)
Survivability: Low
Capacity: Maximum
Role: Rapid crew escape from doomed ships
```

### Hauler Configurations

#### Configuration A: "Pure Freighter"
```
Hull: Hauler (5000 tons, 800 MW)
Modules:
  - 0x Weapons
  - 2x Engine (200 tons, 150 MW each) [Utility]
  - 2x Fuel Tank (100 tons, 0 MW each) [Utility]
  - 6x ExtraLarge Cargo Bay (750 tons, 50 MW each) [Special]

Total: 4900 tons, 600 MW
Speed: 100%
Combat Power: None
Cargo: 4500 tons
Role: Maximum profit, relies on escorts
```

#### Configuration B: "Armed Merchant"
```
Hull: Hauler (5000 tons, 800 MW)
Modules:
  - 2x Medium Autocannon (100 tons, 100 MW each) [Weapon]
  - 2x Shield (100 tons, 150 MW each) [Utility]
  - 2x Armor (80 tons, 0 MW each) [Utility]
  - 4x Large Cargo Bay (400 tons, 40 MW each) [Special]

Total: 2360 tons, 640 MW
Speed: 100%
Combat Power: Moderate
Cargo: 2000 tons
Role: Self-defense against pirates, half cargo capacity
```

#### Configuration C: "Q-Ship"
```
Hull: Hauler (5000 tons, 800 MW)
Modules:
  - 2x Heavy Railgun (300 tons, 200 MW each) [Weapon, concealed]
  - 2x Shield (100 tons, 150 MW each) [Utility]
  - 1x Armor (100 tons, 0 MW) [Utility]
  - 1x Advanced Sensor (50 tons, 100 MW) [Utility]
  - 2x Cargo Bay (200 tons, 30 MW each) [Special, decoy]
  - 2x Missile Launcher (150 tons, 100 MW each) [Special, concealed]

Total: 1550 tons, 1030 MW (overloaded by 230 MW)
Speed: 100%
Combat Power: High
Cargo: 500 tons (decoy only)
Role: Pirate hunter, looks like hauler until weapons deploy
Power Management: Weapons offline until combat, then life support reduced
```

### Carrier Configurations

#### Configuration A: "Fleet Carrier"
```
Hull: Carrier (10000 tons, 3000 MW)
Modules:
  - 0x Heavy Weapons
  - 4x Point Defense (50 tons, 80 MW each) [Weapon]
  - 4x Shield (250 tons, 500 MW each) [Utility]
  - 2x Armor (200 tons, 0 MW each) [Utility]
  - 6x Fighter Hangar (1500 tons, 600 MW each, 40 fighters) [Special]
  - 2x Repair Bay (300 tons, 200 MW each) [Special]

Total: 10400 tons (overloaded by 400), 4520 MW (overloaded by 1520)
Speed: 40% (mass overload)
Strike Craft: 240 fighters
Capital Firepower: Point defense only
Role: Pure carrier, relies on escorts and strike craft
Power Management: Repair bays offline except when recovering damaged craft
```

#### Configuration B: "Battlecarrier"
```
Hull: Carrier (10000 tons, 3000 MW)
Modules:
  - 4x Heavy Beam Weapon (300 tons, 400 MW each) [Weapon]
  - 4x Shield (250 tons, 500 MW each) [Utility]
  - 2x Armor (200 tons, 0 MW each) [Utility]
  - 2x Fighter Hangar (1500 tons, 600 MW each, 40 fighters) [Special]
  - 2x Torpedo Bay (400 tons, 300 MW each) [Special]
  - 2x Repair Bay (300 tons, 200 MW each) [Special]
  - 2x Point Defense (50 tons, 80 MW each) [Special]

Total: 6800 tons, 5560 MW (overloaded by 2560)
Speed: 75%
Strike Craft: 80 fighters
Capital Firepower: High
Role: Self-sufficient capital ship, reduced strike craft
Power Management: Must choose between beams or fighters at full power
```

---

## Module Damage and Repair

### Directional Damage Integration

```csharp
public struct ModuleDamageEvent : IComponentData
{
    public Entity TargetVessel;
    public byte HardpointIndex;           // Which slot was hit
    public float Damage;
    public DamageType Type;               // Kinetic, Energy, Explosive
    public uint Tick;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ModuleDamageSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref Module module, in ModuleDamageEvent dmgEvent) =>
        {
            // Damage reduces efficiency
            float damageRatio = dmgEvent.Damage / module.Mass;  // Rough approximation
            module.Efficiency = math.max(0f, module.Efficiency - damageRatio * 0.5f);

            // Critical damage (efficiency < 0.2) = offline
            if (module.Efficiency < 0.2f)
            {
                module.Efficiency = 0f;
                MarkSlotDamaged(dmgEvent.TargetVessel, dmgEvent.HardpointIndex);
            }

            // Catastrophic damage (direct hit on reactor, magazine, etc.)
            if (module.Type == ModuleType.PowerPlant && module.Efficiency == 0f)
            {
                TriggerPowerFailure(dmgEvent.TargetVessel);
            }

            if (module.Type == ModuleType.MissileLauncher && dmgEvent.Type == DamageType.Explosive)
            {
                TriggerMagazineExplosion(dmgEvent.TargetVessel);
            }

        }).Run();
    }
}
```

### Repair System

```csharp
public struct RepairBayModule : IComponentData
{
    public float RepairRate;              // HP per second
    public int RepairCrewSize;
    public bool CanRepairModules;
    public bool CanRepairHull;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ModuleRepairSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities.ForEach((ref VesselHull hull, in DynamicBuffer<ModuleSlot> slots) =>
        {
            // Find active repair bays
            float totalRepairRate = 0f;
            foreach (var slot in slots)
            {
                if (slot.InstalledModule == Entity.Null) continue;

                var module = GetComponent<Module>(slot.InstalledModule);
                if (module.Type == ModuleType.RepairBay && module.Efficiency > 0f)
                {
                    var repairBay = GetComponent<RepairBayModule>(slot.InstalledModule);
                    totalRepairRate += repairBay.RepairRate * module.Efficiency;
                }
            }

            if (totalRepairRate == 0f) return;

            // Repair damaged modules (priority: critical systems first)
            float repairBudget = totalRepairRate * deltaTime;

            // Priority order: PowerPlant > LifeSupport > Engines > Weapons
            var damagedModules = new NativeList<(Entity, byte, float)>(Allocator.Temp);

            for (byte i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.InstalledModule == Entity.Null) continue;

                var module = GetComponent<Module>(slot.InstalledModule);
                if (module.Efficiency < 1f)
                {
                    byte priority = GetRepairPriority(module.Type);
                    damagedModules.Add((slot.InstalledModule, priority, module.Efficiency));
                }
            }

            damagedModules.Sort(new RepairPriorityComparer());

            foreach (var (moduleEntity, priority, efficiency) in damagedModules)
            {
                if (repairBudget <= 0f) break;

                var module = GetComponent<Module>(moduleEntity);
                float repairAmount = math.min(repairBudget / module.Mass, 1f - module.Efficiency);

                module.Efficiency += repairAmount;
                repairBudget -= repairAmount * module.Mass;

                // Repair complete: Re-enable slot
                if (module.Efficiency >= 0.2f)
                {
                    ClearSlotDamaged(entity, GetSlotIndexForModule(slots, moduleEntity));
                }
            }

            damagedModules.Dispose();

        }).Run();
    }
}
```

---

## Balance Considerations

### Mass vs. Power Trade-offs

**Key Principle**: Mass limits speed/maneuverability, power limits active systems.

- **Heavy weapons**: High mass, moderate power → Slow ship, but can sustain fire
- **Energy weapons**: Low mass, very high power → Fast ship, limited uptime
- **Shields**: Moderate mass, very high power → Speed penalty, power-hungry
- **Armor**: High mass, zero power → Slow ship, always-on protection

### Slot Economics

**Small Slots** (5-20 tons):
- Cheap to fill, numerous options
- Point defense, light weapons, basic systems
- Total capacity: 40-160 tons for 8 small slots

**Medium Slots** (20-100 tons):
- Workhorse slot, most versatile
- Main weapons, shields, cargo
- Total capacity: 200-1000 tons for 10 medium slots

**Large Slots** (100-500 tons):
- Heavy commitment, powerful systems
- Capital weapons, major hangars
- Total capacity: 500-2500 tons for 5 large slots

**ExtraLarge Slots** (500-2000 tons):
- Specialized systems only
- Carrier hangars, massive cargo bays
- Total capacity: 2000-8000 tons for 4 XL slots

### Faction Asymmetry Through Modules

**Evil Factions**:
- Access to slave pens (high capacity, low morale, revolt risk)
- Torture chambers (fear debuffs to enemies)
- Dark matter reactors (high power, unstable)

**Good Factions**:
- Medical bays (crew recovery, morale boost)
- Rescue systems (capture lifeboats, recover crews)
- Efficient life support (lower power draw)

**Lawful Factions**:
- Compliance scanners (detect contraband)
- Standardized modules (easier repair, interchangeable)
- Regulation limits (cannot overload mass/power)

**Chaotic Factions**:
- Jury-rigged systems (low mass, unstable)
- Smuggling compartments (hidden cargo)
- Overclocked reactors (150% power, explosion risk)

**Warrior Cultures**:
- Honor shrines (morale boost, last stand chance)
- Ramming prows (melee weapons)
- Berserker modules (damage boost, defense penalty)

---

## Customization Flow

### Player Experience

1. **Select Hull**: Choose base hull class (corvette, carrier, etc.)
2. **View Slots**: See available slot types and sizes
3. **Install Modules**: Drag-and-drop modules into slots
4. **Monitor Budgets**: Real-time mass/power indicators
5. **Validate Configuration**: Red warnings for overloads, alignment locks
6. **Save Template**: Store configuration for future builds
7. **Deploy**: Instantiate vessel with module loadout

### AI Loadout Generation

```csharp
public static VesselConfiguration GenerateAILoadout(HullClass hullClass, FactionAlignment alignment, VesselRole role)
{
    var config = new VesselConfiguration();
    config.Hull = GetHullTemplate(hullClass);

    switch (role)
    {
        case VesselRole.Interceptor:
            // Fast, lightly armed
            config.AddModules(ModuleType.LightWeapon, 2);
            config.AddModules(ModuleType.Engine, 2);
            config.AddModules(ModuleType.Shield, 1);
            break;

        case VesselRole.Bomber:
            // Torpedoes, armor
            config.AddModules(ModuleType.TorpedoTube, 2);
            config.AddModules(ModuleType.Armor, 2);
            config.AddModules(ModuleType.Engine, 1);
            break;

        case VesselRole.Carrier:
            // Maximize hangars
            int hangarSlots = config.Hull.SpecialSlots - 2;  // Reserve 2 for repair/defense
            config.AddModules(ModuleType.Hangar, hangarSlots);
            config.AddModules(ModuleType.RepairBay, 1);
            config.AddModules(ModuleType.PointDefense, 4);
            config.AddModules(ModuleType.Shield, 4);
            break;

        // ... more roles
    }

    // Alignment-specific additions
    if (alignment.MoralAxis < -50)  // Evil
    {
        config.TryAddModule(ModuleType.SlavePen);
    }

    if (alignment.OrderAxis > 70)  // Lawful
    {
        config.ValidateCompliance();  // No overloads
    }

    return config;
}
```

---

## Integration with Existing Systems

### Warrior Pilot Last Stand

When a strike craft with **HonorShrine** module reaches critical damage:
- Increased kamikaze probability (+20%)
- Crash location influenced by shrine's "honor target" logic
- If recovered, shrine provides morale boost to recovery team

### Misc Vessels

**Boarding Craft**: Use modular system to define assault vs. transport variants
**Shuttles**: Utility hull with passenger/cargo module balance
**Lifeboats**: Minimal slots, trade armor vs. capacity
**Probes**: Tiny utility hull, sensor module only

### Divine Guidance

Manual aim can target **specific modules**:
- "Aim for the hangar" → Disable carrier strike capability
- "Aim for the engines" → Cripple mobility
- "Aim for the reactor" → Catastrophic explosion

### Rewind Integration

Module configurations are **fully deterministic** and **rewindable**:
- All module states stored in history buffer
- Damage/repair events logged with tick stamps
- Player can rewind, reconfigure loadout, and replay battle

---

## Cross-Project Notes

### Space4X

- **Tech Progression**: Advanced modules locked behind research
  - Tier 1: Autocannons, basic shields
  - Tier 2: Railguns, improved engines
  - Tier 3: Beam weapons, advanced sensors
  - Tier 4: Exotic modules (dark matter, phase shields)

- **Faction Flavor**: Each faction has unique module variants
  - Empire: Standardized, efficient
  - Rebels: Jury-rigged, overclocked
  - Megacorp: Expensive, high-performance
  - Pirates: Salvaged, mismatched

### Godgame (Potential)

While Godgame focuses on villagers rather than vessels, the modular system could apply to:

- **Wagons**: Trade cargo capacity vs. armor/weapons
- **Buildings**: Modular room/wing construction
- **Siege Engines**: Ballista modules (precision vs. rate of fire)

The **alignment-gating** pattern is identical: Evil factions build torture chambers, Good factions build hospitals.

---

## Implementation Checklist

- [ ] Define `VesselHull` component with mass/power/slot data
- [ ] Define `ModuleSlot` buffer with type/size/installed module
- [ ] Define `Module` component with stats and requirements
- [ ] Create module catalog (50+ module types)
- [ ] Implement `HullMassCalculationSystem` for mass/power budgets
- [ ] Implement `PowerPrioritySystem` for brownout management
- [ ] Implement `ModuleDamageSystem` with directional damage integration
- [ ] Implement `ModuleRepairSystem` with priority queue
- [ ] Create UI for module installation (drag-and-drop)
- [ ] Create AI loadout generator for NPCs
- [ ] Add alignment gates to restricted modules
- [ ] Integrate with existing vessel types (boarding craft, lifeboats, etc.)
- [ ] Add module targeting to Divine Guidance system
- [ ] Implement module state serialization for rewind
- [ ] Create module templates for faction asymmetry
- [ ] Balance mass/power costs across all modules
- [ ] Test extreme configurations (pure carrier, Q-ship, overloaded)

---

## Example Scenarios

### Scenario 1: Pirate Ambush

**Setup**:
- Pirate "Q-Ship" disguised as hauler
- Trade convoy with armed merchants

**Q-Ship Configuration**:
- 2x Hidden Heavy Railgun (1200 tons, 800 MW)
- 2x Cargo Bay (decoy, 400 tons)
- Life support at minimum until weapons deploy

**Execution**:
1. Q-Ship approaches convoy (weapons powered down)
2. Convoy scans: "Just another hauler"
3. Q-Ship closes range
4. Power spike detected (weapons charging)
5. Railguns deploy from hidden hardpoints
6. First volley cripples lead escort
7. Convoy scatters, Q-ship pursues

**Outcome**: Module system enables deception tactics through power management

---

### Scenario 2: Carrier Doctrine Clash

**Fleet A: Pure Carriers**
- 3x Fleet Carrier (240 fighters each = 720 total)
- No capital weapons
- Heavy point defense

**Fleet B: Battlecarriers**
- 4x Battlecarrier (80 fighters each = 320 total)
- Heavy beam weapons
- Moderate point defense

**Battle**:
1. Fleet A launches massive fighter wave
2. Fleet B intercepts with point defense (50% attrition)
3. Remaining fighters engage Fleet B (360 fighters vs. 4 capitals)
4. Fleet B launches own fighters (320) to screen
5. Fighter furball ensues while capitals duel
6. Fleet B's beam weapons shred Fleet A carriers (defenseless)
7. Fleet A's fighters eventually overwhelm Fleet B, but carriers lost

**Outcome**: Pure carrier strategy high-risk/high-reward, battlecarrier more balanced

---

### Scenario 3: Desperate Evacuation

**Setup**:
- Capital ship under attack, hull at 15%
- 200 crew, 6 lifeboats (capacity varies by configuration)

**Lifeboat Configurations**:
- 2x Armored Lifeboats (6 occupants each = 12 total)
- 3x Standard Lifeboats (12 occupants each = 36 total)
- 1x Mass Evacuation Lifeboat (20 occupants = 20 total)

**Total Capacity**: 68 occupants (132 crew stranded)

**Decision**:
- VIPs and critical crew board armored lifeboats
- Injured board mass evac lifeboat (has medical module)
- Remaining crew draws lots for standard lifeboats
- 132 crew go down with ship

**Outcome**: Module choices (armor vs. capacity) have life-or-death consequences

---

## Summary

The **Modular Hull System** provides:

1. **Flexibility**: Radical specialization or balanced loadouts
2. **Trade-offs**: Mass vs. power, capacity vs. survivability
3. **Emergent Tactics**: Q-ships, pure carriers, ram builds
4. **Faction Asymmetry**: Alignment-gated modules create unique playstyles
5. **Meaningful Choices**: Every slot matters, every module has consequences
6. **Deterministic Balance**: Physics-based constraints prevent exploits

**Next Steps**:
- Prototype hull/module component definitions
- Create module catalog with 50+ types
- Implement mass/power calculation system
- Design UI mockups for module installation
- Balance testing with extreme configurations

---

**Related Documents**:
- [MiscVesselsAndMegastructures.md](MiscVesselsAndMegastructures.md) - Vessel types using this system
- [WarriorPilotLastStand.md](WarriorPilotLastStand.md) - Honor shrine module integration
- [DivineGuidanceManualAim.md](../../Packages/com.moni.puredots/Documentation/DesignNotes/DivineGuidanceManualAim.md) - Module-specific targeting

**Design Lead**: [TBD]
**Technical Lead**: [TBD]
**Last Review**: 2025-11-29
