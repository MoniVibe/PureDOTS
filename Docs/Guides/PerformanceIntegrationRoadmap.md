# Performance Integration Roadmap

This roadmap guides the integration of PureDOTS performance infrastructure (LOD, aggregates, density, physics contracts) into existing archetypes and game projects.

**Target**: Make it easy for Space4X and Godgame agents to migrate components, hook up rendering, and reason about performance at 10k / 100k / 1M+ entities.

---

## Phase 1: Sanity Check & "Hello LOD/Aggregates" Scenarios

### Overview

Prove that the new LOD / aggregate / density systems work end-to-end in small, controlled test scenarios before applying to real archetypes.

**Status**: ✅ Implemented and wired

**Infrastructure**:
- `ScenarioTestEntitySpawnerSystem.cs` - Reads `ScenarioEntityCountElement` and spawns test entities
- `ScaleTestSpawnerHelpers.cs` - Reusable helpers for LOD/aggregate component setup
- `ScenarioRunnerEntryPoints.cs` - CLI entry points with debug flag support

### How to Launch Mini LOD/Aggregate Demos

#### Via CLI (Batch Mode)

```bash
# Mini LOD legacy (2k test entities with LOD components)
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_mini_lod_demo \
  --metrics CI/Reports/lod_demo.json \
  --enable-lod-debug

# Mini Aggregate legacy (5 aggregates with 200 members)
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_mini_aggregate_demo \
  --metrics CI/Reports/aggregate_demo.json \
  --enable-aggregate-debug
```

#### Via Editor

1. Open Unity project
2. Use Console to run:
   ```csharp
   // List available scenarios
   PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.ListScaleScenarios();
   
   // Run LOD legacy (will use default args)
   // Or use ScenarioRunnerExecutor.RunFromFile("path/to/scale_mini_lod_demo.json")
   ```

#### Debug Output

With `--enable-lod-debug`:
```
[LODDebug] Tick 50: LOD0=1200, LOD1=500, LOD2=250, LOD3=50 | ShouldRender=1800, AvgDistance=45.2, AvgImportance=0.52
```

With `--enable-aggregate-debug`:
```
[AggregateDebug] Tick 100: Aggregates=5, TotalMembers=200, AvgMembers=40, Range=[35, 45] | AvgHealth=75.3, AvgStrength=55.2
```

### Mini LOD Scenario

**File**: `scale_mini_lod_demo.json`

**What it spawns**:
- 2,000 generic test entities with:
  - `RenderLODData` (distance/importance tracking)
  - `RenderCullable` (culling configuration)
  - `RenderSampleIndex` (density control)
  - `LocalTransform` (position for distance calculation)

**What to look for**:
1. **LOD Level Changes**: As entities move or camera distance changes, `RenderLODData.RecommendedLOD` updates correctly
2. **Density Control**: When `RenderDensitySettings.CurrentDensity` changes, `RenderSampleIndex.ShouldRender` updates accordingly
3. **Culling**: Entities beyond `RenderCullable.CullDistance` are marked for culling

**Configs to tweak**:
- `LODThresholds`: Adjust `LOD1Distance`, `LOD2Distance`, `LOD3Distance` to see LOD transitions
- `RenderDensitySettings.CurrentDensity`: Change from 1 (all) to 10 (1 in 10) to see density filtering
- `RenderCullable.CullDistance`: Set to different values to test culling behavior

**Debug Output**:
- Count of entities per LOD level (0=full, 1=reduced, 2=impostor, 3=hidden)
- Count of entities with `ShouldRender=1` vs `ShouldRender=0`
- Average `CameraDistance` and `ImportanceScore`

**To Enable Debug Logging**:
```csharp
// In scenario setup or bootstrap
var configEntity = EntityManager.CreateEntity();
EntityManager.AddComponentData(configEntity, new ScaleTestMetricsConfig
{
    SampleInterval = 10,
    LogInterval = 50,
    CollectSystemTimings = 1,
    CollectMemoryStats = 0,
    EnableLODDebug = 1,  // Enable LOD debug
    EnableAggregateDebug = 0,
    TargetTickTimeMs = 16.67f,
    TargetMemoryMB = 128f
});
```

