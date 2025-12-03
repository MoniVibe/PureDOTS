# Space4X Prefab Creation Checklist

This document provides step-by-step instructions for creating all prefabs needed for the Space4X demo. Note that Space4X entities are **purely data** - they have no visual representation and are managed entirely by ECS systems.

**Related Documentation:**
- [Godgame Prefab Checklist](./Godgame_PrefabChecklist.md) - For comparison with Godgame's visual entity prefabs
- [Space4X Prefab Build Summary](./Space4X_PrefabBuild_Summary.md) - Implementation summary and next steps

## Overview

Each prefab should be created in Unity Editor by:
1. Creating an empty GameObject
2. Adding the required authoring components
3. Configuring component properties
4. Saving as prefab in the appropriate directory

**Important:** Carriers, mining vessels, and asteroids should have `PlaceholderVisualAuthoring` components for visual representation. Registry entities (colonies, fleets, routes, anomalies) remain data-only.

## Prefab Directory Structure

```
Space4x/Assets/Prefabs/
├── Systems/          # Camera, Input controllers
├── Vessels/          # Mining vessels and other ships
├── Carriers/         # Carrier ships
├── Asteroids/        # Asteroid entities (if standalone)
├── Colonies/         # Colony entities (if standalone)
└── Fleets/           # Fleet entities (if standalone)
```

---

## 1. System Prefabs

### 1.1 Space4XCamera.prefab
**Location:** `Space4x/Assets/Prefabs/Systems/`

**Components:**
- `Camera` (Unity default Camera component)
- `Space4XCameraAuthoring`
- `Space4XCameraInputAuthoring`

**Space4XCameraAuthoring Settings:**
- `profile`: Assign a `Space4XCameraProfile` ScriptableObject (create if needed)
  - `PanSpeed`: 10.0
  - `ZoomSpeed`: 5.0
  - `VerticalMoveSpeed`: 10.0
  - `ZoomMinDistance`: 10.0
  - `ZoomMaxDistance`: 500.0
  - `RotationSpeed`: 90.0
  - `PitchMin`: -30.0
  - `PitchMax`: 85.0
  - `Smoothing`: 0.1
  - `PanBoundsMin`: (-100, 0, -100)
  - `PanBoundsMax`: (100, 100, 100)
  - `UsePanBounds`: false

**Space4XCameraInputAuthoring Settings:**
- `inputActions`: Assign `InputSystem_Actions.inputactions` asset
- `enablePan`: true
- `enableZoom`: true
- `enableVerticalMove`: true
- `enableRotation`: true
- `requireRightMouseForRotation`: true

**Transform Settings:**
- Position: (0, 50, -100) - typical orbital camera position
- Rotation: Look at origin

**Note:** This prefab should be instantiated in the scene at runtime, not baked into SubScene.

---

### 1.2 Space4XCameraProfile.asset (ScriptableObject)
**Location:** `Space4x/Assets/Data/`

**Creation:**
1. Right-click in `Space4x/Assets/Data/`
2. Create → Space4X → Camera Profile (or create script if needed)
3. Configure camera settings as listed above

---

## 2. Vessel Prefabs

### 2.1 MiningVessel.prefab
**Location:** `Space4x/Assets/Prefabs/Vessels/`

**Components:**
- `Space4XMiningVesselAuthoring`
- `PlaceholderVisualAuthoring` - Visual representation
- `MeshFilter` - Assign a primitive mesh (Cylinder recommended)
- `MeshRenderer` - Assign a URP/Lit material
- `LocalTransform` (added automatically by baker)

**Space4XMiningVesselAuthoring Settings:**
- `vesselId`: "MINER-01" (unique identifier)
- `carrierId`: "" (will be linked at runtime or via MiningDemoAuthoring)
- `miningEfficiency`: 0.8 (0-1 range, affects mining speed)
- `speed`: 10.0 (movement speed)
- `cargoCapacity`: 100.0 (maximum cargo before returning to carrier)

**PlaceholderVisualAuthoring Settings:**
- `kind`: `PlaceholderVisualKind.Crate` (or appropriate kind)
- `baseScale`: 1.2 (matches MiningVisualSettings default)
- `baseColor`: (0.25, 0.52, 0.84, 1.0) - Blue vessel color
- `enforceTransformScale`: true

**MeshRenderer Settings:**
- Material: URP/Lit material with blue tint
- Enable GPU Instancing if available

