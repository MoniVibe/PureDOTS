using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Integration tests for spatial query helpers.
    /// </summary>
    public class SpatialQueryTests : EcsTestFixture
    {
        [Test]
        public void GetEntitiesWithinRadius_FindsNearbyEntities()
        {
            var world = World;
            var entityManager = world.EntityManager;

            // Create spatial grid config
            var gridEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(gridEntity, new SpatialGridConfig
            {
                CellSize = 4f,
                WorldMin = new float3(-100f, -10f, -100f),
                WorldMax = new float3(100f, 10f, 100f),
                CellCounts = new int3(50, 5, 50),
                HashSeed = 0u,
                ProviderId = 0
            });

            var cellRanges = entityManager.AddBuffer<SpatialGridCellRange>(gridEntity);
            var gridEntries = entityManager.AddBuffer<SpatialGridEntry>(gridEntity);

            // Create test entities
            var entity1 = entityManager.CreateEntity();
            entityManager.AddComponentData(entity1, new LocalTransform
            {
                Position = new float3(0f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            entityManager.AddComponent<SpatialIndexedTag>(entity1);

            var entity2 = entityManager.CreateEntity();
            entityManager.AddComponentData(entity2, new LocalTransform
            {
                Position = new float3(5f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            entityManager.AddComponent<SpatialIndexedTag>(entity2);

            var entity3 = entityManager.CreateEntity();
            entityManager.AddComponentData(entity3, new LocalTransform
            {
                Position = new float3(50f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            entityManager.AddComponent<SpatialIndexedTag>(entity3);

            // Manually populate spatial grid for test
            cellRanges.ResizeUninitialized(1);
            cellRanges[0] = new SpatialGridCellRange { StartIndex = 0, Count = 3 };

            gridEntries.ResizeUninitialized(3);
            gridEntries[0] = new SpatialGridEntry { Entity = entity1, Position = new float3(0f, 0f, 0f), CellId = 0 };
            gridEntries[1] = new SpatialGridEntry { Entity = entity2, Position = new float3(5f, 0f, 0f), CellId = 0 };
            gridEntries[2] = new SpatialGridEntry { Entity = entity3, Position = new float3(50f, 0f, 0f), CellId = 0 };

            // Query entities within radius
            var config = entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            var results = new NativeList<Entity>(Allocator.Temp);
            var queryPosition = new float3(0f, 0f, 0f);
            SpatialQueryHelper.GetEntitiesWithinRadius(
                ref queryPosition,
                10f,
                config,
                cellRanges,
                gridEntries,
                ref results);

            // Assertions
            Assert.AreEqual(2, results.Length, "Should find 2 entities within radius");
            var managedResults = CopyToManaged(results);
            CollectionAssert.Contains(managedResults, entity1, "Should include entity1");
            CollectionAssert.Contains(managedResults, entity2, "Should include entity2");
            CollectionAssert.DoesNotContain(managedResults, entity3, "Should not include distant entity3");

            results.Dispose();
        }

        [Test]
        public void FindNearestEntity_ReturnsClosest()
        {
            var world = World;
            var entityManager = world.EntityManager;

            var gridEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(gridEntity, new SpatialGridConfig
            {
                CellSize = 4f,
                WorldMin = new float3(-100f, -10f, -100f),
                WorldMax = new float3(100f, 10f, 100f),
                CellCounts = new int3(50, 5, 50),
                HashSeed = 0u,
                ProviderId = 0
            });

            var cellRanges = entityManager.AddBuffer<SpatialGridCellRange>(gridEntity);
            var gridEntries = entityManager.AddBuffer<SpatialGridEntry>(gridEntity);

            // Create test entities at different distances
            var entity1 = entityManager.CreateEntity();
            entityManager.AddComponentData(entity1, new LocalTransform
            {
                Position = new float3(10f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            var entity2 = entityManager.CreateEntity();
            entityManager.AddComponentData(entity2, new LocalTransform
            {
                Position = new float3(5f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Populate grid
            cellRanges.ResizeUninitialized(1);
            cellRanges[0] = new SpatialGridCellRange { StartIndex = 0, Count = 2 };

            gridEntries.ResizeUninitialized(2);
            gridEntries[0] = new SpatialGridEntry { Entity = entity1, Position = new float3(10f, 0f, 0f), CellId = 0 };
            gridEntries[1] = new SpatialGridEntry { Entity = entity2, Position = new float3(5f, 0f, 0f), CellId = 0 };

            var config = entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            var queryPoint = new float3(0f, 0f, 0f);
            var found = SpatialQueryHelper.FindNearestEntity(
                ref queryPoint,
                config,
                cellRanges,
                gridEntries,
                out var nearest,
                out var distance);

            Assert.IsTrue(found, "Should find nearest entity");
            Assert.AreEqual(entity2, nearest, "Should return entity2 (closer)");
            Assert.LessOrEqual(distance, 5.1f, "Distance should be approximately 5");
        }

        [Test]
        public void GetCellEntities_ReturnsAllInCell()
        {
            var world = World;
            var entityManager = world.EntityManager;

            var gridEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(gridEntity, new SpatialGridConfig
            {
                CellSize = 4f,
                WorldMin = new float3(-100f, -10f, -100f),
                WorldMax = new float3(100f, 10f, 100f),
                CellCounts = new int3(50, 5, 50),
                HashSeed = 0u,
                ProviderId = 0
            });

            var cellRanges = entityManager.AddBuffer<SpatialGridCellRange>(gridEntity);
            var gridEntries = entityManager.AddBuffer<SpatialGridEntry>(gridEntity);

            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();

            // Populate cell 0 with 2 entities
            cellRanges.ResizeUninitialized(1);
            cellRanges[0] = new SpatialGridCellRange { StartIndex = 0, Count = 2 };

            gridEntries.ResizeUninitialized(2);
            gridEntries[0] = new SpatialGridEntry { Entity = entity1, Position = new float3(0f, 0f, 0f), CellId = 0 };
            gridEntries[1] = new SpatialGridEntry { Entity = entity2, Position = new float3(1f, 0f, 0f), CellId = 0 };

            var config = entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            var results = new NativeList<Entity>(Allocator.Temp);
            var cellCoords = new int3(0, 0, 0);
            SpatialQueryHelper.GetCellEntities(
                ref cellCoords,
                config,
                cellRanges,
                gridEntries,
                ref results);

            Assert.AreEqual(2, results.Length, "Should find 2 entities in cell");
            var managedResults = CopyToManaged(results);
            CollectionAssert.Contains(managedResults, entity1);
            CollectionAssert.Contains(managedResults, entity2);

            results.Dispose();
        }

        private static Entity[] CopyToManaged(NativeList<Entity> list)
        {
            var nativeArray = list.AsArray();
            var managed = new Entity[nativeArray.Length];
            nativeArray.CopyTo(managed);
            return managed;
        }
    }
}


