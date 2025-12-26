# PureDOTS Performance Infrastructure - Game Integration Guide

This guide is for **Space4X and Godgame agents** to integrate with the PureDOTS performance infrastructure.

---

## Presentation Systems

PureDOTS presentation helpers run in `Unity.Entities.PresentationSystemGroup` (either directly or via `PureDOTS.Systems.PureDotsPresentationSystemGroup`).

**Policy**: All game-facing visual/presentation systems must run in Unity's `PresentationSystemGroup`. PureDOTS sim systems never run in presentation groups - they live in simulation groups (`SimulationSystemGroup`, `FixedStepSimulationSystemGroup`, etc.).

**Example**:
```csharp
// PureDOTS presentation system
[UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
public partial struct AggregateRenderSummarySystem : ISystem
{
    // read-only sim data, write-only presentation/metrics data
}

// Game-side presentation system
[UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
public partial struct MyGameRenderSystem : ISystem
{
    // Read from sim data, write to presentation components
}
```

Games may add additional presentation systems in the same group or in additional child groups. See `Docs/FoundationGuidelines.md` for full presentation system group policy.

---

## Quick Reference

### Stable Components You Can Depend On

| Component | Purpose | When to Use |
|-----------|---------|-------------|
| `RenderLODData` | LOD level and importance | Read for rendering decisions |
| `RenderCullable` | Cull distance and priority | Read for visibility culling |
| `RenderSampleIndex` | Density sampling | Read for "1 in N" rendering |
| `AggregateMembership` | Links entity to aggregate | Add when entity joins group |
| `AggregateRenderSummary` | Aggregate render data | Read for impostor rendering |
| `AggregateState` | Aggregate statistics | Read for gameplay decisions |
| `UsesSpatialGrid` | Spatial grid marker | Add for grid-based queries |
| `RequiresPhysics` | Physics marker | Add only when physics needed |

### Stable Helper Classes

| Helper | Purpose |
|--------|---------|
| `RenderLODHelpers` | Calculate sample indices, LOD levels |
| `VillagerLODHelpers` | Add LOD components to villagers |
| `ResourceChunkLODHelpers` | Add LOD/ballistic to chunks |
| `SpaceLODHelpers` | Add LOD to carriers/asteroids |
| `VillageAggregateHelpers` | Create/manage village aggregates |
| `ScaleTestSpawnerHelpers` | Spawn test entities with LOD |

---

## Per-Archetype Consumption Guide

### Quick Reference: One-Liner Recipes

**Godgame Archetypes:**
- **Villager**: Read `LocalTransform`, `VillagerAIState`, `VillagerJob`, `VillagerNeeds`, `VillagerFlags`, `RenderLODData`, `RenderCullable`, `RenderSampleIndex`, `VillagerVillageRef`, `AggregateMembership`. Optional: `VillagerBeliefOptimized`.
- **ResourceChunk**: Read `LocalTransform`, `ResourceChunkState`, `ResourceTypeId`, `RenderLODData`, `RenderCullable`, `RenderSampleIndex`. Optional: `BallisticMotion` (if thrown).
- **Village Aggregate**: Read `LocalTransform`, `VillageState`, `VillageRenderSummary`, `AggregateState`, `AggregateRenderSummary`, `AggregateMemberElement` buffer. Centroid: `VillageState.CenterPosition`.

**Space4X Archetypes:**
- **Carrier/Craft**: Read `LocalTransform`, `Carrier`, `RenderLODData`, `RenderCullable`, `RenderSampleIndex`, `FleetMemberRef`, `AggregateMembership`. Heading: `LocalTransform.Rotation`.
- **Asteroid/Resource Node**: Read `LocalTransform`, `ResourceSourceState`, `ResourceTypeId`, `RenderLODData`, `RenderCullable`, `RenderSampleIndex`. Heading: `LocalTransform.Rotation`.
- **Fleet Aggregate**: Read `LocalTransform`, `FleetState`, `FleetRenderSummary`, `AggregateState`, `AggregateRenderSummary`, `AggregateMemberElement` buffer. Centroid: `FleetState.AveragePosition`.

