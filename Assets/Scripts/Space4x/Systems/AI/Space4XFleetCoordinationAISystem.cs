using Unity.Entities;
using Space4X.Runtime;

namespace Space4X.Systems.AI
{
    /// <summary>
    /// Placeholder fleet coordination system to satisfy compilation.
    /// TODO: implement actual coordination logic.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct Space4XFleetCoordinationAISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VesselMovement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Coordination logic would go here.
        }
    }
}
