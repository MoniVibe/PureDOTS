# DOTS 1.4 Fix Recommendations - Detailed Implementation Guide

**Status:** Implementation Guide  
**Category:** Performance & Architecture  
**Scope:** Specific fixes for DOTS 1.4 compliance  
**Created:** 2025-01-XX  
**Last Updated:** 2025-01-XX

---

## Purpose

This document provides detailed implementation guidance for fixing DOTS 1.4 compliance issues identified in the audit. Each fix includes code examples and migration steps.

---

## High Priority Fixes

### Fix 1: Remove ToEntityArray/ToComponentDataArray Allocations

**Problem:** Systems allocate managed arrays every frame, causing GC pressure.

**Affected Files:**
- `Systems/Space/HaulingLoopSystem.cs` (lines 35-39)
- `Systems/Space/HaulingJobManagerSystem.cs` (lines 31-32)
- `Systems/Space/HaulingJobPrioritySystem.cs` (lines 27-28)
- `Systems/Space/ResourcePileSystem.cs` (lines 25-27)

**Current Pattern (Bad):**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var piles = _pileQuery.ToEntityArray(Allocator.Temp);  // ❌ Managed array allocation
    var pileData = _pileQuery.ToComponentDataArray<ResourcePile>(Allocator.Temp);  // ❌ Managed array allocation
    
    foreach (var pile in piles)  // ❌ Iterates managed array
    {
        // Process pile
    }
}
```

**Recommended Pattern (Good):**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var job = new ProcessPilesJob
    {
        // Pass lookups if needed
    };
    
    state.Dependency = job.ScheduleParallel(_pileQuery, state.Dependency);
}

[BurstCompile]
public partial struct ProcessPilesJob : IJobEntity
{
    public void Execute(Entity pileEntity, ref ResourcePile pile, in ResourcePileMeta meta)
    {
        // Process pile directly - no allocations
    }
}
```

**Migration Steps:**
1. Convert `OnUpdate` logic to `IJobEntity` or `IJobChunk`
2. Remove `ToEntityArray`/`ToComponentDataArray` calls
3. Use `SystemAPI.Query` or `IJobEntity` for iteration
4. Pass required lookups as job fields

**Example Migration (HaulingLoopSystem):**
```csharp
// Before: Allocates arrays
var piles = _pileQuery.ToEntityArray(Allocator.Temp);
var pileData = _pileQuery.ToComponentDataArray<ResourcePile>(Allocator.Temp);

// After: Use IJobEntity
[BurstCompile]
public partial struct ProcessHaulingJob : IJobEntity
{
    public ComponentLookup<ResourcePile> PileLookup;
    public ComponentLookup<LocalTransform> TransformLookup;
    
    public void Execute(Entity haulerEntity, ref HaulingLoopState loopState, ...)
    {
        // Access piles via lookups or queries as needed
        // No array allocations
    }
}
```

---

### Fix 2: Fix Non-Burst Input System

**Problem:** `CopyInputToEcsSystem` uses `Object.FindFirstObjectByType` which prevents Burst compilation.

**Affected File:** `Systems/Input/CopyInputToEcsSystem.cs`

**Current Pattern (Bad):**
```csharp
public void OnUpdate(ref SystemState state)
{
    var bridge = Object.FindFirstObjectByType<InputSnapshotBridge>();  // ❌ Not Burst-compatible
    if (bridge == null) return;
    
    bridge.FlushSnapshotToEcs(...);
}
```

**Recommended Pattern (Good):**
```csharp
// Option A: Cache bridge in managed wrapper system
public partial struct CopyInputToEcsSystem : ISystem
{
    private InputSnapshotBridge _bridge;  // Cached reference
    
    public void OnCreate(ref SystemState state)
    {
        _bridge = Object.FindFirstObjectByType<InputSnapshotBridge>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        if (_bridge == null) return;
        
        // Bridge operations are still managed, but system can be partially Burst
        _bridge.FlushSnapshotToEcs(...);
    }
}

// Option B: Convert bridge to ECS singleton (better long-term)
// Create InputBridgeSingleton component, update from MonoBehaviour
// Then use SystemAPI.GetSingleton<InputBridgeSingleton>()
```

