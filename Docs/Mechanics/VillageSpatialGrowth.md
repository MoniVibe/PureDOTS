# Village Spatial Growth & Building Placement (Godgame)

## Overview

Villages in Godgame are emergent spatial structures that grow organically based on population, resources, and player intervention. This document defines how villages expand spatially, where buildings are placed, how village boundaries are determined, and the rules governing construction.

---

## Core Concept

**Villages are not pre-defined zones.** Instead:
- **Villages emerge** from villager settlements (first storehouse = village center)
- **Boundaries expand** as population grows and buildings are constructed
- **Building placement** follows spatial rules (proximity, terrain, culture)
- **Player can guide** growth via divine hand (place buildings, bless/curse zones)
- **AI decides** optimal building locations when player doesn't intervene

---

## Village Entity Components

```csharp
// Singleton per village
public struct Village : IComponentData
{
    public FixedString64Bytes VillageName;
    public Entity FoundingStorehouse;            // First storehouse that created this village
    public float3 CenterPosition;                // Average position of all buildings
    public ushort Population;                    // Villagers assigned to this village
    public VillageSize Size;                     // Hamlet → Town → City → Metropolis
    public float InfluenceRadius;                // How far village "owns" surrounding terrain
}

public enum VillageSize : byte
{
    Hamlet,         // 1-20 villagers, 1-5 buildings
    Village,        // 20-50 villagers, 5-15 buildings
    Town,           // 50-150 villagers, 15-40 buildings
    City,           // 150-500 villagers, 40-100 buildings
    Metropolis      // 500+ villagers, 100+ buildings
}
```

```csharp
// Buffer tracking all buildings in this village
public struct VillageBuildingEntry : IBufferElementData
{
    public Entity BuildingEntity;
    public BuildingCategory Category;
    public float3 Position;
    public ushort ConstructionTick;              // When built (for history)
}

public enum BuildingCategory : byte
{
    Storehouse,     // Resource storage (required, village center)
    Housing,        // Villager homes (increases capacity)
    Production,     // Workshop, sawmill, smithy (crafting)
    Farming,        // Field, orchard, fishing hut (food generation)
    Religious,      // Temple, shrine (prayer generation, morale)
    Military,       // Barracks, archery range (training, defense)
    Utility,        // Well, tavern, market (morale, trade)
    Defense,        // Wall, gate, tower (protection)
    Special         // Wonders, monuments (unique bonuses)
}
```

```csharp
// Village boundary (implicit, computed from buildings)
public struct VillageBoundary : IComponentData
{
    public float3 MinBounds;                     // AABB min corner
    public float3 MaxBounds;                     // AABB max corner
    public float InfluenceRadius;                // Circle radius from center
    public BoundaryShape Shape;
}

public enum BoundaryShape : byte
{
    Circle,         // Simple radius from center
    AABB,           // Axis-aligned bounding box
    Convex          // Convex hull of all buildings (expensive, optional)
}
```

---

## Village Founding

A village is created when the **first storehouse** is built.

```csharp
public struct VillageFoundingRequest : IComponentData
{
    public float3 Position;                      // Where to found village
    public Entity RequestingVillager;            // Who initiated (optional)
}
```

**Founding Rules**:
1. **Minimum Distance from Other Villages**: 100 units (prevents overlap)
2. **Terrain Requirements**: Flat-ish ground (slope <30°), not water
3. **Resource Proximity**: Within 50 units of at least 1 resource (forest, deposit, water)
4. **Player Approval**: Optional (player can veto AI founding requests)

**Founding Process**:
1. Villager AI or player issues `VillageFoundingRequest`
2. `VillageFoundingSystem` validates position (distance, terrain, resources)
3. If valid: spawn Storehouse entity, create Village entity, assign founding villagers
4. Village starts as Hamlet size with InfluenceRadius = 30 units

---

## Village Growth Triggers

Villages expand when population or resource thresholds are met.

```csharp
public struct VillageGrowthState : IComponentData
{
    public ushort PopulationCurrent;
    public ushort PopulationCapacity;            // Max before needing housing
    public ushort HousingBuildings;              // How many homes exist
    public VillageSize CurrentSize;
    public VillageSize NextSize;
    public float GrowthProgress;                 // 0.0-1.0 toward next size
}
```

