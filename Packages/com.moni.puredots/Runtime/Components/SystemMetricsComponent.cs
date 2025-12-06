using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Metrics per system for profiling and automatic pruning.
    /// </summary>
    public struct SystemMetrics : IComponentData
    {
        /// <summary>Average tick cost in milliseconds.</summary>
        public float TickCostMs;
        
        /// <summary>Entity count processed.</summary>
        public int EntityCount;
        
        /// <summary>Job count scheduled.</summary>
        public int JobCount;
        
        /// <summary>Last tick when metrics were updated.</summary>
        public uint LastUpdateTick;
        
        /// <summary>Total cost accumulated (for averaging).</summary>
        public float TotalCostMs;
        
        /// <summary>Number of samples collected.</summary>
        public int SampleCount;
    }
}

