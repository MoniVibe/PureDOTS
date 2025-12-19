# Mobile Cities and Land Carriers

## Overview

**Mobile Cities** and **Land Carriers** represent the evolution of settlements from static structures to mobile fortresses capable of traversing vast distances. At certain tech levels, civilizations can construct entire cities on mobile platforms (treads, walker limbs, or anti-gravity), creating nomadic powerhouses that combine the production capacity of a city with the mobility of a vehicle.

At advanced tech levels, these mobile cities can function as **Land Carriers** - deploying smaller units (mechs, vehicles, soldiers) from combat bays, mirroring the carrier mechanics from Space4X but operating on terrain.

**Key Principles**:
- **Tech-Gated Progression**: Stationary → Mobile Bases → Mobile Cities → Land Carriers
- **Locomotion Variety**: Treads, walker limbs, hover platforms, anti-gravity
- **Aggregate Movement**: Entire city moves as single entity
- **Cross-Game Parallel**: Godgame land carriers = Space4X ship carriers (on terrain)
- **Strategic Depth**: Nomadic civilizations, resource harvesting while mobile, siege platforms
- **Bay Integration**: Advanced carriers deploy units from combat bays
- **Deterministic**: Same as all other systems (rewind-compatible)

---

## Core Concepts

### Tech Progression

Mobile cities unlock through research/advancement:

```csharp
public enum MobileCityTechLevel : byte
{
    Stationary = 0,         // Traditional fixed settlements
    MobileStructures = 1,   // Individual buildings can be moved (packed/unpacked)
    MobilePlatforms = 2,    // Small groups of buildings on platforms
    MobileBases = 3,        // Small fortified mobile bases (10-20 buildings)
    MobileCities = 4,       // Full cities on mobile platforms (50+ buildings)
    LandCarriers = 5,       // Cities with combat bays for unit deployment
    MegaCarriers = 6        // Massive mobile fortresses (100+ buildings, multiple bays)
}
```

**Progression Example (Godgame)**:
```
Age 1 (Stone Age): Stationary villages
  ├─ No mobility, must abandon structures to relocate

Age 2 (Bronze Age): Mobile Structures (Tech unlock)
  ├─ Can pack buildings onto carts, slow relocation
  ├─ Buildings must be unpacked to function

Age 3 (Iron Age): Mobile Platforms (Tech unlock)
  ├─ Small platforms (5-10 buildings) that move together
  ├─ Buildings function while moving (reduced efficiency)

Age 4 (Industrial Age): Mobile Bases (Tech unlock)
  ├─ Tracked or wheeled platforms (10-20 buildings)
  ├─ Moderate speed, medium-sized settlements
  ├─ Basic defenses while mobile

Age 5 (Advanced Age): Mobile Cities (Tech unlock)
  ├─ Walker limbs or anti-grav (50+ buildings)
  ├─ Full city functionality while moving
  ├─ Defensive turrets, walls, production facilities

Age 6 (Future Age): Land Carriers (Tech unlock)
  ├─ Deployment bays for mechs, tanks, aircraft
  ├─ Coordinated unit deployment from moving city
  ├─ Massive strategic assets
```

---

## Mobile City Components

### Mobile Platform Base

The physical structure that carries the city:

```csharp
public struct MobileCityPlatform : IComponentData
{
    public Entity PlatformEntity;
    public MobilePlatformType Type;          // Treads, Walker, Hover, AntiGrav
    public float3 PlatformSize;              // Dimensions (width, height, length)
    public float MaxLoadCapacity;            // Tons
    public float CurrentLoad;                // Current weight
    public LocomotionMode LocomotionMode;    // How it moves
    public float MaxSpeed;                   // m/s (usually very slow)
    public float TurnRate;                   // Degrees/s (very slow turns)
    public uint MaxBuildingSlots;            // How many buildings can fit
    public uint OccupiedSlots;               // Current building count
    public float PowerConsumption;           // Power needed to move
    public float FuelConsumption;            // Fuel per second while moving
}

public enum MobilePlatformType : byte
{
    // Early Tech
    WheelCart = 0,          // Primitive wheeled platform (very slow)
    AnimalDrawn = 1,        // Beasts of burden pull platform

    // Industrial Tech
    Treads = 10,            // Tracked platform (moderate speed, any terrain)
    Wheels = 11,            // Wheeled platform (fast on roads, poor offroad)
    Rails = 12,             // Rail-mounted (fast but track-constrained)

    // Advanced Tech
    WalkerBipedal = 20,     // Two-legged walker (versatile, unstable)
    WalkerQuadrupedal = 21, // Four-legged walker (stable, moderate speed)
    WalkerHexapedal = 22,   // Six-legged walker (very stable, any terrain)
    WalkerOctopedal = 23,   // Eight-legged walker (ultra-stable, slow)

    // Future Tech
    Hover = 30,             // Hover platform (ignores terrain, water-capable)
    AntiGrav = 31,          // Anti-gravity (flies low over terrain)
    Maglev = 32,            // Magnetic levitation (requires special terrain)

    // Hybrid
    TrackedWalker = 40,     // Combination treads + walker limbs
    AmphibiousHover = 41    // Can traverse land and water
}
```

