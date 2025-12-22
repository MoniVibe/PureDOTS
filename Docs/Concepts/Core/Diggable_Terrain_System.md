# Diggable Terrain System

**Status:** Concept
**Category:** Core - Terrain Modification & Navigation
**Scope:** Cross-Project (Godgame: Digging/Mining, Space4X: Tunnel Networks)
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Enable diggable terrain with efficient navigation updates, avoiding "rebake hell" by using chunked terrain with diff-based edits, derived navigation graphs, and versioned path invalidation instead of monolithic navmesh rebakes.

**Secondary Goals:**
- Fast terrain edits (chunked data, diff-based)
- Incremental navigation updates (no global rebakes)
- Versioned path invalidation (agents replan only when needed)
- Resource production from digging (terrain is a resource)
- Integration with water/conduits (tunnel flooding)
- Burst-friendly, data-driven design

**Key Principle:** Terrain is chunked authoritative data (dig edits are diffs), nav is a derived tile graph (surface) + derived void graph (underground), edits only dirty a few tiles, and path results self-invalidate via versioning.

---

## 1. Terrain Representation (Fast Edits)

### Core Concept

**Store terrain as chunks, not entities.** Fixed-size chunks hold voxel data. Digging edits only touch a small list of voxels → push diffs.

### Chunk Structure

```csharp
// Terrain chunk (fixed-size, e.g., 32×32×H or 16×16×16 voxels)
public struct TerrainChunk : IComponentData
{
    public int3 ChunkCoord;                  // Chunk coordinates (X, Y, Z)
    public int3 VoxelsPerChunk;              // Dimensions (e.g., 32×32×64)
    public BlobAssetReference<TerrainChunkBlob> BaseBlob; // Base terrain data
}

// Chunk blob (voxel data)
public struct TerrainChunkBlob
{
    // SolidMask bitset (1 = solid, 0 = empty) or uint8 occupancy[]
    public BlobArray<byte> SolidMask;        // 1 = solid, 0 = empty
    
    // Material data
    public BlobArray<byte> MaterialId;       // Stone, soil, ore, bedrock, etc.
    public BlobArray<byte> Hardness;         // Optional, or from MaterialSpec
    
    // Resource deposits
    public BlobArray<byte> DepositId;        // Optional: deposit identifier
    public BlobArray<byte> OreGrade;         // Optional: deposit grade (0-255)
}

// Runtime voxel state (for dynamic edits)
[InternalBufferCapacity(0)]
public struct TerrainVoxelRuntime : IBufferElementData
{
    public byte SolidMask;                   // Current solid/empty state
    public byte MaterialId;
    public byte DepositId;
    public byte OreGrade;
}

// Dirty tracking
public struct TerrainChunkDirty : IComponentData
{
    public uint EditVersion;                 // Incremented on edit
}
```

### Data-Driven Specs

```csharp
// Terrain material specification (authoring data)
public struct TerrainMaterialSpec : IComponentData
{
    public FixedString64Bytes MaterialId;    // "stone", "soil", "ore", "bedrock"
    public float DigTimeMultiplier;          // 1.0 = normal, 2.0 = twice as long
    public BlobAssetReference<ResourceYieldBlob> Yields; // Resources per m³
    public float WaterPermeability;          // 0-1: allows water through?
    public bool TunnelAllowed;               // Can dig tunnels through this?
    public float NavCostModifier;            // Mud, rubble, etc. affect pathfinding cost
}

// Resource yield from material
public struct ResourceYieldBlob
{
    public BlobArray<ResourceYieldEntry> Yields;
}

public struct ResourceYieldEntry
{
    public FixedString64Bytes ResourceId;    // "stone", "iron_ore", "clay"
    public float AmountPerCubicMeter;        // Expected amount per m³
}

// Deposit specification
public struct DepositSpec : IComponentData
{
    public FixedString64Bytes DepositId;     // "iron_vein", "gem_deposit"
    public BlobAssetReference<ProductListBlob> Products; // What resources produced
    public BlobAssetReference<GradeDistributionBlob> GradeDistribution;
    public FixedString64Bytes RefinementChainId; // Processing chain
}
```

