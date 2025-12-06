using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Focus;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Focus
{
    /// <summary>
    /// Event-driven system that processes focus drain events.
    /// Only processes entities where FocusState has changed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FocusUpdateSystem))]
    public partial struct FocusDrainSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            // Event-driven: only process entities with changed FocusState
            foreach (var (focus, entity) in SystemAPI.Query<RefRO<FocusState>>()
                .WithChangeFilter<FocusState>()
                .WithEntityAccess())
            {
                HandleFocusChange(focus.ValueRO, entity);
            }
        }

        [BurstCompile]
        private void HandleFocusChange(in FocusState focus, Entity entity)
        {
            // Process focus drain events
            // This system reacts to FocusState changes (drain events)
            // Additional processing can be added here for focus-based penalties
        }
    }
}