### Mobile Building Integration

Buildings on mobile platforms:

```csharp
public struct MobileBuilding : IComponentData
{
    public Entity BuildingEntity;
    public Entity ParentPlatform;            // Which mobile platform carries this
    public float3 LocalOffset;               // Position on platform
    public MobileBuildingState State;
    public float EfficiencyWhileMoving;      // Production penalty while mobile (0.0 to 1.0)
    public float PowerDraw;                  // Power consumed by building
    public bool RequiresDeployment;          // Must stop to function (defensive turrets)
    public float PackTime;                   // Time to pack for relocation (if applicable)
    public float UnpackTime;                 // Time to unpack and become operational
}

public enum MobileBuildingState : byte
{
    Packed = 0,             // Inactive, packed for transport
    Unpacking = 1,          // Deploying, becoming operational
    Active = 2,             // Fully functional
    ActiveMoving = 3,       // Functional but platform is moving (reduced efficiency)
    ActiveDeployed = 4,     // Deployed for maximum efficiency (stabilizers, anchors)
    Packing = 5,            // Deactivating, preparing for movement
    Damaged = 6             // Needs repair
}

// Example: Smithy on mobile platform
MobileBuilding smithy = new()
{
    BuildingEntity = smithyEntity,
    ParentPlatform = mobileCityEntity,
    State = MobileBuildingState.ActiveMoving,
    EfficiencyWhileMoving = 0.5f,   // 50% production while moving
    RequiresDeployment = false,     // Can work while moving
    PowerDraw = 10.0f               // kW
};

// Example: Defensive turret
MobileBuilding turret = new()
{
    BuildingEntity = turretEntity,
    ParentPlatform = mobileCityEntity,
    State = MobileBuildingState.Packed,
    EfficiencyWhileMoving = 0.0f,   // Cannot function while moving
    RequiresDeployment = true,      // Must deploy to fire
    UnpackTime = 10.0f              // 10 seconds to deploy
};
```

---

## Land Carrier Mechanics

At advanced tech levels, mobile cities become **Land Carriers** with combat deployment bays:

```csharp
public struct LandCarrier : IComponentData
{
    public Entity CarrierEntity;
    public LandCarrierClass Class;           // Scout, Assault, Siege, Fortress
    public float3 Dimensions;                // Size (width, height, length)
    public uint BayCount;                    // How many deployment bays
    public uint MaxDeployedUnits;            // Total units that can be deployed
    public float DeploymentRange;            // How far from carrier can units operate
    public bool CanMoveWhileDeployed;        // Can carrier move with units out?
    public float MaxSpeed;                   // m/s (very slow)
    public float TurnRate;                   // Degrees/s (very slow)
}

public enum LandCarrierClass : byte
{
    Scout = 0,      // Fast, light, 1-2 bays, deploys reconnaissance units
    Assault = 1,    // Medium, 3-5 bays, deploys combat units
    Siege = 2,      // Heavy, 4-6 bays, deploys artillery and heavy mechs
    Fortress = 3,   // Massive, 6-10 bays, mobile city-fortress
    Mega = 4        // Enormous, 10+ bays, capital-class mobile base
}

// Integration with Bay and Platform Combat System
// (Uses same CombatPosition components from Bay_And_Platform_Combat.md)

public struct LandCarrierBay : IBufferElementData
{
    public FixedString32Bytes BayId;         // "Bay_1", "Bay_2", etc.
    public float3 LocalOffset;               // Position on carrier
    public FiringArc Arc;                    // Valid firing angles
    public BayState State;                   // Closed, Opening, Open, etc.
    public float TransitionProgress;         // 0.0 to 1.0
    public byte MaxOccupants;                // How many units per bay
    public byte CurrentOccupants;            // Current deployed units
    public float Health;                     // Bay structural integrity
    public LandCarrierBayType BayType;       // What can deploy from here
}

public enum LandCarrierBayType : byte
{
    MechBay = 0,        // Deploys bipedal mechs
    VehicleBay = 1,     // Deploys tanks, APCs, trucks
    AircraftBay = 2,    // Deploys helicopters, VTOLs
    InfantryBay = 3,    // Deploys ground troops
    ArtilleryBay = 4,   // Deploys mobile artillery
    UniversalBay = 5    // Can deploy any unit type
}
```