**Rule:** Digging edits only touch a small list of voxels/cells → push diffs. No full chunk rebuilds.

---

## 2. Navigation Without Rebakes: Two Graphs

### A) Surface Nav (2.5D)

**Represent walkable surface as a 2D grid (spatial system already exists).**

```csharp
// Surface navigation tile (derived from terrain + obstacles)
public struct SurfaceNavTile : IComponentData
{
    public int2 TileCoord;                   // Tile coordinates (X, Z)
    public uint Version;                     // Incremented when tile changes
    public BlobAssetReference<SurfaceNavTileBlob> Data;
}

// Tile blob (derived data)
public struct SurfaceNavTileBlob
{
    public BlobArray<byte> WalkableMask;     // Bitmask: walkable cells
    public BlobArray<byte> MoveCost;         // 0-255: pathfinding cost
    public BlobArray<float> Height;          // Surface height per cell
    public BlobArray<float> Slope;           // Slope angle per cell
}

// Derived data per tile chunk (e.g., 32×32)
// Walkability/cost comes from:
// - Height / slope
// - Obstacles / buildings
// - Dig edits (height changes, pits, ramps)

// Optional: FlowField cache for mass movement
public struct FlowFieldCache : IComponentData
{
    public BlobAssetReference<FlowFieldBlob> Field; // Direction vectors for crowd movement
}
```

### B) Underground Nav (Void Connectivity)

**Underground is a void graph derived from excavated voxels.**

```csharp
// Underground navigation chunk (void connectivity)
public struct UndergroundNavChunk : IComponentData
{
    public int3 ChunkCoord;                  // Chunk coordinates
    public uint Version;                     // Incremented when void connectivity changes
    public BlobAssetReference<UndergroundNavChunkBlob> Data;
}

// Chunk blob (void graph)
public struct UndergroundNavChunkBlob
{
    // Passable voxel = empty and has enough clearance for agent size
    public BlobArray<byte> PassableMask;     // Bitmask: passable voxels
    public BlobArray<byte> ClearanceHeight;  // Vertical clearance (for agent size)
    
    // Option 1 (MVP): No region ids, pathfinding treats passability as truth
    // Option 2 (Better scaling): Per-chunk region labeling
    public BlobArray<ushort> RegionId;       // Optional: connectivity region ID
}

// Local adjacency is implicit (6-neighbor voxels)
// For pathfinding:
// - Start simple: A* over passable voxels only in loaded/nearby chunks
// - Scale later: cluster each chunk into "portals" (HPA* style), so long paths travel portal→portal
```

**Key:** Don't build a global graph; build per chunk. Local adjacency is implicit (6-neighbor).

---

## 3. Edit Pipeline (Dig/Build) That Keeps Nav Alive

### Core Concept

**Terrain edits are commands → diffs → dirty tiles.** Budget it (N edits per tick) so worst-case player spam can't stall the sim.

### Edit Command Structure

