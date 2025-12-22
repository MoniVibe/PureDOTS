# Liquid-Terrain Integration

**Status:** Concept
**Category:** Core - Unified Terrain & Fluid System
**Scope:** Cross-Project (PureDOTS Foundation)
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Integrate diggable terrain and liquid dynamics into a unified, data-driven system where both use chunked fields + dirty updates, enabling efficient terrain editing, fluid simulation, and agent reactions without global rebakes or expensive computations.

**Secondary Goals:**
- Shared dirty region system (terrain edits → dirty regions → derived systems update)
- Compatible data model (terrain + liquids in same chunk structure)
- Efficient update order (terrain → nav → fluids → hazards)
- Cheap agent reactions (sample liquid state, no fluid reasoning in AI)
- Underground tunnel integration (conduit graph + surface water)
- Boat/floating entity support (buoyancy probes + heightfield water)

**Key Principle:** Terrain edits only create "dirty regions." Everything derived (nav, liquids, hazards) recomputes only in/around those regions. No rebakes, no global sims, no GPU compute needed.

---

## One Shared Rule

### Core Design Principle

**Terrain edits only create "dirty regions". Everything derived (nav, liquids, hazards) recomputes only in/around those regions.**

- No rebakes (nav graphs update incrementally)
- No global sims (only active/dirty regions)
- No GPU compute needed (CPU-friendly, Burst-compatible)

This rule ensures both systems scale efficiently and work together seamlessly.

---

## 1. Data Model (Compatible Structure)

### Core Concept

**Per terrain cell (chunked arrays, not entities) holds both terrain and liquid data in a unified structure.**

### Unified Cell Data

```csharp
// Terrain cell (chunked array, not entities)
public struct TerrainCell
{
    // Terrain data
    public float GroundHeight;               // Height (surface) or solidMask (underground voxel)
    public byte MaterialId;                  // Soil/rock/ore → dig yield/permeability
    public byte SolidMask;                   // Voxel occupancy (underground: 1=solid, 0=empty)
    
    // Liquid data
    public float WaterDepth;                 // Liquid layer (conserved scalar)
    
    // Edge/connectivity data
    public byte EdgeBlock;                   // 0=passable, 255=blocked (dams, sealed rock)
    public byte Conductivity;                // 0-255: flow conductivity (gates, sealed doors)
}

// Chunk structure (reuses existing chunk patterns)
public struct TerrainLiquidChunk : IComponentData
{
    public int3 ChunkCoord;
    public BlobAssetReference<TerrainLiquidChunkBlob> BaseBlob; // Base terrain/liquid data
    public uint TerrainVersion;              // For invalidation tracking
}

public struct TerrainLiquidChunkBlob
{
    public BlobArray<TerrainCell> Cells;
}

// Derived per cell (cached, recomputed when dirty)
public struct DerivedCellData
{
    public float NavCost;                    // Pathfinding cost
    public byte WalkableMask;                // 0=unwalkable, 1=walkable
    public float FloodHazard;                // waterDepth + riseRate + distance to dry
}
```

### Material Properties

```csharp
// Material specification (affects both terrain and liquids)
public struct TerrainMaterialSpec
{
    public FixedString64Bytes MaterialId;
    
    // Digging properties
    public float DigTimeMultiplier;
    public BlobAssetReference<ResourceYieldBlob> Yields;
    
    // Liquid properties
    public float WaterPermeability;          // 0-1: allows water through?
    public float Conductivity;               // Flow rate through material
    
    // Nav properties
    public float NavCostModifier;            // Mud, rubble, etc. affect pathfinding
}
```

**Key:** Same cell structure holds both terrain and liquid data. Digging changes `GroundHeight`/`SolidMask` → marks dirty → liquids flow → nav updates.

---

## 2. Update Order (What Your Agents Implement)

### Core Concept

**Per sim tick (or slower, e.g., 5-20 Hz for fluids): Apply terrain deltas → Nav incremental update → Fluid step → Hazard cache update.**

### Update Pipeline