**Land Carrier Bay Workflow**:
```
1. Carrier approaches combat zone
2. Carrier stops (or slows to minimum speed)
3. Bay doors open (5-15 seconds)
4. Units deploy from bays (walk/drive out)
5. Units engage enemies within deployment range
6. Units return to bays when recalled
7. Bay doors close
8. Carrier resumes movement
```

---

## Locomotion Integration

Mobile cities use the Locomotion System:

```csharp
// Example: Treaded Mobile City
LocomotionCapabilities[] mobileCityTreadedLoco = {
    new() {
        Mode = LocomotionMode.Tracked,
        Directionality = MovementDirectionality.MonoDirectional,
        MaxSpeed = 2.0f,        // Very slow (2 m/s = ~7 km/h)
        Acceleration = 0.2f,    // Takes time to get moving
        Deceleration = 0.5f,
        TurnRate = 5f,          // Very wide turns (5°/s)
        EnergyCost = 500.0f,    // Massive power consumption
        FuelConsumption = 10.0f,// Liters per second
        RequiresSurface = true,
        ValidTerrain = TerrainMask.Ground | TerrainMask.Ice | TerrainMask.Mud,
        MaxSlope = 15f          // Cannot climb steep hills
    }
};

// Example: Walker Mobile City (Hexapedal)
LocomotionCapabilities[] mobileCityWalkerLoco = {
    new() {
        Mode = LocomotionMode.Legged,
        Directionality = MovementDirectionality.PlanarOmni, // Can sidestep
        MaxSpeed = 3.0f,        // Slightly faster than treads
        Acceleration = 0.5f,
        TurnRate = 15f,         // Better turning than treads
        EnergyCost = 800.0f,    // More power than treads (more complex)
        ValidTerrain = TerrainMask.Ground | TerrainMask.Ice | TerrainMask.Mud | TerrainMask.Wall,
        MaxSlope = 45f,         // Can climb steep terrain
        RequiresSurface = true
    }
};

// Example: Hover Mobile City
LocomotionCapabilities[] mobileCityHoverLoco = {
    new() {
        Mode = LocomotionMode.Hovering,
        Directionality = MovementDirectionality.PlanarOmni,
        MaxSpeed = 5.0f,        // Faster than ground-based
        Acceleration = 1.0f,
        TurnRate = 30f,         // Good maneuverability
        EnergyCost = 1500.0f,   // Extreme power consumption
        ValidTerrain = TerrainMask.Ground | TerrainMask.Water | TerrainMask.Ice,
        MinAltitude = 2.0f,     // Hovers 2m above surface
        MaxAltitude = 10.0f     // Cannot fly high
    }
};

// Example: Anti-Grav Mobile City
LocomotionCapabilities[] mobileCityAntiGravLoco = {
    new() {
        Mode = LocomotionMode.Antigrav,
        Directionality = MovementDirectionality.VolumetricLimited,
        MaxSpeed = 10.0f,       // Fastest mobile city type
        Acceleration = 2.0f,
        TurnRate = 45f,
        EnergyCost = 3000.0f,   // Astronomical power requirement
        ValidTerrain = TerrainMask.Any, // Ignores terrain
        MinAltitude = 5.0f,
        MaxAltitude = 100.0f    // Can fly moderately high
    }
};
```

---

## Mobile City Management

### Resource Gathering While Mobile

Mobile cities can gather resources as they move:

```csharp
public struct MobileResourceHarvester : IComponentData
{
    public Entity HarvesterEntity;
    public Entity ParentCarrier;
    public ResourceType HarvestType;         // Food, Metal, Wood, etc.
    public float HarvestRate;                // Resources/s while moving
    public float HarvestRadiusWhileMoving;   // Meters from carrier
    public float StorageCapacity;            // Max resources stored
    public float CurrentStorage;             // Current amount
    public bool RequiresDeployment;          // Must stop to harvest optimally
}

// Example: Mobile city with mining drills
// While moving slowly: Harvests 5 metal/s within 10m radius
// While stationary + deployed: Harvests 20 metal/s within 50m radius
```