**Growth Thresholds**:
- **Hamlet → Village**: 20 population, 5 buildings
- **Village → Town**: 50 population, 15 buildings
- **Town → City**: 150 population, 40 buildings
- **City → Metropolis**: 500 population, 100 buildings

**On Size Increase**:
- InfluenceRadius expands (Hamlet: 30, Village: 50, Town: 80, City: 120, Metropolis: 200)
- New building types unlock (Town unlocks Temple, City unlocks Market, etc.)
- AI building placement priorities shift (more housing, less production)

---

## Building Placement System

### Placement Modes

**1. Player-Directed Placement** (Divine Hand)
- Player selects building type from UI/gesture
- Hand cursor shows ghost preview of building
- Placement constraints validated (terrain, proximity, resources)
- LMB click confirms placement → construction begins

**2. AI-Directed Placement** (Villager Jobs)
- Villager AI evaluates village needs (housing shortage, food deficit)
- AI selects building type and queries spatial system for optimal location
- AI follows same constraints as player
- Construction queued automatically

### Placement Constraints

```csharp
public struct BuildingPlacementConstraints : IComponentData
{
    public BuildingCategory Category;

    // Terrain
    public float MaxSlope;                       // 0.0-90.0 degrees
    public TerrainMask AllowedTerrain;           // Grass, dirt, sand (not water, rock)

    // Proximity
    public float MinDistanceToOtherBuildings;    // Prevent overlap
    public float MaxDistanceFromVillageCenter;   // Buildings can't be too far
    public float MinDistanceToResource;          // e.g., sawmill near forest

    // Special
    public bool RequiresWaterAccess;             // Fishing hut, mill
    public bool RequiresRoadAccess;              // Market, tavern
    public ushort MinPopulation;                 // Unlock threshold
}

[Flags]
public enum TerrainMask : byte
{
    Grass = 1 << 0,
    Dirt = 1 << 1,
    Sand = 1 << 2,
    Stone = 1 << 3,
    Water = 1 << 4,
    Snow = 1 << 5
}
```

**Example Constraints by Category**:

| Category | MaxSlope | AllowedTerrain | MinDistToBuildings | MaxDistFromCenter | Special |
|----------|----------|----------------|---------------------|-------------------|---------|
| Storehouse | 15° | Grass, Dirt | 20 units | N/A (is center) | None |
| Housing | 20° | Grass, Dirt, Sand | 8 units | 60 units | None |
| Production | 25° | Grass, Dirt, Stone | 12 units | 80 units | None |
| Farming | 10° | Grass (only) | 15 units | 100 units | None |
| Religious | 15° | Grass | 25 units | 60 units | High ground preferred |
| Military | 30° | Any solid | 20 units | 100 units | Perimeter preferred |
| Defense | 45° | Any | 5 units | InfluenceRadius | On boundary |

### Placement Scoring System

AI evaluates potential building locations using a **score function**.

```csharp
public struct PlacementScore
{
    public float3 Position;
    public float TotalScore;                     // Higher = better

    // Score breakdown
    public float TerrainScore;                   // Flat = good, steep = bad
    public float ProximityScore;                 // Near similar buildings = good
    public float ResourceScore;                  // Near required resources = good
    public float CenterDistanceScore;            // Near village center = good (usually)
    public float AestheticScore;                 // Aligned with roads, symmetry
}
```

**Score Calculation**:
```
TotalScore = TerrainScore * 0.3
           + ProximityScore * 0.25
           + ResourceScore * 0.25
           + CenterDistanceScore * 0.15
           + AestheticScore * 0.05
```

**Terrain Score**:
- Slope = 0° → 1.0
- Slope = MaxSlope → 0.5
- Slope > MaxSlope → 0.0 (invalid)

**Proximity Score**:
- Housing benefits from clustering (near other houses)
- Production benefits from spacing (noise/pollution separation)
- Religious benefits from isolation (peaceful)

**Resource Score** (category-specific):
- Sawmill: distance to forest (closer = higher score)
- Fishing Hut: distance to water (must be <10 units)
- Storehouse: distance to all resource types (centralized = good)

**Center Distance Score**:
- Most buildings: closer to center = higher score
- Defense: closer to boundary = higher score
- Farming: moderate distance (not center, not edge)

**Aesthetic Score**:
- Grid alignment (snap to 5-unit grid)
- Road adjacency (buildings near roads preferred)
- Symmetry detection (mirror existing layout)

---

## Building Snap & Alignment

