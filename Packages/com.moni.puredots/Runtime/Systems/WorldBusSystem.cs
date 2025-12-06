using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.WorldBus;

namespace PureDOTS.Systems
{
    /// <summary>
    /// System that processes cross-world messages.
    /// Routes events between ECS worlds deterministically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WorldBusSystem : ISystem
    {
        private WorldBus _worldBus;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _worldBus = new WorldBus(1024, Unity.Collections.Allocator.Persistent);
            state.RequireForUpdate<WorldBusState>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _worldBus.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var busState = SystemAPI.GetSingleton<WorldBusState>();
            var timeState = SystemAPI.GetSingleton<TimeState>();

            // Route incoming messages
            if (SystemAPI.TryGetSingletonEntity<WorldBusState>(out var busEntity))
            {
                var messageBuffer = state.EntityManager.GetBuffer<WorldMessage>(busEntity);
                WorldBusRouter.RouteMessages(ref _worldBus, busState.WorldId, ref messageBuffer);
            }

            // Process messages for this world
            // In a real implementation, this would:
            // 1. Read messages from buffer
            // 2. Apply effects based on message type
            // 3. Example: Godgame divine intervention → Space4X orbital climate change
        }
    }
}

