using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Ring buffer for decision events, tied to RewindState for time scrubbing.
    /// </summary>
    [InternalBufferCapacity(256)]
    public struct DecisionEventBuffer : IBufferElementData
    {
        public DecisionEvent Event;
    }

    /// <summary>
    /// Singleton managing the decision event ring buffer.
    /// </summary>
    public struct DecisionEventRegistry : IComponentData
    {
        /// <summary>
        /// Current write index in the ring buffer.
        /// </summary>
        public int WriteIndex;

        /// <summary>
        /// Maximum capacity of the ring buffer.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// Current number of events stored.
        /// </summary>
        public int Count;

        public DecisionEventRegistry(int capacity)
        {
            WriteIndex = 0;
            Capacity = capacity;
            Count = 0;
        }
    }

    /// <summary>
    /// Burst-safe helper for managing decision event ring buffer.
    /// </summary>
    [BurstCompile]
    public static class DecisionEventBufferHelper
    {
        [BurstCompile]
        public static void AddEvent(
            ref DynamicBuffer<DecisionEventBuffer> buffer,
            ref DecisionEventRegistry registry,
            in DecisionEvent evt)
        {
            if (buffer.Length == 0)
            {
                // Initialize buffer
                buffer.ResizeUninitialized(registry.Capacity);
            }

            // Write to ring buffer
            int index = registry.WriteIndex % registry.Capacity;
            buffer[index] = new DecisionEventBuffer { Event = evt };

            registry.WriteIndex = (registry.WriteIndex + 1) % registry.Capacity;
            if (registry.Count < registry.Capacity)
            {
                registry.Count++;
            }
        }

        [BurstCompile]
        public static bool TryGetEventAtTick(
            in DynamicBuffer<DecisionEventBuffer> buffer,
            in DecisionEventRegistry registry,
            uint tick,
            out DecisionEvent evt)
        {
            evt = default;

            if (buffer.Length == 0)
            {
                return false;
            }

            // Search backwards from write index
            int startIndex = (registry.WriteIndex - 1 + registry.Capacity) % registry.Capacity;
            for (int i = 0; i < registry.Count; i++)
            {
                int index = (startIndex - i + registry.Capacity) % registry.Capacity;
                var bufferElement = buffer[index];
                if (bufferElement.Event.Tick == tick)
                {
                    evt = bufferElement.Event;
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        public static void ClearEventsBeforeTick(
            ref DynamicBuffer<DecisionEventBuffer> buffer,
            ref DecisionEventRegistry registry,
            uint minTick)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            // Remove events older than minTick
            int removed = 0;
            int startIndex = (registry.WriteIndex - registry.Count + registry.Capacity) % registry.Capacity;
            for (int i = 0; i < registry.Count; i++)
            {
                int index = (startIndex + i) % registry.Capacity;
                var bufferElement = buffer[index];
                if (bufferElement.Event.Tick < minTick)
                {
                    buffer[index] = default;
                    removed++;
                }
            }

            registry.Count -= removed;
        }
    }
}