Debug metrics are collected by `LODDebugMetricsSystem` and logged at the configured interval.

### Mini Aggregate Scenario

**File**: `scale_mini_aggregate_demo.json`

**What it spawns**:
- 5 aggregate entities (villages/fleets) with:
  - `AggregateTag`
  - `AggregateState`
  - `AggregateRenderSummary`
  - `AggregateRenderConfig`
  - `DynamicBuffer<AggregateMemberElement>` (10-50 members each)

- 200 member entities with:
  - `AggregateMembership` (linking to aggregate)
  - `LocalTransform` (position)
  - Health/strength values

**What to look for**:
1. **Aggregate Updates**: `AggregateRenderSummarySystem` updates summaries every `AggregationInterval` ticks
2. **Member Tracking**: `AggregateMemberElement` buffer reflects current members
3. **Bounds Calculation**: `AggregateRenderSummary.BoundsCenter` and `BoundsRadius` update as members move
4. **Statistics**: `TotalHealth`, `AverageMorale`, `TotalStrength` aggregate correctly

**Configs to tweak**:
- `AggregateState.AggregationInterval`: Change update frequency (default: every N ticks)
- `AggregateRenderConfig.AggregateRenderDistance`: Distance threshold for switching to impostor
- `AggregateRenderConfig.MaxIndividualRender`: Maximum members to render individually

**Debug Output**:
- Aggregate count and member distribution
- Average `MemberCount` per aggregate
- `AggregateRenderSummary` values (bounds, health, strength)
- Update frequency statistics

**To Enable Debug Logging**:
```csharp
// In scenario setup or bootstrap
var configEntity = EntityManager.CreateEntity();
EntityManager.AddComponentData(configEntity, new ScaleTestMetricsConfig
{
    SampleInterval = 20,
    LogInterval = 100,
    CollectSystemTimings = 1,
    CollectMemoryStats = 1,
    EnableLODDebug = 0,
    EnableAggregateDebug = 1,  // Enable aggregate debug
    TargetTickTimeMs = 16.67f,
    TargetMemoryMB = 256f
});
```

Debug metrics are collected by `AggregateDebugMetricsSystem` and logged at the configured interval.

### Debug Logging

Debug metrics are controlled by `ScaleTestMetricsConfig`:
- `CollectSystemTimings`: Enable/disable timing collection
- `CollectMemoryStats`: Enable/disable memory tracking
- `LogInterval`: Ticks between log outputs

To enable debug logging, add `ScaleTestMetricsConfig` singleton with:
```csharp
CollectSystemTimings = 1,
CollectMemoryStats = 1,
LogInterval = 10  // Log every 10 ticks
```

---

## Phase 2: Prioritized Component Migration Plan (Real Archetypes)

### Selected High-Impact Archetypes

#### Godgame Domain

1. **Villager** (highest count, most player-visible)
2. **ResourceChunk** (high count, frequent spawn/destroy)
3. **Village** (aggregate, represents many villagers)

#### Space4X Domain

1. **Carrier/Craft** (player-visible, complex state)
2. **Asteroid** (with resources, static but numerous)
3. **Fleet** (aggregate, represents many carriers)

### Archetype Analysis & Migration Sketches

#### 1. Villager (Godgame)

**Current Hot Components**:
- `VillagerId` (8 bytes) ✅
- `VillagerNeedsHot` (20 bytes) ✅
- `VillagerMovement` (44 bytes) ✅
- `VillagerAIState` (40 bytes) ✅
- `VillagerJob` (16 bytes) ✅
- `VillagerFlags` (2 bytes) ✅
- `VillagerCombatStats` (16 bytes) ✅
- `VillagerAvailability` (16 bytes) ✅
- `LocalTransform` (32 bytes) ✅

**Current Cold Components** (move to companion):
- `VillagerBelief` (72 bytes) ❌ → Use `byte DeityIndex` + catalog
- `VillagerKnowledge` (136+ bytes) ❌ → Move to companion, update on events
- `VillagerColdData` (already on companion) ✅

