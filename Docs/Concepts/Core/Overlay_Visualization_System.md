# Overlay Visualization System

## Overview

Comprehensive data overlay system that visualizes world information through multiple interactive layers. Players toggle between overlays to understand resources, climate, entity traffic, pollution, and aggregate entity positions. Each overlay supports interaction (selection, tooltips, manipulation) and provides actionable insights.

**Key Principles**:
- **Multi-layer visualization**: Toggle between 10+ data overlays
- **Interactive tooltips**: Contextual information on hover
- **Actionable insights**: Not just data, but "what can I do?"
- **Performance**: Burst-optimized spatial queries, GPU instancing for grids
- **Deterministic**: Overlay data derived from simulation state
- **Cross-game**: Godgame (terrain-based) and Space4X (sector-based) variants
- **Accessibility**: Colorblind modes, pattern overlays, screen reader support

---

## Overlay Types

### 1. Resource Overlay

Shows resource distribution, quality, rarity, and extraction rates:

```csharp
public struct ResourceOverlayData : IComponentData
{
    public ResourceOverlayMode Mode;
    public ResourceType FilterType;         // Which resource to show
    public bool ShowQuality;                // Color by quality
    public bool ShowRarity;                 // Show rarity indicators
    public bool ShowExtractionRate;         // Visualize depletion
    public float MinDisplayThreshold;       // Hide small deposits
}

public enum ResourceOverlayMode : byte
{
    AllResources = 0,           // Show all resource types
    SingleResource = 1,         // Filter to one type
    Abundance = 2,              // Heat map of total resources
    Quality = 3,                // Show quality distribution
    Rarity = 4,                 // Highlight rare resources
    ExtractionEfficiency = 5,   // Best extraction locations
    Depletion = 6               // Resources running out
}

public enum ResourceType : byte
{
    // Godgame
    Food = 0,
    Wood = 1,
    Stone = 2,
    Gold = 3,
    Iron = 4,
    Gems = 5,

    // Space4X
    Minerals = 10,
    Energy = 11,
    ExoticMatter = 12,
    Organics = 13,

    // Special
    Mana = 20,                  // Godgame divine energy
    Antimatter = 21             // Space4X high-value
}

/// <summary>
/// Per-tile/sector resource visualization data.
/// </summary>
public struct ResourceVisualizationTile : IBufferElementData
{
    public int2 GridCoord;              // Tile position
    public ResourceType Type;
    public float Amount;                // 0.0 to max
    public float Quality;               // 0.0 to 1.0 (poor to pristine)
    public float Rarity;                // 0.0 to 1.0 (common to mythic)
    public float ExtractionRate;        // Units per second
    public float DepletionTimeSeconds;  // Time until empty

    // Visualization
    public float4 Color;                // RGBA color for this tile
    public float Intensity;             // 0.0 to 1.0 (for heat map)
    public bool IsRareDeposit;          // Show special indicator
}
```

**Visualization**:
- **Heat map**: Color intensity = resource abundance
- **Quality gradient**: Green (high) → Yellow (medium) → Red (low)
- **Rarity icons**: Sparkle/star icons on rare deposits
- **Depletion arrows**: Downward arrows on depleting resources

**Interactions**:
- **Click tile**: Select resource deposit, show extraction options
- **Hover**: Tooltip shows amount, quality, rarity, depletion time
- **Right-click**: Quick-build extractor building
- **Shift-click**: Compare multiple deposits

**Tooltip Example**:
```
Gold Deposit (Pristine Quality)
━━━━━━━━━━━━━━━━━━━━━━━━━━━
Amount:      2,450 units
Quality:     0.92 (Pristine)
Rarity:      0.15 (Uncommon)
Extract Rate: 12.5/sec

Depletion: 3m 16s remaining

⚡ Build Gold Mine (150 wood, 50 stone)
⚡ Assign 5 miners for optimal extraction
```

---

### 2. Climate Overlay

Shows temperature, moisture, wind patterns, and biome information:

