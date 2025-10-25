using System.Collections.Generic;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Spatial
{
    /// <summary>
    /// Rebuilds the spatial grid each frame for entities tagged with <see cref="SpatialIndexedTag" />.
    /// Maintains deterministic ordering and double-buffer semantics for consumer safety.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(global::PureDOTS.Systems.SpatialSystemGroup), OrderFirst = true)]
    public partial struct SpatialGridBuildSystem : ISystem
    {
        private EntityQuery _indexedQuery;
        private EntityQuery _dirtyQuery;
        private ComponentTypeHandle<LocalTransform> _transformHandle;
        private EntityTypeHandle _entityTypeHandle;
        private int _lastIndexedCount;
        private SpatialGridConfig _cachedConfig;
        private bool _hasCachedConfig;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _indexedQuery = SystemAPI.QueryBuilder()
                .WithAll<SpatialIndexedTag, LocalTransform>()
                .Build();

            _dirtyQuery = SystemAPI.QueryBuilder()
                .WithAll<SpatialIndexedTag, LocalTransform>()
                .Build();
            _dirtyQuery.SetChangedVersionFilter(ComponentType.ReadOnly<LocalTransform>());

            state.RequireForUpdate<SpatialGridConfig>();
            state.RequireForUpdate<SpatialGridState>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _transformHandle = state.GetComponentTypeHandle<LocalTransform>(true);
            _entityTypeHandle = state.GetEntityTypeHandle();
            _lastIndexedCount = 0;
            _cachedConfig = default;
            _hasCachedConfig = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformHandle = state.GetComponentTypeHandle<LocalTransform>(true);
            _entityTypeHandle = state.GetEntityTypeHandle();

            var config = SystemAPI.GetSingleton<SpatialGridConfig>();
            if (config.CellCount <= 0 || config.CellSize <= 0f)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record || timeState.IsPaused)
            {
                return;
            }

            var configChanged = !_hasCachedConfig || HasConfigChanged(config, _cachedConfig);
            var indexedCount = _indexedQuery.CalculateEntityCount();
            var countChanged = indexedCount != _lastIndexedCount;
            _dirtyQuery.SetChangedVersionFilter(ComponentType.ReadOnly<LocalTransform>());
            var transformChanged = !_dirtyQuery.IsEmpty;

            if (!configChanged && !countChanged && !transformChanged)
            {
                return;
            }

            _cachedConfig = config;
            _hasCachedConfig = true;
            _lastIndexedCount = indexedCount;

            var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
            var stateRW = SystemAPI.GetComponentRW<SpatialGridState>(gridEntity);
            var activeEntries = state.EntityManager.GetBuffer<SpatialGridEntry>(gridEntity);
            var activeRanges = state.EntityManager.GetBuffer<SpatialGridCellRange>(gridEntity);
            var stagingEntries = state.EntityManager.GetBuffer<SpatialGridStagingEntry>(gridEntity);
            var stagingRanges = state.EntityManager.GetBuffer<SpatialGridStagingCellRange>(gridEntity);

            stagingEntries.Clear();
            stagingRanges.Clear();

            var gatherList = new NativeList<SpatialGridStagingEntry>(Allocator.TempJob);
            gatherList.Capacity = math.max(gatherList.Capacity, indexedCount);

            var gatherJob = new GatherSpatialEntriesJob
            {
                TransformType = _transformHandle,
                EntityType = _entityTypeHandle,
                Config = config,
                Writer = gatherList.AsParallelWriter()
            };

            var gatherHandle = gatherJob.ScheduleParallel(_indexedQuery, state.Dependency);
            gatherHandle.Complete();

            gatherList.Sort(new SpatialGridEntryCellComparer());
            stagingEntries.EnsureCapacity(gatherList.Length);

            var tempRanges = new NativeArray<SpatialGridStagingCellRange>(config.CellCount, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            var currentCell = -1;
            var cellStart = 0;

            for (var i = 0; i < gatherList.Length; i++)
            {
                var entry = gatherList[i];
                if (entry.CellId != currentCell)
                {
                    if (currentCell >= 0)
                    {
                        tempRanges[currentCell] = new SpatialGridStagingCellRange
                        {
                            StartIndex = cellStart,
                            Count = i - cellStart
                        };
                    }

                    currentCell = entry.CellId;
                    cellStart = i;
                }

                stagingEntries.Add(entry);
            }

            if (currentCell >= 0)
            {
                tempRanges[currentCell] = new SpatialGridStagingCellRange
                {
                    StartIndex = cellStart,
                    Count = gatherList.Length - cellStart
                };
            }

            stagingRanges.ResizeUninitialized(config.CellCount);
            for (var i = 0; i < config.CellCount; i++)
            {
                stagingRanges[i] = tempRanges[i];
            }

            CopyStagingToActive(ref activeRanges, ref activeEntries, in stagingRanges, in stagingEntries);

            stateRW.ValueRW = new SpatialGridState
            {
                ActiveBufferIndex = (stateRW.ValueRO.ActiveBufferIndex + 1) & 1,
                TotalEntries = gatherList.Length,
                Version = stateRW.ValueRO.Version + 1,
                LastUpdateTick = timeState.Tick
            };

            if (SystemAPI.HasComponent<SpatialRegistryMetadata>(gridEntity))
            {
                var metadata = SystemAPI.GetComponentRW<SpatialRegistryMetadata>(gridEntity);
                var value = metadata.ValueRO;
                value.ResetHandles();

                AppendHandlesFromDirectory(ref state, ref value);

                metadata.ValueRW = value;
            }

            gatherList.Dispose();
            tempRanges.Dispose();
        }

        private static void AppendHandlesFromDirectory(ref SystemState state, ref SpatialRegistryMetadata metadata)
        {
            var directoryQuery = state.GetEntityQuery(ComponentType.ReadOnly<RegistryDirectory>());
            if (directoryQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var directoryEntity = directoryQuery.GetSingletonEntity();

            if (!state.EntityManager.HasBuffer<RegistryDirectoryEntry>(directoryEntity))
            {
                return;
            }

            var entries = state.EntityManager.GetBuffer<RegistryDirectoryEntry>(directoryEntity);
            for (var i = 0; i < entries.Length; i++)
            {
                metadata.SetHandle(entries[i].Handle);
            }
        }

        private static bool HasConfigChanged(in SpatialGridConfig current, in SpatialGridConfig cached)
        {
            var worldMinChanged = math.any(math.abs(current.WorldMin - cached.WorldMin) > 1e-3f);
            var worldMaxChanged = math.any(math.abs(current.WorldMax - cached.WorldMax) > 1e-3f);
            var cellSizeChanged = math.abs(current.CellSize - cached.CellSize) > 1e-4f;
            var cellCountsChanged = math.any(current.CellCounts != cached.CellCounts);
            var hashChanged = current.HashSeed != cached.HashSeed;
            var providerChanged = current.ProviderId != cached.ProviderId;
            return worldMinChanged || worldMaxChanged || cellSizeChanged || cellCountsChanged || hashChanged || providerChanged;
        }

        [BurstCompile]
        private struct GatherSpatialEntriesJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<LocalTransform> TransformType;
            [ReadOnly] public EntityTypeHandle EntityType;
            public SpatialGridConfig Config;
            public NativeList<SpatialGridStagingEntry>.ParallelWriter Writer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var transforms = chunk.GetNativeArray(ref TransformType);
                var entities = chunk.GetNativeArray(EntityType);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var position = transforms[i].Position;
                    SpatialHash.Quantize(position, Config, out var coords);
                    var cellId = SpatialHash.Flatten(in coords, in Config);

                    if ((uint)cellId >= (uint)Config.CellCount)
                    {
                        continue;
                    }

                    Writer.AddNoResize(new SpatialGridStagingEntry
                    {
                        Entity = entities[i],
                        Position = position,
                        CellId = cellId
                    });
                }
            }
        }

        [BurstCompile]
        internal static void CopyStagingToActive(
            ref DynamicBuffer<SpatialGridCellRange> activeRanges,
            ref DynamicBuffer<SpatialGridEntry> activeEntries,
            in DynamicBuffer<SpatialGridStagingCellRange> stagingRanges,
            in DynamicBuffer<SpatialGridStagingEntry> stagingEntries)
        {
            activeRanges.Clear();
            activeRanges.ResizeUninitialized(stagingRanges.Length);

            for (var i = 0; i < stagingRanges.Length; i++)
            {
                var range = stagingRanges[i];
                activeRanges[i] = new SpatialGridCellRange
                {
                    StartIndex = range.StartIndex,
                    Count = range.Count
                };
            }

            activeEntries.Clear();
            activeEntries.ResizeUninitialized(stagingEntries.Length);

            for (var i = 0; i < stagingEntries.Length; i++)
            {
                var entry = stagingEntries[i];
                activeEntries[i] = new SpatialGridEntry
                {
                    Entity = entry.Entity,
                    Position = entry.Position,
                    CellId = entry.CellId
                };
            }
        }

        internal struct SpatialGridEntryCellComparer : IComparer<SpatialGridStagingEntry>
        {
            public int Compare(SpatialGridStagingEntry x, SpatialGridStagingEntry y)
            {
                var cellCompare = x.CellId.CompareTo(y.CellId);
                if (cellCompare != 0)
                {
                    return cellCompare;
                }

                return x.Entity.Index.CompareTo(y.Entity.Index);
            }
        }
    }
}