**Target Layout**:
```
Main Entity (Hot):
- LocalTransform (32)
- VillagerId (8)
- VillagerNeedsHot (20)
- VillagerMovement (44)
- VillagerAIState (40)
- VillagerJob (16)
- VillagerFlags (2)
- VillagerCombatStats (16)
- VillagerAvailability (16)
- VillagerInventoryRef (4) → companion
- VillagerCompanionRef (4) → companion
- VillagerColdDataRef (4) → companion
- RenderLODData (16) → NEW
- RenderCullable (5) → NEW
- RenderSampleIndex (5) → NEW
- AggregateMembership (6) → NEW (if in village)
Total: ~232 bytes (slightly over, but acceptable)

Companion Entity (Cold):
- VillagerBeliefOptimized (10) → byte index instead of FixedString
- VillagerKnowledge (buffer) → update on events only
- VillagerColdData (existing)
- VillagerStats
- VillagerAnimationState
- VillagerMemoryEvent buffer
```

**LOD/Aggregate Attachment**:
- `RenderLODData` → On villager entity (updated by game camera system)
- `RenderCullable` → On villager entity (config per villager type)
- `RenderSampleIndex` → On villager entity (assigned at spawn)
- `AggregateMembership` → On villager entity (links to village)

#### 2. ResourceChunk (Godgame)

**Current Hot Components**:
- `LocalTransform` (32 bytes) ✅
- `ResourceChunkState` (36 bytes) ✅

**Target Layout**:
```
Main Entity (Hot):
- LocalTransform (32)
- ResourceChunkState (36)
- BallisticMotion (32) → NEW (if thrown)
- GroundCollisionCheck (16) → NEW (if thrown)
- RenderLODData (16) → NEW
- RenderCullable (5) → NEW
Total: ~137 bytes (acceptable)
```

**LOD Attachment**:
- `RenderLODData` → On chunk entity
- `RenderCullable` → On chunk entity (cull at distance)
- `BallisticMotion` → On chunk entity (if thrown, no physics)

#### 3. Village (Godgame Aggregate)

**Current Components**:
- Various village behavior components
- Registry entries

**Target Layout**:
```
Aggregate Entity:
- AggregateTag → NEW
- AggregateState (existing or new)
- AggregateRenderSummary → NEW
- AggregateRenderConfig → NEW
- DynamicBuffer<AggregateMemberElement> → NEW
- VillageBehaviorComponents (existing)
```

**Aggregate Attachment**:
- `AggregateTag` → On village entity
- `AggregateRenderSummary` → On village entity (updated by system)
- `AggregateState` → On village entity (tracks members)
- `AggregateMemberElement` buffer → On village entity (member references)

#### 4. Carrier/Craft (Space4X)

**Current Hot Components** (estimated):
- `LocalTransform` (32 bytes)
- Position/Velocity components
- Module states (buffers)
- Power budget
- Combat state

**Target Layout**:
```
Main Entity (Hot):
- LocalTransform (32)
- CarrierPosition/Velocity (~24)
- CarrierModuleRefs (4) → companion for module buffers
- CarrierPowerBudget (16)
- CarrierCombatState (16)
- RenderLODData (16) → NEW
- RenderCullable (5) → NEW
- RenderSampleIndex (5) → NEW
- AggregateMembership (6) → NEW (if in fleet)
Total: ~124 bytes (acceptable)

Companion Entity (Cold):
- CarrierModuleStates (buffer)
- CarrierConfig (cold data)
- CarrierStats
```

**LOD/Aggregate Attachment**:
- `RenderLODData` → On carrier entity
- `RenderCullable` → On carrier entity
- `RenderSampleIndex` → On carrier entity
- `AggregateMembership` → On carrier entity (links to fleet)

#### 5. Asteroid (Space4X)

**Current Components**:
- `LocalTransform` (32 bytes)
- Resource components
- Static position

