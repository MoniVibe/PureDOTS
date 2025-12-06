using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.WorldBus
{
    /// <summary>
    /// Routes messages between worlds based on target world ID.
    /// Example: Godgame divine intervention → Space4X orbital climate change.
    /// </summary>
    [BurstCompile]
    public static class WorldBusRouter
    {
        /// <summary>
        /// Routes messages from the bus to the appropriate target world.
        /// </summary>
        [BurstCompile]
        public static void RouteMessages(
            ref WorldBus bus,
            byte currentWorldId,
            ref DynamicBuffer<WorldMessage> targetBuffer)
        {
            while (bus.TryDequeueMessage(out var message))
            {
                if (message.TargetWorld == currentWorldId)
                {
                    targetBuffer.Add(message);
                }
                // Messages for other worlds would be forwarded to their respective buses
            }
        }

        /// <summary>
        /// Sends a message from one world to another.
        /// </summary>
        [BurstCompile]
        public static void SendMessage(
            ref WorldBus bus,
            byte sourceWorld,
            byte targetWorld,
            FixedBytes64 payload,
            uint tick,
            byte messageType)
        {
            var message = new WorldMessage(sourceWorld, targetWorld, payload, tick, messageType);
            bus.EnqueueMessage(message);
        }
    }
}

