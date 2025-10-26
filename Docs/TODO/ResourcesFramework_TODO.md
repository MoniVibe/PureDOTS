# Resources Framework TODO (BW2 Parity)

> **Generalisation Guideline**: Treat resource flow (chunks, aggregates, storehouses) as a reusable framework for any resource type. Avoid game-specific behaviours; differences should come from `ResourceProfile` data, not code.

## Goal
- Recreate Black & White 2 resource behaviour in DOTS: physical resource chunks (wood, ore, animals, food) that become aggregates when delivered, with storehouse and construction integration, player/villager interactions, and deterministic processing.
- Provide a reusable template so new resource types can be added via configuration without rewriting code.
- Ensure resource flow is deterministic, rewind-safe, and performs at scale (thousands of chunks/piles).
- Keep contracts aligned with `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and the shared guidance in `Docs/TODO/SystemIntegration_TODO.md`.

## Plain-Language Primer
- Resources exist as **chunks** (physical tokens) that can be carried, thrown, or dropped. When they hit a storehouse/construction intake they convert into **aggregate** (stored totals).
- Dropped aggregates form **resource piles** that tumble, settle, and can be siphoned by the hand or villagers.
- Storehouses track totals per resource; construction sites consume aggregates to progress builds.
- Everything needs consistent rules so villagers and the hand can manipulate chunks and piles effortlessly.

## BW2 Resource Reference Behavior

### Resource Forms

**1. Source Nodes** (Trees, Berry Bushes, Ore Deposits):
- Fixed location resource generators
- Villagers gather from them over time
- Can be hand-picked (uproot tree, grab bush)
- Deplete and respawn/regrow

**2. Chunks** (Physical Objects):
- Spawned when villagers harvest (wood log, food bundle, ore piece)
- Spawned when player miracle creates resources (food/wood miracle)
- Physical entities with collision, can tumble/roll
- Carried by villagers or divine hand
- Thrown by divine hand (bounce, settle)

**3. Aggregate Piles** (Settled Chunks):
- Form when chunks settle on ground and stop moving
- Multiple chunks of same type merge into single pile entity
- Shows visual stack (larger = more resources)
- Immovable (can't be pushed)
- Can be siphoned by hand or gathered by villagers

**4. Storehouse Inventory** (Abstract Float):
- Chunks/piles absorbed by storehouse become float storage
- No longer physical entities, just data
- Villagers withdraw abstract amounts, spawn as chunks

### Lifecycle Flow
```
SOURCE NODE (tree, bush, ore)
    ↓ [villager gathers]
CHUNK spawns (physical object)
    ↓ [tumbles/rolls on ground]
SETTLES after velocity < threshold
    ↓ [converts to pile]
AGGREGATE PILE (stationary entity)
    ↓ [villager carries to storehouse]
    OR [hand siphons]
    OR [merges with nearby same-type pile]
STOREHOUSE INVENTORY (float storage)
    ↓ [villager withdraws]
