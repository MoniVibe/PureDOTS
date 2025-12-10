#if PUREDOTS_DEMO
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace PureDOTS.Demo.Village
{
    /// <summary>
    /// System that adds render components to village demo entities (homes, workplaces, villagers).
    /// Requires VillageWorldTag to be present in the world to run.
    /// Note: Visual assignment is handled by PureDOTS.Demo.Rendering.AssignVisualsSystem.
    /// This system is kept for compatibility but does not perform visual setup.
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(VillageDemoBootstrapSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct VillageVisualSetupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Visual assignment is handled by PureDOTS.Demo.Rendering.AssignVisualsSystem
            // which processes entities with VisualProfile component
            state.RequireForUpdate<VillageWorldTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Visual assignment is handled by PureDOTS.Demo.Rendering.AssignVisualsSystem
            // No work needed here
        }
    }
}
#endif
