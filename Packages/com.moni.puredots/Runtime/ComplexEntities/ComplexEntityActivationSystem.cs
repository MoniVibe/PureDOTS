using PureDOTS.Runtime.ComplexEntities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.ComplexEntities
{
    /// <summary>
    /// Determines which complex entities should have operational expansion enabled
    /// based on active bubble, focus, combat, and docking triggers.
    /// Runs at reduced cadence to avoid overhead.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ComplexEntityOperationalStateSystem))]
    [BurstCompile]
    public partial struct ComplexEntityActivationSystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateCadence = 5; // Check every 5 ticks

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Check feature flag
            var featureFlags = SystemAPI.GetSingleton<SimulationFeatureFlags>();
            if ((featureFlags.Flags & SimulationFeatureFlags.ComplexEntitiesEnabled) == 0)
                return;

            if ((featureFlags.Flags & SimulationFeatureFlags.ComplexEntityOperationalExpansionEnabled) == 0)
                return;

            var currentTick = SystemAPI.TryGetSingleton<TickTimeState>(out var tickState)
                ? tickState.Tick
                : (SystemAPI.TryGetSingleton<TimeState>(out var timeState) ? timeState.Tick : 0u);
            if (currentTick - _lastUpdateTick < UpdateCadence)
                return;

            _lastUpdateTick = currentTick;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Enable operational state for entities with activation triggers
            foreach (var (entity, coreAxes) in SystemAPI.Query<Entity>()
                .WithAny<ActiveBubbleTag, FocusTargetTag, CombatReadyTag, DockingActiveTag>()
                .WithAll<ComplexEntityIdentity>()
                .WithEntityAccess())
            {
                var axes = coreAxes;
                if ((axes.Flags & ComplexEntityFlags.OperationalActive) == 0)
                {
                    // Ensure operational state component exists
                    if (!SystemAPI.HasComponent<ComplexEntityOperationalState>(entity))
                    {
                        ecb.AddComponent(entity, new ComplexEntityOperationalState
                        {
                            OperationalMode = 0,
                            TargetEntity = Entity.Null,
                            StateFlags = 0,
                            LastUpdateTick = (uint)currentTick
                        });
                    }
                    ecb.SetComponentEnabled<ComplexEntityOperationalState>(entity, true);
                    axes.Flags |= ComplexEntityFlags.OperationalActive;
                    ecb.SetComponent(entity, axes);
                }
            }

            // Disable operational state for entities without activation triggers
            foreach (var (entity, coreAxes) in SystemAPI.Query<Entity>()
                .WithNone<ActiveBubbleTag, FocusTargetTag, CombatReadyTag, DockingActiveTag>()
                .WithAll<ComplexEntityIdentity>()
                .WithEntityAccess())
            {
                var axes = coreAxes;
                if ((axes.Flags & ComplexEntityFlags.OperationalActive) != 0)
                {
                    if (SystemAPI.HasComponent<ComplexEntityOperationalState>(entity))
                    {
                        ecb.SetComponentEnabled<ComplexEntityOperationalState>(entity, false);
                    }
                    axes.Flags &= ~ComplexEntityFlags.OperationalActive;
                    ecb.SetComponent(entity, axes);
                }
            }
        }
    }
}
