using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Managed wrapper API for scrubbing AI decisions backward and forward in time.
    /// Used for debugging and introspection.
    /// </summary>
    public static class AIDecisionDebugAPI
    {
        /// <summary>
        /// Gets all decision events for an agent at a specific tick.
        /// </summary>
        public static bool TryGetAgentDecisionsAtTick(
            EntityManager entityManager,
            Entity registryEntity,
            ulong agentGuid,
            uint tick,
            out NativeList<DecisionEvent> events)
        {
            events = new NativeList<DecisionEvent>(Allocator.Temp);

            if (!entityManager.HasComponent<DecisionEventRegistry>(registryEntity))
            {
                return false;
            }

            var buffer = entityManager.GetBuffer<DecisionEventBuffer>(registryEntity);
            var registry = entityManager.GetComponentData<DecisionEventRegistry>(registryEntity);

            // Search for events matching agent and tick
            if (buffer.Length == 0)
            {
                return false;
            }

            int startIndex = (registry.WriteIndex - 1 + registry.Capacity) % registry.Capacity;
            for (int i = 0; i < registry.Count; i++)
            {
                int index = (startIndex - i + registry.Capacity) % registry.Capacity;
                var bufferElement = buffer[index];
                if (bufferElement.Event.Agent == agentGuid && bufferElement.Event.Tick == tick)
                {
                    events.Add(bufferElement.Event);
                }
            }

            return events.Length > 0;
        }

        /// <summary>
        /// Gets all decision events for an agent within a tick range.
        /// </summary>
        public static void GetAgentDecisionsInRange(
            EntityManager entityManager,
            Entity registryEntity,
            ulong agentGuid,
            uint startTick,
            uint endTick,
            out NativeList<DecisionEvent> events)
        {
            events = new NativeList<DecisionEvent>(Allocator.Temp);

            if (!entityManager.HasComponent<DecisionEventRegistry>(registryEntity))
            {
                return;
            }

            var buffer = entityManager.GetBuffer<DecisionEventBuffer>(registryEntity);
            var registry = entityManager.GetComponentData<DecisionEventRegistry>(registryEntity);

            if (buffer.Length == 0)
            {
                return;
            }

            int startIndex = (registry.WriteIndex - 1 + registry.Capacity) % registry.Capacity;
            for (int i = 0; i < registry.Count; i++)
            {
                int index = (startIndex - i + registry.Capacity) % registry.Capacity;
                var bufferElement = buffer[index];
                var evt = bufferElement.Event;
                if (evt.Agent == agentGuid && evt.Tick >= startTick && evt.Tick <= endTick)
                {
                    events.Add(evt);
                }
            }
        }

        /// <summary>
        /// Scrubs AI decisions forward/backward in time for a specific agent.
        /// </summary>
        public static void ScrubAgentDecisions(
            EntityManager entityManager,
            Entity registryEntity,
            ulong agentGuid,
            uint currentTick,
            int deltaTicks,
            out NativeList<DecisionEvent> events)
        {
            uint targetTick = (uint)math.max(0, (int)currentTick + deltaTicks);
            GetAgentDecisionsInRange(entityManager, registryEntity, agentGuid, targetTick, targetTick, out events);
        }
    }
}

