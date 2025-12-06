using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Component storing thread assignment per spatial cell via Morton key ranges.
    /// </summary>
    public struct SpatialThreadPartition : IComponentData
    {
        /// <summary>
        /// Thread ID assigned to this partition.
        /// </summary>
        public int ThreadId;

        /// <summary>
        /// Minimum Morton key in this partition's range.
        /// </summary>
        public uint MinMortonKey;

        /// <summary>
        /// Maximum Morton key in this partition's range.
        /// </summary>
        public uint MaxMortonKey;
    }

    /// <summary>
    /// Border event for cross-thread spatial operations.
    /// </summary>
    public struct SpatialBorderEvent : IBufferElementData
    {
        /// <summary>
        /// Source thread ID.
        /// </summary>
        public int SourceThreadId;

        /// <summary>
        /// Target thread ID.
        /// </summary>
        public int TargetThreadId;

        /// <summary>
        /// Entity involved in the border operation.
        /// </summary>
        public Entity Entity;

        /// <summary>
        /// Morton key of the entity.
        /// </summary>
        public uint MortonKey;

        /// <summary>
        /// Event type: 0 = enter, 1 = exit, 2 = query
        /// </summary>
        public byte EventType;
    }

    /// <summary>
    /// Utilities for spatial thread partitioning using Morton keys.
    /// </summary>
    [BurstCompile]
    public static class SpatialThreadPartitioning
    {
        /// <summary>
        /// Partitions Morton keys into thread ranges for spatial thread assignment.
        /// </summary>
        [BurstCompile]
        public static void PartitionMortonKeys(
            uint minKey,
            uint maxKey,
            int threadCount,
            out NativeArray<uint> threadMinKeys,
            out NativeArray<uint> threadMaxKeys)
        {
            threadMinKeys = new NativeArray<uint>(threadCount, Allocator.Temp);
            threadMaxKeys = new NativeArray<uint>(threadCount, Allocator.Temp);

            uint keyRange = maxKey - minKey;
            uint keysPerThread = keyRange / (uint)threadCount;

            for (int i = 0; i < threadCount; i++)
            {
                threadMinKeys[i] = minKey + (uint)i * keysPerThread;
                threadMaxKeys[i] = (i == threadCount - 1) ? maxKey : threadMinKeys[i] + keysPerThread - 1;
            }
        }

        /// <summary>
        /// Gets the thread ID for a Morton key based on partition ranges.
        /// </summary>
        [BurstCompile]
        public static int GetThreadIdForMortonKey(
            uint mortonKey,
            in NativeArray<uint> threadMinKeys,
            in NativeArray<uint> threadMaxKeys)
        {
            for (int i = 0; i < threadMinKeys.Length; i++)
            {
                if (mortonKey >= threadMinKeys[i] && mortonKey <= threadMaxKeys[i])
                {
                    return i;
                }
            }

            // Fallback to last thread
            return threadMinKeys.Length - 1;
        }

        /// <summary>
        /// Checks if a Morton key is at a partition boundary.
        /// </summary>
        [BurstCompile]
        public static bool IsBoundaryKey(
            uint mortonKey,
            in NativeArray<uint> threadMinKeys,
            in NativeArray<uint> threadMaxKeys,
            float boundaryThreshold = 0.1f)
        {
            uint keyRange = threadMaxKeys[threadMaxKeys.Length - 1] - threadMinKeys[0];
            uint threshold = (uint)(keyRange * boundaryThreshold);

            for (int i = 0; i < threadMinKeys.Length - 1; i++)
            {
                uint boundary = threadMaxKeys[i];
                if (math.abs((long)mortonKey - (long)boundary) < threshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

