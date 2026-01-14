using PureDOTS.Runtime.ComplexEntities;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.ComplexEntities
{
    /// <summary>
    /// Manages crew roster pool lifecycle for complex entities.
    /// Loads crew rosters when operational/narrative expansion activates,
    /// unloads when deactivated (with rollup to core axes).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ComplexEntityActivationSystem))]
    [UpdateAfter(typeof(ComplexEntityNarrativeDetailSystem))]
    [BurstCompile]
    public partial struct ComplexEntityCrewPoolSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Check feature flag
            var featureFlags = SystemAPI.GetSingleton<SimulationFeatureFlags>();
            if ((featureFlags.Flags & SimulationFeatureFlags.ComplexEntitiesEnabled) == 0)
                return;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var currentTick = (uint)SystemAPI.Time.ElapsedTime;

            // Load crew rosters for entities that need them
            foreach (var (entity, coreAxes, identity) in SystemAPI.Query<Entity>()
                .WithAll<ComplexEntityIdentity>()
                .WithNone<ComplexEntityCrewHandle>()
                .WithEntityAccess())
            {
                // Check if entity needs crew roster (operational or narrative active)
                bool needsCrew = (coreAxes.Flags & (ComplexEntityFlags.OperationalActive | ComplexEntityFlags.NarrativeActive)) != 0;

                if (needsCrew)
                {
                    // Create empty crew handle (in full implementation, would load from pool)
                    ecb.AddComponent(entity, new ComplexEntityCrewHandle
                    {
                        Roster = default,
                        LastUpdateTick = currentTick,
                        CrewCount = 0
                    });

                    coreAxes.Flags |= ComplexEntityFlags.CrewLoaded;
                    ecb.SetComponent(entity, coreAxes);
                }
            }

            // Unload crew rosters for entities that don't need them
            foreach (var (crewHandle, entity, coreAxes) in SystemAPI.Query<
                RefRO<ComplexEntityCrewHandle>>()
                .WithEntityAccess())
            {
                // Check if entity still needs crew roster
                bool needsCrew = (coreAxes.Flags & (ComplexEntityFlags.OperationalActive | ComplexEntityFlags.NarrativeActive)) != 0;

                if (!needsCrew)
                {
                    // Rollup crew data to core axes before removing
                    // In full implementation, would aggregate crew mass/capacity to core axes
                    var axes = coreAxes;
                    axes.Flags &= ~ComplexEntityFlags.CrewLoaded;
                    ecb.SetComponent(entity, axes);

                    // Remove crew handle (blob will be cleaned up automatically)
                    ecb.RemoveComponent<ComplexEntityCrewHandle>(entity);
                }
            }
        }
    }
}
