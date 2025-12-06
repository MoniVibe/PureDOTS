using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Memory metrics for tracking fragmentation and chunk reuse.
    /// </summary>
    public struct MemoryMetrics : IComponentData
    {
        /// <summary>Chunk reuse rate (0-1, target >0.9).</summary>
        public float ChunkReuseRate;
        
        /// <summary>Fragmentation score (0-1, lower is better).</summary>
        public float FragmentationScore;
        
        /// <summary>GC count (Gen0, Gen1, Gen2).</summary>
        public int GcCount0;
        public int GcCount1;
        public int GcCount2;
        
        /// <summary>Last tick when metrics were updated.</summary>
        public uint LastUpdateTick;
    }
}

