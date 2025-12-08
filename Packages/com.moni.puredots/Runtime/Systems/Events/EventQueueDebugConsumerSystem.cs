using PureDOTS.Runtime.Components.Events;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Systems.Events
{
    /// <summary>
    /// Lightweight debug consumer that logs a small sample of events each tick.
    /// Purpose: validate end-to-end event flow during development. Remove/disable in production.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EventSystemGroup), OrderLast = true)]
    [DisableAutoCreation] // Enable manually when debugging event flow.
    public partial struct EventQueueDebugConsumerSystem : ISystem
    {
        private const int MaxLogsPerTick = 4;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EventQueue>();
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var queueEntity = SystemAPI.GetSingletonEntity<EventQueue>();
            var queue = SystemAPI.GetComponent<EventQueue>(queueEntity);
            var buffer = SystemAPI.GetBuffer<EventPayload>(queueEntity);

            var toLog = math.min(MaxLogsPerTick, buffer.Length);
            for (int i = 0; i < toLog; i++)
            {
                var ev = buffer[i];
#if UNITY_EDITOR
                Debug.Log($"[EventQueue] Tick={tickState.Tick} Type={ev.EventType} Src={ev.Source.Index} Tgt={ev.Target.Index} Val={ev.Value}");
#endif
            }
        }
    }
}
