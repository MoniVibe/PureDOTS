using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Spatial
{
    /// <summary>
    /// Captures spatial grid state every tick (if changed) for deterministic rewind support.
    /// Stores snapshots in a temporal ring buffer (16 ticks by default).
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(SpatialGridBuildSystem))]
    public partial struct SpatialGridSnapshotSystem : ISystem
    {
        private TemporalGridCache _cache;
        private bool _cacheInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialGridConfig>();
            state.RequireForUpdate<SpatialGridState>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _cacheInitialized = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            // Only capture during record mode
            if (rewindState.Mode != RewindMode.Record || timeState.IsPaused)
            {
                return;
            }

            // Initialize cache if needed
            if (!_cacheInitialized)
            {
                var cacheSize = 16; // Default: 16 ticks
                _cache = TemporalGridCache.Create(cacheSize, Allocator.Persistent);
                _cacheInitialized = true;
            }

            var gridEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<SpatialGridConfig, SpatialGridState>()
                .Build();

            if (gridEntityQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var gridEntity = gridEntityQuery.GetSingletonEntity();
            var gridState = SystemAPI.GetComponent<SpatialGridState>(gridEntity);
            var config = SystemAPI.GetComponent<SpatialGridConfig>(gridEntity);
            var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
            var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);

            // Check if state changed
            if (gridState.Version == 0)
            {
                return; // No data yet
            }

            // Capture snapshot (sparse: only changed cells)
            CaptureSnapshot(ref state, gridEntity, timeState.Tick, gridState.Version, in config, in entries, in ranges);
        }

        private void CaptureSnapshot(ref SystemState state, Entity gridEntity, uint tick, uint version, in SpatialGridConfig config, in DynamicBuffer<SpatialGridEntry> entries, in DynamicBuffer<SpatialGridCellRange> ranges)
        {
            // Collect changed cells (sparse snapshot)
            var changedCells = new NativeHashMap<ulong, SpatialCellSnapshot>(ranges.Length, Allocator.TempJob);

            // For now, capture all cells (full implementation would track deltas)
            for (int i = 0; i < ranges.Length; i++)
            {
                var range = ranges[i];
                if (range.Count <= 0)
                {
                    continue;
                }

                // Get cell coordinates from first entry
                if (range.StartIndex >= entries.Length)
                {
                    continue;
                }

                var firstEntry = entries[range.StartIndex];
                var cellKey = firstEntry.GetPrimaryKey();

                // Compute cell bounds
                SpatialHash.Unflatten(i, config, out var coords);
                var cellSize = config.GetActiveCellSize();
                var cellMin = config.WorldMin + (float3)coords * cellSize;
                var cellMax = cellMin + cellSize;

                var snapshot = new SpatialCellSnapshot
                {
                    Index = coords,
                    Bounds = new AABB
                    {
                        Center = (cellMin + cellMax) * 0.5f,
                        Extents = (cellMax - cellMin) * 0.5f
                    },
                    EntityCount = range.Count,
                    Density = 0f, // Would compute from cell volume
                    Level = 0, // Would determine from config
                    MortonKey = cellKey
                };

                changedCells.TryAdd(cellKey, snapshot);
            }

            // Write snapshot to cache
            _cache.WriteSnapshot(tick, version, changedCells, entries.Length, Allocator.Persistent);

            changedCells.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_cacheInitialized && _cache.IsCreated)
            {
                _cache.Dispose();
                _cacheInitialized = false;
            }
        }
    }
}