```csharp
// Update order system
public struct TerrainLiquidUpdateSystem : ISystem
{
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    void OnUpdate(ref SystemState state)
    {
        // Step 1: Apply Terrain Deltas
        ApplyTerrainDeltas();
        
        // Step 2: Nav Incremental Update
        UpdateNavIncremental();
        
        // Step 3: Fluid Step (Active Set only)
        UpdateFluidStep();
        
        // Step 4: Hazard Cache Update
        UpdateHazardCache();
    }
}
```

### Step 1: Apply Terrain Deltas

```csharp
void ApplyTerrainDeltas()
{
    // Dig/build dam/tunnel edits mutate groundH/solidMask/edgeBlock
    foreach (var delta in GetTerrainDeltas())
    {
        var chunk = GetTerrainChunk(delta.ChunkId);
        var cell = chunk.Cells[delta.LocalIndex];
        
        // Apply delta
        cell.GroundHeight = delta.NewHeight;
        cell.SolidMask = delta.NewSolidMask;
        cell.EdgeBlock = delta.NewEdgeBlock;
        chunk.Cells[delta.LocalIndex] = cell;
        
        // Mark dirty rects
        MarkDirtyRegion(delta.ChunkId, delta.LocalIndex, DirtyFlags.All);
    }
}

// Dirty flags
[Flags]
public enum DirtyFlags : byte
{
    None = 0,
    Terrain = 1 << 0,        // Terrain changed
    Nav = 1 << 1,            // Navigation needs update
    Fluid = 1 << 2           // Fluid needs update
}
```

### Step 2: Nav Incremental Update

```csharp
void UpdateNavIncremental()
{
    // Recompute walkable/cost only for dirty cells (+ 1-cell border)
    foreach (var chunk in GetDirtyNavChunks())
    {
        var dirtyCells = GetDirtyCells(chunk, NavDirtyFlag);
        
        foreach (var cellIdx in dirtyCells)
        {
            var cell = chunk.Cells[cellIdx];
            var derived = GetDerivedData(chunk, cellIdx);
            
            // Recompute nav cost (terrain + water affect cost)
            float baseCost = GetMaterialNavCost(cell.MaterialId);
            float waterCost = ComputeWaterNavCost(cell.WaterDepth);
            float slopeCost = ComputeSlopeCost(chunk, cellIdx);
            derived.NavCost = baseCost + waterCost + slopeCost;
            
            // Recompute walkability
            bool walkable = IsWalkable(cell.GroundHeight, cell.WaterDepth, cell.SolidMask);
            derived.WalkableMask = walkable ? (byte)1 : (byte)0;
            
            SetDerivedData(chunk, cellIdx, derived);
        }
        
        // Bump NavTileVersion for those tiles (paths self-invalidate)
        var navTile = GetNavTile(chunk.ChunkCoord);
        navTile.Version++;
    }
}
```

### Step 3: Fluid Step (Active Set Only)

```csharp
void UpdateFluidStep()
{
    // Heightfield water: move water from higher surface=groundH+waterDepth to lower neighbors
    // Only update active cells near dirty areas / near water / near sources
    
    var activeCells = GetActiveFluidCells();
    
    foreach (var cellIdx in activeCells)
    {
        var chunk = GetTerrainChunk(cellIdx.ChunkId);
        var cell = chunk.Cells[cellIdx.LocalIndex];
        
        // Compute surface height
        float surfaceH = cell.GroundHeight + cell.WaterDepth;
        
        // Flow to neighbors (4-neighbor for 2D, 6-neighbor for 3D)
        var neighbors = GetNeighbors(chunk, cellIdx.LocalIndex);
        foreach (var neighborIdx in neighbors)
        {
            var neighborCell = chunk.Cells[neighborIdx];
            float neighborSurfaceH = neighborCell.GroundHeight + neighborCell.WaterDepth;
            
            // Flow from higher to lower
            if (surfaceH > neighborSurfaceH)
            {
                float d = surfaceH - neighborSurfaceH;
                float flow = ComputeFlow(d, cell.Conductivity, neighborCell.Conductivity);
                
                // Apply flow (limited by edge blocks)
                if (CanFlow(cell, neighborCell, direction))
                {
                    cell.WaterDepth -= flow;
                    neighborCell.WaterDepth += flow;
                    
                    // Mark neighbor active (newly affected)
                    MarkCellActive(neighborIdx);
                }
            }
        }
        
        chunk.Cells[cellIdx.LocalIndex] = cell;
    }
    
    // Mark newly affected neighbors active (expansion)
    ExpandActiveSet();
}

// Active set criteria
bool IsActiveFluidCell(int3 chunkCoord, int cellIdx)
{
    var cell = GetCell(chunkCoord, cellIdx);
    
    // Active if:
    // - Near dirty areas
    // - Near water (waterDepth > threshold)
    // - Near sources (springs, rain, etc.)
    // - Recently changed (neighbor flow affected it)
    
    if (IsDirty(chunkCoord, cellIdx))
        return true;
    if (cell.WaterDepth > 0.01f)
        return true;
    if (HasSourceNearby(chunkCoord, cellIdx))
        return true;
    
    return false;
}
```