**Target Layout**:
```
Main Entity:
- LocalTransform (32)
- AsteroidState (16)
- ResourceSourceState (existing)
- RenderLODData (16) → NEW
- RenderCullable (5) → NEW
Total: ~69 bytes (very efficient)
```

**LOD Attachment**:
- `RenderLODData` → On asteroid entity
- `RenderCullable` → On asteroid entity (cull far asteroids)

#### 6. Fleet (Space4X Aggregate)

**Target Layout**:
```
Aggregate Entity:
- AggregateTag → NEW
- AggregateState → NEW
- AggregateRenderSummary → NEW
- AggregateRenderConfig → NEW
- DynamicBuffer<AggregateMemberElement> → NEW
- FleetBehaviorComponents (existing)
```

**Aggregate Attachment**: Same as Village pattern above.

### Migration Ordering

**Phase 2A: Foundation** ✅ IMPLEMENTED
1. **Villager** + **ResourceChunk** (Godgame)
   - **Rationale**: Highest entity counts, most player-visible
   - **Impact**: Immediate performance gains at scale
   - **Risk**: Medium (core gameplay entities)
   - **Status**: 
     - `VillagerBeliefOptimized` created (6 bytes vs 72 bytes)
     - `VillagerLODComponents` with helper methods
     - `VillagerBeliefMigrationSystem` for transitional compatibility
     - `VillagerLODInitializationSystem` auto-adds LOD to villagers
     - `ResourceChunkLODHelpers` with ballistic motion support
     - `ResourceChunkBallisticSystem` for thrown object physics

**Phase 2B: Space4X Core** ✅ IMPLEMENTED
2. **Carrier/Craft** + **Asteroid** (Space4X)
   - **Rationale**: Player-visible, complex state
   - **Impact**: Enables Space4X scale testing
   - **Risk**: Medium (core gameplay entities)
   - **Status**:
     - `SpaceLODComponents.cs` created with fleet/carrier helpers
     - `FleetMemberRef`, `FleetTag`, `FleetState`, `FleetRenderSummary` components
     - `SpaceLODHelpers` for adding LOD to carriers/asteroids
     - `HarvesterAuthoring` updated with LOD support

**Phase 2C: Aggregates** ✅ IMPLEMENTED
3. **Fleet** + **Village** (Both games)
   - **Rationale**: Aggregates enable impostor rendering at extreme scale
   - **Impact**: Critical for 1M+ entity scenarios
   - **Risk**: Low (aggregates are less frequently accessed)
   - **Status**:
     - `VillageAggregateComponents.cs` with `VillageTag`, `VillageState`, `VillageRenderSummary`
     - `VillageAggregateHelpers` for creating villages and adding members
    - `CollectiveAggregateSystem.cs` updates aggregate summaries from member data
     - `FleetAggregateSystem.cs` updates fleet summaries from member data
     - `SpatialInteractionExampleSystem.cs` demonstrates grid-based interactions

**Justification**:
- Villagers and carriers are the most numerous and player-visible entities
- Resource chunks have high spawn/destroy rates (memory pressure)
- Aggregates come last because they depend on member entities being migrated first

---

## Phase 3: Integration Contracts for Space4X & Godgame

### Game-Facing LOD & Density Contract

#### Components to Read

For any entity the games want to render, read these components:

1. **`RenderLODData`** - Distance and importance scores
   ```csharp
   if (SystemAPI.HasComponent<RenderLODData>(entity))
   {
       var lod = SystemAPI.GetComponent<RenderLODData>(entity);
       var distance = lod.CameraDistance;
       var importance = lod.ImportanceScore;
       var recommendedLOD = lod.RecommendedLOD;
       
       // Use recommendedLOD to select rendering quality:
       // 0 = full detail mesh
       // 1 = reduced detail mesh
       // 2 = impostor sprite/billboard
       // 3 = hidden (don't render)
   }
   ```

2. **`RenderCullable`** - Culling configuration
   ```csharp
   if (SystemAPI.HasComponent<RenderCullable>(entity))
   {
       var cullable = SystemAPI.GetComponent<RenderCullable>(entity);
       if (cameraDistance > cullable.CullDistance)
       {
           // Skip rendering this entity
           return;
       }
   }
   ```

