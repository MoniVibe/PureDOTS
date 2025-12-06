using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Utilities for adaptive batch sizing based on job cost measurement.
    /// </summary>
    [BurstCompile]
    public static class AdaptiveBatchSizing
    {
        /// <summary>
        /// Calculates optimal batch count for parallel jobs based on entity count and thread count.
        /// </summary>
        [BurstCompile]
        public static int CalculateBatchCount(int entityCount, int threadCount, int minBatchSize = 64)
        {
            if (entityCount <= 0 || threadCount <= 0)
            {
                return minBatchSize;
            }

            int batchCount = entityCount / threadCount;
            return math.max(batchCount, minBatchSize);
        }

        /// <summary>
        /// Calculates batch count with micro-task partitioning threshold (1ms target).
        /// </summary>
        [BurstCompile]
        public static int CalculateAdaptiveBatchCount(
            int entityCount,
            int threadCount,
            float estimatedTimePerEntityMs,
            float thresholdMs = 1.0f,
            int minBatchSize = 64)
        {
            if (entityCount <= 0 || threadCount <= 0)
            {
                return minBatchSize;
            }

            // Calculate entities per thread
            int entitiesPerThread = entityCount / threadCount;
            
            // Estimate time per thread
            float estimatedTimeMs = entitiesPerThread * estimatedTimePerEntityMs;
            
            // If estimated time exceeds threshold, subdivide further
            if (estimatedTimeMs > thresholdMs)
            {
                int subdivisions = (int)math.ceil(estimatedTimeMs / thresholdMs);
                entitiesPerThread = entitiesPerThread / subdivisions;
            }

            return math.max(entitiesPerThread, minBatchSize);
        }

        /// <summary>
        /// Schedules a parallel job with adaptive batch sizing.
        /// </summary>
        public static JobHandle ScheduleAdaptive<T>(
            ref T job,
            EntityQuery query,
            ref SystemState state,
            in ThreadingConfig config) where T : struct, IJobEntity
        {
            int entityCount = query.CalculateEntityCount();
            int threadCount = config.SimulationThreadCount;
            int batchCount = CalculateAdaptiveBatchCount(
                entityCount,
                threadCount,
                config.MicroTaskThresholdMs / math.max(entityCount, 1),
                config.MicroTaskThresholdMs,
                config.DefaultBatchCount);

            return job.ScheduleParallel(query, batchCount, state.Dependency);
        }
    }
}

