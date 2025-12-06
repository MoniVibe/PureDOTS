using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Manages ModifierInstance pool lifecycle.
    /// Pre-allocates pool capacity and integrates with ModifierExpirySystem to recycle instances.
    /// Runs in ColdPathSystemGroup after ModifierExpirySystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ModifierColdPathGroup))]
    [UpdateAfter(typeof(ModifierExpirySystem))]
    public partial struct ModifierPoolSystem : ISystem
    {
        private const int DefaultPoolCapacity = 10000;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ensure pool singleton exists
            if (!SystemAPI.HasSingleton<ModifierPool>())
            {
                var poolEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<ModifierPool>(poolEntity);
                var pool = new ModifierPool
                {
                    InitialCapacity = DefaultPoolCapacity,
                    CurrentSize = 0
                };
                state.EntityManager.SetComponentData(poolEntity, pool);
            }

            // Pool management is handled via NativeQueue in managed code if needed.
            // For now, the pool component tracks capacity.
            // Actual recycling happens in ModifierExpirySystem by reusing buffer slots.
        }
    }
}

