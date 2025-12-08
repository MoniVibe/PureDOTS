using PureDOTS.Runtime.Components.Events;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
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
        public uint ProcessedEvents;
        public uint DroppedEvents;
    }

    /// <summary>
    /// Event payload for cross-system routing.
    /// </summary>
    public struct EventPayload : IBufferElementData
    {
        public Entity Source;
        public Entity Target;
        public uint EventType;
        public uint Tick;
        public float Value;
    }

    /// <summary>
    /// Helper utilities for writing to the event queue.
    /// </summary>
    public static class EventQueueHelpers
    {
        public const int DefaultCapacity = 1024;

        /// <summary>
        /// Try to enqueue an event payload with a capacity guard.
        /// Returns true on success, false if dropped due to capacity.
        /// </summary>
        public static bool TryEnqueue(ref EventQueue queue, DynamicBuffer<EventPayload> buffer, in EventPayload payload, int maxCapacity = DefaultCapacity)
        {
            if (buffer.Length >= maxCapacity)
            {
                queue.DroppedEvents++;
                return false;
            }

            buffer.Add(payload);
            queue.ProcessedEvents++;
            return true;
        }
    }

    /// <summary>
    /// Event queue system that manages cross-system event routing.
    /// Runs first in EventSystemGroup to process events before other systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EventSystemGroup), OrderFirst = true)]
        public partial struct EventQueueSystem : ISystem
        {
        private const int DefaultCapacity = EventQueueHelpers.DefaultCapacity;

        // Telemetry counters exposed via EventQueue singleton
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            
                // Create singleton if it doesn't exist
                if (!SystemAPI.HasSingleton<EventQueue>())
                {
                    var entity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(entity, new EventQueue { Version = 0, LastProcessedTick = 0, ProcessedEvents = 0, DroppedEvents = 0 });
                    state.EntityManager.AddBuffer<EventPayload>(entity);
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

            var queueEntity = SystemAPI.GetSingletonEntity<EventQueue>();
            ref var eventQueue = ref SystemAPI.GetComponentRW<EventQueue>(queueEntity).ValueRW;
            var buffer = SystemAPI.GetBuffer<EventPayload>(queueEntity);

            // Reset at the start of a new tick so downstream systems see only current events.
            if (eventQueue.LastProcessedTick != tickState.Tick)
            {
                buffer.Clear();
                eventQueue.ProcessedEvents = 0;
                eventQueue.DroppedEvents = 0;
                eventQueue.LastProcessedTick = tickState.Tick;
                eventQueue.Version++;
            }

            // Enforce hard capacity to avoid unbounded growth even if producers misbehave.
            if (buffer.Length > DefaultCapacity)
            {
                var overflow = buffer.Length - DefaultCapacity;
                buffer.RemoveRange(DefaultCapacity, overflow);
                eventQueue.DroppedEvents += (uint)overflow;
            }

            // TODO (Agent D): expose counters via TelemetryStream
        }
    }
}
