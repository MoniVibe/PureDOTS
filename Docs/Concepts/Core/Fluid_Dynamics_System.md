# Fluid Dynamics System

**Status:** Concept
**Category:** Core - Environmental Simulation
**Scope:** Cross-Project (Godgame: Rivers/Lakes/Flooding, Space4X: Ship Interiors/Compartments)
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Implement heightfield water simulation (2.5D) with conduit graph for underground flows, using sparse/dirty updates to achieve "holes fill, overflow, rivers form, dams work, tunnels siphon" with predictable cost.

**Secondary Goals:**
- Integrate with existing SurfaceFieldsChunk and EnvironmentGrid infrastructure
- Support terrain modifications (digging, damming) through dirty region marking
- Enable multiple liquids (water, lava, oil) with minimal complexity
- Scale efficiently using active set updates (not full-grid every tick)
- Provide visual representation suitable for gameplay (not full CFD)

**Key Principle:** Use heightfield water (2.5D) + conduit graph (for tunnels), not full 3D voxel simulation. Predictable cost, stable simulation, deterministic for replay.

---

## System Overview

### Architecture Pattern

**Heightfield Water (2.5D):**
- Water stored as depth/volume per terrain cell
- Surface = `groundHeight + waterDepth`
- Flow from higher surface to lower surface
- Integrates with existing terrain height data

**Conduit Graph (Underground/Tunnels):**
- Graph-based network for tunnels/burrows (not full 3D voxels)
- Nodes = tunnel junctions, Edges = tunnel segments
- Surface ↔ tunnel coupling at entrance points
- Siphoning through terrain without 3D grid overhead

**Active Set Updates:**
- Only update cells with activity (water changed, terrain edited, neighbors active)
- Dirty region expansion for flow propagation
- Deactivate cells when stable (water below threshold, neighbors stable)

---

## Heightfield Water Representation

### Chunk-Based Structure (Integration with SurfaceFieldsChunk)

**Leverages Existing Infrastructure:**
- Uses `SurfaceFieldsChunkBlob` structure (int3 ChunkCoord, int2 CellsPerChunk)
- Reuses `EnvironmentGridMetadata` pattern for grid resolution/cell size
- Integrates with `SurfaceFieldsChunkComponent` / `SurfaceFieldsChunkCleanup`

**Per-Cell Data (WaterGridChunkBlob):**

```csharp
public struct WaterCellBlob
{
    public float GroundHeight;      // Terrain height (from SurfaceFieldsChunkBlob.HeightQ)
    public float WaterDepth;        // Water depth/volume (0+)
    public byte SolidMask;          // 0 = passable, 255 = wall/dam/rock/sealed
    public byte Conductivity;       // 0-255: how easily water passes (default 255 = full)
}

public struct WaterGridChunkBlob
{
    public uint SchemaVersion;
    public int3 ChunkCoord;         // Matches SurfaceFieldsChunkBlob.ChunkCoord
    public int2 CellsPerChunk;      // Matches SurfaceFieldsChunkBlob.CellsPerChunk
    public float CellSize;          // Meters per cell
    
    public BlobArray<WaterCellBlob> Cells;
    
    // Active set tracking (runtime, stored in component buffer)
    // Dirty regions tracked per-chunk
}
```

**Surface Calculation:**
```csharp
float surfaceHeight = cell.GroundHeight + cell.WaterDepth;
```

**Integration with Existing Terrain:**
- GroundHeight initially populated from `SurfaceFieldsChunkBlob.HeightQ` (quantized height)
- Terrain modifications update GroundHeight, mark cells dirty
- Uses existing `TerrainVersion` singleton for invalidation

### Runtime Cell Buffer (For Dynamic Updates)

**Similar to MoistureGridRuntimeCell:**

```csharp
[InternalBufferCapacity(0)]
public struct WaterGridRuntimeCell : IBufferElementData
{
    public float WaterDepth;        // Dynamic water depth
    public byte ActiveFlag;         // 0 = inactive, 1 = active (in active set)
    public byte DirtyFlag;          // 0 = clean, 1 = dirty (needs update)
    public byte FlowDirection;      // Bitflags: N/S/E/W (for visualization)
}
```

**Component Structure:**

```csharp
public struct WaterGridChunkComponent : IComponentData
{
    public int3 ChunkCoord;
    public BlobAssetReference<WaterGridChunkBlob> BaseBlob;  // GroundHeight, initial state
    public uint LastUpdateTick;
    public uint TerrainVersion;     // For invalidation on terrain edits
}

// Runtime cell buffer attached to chunk entity
// DynamicBuffer<WaterGridRuntimeCell> RuntimeCells;
```