```csharp
// Dig command
public struct DigCommand : IBufferElementData
{
    public float3 TargetPosition;            // World position
    public float Radius;                     // Brush radius
    public float Depth;                      // How deep to dig
    public DigMode Mode;                     // Brush, Tunnel, Ramp
    public Entity DiggerEntity;              // Who is digging (for stats/tools)
}

public enum DigMode : byte
{
    Brush = 0,               // Spherical dig (hole/pit)
    Tunnel = 1,              // Cylindrical dig (tunnel path)
    Ramp = 2                 // Sloped dig (ramp/stairs)
}

// Terrain edit apply system
public struct TerrainEditApplySystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        // Process dig commands (budgeted: N per tick)
        var commands = GetDigCommands();
        int processed = 0;
        const int MaxEditsPerTick = 10; // Budget limit
        
        foreach (var cmd in commands)
        {
            if (processed >= MaxEditsPerTick)
                break; // Budget exhausted
            
            // Apply dig command
            ApplyDigCommand(cmd);
            
            // Produce terrain delta
            var delta = ProduceTerrainDelta(cmd);
            StoreTerrainDelta(delta);
            
            // Mark dirty tiles
            MarkDirtySurfaceTiles(cmd);      // Around affected XY
            MarkDirtyUndergroundChunks(cmd); // Affected chunk and neighbors
            
            processed++;
        }
    }
    
    void ApplyDigCommand(DigCommand cmd)
    {
        // Find affected chunks
        var affectedChunks = GetAffectedChunks(cmd.TargetPosition, cmd.Radius);
        
        foreach (var chunkEntity in affectedChunks)
        {
            var chunk = GetComponentRW<TerrainChunk>(chunkEntity);
            var runtimeVoxels = GetBufferRW<TerrainVoxelRuntime>(chunkEntity);
            
            // Flip SolidMask (solid → empty) for affected voxels
            var voxelsToEdit = GetVoxelsInRange(chunk, cmd);
            
            foreach (var voxelIdx in voxelsToEdit)
            {
                var voxel = runtimeVoxels[voxelIdx];
                var oldState = voxel.SolidMask;
                voxel.SolidMask = 0; // Empty
                runtimeVoxels[voxelIdx] = voxel;
                
                // Store delta
                var delta = new TerrainDelta
                {
                    ChunkId = chunk.ValueRO.ChunkCoord,
                    LocalIndex = voxelIdx,
                    OldState = oldState,
                    NewState = 0,
                    OldMaterial = voxel.MaterialId,
                    NewMaterial = 0 // Air/empty
                };
                StoreDelta(delta);
            }
            
            // Mark chunk dirty
            var dirty = GetComponentRW<TerrainChunkDirty>(chunkEntity);
            dirty.ValueRW.EditVersion++;
        }
    }
}

// Terrain delta (for history/rewind)
public struct TerrainDelta : IBufferElementData
{
    public int3 ChunkId;
    public int LocalIndex;                   // Voxel index in chunk
    public byte OldState;                    // Old solid mask
    public byte NewState;                    // New solid mask
    public byte OldMaterial;
    public byte NewMaterial;
}
```

### Dirty Marking

```csharp
// Mark dirty surface tiles (around affected XY)
void MarkDirtySurfaceTiles(DigCommand cmd)
{
    int2 minTile = WorldToTileCoord(cmd.TargetPosition - cmd.Radius);
    int2 maxTile = WorldToTileCoord(cmd.TargetPosition + cmd.Radius);
    
    for (int x = minTile.x; x <= maxTile.x; x++)
    {
        for (int y = minTile.y; y <= maxTile.y; y++)
        {
            var tileEntity = GetTileEntity(new int2(x, y));
            if (tileEntity != Entity.Null)
            {
                var tile = GetComponentRW<SurfaceNavTile>(tileEntity);
                tile.ValueRW.Version++; // Increment version
                AddComponent<TileDirty>(tileEntity);
            }
        }
    }
}

// Mark dirty underground chunks (affected chunk and neighbors)
void MarkDirtyUndergroundChunks(DigCommand cmd)
{
    int3 chunkCoord = WorldToChunkCoord(cmd.TargetPosition);
    
    // Mark chunk + neighbors (6-neighbor connectivity)
    for (int dx = -1; dx <= 1; dx++)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (math.abs(dx) + math.abs(dy) + math.abs(dz) > 1)
                    continue; // Only 6-neighbors
                
                int3 neighborCoord = chunkCoord + new int3(dx, dy, dz);
                var chunkEntity = GetChunkEntity(neighborCoord);
                if (chunkEntity != Entity.Null)
                {
                    var chunk = GetComponentRW<UndergroundNavChunk>(chunkEntity);
                    chunk.ValueRW.Version++; // Increment version
                    AddComponent<ChunkDirty>(chunkEntity);
                }
            }
        }
    }
}
```

---

## 4. Incremental Nav Updates (No Rebake)