Buildings can snap to grid or align with existing structures.

```csharp
public struct BuildingSnapSettings : IComponentData
{
    public SnapMode Mode;
    public float GridSize;                       // 5.0 units (typical)
    public bool AlignToRoads;
    public bool AlignToExistingBuildings;
    public float AlignmentThreshold;             // <15° = snap rotation
}

public enum SnapMode : byte
{
    None,           // Free placement
    Grid,           // Snap to grid (GridSize intervals)
    Road,           // Align parallel to nearest road
    Existing        // Match rotation of nearby buildings
}
```

**Grid Snap Example**:
```csharp
float3 SnapToGrid(float3 position, float gridSize)
{
    return new float3(
        math.round(position.x / gridSize) * gridSize,
        position.y, // Y unchanged (terrain height)
        math.round(position.z / gridSize) * gridSize
    );
}
```

**Road Alignment**:
- Find nearest road entity within 20 units
- Rotate building to align parallel or perpendicular to road
- Snap rotation to nearest 90° (0°, 90°, 180°, 270°)

---

## Construction Process

Once placement is confirmed, construction begins.

```csharp
public struct BuildingConstruction : IComponentData
{
    public BuildingArchetype Archetype;          // What's being built
    public float3 Position;
    public quaternion Rotation;
    public ConstructionPhase Phase;
    public ushort TicksRemaining;                // Until completion
    public ushort WorkerCount;                   // Villagers assigned
    public DynamicBuffer<ConstructionMaterial> RequiredMaterials;
}

public enum ConstructionPhase : byte
{
    Planned,        // Awaiting materials
    Foundation,     // Base construction (25% progress)
    Framing,        // Walls/structure (50% progress)
    Roofing,        // Roof/ceiling (75% progress)
    Finishing       // Interior/decoration (100% progress)
}

public struct ConstructionMaterial : IBufferElementData
{
    public ushort ResourceTypeId;
    public ushort AmountRequired;
    public ushort AmountDelivered;
}
```

**Construction Steps**:
1. **Planned Phase**: Villagers transport materials from storehouse to site
2. **Foundation Phase**: Workers assigned (BuildJobBehavior), progress ticks
3. **Completion**: Building entity spawned, construction entity destroyed, VillageBuildingEntry added

**Construction Speed**:
```
TicksPerPhase = BaseTime / (1 + WorkerCount * WorkerEfficiency)
```

Example:
- BaseTime = 500 ticks per phase (2000 ticks total for 4 phases)
- 1 worker (efficiency 1.0) = 500 ticks/phase = 2000 ticks total (~33 seconds)
- 5 workers (efficiency 1.0 each) = 500 / 6 = 83 ticks/phase = 332 ticks total (~5.5 seconds)

---

## Village Boundary Expansion

As buildings are added, village boundary grows.

```csharp
[BurstCompile]
public partial struct VillageBoundaryUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (village, boundary, buildingBuffer) in
                 SystemAPI.Query<RefRW<Village>, RefRW<VillageBoundary>, DynamicBuffer<VillageBuildingEntry>>())
        {
            if (buildingBuffer.Length == 0)
                continue;

            // Compute AABB
            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);
            float3 center = float3.zero;

            foreach (var building in buildingBuffer)
            {
                min = math.min(min, building.Position);
                max = math.max(max, building.Position);
                center += building.Position;
            }

            center /= buildingBuffer.Length;

            // Update boundary
            boundary.ValueRW.MinBounds = min;
            boundary.ValueRW.MaxBounds = max;
            village.ValueRW.CenterPosition = center;

            // Compute influence radius (max distance from center + padding)
            float maxDist = 0f;
            foreach (var building in buildingBuffer)
            {
                float dist = math.distance(center, building.Position);
                maxDist = math.max(maxDist, dist);
            }

            village.ValueRW.InfluenceRadius = maxDist + 20f; // 20 unit padding
        }
    }
}
```

---

## Building Archetypes

Each building type has specific spatial footprint and function.

```csharp
public struct BuildingArchetype : IComponentData
{
    public FixedString64Bytes Name;
    public BuildingCategory Category;
    public float3 FootprintSize;                 // Dimensions (x, y, z)
    public ushort ConstructionTime;              // Base ticks
    public DynamicBuffer<ResourceCost> MaterialCosts;
    public ushort PopulationCapacity;            // Housing: +10, Storehouse: 0
    public ushort WorkerSlots;                   // Production: workers needed
}

public struct ResourceCost : IBufferElementData
{
    public ushort ResourceTypeId;
    public ushort Amount;
}
```