### Step 4: Hazard Cache Update

```csharp
void UpdateHazardCache()
{
    // For dirty fluid areas, compute:
    // - waterDepth (already in cell)
    // - depthRate (delta over last N fluid steps)
    // - distanceToDry (cheap multi-source BFS from "dry" cells)
    
    foreach (var chunk in GetDirtyFluidChunks())
    {
        var dirtyCells = GetDirtyCells(chunk, FluidDirtyFlag);
        
        foreach (var cellIdx in dirtyCells)
        {
            var cell = chunk.Cells[cellIdx];
            var derived = GetDerivedData(chunk, cellIdx);
            
            // Compute depth rate (how fast water is rising)
            float previousDepth = GetPreviousDepth(chunk, cellIdx);
            float depthRate = (cell.WaterDepth - previousDepth) / FluidStepInterval;
            
            // Compute distance to dry (cheap multi-source BFS from "dry" cells)
            float distanceToDry = ComputeDistanceToDry(chunk, cellIdx);
            
            // Compute flood hazard
            float hazard = cell.WaterDepth * 0.5f + 
                          math.max(0f, depthRate) * 2.0f + 
                          distanceToDry * 0.1f;
            derived.FloodHazard = hazard;
            
            SetDerivedData(chunk, cellIdx, derived);
        }
    }
}

// Cheap multi-source BFS (local region only)
float ComputeDistanceToDry(TerrainChunk chunk, int cellIdx)
{
    // Start BFS from current cell
    // Find nearest "dry" cell (waterDepth < threshold)
    // Return distance (in cells)
    
    // Limit search to local region (e.g., 10×10 cells) to keep it cheap
    const int MaxSearchRadius = 10;
    
    var queue = new NativeQueue<int>(Allocator.Temp);
    var visited = new NativeHashSet<int>(Allocator.Temp);
    var distances = new NativeHashMap<int, int>(Allocator.Temp);
    
    queue.Enqueue(cellIdx);
    distances[cellIdx] = 0;
    
    while (queue.Count > 0)
    {
        int current = queue.Dequeue();
        int dist = distances[current];
        
        if (dist > MaxSearchRadius)
            continue;
        
        var cell = chunk.Cells[current];
        if (cell.WaterDepth < DryThreshold)
        {
            return dist; // Found dry cell
        }
        
        var neighbors = GetNeighbors(chunk, current);
        foreach (var neighbor in neighbors)
        {
            if (!visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                distances[neighbor] = dist + 1;
                queue.Enqueue(neighbor);
            }
        }
    }
    
    return MaxSearchRadius; // No dry cell found nearby
}
```

**Result:** This is what makes agents "understand cavities fill" without doing "fluid reasoning" in their brains. Agents query precomputed hazard cache.

---

## 3. How Agents React to Filling Cavities (Cheap + Believable)

### Core Concept

**Agents should not predict fluid dynamics themselves. They read a small sample: LiquidSample at position.**

### Liquid Sample API