---

### How to Render Villagers

**One-liner recipe**: Read `LocalTransform`, `VillagerAIState`, `VillagerJob`, `VillagerNeeds`, `VillagerFlags`, `RenderLODData`, `RenderCullable`, `RenderSampleIndex`, `VillagerVillageRef`, `AggregateMembership`. Optional: `VillagerBeliefOptimized` for faith/worship rendering.

**Required Components:**
- **Position/Orientation**: `LocalTransform` (Position, Rotation, Scale)
- **State Feed**: 
  - `VillagerAIState` (CurrentState: Idle/Working/Eating/Sleeping/Fleeing/Fighting/Dead, CurrentGoal)
  - `VillagerJob` (Type: None/Farmer/Builder/Gatherer/etc, Phase: Idle/Assigned/Gathering/etc)
  - `VillagerNeeds` (Health, Hunger, Energy, Morale, Temperature)
  - `VillagerFlags` (IsDead, IsAlive, IsInCombat, IsCarrying, IsWorking, IsFleeing - packed flags)
  - `VillagerId` (Value, FactionId)
- **LOD**: `RenderLODData`, `RenderCullable`, `RenderSampleIndex`
- **Aggregate**: `VillagerVillageRef` (VillageEntity, VillagerIndex), `AggregateMembership`
- **Optional**: `VillagerBeliefOptimized` (PrimaryDeityIndex, Faith, WorshipProgress) for faith-based rendering

**Example:**
```csharp
foreach (var (transform, aiState, job, needs, flags, lodData, cullable, sampleIndex, villageRef, entity) in
    SystemAPI.Query<RefRO<LocalTransform>, RefRO<VillagerAIState>, RefRO<VillagerJob>, 
                    RefRO<VillagerNeeds>, RefRO<VillagerFlags>, RefRO<RenderLODData>, 
                    RefRO<RenderCullable>, RefRO<RenderSampleIndex>, RefRO<VillagerVillageRef>>()
        .WithAll<VillagerId>()
        .WithEntityAccess())
{
    // Skip if density-culled
    if (sampleIndex.ValueRO.ShouldRender == 0) continue;
    
    // Skip if LOD-culled
    if (lodData.ValueRO.RecommendedLOD >= 4) continue;
    
    // Skip if dead
    if (flags.ValueRO.IsDead) continue;
    
    // Render villager
    float3 position = transform.ValueRO.Position;
    quaternion rotation = transform.ValueRO.Rotation;
    byte state = (byte)aiState.ValueRO.CurrentState;
    byte jobType = (byte)job.ValueRO.Type;
    float health = needs.ValueRO.Health;
    bool isAlive = !flags.ValueRO.IsDead;
    
    RenderVillager(entity, position, rotation, state, jobType, health, isAlive, lodData.ValueRO.RecommendedLOD);
}
```

### How to Render ResourceChunks

**One-liner recipe**: Read `LocalTransform`, `ResourceChunkState`, `ResourceTypeId`, `RenderLODData`, `RenderCullable`, `RenderSampleIndex`, `BallisticMotion` (if thrown).

**Required Components:**
- **Position/Orientation**: `LocalTransform` (Position, Rotation, Scale)
- **State Feed**: 
  - `ResourceChunkState` (Flags: Carried/Thrown/PendingDestroy, Units, Velocity, Age, QualityTier)
  - `ResourceTypeId` (Value: FixedString64Bytes)
- **LOD**: `RenderLODData`, `RenderCullable`, `RenderSampleIndex`
- **Physics**: `BallisticMotion` (if Flags.HasFlag(Thrown)), `GroundCollisionCheck` (if thrown)

