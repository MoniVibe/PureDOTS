# Implementation Notes — Environment Systems

**Project:** Godgame  
**Doc Path:** `Docs/Concepts/World/Environment_Systems.md`  
**Phase:** Tier-1 Complete, v3.0+ Extensions  
**Spine:** DOTS 1.4 + PureDOTS time/rewind  
**Related Systems:** Biomes, Vegetation, Climate, Moisture, Weather, Villager stamina/disease modifiers

---

## Prefab vs Data Matrix

| Thing | Prefab? | Why | Token Sockets? | Binding IDs | Notes |
|-------|---------|-----|----------------|-------------|-------|
| Biome Profile | NO | Data-driven classification with ranges/weights | N/A | BiomeId | Blob asset from BiomeCatalog SO |
| Biome Visual Token | YES | Thin presentation token (ground texture, sky color) | N/A | BiomeId | Optional, swappable Minimal/Fancy sets |
| Climate State | NO | Singleton simulation state | N/A | N/A | Global or per-biome, deterministic |
| Moisture Grid | NO | Spatial simulation data | N/A | N/A | Blob grid, reuses SpatialGridConfig |
| Moisture Cell | NO | Grid cell data | N/A | N/A | Part of MoistureGrid blob |
| Weather State | NO | Singleton simulation state | N/A | WeatherTypeId | Deterministic transitions |
| Weather Visual FX | YES | Thin presentation token (rain particles, storm clouds) | N/A | WeatherTypeId | Optional, swappable |
| Vegetation Spec | NO | Species definition (growth, yields, hazards) | N/A | PlantId | Blob asset from VegetationCatalog SO |
| Vegetation Entity | NO | Runtime simulation entity | N/A | PlantId | ECS entity with lifecycle components |
| Vegetation Visual Token | YES | Thin presentation token per species×stage | N/A | PlantId + GrowthStage | Optional, swappable Minimal/Fancy sets |
| Stand Spec | NO | Clustering/spawn configuration | N/A | StandId | Blob asset, deterministic spawning |
| Stand Visual Token | YES | Aggregate visual (tree cluster, patch) | N/A | StandId | Optional, for dense patches |

**Decision Rationale:**
- All simulation truth is data (blobs/components). Visuals are optional presentation tokens.
- Biome/vegetation visuals are swappable bindings, never gameplay truth.
- Moisture grid reuses existing SpatialGridConfig for consistency.

---

## Schemas

### ECS Components (Runtime)

```csharp
// Biome Resolution
public struct BiomeId : IComponentData { public FixedString32Bytes Value; }
public struct BiomeSpecRef : IComponentData { public BlobAssetReference<BiomeSpec> Blob; }
public struct BiomeResolved : IComponentData { public byte BiomeType; public float Score; }

// Climate State (Singleton)
public struct ClimateState : IComponentData 
{ 
    public float Temperature; // -50 to +50°C
    public float Humidity; // 0-100%
    public byte Season; // 0=Spring, 1=Summer, 2=Fall, 3=Winter
    public uint TicksIntoSeason;
    public uint SeasonLengthTicks;
}

// Moisture Grid (Singleton)
public struct MoistureGrid : IComponentData 
{ 
    public int GridWidth, GridHeight;
    public float CellSize; // Meters per cell
    public BlobAssetReference<MoistureCellBlob> Cells;
}

// Weather State (Singleton)
public struct WeatherState : IComponentData 
{ 
    public byte CurrentWeather; // Clear, Rain, Storm, Drought
    public uint DurationRemaining; // Ticks until change
    public float Intensity; // 0-1
}

// Vegetation (per entity, already exists)
// See PureDOTS.Runtime.Components.VegetationComponents.cs
// VegetationId, VegetationLifecycle, VegetationHealth, VegetationProduction, etc.
```

### Blob Assets

```csharp
// Biome Spec Blob
public struct BiomeSpec 
{ 
    public FixedString32Bytes Id;
    public float TemperatureMin, TemperatureMax;
    public float HumidityMin, HumidityMax;
    public BlobArray<BiomeWeight> Weights; // For resolution scoring
    public FixedString32Bytes StyleToken; // Visual binding ID
}

public struct BiomeWeight 
{ 
    public float TemperatureWeight;
    public float HumidityWeight;
    public float MoistureWeight;
}

// Moisture Cell Blob
public struct MoistureCellBlob 
{ 
    public BlobArray<MoistureCell> Cells;
}

public struct MoistureCell 
{ 
    public float MoistureLevel; // 0-1
    public float DrainageRate;
    public float AbsorptionRate;
}
```

