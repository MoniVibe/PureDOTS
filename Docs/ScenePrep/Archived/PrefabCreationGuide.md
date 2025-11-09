# Space4X Prefab Creation Guide

Since Unity prefabs must be created in the Unity Editor, this guide provides step-by-step instructions for authoring the required Space4X prefabs.

## Required Prefabs

1. **Carrier.prefab** - Fleet mothership for mining operations
2. **MiningVessel.prefab** - Mining drones that extract resources
3. **AsteroidNode.prefab** - Resource nodes for mining

## Carrier Prefab

1. Create empty GameObject in `Assets/Space4X/Prefabs/`
2. Name it "Carrier"
3. Add component: `Space4XCarrierAuthoring`
4. Configure fields:
   - `CarrierId` = "CARRIER-1" (or unique ID)
   - `PatrolCenter` = (0, 0, 0) (will be overridden in scene)
   - `PatrolRadius` = 50
   - `Speed` = 5
   - `WaitTime` = 2
   - `ResourceStorages` = Add entries for Minerals, RareMetals, EnergyCrystals, OrganicMatter (capacity 10000 each)
5. Add visual representation:
   - Add child GameObject with `MeshRenderer` + `MeshFilter`
   - Use primitive mesh (Capsule recommended) or custom model
   - Assign material (can use URP default material)
   - Scale to appropriate size (3x scale recommended)
6. Save as prefab: Drag GameObject to `Assets/Space4X/Prefabs/Carrier.prefab`

## MiningVessel Prefab

1. Create empty GameObject in `Assets/Space4X/Prefabs/`
2. Name it "MiningVessel"
3. Add component: `Space4XMiningVesselAuthoring`
4. Configure fields:
   - `VesselId` = "MINER-1" (or unique ID)
   - `CarrierId` = "" (will be linked by Space4XMiningDemoAuthoring)
   - `MiningEfficiency` = 0.8
   - `Speed` = 10
   - `CargoCapacity` = 100
5. Add visual representation:
   - Add child GameObject with `MeshRenderer` + `MeshFilter`
   - Use primitive mesh (Cylinder recommended) or custom model
   - Assign material
   - Scale to appropriate size (1.2x scale recommended)
6. Save as prefab: Drag GameObject to `Assets/Space4X/Prefabs/MiningVessel.prefab`

## AsteroidNode Prefab

1. Create empty GameObject in `Assets/Space4X/Prefabs/`
2. Name it "AsteroidNode"
3. Add component: `Space4XMiningDemoAuthoring` (or create standalone authoring if preferred)
4. **Alternative**: Since `Space4XMiningDemoAuthoring` bakes asteroids directly, you may not need a prefab. Instead:
   - Use `Space4XMiningDemoAuthoring` in the scene to define asteroids programmatically
   - Or create a simple authoring component that creates an `Asteroid` component with `ResourceType` and amounts
5. If creating standalone prefab:
   - Add visual representation (Sphere mesh, brown material, 2.25x scale)
   - Note: Asteroid entities are typically created by `Space4XMiningDemoAuthoring.Baker`, not spawned from prefabs

## Visual Assets

For quick prototyping, use Unity primitives:

- **Carrier**: Capsule (scale 3, color: RGB 89, 102, 158)
- **MiningVessel**: Cylinder (scale 1.2, color: RGB 64, 133, 215)
- **Asteroid**: Sphere (scale 2.25, color: RGB 133, 110, 87)

## Prefab Validation

After creating prefabs:

1. Open each prefab in Prefab Mode
2. Verify authoring components are configured
3. Check that meshes/materials are assigned
4. Ensure prefabs can be instantiated in a test scene without errors
5. Verify bakers run successfully (check console for conversion errors)

## Integration with Hybrid Scene

Once prefabs are created:

1. Reference them in `HybridPresentationRegistry.asset` descriptors
2. Use them in `Space4XMiningDemoAuthoring` visual settings (if using visual prefab system)
3. Ensure prefab paths match registry entries exactly

**Note**: `Space4XMiningDemoAuthoring` creates entities directly via its Baker, so prefabs are primarily for visual representation via the presentation system, not for entity spawning.