**Example:**
```csharp
foreach (var (transform, chunkState, resourceType, lodData, cullable, sampleIndex, entity) in
    SystemAPI.Query<RefRO<LocalTransform>, RefRO<ResourceChunkState>, RefRO<ResourceTypeId>,
                    RefRO<RenderLODData>, RefRO<RenderCullable>, RefRO<RenderSampleIndex>>()
        .WithEntityAccess())
{
    // Skip if density-culled
    if (sampleIndex.ValueRO.ShouldRender == 0) continue;
    
    // Skip if culled
    if (lodData.ValueRO.RecommendedLOD >= 4) continue;
    
    // Check if thrown (has ballistic motion)
    bool isThrown = chunkState.ValueRO.Flags.HasFlag(ResourceChunkFlags.Thrown);
    
    float3 position = transform.ValueRO.Position;
    float units = chunkState.ValueRO.Units;
    string resourceTypeId = resourceType.ValueRO.Value.ToString();
    
    RenderResourceChunk(entity, position, units, resourceTypeId, isThrown, lodData.ValueRO.RecommendedLOD);
}
```

### How to Render Villages (Aggregate)

**One-liner recipe**: Read `LocalTransform`, `VillageState`, `VillageRenderSummary`, `AggregateState`, `AggregateRenderSummary`, `AggregateMemberElement` buffer. Centroid position from `VillageState.CenterPosition` or `AggregateState.AveragePosition`.

**Required Components:**
- **Position/Orientation**: `LocalTransform` (on aggregate entity) - Position represents village center, or use `VillageState.CenterPosition` / `AggregateState.AveragePosition`
- **State Feed**: 
  - `VillageState` (PopulationCount, CenterPosition, BoundsMin, BoundsMax, TotalFood, TotalWealth, AverageMorale, AverageFaith, DominantDeityIndex)
  - `VillageRenderSummary` (PopulationCount, CenterPosition, BoundsCenter, BoundsRadius, TotalWealth, AverageMorale, AverageFaith, DominantBuildingType, FactionIndex)
  - `AggregateState` (MemberCount, AveragePosition, BoundsMin, BoundsMax, TotalHealth, AverageMorale, TotalStrength)
- **LOD**: `RenderLODData`, `RenderCullable`, `RenderSampleIndex` (on aggregate entity)
- **Members**: `AggregateMemberElement` buffer (MemberEntity, StrengthContribution, Health)

**Example:**
```csharp
foreach (var (transform, villageState, renderSummary, aggregateState, members, lodData, entity) in
    SystemAPI.Query<RefRO<LocalTransform>, RefRO<VillageState>, RefRO<VillageRenderSummary>,
                    RefRO<AggregateState>, DynamicBuffer<AggregateMemberElement>, RefRO<RenderLODData>>()
        .WithAll<VillageTag>()
        .WithEntityAccess())
{
    // Use CenterPosition from VillageState as centroid
    float3 centerPosition = villageState.ValueRO.CenterPosition; // Village centroid
    int population = villageState.ValueRO.PopulationCount;
    float wealth = villageState.ValueRO.TotalWealth;
    float morale = villageState.ValueRO.AverageMorale;
    float faith = villageState.ValueRO.AverageFaith;
    
    // Skip if LOD-culled
    if (lodData.ValueRO.RecommendedLOD >= 4) continue;
    
    // Render village marker/impostor
    RenderVillageMarker(entity, centerPosition, population, wealth, morale, faith);
    
    // Optionally render individual villagers if close enough
    var config = SystemAPI.GetComponent<AggregateRenderConfig>(entity);
    float cameraDistance = math.distance(cameraPosition, centerPosition);
    
    if (cameraDistance < config.AggregateRenderDistance)
    {
        int renderCount = math.min(members.Length, config.MaxIndividualRender);
        for (int i = 0; i < renderCount; i++)
        {
            RenderVillager(members[i].MemberEntity);
        }
    }
}
```

---

## For Space4X

