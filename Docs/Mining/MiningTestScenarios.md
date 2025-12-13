# Mining Loop Test Scenarios

This document describes test scenarios for validating the hardened mining loops in both Space4X and Godgame.

## Space4X Test Scenarios

### Scenario 1: Stable Mining Loop

**Setup:**
- 3 carriers positioned at different locations
- 10 mining vessels
- 20 asteroids with MineableSource components
- All entities have proper LocalTransform components

**Expected Behavior:**
1. Miners should find nearest asteroids and begin mining
2. When cargo capacity reached, miners should return to assigned carrier
3. After delivery, miners should return to asteroid (or find new one if depleted)
4. Loop should continue indefinitely without miners getting stuck

**Validation:**
- No miners stuck in invalid states
- Resource totals increase over time
- No exceptions or errors in console
- MiningDiagnostics shows stable active session count

### Scenario 2: Physics Disruption - Pickup and Throw

**Setup:**
- Same as Scenario 1
- Player can pick up miners and asteroids using hand interaction

**Test Steps:**
1. Let miners establish mining loops
2. Pick up a miner mid-flight (while going to asteroid or returning to carrier)
3. Throw the miner to a different location
4. Observe miner behavior after landing

**Expected Behavior:**
- Miner should detect MovementSuppressed/BeingThrown and pause mining logic
- After BeingThrown removed, miner should:
  - Validate MiningSession source/carrier still valid
  - If valid and still in reasonable range, resume mining
  - If too far or invalid, gracefully reset to Idle and find new targets
- No hard exceptions or infinite loops

**Validation:**
- MiningDiagnostics.PhysicsDisruptionResets increments appropriately
- Miners resume or reset gracefully
- No miners stuck forever

### Scenario 3: Source Destruction

**Setup:**
- Same as Scenario 1
- Ability to destroy asteroids

**Test Steps:**
1. Let miners establish mining loops
2. Destroy an asteroid that miners are currently mining or targeting
3. Observe miner behavior

**Expected Behavior:**
- Miners with invalid Source should detect missing entity
- MiningSession should be cleared
- State should reset to Idle
- Miners should find new asteroids
- MiningDiagnostics.InvalidSourceResets should increment

**Validation:**
- No exceptions when accessing destroyed entities
- Miners reassign to new sources
- No miners stuck with invalid references

### Scenario 4: Carrier Destruction

**Setup:**
- Same as Scenario 1
- Ability to destroy carriers

**Test Steps:**
1. Let miners establish mining loops
2. Destroy a carrier that miners are returning to
3. Observe miner behavior

**Expected Behavior:**
- Miners with invalid Carrier should detect missing entity
- Should attempt to find new carrier
- If no carrier available, should clear session and go Idle
- MiningDiagnostics.InvalidCarrierResets should increment

**Validation:**
- No exceptions when accessing destroyed carriers
- Miners reassign to new carriers or go Idle gracefully
- No miners stuck trying to deliver to destroyed carrier

### Scenario 5: Asteroid Movement

**Setup:**
- Same as Scenario 1
- Ability to move asteroids (via physics or hand)

**Test Steps:**
1. Let miners establish mining loops
2. Move an asteroid while miner is mining it
3. Observe miner behavior

**Expected Behavior:**
- Miner should always use current LocalTransform position (never cached)
- Miner should adjust path to new asteroid position
- If asteroid moves too far, miner should detect and reset if needed
- Mining should continue if still in range

**Validation:**
- Miners track moving asteroids correctly
- No position caching issues
- Smooth transitions when asteroids move

## Godgame Test Scenarios

### Scenario 1: Stable Villager Mining Loop

**Setup:**
- 5 villagers with MiningJobTag
- 10 resource nodes (trees, ore veins, etc.) with MineableSource
- 2 storehouses with ResourceSink
- Villagers assigned to Gatherer jobs

**Expected Behavior:**
1. Villagers should find resource nodes via VillagerJobAssignmentSystem
2. MiningSession should be created when job assigned
3. Villagers should gather resources
4. When carrying capacity reached, villagers should deliver to storehouse
5. After delivery, villagers should return to node or get new job
6. Loop should continue indefinitely