### A) Surface: Update Only Changed Cells + Local Smoothing

```csharp
// Surface nav update system
public struct SurfaceNavUpdateSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        // Only update dirty tiles
        foreach (var (tile, entity) in SystemAPI.Query<
            RefRW<SurfaceNavTile>>().WithEntityAccess())
        {
            if (!HasComponent<TileDirty>(entity))
                continue;
            
            // Recompute WalkableMask and MoveCost for dirty cells (and neighbors)
            // Slopes/steps depend on adjacency
            UpdateSurfaceTile(tile, entity);
            
            // Increment NavTileVersion
            tile.ValueRW.Version++;
            
            RemoveComponent<TileDirty>(entity);
        }
    }
    
    void UpdateSurfaceTile(SurfaceNavTile tile, Entity tileEntity)
    {
        var blob = tile.ValueRO.Data;
        var terrain = GetTerrainDataForTile(tile.ValueRO.TileCoord);
        
        // Recompute walkability for each cell
        for (int x = 0; x < TileSize; x++)
        {
            for (int y = 0; y < TileSize; y++)
            {
                int2 cellCoord = new int2(x, y);
                float height = GetTerrainHeight(terrain, cellCoord);
                float slope = ComputeSlope(terrain, cellCoord);
                
                // Determine walkability
                bool walkable = height > WaterLevel && slope < MaxWalkableSlope;
                SetWalkableMask(blob, cellCoord, walkable);
                
                // Compute move cost (slope, material, obstacles)
                float baseCost = 1.0f;
                float slopeCost = slope * SlopeCostMultiplier;
                float materialCost = GetMaterialNavCost(terrain, cellCoord);
                float totalCost = baseCost + slopeCost + materialCost;
                SetMoveCost(blob, cellCoord, totalCost);
            }
        }
    }
}
```

### B) Underground: Local Connectivity Maintenance

```csharp
// Underground nav update system
public struct UndergroundNavUpdateSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        // Only update dirty chunks
        foreach (var (chunk, entity) in SystemAPI.Query<
            RefRW<UndergroundNavChunk>>().WithEntityAccess())
        {
            if (!HasComponent<ChunkDirty>(entity))
                continue;
            
            UpdateUndergroundChunk(chunk, entity);
            
            chunk.ValueRW.Version++;
            RemoveComponent<ChunkDirty>(entity);
        }
    }
    
    void UpdateUndergroundChunk(UndergroundNavChunk chunk, Entity chunkEntity)
    {
        var blob = chunk.ValueRO.Data;
        var terrain = GetTerrainChunk(chunk.ValueRO.ChunkCoord);
        
        // Option 1 (MVP, simplest): No region ids
        // Pathfinding just treats passability as truth
        // If a path fails due to new blockage, agent replans
        
        // Recompute passability for each voxel
        for (int x = 0; x < ChunkSize.x; x++)
        {
            for (int y = 0; y < ChunkSize.y; y++)
            {
                for (int z = 0; z < ChunkSize.z; z++)
                {
                    int3 voxelCoord = new int3(x, y, z);
                    bool isEmpty = !IsSolid(terrain, voxelCoord);
                    float clearance = ComputeClearance(terrain, voxelCoord);
                    bool passable = isEmpty && clearance >= MinAgentClearance;
                    
                    SetPassableMask(blob, voxelCoord, passable);
                }
            }
        }
        
        // Option 2 (better scaling): Per-chunk region labeling
        if (UseRegionIds)
        {
            // Maintain RegionId[] for passable voxels
            // On small edits: do a localized flood fill starting from changed voxel(s)
            // If a removal potentially splits: flood-fill from neighbors to relabel
            // (bounded to chunk; if it spills, mark neighbor chunk dirty too)
            UpdateRegionIds(blob, terrain);
        }
    }
}
```

**Key:** Updates are localized to dirty tiles/chunks only. No global recomputation.

---

## 5. Path Results Stay Valid via Versioning

### Core Concept

