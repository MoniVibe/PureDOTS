using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Tests;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Tests for spatial grid snapshot capture and restore functionality.
    /// </summary>
    public class SpatialGridSnapshotTests : DeterministicRewindTestFixture
    {
        private Entity _gridEntity;

        [SetUp]
        public new void SetUp()
        {
            base.SetUp();

            // Create spatial grid config and state
            _gridEntity = EntityManager.CreateEntity(typeof(SpatialGridConfig), typeof(SpatialGridState));
            EntityManager.SetComponentData(_gridEntity, new SpatialGridConfig
            {
                CellSize = 5f,
                WorldMin = new float3(-50f, -50f, -50f),
                WorldMax = new float3(50f, 50f, 50f),
                CellCounts = new int3(20, 20, 20),
                HashSeed = 12345,
                ProviderId = 0
            });
            EntityManager.SetComponentData(_gridEntity, new SpatialGridState
            {
                ActiveBufferIndex = 0,
                TotalEntries = 0,
                Version = 0,
                LastUpdateTick = 0,
                LastDirtyTick = 0,
                DirtyVersion = 0,
                DirtyAddCount = 0,
                DirtyUpdateCount = 0,
                DirtyRemoveCount = 0,
                LastRebuildMilliseconds = 0f,
                LastStrategy = SpatialGridRebuildStrategy.None
            });

            // Add buffers
            EntityManager.AddBuffer<SpatialGridEntry>(_gridEntity);
            EntityManager.AddBuffer<SpatialGridCellRange>(_gridEntity);
            EntityManager.AddBuffer<SpatialGridDirtyOp>(_gridEntity);
        }

        [Test]
        public void SpatialGridSnapshot_CapturesState()
        {
            // Modify grid state
            var gridState = EntityManager.GetComponentData<SpatialGridState>(_gridEntity);
            gridState.Version = 5;
            gridState.TotalEntries = 10;
            gridState.LastUpdateTick = 100;
            EntityManager.SetComponentData(_gridEntity, gridState);

            // Capture snapshot
            var timeState = EntityManager.GetSingleton<TimeState>();
            var snapshot = SpatialGridSnapshot.FromState(gridState, timeState.Tick);

            Assert.AreEqual(timeState.Tick, snapshot.CapturedTick);
            Assert.AreEqual(5u, snapshot.Version);
            Assert.AreEqual(10, snapshot.TotalEntries);
            Assert.AreEqual(100u, snapshot.LastUpdateTick);
        }

        [Test]
        public void SpatialGridSnapshot_MatchesCurrentState()
        {
            // Set initial state
            var gridState = EntityManager.GetComponentData<SpatialGridState>(_gridEntity);
            gridState.Version = 3;
            gridState.TotalEntries = 5;
            EntityManager.SetComponentData(_gridEntity, gridState);

            // Capture snapshot
            var timeState = EntityManager.GetSingleton<TimeState>();
            var snapshot = SpatialGridSnapshot.FromState(gridState, timeState.Tick);

            // Verify snapshot matches
            Assert.IsTrue(snapshot.Matches(gridState, out var difference));
            Assert.IsEmpty(difference);

            // Modify state
            gridState.Version = 4;
            gridState.TotalEntries = 6;
            EntityManager.SetComponentData(_gridEntity, gridState);

            // Verify snapshot no longer matches
            Assert.IsFalse(snapshot.Matches(gridState, out difference));
            Assert.IsNotEmpty(difference);
        }

        [Test]
        public void SpatialGridBufferSnapshot_CapturesBuffers()
        {
            // Populate buffers
            var entries = EntityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            entries.Add(new SpatialGridEntry { Entity = Entity.Null, Position = new float3(1, 2, 3), CellId = 1 });
            entries.Add(new SpatialGridEntry { Entity = Entity.Null, Position = new float3(4, 5, 6), CellId = 2 });

            var cellRanges = EntityManager.GetBuffer<SpatialGridCellRange>(_gridEntity);
            cellRanges.Add(new SpatialGridCellRange { StartIndex = 0, Count = 2 });

            var dirtyOps = EntityManager.GetBuffer<SpatialGridDirtyOp>(_gridEntity);
            dirtyOps.Add(new SpatialGridDirtyOp { Entity = Entity.Null, Operation = SpatialGridDirtyOpType.Add });

            // Capture snapshot
            var timeState = EntityManager.GetSingleton<TimeState>();
            var snapshot = SpatialGridBufferSnapshot.Capture(
                entries,
                cellRanges,
                dirtyOps,
                timeState.Tick,
                Allocator.Temp);
            try
            {
                Assert.AreEqual(timeState.Tick, snapshot.CapturedTick);
                Assert.AreEqual(2, snapshot.Entries.Length);
                Assert.AreEqual(1, snapshot.CellRanges.Length);
                Assert.AreEqual(1, snapshot.DirtyOps.Length);
            }
            finally
            {
                snapshot.Dispose();
            }
        }

        [Test]
        public void SpatialGridBufferSnapshot_MatchesCurrentBuffers()
        {
            // Populate buffers
            var entries = EntityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            entries.Add(new SpatialGridEntry { Entity = Entity.Null, Position = new float3(1, 2, 3), CellId = 1 });

            var cellRanges = EntityManager.GetBuffer<SpatialGridCellRange>(_gridEntity);
            cellRanges.Add(new SpatialGridCellRange { StartIndex = 0, Count = 1 });

            var dirtyOps = EntityManager.GetBuffer<SpatialGridDirtyOp>(_gridEntity);

            // Capture snapshot
            var timeState = EntityManager.GetSingleton<TimeState>();
            var snapshot = SpatialGridBufferSnapshot.Capture(
                entries,
                cellRanges,
                dirtyOps,
                timeState.Tick,
                Allocator.Temp);

            try
            {
                // Verify snapshot matches
                Assert.IsTrue(snapshot.Matches(entries, cellRanges, dirtyOps, out var difference));
                Assert.IsEmpty(difference);

                // Modify buffers
                entries.Add(new SpatialGridEntry { Entity = Entity.Null, Position = new float3(4, 5, 6), CellId = 2 });

                // Verify snapshot no longer matches
                Assert.IsFalse(snapshot.Matches(entries, cellRanges, dirtyOps, out difference));
                Assert.IsNotEmpty(difference);
            }
            finally
            {
                snapshot.Dispose();
            }
        }

        [Test]
        public void SpatialGridSnapshot_RestoresUnderRewind()
        {
            // Set initial state
            var gridState = EntityManager.GetComponentData<SpatialGridState>(_gridEntity);
            gridState.Version = 10;
            gridState.TotalEntries = 20;
            gridState.LastUpdateTick = 50;
            EntityManager.SetComponentData(_gridEntity, gridState);

            // Capture snapshot at tick 50
            var snapshot = SpatialGridSnapshot.FromState(gridState, 50);
            EntityManager.AddComponentData(_gridEntity, snapshot);

            // Modify state (simulating continued simulation)
            gridState.Version = 15;
            gridState.TotalEntries = 25;
            gridState.LastUpdateTick = 100;
            EntityManager.SetComponentData(_gridEntity, gridState);

            // Rewind to tick 50 and restore snapshot
            var rewindState = EntityManager.GetSingleton<RewindState>();
            rewindState.Mode = RewindMode.Playback;
            rewindState.TargetTick = 50;
            EntityManager.SetSingleton(rewindState);

            // Restore from snapshot
            var restoredSnapshot = EntityManager.GetComponentData<SpatialGridSnapshot>(_gridEntity);
            gridState.Version = restoredSnapshot.Version;
            gridState.TotalEntries = restoredSnapshot.TotalEntries;
            gridState.LastUpdateTick = restoredSnapshot.LastUpdateTick;
            EntityManager.SetComponentData(_gridEntity, gridState);

            // Verify restored state matches snapshot
            Assert.IsTrue(restoredSnapshot.Matches(gridState, out var difference));
            Assert.IsEmpty(difference);
        }
    }
}