```csharp
public struct ClimateOverlayData : IComponentData
{
    public ClimateOverlayMode Mode;
    public bool ShowWindVectors;
    public bool ShowMoistureFlow;
    public bool ShowTemperatureGradient;
    public bool ShowBiomeBoundaries;
}

public enum ClimateOverlayMode : byte
{
    Temperature = 0,        // Heat map
    Moisture = 1,           // Humidity/rainfall
    WindPatterns = 2,       // Wind direction/speed
    Biomes = 3,             // Ecosystem types
    Seasons = 4,            // Seasonal changes (preview)
    Habitability = 5        // Suitability for settlement
}

/// <summary>
/// Per-tile climate data for visualization.
/// </summary>
public struct ClimateVisualizationTile : IBufferElementData
{
    public int2 GridCoord;

    // Climate data
    public float Temperature;           // -50°C to +50°C
    public float Moisture;              // 0.0 to 1.0
    public float2 WindVector;           // Direction and speed
    public BiomeType Biome;
    public float Habitability;          // 0.0 to 1.0

    // Seasonal (for preview)
    public float TemperatureSpring;
    public float TemperatureSummer;
    public float TemperatureFall;
    public float TemperatureWinter;

    // Modification potential
    public bool CanModifyClimate;
    public float ModificationCost;      // Mana/energy cost
    public BiomeType[] PossibleBiomes;  // What can this become
}

public enum BiomeType : byte
{
    // Godgame
    Forest = 0,
    Plains = 1,
    Desert = 2,
    Tundra = 3,
    Swamp = 4,
    Mountain = 5,
    Ocean = 6,
    Volcanic = 7,

    // Space4X
    Terran = 10,
    Barren = 11,
    Frozen = 12,
    Toxic = 13,
    Lava = 14,
    GasGiant = 15
}
```

**Visualization**:
- **Temperature**: Blue (cold) → Green (temperate) → Red (hot)
- **Moisture**: Brown (dry) → Green (moist) → Blue (wet)
- **Wind**: Animated arrows showing direction/speed
- **Biomes**: Color-coded regions with boundary lines

**Interactions**:
- **Hover**: Tooltip shows climate stats, habitability, modification options
- **Click**: Open climate modification panel (Godgame: miracles, Space4X: terraforming)
- **Drag**: Preview climate change effect radius
- **Ctrl+click**: Compare climate between tiles

**Tooltip Example**:
```
Plains Biome (Temperate)
━━━━━━━━━━━━━━━━━━━━━━━━━━━
Temperature:  18°C (Spring: 12°C, Summer: 24°C)
Moisture:     0.65 (Humid)
Wind:         NW, 8 km/h
Habitability: 0.85 (Excellent)

🌱 Suitable Crops: Wheat, Corn, Vegetables
🏠 Settlement Bonus: +20% food production

⚡ Change to Forest (500 mana, Growth miracle)
⚡ Change to Swamp (300 mana, Water miracle)
⚡ Increase Moisture (+150 mana)
```

---

### 3. Moisture Grid Overlay

Detailed water/moisture distribution and flow:

```csharp
public struct MoistureGridOverlayData : IComponentData
{
    public MoistureOverlayMode Mode;
    public bool ShowRivers;
    public bool ShowAquifers;
    public bool ShowEvaporation;
    public bool ShowIrrigation;
}

public enum MoistureOverlayMode : byte
{
    SurfaceMoisture = 0,    // Rainfall and surface water
    Groundwater = 1,        // Aquifer depth
    WaterFlow = 2,          // River/stream direction
    Evaporation = 3,        // Water loss rate
    IrrigationCoverage = 4, // Irrigated areas
    DroughtRisk = 5         // Areas at risk of drought
}

public struct MoistureGridTile : IBufferElementData
{
    public int2 GridCoord;

    public float SurfaceMoisture;       // 0.0 to 1.0
    public float GroundwaterDepth;      // Meters below surface
    public float GroundwaterAmount;     // Water volume
    public float2 FlowDirection;        // Water flow vector
    public float EvaporationRate;       // Units per second
    public bool IsIrrigated;
    public float DroughtRisk;           // 0.0 to 1.0

    // Visualization
    public float4 Color;
    public bool ShowFlowArrow;
}
```