**Every PathResult stores tile/chunk versions. When an agent is about to use a path, check versions → request replan if stale.**

```csharp
// Path result with versioning
public struct NavPathResult : IComponentData
{
    public BlobAssetReference<NavPathBlob> Path;
    public uint PathVersion;                 // Overall path version
}

// Path blob (waypoints + version snapshots)
public struct NavPathBlob
{
    public BlobArray<float3> Waypoints;      // Path waypoints
    public BlobArray<PathTileSnapshot> TileSnapshots; // Version snapshots
}

public struct PathTileSnapshot
{
    public int2 TileCoord;                   // Surface tile coordinate
    public uint TileVersion;                 // Version when path was computed
    public PathTileType Type;                // Surface or Underground
}

public enum PathTileType : byte
{
    Surface = 0,
    Underground = 1
}

// Path invalidation check
bool IsPathValid(Entity agent, NavPathResult pathResult)
{
    var path = pathResult.Path.Value;
    
    // Check each tile version
    for (int i = 0; i < path.TileSnapshots.Length; i++)
    {
        var snapshot = path.TileSnapshots[i];
        
        if (snapshot.Type == PathTileType.Surface)
        {
            var tileEntity = GetTileEntity(snapshot.TileCoord);
            if (tileEntity != Entity.Null)
            {
                var tile = GetComponentRO<SurfaceNavTile>(tileEntity);
                if (tile.ValueRO.Version != snapshot.TileVersion)
                {
                    return false; // Tile version changed, path is stale
                }
            }
        }
        else // Underground
        {
            int3 chunkCoord = GetChunkCoordFromTile(snapshot.TileCoord);
            var chunkEntity = GetChunkEntity(chunkCoord);
            if (chunkEntity != Entity.Null)
            {
                var chunk = GetComponentRO<UndergroundNavChunk>(chunkEntity);
                if (chunk.ValueRO.Version != snapshot.TileVersion)
                {
                    return false; // Chunk version changed, path is stale
                }
            }
        }
    }
    
    return true; // All tiles still valid
}

// Agent path usage
void UsePath(Entity agent, NavPathResult pathResult)
{
    // Check if path is still valid
    if (!IsPathValid(agent, pathResult))
    {
        // Path is stale → request replan
        RequestReplan(agent);
        return;
    }
    
    // Path is valid → proceed with movement
    FollowPath(agent, pathResult);
}
```

**Result:** "Nav stays alive" with zero global recomputation. Agents replan only when their path's touched tiles change.

---

## 6. Digging Produces Resources (Terrain is a Resource)

### Core Concept

**When a voxel is dug, look up MaterialSpec.yields, apply DepositSpec if present, emit resource flow events into piles.**

```csharp
// Resource production from digging
void ProduceResourcesFromDigging(TerrainDelta delta)
{
    // Look up material spec
    var materialSpec = GetMaterialSpec(delta.OldMaterial);
    if (materialSpec == null)
        return;
    
    // Get base yields from material
    var yields = materialSpec.Yields.Value;
    
    // Apply deposit spec if DepositId != 0
    float gradeMultiplier = 1.0f;
    if (delta.DepositId != 0)
    {
        var depositSpec = GetDepositSpec(delta.DepositId);
        if (depositSpec != null)
        {
            // Grade affects yield
            float grade01 = delta.OreGrade / 255f;
            gradeMultiplier = GetGradeMultiplier(depositSpec, grade01);
            
            // Deposit may add extra products
            yields = MergeYields(yields, depositSpec.Products);
        }
    }
    
    // Emit resource flow events
    float3 digPosition = GetVoxelWorldPosition(delta.ChunkId, delta.LocalIndex);
    float volume = VoxelVolume; // Volume of dug voxel
    
    foreach (var yieldEntry in yields.Yields)
    {
        float amount = yieldEntry.AmountPerCubicMeter * volume * gradeMultiplier;
        
        // Create or add to resource pile at location
        CreateOrAddToResourcePile(digPosition, yieldEntry.ResourceId, amount);
    }
}

// Resource pile entity (aggregated)
public struct ResourcePile : IComponentData
{
    public float3 Position;
    public BlobAssetReference<ResourceAmountsBlob> Resources; // resourceId → amount
}

// Create or add to pile
void CreateOrAddToResourcePile(float3 position, FixedString64Bytes resourceId, float amount)
{
    // Check for nearby pile (within merge radius)
    var nearbyPile = FindNearbyPile(position, MergeRadius, resourceId);
    
    if (nearbyPile != Entity.Null)
    {
        // Add to existing pile
        var pile = GetComponentRW<ResourcePile>(nearbyPile);
        AddResource(pile, resourceId, amount);
    }
    else
    {
        // Create new pile entity
        var pileEntity = EntityManager.CreateEntity();
        EntityManager.AddComponent<ResourcePile>(pileEntity);
        EntityManager.AddComponent<LocalTransform>(pileEntity);
        
        var pile = new ResourcePile
        {
            Position = position,
            Resources = CreateResourceAmountsBlob(resourceId, amount)
        };
        EntityManager.SetComponentData(pileEntity, pile);
        EntityManager.SetComponentData(pileEntity, LocalTransform.FromPosition(position));
    }
}

// Existing logistics pattern works:
// - Site/builders publish needs
// - Haulers reserve, pick up piles, deliver
```

