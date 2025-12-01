/// <summary>
/// PureDOTS Demo Assembly - Public API Documentation
/// 
/// OVERVIEW:
/// This assembly provides game-agnostic demo systems and components that host games
/// (Godgame, Space4x) can reference to get visual ECS validation and simple demo behaviors.
/// 
/// NAMESPACES:
/// 
/// - PureDOTS.Demo.Orbit: Orbit demo systems and components
/// - PureDOTS.Demo.Village: Village demo systems and components
/// - PureDOTS.Demo.Rendering: Shared rendering utilities (RenderMeshArraySingleton, DemoMeshIndices)
/// 
/// ORBIT DEMO (PureDOTS.Demo.Orbit):
/// 
/// Systems:
/// - OrbitCubeSystem: Spawns a huge 10-unit debug cube at (0,1,0) in bright magenta,
///   plus 4 colored orbiting cubes (red, green, blue, yellow) around the center.
///   Updates cube positions each frame to demonstrate ECS motion.
///   Runs in SimulationSystemGroup.
///   Logs: "[OrbitCubeSystem] World '{World.Name}': spawned X orbit cubes (including big debug cube)."
/// 
/// Components:
/// - OrbitCubeTag: Tag component marking orbit cube entities
/// - OrbitCube: Orbital motion parameters (radius, angular speed, angle, height)
/// 
/// VILLAGE DEMO (PureDOTS.Demo.Village):
/// 
/// Systems:
/// - VillageDemoBootstrapSystem: Creates 10 homes, 10 workplaces, and 10 villagers in a strip layout.
///   Homes positioned at (x, 0, 0), workplaces at (x, 0, 10) where x ranges from -9 to 9.
///   Villagers start at their home positions.
///   Runs in InitializationSystemGroup, after SharedRenderBootstrap.
///   Logs: "[VillageDemoBootstrapSystem] World '{World.Name}': spawned V Villagers, H Homes, W Works."
/// 
/// - VillageVisualSetupSystem: Adds render components (MaterialMeshInfo, RenderMeshArray) to village entities.
///   Requires VillageWorldTag to be present in the world.
///   Runs in InitializationSystemGroup, after VillageDemoBootstrapSystem.
/// 
/// - VillagerWalkLoopSystem: Moves villagers back and forth between home and work positions.
///   Phase 0: going to work, Phase 1: going home.
///   Speed: 2 units/second.
///   Runs in SimulationSystemGroup.
/// 
/// - VillageDebugSystem: Logs counts of villagers, homes, and works once per world.
///   Provides visibility into demo entity spawning.
///   Runs in SimulationSystemGroup.
///   Logs: "[VillageDebugSystem] World '{World.Name}': Villages: {villageCount}, Villagers: {villagerCount}, Homes: {homeCount}, Works: {workCount}"
/// 
/// Components:
/// - VillageWorldTag: World-level tag to enable VillageVisualSetupSystem
/// - VillageTag: Marks village aggregate entities (homes, workplaces)
/// - VillagerTag: Marks villager entities
/// - HomeLot: Position marker for village home structures (float3 Position)
/// - WorkLot: Position marker for village workplace structures (float3 Position)
/// - VillagerHome: Stores a villager's home position (float3 Position)
/// - VillagerWork: Stores a villager's work position (float3 Position)
/// - VillagerState: Tracks villager phase (byte Phase; 0=going to work, 1=going home)
/// 
/// RENDERING UTILITIES (PureDOTS.Demo.Rendering):
/// 
/// - DemoMeshIndices: Static class with mesh index constants:
///   - VillageGroundMeshIndex (0): Ground/terrain mesh
///   - VillageHomeMeshIndex (1): Home structures mesh
///   - VillageWorkMeshIndex (2): Workplace structures mesh
///   - VillageVillagerMeshIndex (3): Villagers and orbit cubes mesh
///   - DemoMaterialIndex (0): Demo material (typically Simple Lit shader)
/// 
/// - RenderMeshArraySingleton: Shared component that holds the RenderMeshArray for demo entities.
///   Host games must populate this singleton with meshes at the indices defined in DemoMeshIndices.
/// 
/// - SharedRenderBootstrap: Bootstrap system that initializes the RenderMeshArraySingleton.
///   Runs in InitializationSystemGroup, after TimeSystemGroup.
/// 
/// REQUIREMENTS:
/// 
/// - RenderMeshArraySingleton must be set up with meshes at DemoMeshIndices:
///   - Index 0: VillageGroundMeshIndex (ground/terrain)
///   - Index 1: VillageHomeMeshIndex (home structures)
///   - Index 2: VillageWorkMeshIndex (workplace structures)
///   - Index 3: VillageVillagerMeshIndex (villagers and orbit cubes)
/// - Material index 0: DemoMaterialIndex (typically Simple Lit shader)
/// 
/// USAGE:
/// 
/// To use Orbit demo:
/// - Add com.moni.puredots package.
/// - Ensure a bootstrap that creates a RenderMeshArraySingleton (SharedRenderBootstrap).
/// - Ensure at least one system updates in the default world (OrbitCubeSystem is marked with WorldSystemFilter for default worlds).
/// 
/// To use Village demo:
/// - Same as Orbit demo, plus any world filter attributes so VillageDemoBootstrapSystem,
///   VillageVisualSetupSystem, VillagerWalkLoopSystem run automatically in default worlds.
/// - Add VillageWorldTag to world entity if using VillageVisualSetupSystem.
/// 
/// All systems include debug logging with world names and entity counts for troubleshooting.
/// </summary>
namespace PureDOTS.Demo
{
    // This file exists only for documentation purposes.
    // The actual implementation is in the Orbit/, Village/, and Rendering/ subdirectories.
}