**Visualization**:
- **Surface moisture**: Gradient from brown (dry) to blue (saturated)
- **Groundwater**: Depth visualization (shallow = bright, deep = dark)
- **Flow**: Animated blue arrows showing water movement
- **Drought risk**: Red overlay on at-risk areas

**Interactions**:
- **Hover**: Tooltip shows moisture levels, aquifer depth, irrigation status
- **Click**: Build irrigation, wells, or water management structures
- **Area select**: Preview irrigation coverage for planned farm

---

### 4. Entity Traffic Overlay

Shows entity movement patterns, congestion, and pathfinding:

```csharp
public struct EntityTrafficOverlayData : IComponentData
{
    public TrafficOverlayMode Mode;
    public EntityTypeFilter Filter;     // Which entities to show
    public bool ShowPathingLines;
    public bool ShowCongestionHotspots;
    public bool ShowMovementVectors;
    public uint TimeWindowTicks;        // How far back to track
}

public enum TrafficOverlayMode : byte
{
    AllMovement = 0,        // All entity movement
    Congestion = 1,         // Traffic bottlenecks
    Pathfinding = 2,        // Active paths
    FrequentRoutes = 3,     // Commonly used routes
    IdleZones = 4,          // Areas with no movement
    Speed = 5               // Movement speed heat map
}

public enum EntityTypeFilter : byte
{
    All = 0,
    Villagers = 1,          // Godgame
    Military = 2,
    Traders = 3,
    Ships = 4,              // Space4X
    Carriers = 5
}

/// <summary>
/// Traffic density per tile.
/// </summary>
public struct TrafficDensityTile : IBufferElementData
{
    public int2 GridCoord;

    public uint EntitiesPassedLast60Ticks;
    public uint CurrentEntitiesOnTile;
    public float AverageSpeed;          // Units per second
    public float CongestionLevel;       // 0.0 to 1.0
    public bool IsBottleneck;

    // Movement vectors (average)
    public float2 DominantDirection;
    public float DirectionStrength;     // 0.0 to 1.0 (chaotic vs uniform)

    // Visualization
    public float4 Color;                // Heat map color
    public bool ShowArrow;
}

/// <summary>
/// Individual entity trail for detailed visualization.
/// </summary>
[InternalBufferCapacity(32)]
public struct EntityTrailPoint : IBufferElementData
{
    public float3 Position;
    public uint Tick;
    public float Speed;
    public LocomotionMode Mode;
}
```

**Visualization**:
- **Heat map**: Blue (low traffic) → Yellow (medium) → Red (high congestion)
- **Movement trails**: Colored lines showing entity paths (fades over time)
- **Arrows**: Show dominant movement direction per tile
- **Congestion icons**: Warning icons on bottlenecks

**Interactions**:
- **Hover**: Tooltip shows traffic density, average speed, congestion level
- **Click entity**: Highlight that entity's trail
- **Area select**: Show all paths through area
- **Right-click bottleneck**: Suggest solutions (build road, widen path, etc.)

**Tooltip Example**:
```
High Traffic Zone
━━━━━━━━━━━━━━━━━━━━━━━━━━━
Entities/minute: 145
Avg Speed:        2.3 m/s (slow)
Congestion:       0.78 (High)

⚠️ Bottleneck detected!
   Entities spend 12s waiting here

💡 Build Road (+50% speed)
💡 Widen Path (reduce congestion 40%)
💡 Create Alternate Route
```

---

### 5. Aggregate Entity Overlay

Shows faction/group positions as aggregated visualizations:

```csharp
public struct AggregateEntityOverlayData : IComponentData
{
    public AggregateMode Mode;
    public bool ShowIndividualEntities;
    public bool ShowGroupCentroids;
    public bool ShowTerritoryBoundaries;
    public float MinGroupSize;          // Min entities to show aggregate
}

public enum AggregateMode : byte
{
    Factions = 0,           // Group by faction
    Behavior = 1,           // Group by current behavior
    EntityType = 2,         // Group by type (villager, warrior, etc.)
    Strongholds = 3,        // Group by settlement
    Squads = 4,             // Military units
    Fleets = 5              // Space4X ships
}

/// <summary>
/// Aggregate visualization for a group of entities.
/// </summary>
public struct AggregateEntityGroup : IBufferElementData
{
    public FixedString32Bytes GroupId;
    public AggregateGroupType Type;

    // Spatial data
    public float3 Centroid;             // Average position
    public float Radius;                // Spread from centroid
    public AABB BoundingBox;

    // Composition
    public uint TotalEntities;
    public uint VillagerCount;
    public uint WarriorCount;
    public uint BuildingCount;

    // Status
    public float AverageHealth;
    public float AverageMorale;
    public MoraleChange DominantMorale;
    public bool IsInCombat;
    public bool IsIdle;

    // Visualization
    public float4 Color;                // Faction color
    public IconType Icon;               // Group icon
    public bool IsSelected;
}

public enum AggregateGroupType : byte
{
    Faction = 0,
    Village = 1,
    Army = 2,
    TradingCaravan = 3,
    Fleet = 4,              // Space4X
    Colony = 5              // Space4X
}

public enum IconType : byte
{
    Village = 0,
    Fortress = 1,
    Army = 2,
    Navy = 3,
    Caravan = 4,
    Fleet = 5,
    Colony = 6,
    Stronghold = 7
}
```

**Visualization**:
- **Centroid marker**: Icon at average position (sized by entity count)
- **Spread circle**: Translucent circle showing group radius
- **Territory boundaries**: Convex hull around group
- **Health bar**: Above centroid showing average health
- **Morale indicator**: Color-coded ring around icon

**Interactions**:
- **Click centroid**: Select all entities in group
- **Hover**: Tooltip shows group composition, status, orders
- **Drag**: Multi-select groups
- **Double-click**: Zoom to group and show individual entities
- **Right-click**: Issue group orders

**Tooltip Example**:
```
Northern Army (Faction: Player)
━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 Composition:
   Warriors:   120 (avg health: 85%)
   Archers:     40 (avg health: 92%)
   Cavalry:     25 (avg health: 78%)

🎭 Morale: Rallied (+20% effectiveness)
⚔️ Status: Engaging enemy
📍 Location: Northern Plains

🗡️ Attack Nearby Enemy
🛡️ Defensive Formation
🏃 Retreat to Stronghold
```

---

### 6. Pollution Overlay (Future)

Environmental degradation tracking:

```csharp
public struct PollutionOverlayData : IComponentData
{
    public PollutionOverlayMode Mode;
    public bool ShowPollutionSources;
    public bool ShowSpreadVectors;
    public bool ShowCleanupZones;
}

public enum PollutionOverlayMode : byte
{
    TotalPollution = 0,     // All pollution types
    Air = 1,                // Atmospheric
    Water = 2,              // Rivers/groundwater
    Soil = 3,               // Land contamination
    Toxic = 4,              // Hazardous waste
    Spread = 5,             // Pollution propagation
    Cleanup = 6             // Cleanup efforts
}

public struct PollutionTile : IBufferElementData
{
    public int2 GridCoord;

    public float AirPollution;          // 0.0 to 1.0
    public float WaterPollution;
    public float SoilPollution;
    public float ToxicLevel;            // Hazardous threshold

    public float2 SpreadVector;         // Wind/water carries pollution
    public float SpreadRate;

    // Effects
    public float HealthPenalty;         // Villager health reduction
    public float CropYieldPenalty;      // Agriculture impact
    public float HabitabilityPenalty;

    // Cleanup
    public bool HasCleanupStructure;
    public float CleanupRate;           // Units per second
    public float TimeToClean;           // Seconds to threshold
}
```

**Visualization**:
- **Pollution heat map**: Green (clean) → Yellow (moderate) → Red (severe) → Black (toxic)
- **Source markers**: Factory icons at pollution sources
- **Spread vectors**: Animated smoke/water flow showing propagation
- **Cleanup zones**: Blue rings around cleanup structures

**Interactions**:
- **Hover**: Tooltip shows pollution levels, health effects, cleanup options
- **Click source**: See pollution output, shutdown options
- **Click tile**: Build cleanup structure, miracle to purify
- **Area select**: Mass cleanup operation