### Authoring ScriptableObjects

```csharp
[CreateAssetMenu]
public class BiomeCatalog : ScriptableObject 
{ 
    public List<BiomeEntryAuthoring> Biomes;
}

[Serializable]
public struct BiomeEntryAuthoring 
{ 
    public string id;
    public float temperatureMin, temperatureMax;
    public float humidityMin, humidityMax;
    public float temperatureWeight, humidityWeight, moistureWeight;
    public string styleToken;
}

[CreateAssetMenu]
public class MoistureGridConfig : ScriptableObject 
{ 
    public int gridWidth = 100;
    public int gridHeight = 100;
    public float cellSize = 1f; // Meters
    public float defaultMoisture = 0.5f;
    public float defaultDrainageRate = 0.01f;
    public float defaultAbsorptionRate = 0.1f;
}
```

### Baker Mapping

```csharp
public sealed class BiomeCatalogBaker : Baker<BiomeCatalog> 
{
    public override void Bake(BiomeCatalog src) 
    {
        using var bb = new BlobBuilder(Allocator.Temp);
        ref var root = ref bb.ConstructRoot<BiomeSpec>();
        
        var arr = bb.Allocate(ref root.Entries, src.biomes.Count);
        for (int i = 0; i < src.biomes.Count; i++) 
        {
            arr[i] = new BiomeEntry 
            { 
                Id = src.biomes[i].id,
                TemperatureMin = src.biomes[i].temperatureMin,
                TemperatureMax = src.biomes[i].temperatureMax,
                HumidityMin = src.biomes[i].humidityMin,
                HumidityMax = src.biomes[i].humidityMax,
                TemperatureWeight = src.biomes[i].temperatureWeight,
                HumidityWeight = src.biomes[i].humidityWeight,
                MoistureWeight = src.biomes[i].moistureWeight,
                StyleToken = src.biomes[i].styleToken
            };
        }
        
        var blob = bb.CreateBlobAssetReference<BiomeSpec>(Allocator.Persistent);
        var e = GetEntity(TransformUsageFlags.None);
        AddComponent(e, new BiomeSpecRef { Blob = blob });
    }
}

public sealed class MoistureGridBaker : Baker<MoistureGridConfig> 
{
    public override void Bake(MoistureGridConfig src) 
    {
        using var bb = new BlobBuilder(Allocator.Temp);
        ref var root = ref bb.ConstructRoot<MoistureCellBlob>();
        
        int cellCount = src.gridWidth * src.gridHeight;
        var cells = bb.Allocate(ref root.Cells, cellCount);
        for (int i = 0; i < cellCount; i++) 
        {
            cells[i] = new MoistureCell 
            { 
                MoistureLevel = src.defaultMoisture,
                DrainageRate = src.defaultDrainageRate,
                AbsorptionRate = src.defaultAbsorptionRate
            };
        }
        
        var blob = bb.CreateBlobAssetReference<MoistureCellBlob>(Allocator.Persistent);
        var e = GetEntity(TransformUsageFlags.None);
        AddComponent(e, new MoistureGrid 
        { 
            GridWidth = src.gridWidth,
            GridHeight = src.gridHeight,
            CellSize = src.cellSize,
            Cells = blob
        });
    }
}
```

---

## Systems & Ordering

### System Groups

**Initialization:**
- `BiomeCatalogInitializationSystem` - Creates singleton BiomeSpecRef from catalog
- `MoistureGridInitializationSystem` - Creates singleton MoistureGrid from config
- `ClimateStateInitializationSystem` - Creates singleton ClimateState with defaults
- `WeatherStateInitializationSystem` - Creates singleton WeatherState (Clear)