CHUNK spawns again (cycle repeats)
```

## System Update Flow (Per-Tick Order)

Systems run in this order each tick to ensure correct data flow:

1. `ResourceChunkSpawnSystem` - Processes spawn commands (from harvesting, miracles, withdrawals)
2. `ResourceChunkPhysicsSystem` - Applies gravity, friction, collisions, tumbling
3. `ResourceChunkSettleDetectionSystem` - Detects when velocity below threshold for duration
4. `ResourceChunkToPileConversionSystem` - Converts settled chunks to piles (or merges into existing)
5. `ResourcePileMergeSystem` - Merges nearby piles of same type (runs every 30 ticks)
6. `ResourcePileSizeUpdateSystem` - Updates visual size category based on units
7. `ResourcePileRegistrySystem` - Updates registry for villager/hand queries
8. `ResourceHandSiphonSystem` - Handles hand siphoning from piles
9. `ResourceStorehouseAggregateSystem` - Absorbs chunks/piles into storehouse float storage
10. `ResourceConstructionAggregateSystem` - Absorbs chunks/piles into construction sites

All systems respect `RewindState.Mode` and skip updates during playback.

## Spatial Grid Integration

### Pile Discovery
- Piles registered in spatial grid for quick radius queries
- Villagers query "nearest pile of resource type X within 50m" (no linear scan)
- Hand siphon uses spatial query for hover detection and targeting

### Merge Detection
- `ResourcePileMergeSystem` queries spatial grid for same-type piles within 1m radius
- Deterministic ordering (sort results by entity index before processing)
- Smaller piles merge into larger ones (prevents oscillation)

### Chunk Settlement
- `ResourceChunkToPileConversionSystem` checks spatial grid for nearby piles (<2m)
- If pile exists and not at capacity: merge chunk into pile, destroy chunk
- Otherwise: create new pile entity at chunk position, add to spatial grid

### Performance Benefits
- Spatial queries O(neighbors) vs. O(all_piles) for linear scan
- Scales to 5k+ piles without performance degradation
- Villager job assignment instant (registry + spatial query)

## Dependencies & Shared Infrastructure
- Use environment grid cadence (moisture/temperature/wind/sunlight sampling) when resource systems react to climate (e.g., drying piles, rain-soaked wood).
- Adopt shared registry utilities (`DeterministicRegistryBuilder`, resource/storehouse registries) and spatial query helpers for all proximity lookups.
- Feed hauler/miner agents through the shared `AISystemGroup` pipeline; author resource-specific behaviour by supplying `AISensorConfig` ranges and utility blobs rather than bespoke AI systems.
- Integrate with the central hand/router (`HandInputRouterSystem`, `HandInteractionState`) for siphon priorities, cooldowns, and rewind guards.

## Workstreams & Tasks

### 0. Recon & Spec Alignment
- [x] Audit current resource systems (`ResourceGatheringSystem`, `ResourceDepositSystem`, `StorehouseInventorySystem`) to identify existing data structures and gaps.
- [x] Review legacy truth sources (`Aggregate_Resources.md`, `Storehouse_API.md`, `RMBtruthsource.md`) for contracts (pile API, storehouse API, interaction priority).
- [x] Inventory per-system pooling usages (Native containers, ECBs, entity instantiation) and flag candidates for shared `Nx.Pooling` adoption.
- [ ] Document resource types needed (wood, ore, food, animal, miracle tokens?) and expected behaviours (tumble physics, stacking).
- [ ] Inventory existing assets (resource models, pile visuals) and note missing ones.
- [ ] Confirm rewind requirements (resource history, pile states) and identify components needing history logging.

### 1. Data & Asset Model
- [ ] Create `ResourceProfile` ScriptableObject:
  - Resource type id, display name, alignment impact
  - Chunk mass, size, default stack amount
  - Aggregate max per pile, tumble physics settings
  - Storehouse conversion value (units per chunk)
  - Visual references for chunk/pile states
- [ ] Build `ResourceProfileBlob` for runtime lookup (Burst-friendly).
- [ ] **Define enhanced chunk components** in `ResourceComponents.cs`:
  - `ResourceChunkState` (existing, enhance with SettleTimer field)
  - `ResourceChunkPhysics` - Mass, Friction, Bounciness, SettleVelocityThreshold, SettleTimeRequired, CollisionLayer
  - `HandPickupableChunk` - Tag with Mass, ThrowMultiplier for hand interaction
- [ ] **Define pile components** (NEW):
  - `ResourcePile` - ResourceTypeIndex, TotalUnits, MaxCapacity, SizeCategory, CreationTick, LastMergeTick, Flags
  - `ResourcePileMergeCandidate` buffer - OtherPile entity, Distance, OtherUnits
  - `HandSiphonable` - Tag with SiphonRate, MaxHandCapacity
- [ ] **Define siphon/hand state** (NEW):
  - `ResourceSiphonState` (on divine hand) - TargetPile, ResourceTypeIndex, UnitsHeld, SiphonRate, SiphonTimer, IsActive
- [ ] **Define pile registry** (NEW):
  - `ResourcePileRegistry` singleton - TotalPiles, TotalPilesByType array, TotalUnitsInPiles, LastUpdateTick
  - `ResourcePileEntry` buffer - PileEntity, ResourceTypeIndex, Position, TotalUnits, SizeCategory, SpatialCellIndex, AvailableForGathering
- [ ] Update bakers to convert profiles and authoring components (chunks, piles, storehouses) into runtime data.
- [x] Ensure pooling capacities for chunk/pile prefabs wired through shared pooling settings (config-driven warmup via `PoolingSettingsConfig`).

### 1.5 Resource Nodes & Regeneration
- [ ] Catalogue node archetypes and behaviour rules:
  - **Tree / Wood Nodes** (integrate with vegetation systems): trees regrow and propagate naturally; harvesting spawns wood chunks but does not permanently remove resource unless tree is destroyed.
  - **Food Bush / Crop Nodes**: produce food chunks; nodes deplete after harvest and regenerate seasonally (spring/summer) based on biome profile.
  - **Ore / Mine Nodes**: provide infinite output but purity/quality declines with continuous extraction (reducing chunk value or requiring more chunks per building). Purity slowly recovers when idle.
- [ ] Define `ResourceNode` component storing NodeType, CurrentPurity, MaxPurity, RegenTimer, LastHarvestTick.
- [ ] Update gathering systems to read node state, adjust chunk output (units, quality) accordingly.
- [ ] Hook node regeneration into climate/biome events (seasonal triggers, miracles).
- [ ] Leave extension points for future metals/minerals (rare nodes, deep mining, explosives).

### 2. Chunk Lifecycle & Physics
- [ ] **Add ResourceChunkPhysics component** with mass, friction, bounciness per resource type
- [ ] **Implement ResourceChunkPhysicsSystem**:
  - Apply gravity (-9.81 m/s²), friction, air resistance
  - Handle collision with terrain (Unity Physics or raycast-based)
  - Apply bounciness on collision (restitution coefficient)
  - Update velocity deterministically (fixed timestep from TimeState)
  - Optional: Chunk-to-chunk collision (expensive, may skip initially)
- [ ] **Implement ResourceChunkSettleDetectionSystem**:
  - Monitor velocity.magnitude each tick
  - If below SettleVelocityThreshold (0.1 m/s) → increment SettleTimer
  - If SettleTimer >= SettleTimeRequired (0.5s) → mark for pile conversion
  - If velocity spikes above threshold → reset SettleTimer
- [ ] **Implement ResourceChunkToPileConversionSystem**:
  - Query settled chunks
  - Check spatial grid for nearby piles (<2m) of same type
  - If pile exists: add chunk units to pile, destroy chunk
  - If no pile: create new ResourcePile entity at chunk position
- [ ] Ensure deterministic random seeds for tumble orientation and settle position (use tick + entityID).
- [ ] **Add ResourceChunkState.Flags**: Settling, PendingMerge to track lifecycle
- [ ] Adopt shared pooling utilities for chunk/entity reuse (chunk spawn/despawn should pull from central pools).
- [ ] Review chunk data layout for SoA compliance (separate position/velocity/mass arrays if performance dictates).
- [ ] Keep chunk/aggregate behaviour data-driven (physics, settle thresholds, interaction rules defined in `ResourceProfile`).
- [x] Route spawn/despawn through pooled command buffer helpers so chunk systems do not create transient ECBs per frame.

### 3. Storehouse & Construction Integration
- [ ] Create `StorehouseIntakeSystem`:
  - On chunk contact with storehouse intake collider, convert chunk units to storehouse aggregate totals.
  - Trigger UI events (`OnTotalsChanged`); handle capacity (reject overflow, create pile outside).
- [ ] Create `ConstructionSiteIntakeSystem`:
  - Similar to storehouse but routes resources to construction progress.
  - Update construction state; spawn leftover pile if over-delivered.
- [ ] Ensure `ResourceAggregate` totals stay in sync with registries.
- [ ] Implement `ResourceHistorySystem` recording add/remove events for rewind.
- [x] Route storehouse/construction spawn/despawn through shared spawn pipeline to maintain deterministic ordering.

### 4. Aggregate Piles
- [ ] **Implement ResourcePileMergeSystem**:
  - Query spatial grid for same-type piles within merge radius (1m)
  - Deterministic ordering: sort by entity index (lower absorbs higher)
  - Transfer units from smaller pile to larger pile
  - Destroy absorbed pile, update larger pile's TotalUnits
  - Update LastMergeTick for history tracking
  - Run every 30 ticks (optimization, not every frame)
- [ ] **Implement ResourcePileSizeUpdateSystem**:
  - Monitor TotalUnits, determine size category (Tiny/Small/Medium/Large/Huge)
  - Size thresholds: [50, 150, 400, 800, 1000] units
  - Swap pile prefab/mesh when crossing threshold
  - Update visual scale smoothly (lerp or instant based on config)
- [ ] **Implement ResourcePileRegistrySystem**:
  - Update registry each tick (during record mode only)
  - Cache SpatialCellIndex and position for queries
  - Mark AvailableForGathering based on reservations
  - Provide totals for analytics/HUD
- [ ] Convert dropped chunks into piles (handled by ResourceChunkToPileConversionSystem).
- [ ] Ensure piles are immovable (no Velocity component, static position).
- [ ] **Biomes & node behaviour**:
  - Resource nodes remain the authoritative source; piles represent transient dropped aggregates.
  - Piles should support seasonal visuals (snow cover, wetness) but do not change node regeneration.

### 5. Player Hand & Villager Interactions
- [ ] **Hand chunk pickup** (individual chunks):
  - Add `HandPickupableChunk` tag to all chunk entities
  - Integrate with existing hand grab system (reuse grab/hold/throw logic)
  - Chunks follow hand cursor when held (lerp movement)
  - Throw mechanics reuse hand slingshot system
  - Chunks retain physics after throw (tumble on landing)
- [ ] **Hand pile siphon** (RMB hold mechanic):
  - Implement `ResourceHandSiphonSystem` running each tick
  - Start siphon: RMB hold over pile → validate mana/capacity → begin transfer
  - Per-tick update: transfer SiphonRate * DeltaTime units from pile to hand
  - Type locking: ResourceTypeIndex locked until hand emptied
  - Capacity limit: Stop at 500 units (hand full feedback)
  - Distance check: Break siphon if hand moves >3m from pile
  - Visual: Particle stream from pile to hand, pile shrinks, hand fills
  - Audio: Siphon loop sound while active
- [ ] **Hand dump to storehouse**:
  - Detect when hand holds resources AND hovers over storehouse
  - Auto-trigger dump (add to RMB priority router)
  - Transfer all hand units to storehouse inventory instantly
  - Visual/audio feedback (absorption particles, sound)
  - Clear hand ResourceTypeIndex (unlock for new type)
- [ ] **Integrate with RMB priority router**:
  - Update priority: StorehouseDump > PileSiphon > ChunkPickup > GroundDrip
  - Add hysteresis (3 frames) to prevent mode jitter
- [ ] **Villager chunk spawning**: Villagers gathering from sources spawn chunks at their position
- [x] Ensure villager spawn flow uses pooled chunk entities (request from `Nx.Pooling` rather than direct instantiate).
- [ ] **Villager chunk carrying**:
  - Chunks auto-attach to villager (Carried flag, Carrier = villager entity)
  - Visual: Chunk entity follows villager position (offset)
  - Capacity: Villager can carry limited chunks (weight/volume limit)
- [ ] **Villager chunk delivery**:
  - Villager reaches storehouse/construction → chunks aggregate (handled by intake systems)
  - Chunks transfer from villager inventory to destination
  - Visual: Chunks disappear, counters update
- [ ] **Villager pile gathering** (future - optional):
  - Villagers can gather from piles as alternative to sources
  - Registry flags piles as gatherable nodes
  - Reduces pile units, villager gains chunk/units

### 6. Token Miracles & Special Resources
- [ ] Reuse resource chunk model for miracle tokens (alignment-adjusted weights, effect triggers).
- [ ] Provide generic chunk → miracle effect hook (e.g., deliver to altar to cast).
- [ ] Ensure miracle framework can request resource tokens (integration with `MiraclesFramework_TODO.md`).

### 7. Registry & Analytics
- [ ] Extend `ResourceRegistry` to include:
  - Chunk entities (position, units, state)
  - Aggregate piles (location, units)
  - Storehouse aggregates (per resource)
- [ ] Provide queries for villager jobs and presentation (total resources, available piles).
- [ ] Emit analytics/log events for resource flow (gathered, stored, consumed).
- [ ] Adopt shared registry utilities (`RegistryUtilities.cs`) to align with integration plan.

### 8. Testing & Validation
- [ ] Unit tests for chunk state transitions, storehouse intake, pile merging.
- [ ] Playmode tests:
  - Throw chunk at storehouse → aggregate increases, chunk removed.
  - Drop chunk on ground → pile created with correct units.
  - Siphon/pickup flows for hand and villager (deterministic results).
  - Deplete resource nodes → chunk production stops.
- [ ] Rewind tests: record → gather/dump → rewind; ensure totals match original state.
- [ ] Stress tests with thousands of chunks/piles; ensure zero GC, acceptable frame time.

### 9. Tooling & Documentation
- [ ] Add debug overlays for resource piles (units, type) and storehouse totals.
- [ ] Provide editor gizmos for pile merge radius, storehouse intake zones.
- [ ] Update guides (`Docs/Guides/SceneSetup.md`, new `Docs/DesignNotes/ResourcesFramework.md`) explaining resource authoring and configuration.
- [ ] Document field tuning (siphon rates, chunk mass).

## Configuration Reference (BW2 Values)

### Chunk Physics (per resource type)
```
Wood:
  Mass: 5kg
  Friction: 0.5
  Bounciness: 0.2
  Tumble: Rolls moderately