### How to Render Carriers/Crafts

**One-liner recipe**: Read `LocalTransform`, `Carrier` (for carriers), `RenderLODData`, `RenderCullable`, `RenderSampleIndex`, `FleetMemberRef`, `AggregateMembership`. For heading/orientation, use `LocalTransform.Rotation`.

**Required Components:**
- **Position/Orientation**: `LocalTransform` (Position, Rotation, Scale) - Rotation provides heading/orientation
- **State Feed**: 
  - `Carrier` (CarrierId, TotalCapacity, CurrentLoad) - for carrier entities
  - `CarrierInventoryItem` buffer (ResourceTypeIndex, Amount) - optional, for inventory display
- **LOD**: `RenderLODData`, `RenderCullable`, `RenderSampleIndex`
- **Aggregate**: `FleetMemberRef` (FleetEntity, MemberIndex), `AggregateMembership`

**Example:**
```csharp
foreach (var (transform, carrier, lodData, cullable, sampleIndex, fleetRef, entity) in
    SystemAPI.Query<RefRO<LocalTransform>, RefRO<Carrier>, RefRO<RenderLODData>,
                    RefRO<RenderCullable>, RefRO<RenderSampleIndex>, RefRO<FleetMemberRef>>()
        .WithEntityAccess())
{
    // Skip if density-culled
    if (sampleIndex.ValueRO.ShouldRender == 0) continue;
    
    // Skip if LOD-culled
    if (lodData.ValueRO.RecommendedLOD >= 4) continue;
    
    float3 position = transform.ValueRO.Position;
    quaternion rotation = transform.ValueRO.Rotation; // Heading/orientation
    float cargoCapacity = carrier.ValueRO.TotalCapacity;
    float currentLoad = carrier.ValueRO.CurrentLoad;
    
    RenderCarrier(entity, position, rotation, cargoCapacity, currentLoad, lodData.ValueRO.RecommendedLOD);
}
```

### Adding LOD to Ships

```csharp
// In your ship spawner or baker:
SpaceLODHelpers.AddLODComponents(entityManager, shipEntity, cullDistance: 300f, importance: 0.6f);
```

### Creating Fleet Aggregates

```csharp
// Create fleet
var fleetEntity = SpaceLODHelpers.CreateFleetAggregate(entityManager, position, expectedMemberCount: 10);

// Add ships to fleet
SpaceLODHelpers.AddFleetMembership(entityManager, shipEntity, fleetEntity, memberIndex, FleetMemberFlags.IsActive);
```

### How to Render Asteroids/Resource Nodes

**One-liner recipe**: Read `LocalTransform`, `ResourceSourceState`, `ResourceTypeId`, `RenderLODData`, `RenderCullable`, `RenderSampleIndex`. For heading/orientation, use `LocalTransform.Rotation`.

**Required Components:**
- **Position/Orientation**: `LocalTransform` (Position, Rotation, Scale) - Rotation provides heading/orientation
- **State Feed**: 
  - `ResourceSourceState` (SourceType, UnitsRemaining, QualityTier, BaseQuality, QualityVariance)
  - `ResourceTypeId` (Value: FixedString64Bytes)
  - `ResourceSourceConfig` (optional - GatherRatePerWorker, MaxSimultaneousWorkers, Flags)
- **LOD**: `RenderLODData`, `RenderCullable`, `RenderSampleIndex`

