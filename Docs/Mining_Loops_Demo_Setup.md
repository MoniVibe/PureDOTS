# Mining Loops Demo Setup - PureDOTS Foundation

This document describes the setup and architecture for showcasing both mining loops:
1. **Villager Mining (GodGame)** - Ground-based resource gathering
2. **Vessel Mining (Space4X)** - Space-based asteroid mining

## System Architecture

### Villager Mining Loop (GodGame Style)

**Systems Flow:**
1. `VillagerJobAssignmentSystem` - Assigns villagers to resource nodes
2. `VillagerAISystem` - Sets villager goals based on job assignments
3. `VillagerTargetingSystem` - Resolves target entities to positions
4. `VillagerMovementSystem` - Moves villagers toward targets
5. `VillagerJobExecutionSystem` - Gathers resources when close enough
6. `VillagerJobDeliverySystem` - Finds nearest storehouse
7. `ResourceDepositSystem` - Deposits resources to storehouses

**Components:**
- `VillagerJob` - Job assignment and phase tracking
- `VillagerJobTicket` - Resource and storehouse references
- `VillagerJobCarryItem` - Buffer of carried resources
- `VillagerAIState` - Goal and state tracking

### Vessel Mining Loop (Space4X Style)

**Systems Flow:**
1. `VesselAISystem` - Assigns vessels to asteroids, manages state transitions
2. `VesselTargetingSystem` - Resolves target entities (asteroids/carriers) to positions
3. `VesselMovementSystem` - Moves vessels toward targets (stops when mining)
4. `VesselGatheringSystem` - Gathers resources from asteroids when close enough
5. `VesselDepositSystem` - Deposits resources to carriers when full

**Components:**
- `MinerVessel` - Vessel capacity, load, and resource type
- `VesselAIState` - Goal and state tracking (Idle, MovingToTarget, Mining, Returning)
- `VesselMovement` - Movement velocity and speed
- `Carrier` - Carrier capacity and current load
- `CarrierInventoryItem` - Buffer of resources stored in carrier

## Key Differences

| Aspect | Villager Mining | Vessel Mining |
|--------|----------------|---------------|
| **Targets** | Resource nodes (wood, ore) | Asteroids (ore) |
| **Storage** | Storehouses | Carriers |
| **Job System** | Complex job ticket system | Simpler state machine |
| **Movement** | Continuous (may pause to gather) | Stops completely when mining |
| **Resource Types** | Multiple (wood, ore, etc.) | Primarily ore (space) |

## Scene Setup Requirements

### Required Singletons

1. **PureDotsConfigAuthoring** (in main scene)
   - References `PureDotsRuntimeConfig.asset`
   - References `PureDotsResourceTypes.asset`
   - Creates `ResourceTypeIndex` singleton

2. **ResourceRegistrySystem** 
   - Requires `ResourceTypeIndex` to function
   - Registers all resource sources

### SubScene Structure

```
Main Scene (e.g., Space4XMineLoop.unity)
├── PureDotsConfig (PureDotsConfigAuthoring)
├── Camera
└── SubScene Component
    └── GameEntities.unity (subscene)
        ├── Villager1, Villager2 (VillagerAuthoring)
        ├── MiningVessel1, MiningVessel2 (MiningVesselAuthoring)
        ├── ResourceNode_Wood1-3 (ResourceSourceAuthoring)
        ├── OreNode1-2 (ResourceSourceAuthoring)
        ├── Asteroid_Ore1-3 (ResourceSourceAuthoring)
        ├── Storehouse (StorehouseAuthoring)
        └── Carrier (CarrierAuthoring)
```

### Authoring Components Required

**Villagers:**
- `VillagerAuthoring` - Base speed, productivity

**Vessels:**
- `MiningVesselAuthoring` - Base speed, capacity, resource type index

**Resources:**
- `ResourceSourceAuthoring` - Resource type ID, initial units, gather rate, max workers, respawn settings

**Storage:**
- `StorehouseAuthoring` - Capacity entries for each resource type
- `CarrierAuthoring` - Total capacity

## New Systems Added

### VesselGatheringSystem
- Handles vessels gathering resources from asteroids
- Transitions vessels to Mining state when close enough
- Updates `ResourceSourceState` and `MinerVessel.Load`
- Automatically transitions to Returning state when vessel is full

### VesselDepositSystem
- Handles vessels depositing resources to carriers
- Checks distance to carrier
- Updates `Carrier.CurrentLoad` and `CarrierInventoryItem` buffer
- Transitions vessel back to Idle when empty

## System Ordering

