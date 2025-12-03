# Extension Request: Caravan & Long-Distance Logistics System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Logistics/CaravanComponents.cs` - CaravanState, AmbushOutcome, TradeRoute, Caravan, CargoManifest, RouteInfrastructure, AmbushEvent, LogisticsConfig
- `Packages/com.moni.puredots/Runtime/Runtime/Logistics/CaravanHelpers.cs` - Static helpers for route quality, travel time, ambush resolution, cargo management

---

## Use Case

Long-distance logistics are needed for:

**Godgame:**
- Merchant caravans between villages
- Stagecoach passenger transport
- Trade route establishment and maintenance
- Route quality and infrastructure
- Bandit ambushes and escort mechanics

**Space4X:**
- Trade convoys between colonies
- Supply chains
- Pirate interdiction
- Escort missions

---

## Proposed Components

```csharp
// === Trade Routes ===
public struct TradeRoute : IBufferElementData
{
    public Entity SourceLocation;        // Village, colony
    public Entity DestinationLocation;
    public float Distance;               // In world units
    public float TravelTime;             // Ticks to traverse
    public float TransportCostPerUnit;   // Currency per 100kg per 100km
    public byte RouteQuality;            // 0-3 (Uncharted→Major)
    public uint TotalTrips;              // Trips completed, affects quality
    public float SecurityRating;         // 0-1, higher = safer
}

// === Caravans/Convoys ===
public struct Caravan : IComponentData
{
    public Entity HomeBase;
    public Entity CurrentRoute;          // Active trade route
    public float3 CurrentPosition;
    public float Progress;               // 0-1 along route
    public CaravanState State;
    public float CargoCapacity;          // Max weight
    public float CurrentCargoWeight;
    public byte GuardCount;              // Escorts
    public float Speed;                  // Modifies travel time
}

public enum CaravanState : byte
{
    Idle = 0,
    Loading = 1,
    Traveling = 2,
    Unloading = 3,
    Returning = 4,
    UnderAttack = 5,
    Destroyed = 6
}

// === Cargo Manifest ===
public struct CargoManifest : IBufferElementData
{
    public FixedString32Bytes ResourceType;
    public float Quantity;
    public float PurchasePrice;          // For profit calculation
    public Entity DestinationStorage;    // Where to deliver
}

// === Route Infrastructure ===
public struct RouteInfrastructure : IBufferElementData
{
    public Entity RouteEntity;
    public FixedString32Bytes InfraType; // "road", "bridge", "inn", "guard_post"
    public float3 Position;
    public float Condition;              // 0-1, degrades over time
    public float EffectRadius;
    public float BenefitModifier;        // Cost reduction, safety bonus
}

// === Ambush/Interdiction ===
public struct AmbushEvent : IComponentData
{
    public Entity TargetCaravan;
    public Entity AttackerEntity;        // Bandit camp, pirate fleet
    public float AmbushStrength;
    public float DefenseStrength;
    public bool IsResolved;
    public AmbushOutcome Outcome;
}

public enum AmbushOutcome : byte
{
    Pending = 0,
    CaravanEscaped = 1,
    CaravanDefended = 2,
    CargoStolen = 3,
    CaravanDestroyed = 4
}

// === Configuration ===
public struct LogisticsConfig : IComponentData
{
    public uint RouteQualityThreshold1;  // Trips for "Developing"
    public uint RouteQualityThreshold2;  // Trips for "Established"
    public uint RouteQualityThreshold3;  // Trips for "Major"
    public float BaseAmbushChance;
    public float GuardEffectiveness;     // Per guard reduction in ambush
    public float InfrastructureDecayRate;
}
```

### New Systems
- `CaravanMovementSystem` - Moves caravans along routes
- `RouteQualitySystem` - Upgrades routes based on usage
- `CargoLoadingSystem` - Handles loading/unloading
- `AmbushResolutionSystem` - Resolves attacks on caravans
- `InfrastructureDecaySystem` - Maintains route infrastructure

---

## Example Usage

```csharp
// === Establish trade route ===
var route = new TradeRoute {
    SourceLocation = villageA,
    DestinationLocation = villageB,
    Distance = 150f,
    TravelTime = 300, // 5 days
    RouteQuality = 0, // Uncharted
    SecurityRating = 0.6f
};

// === Send caravan ===
var caravan = new Caravan {
    HomeBase = villageA,
    CurrentRoute = routeEntity,
    CargoCapacity = 500f,
    GuardCount = 2,
    Speed = 1.0f
};

// === Cargo manifest ===
cargoBuffer.Add(new CargoManifest {
    ResourceType = "Iron Ingots",
    Quantity = 200f,
    PurchasePrice = 5f,
    DestinationStorage = villageBStorehouse
});
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Logistics/` directory
- Integration: Resource systems, spatial queries

**Breaking Changes:** None - new system

---

## Review Notes

*(PureDOTS team use)*

