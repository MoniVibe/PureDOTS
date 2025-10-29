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

namespace PureDOTS.Tests
{
    /// <summary>
    /// Covers high-volume scenarios for the spatial grid and registry metadata so regressions are visible in CI.
    /// </summary>
    public class SpatialRegistryPerformanceTests
    {
        const int kLargeEntityCount = 4096;
        const int kStressEntityCount = 16384;

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
    }
}
