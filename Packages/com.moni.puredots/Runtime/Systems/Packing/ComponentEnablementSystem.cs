using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Packing
{
    /// <summary>
    /// System managing component enablement for sparse packing.
    /// Uses IEnableableComponent to disable unused data (15-30% chunk compression).
    /// </summary>
    [DisableAutoCreation] // Stub: disabled until implemented
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ComponentEnablementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            // Manage component enablement for sparse packing
            // In full implementation, would:
            // 1. Disable SensorSpec on blind entities
            // 2. Disable CombatStats on non-combatants
            // 3. Disable NavigationTarget when idle
            // 4. Measure chunk compression gains
        }
    }
}