3. **`RenderSampleIndex`** - Density control
   ```csharp
   if (SystemAPI.HasComponent<RenderSampleIndex>(entity))
   {
       var sample = SystemAPI.GetComponent<RenderSampleIndex>(entity);
       if (sample.ShouldRender == 0)
       {
           // Skip rendering (density filtering)
           return;
       }
   }
   ```

#### Config Components to Tweak

Games can configure these singletons in their world setup:

1. **`RenderDensitySettings`** - Global render density
   ```csharp
   // In game bootstrap/initialization
   var densityEntity = EntityManager.CreateEntity();
   EntityManager.AddComponentData(densityEntity, new RenderDensitySettings
   {
       CurrentDensity = 1,  // 1 = all, 10 = 1 in 10
       MaxRenderCount = 0,   // 0 = unlimited
       LastUpdateTick = 0
   });
   ```

2. **`LODThresholds`** - LOD distance thresholds
   ```csharp
   var lodEntity = EntityManager.CreateEntity();
   EntityManager.AddComponentData(lodEntity, new LODThresholds
   {
       LOD1Distance = 50f,   // Reduced detail at 50 units
       LOD2Distance = 100f,  // Impostor at 100 units
       LOD3Distance = 200f,  // Hidden at 200 units
       Hysteresis = 5f       // Prevent flickering
   });
   ```

#### Example: Space4X Craft vs Fleet Impostor

```csharp
// In Space4X presentation system
public void RenderCarrierOrFleet(Entity entity)
{
    // Check if this is an aggregate (fleet)
    if (SystemAPI.HasComponent<AggregateRenderSummary>(entity))
    {
        var summary = SystemAPI.GetComponent<AggregateRenderSummary>(entity);
        var lod = SystemAPI.GetComponent<RenderLODData>(entity);
        
        // Use aggregate impostor if far away
        if (lod.CameraDistance > 100f)
        {
            RenderFleetImpostor(summary);
            return;
        }
    }
    
    // Otherwise render individual craft
    if (SystemAPI.HasComponent<RenderSampleIndex>(entity))
    {
        var sample = SystemAPI.GetComponent<RenderSampleIndex>(entity);
        if (sample.ShouldRender == 0) return; // Density filtered
    }
    
    RenderCraftMesh(entity);
}
```

#### Example: Godgame Villager Density

```csharp
// In Godgame presentation system
public void RenderVillagers()
{
    foreach (var (lod, sample, entity) in 
        SystemAPI.Query<RenderLODData, RenderSampleIndex>()
            .WithEntityAccess())
    {
        // Skip if density filtered
        if (sample.ShouldRender == 0) continue;
        
        // Skip if culled
        if (lod.RecommendedLOD >= 3) continue;
        
        // Select rendering quality
        switch (lod.RecommendedLOD)
        {
            case 0: RenderVillagerFullDetail(entity); break;
            case 1: RenderVillagerReducedDetail(entity); break;
            case 2: RenderVillagerImpostor(entity); break;
        }
    }
}
```

### Aggregate Rendering Contracts

#### Components to Read

1. **`AggregateRenderSummary`** - Summary data for impostor rendering
   ```csharp
   if (SystemAPI.HasComponent<AggregateRenderSummary>(entity))
   {
       var summary = SystemAPI.GetComponent<AggregateRenderSummary>(entity);
       
       // Render single marker at summary position
       RenderAggregateMarker(
           summary.BoundsCenter,
           summary.MemberCount,
           summary.TotalHealth,
           summary.DominantTypeIndex
       );
   }
   ```

2. **`AggregateState`** - Detailed aggregate statistics
   ```csharp
   var state = SystemAPI.GetComponent<AggregateState>(aggregateEntity);
   // Use for UI, tooltips, gameplay decisions
   ```