**FixedStep Simulation:**
- `ClimateOscillationSystem` - Updates temperature/humidity over time (seasonal cycles)
- `MoistureGridUpdateSystem` - Processes moisture sources/sinks per cell
- `WeatherTransitionSystem` - Handles weather state changes (natural + miracle triggers)
- `BiomeResolveSystem` - Resolves best-match biome per spatial cell (if multi-biome)
- `VegetationGrowthSystem` - Already exists, reads moisture/climate for growth factors
- `VegetationHealthSystem` - Already exists, applies drought/stress from moisture

**Presentation:**
- `BiomeVisualBindingSystem` - Spawns/destroys biome visual tokens based on resolved biome
- `WeatherFXPlaybackSystem` - Plays rain/storm visual FX based on WeatherState
- `VegetationVisualBindingSystem` - Already exists, binds vegetation visuals

### Code Sketches

```csharp
[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MoistureGridUpdateSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        var moistureGrid = SystemAPI.GetSingleton<MoistureGrid>();
        var weatherState = SystemAPI.GetSingleton<WeatherState>();
        var climateState = SystemAPI.GetSingleton<ClimateState>();
        
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();
        
        new UpdateMoistureJob 
        { 
            Grid = moistureGrid,
            Weather = weatherState,
            Climate = climateState,
            Dt = SystemAPI.Time.DeltaTime
        }.ScheduleParallel(moistureGrid.GridWidth * moistureGrid.GridHeight, 64);
    }
    
    [BurstCompile]
    public partial struct UpdateMoistureJob : IJobParallelFor 
    {
        public MoistureGrid Grid;
        public WeatherState Weather;
        public ClimateState Climate;
        public float Dt;
        
        void Execute(int index) 
        {
            ref var cell = ref Grid.Cells.Value.Cells[index];
            
            // Sources
            if (Weather.CurrentWeather == WeatherType.Rain) 
                cell.MoistureLevel += Weather.Intensity * 0.3f * Dt;
            
            // Sinks
            float evaporation = cell.DrainageRate * Climate.Temperature * 0.01f * Dt;
            cell.MoistureLevel -= evaporation;
            
            // Clamp
            cell.MoistureLevel = math.clamp(cell.MoistureLevel, 0f, 1f);
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct BiomeResolveSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        var biomeSpec = SystemAPI.GetSingleton<BiomeSpecRef>();
        var climateState = SystemAPI.GetSingleton<ClimateState>();
        var moistureGrid = SystemAPI.GetSingleton<MoistureGrid>();
        
        // Resolve biome for each spatial cell (or global if 1×1 grid)
        // Score each biome, pick best match
        // Store in BiomeResolved component per cell entity
    }
}
```

**ECB Boundaries:**
- Structural changes (spawning/destroying vegetation, visual tokens) only at Begin/End Presentation via ECB.
- Simulation systems mutate components directly (moisture levels, climate state).

---

## Prefab Maker Tasks

### What to Generate

**Biome Visual Tokens (Optional):**
- Generate placeholder prefabs per biome ID in `Assets/Prefabs/Godgame/Biomes/`
- Components: `BiomeIdAuthoring`, `StyleTokensAuthoring`, placeholder visual child
- Visual: Ground plane with biome-specific color/material

**Weather Visual FX Tokens (Optional):**
- Generate placeholder prefabs per weather type in `Assets/Prefabs/Godgame/Weather/`
- Components: `WeatherTypeIdAuthoring`, `StyleTokensAuthoring`
- Visual: Particle system placeholder (rain, storm clouds)

**Vegetation Visual Tokens (Already Handled):**
- Prefab Maker Vegetation tab already generates per species×stage tokens
- No changes needed

### Validation

**Catalog Validation:**
- All biome entries have valid temperature/humidity ranges (min < max)
- All biome entries have non-zero weights
- Moisture grid config has positive dimensions and cell size

**Binding Validation:**
- All biome IDs have corresponding visual tokens (if visuals enabled)
- All weather types have corresponding FX tokens (if visuals enabled)

**Idempotency:**
- Hash report: Catalog SO hash → blob hash → prefab hash
- CLI dry-run: `--validate-only` flag checks all catalogs without generating
- Re-run produces identical outputs (deterministic blob building)

---

## Determinism & Tests

### Test Names & Descriptions

