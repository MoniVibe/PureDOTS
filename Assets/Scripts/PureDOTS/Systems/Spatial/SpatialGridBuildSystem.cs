using System.Collections.Generic;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _indexedQuery = SystemAPI.QueryBuilder()
                .WithAll<SpatialIndexedTag, LocalTransform>()
                .Build();

            state.RequireForUpdate<SpatialGridConfig>();
            state.RequireForUpdate<SpatialGridState>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<SpatialGridConfig>();
            if (config.CellCount <= 0 || config.CellSize <= 0f)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
            var stateRW = SystemAPI.GetComponentRW<SpatialGridState>(gridEntity);
            var activeEntries = state.EntityManager.GetBuffer<SpatialGridEntry>(gridEntity);
            var activeRanges = state.EntityManager.GetBuffer<SpatialGridCellRange>(gridEntity);
            var stagingEntries = state.EntityManager.GetBuffer<SpatialGridStagingEntry>(gridEntity);
            var stagingRanges = state.EntityManager.GetBuffer<SpatialGridStagingCellRange>(gridEntity);

            stagingEntries.Clear();
            stagingRanges.Clear();

            var indexedCount = _indexedQuery.CalculateEntityCount();
            stagingEntries.EnsureCapacity(indexedCount);

            // Gather entities with deterministic ordering (entity index) before assigning cells.
            var gatherList = new NativeList<SpatialGridStagingEntry>(Allocator.Temp);
            gatherList.Capacity = math.max(gatherList.Capacity, indexedCount);
            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<SpatialIndexedTag>()
                         .WithEntityAccess())
            {
                var position = transform.ValueRO.Position;
                var coords = SpatialHash.Quantize(position, config);
                var cellId = SpatialHash.Flatten(coords, config);

                if ((uint)cellId >= (uint)config.CellCount)
                {
                    continue;
                }

                gatherList.Add(new SpatialGridStagingEntry
                {
                    Entity = entity,
                    Position = position,
                    CellId = cellId
                });
            }

            gatherList.Sort(new SpatialGridEntryCellComparer());

            var tempRanges = new NativeArray<SpatialGridStagingCellRange>(config.CellCount, Allocator.Temp, NativeArrayOptions.ClearMemory);

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

            // Ensure cells without occupants still write a zeroed range for deterministic size.
            stagingRanges.ResizeUninitialized(config.CellCount);
            for (var i = 0; i < config.CellCount; i++)
            {
                stagingRanges[i] = tempRanges[i];
            }

            CopyStagingToActive(ref activeRanges, ref activeEntries, stagingRanges, stagingEntries);

            stateRW.ValueRW = new SpatialGridState
            {
                ActiveBufferIndex = (stateRW.ValueRO.ActiveBufferIndex + 1) & 1,
                TotalEntries = gatherList.Length,
                Version = stateRW.ValueRO.Version + 1
            };

            gatherList.Dispose();
            tempRanges.Dispose();
        }

        [BurstCompile]
        internal static void CopyStagingToActive(
            ref DynamicBuffer<SpatialGridCellRange> activeRanges,
            ref DynamicBuffer<SpatialGridEntry> activeEntries,
            DynamicBuffer<SpatialGridStagingCellRange> stagingRanges,
            DynamicBuffer<SpatialGridStagingEntry> stagingEntries)
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