Food:
  Mass: 2kg
  Friction: 0.8
  Bounciness: 0.1
  Tumble: Stops quickly

Ore/Stone:
  Mass: 10kg
  Friction: 0.3
  Bounciness: 0.4
  Tumble: Rolls far, heavy bounces
```

### Settle Parameters
```
VelocityThreshold: 0.1 m/s
SettleTime: 0.5 seconds
MergeRadius (chunks): 2m
MergeRadius (piles): 1m
```

### Pile Sizes
```
Tiny: 1-50 units (0.3m visual radius)
Small: 50-150 units (0.6m radius)
Medium: 150-400 units (1.0m radius)
Large: 400-800 units (1.5m radius)
Huge: 800-1000 units (2.0m radius)
MaxCapacity: 1000 units per pile
```

### Siphon Settings
```
SiphonRate: 50 units/second
HandCapacity: 500 units maximum
SiphonStartDelay: 0.1s (prevent instant drain)
TypeLockDuration: Until hand emptied
MaxSiphonDistance: 3m (breaks if hand moves away)
```

### Aggregation
```
StorehouseAbsorbRadius: 5m
ConstructionAbsorbRadius: 3m
AggregationCooldown: 0.1s per chunk (prevent spam)
```

## BW2 Parity Checklist
- [ ] Resources spawn as physical chunks (wood logs, food bundles, ore pieces)
- [ ] Chunks tumble and roll realistically based on terrain and physics
- [ ] Chunks settle after brief time below velocity threshold
- [ ] Settled chunks convert to aggregate piles automatically
- [ ] Piles of same type merge when close together (<1m)
- [ ] Piles scale visually based on stored amount (5 size tiers)
- [ ] Hand can siphon piles with RMB hold (particles flow from pile to hand)
- [ ] Hand type-locked during siphon (can't mix resource types)
- [ ] Hand dumps resources to storehouse automatically when hovering
- [ ] Villagers gather from sources, spawn chunks at their position
- [ ] Villagers carry chunks and deliver to storehouse/construction
- [ ] Chunks aggregate into storehouse (become float storage, chunk destroyed)
- [ ] Construction sites accept resource chunks, add to material requirements
- [ ] Piles act as gatherable nodes for villagers (alternative to sources)
- [ ] Visual feedback clear for all transitions (tumble, merge, aggregate, siphon)

## Resource Types (Initial Set)
1. **Wood Log** – chunk from trees; used for construction, storehouse aggregate.
2. **Ore Chunk** – from ore nodes; used for advanced construction.
3. **Food Crate** – from farms; consumed by villagers.
4. **Animal** – livestock resource; can be converted to food or miracles (e.g., sacrifice).
5. **Gold Tribute** (optional) – alignment/tribute resource.

## Rewind Compatibility

### Chunk Physics Determinism
- Use fixed timestep for physics (matches TimeState.FixedDeltaTime)
- Deterministic collision resolution (sorted entity pairs if chunk-to-chunk enabled)
- Velocity/position clamping to prevent floating-point drift
- Seed-based randomness (tick + entity ID) for tumble direction variations

### State Snapshots
```
ResourceChunkHistorySample : IBufferElementData
{
  uint Tick;
  float3 Position;
  float3 Velocity;
  float Units;
  byte Flags; // Carried, Settling, etc.
}