1. **Biomes_ResolveBestMatch_EditMode** - Given climate/moisture inputs, resolves correct biome with deterministic scoring
2. **MoistureGrid_UpdateDeterministic_EditMode** - Seeded moisture updates produce identical results across runs
3. **Climate_OscillationDeterministic_EditMode** - Seasonal cycles produce identical temperature/humidity curves
4. **Weather_TransitionDeterministic_EditMode** - Seeded weather transitions follow deterministic state machine
5. **Vegetation_MoistureIntegration_PlayMode** - Vegetation growth respects moisture grid values (integration test)
6. **Rewind_MoistureGridState_PlayMode** - Moisture grid state rewinds correctly (snapshot/restore)

**Rewind Check:**
- MoistureGrid, ClimateState, WeatherState are singletons → snapshot via `ISnapshotable<T>` or manual blob copy
- Vegetation entities already support rewind via PureDOTS time spine

---

## CI & Budgets

### Metrics to Export

**Per Scenario:**
- Fixed tick ms: MoistureGridUpdateSystem, BiomeResolveSystem
- Entity count: Vegetation entities, visual token entities
- Memory: MoistureGrid blob size, BiomeSpec blob size
- Snapshot size: ClimateState + MoistureGrid + WeatherState serialized size

### Budgets

- **MoistureGridUpdateSystem**: < 1ms per 10,000 cells (100×100 grid)
- **BiomeResolveSystem**: < 0.5ms per resolution (runs once per tick, not per cell if global)
- **Vegetation entity count**: < 100,000 active vegetation entities
- **MoistureGrid blob**: < 1MB for 100×100 grid (400 bytes per cell)

### Gates

- MoistureGridUpdateSystem exceeds 2ms → fail CI
- Snapshot size exceeds 10MB → warn
- Vegetation entity count exceeds 200,000 → warn

---

## Risks & "Why Invalid" Checks

### Lint Rules

1. **Biome Range Validation:**
   - Rule: `temperatureMin < temperatureMax`, `humidityMin < humidityMax`
   - Message: "Biome '{id}' has invalid range: {min} >= {max}"

2. **Moisture Grid Bounds:**
   - Rule: `gridWidth > 0`, `gridHeight > 0`, `cellSize > 0`
   - Message: "MoistureGridConfig has invalid dimensions: {width}×{height} @ {cellSize}m"

3. **Weather State Consistency:**
   - Rule: `durationRemaining > 0` when weather != Clear
   - Message: "WeatherState has non-Clear weather with zero duration"

4. **Biome Spec Blob Missing:**
   - Rule: BiomeResolveSystem requires BiomeSpecRef singleton
   - Message: "BiomeResolveSystem requires BiomeSpecRef singleton (add BiomeCatalog to scene)"

5. **Moisture Grid Missing:**
   - Rule: MoistureGridUpdateSystem requires MoistureGrid singleton
   - Message: "MoistureGridUpdateSystem requires MoistureGrid singleton (add MoistureGridConfig to scene)"

6. **Vegetation Integration:**
   - Rule: VegetationGrowthSystem reads moisture but MoistureGrid missing
   - Message: "VegetationGrowthSystem requires MoistureGrid singleton for moisture-dependent growth"

### Design Risks

**Risk: Moisture Grid Performance**
- **Mitigation:** Use coarse grid (10m cells) for MVP, fine grid (1m) only if needed
- **Fallback:** Global moisture (1×1 grid) if performance issues

**Risk: Biome Resolution Complexity**
- **Mitigation:** Start with global biome (1×1), add spatial resolution later
- **Fallback:** Single biome (temperate) if multi-biome too complex

**Risk: Weather State Determinism**
- **Mitigation:** Use seeded RNG for weather transitions, store seed in snapshot
- **Fallback:** Deterministic weather schedule (no randomness)

---

## TODO(Design) Items

1. **Seasonal Cycles:** Are seasons in scope? Default: No seasons (always summer) for MVP.
2. **Spatial Biome Resolution:** Global (1×1) or spatial grid? Default: Global for MVP.
3. **Moisture Grid Resolution:** Coarse (10m) or fine (1m)? Default: Coarse (10m) for MVP.
4. **Weather Triggers:** Natural cycles or player-only? Default: Player-only (miracles) for MVP.
5. **Wind System:** Visual only or gameplay impact? Default: Skip for MVP.

---

**End of Implementation Notes — Environment Systems**

