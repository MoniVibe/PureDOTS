using PureDOTS.Runtime.Components.Events;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Events
{
    /// <summary>
    /// Sample consumer that drains the EventQueue and dispatches to handlers.
    /// Replace/extend handlers as real consumers are added.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EventSystemGroup), OrderLast = true)]
    public partial struct EventQueueConsumerSystem : ISystem
    {
        private uint _lastConsumedTick;
        private uint _consumedThisTick;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EventQueue>();
            state.RequireForUpdate<TickTimeState>();

            if (!SystemAPI.HasSingleton<EventQueueConsumerStats>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new EventQueueConsumerStats
                {
                    LastTick = 0,
                    ConsumedCount = 0
                });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var queueEntity = SystemAPI.GetSingletonEntity<EventQueue>();
            var buffer = SystemAPI.GetBuffer<EventPayload>(queueEntity);
            _consumedThisTick = 0;

            // Example: consume morale change events (no-op handler for now)
            for (int i = 0; i < buffer.Length; i++)
            {
                var ev = buffer[i];
                switch ((EventType)ev.EventType)
                {
                    case EventType.MoraleChange:
                        RouteMoraleEvent(ev);
                        _consumedThisTick++;
                        break;
                    default:
                        break;
                }
            }

            // Track consumed count per tick for telemetry (Agent D can pull this via a shared component or stream).
            _lastConsumedTick = tickState.Tick;

            if (SystemAPI.TryGetSingletonRW<EventQueueConsumerStats>(out var stats))
            {
                stats.ValueRW.LastTick = _lastConsumedTick;
                stats.ValueRW.ConsumedCount = _consumedThisTick;
            }
        }

        private static void RouteMoraleEvent(in EventPayload payload)
        {
            // Placeholder hook: route to telemetry/UI/AI triggers as needed.
            // For now, this is a no-op to validate end-to-end event flow.
        }
    }
}