---

## Cross-Overlay Features

### Multi-Layer View

Combine multiple overlays:

```csharp
public struct OverlayComposition : IComponentData
{
    public FixedList32Bytes<OverlayType> ActiveLayers;
    public float[] OpacityPerLayer;     // Blend multiple overlays
    public OverlayBlendMode BlendMode;
}

public enum OverlayType : byte
{
    None = 0,
    Resources = 1,
    Climate = 2,
    Moisture = 3,
    Traffic = 4,
    Aggregates = 5,
    Pollution = 6,
    Biomes = 7,
    Territory = 8,
    Influence = 9,          // Divine influence (Godgame)
    SectorControl = 10      // Space4X control zones
}

public enum OverlayBlendMode : byte
{
    Replace = 0,            // Only show top layer
    Additive = 1,           // Add colors
    Multiply = 2,           // Multiply colors
    Overlay = 3,            // Photoshop-style overlay
    Alpha = 4               // Transparency blend
}
```

**Example Combinations**:
- **Resources + Climate**: See which biomes have which resources
- **Traffic + Aggregates**: Understand army movement patterns
- **Pollution + Moisture**: Track water contamination spread
- **Climate + Territory**: Plan settlement placement

---

## Performance Architecture

```csharp
/// <summary>
/// Burst-compiled overlay data generation.
/// </summary>
[BurstCompile]
public partial struct OverlayDataGenerationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var overlayConfig = SystemAPI.GetSingleton<OverlayComposition>();

        // Generate data for active overlays only
        foreach (var layer in overlayConfig.ActiveLayers)
        {
            switch (layer)
            {
                case OverlayType.Resources:
                    GenerateResourceOverlay(ref state);
                    break;
                case OverlayType.Climate:
                    GenerateClimateOverlay(ref state);
                    break;
                case OverlayType.Traffic:
                    GenerateTrafficOverlay(ref state);
                    break;
                // ... etc
            }
        }
    }

    [BurstCompile]
    private void GenerateResourceOverlay(ref SystemState state)
    {
        // Spatial query for resources
        var spatialGrid = SystemAPI.GetSingleton<SpatialPartitioningGrid>();
        var resourceBuffer = SystemAPI.GetBuffer<ResourceVisualizationTile>(overlayEntity);

        resourceBuffer.Clear();

        // Burst-compiled: iterate all resource deposits
        foreach (var (resource, transform) in SystemAPI.Query<
            RefRO<ResourceDeposit>,
            RefRO<LocalTransform>>())
        {
            var gridCoord = WorldToGrid(transform.ValueRO.Position);

            resourceBuffer.Add(new ResourceVisualizationTile
            {
                GridCoord = gridCoord,
                Type = resource.ValueRO.Type,
                Amount = resource.ValueRO.Amount,
                Quality = resource.ValueRO.Quality,
                Rarity = CalculateRarity(resource.ValueRO.Type, resource.ValueRO.Amount),
                Color = GetResourceColor(resource.ValueRO.Type, resource.ValueRO.Quality)
            });
        }
    }
}

/// <summary>
/// GPU-instanced overlay rendering (managed side).
/// </summary>
public class OverlayRenderingSystem : SystemBase
{
    private Material _overlayMaterial;
    private ComputeBuffer _tileDataBuffer;

    protected override void OnUpdate()
    {
        // Get overlay tile data from ECS
        var resourceTiles = GetBuffer<ResourceVisualizationTile>(overlayEntity);

        // Upload to GPU
        UpdateGPUBuffer(resourceTiles);

        // Render with GPU instancing
        Graphics.DrawMeshInstancedProcedural(
            _tileMesh,
            0,
            _overlayMaterial,
            bounds,
            resourceTiles.Length
        );
    }
}
```

**Performance Targets**:
```
Overlay Data Generation:  <2ms per overlay (10,000 tiles)
GPU Upload:               <1ms (instanced rendering)
Tooltip Query:            <0.1ms (spatial hash lookup)
Selection:                <0.5ms (spatial query)
────────────────────────────────────────
Total Frame Cost:         <5ms (3 overlays active)
```

---

