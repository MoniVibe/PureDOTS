using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// Helper utilities for maintaining CellAgentBuffer membership on spawn/move/despawn.
    /// </summary>
    public static class CellAgentBufferHelpers
    {
        /// <summary>
        /// Adds an agent to a cell's buffer if not already present.
        /// Returns false if the cell is missing the buffer.
        /// </summary>
        public static bool TryAddAgent(EntityManager entityManager, Entity cell, Entity agent)
        {
            if (!entityManager.HasBuffer<CellAgentBuffer>(cell))
                return false;

            var buffer = entityManager.GetBuffer<CellAgentBuffer>(cell);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].AgentEntity == agent)
                {
                    return true; // already present
                }
            }

            buffer.Add(new CellAgentBuffer { AgentEntity = agent });
            return true;
        }

        /// <summary>
        /// Removes an agent from a cell's buffer if present.
        /// Returns false if the cell is missing the buffer.
        /// </summary>
        public static bool TryRemoveAgent(EntityManager entityManager, Entity cell, Entity agent)
        {
            if (!entityManager.HasBuffer<CellAgentBuffer>(cell))
                return false;

            var buffer = entityManager.GetBuffer<CellAgentBuffer>(cell);
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (buffer[i].AgentEntity == agent)
                {
                    buffer.RemoveAt(i);
                    return true;
                }
            }
            return true;
        }

        /// <summary>
        /// Moves an agent between two cells; add to destination before removing from source.
        /// Returns false if either buffer is missing.
        /// </summary>
        public static bool TryMoveAgent(EntityManager entityManager, Entity sourceCell, Entity destCell, Entity agent)
        {
            if (!entityManager.HasBuffer<CellAgentBuffer>(sourceCell) ||
                !entityManager.HasBuffer<CellAgentBuffer>(destCell))
                return false;

            // Add to destination first to avoid transient loss if remove fails
            TryAddAgent(entityManager, destCell, agent);
            TryRemoveAgent(entityManager, sourceCell, agent);
            return true;
        }
    }
}