---

## Flow Rule (Cheap, Stable, Deterministic)

### Core Algorithm

**Per-Cell Flow Calculation:**

```csharp
// For each active cell i, for each neighbor j (4-neighborhood: N/S/E/W)
float hi = groundHeight[i] + waterDepth[i];  // Surface height
float hj = groundHeight[j] + waterDepth[j];
float d = hi - hj;  // Height difference

if (d <= 0) continue;  // No flow if neighbor is higher or equal

// Maximum transferable this step
float cap = waterDepth[i];

// Fractional equalization (prevents oscillation)
float kFlow = 0.25f;  // Typical: 0.1 to 0.5 (controls speed/stability)
float flow = math.min(cap, d * kFlow);

// Clamp by edge conductivity (dam/wall blocks flow)
float conductivity = GetEdgeConductivity(i, j);  // 0-1
flow *= conductivity;

flowBuffer[i->j] += flow;
```

**Apply Flows (Second Pass):**

```csharp
// Compute net flow for each cell
for each cell i:
    float sumOut = sum(flowBuffer[i->j] for all neighbors j);
    float sumIn = sum(flowBuffer[j->i] for all neighbors j);
    
    waterDepth[i] = waterDepth[i] - sumOut + sumIn;
    waterDepth[i] = math.max(0f, waterDepth[i]);  // No negative depth
```

### Implementation Details

**Double Buffering for Determinism:**

```csharp
// Flow computation uses separate buffer (avoids race conditions)
struct FlowBuffer : IBufferElementData
{
    public float FlowN;  // Flow to north neighbor
    public float FlowS;  // Flow to south neighbor
    public float FlowE;  // Flow to east neighbor
    public float FlowW;  // Flow to west neighbor
}

// Compute flows (parallel job, reads waterDepth, writes FlowBuffer)
// Apply flows (parallel job, reads FlowBuffer, writes waterDepth)
```

**Edge Conductivity:**

```csharp
float GetEdgeConductivity(int cellIndex, int neighborIndex, Direction dir)
{
    var cell = cells[cellIndex];
    var neighbor = cells[neighborIndex];
    
    // Check solid mask (walls/dams block flow)
    if (cell.SolidMask == 255 || neighbor.SolidMask == 255)
        return 0f;
    
    // Use minimum conductivity (bottleneck)
    float cellCond = cell.Conductivity / 255f;
    float neighborCond = neighbor.Conductivity / 255f;
    return math.min(cellCond, neighborCond);
}
```

**Simulation Rate:**

```csharp
// Run at lower rate (5-20 Hz), independent of render frame
public struct WaterGridUpdateConfig : IComponentData
{
    public float UpdateIntervalSeconds;  // Default: 0.1s (10 Hz)
    public float FlowCoefficient;        // kFlow: 0.1-0.5 (controls speed)
    public float MaxOutPerCell;          // Cap bandwidth per step (prevent instability)
    public float MinWaterThreshold;      // Deactivate cells below this depth
}
```

---

## Active Set + Dirty Regions (Performance)

### Active Cell Criteria

**A cell becomes active if:**

1. **Terrain edited** in/near it (dig/build dam) → mark cell + neighbors active
2. **Water changed** above epsilon threshold (flow propagated) → mark active
3. **Neighbors active** → mark active (expansion)
4. **Source/sink** (rain, spring, drain) → mark active
5. **Conduit coupling** (surface ↔ tunnel interaction) → mark active

**A cell becomes inactive if:**

- Water depth < MinWaterThreshold (e.g., 0.01m)
- All neighbors stable (water depth unchanged)
- No sources/sinks nearby

**Active Set Maintenance:**

```csharp
public struct WaterActiveSetSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        // 1. Expand active set (mark neighbors of active cells)
        ExpandActiveSet();
        
        // 2. Deactivate stable cells (water < threshold, neighbors stable)
        DeactivateStableCells();
        
        // 3. Mark new active cells from terrain edits
        MarkDirtyFromTerrainEdits();
        
        // 4. Mark new active cells from sources/sinks
        MarkDirtyFromSources();
    }
}
```

### Dirty Region Tracking

**Per-Chunk Dirty Regions:**

```csharp
public struct WaterDirtyRegion : IBufferElementData
{
    public int2 MinCell;    // Bounding box min
    public int2 MaxCell;    // Bounding box max
    public uint Tick;       // When marked dirty
}

// Dirty regions merged/expanded per-chunk
// Only cells in dirty regions processed during flow update
```