```csharp
// Liquid sample (cheap query, no fluid reasoning)
public struct LiquidSample
{
    public float Depth;                      // Current water depth
    public float DepthRate;                  // How fast water is rising (m/s)
    public bool IsPassable;                  // Can wade/swim through?
    public float3 EscapeGradient;            // Optional: direction to decreasing hazard
}

// Sample liquid state at position
LiquidSample SampleLiquid(float3 worldPosition)
{
    var chunk = GetTerrainChunk(worldPosition);
    var cellIdx = WorldToCellIndex(worldPosition);
    var cell = chunk.Cells[cellIdx];
    var derived = GetDerivedData(chunk, cellIdx);
    
    return new LiquidSample
    {
        Depth = cell.WaterDepth,
        DepthRate = GetDepthRate(chunk, cellIdx),
        IsPassable = CanWadeOrSwim(cell.WaterDepth),
        EscapeGradient = ComputeEscapeGradient(chunk, cellIdx)
    };
}

bool CanWadeOrSwim(float waterDepth)
{
    const float WadeThreshold = 0.3f;  // Can wade up to 0.3m
    const float SwimThreshold = 1.5f;  // Must swim above 1.5m
    
    return waterDepth < SwimThreshold; // Can wade or swim
}

float3 ComputeEscapeGradient(TerrainChunk chunk, int cellIdx)
{
    // Gradient toward decreasing hazard (direction to safety)
    var derived = GetDerivedData(chunk, cellIdx);
    float currentHazard = derived.FloodHazard;
    
    var neighbors = GetNeighbors(chunk, cellIdx);
    float3 gradient = float3.zero;
    
    foreach (var neighborIdx in neighbors)
    {
        var neighborDerived = GetDerivedData(chunk, neighborIdx);
        float neighborHazard = neighborDerived.FloodHazard;
        
        if (neighborHazard < currentHazard)
        {
            float3 direction = GetDirectionToNeighbor(cellIdx, neighborIdx);
            float hazardReduction = currentHazard - neighborHazard;
            gradient += direction * hazardReduction;
        }
    }
    
    return math.normalize(gradient); // Normalized direction to safety
}
```

### Behavior Rules

```csharp
// Agent behavior based on liquid sample
void ReactToLiquid(Entity agent, float3 position)
{
    var sample = SampleLiquid(position);
    var locomotion = GetComponentRW<ActiveLocomotion>(agent);
    var intent = GetComponentRW<EntityIntent>(agent);
    
    // Rule 1: If depth > wadeThreshold → switch locomotion to swim
    if (sample.Depth > WadeThreshold)
    {
        locomotion.ValueRW.CurrentMode = LocomotionMode.Swimming;
    }
    
    // Rule 2: If depth > headHeight or (depthRate rising fast and predicted t_fill < T) → evacuate
    const float HeadHeight = 1.8f;  // Agent head height
    const float FillTimeThreshold = 10f; // Evacuate if will fill in 10 seconds
    float predictedFillTime = sample.Depth / math.max(sample.DepthRate, 0.001f);
    
    if (sample.Depth > HeadHeight || 
        (sample.DepthRate > 0.1f && predictedFillTime < FillTimeThreshold))
    {
        // Evacuate
        intent.ValueRW.Mode = IntentMode.Flee;
        intent.ValueRW.TargetPosition = ComputeEvacuationTarget(agent, sample);
    }
}

// Evacuation path uses nav with a "water hazard" cost layer
float3 ComputeEvacuationTarget(Entity agent, LiquidSample sample)
{
    // High cost in deep/rising water
    // Low cost toward dry/shallower cells
    // If no path: seek high ground (maximize groundH locally)
    
    float3 currentPos = GetPosition(agent);
    var nav = GetNavGraph();
    
    // Try to find path toward escape gradient
    if (math.lengthsq(sample.EscapeGradient) > 0.1f)
    {
        float3 targetPos = currentPos + sample.EscapeGradient * 10f; // 10m toward safety
        var path = FindPathWithHazardCost(currentPos, targetPos);
        if (path.IsValid)
        {
            return targetPos;
        }
    }
    
    // Fallback: seek high ground (maximize groundH locally)
    return FindHighGround(currentPos, SearchRadius);
}

// Pathfinding with water hazard cost layer
NavPath FindPathWithHazardCost(float3 start, float3 goal)
{
    // Standard A* pathfinding, but cost includes:
    // - Base nav cost
    // - Water hazard cost (high cost in deep/rising water)
    // - Low cost toward dry/shallower cells
    
    return AStarPathfind(start, goal, (cell) =>
    {
        float baseCost = GetNavCost(cell);
        float hazardCost = GetFloodHazard(cell) * HazardCostMultiplier;
        return baseCost + hazardCost;
    });
}
```