**Keep it aggregated:** Pile entity stores {resourceId → amount} in a small buffer. Existing logistics pattern handles delivery.

---

## 7. Water + Digging (Optional but Coherent)

### Core Concept

**Don't do full 3D fluids.** Surface water: heightfield layer. Tunnels: conduit graph. Digging that connects water→tunnel creates/updates conduit links + marks water dirty locally.

### Integration with Fluid Dynamics

```csharp
// Water + digging integration
void OnTerrainDug(TerrainDelta delta)
{
    // Check if digging connected water to tunnel
    float3 voxelPos = GetVoxelWorldPosition(delta.ChunkId, delta.LocalIndex);
    
    // Check if surface water exists above
    float surfaceWaterDepth = SampleWaterDepth(voxelPos);
    if (surfaceWaterDepth > 0.1f)
    {
        // Surface water exists → may flow into tunnel
        CheckWaterTunnelConnection(voxelPos, delta);
    }
    
    // Check if tunnel now connects to water source
    if (IsInTunnel(voxelPos))
    {
        CheckTunnelWaterSource(voxelPos, delta);
    }
}

void CheckWaterTunnelConnection(float3 position, TerrainDelta delta)
{
    // Digging that connects water→tunnel creates/updates conduit links
    // (See Fluid_Dynamics_System.md: Conduit Graph)
    
    // Create conduit surface coupling if needed
    if (ShouldCreateConduitCoupling(position))
    {
        CreateConduitSurfaceCoupling(position, GetConduitNodeAt(position));
        
        // Mark water dirty locally (water may flow into tunnel)
        MarkWaterDirty(position, CouplingRadius);
    }
}

// Water dirty marking (local only)
void MarkWaterDirty(float3 position, float radius)
{
    // Mark water grid cells dirty (see Fluid_Dynamics_System.md)
    var affectedCells = GetWaterCellsInRadius(position, radius);
    foreach (var cell in affectedCells)
    {
        MarkCellDirty(cell); // Water grid will update
    }
}
```

**Integration:** Uses existing Fluid Dynamics System conduit graph. Digging creates/updates conduit links, marking water dirty locally (no global recomputation).

---

## 8. MVP Build Order (Fastest Path)

### Phase 1: Surface Digging Only

**Heightfield edits + nav grid dirty update + path versioning**

1. Implement `TerrainChunk` with heightfield (2.5D, not full 3D voxels)
2. Implement `DigCommand` → `TerrainEditApplySystem` → terrain deltas
3. Implement `SurfaceNavTile` with versioning
4. Implement `SurfaceNavUpdateSystem` (update dirty tiles only)
5. Implement path versioning (`NavPathResult` with `PathTileSnapshot`)
6. Implement path invalidation check (agent replans when path stale)