**Benefits:**
- Only update cells with activity → O(active cells) instead of O(world size)
- Predictable cost (active cells typically <10% of total cells)
- Scales to large worlds (millions of cells, thousands active)

---

## Underground Burrows/Tunnels (Conduit Graph)

### Conduit Network Structure

**Graph-Based (Not 3D Voxels):**

```csharp
// Conduit nodes (tunnel junctions)
public struct ConduitNode : IComponentData
{
    public float3 Position;        // World position
    public float InvertHeight;     // Lowest elevation inside tunnel
    public float WaterVolume;      // Water stored at node
    public byte SurfaceCoupling;   // 0 = no surface link, 1+ = entrance ID
}

// Conduit edges (tunnel segments)
public struct ConduitEdge : IBufferElementData
{
    public Entity NodeA;           // Start node entity
    public Entity NodeB;           // End node entity
    public float Capacity;         // Cross-section area (m²)
    public float Resistance;       // Roughness/length coefficient
    public float Length;           // Segment length (meters)
    public byte Blocked;           // 0 = open, 1 = blocked/sealed
}

// Surface coupling (tunnel entrance)
public struct ConduitSurfaceCoupling : IComponentData
{
    public Entity ConduitNode;     // Linked tunnel node
    public int2 SurfaceCell;       // Surface cell index
    public float EntranceHeight;   // Elevation where surface connects
    public float CouplingRadius;   // How far surface water affects tunnel
}
```

### Flow Along Conduit Edges

**Same Head Difference Principle:**

```csharp
// For each conduit edge (A → B)
float headA = nodeA.InvertHeight + nodeA.WaterVolume / nodeA.Capacity;
float headB = nodeB.InvertHeight + nodeB.WaterVolume / nodeB.Capacity;
float d = headA - headB;

if (d <= 0) continue;  // No flow

// Flow limited by capacity and resistance
float flowRate = d / (Resistance * Length);  // Simplified flow model
float maxFlow = Capacity * flowRate * dt;    // Capacity limits flow
float actualFlow = math.min(maxFlow, nodeA.WaterVolume);

nodeA.WaterVolume -= actualFlow;
nodeB.WaterVolume += actualFlow;
```

**Resistance Model (Simplified):**

```csharp
// Resistance based on roughness and length
float Resistance = BaseRoughness + (Length * FrictionCoefficient);
// Higher resistance = slower flow
// Capacity = cross-section area (wider tunnels flow more)
```

### Surface ↔ Tunnel Coupling

**Bidirectional Transfer:**

```csharp
// Surface cell to tunnel node (if surface water above entrance)
float surfaceHeight = groundHeight[surfaceCell] + waterDepth[surfaceCell];
if (surfaceHeight > entrance.EntranceHeight)
{
    float excess = surfaceHeight - entrance.EntranceHeight;
    float transfer = excess * CouplingRate * dt;
    
    waterDepth[surfaceCell] -= transfer;
    conduitNode.WaterVolume += transfer;
}

// Tunnel node to surface cell (backflow/flooding)
float tunnelHead = conduitNode.InvertHeight + conduitNode.WaterVolume / conduitNode.Capacity;
if (tunnelHead > entrance.EntranceHeight)
{
    float excess = tunnelHead - entrance.EntranceHeight;
    float transfer = excess * CouplingRate * dt;
    
    conduitNode.WaterVolume -= transfer;
    waterDepth[surfaceCell] += transfer;
}
```

**Benefits:**
- Siphoning through terrain (dig tunnel, water flows)
- Underground channels (connect two surface points via tunnel)
- Flooding from below (tunnel fills, water backs up to surface)
- No 3D voxel grid needed (graph is sparse, efficient)

---

## Multiple Liquids (Water, Lava, Oil)

### Single-Layer Approach (MVP)

**Start Simple:**

```csharp
public struct WaterCellBlob
{
    public float GroundHeight;
    public float WaterDepth;        // Primary liquid depth
    public LiquidType LiquidType;   // Water, Lava, Oil, etc.
    // ... other fields
}

public enum LiquidType : byte
{
    Water = 0,
    Lava = 1,
    Oil = 2,
    // Add more as needed
}
```

**Mixing Rules (Avoid Complexity):**

- **Replace:** New liquid type replaces old (no mixing)
- **Stratify:** Heavy liquids (lava) sink below light (oil), separate layers
- **No Mixing:** Avoid complex multi-phase fluids (too expensive)

**If Stratification Needed (Future):**

```csharp
// At most 2 layers per cell
public struct StratifiedLiquid
{
    public LiquidType HeavyLayer;   // Bottom layer (lava)
    public float HeavyDepth;
    public LiquidType LightLayer;   // Top layer (oil)
    public float LightDepth;
}

// Heavy liquids settle, light liquids float
// Flow rules applied per layer independently
```