**Migration Steps:**
1. Cache bridge reference in `OnCreate`
2. Use cached reference in `OnUpdate`
3. (Optional) Convert bridge to ECS singleton for full Burst compatibility

---

## Medium Priority Fixes

### Fix 3: Migrate to Singleton ECB Systems

**Problem:** Systems create ECB directly instead of using singleton ECB systems (DOTS 1.4 pattern).

**Affected Files:**
- `Systems/Space/ResourceDropSpawnerSystem.cs`
- `Systems/Space/DropOnlyHarvestDepositSystem.cs`
- `Systems/Space/ResourcePileDecaySystem.cs`
- `Systems/Space/ResourcePileSystem.cs`
- `Systems/Presentation/PresentationSpawnSystem.cs`

**Current Pattern (Bad):**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var ecb = new EntityCommandBuffer(Allocator.Temp);  // ❌ Direct creation
    
    foreach (var entity in SystemAPI.Query<...>())
    {
        ecb.CreateEntity(...);
    }
    
    ecb.Playback(state.EntityManager);  // ❌ Immediate playback
    ecb.Dispose();
}
```

**Recommended Pattern (Good):**
```csharp
// For systems in SimulationSystemGroup:
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);  // ✅ Singleton ECB
    
    var job = new SpawnResourceJob
    {
        ECB = ecb.AsParallelWriter()  // ✅ Parallel writer for parallel jobs
    };
    
    state.Dependency = job.ScheduleParallel(state.Dependency);
    // ✅ ECB plays back automatically at EndSimulation
}

[BurstCompile]
public partial struct SpawnResourceJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    
    public void Execute([EntityIndexInQuery] int entityInQueryIndex, ...)
    {
        var entity = ECB.CreateEntity(entityInQueryIndex, ...);  // ✅ Use entityInQueryIndex
        ECB.SetComponent(entityInQueryIndex, entity, ...);
    }
}
```

**ECB Singleton Reference:**
- **Initialization:** `BeginInitializationEntityCommandBufferSystem`, `EndInitializationEntityCommandBufferSystem`
- **Simulation:** `BeginSimulationEntityCommandBufferSystem`, `EndSimulationEntityCommandBufferSystem`
- **FixedStep:** `BeginFixedStepSimulationEntityCommandBufferSystem`, `EndFixedStepSimulationEntityCommandBufferSystem`
- **Presentation:** `BeginPresentationEntityCommandBufferSystem`, `EndPresentationEntityCommandBufferSystem`

**Migration Steps:**
1. Identify which system group the system belongs to
2. Get appropriate ECB singleton (Begin/End based on when commands execute)
3. Convert to `IJobEntity` if not already
4. Use `ECB.AsParallelWriter()` for parallel jobs
5. Remove `Playback()` and `Dispose()` calls (handled by singleton)

**Example Migration (ResourceDropSpawnerSystem):**
```csharp
// Before:
var ecb = new EntityCommandBuffer(Allocator.Temp);
foreach (var (loopState, dropConfig, transform) in SystemAPI.Query<...>())
{
    var pileEntity = ecb.CreateEntity(_pileArchetype);
    ecb.SetComponent(pileEntity, ...);
}
ecb.Playback(state.EntityManager);
ecb.Dispose();

// After:
var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

var job = new SpawnResourceDropJob
{
    ECB = ecb.AsParallelWriter(),
    PileArchetype = _pileArchetype
};

state.Dependency = job.ScheduleParallel(state.Dependency);

