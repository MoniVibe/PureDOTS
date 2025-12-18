#if PUREDOTS_SCENARIO
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Demo.Orbit
{
    /// <summary>
    /// Legacy orbit demo system.
    /// Disabled for Space4X because Space4X has its own
    /// debug spawner + orbit systems that use the correct
    /// RenderMeshArray / Entities Graphics APIs.
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation] // make sure it never runs
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct OrbitCubeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Intentionally empty.
            // If you ever want to revive this demo, implement spawning/orbit here
            // using the current Entities Graphics APIs.
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Intentionally empty – no work done.
        }
    }
}
#endif
