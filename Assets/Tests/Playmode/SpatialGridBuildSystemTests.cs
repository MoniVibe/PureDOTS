using NUnit.Framework;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using PureDOTS.Systems.Spatial;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Tests
{
    public class SpatialGridBuildSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private SpatialGridDirtyTrackingSystem _dirtySystem;
        private SpatialGridBuildSystem _buildSystem;
        private SystemHandle _dirtyHandle;
        private SystemHandle _buildHandle;
        private Entity _gridEntity;
        private Entity _timeEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("SpatialGridBuildSystemTests");
            _entityManager = _world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            _gridEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>(), ComponentType.ReadOnly<SpatialGridState>()).GetSingletonEntity();
            var config = _entityManager.GetComponentData<SpatialGridConfig>(_gridEntity);
            config.WorldMin = float3.zero;
            config.WorldMax = new float3(4f, 1f, 4f);
            config.CellSize = 1f;
            config.CellCounts = new int3(4, 1, 4);
            _entityManager.SetComponentData(_gridEntity, config);

            var state = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            state.ActiveBufferIndex = 0;
            state.TotalEntries = 0;
            state.Version = 0;
            state.LastUpdateTick = 0;
            state.LastDirtyTick = 0;
            state.DirtyVersion = 0;
            state.DirtyAddCount = 0;
            state.DirtyUpdateCount = 0;
            state.DirtyRemoveCount = 0;
            state.LastRebuildMilliseconds = 0f;
            state.LastStrategy = SpatialGridRebuildStrategy.None;
            _entityManager.SetComponentData(_gridEntity, state);

            _timeEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();

            _dirtySystem = new SpatialGridDirtyTrackingSystem();
            _dirtyHandle = _world.CreateSystem<SpatialGridDirtyTrackingSystem>();
            ref var dirtyState = ref _world.Unmanaged.ResolveSystemStateRef(_dirtyHandle);
            _dirtySystem.OnCreate(ref dirtyState);

            _buildSystem = new SpatialGridBuildSystem();
            _buildHandle = _world.CreateSystem<SpatialGridBuildSystem>();
            ref var buildState = ref _world.Unmanaged.ResolveSystemStateRef(_buildHandle);
            _buildSystem.OnCreate(ref buildState);
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void SpatialGridBuildSystem_UsesPartialRebuildForDirtyChanges()
        {
            var indexedEntity = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
            _entityManager.SetComponentData(indexedEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            StepSystems();

            var activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(1, activeEntries.Length);
            Assert.AreEqual(indexedEntity, activeEntries[0].Entity);

            var gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy);
            Assert.AreEqual(1, gridState.TotalEntries);
            Assert.AreEqual(1, gridState.DirtyAddCount);

            // Move entity to a new cell to trigger a partial rebuild.
            var movedPosition = new float3(2f, 0f, 0f);
            _entityManager.SetComponentData(indexedEntity, LocalTransform.FromPositionRotationScale(movedPosition, quaternion.identity, 1f));

            StepSystems();

            activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(1, activeEntries.Length);
            Assert.AreEqual(indexedEntity, activeEntries[0].Entity);
            Assert.AreEqual(movedPosition, activeEntries[0].Position);

            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy);
            Assert.AreEqual(1, gridState.TotalEntries);
            Assert.AreEqual(1, gridState.DirtyUpdateCount);
            Assert.AreEqual(0, gridState.DirtyRemoveCount);

            var config = _entityManager.GetComponentData<SpatialGridConfig>(_gridEntity);
            SpatialHash.Quantize(movedPosition, config, out var coords);
            var expectedCell = SpatialHash.Flatten(in coords, in config);
            Assert.AreEqual(expectedCell, activeEntries[0].CellId);

            // Remove entity to ensure partial rebuild handles removals.
            _entityManager.RemoveComponent<SpatialIndexedTag>(indexedEntity);

            StepSystems();

            activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(0, activeEntries.Length);

            var lookup = _entityManager.GetBuffer<SpatialGridEntryLookup>(_gridEntity);
            Assert.AreEqual(0, lookup.Length);

            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy);
            Assert.AreEqual(0, gridState.TotalEntries);
            Assert.AreEqual(1, gridState.DirtyRemoveCount);
        }

        [Test]
        public void SpatialGridBuildSystem_HandlesCrossCellMoves()
        {
            var entities = new Entity[3];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
                _entityManager.SetComponentData(entities[i], LocalTransform.FromPositionRotationScale(new float3(i, 0, 0), quaternion.identity, 1f));
            }

            StepSystems();

            var gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(3, gridState.TotalEntries);
            Assert.AreEqual(3, gridState.DirtyAddCount);
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy);

            var activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(3, activeEntries.Length);

            // Move all entities to new cells
            _entityManager.SetComponentData(entities[0], LocalTransform.FromPositionRotationScale(new float3(3f, 0f, 0f), quaternion.identity, 1f));
            _entityManager.SetComponentData(entities[1], LocalTransform.FromPositionRotationScale(new float3(0f, 0f, 3f), quaternion.identity, 1f));
            _entityManager.SetComponentData(entities[2], LocalTransform.FromPositionRotationScale(new float3(3f, 0f, 3f), quaternion.identity, 1f));

            StepSystems();

            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(3, gridState.TotalEntries);
            Assert.AreEqual(3, gridState.DirtyUpdateCount);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy);

            activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(3, activeEntries.Length);

            // Verify all entities are in the correct cells
            var config = _entityManager.GetComponentData<SpatialGridConfig>(_gridEntity);
            for (int i = 0; i < activeEntries.Length; i++)
            {
                var entry = activeEntries[i];
                var transform = _entityManager.GetComponentData<LocalTransform>(entry.Entity);
                SpatialHash.Quantize(transform.Position, config, out var coords);
                var expectedCell = SpatialHash.Flatten(in coords, in config);
                Assert.AreEqual(expectedCell, entry.CellId, $"Entity {i} should be in cell {expectedCell}");
                Assert.AreEqual(transform.Position, entry.Position, $"Entity {i} position should match");
            }

            // Verify lookup buffer is synchronized
            var lookup = _entityManager.GetBuffer<SpatialGridEntryLookup>(_gridEntity);
            Assert.AreEqual(3, lookup.Length);
            for (int i = 0; i < lookup.Length; i++)
            {
                Assert.AreEqual(activeEntries[i].Entity, lookup[i].Entity);
                Assert.AreEqual(i, lookup[i].EntryIndex);
                Assert.AreEqual(activeEntries[i].CellId, lookup[i].CellId);
            }
        }

        [Test]
        public void SpatialGridBuildSystem_HandlesMultiEntityChurn()
        {
            var persistent = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
            _entityManager.SetComponentData(persistent, LocalTransform.FromPositionRotationScale(new float3(0.5f, 0f, 0.5f), quaternion.identity, 1f));

            StepSystems();

            var gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(1, gridState.TotalEntries);

            // Add 5 entities
            var added = new Entity[5];
            for (int i = 0; i < added.Length; i++)
            {
                added[i] = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
                _entityManager.SetComponentData(added[i], LocalTransform.FromPositionRotationScale(new float3(i * 0.5f, 0f, i * 0.5f), quaternion.identity, 1f));
            }

            StepSystems();

            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(6, gridState.TotalEntries);
            Assert.AreEqual(5, gridState.DirtyAddCount);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy);

            // Remove 3 entities
            for (int i = 0; i < 3; i++)
            {
                _entityManager.RemoveComponent<SpatialIndexedTag>(added[i]);
            }

            StepSystems();

            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(3, gridState.TotalEntries);
            Assert.AreEqual(3, gridState.DirtyRemoveCount);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy);

            // Move remaining entities
            _entityManager.SetComponentData(persistent, LocalTransform.FromPositionRotationScale(new float3(3f, 0f, 3f), quaternion.identity, 1f));
            _entityManager.SetComponentData(added[3], LocalTransform.FromPositionRotationScale(new float3(2f, 0f, 2f), quaternion.identity, 1f));

            StepSystems();

            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(3, gridState.TotalEntries);
            Assert.AreEqual(2, gridState.DirtyUpdateCount);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, gridState.LastStrategy);

            var activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(3, activeEntries.Length);
        }

        [Test]
        public void SpatialGridBuildSystem_FallsBackToFullRebuildOnHighDirtyRatio()
        {
            // Create initial set of entities
            var entities = new Entity[10];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
                _entityManager.SetComponentData(entities[i], LocalTransform.FromPositionRotationScale(new float3(i * 0.3f, 0f, 0f), quaternion.identity, 1f));
            }

            StepSystems();

            var gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(10, gridState.TotalEntries);
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy);

            // Move 4 entities (40% dirty ratio should trigger full rebuild)
            for (int i = 0; i < 4; i++)
            {
                _entityManager.SetComponentData(entities[i], LocalTransform.FromPositionRotationScale(new float3(i * 0.3f + 2f, 0f, i * 0.3f + 2f), quaternion.identity, 1f));
            }

            StepSystems();

            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(10, gridState.TotalEntries);
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy, "High dirty ratio should trigger full rebuild");
        }

        [Test]
        public void SpatialGridBuildSystem_SupportsUniformProvider()
        {
            var config = _entityManager.GetComponentData<SpatialGridConfig>(_gridEntity);
            config.ProviderId = SpatialGridProviderIds.Uniform;
            _entityManager.SetComponentData(_gridEntity, config);

            var entity = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            StepSystems();

            var gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, gridState.LastStrategy);
            Assert.AreEqual(1, gridState.TotalEntries);

            var entries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(1, entries.Length);
            Assert.AreEqual(entity, entries[0].Entity);
        }

        [Test]
        public void SpatialGridBuildSystem_HandlesResidencyFallback()
        {
            var entity = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            StepSystems();

            var gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(1, gridState.TotalEntries);

            // Manually corrupt residency to simulate stale data
            var residency = _entityManager.GetComponentData<SpatialGridResidency>(entity);
            residency.CellId = 999; // Invalid cell
            _entityManager.SetComponentData(entity, residency);

            // Move entity to trigger dirty tracking
            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(new float3(1f, 0f, 1f), quaternion.identity, 1f));

            StepSystems();

            // System should handle the invalid residency gracefully
            gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(1, gridState.TotalEntries);

            var activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(1, activeEntries.Length);

            // Verify entity is in the correct cell despite corrupted residency
            var config = _entityManager.GetComponentData<SpatialGridConfig>(_gridEntity);
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);
            SpatialHash.Quantize(transform.Position, config, out var coords);
            var expectedCell = SpatialHash.Flatten(in coords, in config);
            Assert.IsTrue((uint)activeEntries[0].CellId < (uint)config.CellCount, "Entity should be in a valid cell");
        }

        [Test]
        public void SpatialGridBuildSystem_MaintainsDeterministicOrdering()
        {
            // Create entities with specific indices to test deterministic sorting
            var entities = new Entity[5];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = _entityManager.CreateEntity(typeof(SpatialIndexedTag), typeof(LocalTransform));
                // Place all in the same cell to test entity index sorting
                _entityManager.SetComponentData(entities[i], LocalTransform.FromPositionRotationScale(new float3(0.1f, 0f, 0.1f), quaternion.identity, 1f));
            }

            StepSystems();

            var activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(5, activeEntries.Length);

            // Verify entries are sorted by entity index
            for (int i = 0; i < activeEntries.Length - 1; i++)
            {
                Assert.Less(activeEntries[i].Entity.Index, activeEntries[i + 1].Entity.Index, "Entries should be sorted by entity index");
            }

            // Move one entity to trigger partial rebuild
            _entityManager.SetComponentData(entities[2], LocalTransform.FromPositionRotationScale(new float3(0.2f, 0f, 0.2f), quaternion.identity, 1f));

            StepSystems();

            activeEntries = _entityManager.GetBuffer<SpatialGridEntry>(_gridEntity);
            Assert.AreEqual(5, activeEntries.Length);

            // Verify ordering is maintained after partial rebuild
            for (int i = 0; i < activeEntries.Length - 1; i++)
            {
                Assert.LessOrEqual(activeEntries[i].Entity.Index, activeEntries[i + 1].Entity.Index, "Entries should remain sorted after partial rebuild");
            }
        }

        [Test]
        public void SpatialGridBuildSystem_SkipsUpdateWhenConfigInvalid()
        {
            var config = _entityManager.GetComponentData<SpatialGridConfig>(_gridEntity);
            config.CellSize = 0f;
            _entityManager.SetComponentData(_gridEntity, config);

            StepSystems();

            var gridState = _entityManager.GetComponentData<SpatialGridState>(_gridEntity);
            Assert.AreEqual(SpatialGridRebuildStrategy.None, gridState.LastStrategy);
            Assert.AreEqual(0, gridState.TotalEntries);
        }

        [Test]
        public void SpatialPartitionProfile_ToComponent_RespectsProviderSelection()
        {
            var profile = ScriptableObject.CreateInstance<SpatialPartitionProfile>();
            profile.SetProviderType(SpatialProviderType.HashedGrid);

            var config = profile.ToComponent();
            Assert.AreEqual(SpatialGridProviderIds.Hashed, config.ProviderId);

            profile.SetProviderType(SpatialProviderType.UniformGrid);
            config = profile.ToComponent();
            Assert.AreEqual(SpatialGridProviderIds.Uniform, config.ProviderId);

            ScriptableObject.DestroyImmediate(profile);
        }

        private void StepSystems(uint tickIncrement = 1)
        {
            var timeState = _entityManager.GetComponentData<TimeState>(_timeEntity);
            timeState.Tick += tickIncrement;
            timeState.IsPaused = false;
            _entityManager.SetComponentData(_timeEntity, timeState);

            ref var dirtyState = ref _world.Unmanaged.ResolveSystemStateRef(_dirtyHandle);
            _dirtySystem.OnUpdate(ref dirtyState);

            ref var buildState = ref _world.Unmanaged.ResolveSystemStateRef(_buildHandle);
            _buildSystem.OnUpdate(ref buildState);
        }
    }
}