**Transform Settings:**
- Position: (0, 0, 0) - will be set by spawn systems
- Scale: (1.2, 1.2, 1.2) - matches baseScale

**Runtime Components Added by Baker:**
- `MiningVessel` - vessel identity and stats
- `MiningJob` - current mining job state
- `LocalTransform` - position/rotation

---

### 2.2 CombatVessel.prefab (Future Expansion)
**Location:** `Space4x/Assets/Prefabs/Vessels/`

**Components:**
- Custom authoring component (to be created)
- Configure combat stats, weapons, shields

**Note:** This is a placeholder for future combat vessel implementation.

---

## 3. Carrier Prefabs

### 3.1 Carrier.prefab
**Location:** `Space4x/Assets/Prefabs/Carriers/`

**Components:**
- `Space4XCarrierAuthoring`
- `PlaceholderVisualAuthoring` - Visual representation
- `MeshFilter` - Assign a primitive mesh (Capsule recommended)
- `MeshRenderer` - Assign a URP/Lit material
- `LocalTransform` (added automatically by baker)

**Space4XCarrierAuthoring Settings:**
- `carrierId`: "CARRIER-01" (unique identifier)
- `patrolCenter`: (0, 0, 0) - center of patrol area
- `patrolRadius`: 50.0 - radius of patrol area
- `waitTime`: 2.0 - seconds to wait at each waypoint
- `speed`: 5.0 - movement speed
- `resourceStorages`: Add entries for:
  - `ResourceType.Minerals`: capacity 10000
  - `ResourceType.RareMetals`: capacity 10000
  - `ResourceType.EnergyCrystals`: capacity 10000
  - `ResourceType.OrganicMatter`: capacity 10000

**PlaceholderVisualAuthoring Settings:**
- `kind`: `PlaceholderVisualKind.Crate` (or appropriate kind)
- `baseScale`: 3.0 (matches MiningVisualSettings default, carriers are larger)
- `baseColor`: (0.35, 0.4, 0.62, 1.0) - Blue-gray carrier color
- `enforceTransformScale`: true

**MeshRenderer Settings:**
- Material: URP/Lit material with blue-gray tint
- Enable GPU Instancing if available

**Transform Settings:**
- Position: (0, 0, 0) - will be set by spawn systems
- Scale: (3.0, 3.0, 3.0) - matches baseScale

**Runtime Components Added by Baker:**
- `Carrier` - carrier identity and patrol config
- `PatrolBehavior` - patrol waypoint management
- `MovementCommand` - movement target
- `ResourceStorage` buffer - resource storage per type

---

### 3.2 Carrier_Large.prefab (Optional Variant)
**Location:** `Space4x/Assets/Prefabs/Carriers/`

**Components:** Same as Carrier.prefab

**Settings:** Copy from Carrier.prefab, then:
- `patrolRadius`: 100.0 (larger patrol area)
- `speed`: 3.0 (slower movement)
- `resourceStorages`: Increase capacities to 50000 each

---

## 4. Asteroid Prefabs

### 4.1 Asteroid_Minerals.prefab
**Location:** `Space4x/Assets/Prefabs/Asteroids/`

**Components:**
- `Space4XAsteroidAuthoring` (if standalone authoring exists) OR
- Use `Space4XMiningDemoAuthoring` to bulk-create asteroids
- `PlaceholderVisualAuthoring` - Visual representation
- `MeshFilter` - Assign a primitive mesh (Sphere recommended)
- `MeshRenderer` - Assign a URP/Lit material

**Note:** Currently, asteroids are typically created via `Space4XMiningDemoAuthoring` which bakes multiple asteroids at once. If creating standalone prefabs:

**Space4XAsteroidAuthoring Settings (if exists):**
- `asteroidId`: "ASTEROID-MINERALS-01"
- `resourceType`: `ResourceType.Minerals`
- `resourceAmount`: 500.0
- `maxResourceAmount`: 500.0
- `miningRate`: 10.0 (resources per second)

**PlaceholderVisualAuthoring Settings:**
- `kind`: `PlaceholderVisualKind.Crate` (or appropriate kind)
- `baseScale`: 2.25 (matches MiningVisualSettings default)
- `baseColor`: (0.52, 0.43, 0.34, 1.0) - Brown asteroid color
- `enforceTransformScale`: true

**MeshRenderer Settings:**
- Material: URP/Lit material with brown/rock texture
- Enable GPU Instancing if available

