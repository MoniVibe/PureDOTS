using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Serialization
{
    /// <summary>
    /// Future interface for offloading ECS worlds to separate processes or machines.
    /// Stub implementation - not yet implemented.
    /// </summary>
    public interface IDistributedSimulationInterface
    {
        /// <summary>
        /// Sends world state to a remote process.
        /// </summary>
        void SendWorldState(byte targetProcessId, NativeArray<byte> serializedState);

        /// <summary>
        /// Receives world state from a remote process.
        /// </summary>
        bool TryReceiveWorldState(out byte sourceProcessId, out NativeArray<byte> serializedState);

        /// <summary>
        /// Synchronizes world partitions across distributed processes.
        /// </summary>
        void SynchronizePartitions(NativeArray<WorldPartition> partitions);
    }

    /// <summary>
    /// Stub implementation of distributed simulation interface.
    /// </summary>
    public class DistributedSimulationInterfaceStub : IDistributedSimulationInterface
    {
        public void SendWorldState(byte targetProcessId, NativeArray<byte> serializedState)
        {
            // Stub: Not yet implemented
        }

        public bool TryReceiveWorldState(out byte sourceProcessId, out NativeArray<byte> serializedState)
        {
            sourceProcessId = 0;
            serializedState = default;
            return false; // Stub: Not yet implemented
        }

        public void SynchronizePartitions(NativeArray<WorldPartition> partitions)
        {
            // Stub: Not yet implemented
        }
    }
}