### Power Management

Mobile cities have massive power requirements:

```csharp
public struct MobileCityPower : IComponentData
{
    public float TotalPowerGeneration;       // MW generated by onboard reactors
    public float PropulsionPowerDraw;        // Power needed for movement
    public float BuildingPowerDraw;          // Power needed for buildings
    public float DefensePowerDraw;           // Power for shields, weapons
    public float CurrentPowerUsage;          // Total draw
    public float PowerEfficiency;            // 0.0 to 1.0 (degradation, damage)

    public PowerPriority Priority;           // What gets power first
}

public enum PowerPriority : byte
{
    PropulsionFirst = 0,    // Movement prioritized (flee mode)
    DefenseFirst = 1,       // Shields/weapons prioritized (combat mode)
    ProductionFirst = 2,    // Buildings prioritized (economic mode)
    Balanced = 3            // Even distribution
}

// Power starvation scenarios:
// - If PropulsionPowerDraw > Available: City slows down or stops
// - If BuildingPowerDraw > Available: Buildings shut down (efficiency 0%)
// - If DefensePowerDraw > Available: Shields fail, weapons offline
```

### Population and Logistics

Citizens living on mobile cities:

```csharp
public struct MobileCityPopulation : IComponentData
{
    public uint TotalPopulation;             // Citizens on mobile city
    public float Morale;                     // 0.0 to 1.0
    public float FoodConsumption;            // Food/day
    public float FoodStorage;                // Days of food stored
    public float WaterConsumption;           // Water/day
    public float WaterStorage;               // Days of water stored
    public MoraleModifiers MoraleFactors;
}

public struct MoraleModifiers
{
    public float ConstantMovement;           // -0.2 (people dislike constant motion)
    public float CombatNearby;               // -0.3 (fear from combat)
    public float ResourceAbundance;          // +0.3 (well-fed, well-supplied)
    public float Victory;                    // +0.5 (recent successful battles)
    public float Safety;                     // +0.2 (strong defenses)
}

// Low morale effects:
// - Slower production (efficiency penalty)
// - Risk of desertion (population leaves city)
// - Potential rebellion (city becomes hostile)
```

---

## Land Carrier Combat Deployment

### Unit Deployment Flow

```csharp
[BurstCompile]
public partial struct LandCarrierDeploymentSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (carrier, bays, transform) in SystemAPI.Query<
            RefRO<LandCarrier>,
            DynamicBuffer<LandCarrierBay>,
            RefRO<LocalTransform>>())
        {
            for (int i = 0; i < bays.Length; i++)
            {
                var bay = bays[i];

                // Check if bay is ready to deploy
                if (bay.State == BayState.Open && bay.CurrentOccupants < bay.MaxOccupants)
                {
                    // Deploy unit from bay
                    var deployPosition = CalculateDeployPosition(transform.ValueRO, bay);
                    var unitEntity = SpawnUnitFromBay(state, bay.BayType, deployPosition);

                    // Track deployed unit
                    AddDeployedUnitTracking(state, carrier.ValueRO.CarrierEntity, unitEntity, bay.BayId);

                    bay.CurrentOccupants++;
                    bays[i] = bay;
                }

                // Check for units returning to bay
                if (bay.State == BayState.Open)
                {
                    RecallUnitsInRange(state, carrier.ValueRO, bay, transform.ValueRO);
                }
            }
        }
    }

    private float3 CalculateDeployPosition(LocalTransform carrierTransform, LandCarrierBay bay)
    {
        // Calculate world position from carrier position + bay local offset
        var worldOffset = math.mul(carrierTransform.Rotation, bay.LocalOffset);
        return carrierTransform.Position + worldOffset;
    }
}

public struct DeployedUnit : IComponentData
{
    public Entity UnitEntity;
    public Entity ParentCarrier;             // Which carrier deployed this unit
    public FixedString32Bytes SourceBayId;   // Which bay it came from
    public float DeploymentTimestamp;        // When was it deployed
    public UnitDeploymentState State;
    public float3 RallyPoint;                // Where unit should go after deployment
    public float RecallDistance;             // Distance from carrier to auto-recall
}

public enum UnitDeploymentState : byte
{
    Deploying = 0,      // Exiting bay
    Deployed = 1,       // Active in field
    Returning = 2,      // Moving back to carrier
    Docking = 3,        // Entering bay
    Stored = 4          // Inside carrier
}
```

