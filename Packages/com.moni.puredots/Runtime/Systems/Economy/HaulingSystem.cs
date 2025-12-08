using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Economy
{
    /// <summary>
    /// Placeholder hauling system for ownership logistics scheduling.
    /// Runs in the body economy layer so logistics executes afterwards.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BodyEconomySystemGroup))]
    public partial struct HaulingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
        }

        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            // Placeholder: actual hauling logic runs elsewhere.
        }
    }
}