**Example:**
```csharp
foreach (var (transform, sourceState, resourceType, lodData, cullable, sampleIndex, entity) in
    SystemAPI.Query<RefRO<LocalTransform>, RefRO<ResourceSourceState>, RefRO<ResourceTypeId>,
                    RefRO<RenderLODData>, RefRO<RenderCullable>, RefRO<RenderSampleIndex>>()
        .WithEntityAccess())
{
    // Skip if density-culled
    if (sampleIndex.ValueRO.ShouldRender == 0) continue;
    
    // Skip if culled
    if (lodData.ValueRO.RecommendedLOD >= 4) continue;
    
    float3 position = transform.ValueRO.Position;
    quaternion rotation = transform.ValueRO.Rotation; // Heading/orientation
    float unitsRemaining = sourceState.ValueRO.UnitsRemaining;
    string resourceTypeId = resourceType.ValueRO.Value.ToString();
    byte qualityTier = (byte)sourceState.ValueRO.QualityTier;
    bool isEmpty = unitsRemaining <= 0f;
    
    RenderAsteroid(entity, position, rotation, unitsRemaining, resourceTypeId, qualityTier, isEmpty, lodData.ValueRO.RecommendedLOD);
}
```

### How to Render Fleets (Aggregate)

**One-liner recipe**: Read `LocalTransform`, `FleetState`, `FleetRenderSummary`, `AggregateState`, `AggregateRenderSummary`, `AggregateMemberElement` buffer. Centroid position from `FleetState.AveragePosition` or `AggregateState.AveragePosition`.

**Required Components:**
- **Position/Orientation**: `LocalTransform` (on aggregate entity) - Position represents fleet centroid, or use `FleetState.AveragePosition` / `AggregateState.AveragePosition`
- **State Feed**: 
  - `FleetState` (MemberCount, AveragePosition, BoundsMin, BoundsMax, TotalStrength, TotalHealth, TotalCargoCapacity)
  - `FleetRenderSummary` (MemberCount, AveragePosition, BoundsCenter, BoundsRadius, TotalStrength, TotalHealth, DominantShipType, FactionIndex)
  - `AggregateState` (MemberCount, AveragePosition, BoundsMin, BoundsMax, TotalHealth, AverageMorale, TotalStrength)
- **LOD**: `RenderLODData`, `RenderCullable`, `RenderSampleIndex` (on aggregate entity)
- **Members**: `AggregateMemberElement` buffer (MemberEntity, StrengthContribution, Health)

**Example:**
```csharp
// In your presentation system:
foreach (var (transform, fleetState, renderSummary, aggregateState, members, lodData, entity) in
    SystemAPI.Query<RefRO<LocalTransform>, RefRO<FleetState>, RefRO<FleetRenderSummary>,
                    RefRO<AggregateState>, DynamicBuffer<AggregateMemberElement>, RefRO<RenderLODData>>()
        .WithAll<FleetTag>()
        .WithEntityAccess())
{
    // Use AveragePosition from FleetState as centroid
    float3 position = fleetState.ValueRO.AveragePosition; // Fleet centroid
    float strength = fleetState.ValueRO.TotalStrength;
    int memberCount = fleetState.ValueRO.MemberCount;
    float health = fleetState.ValueRO.TotalHealth;
    float cargoCapacity = fleetState.ValueRO.TotalCargoCapacity;
    
    // Skip if LOD-culled
    if (lodData.ValueRO.RecommendedLOD >= 4) continue;
    
    // Render fleet impostor
    RenderFleetImpostor(entity, position, strength, memberCount, health, cargoCapacity);
    
    // Optionally render individual ships if close enough
    var config = SystemAPI.GetComponent<AggregateRenderConfig>(entity);
    float cameraDistance = math.distance(cameraPosition, position);
    
    if (cameraDistance < config.AggregateRenderDistance)
    {
        int renderCount = math.min(members.Length, config.MaxIndividualRender);
        for (int i = 0; i < renderCount; i++)
        {
            RenderCarrier(members[i].MemberEntity);
        }
    }
}
```

### Reading Fleet Impostor Data

```csharp
// In your presentation system:
if (SystemAPI.HasComponent<FleetRenderSummary>(fleetEntity))
{
    var summary = SystemAPI.GetComponent<FleetRenderSummary>(fleetEntity);
    
    // Use summary for impostor rendering
    float3 position = summary.AveragePosition;
    float strength = summary.TotalStrength;
    int memberCount = summary.MemberCount;
    
    // Render single fleet marker instead of individual ships
    RenderFleetImpostor(position, strength, memberCount);
}
```

