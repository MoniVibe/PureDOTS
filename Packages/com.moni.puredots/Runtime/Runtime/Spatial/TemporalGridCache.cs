using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Snapshot of spatial grid state at a specific tick.
    /// Stores sparse cell data for deterministic rewind and prediction interpolation.
    /// </summary>
    public struct SpatialGridSnapshot
    {
        /// <summary>
        /// Tick when this snapshot was captured.
        /// </summary>
        public uint Tick;

        /// <summary>
        /// Grid version at this tick.
        /// </summary>
        public uint Version;

        /// <summary>
        /// Sparse cell data (only cells that changed).
        /// Key: SFC cell key, Value: cell state
        /// </summary>
        public NativeHashMap<ulong, SpatialCellSnapshot> Cells;

        /// <summary>
        /// Total number of entities in the grid at this tick.
        /// </summary>
        public int TotalEntityCount;

        /// <summary>
        /// Whether this snapshot has valid data.
        /// </summary>
        public bool IsValid;

        public SpatialGridSnapshot(uint tick, uint version, Allocator allocator)
        {
            Tick = tick;
            Version = version;
            Cells = new NativeHashMap<ulong, SpatialCellSnapshot>(1024, allocator);
            TotalEntityCount = 0;
            IsValid = true;
        }

        public void Dispose()
        {
            if (Cells.IsCreated)
            {
                Cells.Dispose();
            }
            IsValid = false;
        }
    }

    /// <summary>
    /// Snapshot of a single spatial cell's state.
    /// </summary>
    public struct SpatialCellSnapshot
    {
        public int3 Index;
        public AABB Bounds;
        public int EntityCount;
        public float Density;
        public byte Level;
        public ulong MortonKey;
    }

    /// <summary>
    /// Temporal grid cache with ring buffer for deterministic rewind.
    /// Stores grid snapshots for the last N ticks (default: 16).
    /// </summary>
    public struct TemporalGridCache
    {
        /// <summary>
        /// Ring buffer of snapshots (N=16 ticks by default).
        /// </summary>
        public NativeArray<SpatialGridSnapshot> Snapshots;

        /// <summary>
        /// Cache size (number of ticks to store).
        /// </summary>
        public int CacheSize;

        /// <summary>
        /// Current write index in the ring buffer.
        /// </summary>
        public int WriteIndex;

        /// <summary>
        /// Last tick that was written.
        /// </summary>
        public uint LastWrittenTick;

        /// <summary>
        /// Creates a new temporal grid cache with the specified size.
        /// </summary>
        public static TemporalGridCache Create(int cacheSize, Allocator allocator)
        {
            var snapshots = new NativeArray<SpatialGridSnapshot>(cacheSize, allocator);
            for (int i = 0; i < cacheSize; i++)
            {
                snapshots[i] = new SpatialGridSnapshot(0, 0, allocator);
                snapshots[i].IsValid = false;
            }

            return new TemporalGridCache
            {
                Snapshots = snapshots,
                CacheSize = cacheSize,
                WriteIndex = 0,
                LastWrittenTick = 0
            };
        }

        /// <summary>
        /// Writes a new snapshot to the cache (sparse: only if changed).
        /// </summary>
        public void WriteSnapshot(uint tick, uint version, in NativeHashMap<ulong, SpatialCellSnapshot> changedCells, int totalEntityCount, Allocator allocator)
        {
            if (LastWrittenTick > 0 && version == GetSnapshot(LastWrittenTick).Version)
            {
                return; // No change, skip write (sparse temporal grid)
            }

            var index = (int)(tick % (uint)CacheSize);
            var snapshot = Snapshots[index];

            // Dispose old snapshot if valid
            if (snapshot.IsValid)
            {
                snapshot.Dispose();
            }

            // Create new snapshot
            snapshot = new SpatialGridSnapshot(tick, version, allocator);
            snapshot.TotalEntityCount = totalEntityCount;

            // Copy changed cells
            foreach (var kvp in changedCells)
            {
                snapshot.Cells.TryAdd(kvp.Key, kvp.Value);
            }

            Snapshots[index] = snapshot;
            WriteIndex = index;
            LastWrittenTick = tick;
        }

        /// <summary>
        /// Gets a snapshot for a specific tick (from ring buffer).
        /// </summary>
        public readonly SpatialGridSnapshot GetSnapshot(uint tick)
        {
            var index = (int)(tick % (uint)CacheSize);
            if (index < 0 || index >= CacheSize)
            {
                return default;
            }

            var snapshot = Snapshots[index];
            if (snapshot.IsValid && snapshot.Tick == tick)
            {
                return snapshot;
            }

            return default; // Snapshot not found or invalid
        }

        /// <summary>
        /// Checks if a snapshot exists for the specified tick.
        /// </summary>
        public readonly bool HasSnapshot(uint tick)
        {
            var snapshot = GetSnapshot(tick);
            return snapshot.IsValid && snapshot.Tick == tick;
        }

        /// <summary>
        /// Disposes all snapshots in the cache.
        /// </summary>
        public void Dispose()
        {
            if (!Snapshots.IsCreated)
            {
                return;
            }

            for (int i = 0; i < CacheSize; i++)
            {
                if (Snapshots[i].IsValid)
                {
                    Snapshots[i].Dispose();
                }
            }

            Snapshots.Dispose();
            CacheSize = 0;
            WriteIndex = 0;
            LastWrittenTick = 0;
        }

        /// <summary>
        /// Checks if the cache has been disposed.
        /// </summary>
        public readonly bool IsCreated => Snapshots.IsCreated;
    }
}