**Result:** "Oh no it's flooding" reactions with constant-time queries per agent. No fluid reasoning in AI.

---

## 4. Underground Tunnels / Siphoning (Still Compatible)

### Core Concept

**Don't do 3D fluids everywhere. Use the conduit graph you already planned.** Tunnels/burrows create conduit edges. Surface↔tunnel entrances are links.

### Integration

```csharp
// Tunnels/burrows create conduit edges (see Fluid_Dynamics_System.md: Conduit Graph)
// Surface↔tunnel entrances are links

// Water drains into tunnels if surface water exceeds entrance elevation
void CheckWaterTunnelConnection(float3 surfacePos, float3 tunnelEntrancePos)
{
    float surfaceHeight = GetGroundHeight(surfacePos) + GetWaterDepth(surfacePos);
    float entranceHeight = GetConduitEntranceHeight(tunnelEntrancePos);
    
    if (surfaceHeight > entranceHeight)
    {
        // Surface water drains into tunnel
        float excess = surfaceHeight - entranceHeight;
        float transfer = excess * CouplingRate * DeltaTime;
        
        // Transfer water from surface to tunnel conduit node
        DrainWaterFromSurface(surfacePos, transfer);
        AddWaterToConduit(tunnelEntrancePos, transfer);
        
        // Mark dirty (surface and tunnel both affected)
        MarkDirtyRegion(surfacePos, DirtyFlags.Fluid);
        MarkDirtyRegion(tunnelEntrancePos, DirtyFlags.Fluid);
    }
}

// Backflow can flood out if tunnel head rises
void CheckTunnelBackflow(float3 tunnelEntrancePos, ConduitNode tunnelNode)
{
    float tunnelHead = tunnelNode.InvertHeight + tunnelNode.WaterVolume / tunnelNode.Capacity;
    float entranceHeight = GetConduitEntranceHeight(tunnelEntrancePos);
    
    if (tunnelHead > entranceHeight)
    {
        // Tunnel water backs up to surface
        float excess = tunnelHead - entranceHeight;
        float transfer = excess * CouplingRate * DeltaTime;
        
        // Transfer water from tunnel to surface
        DrainWaterFromConduit(tunnelNode, transfer);
        AddWaterToSurface(tunnelEntrancePos, transfer);
        
        // Mark dirty
        MarkDirtyRegion(tunnelEntrancePos, DirtyFlags.Fluid);
    }
}

// Terrain edits that open/close tunnels just:
void OnTunnelEdit(TerrainDelta delta)
{
    // Update conduit links
    UpdateConduitLinks(delta);
    
    // Mark a small dirty neighborhood for fluids
    MarkDirtyRegion(delta.ChunkId, delta.LocalIndex, DirtyFlags.Fluid);
}
```

**Integration:** Uses existing Fluid Dynamics System conduit graph. Terrain edits update conduit links and mark dirty regions (no global recomputation).

---

## 5. Boats / Floating Ships (Works Great with Heightfield Water)

### Core Concept

**Since water is a surface heightfield, buoyancy is easy and Burst-friendly.** Use buoyancy probes to sample water surface.

### Buoyancy Probe System