**Validation:**
- No villagers stuck in invalid states
- Resource totals increase over time
- Storehouse inventories fill correctly
- No exceptions or errors

### Scenario 2: Physics Disruption - Pickup and Throw

**Setup:**
- Same as Scenario 1
- Player can pick up villagers and resource nodes using divine hand

**Test Steps:**
1. Let villagers establish mining loops
2. Pick up a villager mid-job (while gathering or delivering)
3. Throw the villager to a different location
4. Observe villager behavior after landing

**Expected Behavior:**
- Villager should detect MovementSuppressed/BeingThrown and pause mining logic
- After BeingThrown removed, villager should:
  - Validate MiningSession source/storehouse still valid
  - If valid and path still reasonable, resume job
  - If too far or invalid, mark job as Interrupted and return to Idle
- Should integrate with GodgameGroundContactSystem for landing

**Validation:**
- MiningDiagnostics.PhysicsDisruptionResets increments
- Villagers resume or reset gracefully
- Ground contact system works correctly after throw

### Scenario 3: Resource Node Destruction

**Setup:**
- Same as Scenario 1
- Ability to destroy resource nodes

**Test Steps:**
1. Let villagers establish mining loops
2. Destroy a resource node that villagers are currently gathering from
3. Observe villager behavior

**Expected Behavior:**
- Villagers with invalid Source should detect missing entity
- MiningSession should be cleared
- Job should be marked as Interrupted
- Villagers should return to Idle and get new job assignments
- MiningDiagnostics.InvalidSourceResets should increment

**Validation:**
- No exceptions when accessing destroyed nodes
- Villagers reassign to new jobs
- No villagers stuck with invalid references

### Scenario 4: Storehouse Destruction

**Setup:**
- Same as Scenario 1
- Ability to destroy storehouses

**Test Steps:**
1. Let villagers establish mining loops
2. Destroy a storehouse that villagers are delivering to
3. Observe villager behavior

**Expected Behavior:**
- Villagers with invalid Carrier should detect missing entity
- Should attempt to find new storehouse from ticket
- If no valid storehouse, job should be marked as Interrupted
- MiningDiagnostics.InvalidCarrierResets should increment

**Validation:**
- No exceptions when accessing destroyed storehouses
- Villagers reassign to new storehouses or reset gracefully
- No villagers stuck trying to deliver to destroyed storehouse

### Scenario 5: Storehouse Full

**Setup:**
- Same as Scenario 1
- Storehouses with limited capacity

**Test Steps:**
1. Let villagers fill storehouse to capacity
2. Continue mining operations
3. Observe villager behavior when storehouse is full

**Expected Behavior:**
- Villagers should detect full storehouse via ResourceSink.CurrentAmount >= Capacity
- Should attempt to find alternative storehouse
- If no alternative, should wait or mark job as interrupted
- No over-delivery past capacity

**Validation:**
- ResourceSink capacity respected
- No negative amounts
- No over-delivery

## Common Validation Points

### Data Flow Clarity
- MiningSession.Source always points to valid entity or Entity.Null
- MiningSession.Carrier always points to valid entity or Entity.Null
- MiningSession.Accumulated never negative
- MineableSource.CurrentAmount clamped between 0 and MaxAmount

### Debugging and Tuning
- MiningDiagnostics provides useful metrics
- State transitions logged (in editor)
- Easy to identify stuck miners/villagers
- Performance metrics available

### Reusability
- Same MiningSession component used in both games
- Same robustness patterns applied
- Easy to extend to new mining scenarios

## Performance Considerations

- Systems should handle 100+ miners/villagers efficiently
- Spatial queries should be optimized (use SpatialGrid)
- No per-frame allocations in hot paths
- Burst compilation enabled for all jobs

## Edge Cases

1. **Multiple miners targeting same asteroid:**
   - Should handle gracefully (existing reservation system)
   - No resource duplication

2. **Miner/villager destroyed mid-operation:**
   - Session should be cleaned up
   - No orphaned references

3. **Simultaneous source and carrier destruction:**
   - Should handle both gracefully
   - No cascading failures

4. **Rewind/playback:**
   - Systems should respect RewindState
   - No updates during playback



















