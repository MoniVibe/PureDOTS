using Unity.Entities;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Configuration singleton for threading and parallelization settings.
    /// </summary>
    public struct ThreadingConfig : IComponentData
    {
        /// <summary>
        /// Number of threads for simulation/logic domain (default: 2-4).
        /// </summary>
        public int SimulationThreadCount;

        /// <summary>
        /// Number of threads for physics domain (default: 2-4).
        /// </summary>
        public int PhysicsThreadCount;

        /// <summary>
        /// Number of threads for async/IO domain (default: 1-2).
        /// </summary>
        public int AsyncIOThreadCount;

        /// <summary>
        /// Number of threads for background tasks (default: 1).
        /// </summary>
        public int BackgroundThreadCount;

        /// <summary>
        /// Micro-task batch size threshold in milliseconds. Jobs exceeding this will be subdivided.
        /// </summary>
        public float MicroTaskThresholdMs;

        /// <summary>
        /// Default batch count for parallel jobs (entities per batch).
        /// </summary>
        public int DefaultBatchCount;

        /// <summary>
        /// Enable work-stealing queues.
        /// </summary>
        public bool EnableWorkStealing;

        /// <summary>
        /// Enable adaptive load balancing.
        /// </summary>
        public bool EnableLoadBalancing;

        /// <summary>
        /// Load imbalance threshold (0.0-1.0). Rebalance if imbalance exceeds this.
        /// </summary>
        public float LoadImbalanceThreshold;

        /// <summary>
        /// Enable spatial thread partitioning via Morton keys.
        /// </summary>
        public bool EnableSpatialPartitioning;

        /// <summary>
        /// Enable double buffering for write contention avoidance.
        /// </summary>
        public bool EnableDoubleBuffering;

        public static ThreadingConfig Default => new ThreadingConfig
        {
            SimulationThreadCount = 2,
            PhysicsThreadCount = 2,
            AsyncIOThreadCount = 1,
            BackgroundThreadCount = 1,
            MicroTaskThresholdMs = 1.0f,
            DefaultBatchCount = 256,
            EnableWorkStealing = true,
            EnableLoadBalancing = true,
            LoadImbalanceThreshold = 0.2f,
            EnableSpatialPartitioning = true,
            EnableDoubleBuffering = true
        };
    }
}