```csharp
// Boat entity with buoyancy probes
public struct FloatingEntity : IComponentData
{
    public BlobAssetReference<HullBuoyancyProbeBlob> Probes;
}

// Hull buoyancy probe (4-12 points in local space)
public struct HullBuoyancyProbe
{
    public float3 LocalPosition;            // Position relative to entity center
    public float EffectiveArea;             // Area this probe represents
}

// Buoyancy computation (Burst-friendly)
void UpdateBuoyancy(Entity boat)
{
    var transform = GetComponentRO<LocalTransform>(boat);
    var probes = GetComponentRO<FloatingEntity>(boat);
    var physics = GetComponentRW<PhysicsVelocity>(boat);
    
    float3 totalBuoyantForce = float3.zero;
    float3 centerOfBuoyancy = float3.zero;
    float totalSubmergedArea = 0f;
    
    // Sample water surface at each probe
    foreach (var probe in probes.Probes.Value.Probes)
    {
        float3 worldPos = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, probe.LocalPosition);
        
        // Sample water surface height
        float waterSurfaceH = SampleWaterSurfaceHeight(worldPos);
        
        // Compute submersion depth
        float probeHeight = worldPos.y;
        float submersionDepth = math.max(0f, waterSurfaceH - probeHeight);
        
        if (submersionDepth > 0f)
        {
            // Compute buoyant force for this probe
            // F = ρ * g * submergedVolumeApprox
            const float WaterDensity = 1000f;  // kg/m³
            const float Gravity = 9.81f;       // m/s²
            float submergedVolume = submersionDepth * probe.EffectiveArea;
            float buoyantForce = WaterDensity * Gravity * submergedVolume;
            
            totalBuoyantForce.y += buoyantForce;
            centerOfBuoyancy += worldPos * probe.EffectiveArea;
            totalSubmergedArea += probe.EffectiveArea;
        }
    }
    
    if (totalSubmergedArea > 0f)
    {
        centerOfBuoyancy /= totalSubmergedArea;
        
        // Apply buoyant force
        physics.ValueRW.ApplyLinearImpulse(totalBuoyantForce * DeltaTime, centerOfBuoyancy);
        
        // Add drag against relative water velocity
        float3 waterVelocity = SampleWaterVelocity(transform.ValueRO.Position);
        float3 relativeVelocity = physics.ValueRO.Linear - waterVelocity;
        float3 dragForce = -relativeVelocity * DragCoefficient * totalSubmergedArea;
        physics.ValueRW.ApplyLinearImpulse(dragForce * DeltaTime);
    }
}
```

### LOD Integration

```csharp
// Tier0 (near camera): Unity Physics rigidbody + forces
if (GetLOD(boat) == LODTier.Tier0)
{
    // Full physics integration (already handled by UpdateBuoyancy)
}

// Tier1+: kinematic integration (cheap) using the same probes
else
{
    // Kinematic approximation (no rigidbody, just position updates)
    float3 buoyantAccel = totalBuoyantForce / Mass;
    float3 newVelocity = currentVelocity + buoyantAccel * DeltaTime;
    float3 newPosition = currentPosition + newVelocity * DeltaTime;
    SetPosition(boat, newPosition);
}
```

**Result:** No GPU cost besides rendering the water surface. Buoyancy is CPU-friendly and Burst-compatible.

---

## 6. Performance Guardrails (So Nothing "Blows Up")

### Core Constraints

**1. Active Set Only for Fluids:**
- No full-map sweeps
- Only update active cells (near dirty areas, near water, near sources)
- Expand active set when neighbors affected

**2. Low-Rate Fluid Tick:**
- Independent from sim tick (e.g., 5-20 Hz for fluids)
- Configurable update interval

```csharp
public struct FluidUpdateConfig : IComponentData
{
    public float UpdateIntervalSeconds;  // Default: 0.1s (10 Hz)
    public uint LastUpdateTick;
}
```

**3. Cap Flow Per Step:**
- Prevents oscillation/explosions
- Limit maximum flow per cell per step

```csharp
float ComputeFlow(float heightDiff, float conductivity1, float conductivity2)
{
    float baseFlow = heightDiff * FlowCoefficient;
    float conductivity = math.min(conductivity1, conductivity2); // Bottleneck
    float flow = baseFlow * conductivity;
    
    // Cap flow per step (prevents oscillation)
    flow = math.min(flow, MaxFlowPerStep);
    
    return flow;
}
```

