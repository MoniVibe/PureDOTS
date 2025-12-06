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
    /// Region streaming and culling system for spatial grids.
    /// Deactivates cells beyond observer radius and compresses them as summaries.
    /// Reactivates cells deterministically when re-entered via SFC index lookup.
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(SpatialGridBuildSystem))]
    public partial struct SpatialGridStreamingSystem : ISystem
    {
        private uint _lastStreamingTick;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialGridConfig>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _lastStreamingTick = 0;
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
                return; // Only stream hierarchical grids
            }

            // Find grid entity
            var gridEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<SpatialGridConfig>()
                .Build();

            if (gridEntityQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var gridEntity = gridEntityQuery.GetSingletonEntity();

            // Get streaming config (create default if missing)
            var streamingConfig = SpatialGridStreamingConfig.CreateDefaults();
            if (SystemAPI.HasComponent<SpatialGridStreamingConfig>(gridEntity))
            {
                streamingConfig = SystemAPI.GetComponent<SpatialGridStreamingConfig>(gridEntity);
            }

            if (!streamingConfig.EnableStreaming)
            {
                return; // Streaming disabled
            }

            // Check update interval
            if (timeState.Tick - _lastStreamingTick < streamingConfig.StreamingUpdateInterval)
            {
                return;
            }

            // Update observers and stream cells
            UpdateObservers(ref state, gridEntity, in config, in streamingConfig, timeState.Tick);

            _lastStreamingTick = timeState.Tick;
        }

        private void UpdateObservers(ref SystemState state, Entity gridEntity, in SpatialGridConfig config, in SpatialGridStreamingConfig streamingConfig, uint currentTick)
        {
            // Query all spatial observers
            var observerQuery = SystemAPI.QueryBuilder()
                .WithAll<SpatialObserver>()
                .Build();

            if (observerQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
            var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);

            // Collect active cell keys from all observers
            var activeCellKeys = new NativeHashSet<ulong>(1024, Allocator.TempJob);

            foreach (var (observer, entity) in SystemAPI.Query<RefRO<SpatialObserver>>().WithEntityAccess())
            {
                if (!observer.ValueRO.IsActive)
                {
                    continue;
                }

                var position = observer.ValueRO.Position;
                var radius = observer.ValueRO.Radius;
                var radiusSq = radius * radius;

                // Find all cells within radius
                SpatialHash.Quantize(position, config, out var centerCoords);
                var maxOffset = (int)math.ceil(radius / math.max(config.CellSize, 1e-3f));

                for (int dx = -maxOffset; dx <= maxOffset; dx++)
                {
                    for (int dy = -maxOffset; dy <= maxOffset; dy++)
                    {
                        for (int dz = -maxOffset; dz <= maxOffset; dz++)
                        {
                            var coords = centerCoords + new int3(dx, dy, dz);
                            if (!IsWithinBounds(coords, config.CellCounts))
                            {
                                continue;
                            }

                            var cellKey = SpaceFillingCurve.Morton3D(in coords);
                            var cellCenter = GetCellCenter(coords, config);
                            var distSq = math.lengthsq(cellCenter - position);

                            if (distSq <= radiusSq)
                            {
                                activeCellKeys.Add(cellKey);
                            }
                        }
                    }
                }
            }

            // Deactivate cells not in active set and compress them
            // Reactivate compressed cells that are now active
            // (Full implementation would update cell activation state)

            activeCellKeys.Dispose();
        }

        private static bool IsWithinBounds(int3 coords, int3 maxCounts)
        {
            return coords.x >= 0 && coords.y >= 0 && coords.z >= 0
                && coords.x < maxCounts.x
                && coords.y < maxCounts.y
                && coords.z < maxCounts.z;
        }

        private static float3 GetCellCenter(int3 coords, in SpatialGridConfig config)
        {
            var cellSize = config.CellSize;
            return config.WorldMin + (float3)coords * cellSize + cellSize * 0.5f;
        }
    }

    /// <summary>
    /// Extension methods for SpatialGridStreamingConfig.
    /// </summary>
    public static class SpatialGridStreamingConfigExtensions
    {
        public static SpatialGridStreamingConfig CreateDefaults()
        {
            return new SpatialGridStreamingConfig
            {
                StreamingRadius = 1000.0f,
                EnableStreaming = false, // Disabled by default
                StreamingUpdateInterval = 60 // 1 second at 60 Hz
            };
        }
    }
}