ResourcePileHistorySample : IBufferElementData
{
  uint Tick;
  float TotalUnits;
  byte SizeCategory;
  uint LastMergeTick;
}
```

### Aggregation Events
- Record when chunks merge into piles (event log with tick, entities involved)
- Record when piles merge (source pile ID, target pile ID, units transferred)
- Record storehouse deposits (amount, resource type, tick)
- Replay produces identical merge sequences and final totals

### Rewind Modes
- **Record**: Update all systems normally, write snapshots
- **Playback**: Freeze chunk physics, restore positions from history
- **Catch-Up**: Fast-forward physics deterministically, rebuild piles

## File Structure
```
Assets/Scripts/PureDOTS/
  Runtime/
    ResourceComponents.cs        (EXPAND - add physics, pile, siphon components)
  
  Systems/
    ResourceChunkSpawnSystem.cs           (EXPAND existing)
    ResourceChunkPhysicsSystem.cs         (NEW - deterministic tumble physics)
    ResourceChunkSettleDetectionSystem.cs (NEW - velocity monitoring)
    ResourceChunkToPileConversionSystem.cs (NEW - chunk → pile)
    ResourcePileMergeSystem.cs            (NEW - merge nearby piles)
    ResourcePileSizeUpdateSystem.cs       (NEW - visual scaling)
    ResourcePileRegistrySystem.cs         (NEW - registry updates)
    ResourceHandSiphonSystem.cs           (NEW - RMB hold siphon)
    ResourceStorehouseAggregateSystem.cs  (NEW - storehouse absorption)
    ResourceConstructionAggregateSystem.cs (NEW - construction delivery)
  
  Authoring/
    ResourceProfileAsset.cs      (NEW - ScriptableObject for resource config)
  
  Tests/
    ResourceChunkTests.cs        (NEW - physics, settle, pile conversion)
    ResourcePileTests.cs         (NEW - merge, scaling, registry)
    ResourceSiphonTests.cs       (NEW - hand siphon mechanics)
    ResourceChunkRewindTests.cs  (NEW - determinism validation)