**4. Chunk LOD:**
- Far chunks update slower
- Tier 0 (near camera): full update
- Tier 1+: reduced update cadence
- Tier 2+: event-driven only

**5. Derived Caches:**
- Nav + hazard update only in dirty regions
- No global recomputation

**6. Path Invalidation:**
- Paths invalidated by tile versions
- Replan only when needed (not every tick)

---

## 7. Practical Next Steps

### Implementation Order

**Step 1: Terrain Deltas + Dirty Region Propagation (Shared Service)**
- Implement `TerrainDelta` system (from Diggable_Terrain_System.md)
- Implement `DirtyRegion` marking system (shared by terrain, nav, fluids)
- Wire terrain edits to mark dirty regions

**Step 2: Heightfield WaterDepth + Active-Set Solver**
- Implement `TerrainCell` with `WaterDepth`
- Implement `FluidStep` system (active set only)
- Implement flow computation (height difference → flow)
- Implement active set expansion

**Step 3: Wire Nav to Include Water Hazard Cost Layer**
- Extend nav cost computation to include water hazard
- Add depth thresholds (wade threshold, swim threshold)
- Implement hazard-based pathfinding

**Step 4: Implement LiquidSample and Use It to Trigger Swim/Evacuate Intents**
- Implement `LiquidSample` API (sample liquid state)
- Wire agent behavior to react to liquid samples
- Implement evacuation logic (flee to high ground)

**Step 5: Add Buoyancy Probes for Boats and Small Ships**
- Implement `HullBuoyancyProbe` system
- Implement buoyancy computation (probe-based)
- Implement drag (relative water velocity)
- Add LOD support (Tier0: physics, Tier1+: kinematic)

**Step 6 (Later): Conduit Graph for Tunnels + Dams/Gates**
- Implement conduit graph (from Fluid_Dynamics_System.md)
- Implement surface↔tunnel coupling
- Implement dams/gates as `EdgeBlock`/`Conductivity` modifiers

---

## Integration Summary

### Unified Data Model

- **TerrainCell:** Holds both terrain (`GroundHeight`, `SolidMask`, `MaterialId`) and liquid (`WaterDepth`) data
- **DerivedCellData:** Cached nav and hazard data (recomputed when dirty)

### Shared Dirty Region System

- **DirtyFlags:** Terrain, Nav, Fluid (independent dirty marking)
- **Update Order:** Terrain Deltas → Nav Update → Fluid Step → Hazard Cache

### Agent Integration

- **LiquidSample API:** Cheap queries, no fluid reasoning in AI
- **Behavior Rules:** React to depth, depth rate, evacuation logic
- **Pathfinding:** Includes water hazard cost layer

### Underground Integration

- **Conduit Graph:** Reuses Fluid_Dynamics_System.md pattern
- **Surface↔Tunnel Coupling:** Water drains/floods through entrances
- **Terrain Edits:** Update conduit links, mark dirty regions

### Performance

- **Active Set:** Only update active fluid cells
- **Low-Rate Tick:** Fluids update at 5-20 Hz (independent from sim)
- **Capped Flow:** Prevents oscillation/explosions
- **Chunk LOD:** Far chunks update slower
- **Versioned Paths:** Replan only when needed

---

## Related Documentation

- **Fluid Dynamics System:** `Docs/Concepts/Core/Fluid_Dynamics_System.md` - Conduit graph, heightfield water
- **Diggable Terrain System:** `Docs/Concepts/Core/Diggable_Terrain_System.md` - Chunked terrain, dig commands
- **Capabilities & Affordances:** `Docs/Concepts/Core/Capabilities_And_Affordances_System.md` - Swim/dive traversal
- **Simulation LOD:** `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` - LOD framework

---

**For Implementers:** Focus on unified TerrainCell structure, shared dirty region system, active-set fluid solver, and LiquidSample API  
**For Designers:** Focus on evacuation thresholds, hazard cost tuning, and boat buoyancy parameters