**Example Archetypes**:

| Building | Footprint | Construction Time | Materials | Capacity | Workers |
|----------|-----------|-------------------|-----------|----------|---------|
| **Small House** | 5x4x3 | 300 ticks | Wood: 20 | +5 villagers | 0 |
| **Storehouse** | 10x8x5 | 800 ticks | Wood: 50, Stone: 30 | 0 | 2 (management) |
| **Sawmill** | 8x8x4 | 500 ticks | Wood: 30, Stone: 10 | 0 | 3 (workers) |
| **Temple** | 12x12x8 | 1500 ticks | Wood: 80, Stone: 100 | 0 | 5 (priests) |
| **Barracks** | 10x10x4 | 1000 ticks | Wood: 60, Stone: 50 | 0 | 10 (soldiers) |
| **Wall Segment** | 5x2x3 | 200 ticks | Stone: 20 | 0 | 0 |

---

## Road System (Optional Enhancement)

Roads connect buildings and improve village aesthetics/efficiency.

```csharp
public struct Road : IComponentData
{
    public RoadType Type;
    public float Width;                          // 2.0 units (dirt path) to 6.0 (stone road)
}

public enum RoadType : byte
{
    DirtPath,       // Free, low durability
    GravelRoad,     // Cheap, medium durability
    StoneRoad,      // Expensive, high durability
    BrickRoad       // Very expensive, decorative
}
```

**Road Pathfinding**:
- Roads automatically connect buildings (AI places dirt paths between new buildings and center)
- Player can upgrade roads (dirt → gravel → stone)
- Villagers move faster on roads (1.5x speed multiplier)

**Road Placement**:
- AI system detects isolated buildings (>20 units from nearest road)
- Pathfinding calculates shortest route to existing road network
- Road entities spawned along path (5 unit segments)

---

## Cultural Influence on Placement

Different cultures (see [HeritageAndKnowledgeSystem.md](../DesignNotes/HeritageAndKnowledgeSystem.md)) have unique building preferences.

```csharp
public struct CulturalBuildingPreferences : IComponentData
{
    public Entity CultureEntity;
    public float GridAlignmentPreference;        // 0.0-1.0 (ordered vs. organic)
    public float CentralizationPreference;       // Cluster vs. sprawl
    public float SymmetryPreference;             // Mirror layouts vs. asymmetry
    public BuildingCategory PreferredCategory;   // Cultural focus
}
```

**Example Cultural Styles**:
- **Militaristic Culture**: High centralization, barracks near center, walls prioritized
- **Agrarian Culture**: Low centralization, farms spread wide, housing clustered
- **Religious Culture**: Temples on high ground, symmetrical layout, central shrine
- **Mercantile Culture**: Market at center, road network extensive, high grid alignment

**AI Placement Adjustment**:
```csharp
PlacementScore.AestheticScore *= CulturalPreferences.GridAlignmentPreference;
PlacementScore.CenterDistanceScore *= CulturalPreferences.CentralizationPreference;
```

---

## Village Merging (Advanced)

Two nearby villages can merge if they grow into each other.

```csharp
public struct VillageMergeRequest : IComponentData
{
    public Entity Village1;
    public Entity Village2;
    public MergeReason Reason;
}

public enum MergeReason : byte
{
    OverlappingBoundaries,   // Influence radii overlap >50%
    PlayerForced,            // Player manually merges
    PoliticalUnion,          // Diplomacy/alignment agreement
    Conquest                 // One village absorbed by force
}
```

**Merge Process**:
1. Combine building buffers (Village1 + Village2)
2. Recalculate center position (weighted by population)
3. Merge villager assignments (assign all to larger village)
4. Destroy smaller village entity
5. Recalculate boundary (may become AABB or Convex)

---

## Player Intervention Options

### Divine Building Placement
- Player selects building from UI wheel/menu
- Ghost preview shows footprint with color-coded validity (green = valid, red = invalid)
- Constraints displayed (e.g., "Too steep", "Requires water access")
- LMB confirms → construction entity spawned, villagers auto-assigned

### Blessing/Cursing Zones
- Player can bless zone → buildings built 2x faster, materials cost -20%
- Player can curse zone → buildings decay faster, villagers avoid area
- Zones represented as `AreaEffect` entities with radius + duration