### Villager Systems (in VillagerSystemGroup)
1. VillagerNeedsSystem
2. VillagerStatusSystem
3. VillagerJobAssignmentSystem
4. VillagerAISystem
5. VillagerTargetingSystem
6. VillagerMovementSystem

### Vessel Systems (in SimulationSystemGroup)
1. VesselAISystem (after ResourceRegistrySystem)
2. VesselTargetingSystem (after VesselAISystem)
3. VesselMovementSystem (after VesselTargetingSystem)
4. VesselGatheringSystem (after VesselMovementSystem)
5. VesselDepositSystem (after VesselGatheringSystem)

### Resource Systems (in ResourceSystemGroup)
1. ResourceReservationBootstrapSystem
2. ResourceGatheringSystem (villager gathering)
3. ResourceDepositSystem (villager deposits)
4. ResourceSourceManagementSystem (respawn logic)

## Demo Scene Checklist

- [ ] Main scene has PureDotsConfigAuthoring with config assets assigned
- [ ] SubScene component references GameEntities.unity
- [ ] All GameObjects in subscene have appropriate authoring components
- [ ] Resource nodes have ResourceSourceAuthoring with correct resourceTypeId
- [ ] Storehouse has capacity entries for "wood" and "ore"
- [ ] Carrier has CarrierAuthoring with totalCapacity set
- [ ] Villagers have VillagerAuthoring with baseSpeed set
- [ ] Vessels have MiningVesselAuthoring with baseSpeed and capacity set
- [ ] SubScene is closed (not open) during Play mode

> **Automation tip:** Run `Space4X > Setup Dual Mining Demo Scene` to apply villager presets, assign placeholder meshes, and wire resource references automatically.

### Diagnostics & QA

- `Space4XCameraDiagnostics` singleton captures per-frame tick counts, stale input detections, and budget state. View it via **Space4X ▸ Diagnose Camera & Entities**.
- `VillagerJobDiagnostics` singleton exposes job queue and idle villager counts; the same diagnostic menu prints its summary.
- `PureDOTS.Diagnostics.CatchupHarness` component injects periodic frame hitches and logs both diagnostics. Add it to a scene object to validate catch-up behaviour.
- `Assets/Tests/Playmode/DualMiningDeterminismTests` runs the demo scene twice and asserts identical ECS hashes after 180 ticks to guard against determinism regressions.

## Expected Behavior

### Villagers
1. Spawn or start idle
2. Get assigned to nearest resource node via VillagerJobAssignmentSystem
3. Move toward resource node
4. Gather resources when within range (3 units)
5. When carrying enough (40 units), transition to Delivering phase
6. Find nearest storehouse with capacity
7. Move to storehouse and deposit resources
8. Return to Idle, repeat

### Vessels
1. Start idle
2. VesselAISystem finds nearest asteroid
3. Move toward asteroid (MovingToTarget state)
4. When close enough (3 units), transition to Mining state
5. Gather resources continuously while in Mining state
6. When full (95% capacity), transition to Returning state
7. Move back to carrier
8. When close enough (2 units), deposit resources
9. Return to Idle when empty, repeat

## Debugging

### Console Messages
- `[VesselAISystem]` - Vessel AI state changes and target assignments
- `[VesselMovementSystem]` - Vessel movement status
- `[ResourceCatalogDebug]` - Resource registration status
- `[MovementDiagnostic]` - Comprehensive movement diagnostics

### Common Issues

**Vessels don't move:**
- Check ResourceRegistry has entries
- Verify vessels have MiningVesselAuthoring
- Check VesselAIState.TargetEntity is not null

**Vessels don't gather:**
- Verify VesselGatheringSystem is running
- Check distance to asteroid (should be < 3 units)
- Verify ResourceSourceState.UnitsRemaining > 0

**Vessels don't deposit:**
- Check Carrier has CarrierAuthoring
- Verify distance to carrier (should be < 2 units)
- Check carrier capacity is not full

**Villagers don't move:**
- Check VillagerJobAssignmentSystem assigns jobs
- Verify VillagerJobTicket has ResourceEntity set
- Check VillagerAvailability.IsAvailable == 1

## Next Steps

1. Extend placeholder visuals with authored prefabs/FX while preserving the deterministic surrogates.
2. Add HUD widgets that surface `Space4XCameraDiagnostics` / `VillagerJobDiagnostics` directly in play mode.
3. Integrate the determinism test into CI and run `CatchupHarness` scenarios on perf bots.
4. Add particles/animation hooks for mining states once placeholder visuals are replaced.
5. Produce reusable prefabs for villagers, vessels, carriers, and resource nodes with presets baked in.