3. **`AggregateMembership`** - Member entity tracking
   ```csharp
   // On member entity
   var membership = SystemAPI.GetComponent<AggregateMembership>(memberEntity);
   var aggregate = membership.AggregateEntity;
   // Use to find aggregate from member
   ```

#### Example: Village Impostor (Godgame)

```csharp
// Village entity has AggregateRenderSummary
public void RenderVillage(Entity villageEntity)
{
    var summary = SystemAPI.GetComponent<AggregateRenderSummary>(villageEntity);
    var lod = SystemAPI.GetComponent<RenderLODData>(villageEntity);
    
    // Render impostor if far away
    if (lod.CameraDistance > summary.AggregateRenderDistance)
    {
        RenderVillageImpostor(
            summary.BoundsCenter,
            summary.MemberCount,
            summary.AverageMorale,
            summary.DominantTypeIndex
        );
    }
    else
    {
        // Render individual villagers (with density)
        RenderVillageMembers(villageEntity);
    }
}
```

#### Example: Fleet Icon (Space4X)

```csharp
// Fleet entity has AggregateRenderSummary
public void RenderFleet(Entity fleetEntity)
{
    var summary = SystemAPI.GetComponent<AggregateRenderSummary>(fleetEntity);
    
    // Always render fleet icon at aggregate position
    RenderFleetIcon(
        summary.AveragePosition,
        summary.TotalStrength,
        summary.MemberCount
    );
    
    // Optionally render individual carriers if close
    if (lod.CameraDistance < 50f)
    {
        RenderFleetMembers(fleetEntity);
    }
}
```

### Physics vs Spatial Grid Contract

#### When to Use Each

**Default: Spatial Grid** (`UsesSpatialGrid`)
- All interactions start here
- Fast (~0.01ms per query)
- Deterministic
- Scales to millions

**Opt-In: Physics** (`RequiresPhysics`)
- Only when:
  - Player-visible spectacle requires realistic motion
  - Gameplay mechanics depend on physics (pushing, momentum)
  - Visual feedback needs collision response

#### Component Usage

```csharp
// Default: spatial grid (implicit, no component needed)
// Or explicit:
EntityManager.AddComponent(entity, new UsesSpatialGrid
{
    QueryRadius = 10f,
    Flags = SpatialQueryFlags.Queryable | SpatialQueryFlags.CanQuery
});

// Opt-in: physics (rare, < 1% of entities)
EntityManager.AddComponent(entity, new RequiresPhysics
{
    Priority = 100,
    Flags = PhysicsInteractionFlags.Collidable | PhysicsInteractionFlags.Dynamic
});
```

#### Ballistic Motion (No Physics)

**Godgame Thrown Objects**:
```csharp
// On throw
var motion = new BallisticMotion
{
    Velocity = PhysicsInteractionHelpers.CalculateBallisticArc(
        throwPosition, targetPosition, -9.81f, flightTime),
    Gravity = new float3(0, -9.81f, 0),
    FlightTime = 0f,
    MaxFlightTime = 5f,
    Flags = BallisticMotionFlags.Active | BallisticMotionFlags.UseGravity
};
EntityManager.AddComponent(chunkEntity, motion);

// Update each tick (in movement system)
var motion = SystemAPI.GetComponent<BallisticMotion>(entity);
PhysicsInteractionHelpers.UpdateBallisticPosition(
    ref position, ref motion.Velocity, motion.Gravity, deltaTime);
motion.FlightTime += deltaTime;
SystemAPI.SetComponent(entity, motion);
```

**Space4X Projectiles**:
```csharp
// For visual projectiles (not hitscan)
var motion = new BallisticMotion
{
    Velocity = direction * speed,
    Gravity = float3.zero,  // No gravity in space
    FlightTime = 0f,
    MaxFlightTime = lifetime,
    Flags = BallisticMotionFlags.Active
};
EntityManager.AddComponent(projectileEntity, motion);
```

#### Ground Collision (No Physics)