### Deciding Ship vs Fleet Impostor

```csharp
// In your rendering decision system:
float cameraDistance = math.distance(cameraPosition, fleetPosition);
var config = SystemAPI.GetComponent<AggregateRenderConfig>(fleetEntity);

if (cameraDistance > config.AggregateRenderDistance)
{
    // Render fleet impostor
    RenderFleetImpostor(fleetEntity);
}
else
{
    // Render individual ships (up to MaxIndividualRender)
    var members = SystemAPI.GetBuffer<AggregateMemberElement>(fleetEntity);
    int renderCount = math.min(members.Length, config.MaxIndividualRender);
    
    for (int i = 0; i < renderCount; i++)
    {
        RenderShip(members[i].MemberEntity);
    }
}
```

---

## For Godgame

### Adding LOD to Villagers

```csharp
// In your villager spawner or baker:
VillagerLODHelpers.AddLODComponents(entityManager, villagerEntity, cullDistance: 200f);
```

### Creating Village Aggregates

```csharp
// Create village
var villageEntity = VillageAggregateHelpers.CreateVillageAggregate(entityManager, position, expectedPopulation: 20);

// Add villagers to village
VillageAggregateHelpers.AddVillagerToVillage(entityManager, villagerEntity, villageEntity, villagerIndex);
```

### Rendering 1 in N Villagers (Density Control)

```csharp
// In your presentation system:
var densitySettings = SystemAPI.GetSingleton<RenderDensitySettings>();

foreach (var (lodData, sampleIndex, entity) in 
    SystemAPI.Query<RefRO<RenderLODData>, RefRO<RenderSampleIndex>>()
        .WithAll<VillagerId>()
        .WithEntityAccess())
{
    // Skip if density-culled
    if (sampleIndex.ValueRO.ShouldRender == 0)
    {
        continue;
    }
    
    // Skip if LOD-culled
    if (lodData.ValueRO.RecommendedLOD >= 4)
    {
        continue;
    }
    
    // Render villager at appropriate LOD
    RenderVillager(entity, lodData.ValueRO.RecommendedLOD);
}
```

### Reading Village Impostor Data

```csharp
// In your presentation system:
if (SystemAPI.HasComponent<VillageRenderSummary>(villageEntity))
{
    var summary = SystemAPI.GetComponent<VillageRenderSummary>(villageEntity);
    
    // Use summary for village marker
    float3 position = summary.CenterPosition;
    int population = summary.PopulationCount;
    float wealth = summary.TotalWealth;
    
    // Render village marker
    RenderVillageMarker(position, population, wealth);
}
```

### Thrown Resource Chunks

```csharp
// When villager throws a chunk:
ResourceChunkLODHelpers.MarkAsThrown(entityManager, chunkEntity);
ResourceChunkLODHelpers.AddBallisticMotion(
    entityManager, 
    chunkEntity, 
    initialVelocity: velocity,
    gravity: new float3(0, -9.81f, 0),
    maxFlightTime: 5f
);
```

---

## legacy Scenarios

PureDOTS provides game-flavored legacy scenarios that showcase the LOD/aggregate/physics infrastructure:

### Space4X legacy (`scenario_space_demo_01.json`)

**Purpose**: Demonstrates Space4X entities with full performance infrastructure.

**Entities**:
- 8 Carriers with LOD components
- 25 Strike Craft with LOD components
- 12 Asteroids with LOD components
- 2 Fleet aggregates with summary components
- 50 Projectiles using spatial grid (`UsesSpatialGrid`)

**Features**:
- Carriers and crafts assigned to fleet aggregates
- Fleet impostor rendering data available
- Asteroids use LOD for distance culling
- Projectiles use spatial grid (not physics) for interactions

