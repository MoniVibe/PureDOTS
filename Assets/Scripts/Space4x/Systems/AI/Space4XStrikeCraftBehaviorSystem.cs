using Unity.Entities;
using Space4X.Runtime;

namespace Space4X.Systems.AI
{
    /// <summary>
    /// Placeholder strike craft behavior system to satisfy compilation.
    /// TODO: implement actual behavior.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct Space4XStrikeCraftBehaviorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Require movement component if needed in future.
            state.RequireForUpdate<VesselMovement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Behavior logic would go here.
        }
    }
}