[BurstCompile]
public partial struct SpawnResourceDropJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public EntityArchetype PileArchetype;
    
    public void Execute([EntityIndexInQuery] int index, RefRW<MiningLoopState> loopState, ...)
    {
        if (loopState.ValueRO.Phase != MiningLoopPhase.Harvesting) return;
        
        var pileEntity = ECB.CreateEntity(index, PileArchetype);
        ECB.SetComponent(index, pileEntity, new ResourcePile { ... });
    }
}
```

---

### Fix 4: Fix Presentation Structural Changes

**Problem:** Presentation systems perform structural changes, violating DOTS 1.4 rule.

**Affected Files:**
- `Systems/Presentation/PresentationSpawnSystem.cs`
- `Systems/Presentation/PresentationRecycleSystem.cs`

**Current Pattern (Bad):**
```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct PresentationSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        // ... creates entities
        ecb.Playback(state.EntityManager);  // ❌ Structural changes in Presentation
    }
}
```

**Recommended Pattern (Good):**
```csharp
// Option A: Move to Simulation group
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PresentationSystemGroup))]
public partial struct PresentationSpawnSystem : ISystem
{
    // ... same logic, but in Simulation group
}

// Option B: Defer to next frame (queue in component)
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct PresentationSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Read-only: Process presentation requests, update visuals
        // Don't create/destroy entities here
    }
}

// Separate system in Simulation processes queue:
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PresentationSpawnProcessorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        // Process spawn queue, create entities
        // Presentation system reads results next frame
    }
}
```

**Migration Steps:**
1. Identify structural changes in Presentation systems
2. Either:
   - Move system to Simulation group (if timing allows)
   - Or split into Simulation (spawn) + Presentation (visual update)
3. Use singleton ECB systems for structural changes

---

## Low Priority Optimizations

### Fix 5: Optimize NativeList Allocations

**Problem:** Systems create `NativeList` allocations every frame (though with `Allocator.TempJob`).

**Affected File:** `Systems/AI/AISystems.cs` (lines 139-143)

**Current Pattern (Acceptable but Optimizable):**
```csharp
var descriptorList = new NativeList<SpatialQueryDescriptor>(Allocator.TempJob);
var rangeList = new NativeList<SpatialQueryRange>(Allocator.TempJob);
// ... used in job, disposed after
```

**Recommended Pattern (If Size Predictable):**
```csharp
// Cache lists if size is predictable
private NativeList<SpatialQueryDescriptor> _descriptorList;
private NativeList<SpatialQueryRange> _rangeList;

[BurstCompile]
public void OnCreate(ref SystemState state)
{
    _descriptorList = new NativeList<SpatialQueryDescriptor>(64, Allocator.Persistent);
    _rangeList = new NativeList<SpatialQueryRange>(64, Allocator.Persistent);
}

[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    _descriptorList.Clear();  // Reuse instead of allocating
    _rangeList.Clear();
    // ... use lists
}

public void OnDestroy(ref SystemState state)
{
    _descriptorList.Dispose();
    _rangeList.Dispose();
}
```

**Note:** `Allocator.TempJob` is acceptable for job-local allocations. Only optimize if profiling shows it's a bottleneck.

---

## Rewind Compatibility Considerations

### Rewind-Safe ECB Usage

**When using singleton ECB systems:**
- ECB commands are automatically recorded for rewind
- Commands play back deterministically during rewind
- No special handling needed for rewind compatibility

**When using direct ECB (legacy pattern):**
- Must ensure commands are recorded for rewind
- May need manual rewind handling
- **Recommendation:** Migrate to singleton ECB systems

### Rewind Guard Patterns

**Current Pattern (Good):**
```csharp
public void OnUpdate(ref SystemState state)
{
    var rewindState = SystemAPI.GetSingleton<RewindState>();
    if (rewindState.Mode != RewindMode.Record)
    {
        return;  // ✅ Skip during playback
    }
    
    // Process logic
}
```

**Keep this pattern** - it's correct for rewind compatibility.

---

## Godgame-Specific Recommendations

### Rendering Optimization

**For 100k+ 3D villagers:**

1. **Use Entities Graphics (Hybrid Renderer V2)**
   - Convert villagers to use `RenderMesh` component
   - Use `MaterialMeshInfo` for instancing
   - Implement LOD system with `LODGroup` component

2. **Culling System**
   ```csharp
   [UpdateInGroup(typeof(PresentationSystemGroup))]
   [UpdateBefore(typeof(EntitiesGraphicsSystem))]
   public partial struct VillagerCullingSystem : ISystem
   {
       public void OnUpdate(ref SystemState state)
       {
           // Read-only: Update culling flags based on camera
           // Entities Graphics handles actual culling
       }
   }
   ```

3. **LOD System**
   - Add `LODComponent` to villagers
   - Update LOD level in Presentation (read-only)
   - Use different meshes/materials per LOD

### Presentation Layer Architecture

**Recommended Structure:**
```
SimulationSystemGroup
  └─ PresentationSpawnProcessorSystem (creates entities)
  
