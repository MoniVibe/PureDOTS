using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Minimal temporal cache for spatial grid snapshots (stub for demo builds).
    /// </summary>
    public struct TemporalGridCache
    {
        public bool IsCreated;
        public uint LastUpdatedTick;

        public static TemporalGridCache Create(int cacheSize, Allocator allocator)
        {
            return new TemporalGridCache
            {
                IsCreated = true,
                LastUpdatedTick = 0
            };
        }

        public void WriteSnapshot(uint tick, uint version, NativeHashMap<ulong, SpatialCellSnapshot> changedCells, int entryCount, Allocator allocator)
        {
            LastUpdatedTick = tick;
            // Stub: real implementation would store changedCells.
        }

        public void Dispose()
        {
            IsCreated = false;
        }
    }
}