## Interaction System

```csharp
/// <summary>
/// Handles user interactions with overlays.
/// </summary>
public struct OverlayInteraction : IComponentData
{
    public OverlayType ActiveOverlay;
    public int2 HoveredTile;
    public int2 SelectedTile;
    public AABB SelectionBox;           // For area select
    public bool IsShowingTooltip;
    public Entity TooltipEntity;
}

/// <summary>
/// Tooltip data for overlay tiles.
/// </summary>
public struct OverlayTooltip : IComponentData
{
    public int2 TileCoord;
    public OverlayType SourceOverlay;
    public FixedString512Bytes Title;
    public FixedString512Bytes Content;
    public FixedList64Bytes<TooltipAction> Actions;
}

public struct TooltipAction
{
    public FixedString64Bytes Label;    // "Build Gold Mine"
    public FixedString64Bytes IconId;
    public ActionType Type;
    public float Cost;                   // Resource cost
    public bool IsAffordable;
}

public enum ActionType : byte
{
    BuildStructure = 0,
    CastMiracle = 1,        // Godgame
    TerraformPlanet = 2,    // Space4X
    AssignWorkers = 3,
    IssueCommand = 4,
    SetRoute = 5
}
```

---

## Godgame vs Space4X Variants

### Godgame Overlays
- **Terrain-based**: 2D heightmap grid
- **Miracles integration**: Climate modification via divine powers
- **Villager traffic**: Ground-based movement patterns
- **Biomes**: Forest, desert, tundra, swamp, etc.
- **Resource types**: Food, wood, stone, gold, gems
- **Divine influence overlay**: Show which god controls which area

### Space4X Overlays
- **Sector-based**: 3D space sectors
- **Terraforming integration**: Climate modification via tech
- **Fleet traffic**: 3D space movement with orbital patterns
- **Planet types**: Terran, barren, gas giant, etc.
- **Resource types**: Minerals, energy, exotic matter
- **Control zones overlay**: Faction territory in space

---

## Accessibility Features

```csharp
public struct OverlayAccessibilityConfig : IComponentData
{
    public ColorBlindMode ColorMode;
    public bool UsePatterns;            // In addition to colors
    public bool UseTextLabels;
    public float IconScale;             // 0.5 to 2.0
    public bool HighContrast;
}

public enum ColorBlindMode : byte
{
    Normal = 0,
    Protanopia = 1,         // Red-blind
    Deuteranopia = 2,       // Green-blind
    Tritanopia = 3,         // Blue-blind
    Monochromacy = 4        // Total color blindness
}
```

**Pattern Overlays**:
- Resources: Dots, diagonal lines, cross-hatch
- Climate: Horizontal lines (cold), vertical lines (hot)
- Pollution: Wavy lines for contamination

---

## Summary

**Overlay Types** (8 core + 2 future):
1. **Resources**: Distribution, quality, rarity, depletion
2. **Climate**: Temperature, moisture, wind, biomes
3. **Moisture Grid**: Rainfall, aquifers, flow, irrigation
4. **Entity Traffic**: Movement patterns, congestion, bottlenecks
5. **Aggregate Entities**: Faction positions, group status
6. **Pollution** (future): Environmental degradation
7. **Territory**: Faction control zones
8. **Influence**: Divine/tech influence zones

**Interaction Features**:
- Click-to-select (tiles, deposits, groups)
- Hover tooltips (contextual information)
- Action buttons (build, cast, assign, command)
- Area selection (multi-select, mass operations)
- Comparison mode (ctrl+click multiple tiles)
- Zoom-to-detail (double-click aggregates)

**Performance**:
- Burst-compiled data generation (<2ms per overlay)
- GPU instanced rendering (<1ms upload)
- Spatial hash queries (<0.1ms tooltip lookup)
- Supports 10,000+ tiles with 3 overlays active (<5ms total)

**Cross-Game Support**:
- Godgame: 2D terrain, miracles, villagers, divine influence
- Space4X: 3D sectors, terraforming, fleets, control zones

**Accessibility**:
- 4 colorblind modes
- Pattern overlays
- Text labels
- Scalable icons
- High contrast mode