**Deployment Example (Land Carrier in Combat)**:
```
1. Land Carrier "Fortress Prime" approaches enemy settlement
2. Carrier stops 500m from enemy walls
3. Commander orders deployment:
   - Bay 1 (Mech Bay): Deploy 3 Assault Mechs
   - Bay 2 (Vehicle Bay): Deploy 2 Siege Tanks
   - Bay 3 (Infantry Bay): Deploy 50 soldiers
4. Bay doors open (10 seconds each)
5. Units deploy and advance toward enemy
6. Carrier remains stationary, provides fire support from mounted turrets
7. After combat, units return to carrier
8. Bay doors close, carrier resumes movement
```

### Firing Arcs from Carrier

Land carriers can have mounted weapons with firing arcs:

```csharp
// Uses CombatPosition from Bay_And_Platform_Combat.md

public struct LandCarrierTurret : IBufferElementData
{
    public FixedString32Bytes TurretId;
    public float3 LocalOffset;               // Position on carrier
    public FiringArc Arc;                    // Valid firing angles
    public WeaponType Weapon;                // Cannon, missile, laser, etc.
    public float Damage;
    public float Range;                      // Meters
    public float CooldownRemaining;          // Seconds until can fire again
    public float PowerCost;                  // Power per shot
    public Entity CurrentTarget;
    public bool RequiresDeployment;          // Must stop to fire accurately
}

// Example: 360° turret on top of carrier
LandCarrierTurret topTurret = new()
{
    TurretId = "TopTurret_1",
    LocalOffset = new float3(0, 20, 0), // 20m above carrier center
    Arc = new FiringArc {
        HorizontalMin = 0f,
        HorizontalMax = 360f,   // Full rotation
        VerticalMin = -10f,     // Can aim slightly down
        VerticalMax = 80f       // Can aim up (anti-air)
    },
    Range = 1000f,              // 1km range
    RequiresDeployment = false  // Can fire while moving
};

// Example: Front-facing cannon
LandCarrierTurret frontCannon = new()
{
    TurretId = "FrontCannon_1",
    LocalOffset = new float3(0, 5, 15), // Front of carrier
    Arc = new FiringArc {
        HorizontalMin = -30f,   // 60° arc (30° left to 30° right)
        HorizontalMax = 30f,
        VerticalMin = -5f,
        VerticalMax = 20f
    },
    Range = 1500f,              // Longer range
    RequiresDeployment = true,  // Must stop for accurate fire (recoil)
    PowerCost = 50.0f           // High power cost
};
```

---

## Cross-Game Comparison

### Godgame: Land Carriers on Terrain

```csharp
// Mobile city traversing fantasy world
LandCarrier godgameCarrier = new()
{
    CarrierEntity = cityEntity,
    Class = LandCarrierClass.Fortress,
    Dimensions = new float3(100, 30, 100),  // 100x100m base, 30m tall
    BayCount = 6,
    MaxDeployedUnits = 30,
    DeploymentRange = 500f,                 // Units can go 500m from carrier
    CanMoveWhileDeployed = false,           // Must stop for deployment
    MaxSpeed = 2.0f,                        // Very slow (walking speed)
    TurnRate = 3f                           // Very wide turns
};

// Deploys:
// - War Elephants (from Vehicle Bays)
// - Catapults (from Artillery Bays)
// - Knight squadrons (from Infantry Bays)
// - Flying units (from Aircraft Bays - dragons, griffins, etc.)
```

### Space4X: Ship Carriers in Space

```csharp
// Carrier ship in space (for comparison)
public struct SpaceCarrier : IComponentData
{
    public Entity CarrierEntity;
    public ShipClass Class;
    public float3 Dimensions;
    public uint BayCount;
    public uint MaxDeployedUnits;
    public float DeploymentRange;
    public bool CanMoveWhileDeployed;       // Yes (space allows it)
    public float MaxSpeed;                  // Much faster (200+ m/s)
    public float TurnRate;                  // Moderate (45°/s)
}

// Deploys:
// - Fighters (from Fighter Bays)
// - Bombers (from Bomber Bays)
// - Mechs for boarding (from Mech Bays)
// - Drones (from Drone Bays)

// Key difference: Space carriers can move while deployed
// Land carriers typically must stop (terrain constraints)
```

