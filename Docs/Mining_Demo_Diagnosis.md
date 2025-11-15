# Mining Demo Diagnosis Report

## Critical Issues Found

### 1. Missing Authoring Components (CRITICAL)
GameObjects are missing authoring components, preventing them from being baked into DOTS entities:

- **Villager1, Villager2**: Missing `VillagerAuthoring` component
- **MiningVessel1, MiningVessel2**: Missing `MiningVesselAuthoring` component
- **ResourceNode_Wood1, Wood2, Wood3**: Missing `ResourceSourceAuthoring` component
- **OreNode1, OreNode2**: Missing `ResourceSourceAuthoring` component
- **Asteroid_Ore1, Ore2, Ore3**: Missing `ResourceSourceAuthoring` component
- **Storehouse**: Needs `StorehouseAuthoring` component
- **Carrier**: Needs `CarrierAuthoring` component

### 2. Missing Scene Configuration
- Scene needs `PureDotsConfigAuthoring` component to initialize DOTS singletons
- Need to verify sub-scene setup for DOTS baking

### 3. Villager Job Assignment
Villagers require job assignments via `VillagerJobAssignmentSystem` to move - they need worksites assigned.

## Fix Steps

### Step 1: Add Authoring Components

1. **Villagers**:
   - Select Villager1 and Villager2
   - Add Component → `VillagerAuthoring`
   - Set baseSpeed (default 3.0 is fine)
   - Leave other defaults

2. **Mining Vessels**:
   - Select MiningVessel1 and MiningVessel2
   - Add Component → `MiningVesselAuthoring`
   - Set baseSpeed (default 5.0 is fine)
   - Set capacity (default 50.0 is fine)

3. **Resource Nodes** (Wood):
   - Select ResourceNode_Wood1, Wood2, Wood3
   - Add Component → `ResourceSourceAuthoring`
   - Set resourceTypeId: `"wood"`
   - Set initialUnits: `200`
   - Set gatherRatePerWorker: `4`
   - Set maxSimultaneousWorkers: `3`
   - Set debugGatherRadius: `3`
   - Enable `respawns`, set respawnSeconds: `45`

4. **Ore Nodes**:
   - Select OreNode1, OreNode2
   - Add Component → `ResourceSourceAuthoring`
   - Set resourceTypeId: `"ore"`
   - Set initialUnits: `150`
   - Set gatherRatePerWorker: `3`
   - Set maxSimultaneousWorkers: `2`
   - Set debugGatherRadius: `3`
   - Enable `respawns`, set respawnSeconds: `60`

5. **Asteroids**:
   - Select Asteroid_Ore1, Ore2, Ore3
   - Add Component → `ResourceSourceAuthoring`
   - Set resourceTypeId: `"ore"`
   - Set initialUnits: `500`
   - Set gatherRatePerWorker: `8`
   - Set maxSimultaneousWorkers: `5`
   - Set debugGatherRadius: `5`
   - Enable `respawns`, set respawnSeconds: `120`

6. **Storehouse**:
   - Select Storehouse
   - Add Component → `StorehouseAuthoring`
   - Add capacity entries:
     - Resource: `"wood"`, Capacity: `1000`
     - Resource: `"ore"`, Capacity: `1000`

7. **Carrier**:
   - Select Carrier
   - Add Component → `CarrierAuthoring`

### Step 2: Verify Scene Configuration

1. Ensure scene has `PureDotsConfigAuthoring` GameObject:
   - If missing, create empty GameObject
   - Add Component → `PureDotsConfigAuthoring`
   - Assign `PureDotsRuntimeConfig.asset` reference
   - Assign `PureDotsResourceTypes.asset` reference

2. Ensure sub-scene is set up:
   - Check if "New Sub Scene" is enabled
   - Verify sub-scene is loaded for DOTS baking

### Step 3: Check Resource Type Catalog

Verify `PureDotsResourceTypes.asset` contains:
- `"wood"`
- `"ore"`
- `"stone"` (if used)

### Step 4: Test Movement

After adding components:
1. Enter Play mode
2. Check Unity Console for:
   - `[ResourceCatalogDebug]` messages - should show resources registered
   - `[VesselAISystem]` messages - should show vessels finding targets
   - `[EntityStateDebug]` messages - should show entities moving
   - `[MovementDiagnostic]` messages - comprehensive movement status

## Expected Behavior After Fix

**Vessels**:
- Should find nearest asteroid
- Move toward asteroid
- Start mining when close enough
- Return to carrier when full

**Villagers**:
- Need job assignments (worksites)
- Should find nearby resource nodes
- Move toward nodes
- Gather resources
- Deposit at storehouse

## Debug Systems Available

The following debug systems will help diagnose issues:

1. **ResourceCatalogDebugSystem** - Logs resource catalog and registry status
2. **EntityStateDebugSystem** - Logs vessel/villager states every ~1 second
3. **VesselMovementDebugSystem** - Detailed vessel movement logging
4. **MovementDiagnosticSystem** - Comprehensive movement diagnostics

All systems log to Unity Console when in Play mode.












