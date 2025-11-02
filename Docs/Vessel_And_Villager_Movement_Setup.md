# Vessel and Villager Movement Setup

## Overview
This document describes the movement systems for villagers and vessels (mining vessels) that were added to enable the mining loop demo.

## Systems Created

### Vessel Systems (Space4X)

1. **VesselMovementSystem** (`Assets/Scripts/Space4x/Systems/VesselMovementSystem.cs`)
   - Moves vessels toward their target positions
   - Similar to VillagerMovementSystem but designed for ships
   - Updates vessel position and rotation based on VesselAIState

2. **VesselAISystem** (`Assets/Scripts/Space4x/Systems/VesselAISystem.cs`)
   - Assigns vessels to asteroids for mining
   - Manages vessel goals (Mining, Returning, Idle)
   - Auto-assigns idle vessels to nearest asteroids

3. **VesselTargetingSystem** (`Assets/Scripts/Space4x/Systems/VesselTargetingSystem.cs`)
   - Resolves target entities to world positions
   - Similar to VillagerTargetingSystem but for vessels
   - Updates TargetPosition from registry or transform lookup

### Components Created

1. **VesselMovement** (`Assets/Scripts/Space4x/Runtime/VesselMovement.cs`)
   - Movement data (velocity, speed, rotation)
   - Similar to VillagerMovement

2. **VesselAIState** (`Assets/Scripts/Space4x/Runtime/VesselMovement.cs`)
   - AI state for vessels (goals, states, targets)
   - Goals: None, Mining, Returning, Idle
   - States: Idle, MovingToTarget, Mining, Returning

3. **MiningVesselAuthoring** (`Assets/Scripts/Space4x/Authoring/VesselAuthoring.cs`)
   - Unity authoring component for mining vessels
   - Adds MinerVessel, VesselAIState, VesselMovement components
   - Configurable: baseSpeed, capacity, resourceTypeIndex

## Villager Systems (Already Existed)

Villagers use existing PureDOTS systems:
- **VillagerMovementSystem** - Moves villagers to targets
- **VillagerAISystem** - Evaluates villager goals based on needs
- **VillagerTargetingSystem** - Resolves target positions
- **VillagerJobAssignmentSystem** - Assigns villagers to resource nodes
- **VillagerJobRequestSystem** - Creates job requests for villagers

## Setup Instructions

### For Mining Vessels

1. Add `MiningVesselAuthoring` component to mining vessel GameObjects
   - Set `baseSpeed` (e.g., 5.0)
   - Set `capacity` (e.g., 50.0)
   - Set `resourceTypeIndex` (0 for ore)

2. Ensure vessels have:
   - Transform component (automatic)
   - LocalTransform (added by Baker)
   - MinerVessel component (added by Baker)
   - VesselAIState component (added by Baker)
   - VesselMovement component (added by Baker)

### For Villagers

Villagers should already have all required components from `VillagerAuthoring`:
- VillagerAIState
- VillagerMovement
- VillagerJob
- VillagerJobTicket
- LocalTransform

**Important**: Villagers need job requests to be created. The `VillagerJobRequestSystem` automatically creates requests for villagers that are available and have matching resource nodes in the scene.

## How It Works

### Vessel Flow
1. Vessel starts idle with empty load
2. VesselAISystem finds nearest asteroid and assigns it as target
3. VesselTargetingSystem resolves asteroid position
4. VesselMovementSystem moves vessel toward asteroid
5. When vessel reaches asteroid (arrival distance), it starts mining (to be implemented)
6. When vessel is full (load >= 95% capacity), it returns to carrier/origin
7. Vessel unloads at carrier (to be implemented), then repeats

### Villager Flow
1. Villager starts idle
2. VillagerJobRequestSystem creates job requests for available villagers
3. VillagerJobAssignmentSystem assigns villagers to resource nodes (wood, ore)
4. VillagerAISystem sets goal to Work
5. VillagerTargetingSystem resolves resource node position
6. VillagerMovementSystem moves villager to resource node
7. VillagerJobExecutionSystem handles gathering and delivery (existing)

## Testing

To verify movement is working:

1. **Vessels**: Check that vessels move toward asteroids when spawned
   - Vessels should automatically target nearest asteroid
   - Vessels should rotate and move toward target

2. **Villagers**: Check that villagers move toward resource nodes
   - Villagers need resource nodes with ResourceSourceAuthoring
   - Villagers need VillagerJobRequestSystem to create job requests
   - Check console for job assignment logs

## Known Issues / TODOs

1. **Vessel Mining**: Mining logic not yet implemented - vessels reach asteroids but don't extract resources
2. **Vessel Unloading**: Unloading at carrier not yet implemented
3. **Carrier Integration**: Vessels return to origin (0,0,0) instead of carrier entity
4. **Resource Type Filtering**: Vessels don't filter by resource type when selecting asteroids
5. **Job Requests**: Need to verify VillagerJobRequestSystem is creating requests automatically

## Next Steps

1. Implement vessel mining at asteroids
2. Implement vessel unloading at carrier
3. Link vessels to carrier entity
4. Add resource type filtering for vessels
5. Test villager job request/assignment flow