PresentationSystemGroup
  └─ PresentationSpawnSystem (updates visuals, read-only)
  └─ VillagerCullingSystem (updates culling flags, read-only)
  └─ EntitiesGraphicsSystem (rendering)
```

---

## Space4X-Specific Recommendations

### UI-Only Entity Pattern

**For pops (no 3D rendering):**

1. **Lightweight Components**
   ```csharp
   // Pop entity: Data-only, no rendering components
   Entity popEntity;
   AddComponent(popEntity, new VillagerAlignment { ... });
   AddComponent(popEntity, new VillagerBehavior { ... });
   // NO RenderMesh, LocalTransform (if not needed), etc.
   ```

2. **UI System Reads ECS**
   ```csharp
   [UpdateInGroup(typeof(PresentationSystemGroup))]
   public partial struct PopUISystem : ISystem
   {
       public void OnUpdate(ref SystemState state)
       {
           // Read-only: Query pops, update UI
           foreach (var (alignment, behavior, entity) in SystemAPI.Query<...>())
           {
               // Update UI panel/card
           }
       }
   }
   ```

3. **Ship Representation**
   ```csharp
   // Ship entity: Has presentation components
   Entity shipEntity;
   AddComponent(shipEntity, new RenderMesh { ... });
   AddComponent(shipEntity, new ShipPresentation { ... });
   
   // Pop assigned to ship: Reference only
   AddComponent(popEntity, new Space4XShipAssignment { ShipEntity = shipEntity });
   ```

### Aggregate Visualization

**For planets/fleets/sectors:**
- Aggregate entities compute alignment/behavior from members
- UI systems read aggregate data, display in panels
- Ship collections represent aggregates visually
- No individual pop rendering needed

---

## Testing Checklist

### After Each Fix

- [ ] Verify system compiles
- [ ] Verify Burst compilation (check Burst Inspector)
- [ ] Profile GC allocations (should be zero for gameplay systems)
- [ ] Test with 100k+ entities
- [ ] Verify rewind/playback still works
- [ ] Check system group ordering

### Performance Benchmarks

- [ ] Measure frame time with 100k entities
- [ ] Measure GC allocations per frame (target: zero)
- [ ] Measure memory usage
- [ ] Verify deterministic behavior (rewind test)

---

## Migration Priority

1. **Week 1:** Fix high-priority issues (ToEntityArray, Input system)
2. **Week 2:** Migrate ECB systems (medium priority)
3. **Week 3:** Fix presentation structural changes
4. **Week 4:** Optimize and profile (low priority)

---

**Related Documentation:**
- DOTS 1.4 Compliance Audit: `Docs/Audit/DOTS_1.4_Compliance_Audit.md`
- Unity DOTS 1.4 Documentation
- PureDOTS System Groups: `Systems/SystemGroups.cs`

---

**Last Updated:** 2025-01-XX  
**Status:** Implementation Guide - Ready for Execution

