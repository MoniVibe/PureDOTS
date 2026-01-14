using PureDOTS.Runtime.ComplexEntities;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.ComplexEntities
{
    /// <summary>
    /// Updates operational state for complex entities that have operational expansion enabled.
    /// Only processes entities with enabled ComplexEntityOperationalState component.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ComplexEntityActivationSystem))]
    [BurstCompile]
    public partial struct ComplexEntityOperationalStateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Check feature flag
            var featureFlags = SystemAPI.GetSingleton<SimulationFeatureFlags>();
            if ((featureFlags.Flags & SimulationFeatureFlags.ComplexEntitiesEnabled) == 0)
                return;

            if ((featureFlags.Flags & SimulationFeatureFlags.ComplexEntityOperationalExpansionEnabled) == 0)
                return;

            var currentTick = (uint)SystemAPI.Time.ElapsedTime;

            // Process operational entities
            foreach (var (operationalState, coreAxes, entity) in SystemAPI.Query<
                RefRW<ComplexEntityOperationalState>,
                RefRW<ComplexEntityCoreAxes>>()
                .WithEntityAccess())
            {
                // Update operational state
                operationalState.ValueRW.LastUpdateTick = currentTick;

                // Sync operational state to core axes if needed
                // (e.g., update position/velocity from operational state)
                // This is a placeholder - actual sync logic depends on game-specific needs
            }
        }
    }
}