**Run**: `PureDOTS > Scale Tests > Game Demos > Space4X legacy`

### Godgame legacy (`scenario_god_demo_01.json`)

**Purpose**: Demonstrates Godgame entities with full performance infrastructure.

**Entities**:
- 75 Villagers with LOD components and village membership
- 30 Resource chunks with LOD components and ballistic motion support
- 3 Village aggregates with summary components
- 5 Storehouses with LOD components

**Features**:
- Villagers assigned to village aggregates
- Village impostor rendering data available
- Resource chunks support ballistic motion for thrown objects
- All entities use spatial grid (`UsesSpatialGrid`) for interactions

**Run**: `PureDOTS > Scale Tests > Game Demos > Godgame legacy`

---

## Running Scale Tests

### From Editor

Use menu: **PureDOTS > Scale Tests > ...**

**Scale Tests:**
- **Run Mini LOD legacy**: 2k test entities with LOD
- **Run Mini Aggregate legacy**: 5 aggregates with members
- **Run Baseline 10k**: 10k entities, 60 FPS target
- **Run Stress 100k**: 100k entities, 30 FPS target
- **Run Extreme 1M**: 1M entities, 10 FPS target

**Game Demos:**
- **Space4X legacy**: Carriers, crafts, asteroids, fleets with LOD/aggregate infrastructure
- **Godgame legacy**: Villagers, resource chunks, villages with LOD/aggregate infrastructure

### From CLI

```bash
# Run baseline test
Unity -batchmode -quit -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_baseline_10k \
  --metrics CI/Reports/baseline.json \
  --enable-lod-debug

# Run all tests
./CI/run_scale_tests.sh --all

# Validate results
python3 CI/validate_metrics.py CI/Reports/
```

### Interpreting Results

**Good Result**:
```
[ScaleTest] Average tick time: 12.5ms (budget: 16.67ms) ✓
[ScaleTest] Peak memory: 450MB (budget: 512MB) ✓
```

**Problem Result**:
```
[ScaleTest] Average tick time: 25.3ms (budget: 16.67ms) ✗
```

**Fix**: Profile to find slow systems, consider:
- Reducing entity counts
- Increasing update intervals on aggregates
- Using density culling more aggressively

---

## Common Patterns

### Pattern: Conditional LOD Updates

```csharp
// Only update LOD data when camera moves significantly
if (math.distance(lastCameraPosition, currentCameraPosition) > 10f)
{
    UpdateLODForAllEntities();
    lastCameraPosition = currentCameraPosition;
}
```

### Pattern: Aggregate Update Throttling

```csharp
// Aggregates update at intervals, not every tick
var aggregateState = SystemAPI.GetComponent<AggregateState>(entity);
if (currentTick - aggregateState.LastAggregationTick < aggregateState.AggregationInterval)
{
    return; // Skip update this tick
}
```

### Pattern: Spatial Grid vs Physics Decision

```csharp
// Default: Use spatial grid
entityManager.AddComponent<UsesSpatialGrid>(entity);

// Exception: Add physics only when needed
if (requiresCollisionResponse || requiresPreciseShapes)
{
    entityManager.AddComponent<RequiresPhysics>(entity);
}
```

---

## Remaining TODOs / Caveats

1. **LOD thresholds not yet tuned** - Default values may need adjustment per game
2. **Aggregate update intervals** - May need tuning based on gameplay requirements
3. **Density sampling** - Currently uniform; game-specific importance weighting TBD
4. **Physics integration** - `RequiresPhysics` marker exists but full integration pending

---

## Getting Help

- See `Docs/Guides/PerformanceIntegrationRoadmap.md` for full implementation details
- See `Docs/Guides/PhysicsVsSpatialGrid.md` for physics decision guidelines
- See `Docs/Guides/ComponentMigrationGuide.md` for component refactoring patterns
- See `Docs/QA/PerformanceBudgets.md` for budget thresholds