```

## Dependencies & Links
- **Villager job systems** (see `VillagerSystems_TODO.md`) - gathering, carrying, delivering chunks
- **Storehouse registry** - inventory management, capacity checks, float storage
- **Spatial grid** (see `SpatialServices_TODO.md`) - pile discovery, merge detection, chunk settlement
- **Divine hand system** (see `DivineHandCamera_TODO.md`) - siphon, pickup, dump mechanics, `HandInputRouterSystem`
- **Construction system** - material requirements, progress tracking, completion
- **Miracles framework** (see `MiraclesFramework_TODO.md`) - miracle-spawned resource chunks
- **Physics system** - Unity Physics or custom deterministic physics for tumbling
- **Rewind system** - history buffers, snapshot/restore, deterministic replay
- **VFX/presentation** - siphon particles, aggregation effects, pile visuals, size scaling
- **Vegetation systems** - tree propagation affecting wood nodes; seasonal data for food bushes
- **Terraforming prototype** - terrain/biome changes may alter node regeneration in future

## Testing Strategy

### Unit Tests
- Chunk physics (deterministic tumble, bounce, settle timing)
- Pile merge logic (correct pile selected, units transferred accurately)
- Siphon rate calculations (elapsed time → correct units transferred)
- Type locking enforcement (deny mixed-type siphon attempts)
- Capacity limits (hand capacity, pile max, storehouse capacity)
- Aggregation math (chunk units + pile units = correct total)

### Playmode Tests
- Full chunk lifecycle (spawn → tumble → settle → pile → siphon → dump → aggregate)
- Villager gather-carry-deliver loop with chunks (end-to-end)
- Multiple chunks merging into single pile (2+ chunks → 1 pile)
- Storehouse aggregation accuracy (sum of chunks = inventory increase)
- Construction material delivery completion (all materials → construction done)
- Spatial grid integration (find nearest pile query works)
- Hand siphon full flow (start → fill → dump → unlock)

### Rewind Tests (in ResourceChunkRewindTests.cs)
- Chunk tumble physics identical on replay (same position at same tick)
- Pile merge sequence reproduces exactly (same piles merge, same order)
- Siphon progress matches (same units transferred per tick)
- Aggregation events replay correctly (same amounts added to storehouse)
- Physics determinism (no float drift over 100+ ticks)

### Performance Tests
- 1k chunks tumbling simultaneously (measure physics update time)
- 5k piles scattered across map (merge detection performance)
- Siphon from massive pile (1000 units) runs smoothly (no hitches)
- Merge detection with 100+ nearby piles (spatial grid efficiency)
- Zero GC allocations per frame (Burst compliance verified)

## Success Criteria

### BW2 Parity Achieved When:
- Chunks tumble and behave like BW2 resources (designer feel test)
- Piles merge naturally, visual size scaling appropriate
- Siphoning feels smooth and satisfying (matches BW2 flow)
- Aggregation instant and clear (storehouse/construction feedback)
- Economy flows naturally (sources → chunks → piles → storage → consumption)

### System Complete When:
- All 10 implementation phases complete and tested
- Rewind deterministic (100% replay accuracy on all tests)
- Performance targets met (10k chunks at 60 FPS minimum)
- Integration with villagers, hand, storehouses seamless (no bugs)
- Configuration assets allow tuning without code changes
- Documentation complete (designer can add new resource types)

## Open Questions

1. Should chunks have lifetime (despawn after 5 minutes if uncollected)?
2. Do we need chunk-to-chunk collision, or only chunk-to-terrain (performance concern)?
3. Should piles have visual variety (different arrangements per size tier)?
4. Can villagers carry multiple chunk types simultaneously (mixed inventory)?
5. Should there be "mega piles" (>1000 units) or strictly enforce cap?
6. Do chunks float in water, or sink (water body interaction)?
7. Should wind affect light chunks (food, leaves blown around)?
8. Can miracles create chunks directly (food/wood miracle spawns chunks)?
9. Should chunks be destructible (fire burns wood chunks)?
10. Do we need "chunk trails" (visual effect of resources flying from tree to villager)?
11. Should pile merging be instant or animated (visual transition)?
12. Do we need different pile shapes per resource type (wood pile vs. ore pile vs. food pile)?
13. How should resource node purity regeneration scale (linear, exponential) and can players influence it?
14. Do we need quality tiers for ore/metal chunks now or defer until metallurgy systems exist?

## Implementation Phases & Time Estimates

### Phase 1: Chunk Physics & Settlement (1-2 weeks)
- Add ResourceChunkPhysics component
- Implement ResourceChunkPhysicsSystem with deterministic physics
- Add ResourceChunkSettleDetectionSystem
- Implement ResourceChunkToPileConversionSystem
- Add ResourcePile component
- **Validation**: Chunks tumble naturally, settle, convert to piles

### Phase 2: Pile Merging & Management (1 week)
- Implement ResourcePileMergeSystem with spatial grid
- Add deterministic merge ordering
- Implement ResourcePileSizeUpdateSystem
- Add size category thresholds and prefab swapping
- **Validation**: Piles merge correctly, visual size appropriate

### Phase 3: Hand Siphon Mechanics (1-2 weeks)
- Add ResourceSiphonState to divine hand
- Implement ResourceHandSiphonSystem
- Integrate with RMB router
- Add type locking and capacity limits
- Visual/audio feedback
- **Validation**: Siphoning feels smooth, type locking works

### Phase 4: Chunk Pickup & Throw (1 week)
- Add HandPickupableChunk tag
- Integrate with existing hand grab system
- Add throw mechanics
- Test throw → tumble → settle → pile
- **Validation**: Throwing chunks feels satisfying

### Phase 5: Storehouse Aggregation (1 week)
- Implement ResourceStorehouseAggregateSystem
- Chunks/piles absorbed into float storage
- Update storehouse registry
- Visual/audio feedback
- **Validation**: Resources aggregate correctly, totals accurate

### Phase 6: Construction Site Integration (1 week)
- Implement ResourceConstructionAggregateSystem
- Material delivery and progress tracking
- Construction completion logic
- **Validation**: Construction works with chunk delivery

### Phase 7: Villager Chunk Interaction (1 week)
- Villagers spawn chunks when harvesting
- Chunks auto-attach to villagers
- Delivery to storehouse/construction
- Update job system for chunk lifecycle
- **Validation**: Villagers handle chunks correctly

### Phase 8: Pile as Job Nodes (1 week)
- Add piles to resource registry
- Villagers query and gather from piles
- Depleted pile cleanup
- **Validation**: Villagers use piles intelligently

### Phase 9: Rewind & Determinism (1 week)
- Add chunk/pile history buffers
- Implement snapshot/restore for physics
- Test and fix non-determinism
- **Validation**: Rewind produces identical states

### Phase 10: Optimization & Polish (1-2 weeks)
- Optimize physics (Burst jobs)
- Optimize merge detection
- Add chunk pooling
- LOD for distant chunks
- **Validation**: 10k chunks runs at 60+ FPS, zero GC

**Total Estimated Time**: 10-14 weeks for complete BW2 parity

## Resource Types (Initial Set)
1. **Wood Log** – chunk from trees; used for construction, storehouse aggregate.
2. **Ore Chunk** – from ore nodes; used for advanced construction.
3. **Food Crate** – from farms; consumed by villagers.
4. **Animal** – livestock resource; can be converted to food or miracles (e.g., sacrifice).
5. **Gold Tribute** (optional) – alignment/tribute resource.

## Next Steps & Implementation Order
1. **Complete recon** (Workstream 0) - 2-3 days
2. **Data model & components** (Workstream 1) - 1 week
3. **Chunk physics foundation** (Phase 1) - 1-2 weeks
4. **Pile system** (Phases 2 & 4) - 2 weeks
5. **Hand siphon** (Phase 3) - 1-2 weeks
6. **Storehouse/construction aggregation** (Phases 5 & 6) - 2 weeks
7. **Villager integration** (Phases 7 & 8) - 2 weeks
8. **Rewind & testing** (Phase 9) - 1 week
9. **Optimization & polish** (Phase 10) - 1-2 weeks
10. **Documentation** (Workstream 9) - 3-4 days

Finalize resource catalogue and desired behaviours with design team. Draft detailed design note (`ResourcesFramework.md`) outlining component layout and state machines. Track progress in `Docs/Progress.md` and update this TODO as milestones complete.