### Forced Demolition
- Player can select building with divine hand
- RMB hold → demolition countdown (5 seconds)
- Building destroyed, materials refunded 50% to storehouse
- Villagers reassigned automatically

---

## Integration with Other Systems

### Villager Job System
- Construction buildings create `BuildJob` tickets
- Villagers assigned via `VillagerJobAssignmentSystem`
- Progress increments based on worker count + skill

### Resource System
- Construction materials withdrawn from storehouse via `WithdrawalRequest`
- Delivered to construction site by hauler villagers
- `ConstructionMaterial` buffer tracks delivery progress

### Spatial Grid
- Buildings register in spatial grid for proximity queries
- AI placement queries grid for nearby entities (avoid overlap)
- Village boundary updates spatial grid entries

### Ambient Happiness
(from [VillageAmbientHappiness.md](../Mechanics/VillageAmbientHappiness.md))
- Building placement affects happiness emissions
- Temple near housing → +morale
- Sawmill near housing → -morale (noise)
- Player strategic placement optimizes happiness

---

## Performance Considerations

**Placement Query Optimization**:
- AI doesn't evaluate every possible position (too slow)
- Sample grid positions at 5-unit intervals
- Limit samples to InfluenceRadius (e.g., 50 samples for Village size)
- Cache terrain slope data to avoid repeated raycasts

**Boundary Update Frequency**:
- Only recalculate boundary when building added/removed (not every tick)
- Use dirty flag: `VillageBoundaryDirty` component added on building change
- `VillageBoundaryUpdateSystem` processes only dirty villages, removes flag after update

**Spatial Grid Registration**:
- Buildings registered once on construction completion (not during construction)
- Construction site uses placeholder spatial entry (smaller footprint)

---

## Open Questions / Design Decisions Needed

1. **Building Rotation**: Can player rotate buildings freely, or snap to 90° increments?
   - *Suggestion*: 90° snaps for Phase 1 (simpler), free rotation later

2. **Multi-Tile Buildings**: Can buildings span multiple grid cells (e.g., 10x10 Temple)?
   - *Suggestion*: Yes, footprint defined per archetype

3. **Terrain Modification**: Can player flatten terrain for building placement (divine landscaping)?
   - *Suggestion*: Yes, but costs favor/mana (miracle-based)

4. **Overlapping Villages**: What happens if two villages' influence radii overlap but don't merge?
   - *Suggestion*: Villagers assigned to nearest village center, buildings belong to founding village

5. **Building Upgrades**: Can buildings be upgraded in-place (Small House → Large House)?
   - *Suggestion*: Yes, via construction system (destroy + rebuild, materials refunded partially)

6. **Village Abandonment**: If population drops to 0, does village entity persist?
   - *Suggestion*: Yes, becomes "ghost town" (reusable if new villagers arrive)

7. **Building Decay**: Do unused buildings degrade over time?
   - *Suggestion*: Yes, if no workers assigned for >1000 ticks, condition drops

8. **Cultural Conflict**: If two cultures merge villages, which building style wins?
   - *Suggestion*: Dominant culture (higher population) sets preferences

---

## Implementation Notes

- **Village** entity = singleton per village with `Village + VillageBoundary + VillageBuildingEntry buffer`
- **VillageFoundingSystem** = detects founding requests, validates, creates village
- **VillageBoundaryUpdateSystem** = recalculates boundary when buildings change
- **BuildingPlacementSystem** = AI queries spatial grid, scores positions, selects best
- **BuildingConstructionSystem** = processes construction phases, spawns buildings
- **VillageGrowthSystem** = monitors population, triggers size increases
- **VillageMergeSystem** = detects overlapping boundaries, processes merges

---

## References

- **Villager Jobs**: [VillagerJobs_DOTS.md](../DesignNotes/VillagerJobs_DOTS.md) - Construction job integration
- **Ambient Happiness**: [VillageAmbientHappiness.md](VillageAmbientHappiness.md) - Building placement affects morale
- **Spatial Grid**: Proximity queries for building placement validation
- **Heritage & Knowledge**: [HeritageAndKnowledgeSystem.md](../DesignNotes/HeritageAndKnowledgeSystem.md) - Cultural building preferences
- **Production Chains**: [ProductionChains.md](../DesignNotes/ProductionChains.md) - Production buildings integration
