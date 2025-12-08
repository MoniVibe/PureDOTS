using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Spatial
{
    /// <summary>
    /// Adaptive density-driven subdivision system for hierarchical spatial grids.
    /// Runs at ~0.2 Hz (every 5 seconds at 60 Hz) to subdivide/merge cells based on entity density.
    /// Only operates on L2_Planet and L3_Local levels (L0/L1 use analytic orbits only).
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(SpatialGridBuildSystem))]
    public partial struct SpatialGridRefinementSystem : ISystem
    {
        private const float RefinementTickRate = 0.2f; // 0.2 Hz = every 5 seconds
        private uint _lastRefinementTick;
        private int _tickInterval;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialGridConfig>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _lastRefinementTick = 0;
            _tickInterval = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            // Only refine during record mode
            if (rewindState.Mode != RewindMode.Record || timeState.IsPaused)
            {
                return;
            }

            // Calculate tick interval based on refinement rate
            if (_tickInterval == 0)
            {
                var tickRate = timeState.TickRate;
                _tickInterval = (int)math.ceil(tickRate / RefinementTickRate);
                if (_tickInterval <= 0)
                {
                    _tickInterval = 1;
                }
            }

            // Check if it's time to refine
            if (timeState.Tick - _lastRefinementTick < (uint)_tickInterval)
            {
                return;
            }

            var config = SystemAPI.GetSingleton<SpatialGridConfig>();
            if (!config.IsHierarchical)
            {
                return; // Only refine hierarchical grids
            }

            // Get default thresholds if not set
            var upperThreshold = config.UpperDensityThreshold > 0f
                ? config.UpperDensityThreshold
                : 100.0f;
            var lowerThreshold = config.LowerDensityThreshold > 0f
                ? config.LowerDensityThreshold
                : 10.0f;
            var maxDepth = config.MaxSubdivisionDepth > 0
                ? (byte)config.MaxSubdivisionDepth
                : (byte)4;

            // Find grid entity
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
                _lastRefinementTick = timeState.Tick;
                return;
            }

            // Refine only L2_Planet and L3_Local levels
            RefineLevel(ref state, gridEntity, HierarchicalGridLevel.L2_Planet, in config, in entries, in ranges, upperThreshold, lowerThreshold, maxDepth);
            RefineLevel(ref state, gridEntity, HierarchicalGridLevel.L3_Local, in config, in entries, in ranges, upperThreshold, lowerThreshold, maxDepth);

            _lastRefinementTick = timeState.Tick;
        }

        private void RefineLevel(ref SystemState state, Entity gridEntity, HierarchicalGridLevel level, in SpatialGridConfig config, in DynamicBuffer<SpatialGridEntry> entries, in DynamicBuffer<SpatialGridCellRange> ranges, float upperThreshold, float lowerThreshold, byte maxDepth)
        {
            if (!config.TryGetLevelConfig(level, out var levelConfig))
            {
                return;
            }

            // Compute density per cell for this level
            var cellDensities = new NativeParallelHashMap<int, float>(ranges.Length, Allocator.TempJob);

            var computeDensityJob = new ComputeCellDensityJob
            {
                Entries = entries.AsNativeArray(),
                Ranges = ranges.AsNativeArray(),
                Level = (byte)level,
                CellSize = levelConfig.CellSize,
                Densities = cellDensities.AsParallelWriter()
            };

            var jobHandle = computeDensityJob.ScheduleParallel(ranges.Length, 64, state.Dependency);
            jobHandle.Complete();

            // Subdivide high-density cells and merge low-density cells
            // Note: Actual octree subdivision would require storing OctreeSoA in a component
            // For now, this is a placeholder that computes densities
            // Full implementation would require integrating with OctreeSoA storage

            cellDensities.Dispose();
        }

            [BurstCompile]
            private struct ComputeCellDensityJob : IJobFor
            {
                [ReadOnly] public NativeArray<SpatialGridEntry> Entries;
                [ReadOnly] public NativeArray<SpatialGridCellRange> Ranges;
                [ReadOnly] public byte Level;
                [ReadOnly] public float CellSize;
                [WriteOnly] public NativeParallelHashMap<int, float>.ParallelWriter Densities;

            public void Execute(int index)
            {
                if (index >= Ranges.Length)
                {
                    return;
                }

                var range = Ranges[index];
                if (range.Count <= 0)
                {
                    return;
                }

                // Compute cell volume
                var cellVolume = CellSize * CellSize * CellSize;
                if (cellVolume <= 0f)
                {
                    return;
                }

                // Count entities in this cell
                var entityCount = 0;
                for (int i = range.StartIndex; i < range.StartIndex + range.Count && i < Entries.Length; i++)
                {
                    entityCount++;
                }

                // Compute density
                var density = entityCount / cellVolume;
                Densities.TryAdd(index, density);
            }
        }
    }
}