**Parallel Mechanics**:
| Feature | Godgame Land Carrier | Space4X Ship Carrier |
|---------|---------------------|---------------------|
| Deployment Bays | Yes (mechs, vehicles, troops) | Yes (fighters, bombers, mechs) |
| Firing Arcs | Yes (turrets) | Yes (broadside guns) |
| Movement Constraints | Terrain-based | 3D space |
| Deploy While Moving | No (usually) | Yes (space allows) |
| Bay States | Opening/Closing | Opening/Closing |
| Coordinated Fire | Yes | Yes |
| Power Management | Critical | Critical |

---

## Strategic Use Cases

### Nomadic Civilizations

Mobile cities enable nomadic gameplay:

```csharp
public struct NomadicCivilization : IComponentData
{
    public Entity LeaderCarrier;             // Primary mobile city
    public uint TotalCarriers;               // Fleet of mobile cities
    public NomadicStrategy Strategy;
    public float ResourceGatheringEfficiency; // Bonus while mobile
    public float SettlementPenalty;          // Penalty if stationary too long
}

public enum NomadicStrategy : byte
{
    Wanderer = 0,       // Constant movement, never settle
    Seasonal = 1,       // Move with seasons (migrate)
    Opportunist = 2,    // Move to resource-rich areas, stay briefly
    Raider = 3,         // Mobile hit-and-run attacks
    Explorer = 4        // Discover new territories, map world
}

// Nomadic bonuses:
// + Gather resources from multiple biomes
// + Evade enemies by relocating
// + Difficult to siege (no fixed location)
// - Cannot build some static structures
// - Higher maintenance costs (fuel, power)
```

### Siege Platforms

Mobile cities as siege weapons:

```csharp
public struct SiegePlatform : IComponentData
{
    public Entity PlatformEntity;
    public uint MountedArtillery;            // Cannons, catapults, trebuchets
    public uint MountedBatteringRams;        // For wall breaching
    public uint GarrisonCapacity;            // Soldiers inside
    public float WallDamageBonus;            // +200% damage to walls
    public bool HasSiegeLadders;             // Can deploy ladders for assaults
    public bool HasSiegeTowers;              // Mobile towers for wall scaling
}

// Siege carrier approaches enemy city:
// 1. Deploys artillery to bombard walls
// 2. Advances siege towers to walls
// 3. Deploys infantry up siege ladders
// 4. Battering rams breach gates
// 5. Carrier acts as mobile command center
```

### Mobile Production Hubs

Cities that produce while moving:

```csharp
public struct MobileFactory : IComponentData
{
    public Entity FactoryEntity;
    public ProductionType Type;              // Weapons, vehicles, supplies
    public float ProductionRate;             // Items/day
    public float ProductionPenaltyWhileMoving; // 0.5 (50% slower when moving)
    public float RawMaterialStorage;         // Tons of materials stored
    public float FinishedGoodsStorage;       // Tons of products stored
}

// Mobile weapon factory:
// - Moves to frontline
// - Produces weapons near combat zone
// - Supplies nearby troops
// - Retreats if threatened
```

---

## Terrain Interaction

Mobile cities interact with terrain differently based on platform type:

```csharp
public struct MobileCityTerrainInteraction : IComponentData
{
    public TerrainEffect CurrentTerrain;
    public float SpeedModifier;              // Terrain speed penalty/bonus
    public float PowerModifier;              // Power consumption change
    public bool IsStuck;                     // Immobilized by terrain
    public float StuckSeverity;              // How stuck (0.0 to 1.0)
}

public enum TerrainEffect : byte
{
    None = 0,

    // Favorable
    Road = 1,           // +50% speed, -20% power
    FlatPlains = 2,     // Normal speed
    Ice = 3,            // +20% speed (low friction) but hard to turn

    // Neutral
    Forest = 10,        // -20% speed (obstacles)
    Hills = 11,         // -30% speed (climbing)

    // Unfavorable
    Mud = 20,           // -50% speed, +50% power (sinking)
    Sand = 21,          // -40% speed (loose surface)
    Swamp = 22,         // -70% speed, risk of stuck
    Mountains = 23,     // -80% speed or impassable

    // Hazardous
    Lava = 30,          // Damage per second
    DeepWater = 31,     // Impassable (unless amphibious)
    Crevasse = 32       // Instant destruction if fall in
}

// Walker platforms: Best all-terrain (can traverse mountains)
// Treaded platforms: Good on flat/hills, poor on mud/sand
// Wheeled platforms: Excellent on roads, terrible offroad
// Hover platforms: Ignore most terrain, struggle in wind
// Anti-grav platforms: Ignore all terrain
```