```csharp
// Add to thrown objects
EntityManager.AddComponent(entity, new GroundCollisionCheck
{
    HeightOffset = 0f,
    BreakVelocityThreshold = 5f,  // Break if impact > 5 m/s
    Flags = 0
});

// In movement system
var collision = SystemAPI.GetComponent<GroundCollisionCheck>(entity);
float groundHeight = GetTerrainHeight(position.xz);
if (position.y <= groundHeight + collision.HeightOffset)
{
    // Landed
    collision.Flags |= GroundCollisionCheck.FlagHasCollided;
    
    if (math.lengthsq(velocity) > collision.BreakVelocityThreshold)
    {
        collision.Flags |= GroundCollisionCheck.FlagShouldBreak;
        SpawnFragments(entity);
    }
    
    SystemAPI.SetComponent(entity, collision);
}
```

---

## Phase 4: Scale Test Tuning & CI Integration Notes

### How to Run Scale Tests

#### Command Line

```bash
# List available scenarios
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.ListScaleScenarios

# Run baseline scenario
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_baseline_10k \
  --metrics CI/Reports/baseline.json

# Run stress scenario
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_stress_100k \
  --metrics CI/Reports/stress.json

# Run extreme scenario
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_extreme_1m \
  --metrics CI/Reports/extreme.json \
  --target-ms 100.0
```

#### In Unity Editor

1. Open Unity project
2. Go to menu: `PureDOTS/Performance/Run Scale Tests` (if implemented)
3. Or use Console: `RunScaleTest("scale_baseline_10k")`

#### Interpreting Results

**ScaleTestResult** contains:
- `AverageTickTimeMs` - Average tick time (should be < budget)
- `MaxTickTimeMs` - Worst-case tick time (check for spikes)
- `P95TickTimeMs` - 95th percentile (most ticks should be below this)
- `PeakMemoryBytes` - Peak memory usage
- `PassedBudget` - 1 if passed, 0 if failed

**Common Failure Modes**:
1. **Average tick time exceeds budget**
   - **Cause**: System too slow for entity count
   - **Fix**: Optimize hot systems, reduce update frequency, enable LOD/aggregates

2. **Max tick time spikes**
   - **Cause**: Periodic expensive operations (garbage collection, registry rebuilds)
   - **Fix**: Spread work across ticks, use incremental updates

3. **Memory exceeds budget**
   - **Cause**: Large components, too many buffers, memory leaks
   - **Fix**: Move to companion entities, reduce buffer sizes, check for leaks

4. **Entity count exceeds recommended**
   - **Cause**: Too many hot entities
   - **Fix**: Enable render density, use aggregates, move to cold data

### CI Integration

**CI/validate_metrics.py** validates JSON reports against budgets:

```bash
# Validate all reports
python CI/validate_metrics.py CI/Reports/

# Output:
# ============================================================
# PureDOTS Scale Test Validation
# ============================================================
# 
# Validating: baseline.json
# ----------------------------------------
#   Status: PASSED
# 
# Validating: stress.json
# ----------------------------------------
#   Status: FAILED
#   ERROR: Average tick time 35.50ms exceeds budget 33.33ms
# 
# ============================================================
# Summary: 2 reports, 1 errors, 0 warnings
# ============================================================
```

**If CI Fails**:
1. Check which metric exceeded budget
2. Review `ScaleTestMetrics` output for details
3. Identify hot systems using `CollectSystemTimings`
4. Apply optimizations (LOD, aggregates, component migration)
5. Re-run test and verify fix

---

## Phase 5: Tiny Implementation Deltas (Optional)

### Example Migration: ResourceChunk

**Before**:
```csharp
// ResourceChunkState on main entity
// No LOD/density support
// No ballistic motion
```

**After**:
```csharp
// Main entity: hot components + LOD
- LocalTransform
- ResourceChunkState
- BallisticMotion (if thrown)
- GroundCollisionCheck (if thrown)
- RenderLODData
- RenderCullable

// Systems updated:
- ResourceChunkMovementSystem: Uses BallisticMotion
- ResourceChunkCollisionSystem: Uses GroundCollisionCheck
- Game presentation: Reads RenderLODData
```