### Phase 2: Resource Yields

**Resource yields from dug cells → piles → hauler delivery**

1. Implement `TerrainMaterialSpec` with yields
2. Implement resource production from digging (`ProduceResourcesFromDigging`)
3. Implement `ResourcePile` entities (aggregated amounts)
4. Integrate with existing hauling system (haulers pick up piles)

### Phase 3: Underground Void

**Tunnels in a small local area (chunked passability, no regions)**

1. Extend `TerrainChunk` to full 3D voxels (not just heightfield)
2. Implement `UndergroundNavChunk` with `PassableMask`
3. Implement `UndergroundNavUpdateSystem` (update dirty chunks only)
4. Extend path versioning to underground chunks

### Phase 4: Underground Pathing

**Local A* + version invalidation**

1. Implement underground pathfinding (A* over passable voxels in loaded chunks)
2. Extend path invalidation to check underground chunk versions
3. Integrate with agent movement (follow underground paths)

### Phase 5 (Later): Advanced Features

**Portal/HPA* for long underground routes + conduit water links + dams**

1. Implement region labeling (`RegionId` in `UndergroundNavChunkBlob`)
2. Implement portal clustering (HPA* style for long paths)
3. Implement conduit water links (digging creates conduit couplings)
4. Implement dams (block water flow in tunnels)

---

## Key Rules (Burst-Friendly)

### Non-Negotiables

1. **No Per-Voxel Entities:** Voxels are data in chunk blobs, not entities
2. **No Global Rebakes:** Only dirty tiles/chunks update
3. **All Edits are Diff Lists:** Store terrain deltas, not full chunk snapshots
4. **All Derived Nav Caches are Per-Tile and Versioned:** Surface tiles and underground chunks have versions
5. **Agents Replan Only When Path's Touched Tiles Change:** Version checking enables efficient invalidation

### Performance Guardrails

- **Edit Budget:** Max N edits per tick (prevent player spam from stalling sim)
- **Dirty Tile Limit:** Max M tiles updated per tick (throttle nav updates)
- **Chunk Loading:** Only update loaded/nearby chunks (streaming support)
- **LOD:** Far chunks use simplified nav (coarse grid, no detailed passability)

---

## Integration Summary

### Existing Systems Enhanced

- **TerrainVersion:** Used for global terrain change tracking
- **SurfaceFieldsChunk:** Extended with voxel data for digging
- **Navigation/Pathfinding:** Extended with versioned tiles/chunks
- **Resource Piles:** Used for digging yields (existing logistics pattern)
- **Fluid Dynamics:** Integrated via conduit graph (water + tunnels)

### New Systems Added

- **TerrainChunk:** Chunked voxel terrain data
- **TerrainEditApplySystem:** Command → diff → dirty marking
- **SurfaceNavTile:** Derived surface navigation (versioned)
- **UndergroundNavChunk:** Derived void navigation (versioned)
- **NavPathResult:** Versioned path with tile snapshots

---

## Related Documentation

- **Liquid-Terrain Integration:** `Docs/Concepts/Core/Liquid_Terrain_Integration.md` - Unified terrain + liquid system with shared dirty regions
- **Fluid Dynamics System:** `Docs/Concepts/Core/Fluid_Dynamics_System.md` - Conduit graph integration
- **Capabilities & Affordances:** `Docs/Concepts/Core/Capabilities_And_Affordances_System.md` - Dig traversal action
- **Terraforming:** `Docs/Archive/TODO/TerraformingPrototype_TODO.md` - Heightfield editing
- **Resource Piles:** `Docs/Concepts/Implemented/Resources/Aggregate_Piles.md` - Pile system

---

**For Implementers:** Focus on chunked terrain data, diff-based edits, versioned nav tiles/chunks, path invalidation checks, and integration with liquid system dirty regions  
**For Designers:** Focus on material specs, resource yields, digging gameplay balance, and terrain-liquid interaction gameplay

