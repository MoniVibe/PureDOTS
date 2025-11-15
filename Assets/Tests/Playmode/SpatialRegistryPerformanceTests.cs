using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using PureDOTS.Systems.Spatial;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Covers high-volume scenarios for the spatial grid and registry metadata so regressions are visible in CI.
    /// </summary>
    public class SpatialRegistryPerformanceTests
    {
        const int kLargeEntityCount = 4096;
        const int kStressEntityCount = 16384;
        static readonly Regex s_SpatialLogRegex = new Regex(
            @"\[PureDOTS\]\[Spatial\]\s+Cells=(?<cells>\d+)\s+Entries=(?<entries>\d+)\s+Version=(?<version>\d+)\s+Tick=(?<tick>\d+)\s+Avg/Cell=(?<avg>-?[0-9.]+)\s+Buffer=(?<buffer>\d+)\s+Strategy=(?<strategy>\w+)\s+Dirty\(\+\/(?<dirtyAdd>\d+),~\/(?<dirtyUpdate>\d+),-\/(?<dirtyRemove>\d+)\)=(?<dirtyTotal>\d+)\s+RebuildMs=(?<rebuildMs>-?[0-9.]+)",
            RegexOptions.Compiled);

        [Test]
        public void SpatialGridBuildSystem_IndexesLargeEntitySetAndCachesHandles()
        {
            using var world = CreateWorldWithSpatialEntries(kLargeEntityCount, out var gridEntity, out var config);
            var entityManager = world.EntityManager;

            var gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.AreEqual(kLargeEntityCount, gridState.TotalEntries);
            Assert.Greater(gridState.Version, 0u);
            Assert.GreaterOrEqual(gridState.LastUpdateTick, 1u);

            var metadata = entityManager.GetComponentData<SpatialRegistryMetadata>(gridEntity);
            Assert.Greater(metadata.Version, 0u, "Spatial registry metadata should increment version after rebuild.");
            Assert.Greater(metadata.Handles.Length, 0, "Spatial registry metadata should cache directory handles.");

            var ranges = entityManager.GetBuffer<SpatialGridCellRange>(gridEntity);
            var entries = entityManager.GetBuffer<SpatialGridEntry>(gridEntity);
            Assert.AreEqual(config.CellCount, ranges.Length, "All cell ranges should be initialised.");
            Assert.AreEqual(gridState.TotalEntries, entries.Length, "Flattened entry buffer should mirror entity count.");
        }

        [Test]
        public void SpatialQueryHelper_FindKNearest_ReturnsSortedResultsUnderLoad()
        {
            using var world = CreateWorldWithSpatialEntries(kLargeEntityCount, out var gridEntity, out var config);
            var entityManager = world.EntityManager;

            var ranges = entityManager.GetBuffer<SpatialGridCellRange>(gridEntity);
            var entries = entityManager.GetBuffer<SpatialGridEntry>(gridEntity);

            var results = new NativeList<KNearestResult>(Allocator.Temp);
            try
            {
                var queryOrigin = new float3(config.WorldMin.x + config.WorldExtent.x * 0.5f, 0f, config.WorldMin.z + config.WorldExtent.z * 0.5f);
                const int kNeighbours = 16;

                SpatialQueryHelper.FindKNearest(queryOrigin, kNeighbours, config, ranges, entries, ref results);

                Assert.AreEqual(kNeighbours, results.Length, "Query should return the requested number of neighbours.");

                for (var i = 1; i < results.Length; i++)
                {
                    Assert.LessOrEqual(results[i - 1].DistanceSq, results[i].DistanceSq, "Results must be sorted by distance.");
                }
            }
            finally
            {
                results.Dispose();
            }
        }

        static World CreateWorldWithSpatialEntries(int entityCount, out Entity gridEntity, out SpatialGridConfig config)
        {
            var world = new World("SpatialPerformanceWorld");
            var entityManager = world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            gridEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()).GetSingletonEntity();
            config = entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            config.WorldMin = float3.zero;
            config.WorldMax = new float3(64f, 16f, 64f);
            config.CellCounts = new int3(64, 1, 64);
            config.CellSize = 1f;
            entityManager.SetComponentData(gridEntity, config);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            if (timeState.Tick == 0u)
            {
                timeState.Tick = 1u;
            }
            entityManager.SetComponentData(timeEntity, timeState);

            for (var i = 0; i < entityCount; i++)
            {
                var position = new float3(i % config.CellCounts.x, 0f, i / config.CellCounts.x);
                var entity = entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
                entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
            }

            RunSystem<RegistryDirectorySystem>(world);
            RunSystem<SpatialGridBuildSystem>(world);

            // Refresh config in case systems adjusted derived data.
            config = entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            return world;
        }

        [Test]
        public void SpatialGridBuildSystem_PartialRebuildScalesWithDirtyCount()
        {
            using var world = CreateWorldWithSpatialEntries(kLargeEntityCount, out var gridEntity, out var config);
            var entityManager = world.EntityManager;

            var gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            var versionAfterInitial = gridState.Version;
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy);

            // Query all indexed entities to move a subset
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialIndexedTag>(), ComponentType.ReadWrite<LocalTransform>());
            using var allEntities = query.ToEntityArray(Allocator.Temp);

            // Move 10% of entities (well below 35% threshold)
            var moveCount = math.max(1, allEntities.Length / 10);
            for (int i = 0; i < moveCount; i++)
            {
                var entity = allEntities[i];
                var transform = entityManager.GetComponentData<LocalTransform>(entity);
                transform.Position += new float3(0.1f, 0f, 0.1f);
                entityManager.SetComponentData(entity, transform);
            }

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick += 1;
            entityManager.SetComponentData(timeEntity, timeState);

            RunSystem<SpatialGridDirtyTrackingSystem>(world);
            RunSystem<SpatialGridBuildSystem>(world);

            gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy, "Low dirty ratio should use partial rebuild");
            Assert.AreEqual(moveCount, gridState.DirtyUpdateCount);
            Assert.Greater(gridState.Version, versionAfterInitial, "Version should increment after rebuild");
            Assert.Greater(gridState.LastRebuildMilliseconds, 0f, "Rebuild time should be recorded");
        }

        [Test]
        public void SpatialGridBuildSystem_FullRebuildTriggersOnHighDirtyRatio()
        {
            using var world = CreateWorldWithSpatialEntries(kLargeEntityCount, out var gridEntity, out var config);
            var entityManager = world.EntityManager;

            var gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            var versionAfterInitial = gridState.Version;

            // Move 40% of entities (above 35% threshold)
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialIndexedTag>(), ComponentType.ReadWrite<LocalTransform>());
            using var allEntities = query.ToEntityArray(Allocator.Temp);

            var moveCount = math.max(1, (allEntities.Length * 40) / 100);
            for (int i = 0; i < moveCount; i++)
            {
                var entity = allEntities[i];
                var transform = entityManager.GetComponentData<LocalTransform>(entity);
                transform.Position += new float3(0.5f, 0f, 0.5f);
                entityManager.SetComponentData(entity, transform);
            }

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick += 1;
            entityManager.SetComponentData(timeEntity, timeState);

            RunSystem<SpatialGridDirtyTrackingSystem>(world);
            RunSystem<SpatialGridBuildSystem>(world);

            gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy, "High dirty ratio should trigger full rebuild");
            Assert.Greater(gridState.Version, versionAfterInitial);
            Assert.Greater(gridState.LastRebuildMilliseconds, 0f);
        }

        [Test]
        public void SpatialGridBuildSystem_StressTestWithChurn()
        {
            using var world = CreateWorldWithSpatialEntries(kStressEntityCount, out var gridEntity, out var config);
            var entityManager = world.EntityManager;

            var gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            var initialVersion = gridState.Version;

            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialIndexedTag>(), ComponentType.ReadWrite<LocalTransform>());
            using var allEntities = query.ToEntityArray(Allocator.Temp);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();

            // Simulate 10 ticks of moderate churn (5% moves per tick)
            for (int tick = 0; tick < 10; tick++)
            {
                var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
                timeState.Tick += 1;
                entityManager.SetComponentData(timeEntity, timeState);

                var moveCount = math.max(1, allEntities.Length / 20);
                for (int i = 0; i < moveCount; i++)
                {
                    var entity = allEntities[(i + tick * moveCount) % allEntities.Length];
                    var transform = entityManager.GetComponentData<LocalTransform>(entity);
                    transform.Position += new float3(0.05f, 0f, 0.05f);
                    entityManager.SetComponentData(entity, transform);
                }

                RunSystem<SpatialGridDirtyTrackingSystem>(world);
                RunSystem<SpatialGridBuildSystem>(world);
            }

            gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.Greater(gridState.Version, initialVersion);
            Assert.AreEqual(kStressEntityCount, gridState.TotalEntries);
            Assert.Greater(gridState.LastRebuildMilliseconds, 0f);

            // Most ticks should use partial rebuilds under moderate churn
            var lookup = entityManager.GetBuffer<SpatialGridEntryLookup>(gridEntity);
            Assert.AreEqual(gridState.TotalEntries, lookup.Length, "Lookup buffer should stay synchronized");
        }

        [Test]
        public void SpatialInstrumentationSystem_EmitsMetricsMatchingGridState()
        {
            using var world = CreateWorldWithSpatialEntries(kLargeEntityCount, out var gridEntity, out var config);
            var entityManager = world.EntityManager;

            EnableConsoleInstrumentation(entityManager, gridEntity);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var expectedTick = entityManager.GetComponentData<TimeState>(timeEntity).Tick;

            var instrumentationLogs = new List<string>();
            Application.LogCallback handler = (condition, stackTrace, type) =>
            {
                if (type == LogType.Log && condition.StartsWith("[PureDOTS][Spatial]"))
                {
                    instrumentationLogs.Add(condition);
                }
            };

            try
            {
                Application.logMessageReceived += handler;
                RunSystem<SpatialInstrumentationSystem>(world);
            }
            finally
            {
                Application.logMessageReceived -= handler;
            }

            Assert.AreEqual(1, instrumentationLogs.Count, "Spatial instrumentation should emit one log entry.");
            var logLine = instrumentationLogs[0];
            var match = s_SpatialLogRegex.Match(logLine);
            Assert.IsTrue(match.Success, "Spatial instrumentation log format changed: " + logLine);

            var gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);

            Assert.AreEqual(config.CellCount, int.Parse(match.Groups["cells"].Value));
            Assert.AreEqual(gridState.TotalEntries, int.Parse(match.Groups["entries"].Value));
            Assert.AreEqual(gridState.Version, uint.Parse(match.Groups["version"].Value));
            Assert.AreEqual(gridState.LastUpdateTick, uint.Parse(match.Groups["tick"].Value));
            Assert.AreEqual(gridState.ActiveBufferIndex, int.Parse(match.Groups["buffer"].Value));
            Assert.AreEqual(gridState.LastStrategy.ToString(), match.Groups["strategy"].Value);
            Assert.AreEqual(gridState.DirtyAddCount, int.Parse(match.Groups["dirtyAdd"].Value));
            Assert.AreEqual(gridState.DirtyUpdateCount, int.Parse(match.Groups["dirtyUpdate"].Value));
            Assert.AreEqual(gridState.DirtyRemoveCount, int.Parse(match.Groups["dirtyRemove"].Value));

            var dirtyTotal = gridState.DirtyAddCount + gridState.DirtyUpdateCount + gridState.DirtyRemoveCount;
            Assert.AreEqual(dirtyTotal, int.Parse(match.Groups["dirtyTotal"].Value));

            var rebuildMs = float.Parse(match.Groups["rebuildMs"].Value, CultureInfo.InvariantCulture);
            Assert.Greater(rebuildMs, 0f, "Instrumentation must surface rebuild timings.");

            var instrumentation = entityManager.GetComponentData<SpatialConsoleInstrumentation>(gridEntity);
            Assert.AreEqual(gridState.Version, instrumentation.LastLoggedVersion, "Instrumentation should cache the logged spatial version.");
            Assert.AreEqual(expectedTick, instrumentation.LastLoggedTick, "Instrumentation should record the tick used for logging.");
        }

        [Test]
        public void SpatialGridBuildSystem_RecordsRebuildMetrics()
        {
            using var world = CreateWorldWithSpatialEntries(kLargeEntityCount, out var gridEntity, out var config);
            var entityManager = world.EntityManager;

            var gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.Greater(gridState.LastRebuildMilliseconds, 0f, "Initial rebuild should record timing");
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy);

            // Make a small change to trigger partial rebuild
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialIndexedTag>(), ComponentType.ReadWrite<LocalTransform>());
            using var allEntities = query.ToEntityArray(Allocator.Temp);

            var entity = allEntities[0];
            var transform = entityManager.GetComponentData<LocalTransform>(entity);
            transform.Position += new float3(0.5f, 0f, 0.5f);
            entityManager.SetComponentData(entity, transform);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick += 1;
            entityManager.SetComponentData(timeEntity, timeState);

            RunSystem<SpatialGridDirtyTrackingSystem>(world);
            RunSystem<SpatialGridBuildSystem>(world);

            gridState = entityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy);
            Assert.Greater(gridState.LastRebuildMilliseconds, 0f, "Partial rebuild should record timing");
            Assert.AreEqual(1, gridState.DirtyUpdateCount);
        }

        static void RunSystem<T>(World world) where T : unmanaged, ISystem
        {
            var handle = world.GetOrCreateSystem<T>();
            ref var system = ref world.Unmanaged.GetUnsafeSystemRef<T>(handle);
            ref var state = ref world.Unmanaged.ResolveSystemStateRef(handle);
            system.OnUpdate(ref state);
        }

        static void EnableConsoleInstrumentation(EntityManager entityManager, Entity gridEntity, uint minTickDelta = 0u)
        {
            var instrumentation = new SpatialConsoleInstrumentation
            {
                MinTickDelta = minTickDelta,
                LastLoggedTick = 0u,
                LastLoggedVersion = 0u,
                Flags = SpatialConsoleInstrumentation.FlagLogOnlyOnChange
            };

            if (entityManager.HasComponent<SpatialConsoleInstrumentation>(gridEntity))
            {
                entityManager.SetComponentData(gridEntity, instrumentation);
            }
            else
            {
                entityManager.AddComponentData(gridEntity, instrumentation);
            }
        }
    }
}
