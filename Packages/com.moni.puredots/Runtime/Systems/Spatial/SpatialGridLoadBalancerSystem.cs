using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Spatial
{
    /// <summary>
    /// Work queue range owned by a worker thread.
    /// Each range owns a span of SFC keys: [StartKey, EndKey).
    /// </summary>
    public struct WorkQueueRange
    {
        public ulong StartKey; // Inclusive start of SFC key range
        public ulong EndKey; // Exclusive end of SFC key range
        public int OwnerThreadId; // Thread that owns this range (atomic)
        public int CellCount; // Number of cells in this range (for load balancing)
    }

    /// <summary>
    /// Dynamic load balancing system for spatial grid rebuilds.
    /// Maintains work queues per thread and migrates key ranges to equalize load.
    /// Uses atomic operations for lock-free load balancing.
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateBefore(typeof(SpatialGridBuildSystem))]
    public partial struct SpatialGridLoadBalancerSystem : ISystem
    {
        private const int LoadBalanceSampleInterval = 100; // Sample every 100 ticks
        private const float LoadBalanceThreshold = 1.2f; // Migrate if load > 1.2x average
        private uint _lastSampleTick;
        private bool _isInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialGridConfig>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _lastSampleTick = 0;
            _isInitialized = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record || timeState.IsPaused)
            {
                return;
            }

            var config = SystemAPI.GetSingleton<SpatialGridConfig>();
            if (!config.IsHierarchical)
            {
                return; // Only balance hierarchical grids
            }

            // Check if it's time to sample and rebalance
            if (timeState.Tick - _lastSampleTick < LoadBalanceSampleInterval)
            {
                return;
            }

            var gridEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<SpatialGridConfig>()
                .Build();

            if (gridEntityQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var gridEntity = gridEntityQuery.GetSingletonEntity();
            var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
            var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);

            if (entries.Length == 0 || ranges.Length == 0)
            {
                _lastSampleTick = timeState.Tick;
                return;
            }

            // Initialize work queues if needed
            if (!_isInitialized)
            {
                InitializeWorkQueues(ref state, gridEntity, in config, in entries, in ranges);
                _isInitialized = true;
            }

            // Sample per-thread cell counts and rebalance
            RebalanceWorkQueues(ref state, gridEntity, in config, in entries, in ranges, timeState.Tick);

            _lastSampleTick = timeState.Tick;
        }

        private void InitializeWorkQueues(ref SystemState state, Entity gridEntity, in SpatialGridConfig config, in DynamicBuffer<SpatialGridEntry> entries, in DynamicBuffer<SpatialGridCellRange> ranges)
        {
            // Get number of worker threads
            var threadCount = JobsUtility.JobWorkerCount + 1; // +1 for main thread
            if (threadCount <= 0)
            {
                threadCount = 1;
            }

            // Create work queue ranges
            // For now, use a simple approach: divide SFC key space evenly
            // In a full implementation, this would be stored in a component or blob asset
            // Placeholder: actual implementation would store WorkQueueRange[] in a component
        }

        private void RebalanceWorkQueues(ref SystemState state, Entity gridEntity, in SpatialGridConfig config, in DynamicBuffer<SpatialGridEntry> entries, in DynamicBuffer<SpatialGridCellRange> ranges, uint currentTick)
        {
            // Sample per-thread cell counts
            // Compute average load
            // Migrate key ranges if load imbalance exceeds threshold
            // Use atomic exchange on WorkQueueRange.OwnerThreadId for lock-free migration

            // Placeholder: full implementation would:
            // 1. Sample cell counts per thread
            // 2. Compute average load
            // 3. Identify overloaded/underloaded threads
            // 4. Migrate key ranges using atomic operations
        }
    }

    /// <summary>
    /// Component storing work queue ranges for load balancing.
    /// </summary>
    public struct SpatialGridWorkQueues : IComponentData
    {
        /// <summary>
        /// Number of work queue ranges.
        /// </summary>
        public int RangeCount;
    }

    /// <summary>
    /// Buffer storing work queue ranges.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridWorkQueueRangeBuffer : IBufferElementData
    {
        public WorkQueueRange Range;
    }
}

