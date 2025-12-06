using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Per-thread load profile for adaptive load balancing.
    /// </summary>
    public struct ThreadLoadProfile : IComponentData
    {
        /// <summary>
        /// Thread ID.
        /// </summary>
        public int ThreadId;

        /// <summary>
        /// Average job duration in milliseconds.
        /// </summary>
        public float AvgJobDurationMs;

        /// <summary>
        /// Total job count.
        /// </summary>
        public int JobCount;

        /// <summary>
        /// Last measurement tick.
        /// </summary>
        public uint LastMeasurementTick;
    }

    /// <summary>
    /// Load balancing state singleton.
    /// </summary>
    public struct ThreadLoadState : IComponentData
    {
        /// <summary>
        /// Last rebalance tick.
        /// </summary>
        public uint LastRebalanceTick;

        /// <summary>
        /// Rebalance interval in ticks (default: 60 ticks = 1 second at 60Hz).
        /// </summary>
        public uint RebalanceIntervalTicks;

        /// <summary>
        /// Current imbalance ratio (0.0-1.0).
        /// </summary>
        public float CurrentImbalanceRatio;
    }

    /// <summary>
    /// Adaptive load balancer that measures per-thread job duration and redistributes work.
    /// </summary>
    [BurstCompile]
    public static class LoadBalancer
    {
        /// <summary>
        /// Measures load imbalance across threads.
        /// Returns true if imbalance exceeds threshold.
        /// </summary>
        [BurstCompile]
        public static bool MeasureImbalance(
            in NativeArray<ThreadLoadProfile> profiles,
            float threshold,
            out float imbalanceRatio)
        {
            if (profiles.Length == 0)
            {
                imbalanceRatio = 0f;
                return false;
            }

            float minLoad = float.MaxValue;
            float maxLoad = float.MinValue;

            for (int i = 0; i < profiles.Length; i++)
            {
                float load = profiles[i].AvgJobDurationMs;
                minLoad = math.min(minLoad, load);
                maxLoad = math.max(maxLoad, load);
            }

            if (maxLoad <= 0f)
            {
                imbalanceRatio = 0f;
                return false;
            }

            imbalanceRatio = (maxLoad - minLoad) / maxLoad;
            return imbalanceRatio > threshold;
        }

        /// <summary>
        /// Redistributes chunk ranges based on load profiles.
        /// </summary>
        [BurstCompile]
        public static void RedistributeRanges(
            in NativeArray<ThreadLoadProfile> profiles,
            int totalChunks,
            out NativeArray<int> chunkCountsPerThread)
        {
            chunkCountsPerThread = new NativeArray<int>(profiles.Length, Allocator.Temp);

            if (profiles.Length == 0 || totalChunks <= 0)
            {
                return;
            }

            // Calculate total load
            float totalLoad = 0f;
            for (int i = 0; i < profiles.Length; i++)
            {
                totalLoad += profiles[i].AvgJobDurationMs;
            }

            if (totalLoad <= 0f)
            {
                // Equal distribution
                int chunksPerThread = totalChunks / profiles.Length;
                int remainder = totalChunks % profiles.Length;
                for (int i = 0; i < profiles.Length; i++)
                {
                    chunkCountsPerThread[i] = chunksPerThread + (i < remainder ? 1 : 0);
                }
                return;
            }

            // Distribute chunks proportionally to inverse load (faster threads get more work)
            int allocatedChunks = 0;
            for (int i = 0; i < profiles.Length; i++)
            {
                float loadRatio = profiles[i].AvgJobDurationMs / totalLoad;
                // Inverse: threads with lower load get more chunks
                float inverseRatio = 1f - loadRatio;
                int chunks = (int)(totalChunks * inverseRatio / profiles.Length);
                chunkCountsPerThread[i] = chunks;
                allocatedChunks += chunks;
            }

            // Distribute remainder
            int remainder2 = totalChunks - allocatedChunks;
            for (int i = 0; i < remainder2 && i < profiles.Length; i++)
            {
                chunkCountsPerThread[i]++;
            }
        }

        /// <summary>
        /// Updates load profile for a thread.
        /// </summary>
        [BurstCompile]
        public static void UpdateLoadProfile(
            ref ThreadLoadProfile profile,
            float jobDurationMs,
            uint currentTick)
        {
            if (profile.JobCount == 0)
            {
                profile.AvgJobDurationMs = jobDurationMs;
            }
            else
            {
                // Exponential moving average
                float alpha = 0.1f; // Smoothing factor
                profile.AvgJobDurationMs = alpha * jobDurationMs + (1f - alpha) * profile.AvgJobDurationMs;
            }

            profile.JobCount++;
            profile.LastMeasurementTick = currentTick;
        }
    }
}

