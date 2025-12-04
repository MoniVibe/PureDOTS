using PureDOTS.Runtime;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Bootstrap system that initializes the DemoScenarioState singleton.
    /// Runs in InitializationSystemGroup to ensure scenario state is available before other systems.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct DemoScenarioBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<DemoScenarioState>())
            {
                var entity = state.EntityManager.CreateEntity(typeof(DemoScenarioState));
                state.EntityManager.SetComponentData(entity, new DemoScenarioState
                {
                    Current = DemoScenario.AllSystemsShowcase
                });
            }
        }

        public void OnUpdate(ref SystemState state) { }
    }
}