**Transform Settings:**
- Position: (0, 0, 0) - will be set by spawn systems
- Scale: (2.25, 2.25, 2.25) - matches baseScale

**Runtime Components Added by Baker:**
- `Asteroid` - asteroid identity and resource data
- `LocalTransform` - position

---

### 4.2 Asteroid_RareMetals.prefab
**Location:** `Space4x/Assets/Prefabs/Asteroids/`

**Components:** Same as Asteroid_Minerals.prefab

**Settings:** Copy from Asteroid_Minerals.prefab, then:
- `resourceType`: `ResourceType.RareMetals`
- `resourceAmount`: 300.0 (rarer resource, less quantity)
- `maxResourceAmount`: 300.0
- `miningRate`: 5.0 (slower mining rate)
- `PlaceholderVisualAuthoring.baseColor`: (0.7, 0.6, 0.5, 1.0) - Slightly lighter/shiny for rare metals

---

### 4.3 Asteroid_EnergyCrystals.prefab
**Location:** `Space4x/Assets/Prefabs/Asteroids/`

**Components:** Same as Asteroid_Minerals.prefab

**Settings:** Copy from Asteroid_Minerals.prefab, then:
- `resourceType`: `ResourceType.EnergyCrystals`
- `resourceAmount`: 200.0
- `maxResourceAmount`: 200.0
- `miningRate`: 3.0
- `PlaceholderVisualAuthoring.baseColor`: (0.4, 0.6, 1.0, 1.0) - Bright blue for energy crystals
- Consider enabling emission on material for glow effect

---

### 4.4 Asteroid_OrganicMatter.prefab
**Location:** `Space4x/Assets/Prefabs/Asteroids/`

**Components:** Same as Asteroid_Minerals.prefab

**Settings:** Copy from Asteroid_Minerals.prefab, then:
- `resourceType`: `ResourceType.OrganicMatter`
- `resourceAmount`: 400.0
- `maxResourceAmount`: 400.0
- `miningRate`: 8.0
- `PlaceholderVisualAuthoring.baseColor`: (0.3, 0.5, 0.2, 1.0) - Green for organic matter

---

## 5. Registry Entity Prefabs (Optional Standalone)

**Note:** These entities are typically created via `Space4XSampleRegistryAuthoring` which bakes multiple entities at once. Standalone prefabs are optional but useful for manual placement.

### 5.1 Colony.prefab
**Location:** `Space4x/Assets/Prefabs/Colonies/`

**Components:**
- `Space4XColonyAuthoring` (if exists) OR
- Use `Space4XSampleRegistryAuthoring` to bulk-create colonies

**If Standalone Authoring Exists:**
- `colonyId`: "COLONY-01"
- `population`: 250000.0
- `storedResources`: 1200.0
- `sectorId`: 1
- `status`: `Space4XColonyStatus.Growing`

**Runtime Components:**
- `Space4XColony` - colony identity and stats
- `LocalTransform` - position
- `SpatialIndexedTag` - spatial indexing

---

### 5.2 Fleet.prefab
**Location:** `Space4x/Assets/Prefabs/Fleets/`

**Components:**
- `Space4XFleetAuthoring` (if exists) OR
- Use `Space4XSampleRegistryAuthoring` to bulk-create fleets

**If Standalone Authoring Exists:**
- `fleetId`: "FLEET-ALPHA"
- `shipCount`: 5
- `posture`: `Space4XFleetPosture.Patrol`
- `taskForce`: 101

**Runtime Components:**
- `Space4XFleet` - fleet identity and stats
- `LocalTransform` - position
- `SpatialIndexedTag` - spatial indexing

---

### 5.3 LogisticsRoute.prefab (Optional)
**Location:** `Space4x/Assets/Prefabs/Fleets/`

**Components:**
- Use `Space4XSampleRegistryAuthoring` to create routes

**Settings (via SampleRegistryAuthoring):**
- `routeId`: "ROUTE-SOL-ALPHA"
- `originColonyId`: "SOL-1"
- `destinationColonyId`: "ALPHA-2"
- `dailyThroughput`: 180.0
- `risk`: 0.15
- `priority`: 1
- `status`: `Space4XLogisticsRouteStatus.Operational`

---

### 5.4 Anomaly.prefab (Optional)
**Location:** `Space4x/Assets/Prefabs/Fleets/`

**Components:**
- Use `Space4XSampleRegistryAuthoring` to create anomalies

