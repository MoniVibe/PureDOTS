using PureDOTS.Runtime.Components.Events;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Events
{
    /// <summary>
    /// Singleton event queue for cross-system event routing.
    /// Provides a centralized event bus for event-driven systems.
    /// </summary>
    public struct EventQueue : IComponentData
    {
        public uint Version;
        public uint LastProcessedTick;
    }

    /// <summary>
    /// Event queue system that manages cross-system event routing.
    /// Runs first in EventSystemGroup to process events before other systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EventSystemGroup), OrderFirst = true)]
    public partial struct EventQueueSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            
            // Create singleton if it doesn't exist
            if (!SystemAPI.HasSingleton<EventQueue>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new EventQueue
                {
                    Version = 0,
                    LastProcessedTick = 0
                });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            ref var eventQueue = ref SystemAPI.GetSingletonRW<EventQueue>().ValueRW;
            eventQueue.LastProcessedTick = tickState.Tick;
            eventQueue.Version++;
        }
    }
}