**Design Choice:** Start with single liquid type per cell. Add stratification only if gameplay requires it.

---

## Zero-G Liquid Behaviors (Microgravity)

**Implement zero-G liquids using abstraction and compartment graph, not heightfield simulation. Surface tension/wetting/geometry dominate in microgravity, not "falls down and levels out".**

**Reference:** [NASA - Microgravity Slosh](https://www.nasa.gov/centers/marshall/news/background/facts/slosh.html) - Surface-tension-dominated environment where behavior changes drastically compared to 1g.

**Reference:** [Nature - Capillary Behavior in Low-G](https://www.nature.com/articles/s41526-018-0041-2) - Wetting + geometry dominate capillary behavior (Bond number < 1 regime).

### Why Abstraction is the Right Default in Zero-G

**In microgravity, fluid behavior is dominated by:**
- Surface tension (capillary forces)
- Wetting (liquid adheres to surfaces)
- Container geometry (corners, edges act as conduits)

**Heightfield "puddle sim" won't be correct in zero-G anyway.** NASA explicitly calls microgravity slosh a surface-tension-dominated environment where behavior changes drastically compared to 1g.

**Recommended design: 3-tier liquid model (sim stays cheap):**

### Tier 0: Contained Liquids (Tanks, Pipes)

**Just scalar amounts + optional pressure/temperature:**

```csharp
/// <summary>
/// Contained liquid storage (tanks, pipes, containers).
/// </summary>
public struct ContainedLiquid : IComponentData
{
    public LiquidType Type;
    public float Mass;                  // Kilograms
    public float Volume;                // Liters/m³
    public float Pressure;              // Optional (for gameplay)
    public float Temperature;           // Optional (for gameplay)
}
```

**No spatial sim.** Just track amounts per container entity.

### Tier 1: Free Liquid Inside Compartment (Zero-G / Interiors)

**Represent as compartment graph nodes with liquid mass + blob state:**

```csharp
/// <summary>
/// Liquid mass per compartment node (compartment graph).
/// </summary>
public struct CompartmentLiquid : IComponentData
{
    public LiquidType Type;
    public float Mass;                  // Total mass in compartment
    public byte BlobCount;              // Usually 1, can split if needed
    public BlobAnchorType AnchorType;   // FreeFloat | WetSurface | CornerWick
    public float3 BlobCenter;           // Optional: approximate blob position (for visuals/hazards)
}

public enum BlobAnchorType : byte
{
    FreeFloat = 0,      // Floating blob in free space
    WetSurface = 1,     // Adhered to wall/surface
    CornerWick = 2      // Corner/edge wicking (capillary conduit)
}

/// <summary>
/// Wetness per surface group (optional, for tracking surface contamination).
/// </summary>
public struct SurfaceWetness : IComponentData
{
    public float Wetness01;             // 0-1 wetness level
    public uint LastUpdateTick;
}
```

**Key microgravity behavior cheat that matches reality:**

Liquids migrate to corners/wicking structures and stick due to wetting/capillary effects. ISS corner-drain experiments show interior corners act as open capillary conduits in low-g where capillary forces dominate.

**Reference:** [Nature - ISS Corner Drain Experiments](https://www.nature.com/articles/s41526-018-0041-2)

**Transport between compartments:**

Use edge conductance (doors/vents/gaps), same as your gas/smell models:

```csharp
/// <summary>
/// Compartment edge (door, vent, gap) with liquid transport.
/// </summary>
public struct CompartmentEdge : IBufferElementData
{
    public Entity NodeA;                // Source compartment
    public Entity NodeB;                // Target compartment
    public float Conductance;           // 0-1: how easily liquid passes (door closed = 0, open = 1)
    public float MaxFlowRate;           // Max liters/second
}

// Liquid flow: similar to gas diffusion
// Mass flows from high-mass to low-mass compartments
// Limited by edge conductance and max flow rate
```

### Tier 2: Local "Blob Particles" (Only Near Camera / Boarding)

**Materialize a blob as 4-20 particles/metaball centers for visuals + local collision/hazard:**

```csharp
/// <summary>
/// Blob particle representation (only for nearby/visible blobs).
/// </summary>
public struct LiquidBlobParticle : IComponentData
{
    public float3 Position;             // Particle center
    public float Radius;                // Metaball radius
    public float Mass;                  // Partial mass (blob split into particles)
    public uint LastUpdateTick;
}

/// <summary>
/// Blob visualization entity (spawned when player approaches).
/// </summary>
public struct LiquidBlobVisual : IComponentData
{
    public Entity CompartmentNode;      // Source compartment
    public uint ParticleCount;          // 4-20 particles
    public float MaxDistance;           // Convert back to Tier 1 when beyond this
}
```

**Convert back to Tier 1 when leaving interest range** (despawn particles, merge mass back into compartment scalar).

### Zero-G "Effective Gravity" Hook (Makes It Feel Right in Ships)

**Even in orbit, fluids respond strongly to vehicle maneuvers/accelerations (slosh):**

NASA's slosh experiment literally pushes a free-floating tank to study this regime.

**Reference:** [NASA - Slosh Experiment](https://www.nasa.gov/centers/marshall/news/background/facts/slosh.html)

**In simulation:**

```csharp
/// <summary>
/// Effective gravity in compartment frame (gravity + ship acceleration).
/// </summary>
public struct CompartmentEffectiveGravity : IComponentData
{
    public float3 GravityVector;        // g_eff = gravity + (-ship_acceleration)
    public float Magnitude;             // |g_eff|
}

// Compute g_eff = gravity + (-ship_acceleration) in compartment frame
// If |g_eff| > threshold, treat fluid like "puddle" along -g_eff (heightfield logic applies locally)
// If |g_eff| <= threshold, switch to capillary/geometry mode (Tier 1 anchor behavior)
```

**Rule:**

```csharp
const float gThreshold = 0.1f;  // m/s² threshold

if (compartment.gEffective.Magnitude > gThreshold)
{
    // Use heightfield puddle sim (Tier 1 switches to heightfield mode)
    // Flow along -g_eff direction
    // Blob anchor = WetSurface (adheres to floor along -g_eff)
}
else
{
    // Use capillary/geometry mode (Tier 1 anchor behavior)
    // Blob anchor = CornerWick or WetSurface (surface tension dominates)
}
```

**This single rule ties zero-G liquids to flight controls without CFD.**

### Rendering: Blob Visuals are Optional and Should be LOD'd

**Do not render blobs everywhere.** Make it a presentation tier:

**Far:**
- Decals/wetness (surface staining)
- Simple billboards (icon representation)

**Near:**
- Metaballs/SDF blob (signed distance field raymarching)

**A practical GPU-friendly approach is signed distance field metaballs in a small volume, raymarched in a shader** (works great for "floating globules").

**Reference:** [Raymarching Metaballs Tutorial](https://thisisgrow.com/blog/raymarching-metaballs/)

**You only enable it for the handful of blobs the player is actually looking at.**

### Gameplay/AI Hooks (So Agents "Understand" Liquids Cheaply)

**Expose a single EnvironmentSample for interiors:**

```csharp
/// <summary>
/// Liquid presence and hazard info (for AI/ gameplay queries).
/// </summary>
public struct LiquidPresenceSample
{
    public float LiquidPresence01;      // 0-1: how much liquid in compartment
    public float LiquidHazard01;        // 0-1: engulf risk, drowning risk, etc.
    public LiquidTrend Trend;           // Rising | Falling | Stable
    public bool CanBreathe;             // Medium type: Gas (can breathe)
    public bool CanSwim;                // Medium type: Liquid (can swim)
}

public enum LiquidTrend : byte
{
    Stable = 0,
    Rising = 1,
    Falling = 2
}

// Agents react to these scalars (evacuate, seal hatch, grab tether)
// Not to fluid simulation details
```

**Integration with EnvironmentSampler:**

```csharp
public struct EnvironmentSampler
{
    // ... existing methods ...
    
    public LiquidPresenceSample SampleLiquidPresence(float3 worldPosition)
    {
        // Query compartment graph at position
        var compartment = GetCompartmentAtPosition(worldPosition);
        if (!compartment.IsValid) return default;
        
        var liquid = SystemAPI.GetComponent<CompartmentLiquid>(compartment);
        var hazard = ComputeHazard(liquid, compartment);
        
        return new LiquidPresenceSample
        {
            LiquidPresence01 = liquid.Mass / MaxLiquidMass,
            LiquidHazard01 = hazard,
            Trend = ComputeTrend(compartment),
            CanBreathe = liquid.Mass < BreathingThreshold,
            CanSwim = liquid.Mass > SwimmingThreshold
        };
    }
}
```

### Bottom Line

**Sim:** Abstract (compartment scalar + capillary anchors + g_eff response). This is faithful to the real "surface tension dominates in microgravity" regime.

**Render:** Blob visuals only as a near-camera effect (SDF metaballs/raymarch) to avoid GPU blowups.

**This plays well with your existing "fields + dirty regions" approach** — but for zero-G you get the best results by abstracting the simulation and only doing blob rendering as a near-camera effect.

---

## Terrain Modifications (Dams, Gates, Digging)

### Editing Pattern

**All edits mark cells dirty + neighbors active:**

```csharp
// Digging: decrease groundHeight
void DigTerrain(int2 cellCoord, float depth)
{
    var cell = GetCell(cellCoord);
    cell.GroundHeight -= depth;
    cell.GroundHeight = math.max(MinHeight, cell.GroundHeight);
    
    SetCell(cellCoord, cell);
    MarkCellDirty(cellCoord);
    MarkNeighborsActive(cellCoord);  // Flow will propagate
}

// Damming: increase groundHeight or set solidMask
void BuildDam(int2 cellCoord)
{
    var cell = GetCell(cellCoord);
    cell.GroundHeight += DamHeight;
    // OR: cell.SolidMask = 255;  // Block flow completely
    
    SetCell(cellCoord, cell);
    MarkCellDirty(cellCoord);
    MarkNeighborsActive(cellCoord);  // Upstream will rise, downstream affected
}

// Gates: toggle edgeBlocked or change conductivity
void ToggleGate(int2 cellA, int2 cellB, Direction dir)
{
    SetEdgeBlocked(cellA, cellB, dir, isBlocked);
    MarkCellDirty(cellA);
    MarkCellDirty(cellB);
    MarkNeighborsActive(cellA);
    MarkNeighborsActive(cellB);
}
```

### Integration with Terrain System

**Uses Existing TerrainVersion:**

```csharp
// Check terrain version on update
var terrainVersion = SystemAPI.GetSingleton<TerrainVersion>();
if (waterChunk.TerrainVersion != terrainVersion.Version)
{
    // Terrain changed, update groundHeight from terrain system
    UpdateGroundHeightsFromTerrain(waterChunk);
    waterChunk.TerrainVersion = terrainVersion.Version;
    MarkAllCellsDirty(waterChunk);  // Full update needed
}
```

**Event-Driven Updates:**

```csharp
// Listen to TerrainChangeEvent buffer
foreach (var evt in terrainChangeEvents)
{
    if (evt.Flags.HasFlag(TerrainChangeEvent.FlagHeightChanged))
    {
        var affectedCells = GetCellsInRadius(evt.Position, evt.Radius);
        foreach (var cell in affectedCells)
        {
            UpdateGroundHeight(cell, newHeight);
            MarkCellDirty(cell);
            MarkNeighborsActive(cell);
        }
    }
}
```

**Emergent Behavior:**
- Dig hole → groundHeight decreases → surface drops → neighbors flow in → hole fills
- Dig channel → groundHeight decreases along path → lake overflows → river forms
- Build dam → groundHeight increases or solidMask blocks → upstream rises → spill at lowest unblocked point
- Everything else emerges from flow step (no special cases needed)

---

## Visuals (Gameplay-Focused)

### Rendering Approach

**Simple Heightfield Surface:**

```csharp
// Per-chunk water surface mesh
public struct WaterSurfaceMesh : IComponentData
{
    public Entity MeshEntity;       // Render mesh entity
    public uint LastUpdateTick;     // For throttled updates
}

// Generate mesh from waterDepth per cell
void UpdateWaterSurfaceMesh(WaterGridChunkComponent chunk)
{
    // Simple: flat surface per cell (no fancy shaders initially)
    // Optionally: marching squares for smooth surface (future)
    
    var vertices = new NativeList<float3>(Allocator.Temp);
    var indices = new NativeList<int>(Allocator.Temp);
    
    for (int y = 0; y < chunk.CellsPerChunk.y; y++)
    {
        for (int x = 0; x < chunk.CellsPerChunk.x; x++)
        {
            var cell = GetCell(chunk, new int2(x, y));
            float surfaceY = cell.GroundHeight + cell.WaterDepth;
            
            // Add quad vertices at surfaceY
            // (simplified, actual implementation uses proper quad generation)
        }
    }
    
    // Update mesh entity with vertices/indices
}
```

**Throttled Updates:**

```csharp
// Update mesh at lower rate (not every simulation step)
if ((tick - mesh.LastUpdateTick) >= MeshUpdateIntervalTicks)
{
    UpdateWaterSurfaceMesh(chunk);
    mesh.LastUpdateTick = tick;
}
```

**Visual Features (Future):**
- Flow direction indicators (particles following flow)
- Foam/whitewater at boundaries (shader effects)
- Depth-based color (deeper = darker blue)
- Reflection/refraction (advanced shaders, optional)

---

## ECS Structure & Systems

### Component Organization

**Chunk-Owned Data (Not Millions of Cell Entities):**

```csharp
// Chunk entity (one per SurfaceFieldsChunk)
Entity chunkEntity;

// Components on chunk entity:
- WaterGridChunkComponent          // Base blob reference, metadata
- DynamicBuffer<WaterGridRuntimeCell>  // Runtime water depth, active flags
- DynamicBuffer<FlowBuffer>        // Flow computation buffer (double buffer)
- DynamicBuffer<WaterDirtyRegion>  // Dirty region tracking
- WaterGridUpdateConfig            // Simulation parameters

// Conduit entities (tunnel network)
Entity conduitNodeEntity;
- ConduitNode                      // Node position, water volume
- DynamicBuffer<ConduitEdge>       // Edges to other nodes

Entity conduitSurfaceCouplingEntity;
- ConduitSurfaceCoupling           // Surface ↔ tunnel link
- LocalTransform                   // Entrance position
```

**Systems (Update Order):**

```csharp
// 1. Mark dirty regions (terrain edits, sources/sinks)
[UpdateInGroup(typeof(EnvironmentSystemGroup))]
[UpdateBefore(typeof(WaterActiveSetSystem))]
public partial struct WaterDirtyMarkingSystem : ISystem
{
    // Marks cells dirty from terrain changes, sources, sinks
}

// 2. Maintain active set (expand, deactivate stable)
[UpdateInGroup(typeof(EnvironmentSystemGroup))]
[UpdateAfter(typeof(WaterDirtyMarkingSystem))]
public partial struct WaterActiveSetSystem : ISystem
{
    // Expands active set, deactivates stable cells
}

// 3. Compute flows (parallel job)
[UpdateInGroup(typeof(EnvironmentSystemGroup))]
[UpdateAfter(typeof(WaterActiveSetSystem))]
[BurstCompile]
public partial struct WaterFlowComputeJob : IJobEntity
{
    // Computes flows into FlowBuffer (reads WaterDepth, writes FlowBuffer)
}

// 4. Apply flows (parallel job)
[UpdateInGroup(typeof(EnvironmentSystemGroup))]
[UpdateAfter(typeof(WaterFlowComputeJob))]
[BurstCompile]
public partial struct WaterFlowApplyJob : IJobEntity
{
    // Applies flows to WaterDepth (reads FlowBuffer, writes WaterDepth)
}

// 5. Conduit flow (tunnel network)
[UpdateInGroup(typeof(EnvironmentSystemGroup))]
[UpdateAfter(typeof(WaterFlowApplyJob))]
public partial struct ConduitFlowSystem : ISystem
{
    // Updates tunnel graph flow
}

// 6. Surface ↔ tunnel coupling
[UpdateInGroup(typeof(EnvironmentSystemGroup))]
[UpdateAfter(typeof(ConduitFlowSystem))]
public partial struct ConduitSurfaceCouplingSystem : ISystem
{
    // Transfers water between surface and tunnels
}

// 7. Update visuals (throttled)
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct WaterSurfaceMeshUpdateSystem : ISystem
{
    // Updates water surface meshes (throttled, not every frame)
}
```

**Update Frequency:**

```csharp
// Water simulation runs at configurable rate (5-20 Hz)
// Independent of render frame rate (60+ Hz)

[UpdateInGroup(typeof(EnvironmentSystemGroup))]
[UpdateInterval(0.1f)]  // 10 Hz default
public partial struct WaterGridUpdateSystem : ISystem
{
    // Coordinates all water update systems
}
```

---

## Integration with Environment Field API

### Query Interface

**Extend EnvironmentSampler:**

```csharp
public struct EnvironmentSampler
{
    // ... existing methods ...
    
    public float SampleWaterDepth(float3 worldPosition, float defaultValue = 0f)
    {
        // Query water grid chunk at position
        var chunk = GetWaterChunkAtPosition(worldPosition);
        if (!chunk.IsValid) return defaultValue;
        
        var cell = GetCellAtPosition(chunk, worldPosition);
        return cell.WaterDepth;
    }
    
    public float SampleWaterSurface(float3 worldPosition, float defaultValue = 0f)
    {
        var chunk = GetWaterChunkAtPosition(worldPosition);
        if (!chunk.IsValid) return defaultValue;
        
        var cell = GetCellAtPosition(chunk, worldPosition);
        return cell.GroundHeight + cell.WaterDepth;
    }
    
    public bool IsUnderwater(float3 worldPosition)
    {
        float surfaceY = SampleWaterSurface(worldPosition);
        return worldPosition.y < surfaceY;
    }
}
```

**Environment Field Integration:**

```csharp
// Update EnvironmentField with water data
public struct EnvironmentField
{
    // ... existing fields ...
    public float WaterDepth;        // From water grid
    public float3 FlowVelocity;     // From flow direction (future)
    public MediumType Medium;       // Gas / Liquid / Vacuum (updated by water depth)
}

// Entities query environment field (not direct water grid access)
var env = EnvironmentField.Query(position);
if (env.Medium == MediumType.Liquid)
{
    // Entity is in water
    // Use env.WaterDepth, env.FlowVelocity for gameplay
}
```

---

## MVP Scope

### Phase 1: Heightfield Water (Core)

**Minimum Viable Implementation:**

1. **Water Grid Chunk System:**
   - Create `WaterGridChunkBlob` structure (integrates with SurfaceFieldsChunk)
   - Store `GroundHeight` (from terrain), `WaterDepth` (dynamic)
   - Runtime cell buffer for updates

2. **Active Set + Dirty Regions:**
   - Mark cells active based on criteria
   - Only update active cells
   - Deactivate stable cells

3. **Flow Computation:**
   - Basic flow rule (higher surface → lower surface)
   - 4-neighborhood (N/S/E/W)
   - Double buffering for determinism

4. **Terrain Integration:**
   - Update GroundHeight from terrain modifications
   - Mark dirty on terrain edits
   - Uses TerrainVersion for invalidation

5. **Basic Visuals:**
   - Simple water surface mesh (flat per cell)
   - Throttled updates (not every frame)

**This Already Produces:**
- Holes fill (water flows into lower areas)
- Overflow (lakes spill at lowest point)
- Rivers form (channels guide flow)
- Dams work (block flow, upstream rises)

### Phase 2: Conduit Graph (Underground)

**Add Tunnel Support:**

1. **Conduit Node/Edge Components:**
   - Graph-based tunnel network
   - Flow along edges (head difference)

2. **Surface Coupling:**
   - Tunnel entrances link surface ↔ tunnel
   - Bidirectional transfer (siphoning, flooding)

3. **Integration:**
   - Conduit flow system
   - Surface coupling system

**This Adds:**
- Siphoning through terrain (dig tunnel, water flows)
- Underground channels (connect surface points)

### Phase 3: Multiple Liquids (Optional)

**If Needed:**

1. **Liquid Types:**
   - Add LiquidType enum (Water, Lava, Oil)
   - Replace rules (no mixing)

2. **Stratification (If Needed):**
   - Heavy/light layers per cell
   - Settling behavior

---

## Performance Considerations

### Optimization Strategies

1. **Active Set:** Only update active cells (typically <10% of total)
2. **Chunk-Based:** Update chunks independently (parallel-friendly)
3. **Throttled Simulation:** 5-20 Hz (not every frame)
4. **Throttled Visuals:** Mesh updates every N frames
5. **Burst Compilation:** All update jobs must be Burst-compilable

### Scalability

**Target Performance:**
- 1M+ cells (chunked, streaming)
- Thousands of active cells per update
- <5ms per update (10 Hz = 50ms budget)
- Predictable cost (active set size)

**Memory Budget:**
- Chunk blobs: ~16 bytes per cell (GroundHeight + WaterDepth + metadata)
- Runtime buffers: Only for active chunks
- Conduit graph: Sparse (nodes << cells)

---

## Related Documentation

- **Liquid-Terrain Integration:** `Docs/Concepts/Core/Liquid_Terrain_Integration.md` - Unified terrain + liquid system with shared dirty regions
- **Diggable Terrain System:** `Docs/Concepts/Core/Diggable_Terrain_System.md` - Chunked terrain with diff-based edits
- **Simulation LOD & Environment Fields:** `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` - Tier B (Shallow-Water Heightfields)
- **Surface Fields System:** `Packages/com.moni.puredots/Runtime/WorldGen/SurfaceFieldsChunk.cs` - Chunk structure
- **Environment Grids:** `Packages/com.moni.puredots/Runtime/Runtime/Environment/EnvironmentGrids.cs` - Grid metadata pattern
- **Terraforming:** `Docs/Archive/TODO/TerraformingPrototype_TODO.md` - Terrain modification integration

---

**For Implementers:** Focus on active set maintenance, flow computation double-buffering, chunk-based parallel updates, and integration with diggable terrain dirty regions  
**For Designers:** Focus on digging/damming gameplay, visual feedback, strategic water manipulation, and evacuation behavior