---

## Construction and Expansion

### Adding Buildings to Mobile City

```csharp
[BurstCompile]
public partial struct MobileCityBuildingConstructionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (platform, buildingSlots) in SystemAPI.Query<
            RefRW<MobileCityPlatform>,
            DynamicBuffer<MobileBuildingSlot>>())
        {
            // Check if there's space for new building
            if (platform.ValueRO.OccupiedSlots >= platform.ValueRO.MaxBuildingSlots)
                continue; // Platform full

            // Check construction queue
            // Build new building on platform
            // Update occupied slots
            // Update load (weight)
        }
    }
}

public struct MobileBuildingSlot : IBufferElementData
{
    public float3 LocalPosition;             // Where on platform
    public quaternion LocalRotation;         // Orientation
    public bool IsOccupied;                  // Has building?
    public Entity OccupiedBy;                // Which building
    public float MaxWeight;                  // Weight limit for this slot
}

// Mobile city construction:
// - Construct building on existing platform slot
// - Pack building from stationary location onto platform
// - Transfer building between mobile cities
// - Dismantle building for resources
```

### Platform Expansion

```csharp
public struct MobileCityExpansion : IComponentData
{
    public bool CanExpand;                   // Expandable platform?
    public uint MaxExpansions;               // How many times can expand
    public uint CurrentExpansions;           // Current expansion level
    public float ExpansionCost;              // Resources per expansion
    public float ExpansionTime;              // Days to build expansion
}

// Expansion increases:
// - Platform size (dimensions)
// - Max building slots
// - Max load capacity
// - Power generation capacity (more reactors)
// - Bay count (for carriers)
```

---

## Damage and Destruction

Mobile cities can be damaged:

```csharp
public struct MobileCityHealth : IComponentData
{
    public float StructuralIntegrity;        // 0.0 to 1.0
    public float PlatformHealth;             // Platform HP
    public float PropulsionHealth;           // Locomotion HP (legs, treads)
    public DamageEffect[] ActiveDamageEffects;
}

public enum DamageEffect : byte
{
    None = 0,

    // Mobility Damage
    LocomotionImpaired = 1,     // -50% speed (damaged legs/treads)
    LocomotionDestroyed = 2,    // 0% speed (immobilized)
    SteeringDamaged = 3,        // Cannot turn (stuck moving straight)

    // Power Damage
    ReactorDamaged = 4,         // -50% power generation
    ReactorCritical = 5,        // -80% power, risk of meltdown
    ReactorDestroyed = 6,       // 0% power (dead in water)

    // Building Damage
    BuildingsCollapsing = 7,    // Random buildings take damage
    PopulationCasualties = 8,   // Citizens dying
    FireSpreading = 9,          // Fire spreading between buildings

    // Critical Damage
    PlatformBreaking = 10,      // Structural failure imminent
    Sinking = 11,               // Platform sinking into ground/water
    Exploding = 12              // Catastrophic reactor failure
}

// Damage progression:
// 100% HP: Fully operational
// 75% HP: Minor damage (sparks, smoke)
// 50% HP: Moderate damage (locomotion impaired, power reduced)
// 25% HP: Severe damage (fires, building collapse, casualties)
// 0% HP: Destruction (platform destroyed, all buildings/population lost)
```

---

## AI Behavior for Mobile Cities

```csharp
public struct MobileCityAI : IComponentData
{
    public MobileCityAIState CurrentState;
    public Entity Destination;               // Where is it going
    public MobileCityGoal Goal;
    public float CautionLevel;               // 0.0 (reckless) to 1.0 (cautious)
}

public enum MobileCityAIState : byte
{
    Idle = 0,           // Stationary, no orders
    Traveling = 1,      // Moving to destination
    Gathering = 2,      // Harvesting resources
    Combat = 3,         // Engaging enemies
    Fleeing = 4,        // Retreating from threat
    Deploying = 5,      // Opening bays, deploying units
    Recalling = 6,      // Retrieving deployed units
    Repairing = 7,      // Stopped for repairs
    Refueling = 8       // Stopped for fuel/power
}

public enum MobileCityGoal : byte
{
    Explore = 0,        // Discover new territory
    Colonize = 1,       // Find settlement location
    Raid = 2,           // Attack enemy settlements
    Defend = 3,         // Protect friendly territory
    Trade = 4,          // Visit trade partners
    Migrate = 5,        // Relocate to new biome
    Patrol = 6,         // Guard area
    Siege = 7           // Besiege enemy city
}

// AI decision-making:
// High Caution: Avoids combat, flees when outnumbered, prioritizes repairs
// Low Caution: Engages enemies, takes risks, pursues aggressive goals
```