**Settings (via SampleRegistryAuthoring):**
- `anomalyId`: "ANOM-PRIME"
- `classification`: "Gravitic Rift"
- `severity`: `Space4XAnomalySeverity.Severe`
- `state`: `Space4XAnomalyState.Active`
- `instability`: 0.78
- `sectorId`: 4

---

## 6. Bulk Authoring Prefabs

### 6.1 MiningDemoSetup.prefab
**Location:** `Space4x/Assets/Prefabs/Systems/`

**Components:**
- `PureDotsConfigAuthoring` - references `PureDotsRuntimeConfig.asset`
- `SpatialPartitionAuthoring` - references `DefaultSpatialPartitionProfile.asset`
- `Space4XMiningDemoAuthoring` - bulk creates carriers, vessels, asteroids

**Space4XMiningDemoAuthoring Settings:**
- `carriers`: Array of `CarrierDefinition`
  - At least one carrier with unique `CarrierId`
  - Configure patrol area, speed, wait time
  - Set resource storage capacities
- `miningVessels`: Array of `MiningVesselDefinition`
  - Multiple vessels with unique `VesselId`
  - Link to carrier via `CarrierId` (must match carrier's `CarrierId`)
  - Configure speed, efficiency, cargo capacity
- `asteroids`: Array of `AsteroidDefinition`
  - Multiple asteroids with unique `AsteroidId`
  - Set resource type, amount, mining rate
  - Position asteroids within mining range
- `visuals`: `MiningVisualSettings` (optional, for debug visualization)
  - Configure primitive types, scales, colors
  - Only used if visual request systems are active

**Example Configuration:**
```
Carriers:
  - CarrierId: "CARRIER-1"
    Speed: 5.0
    PatrolCenter: (0, 0, 0)
    PatrolRadius: 50.0
    WaitTime: 2.0
    Position: (0, 0, 0)

MiningVessels:
  - VesselId: "MINER-1"
    Speed: 10.0
    MiningEfficiency: 0.8
    CargoCapacity: 100.0
    Position: (5, 0, 0)
    CarrierId: "CARRIER-1"
  - VesselId: "MINER-2"
    Speed: 10.0
    MiningEfficiency: 0.8
    CargoCapacity: 100.0
    Position: (-5, 0, 0)
    CarrierId: "CARRIER-1"

Asteroids:
  - AsteroidId: "ASTEROID-1"
    ResourceType: Minerals
    ResourceAmount: 500.0
    MaxResourceAmount: 500.0
    MiningRate: 10.0
    Position: (20, 0, 0)
  - AsteroidId: "ASTEROID-2"
    ResourceType: RareMetals
    ResourceAmount: 300.0
    MaxResourceAmount: 300.0
    MiningRate: 5.0
    Position: (-20, 0, 0)
```

---

### 6.2 RegistrySetup.prefab
**Location:** `Space4x/Assets/Prefabs/Systems/`

**Components:**
- `PureDotsConfigAuthoring` - references `PureDotsRuntimeConfig.asset`
- `SpatialPartitionAuthoring` - references `DefaultSpatialPartitionProfile.asset`
- `Space4XSampleRegistryAuthoring` - bulk creates colonies, fleets, routes, anomalies

**Space4XSampleRegistryAuthoring Settings:**
- `colonies`: Array of `ColonyDefinition`
  - Unique `ColonyId` for each colony
  - Set population, stored resources, sector, status
- `fleets`: Array of `FleetDefinition`
  - Unique `FleetId` for each fleet
  - Set ship count, posture, task force
- `logisticsRoutes`: Array of `LogisticsRouteDefinition`
  - Unique `RouteId` for each route
  - Link origin/destination via colony IDs
  - Set throughput, risk, priority, status
- `anomalies`: Array of `AnomalyDefinition`
  - Unique `AnomalyId` for each anomaly
  - Set classification, severity, state, instability, sector

**Example Configuration:**
```
Colonies:
  - ColonyId: "SOL-1"
    Population: 250000.0
    StoredResources: 1200.0
    SectorId: 1
    Status: Growing
    Position: (0, 0, 0)

Fleets:
  - FleetId: "FLEET-ALPHA"
    ShipCount: 5
    Posture: Patrol
    TaskForce: 101
    Position: (35, 0, -12)

LogisticsRoutes:
  - RouteId: "ROUTE-SOL-ALPHA"
    OriginColonyId: "SOL-1"
    DestinationColonyId: "ALPHA-2"
    DailyThroughput: 180.0
    Risk: 0.15
    Priority: 1
    Status: Operational
    Position: (16, 0, 6)

Anomalies:
  - AnomalyId: "ANOM-PRIME"
    Classification: "Gravitic Rift"
    Severity: Severe
    State: Active
    Instability: 0.78
    SectorId: 4
    Position: (-18, 0, 22)
```

---

## 7. Support Assets

### 7.1 PureDotsRuntimeConfig.asset
**Location:** `Space4x/Assets/Space4X/Config/` (already exists)

**Settings:**
- Verify resource types are configured:
  - Minerals
  - RareMetals
  - EnergyCrystals
  - OrganicMatter
- Configure time/history/pooling settings as needed

---

### 7.2 DefaultSpatialPartitionProfile.asset
**Location:** `Space4x/Assets/Space4X/Config/` (already exists)

**Settings:**
- Configure spatial grid dimensions and cell size
- Ensure coverage matches demo scene bounds

---

### 7.3 Space4XCameraProfile.asset
**Location:** `Space4x/Assets/Data/`

**Creation:**
1. Create ScriptableObject script if it doesn't exist
2. Configure camera movement parameters (see Camera prefab section)

---

### 7.4 InputSystem_Actions.inputactions
**Location:** `Space4x/Assets/` (already exists)

**Settings:**
- Verify camera input actions are defined:
  - Pan (WASD or Arrow Keys)
  - Zoom (Mouse Scroll)
  - Vertical Move (Q/E or Page Up/Down)
  - Rotate (Right Mouse Drag)

---

## 8. Validation Steps

After creating all prefabs:

1. **Check Component References:**
   - Verify all prefab references are assigned (carrier IDs, colony IDs)
   - Ensure ScriptableObject assets are linked

2. **Test Baking:**
   - Create a test SubScene
   - Add MiningDemoSetup or RegistrySetup prefab
   - Convert to SubScene
   - Verify no baking errors
   - Check entity counts match definitions

3. **Runtime Test:**
   - Create a test scene with PureDotsWorldBootstrap
   - Instantiate prefabs
   - Verify systems recognize entities
   - Check mining loop (vessels → asteroids → carriers)
   - Verify registry systems update correctly

4. **Spatial Indexing:**
   - Verify entities have `SpatialIndexedTag`
   - Check spatial queries work for vessel/asteroid detection
   - Validate carrier patrol area calculations

5. **Documentation:**
   - Update this checklist with any custom settings
   - Note any prefab variants created
   - Document resource type mappings

---

## Notes

- **Visual Representation:** Carriers, mining vessels, and asteroids should have `PlaceholderVisualAuthoring` components with appropriate meshes and materials. Registry entities (colonies, fleets, routes, anomalies) remain data-only.
- **Mesh Selection:** Use Unity primitives:
  - Carriers: Capsule mesh
  - Mining Vessels: Cylinder mesh
  - Asteroids: Sphere mesh
- **Material Setup:** Assign URP/Lit materials with appropriate colors matching the `MiningVisualSettings` defaults, or customize per prefab variant.
- **Transform Usage:** All prefabs should use `TransformUsageFlags.Dynamic` for runtime entities
- **ID Consistency:** Carrier IDs, Colony IDs, Fleet IDs must be consistent across related prefabs
- **Bulk vs Standalone:** Use `MiningDemoAuthoring`/`SampleRegistryAuthoring` for bulk creation, or create standalone prefabs for manual placement
- **Spatial Indexing:** All entities should have `SpatialIndexedTag` for efficient spatial queries
- **Resource Types:** Match `ResourceType` enum values exactly (Minerals, RareMetals, EnergyCrystals, OrganicMatter)

---

## Quick Reference: Component Namespaces

- `Space4X.Authoring.*` - Authoring components
- `Space4X.Registry.*` - Registry and demo components
- `Space4X.Runtime.*` - Runtime vessel components
- `PureDOTS.Authoring.*` - Shared PureDOTS authoring
- `PureDOTS.Runtime.*` - Shared PureDOTS runtime

---

## Resource Type Reference

```csharp
public enum ResourceType : byte
{
    Minerals = 0,        // Common construction material
    RareMetals = 1,      // Advanced components
    EnergyCrystals = 2,  // Power generation
    OrganicMatter = 3    // Life support, research
}
```

---

**Last Updated:** [Current Date]
**Status:** Initial Checklist Created