**Migration Steps** (see `ComponentMigrationGuide.md`):
1. Add `RenderLODData` and `RenderCullable` to ResourceChunk baker
2. Add `BallisticMotion` when chunk is thrown
3. Add `GroundCollisionCheck` for thrown chunks
4. Update movement system to use ballistic helpers
5. Update presentation to read LOD data

---

## Next Steps

1. **Run sanity scenarios** (Phase 1) to validate infrastructure
2. **Review migration plan** (Phase 2) and prioritize archetypes
3. **Implement contracts** (Phase 3) in game projects
4. **Tune scale tests** (Phase 4) with real workloads
5. **Execute migrations** (Phase 5) one archetype at a time

---

---

---

## Implementation Status Summary

### Wave 1: Sanity Scenarios ✅
- `scale_mini_lod_demo.json` and `scale_mini_aggregate_demo.json` created
- `ScenarioTestEntitySpawnerSystem.cs` wired to scenario execution
- `ScaleTestSpawnerHelpers.cs` provides reusable spawn methods
- CLI debug flags (`--enable-lod-debug`, `--enable-aggregate-debug`) implemented
- Editor menu items in `PureDOTS > Scale Tests`

### Wave 2: Villager & ResourceChunk Migration ✅
- `VillagerBeliefOptimized` (6 bytes vs 72 bytes)
- `VillagerLODComponents.cs` with helpers
- `VillagerBeliefMigrationSystem.cs` for compatibility
- `ResourceChunkLODHelpers.cs` with ballistic motion
- `ResourceChunkBallisticSystem.cs` for thrown objects
- Bakers updated with LOD component support

### Wave 3: Space4X Migration ✅
- `SpaceLODComponents.cs` with fleet/carrier helpers
- `FleetMemberRef`, `FleetTag`, `FleetState`, `FleetRenderSummary`
- `HarvesterAuthoring` updated with LOD support

### Wave 4: Aggregates & Physics ✅
- `VillageAggregateComponents.cs` with village helpers
- `CollectiveAggregateSystem.cs` and `FleetAggregateSystem.cs`
- `SpatialInteractionExampleSystem.cs` demonstrating grid usage

### Wave 5: CI Integration ✅
- `ScaleTestEditorMenu.cs` with editor menu items
- `CI/run_scale_tests.sh` for batch execution
- `CI/validate_metrics.py` for budget validation

### Wave 6: Documentation ✅
- `Docs/Guides/GameIntegrationGuide.md` for Space4X/Godgame agents
- All roadmap sections updated with implementation status
- Cross-links to related documentation

---

## Summary

This roadmap provides:

1. **Sanity Scenarios** - Small test scenarios to validate LOD/aggregate systems work
2. **Migration Plan** - Prioritized list of archetypes to migrate with before/after layouts
3. **Integration Contracts** - Clear API for Space4X and Godgame to consume performance infrastructure
4. **Scale Test Guide** - How to run and interpret scale tests, plus CI integration
5. **Example Implementation** - Test entity spawner for validation

**Key Files**:
- `Docs/Guides/PerformanceIntegrationRoadmap.md` - This document
- `scale_mini_lod_demo.json` - LOD validation scenario
- `scale_mini_aggregate_demo.json` - Aggregate validation scenario
- `TestEntitySpawnerSystem.cs` - Helper for spawning test entities
- `LODDebugMetricsSystem.cs` - LOD debug metrics collection
- `AggregateDebugMetricsSystem.cs` - Aggregate debug metrics collection

**Next Actions**:
1. Run sanity scenarios to validate infrastructure
2. Review migration plan and prioritize archetypes
3. Implement contracts in game projects
4. Execute migrations one archetype at a time

---

## See Also

- `Docs/PERFORMANCE_PLAN.md` - Overall performance strategy
- `Docs/FoundationGuidelines.md` - Component size guidelines
- `Docs/Guides/ComponentMigrationGuide.md` - Migration patterns
- `Docs/Guides/PhysicsVsSpatialGrid.md` - Physics guidelines
- `Docs/QA/PerformanceBudgets.md` - Performance targets