---

## Performance Considerations

### LOD System for Mobile Cities

```csharp
public struct MobileCityLOD : IComponentData
{
    public LODLevel CurrentLOD;
    public float DistanceToCamera;
}

public enum LODLevel : byte
{
    High = 0,       // Full detail (all buildings, citizens visible)
    Medium = 1,     // Reduced detail (simplified buildings, no citizens)
    Low = 2,        // Platform only (buildings as simple shapes)
    Minimal = 3     // Single entity (carrier as one mesh)
}

// LOD thresholds:
// 0-200m: High detail
// 200-500m: Medium detail
// 500-1000m: Low detail
// 1000m+: Minimal detail
```

### Simulation Performance

```csharp
// Mobile city simulation is expensive:
// - Movement: Standard locomotion cost
// - Buildings: N buildings × building update cost
// - Population: M citizens × citizen update cost
// - Power: Power grid calculation
// - Combat: Turret targeting, bay management

// Optimization strategies:
// 1. Aggregate simulation: Treat mobile city as single entity (not N buildings)
// 2. Update throttling: Update buildings every 0.5s instead of every frame
// 3. Spatial culling: Don't simulate offscreen mobile cities
// 4. Bay pooling: Reuse bay entities instead of creating/destroying
```

**Profiling Targets**:
```
Locomotion:        <0.5ms per mobile city
Building Updates:  <2.0ms per mobile city (50 buildings)
Population:        <1.0ms per mobile city (500 citizens)
Power Management:  <0.2ms per mobile city
Combat Systems:    <1.0ms per mobile city (6 bays)
────────────────────────────────────────────────────
Total:             <4.7ms per mobile city

Target: Support 10 mobile cities at 60 FPS
        10 cities × 4.7ms = 47ms frame budget
        Requires aggressive LOD and throttling
```

---

## Future Extensions

### Multi-Level Mobile Cities

```csharp
// Stackable platforms (city on top of city)
public struct MultiLevelMobileCity : IComponentData
{
    public uint LevelCount;                  // How many stacked platforms
    public float TotalHeight;                // Meters
    public float StabilityFactor;            // 0.0 to 1.0 (risk of toppling)
}
```

### Underwater Mobile Cities

```csharp
// Submarine cities
LocomotionCapabilities underwaterCity = new()
{
    Mode = LocomotionMode.Swimming,
    Directionality = MovementDirectionality.VolumetricOmni,
    MaxSpeed = 4.0f,
    ValidTerrain = TerrainMask.Water,
    MinAltitude = -1000f,   // Can go 1km deep
    MaxAltitude = -10f      // Must stay submerged
};
```

### Flying Mobile Cities

```csharp
// Anti-gravity megastructures
LocomotionCapabilities skyCity = new()
{
    Mode = LocomotionMode.Antigrav,
    Directionality = MovementDirectionality.VolumetricOmni,
    MaxSpeed = 15.0f,
    ValidTerrain = TerrainMask.Air,
    MinAltitude = 500f,     // Flies high above ground
    MaxAltitude = 5000f     // Below cloud layer
};
```

---

## Summary

Mobile Cities and Land Carriers provide:

1. **Tech Progression**: Stationary → Mobile Structures → Mobile Cities → Land Carriers
2. **Diverse Locomotion**: Treads, walker limbs, hover, anti-gravity
3. **Full City Functionality**: Production, housing, defense while mobile
4. **Combat Deployment**: Deploy units from bays (mechs, vehicles, troops)
5. **Strategic Depth**: Nomadic gameplay, siege platforms, mobile production
6. **Cross-Game Parallel**: Godgame land carriers = Space4X ship carriers (on terrain)
7. **Resource Management**: Power, fuel, population, morale
8. **Terrain Interaction**: Different platforms excel in different terrain
9. **Damage Systems**: Locomotion, power, structural integrity
10. **Integration**: Works with locomotion, bay combat, forces, power systems

**Key Innovation**: Treating entire cities as single mobile entities with aggregate behavior creates strategic gameplay impossible with static settlements. Land carriers bridge RTS base-building with tactical unit deployment, offering unprecedented flexibility in both Godgame and Space4X contexts.
