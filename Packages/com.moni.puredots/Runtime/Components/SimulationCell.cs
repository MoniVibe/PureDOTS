using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Simulation cell entity with child buffers of agents.
    /// Divides world into cells for streaming (simulate only what's in scope).
    /// </summary>
    public struct SimulationCell : IComponentData
    {
        public int2 CellCoordinates;
        public byte IsActive; // 0 = inactive (serialized), 1 = active (simulating)
        public uint LastActivationTick;
        public uint LastDeactivationTick;
    }

    /// <summary>
    /// Buffer of agent entities in a simulation cell.
    /// </summary>
    public struct CellAgentBuffer : IBufferElementData
    {
        public Entity AgentEntity;
    }

    /// <summary>
    /// Cell streaming state for serialization/rehydration.
    /// </summary>
    public struct CellStreamingState : IComponentData
    {
        public uint SerializationVersion;
        public uint LastSerializedTick;
        public byte IsSerialized; // 0 = in memory, 1 = serialized to disk
    }

    /// <summary>
    /// Authority tag for simulation cell ownership in massive multiplayer worlds.
    /// Determines which player/server owns this cell for streaming and replication.
    /// Later: stream cells between players/servers by transferring authority only - no rebuild.
    /// </summary>
    public struct CellAuthority : IComponentData
    {
        /// <summary>
        /// Owner player ID (0 = unassigned/server, >0 = player ID).
        /// </summary>
        public byte OwnerPlayer;
    }
}

