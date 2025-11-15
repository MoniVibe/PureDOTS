using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Transport;
using PureDOTS.Systems;
using PureDOTS.Tests;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Playmode
{
    public class LogisticsRequestRegistryTests
    {
        private World _world;
        private EntityManager _entityManager;
        private uint _gridVersion;

        [SetUp]
        public void SetUp()
        {
            _world = new World("LogisticsRequestRegistryTests");
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Advance time tick so registry writes meaningful values.
            var timeEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 120;
            _entityManager.SetComponentData(timeEntity, timeState);

            ConfigureSpatialGrid();
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
        public void LogisticsRequestRegistrySystem_PopulatesEntriesAndAggregates()
        {
            var sourceAnchor = CreateSpatialAnchor(cellId: 5);
            var destinationAnchor = CreateSpatialAnchor(cellId: 19);

            var reqA = CreateRequest(
                source: new float3(512f, 0f, 512f),
                destination: new float3(-512f, 0f, -512f),
                requested: 100f,
                fulfilled: 25f,
                assigned: 40f,
                priority: LogisticsRequestPriority.High,
                flags: LogisticsRequestFlags.Urgent,
                sourceEntity: sourceAnchor,
                destinationEntity: destinationAnchor);

            CreateRequest(
                source: new float3(5f, 0f, 5f),
                destination: new float3(-5f, 0f, -5f),
                requested: 50f,
                fulfilled: 0f,
                assigned: 0f,
                priority: LogisticsRequestPriority.Normal,
                flags: LogisticsRequestFlags.None);

            _world.UpdateSystem<LogisticsRequestRegistrySystem>();

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<LogisticsRequestRegistry>()).GetSingletonEntity();
            var registry = _entityManager.GetComponentData<LogisticsRequestRegistry>(registryEntity);
            var entries = _entityManager.GetBuffer<LogisticsRequestRegistryEntry>(registryEntity);
            var metadata = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);

            Assert.AreEqual(2, registry.TotalRequests);
            Assert.AreEqual(1, registry.PendingRequests, "One request should remain pending (no assigned units).");
            Assert.AreEqual(1, registry.InProgressRequests, "One request should be in progress (assigned units > 0).");
            Assert.AreEqual(1, registry.CriticalRequests, "Urgent/high request counts as critical.");
            Assert.AreEqual(150f, registry.TotalRequestedUnits, 0.001f);
            Assert.AreEqual(40f, registry.TotalAssignedUnits, 0.001f);
            Assert.AreEqual(125f, registry.TotalRemainingUnits, 0.001f);

            Assert.AreEqual(entries.Length, metadata.EntryCount);
            Assert.Greater(metadata.Version, 0u);
            Assert.IsTrue(metadata.Continuity.HasSpatialData);

            Assert.AreEqual(2, entries.Length);
            Assert.AreEqual(reqA, entries[0].RequestEntity);
            Assert.AreEqual(LogisticsRequestPriority.High, entries[0].Priority);
            Assert.AreEqual(LogisticsRequestFlags.Urgent, entries[0].Flags);
            Assert.AreEqual(75f, entries[0].RemainingUnits, 0.001f);
            Assert.AreEqual(40f, entries[0].AssignedUnits, 0.001f);
            Assert.AreEqual(5, entries[0].SourceCellId, "Source should consume SpatialGridResidency metadata.");
            Assert.AreEqual(19, entries[0].DestinationCellId, "Destination should consume SpatialGridResidency metadata.");
            Assert.AreEqual(_gridVersion, entries[0].SpatialVersion);

            Assert.AreEqual(LogisticsRequestPriority.Normal, entries[1].Priority);
            Assert.AreEqual(50f, entries[1].RemainingUnits, 0.001f);

            Assert.AreEqual(2, registry.SpatialResolvedCount, "Both residency-backed endpoints should count as resolved.");
            Assert.GreaterOrEqual(registry.SpatialFallbackCount, 2, "Fallback counts reflect hashed endpoints.");
            Assert.AreEqual(registry.SpatialResolvedCount, metadata.Continuity.SpatialResolvedCount);
            Assert.AreEqual(registry.SpatialFallbackCount, metadata.Continuity.SpatialFallbackCount);
        }

        private Entity CreateRequest(
            float3 source,
            float3 destination,
            float requested,
            float fulfilled,
            float assigned,
            LogisticsRequestPriority priority,
            LogisticsRequestFlags flags,
            Entity sourceEntity = default,
            Entity destinationEntity = default)
        {
            var entity = _entityManager.CreateEntity(
                typeof(LogisticsRequest),
                typeof(LogisticsRequestProgress));

            _entityManager.SetComponentData(entity, new LogisticsRequest
            {
                SourceEntity = sourceEntity,
                DestinationEntity = destinationEntity,
                SourcePosition = source,
                DestinationPosition = destination,
                ResourceTypeIndex = 1,
                RequestedUnits = requested,
                FulfilledUnits = fulfilled,
                Priority = priority,
                Flags = flags,
                CreatedTick = 10u,
                LastUpdateTick = 100u
            });

            _entityManager.SetComponentData(entity, new LogisticsRequestProgress
            {
                AssignedUnits = assigned,
                AssignedTransportCount = assigned > 0f ? 2 : 0,
                LastAssignmentTick = 110u
            });

            return entity;
        }

        private void ConfigureSpatialGrid()
        {
            var gridEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()).GetSingletonEntity();
            var config = _entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            config.WorldMin = new float3(-16f, 0f, -16f);
            config.WorldMax = new float3(16f, 0f, 16f);
            config.CellSize = 2f;
            config.CellCounts = CalculateCellCounts(config.WorldMin, config.WorldMax, config.CellSize);
            _entityManager.SetComponentData(gridEntity, config);

            var state = _entityManager.GetComponentData<SpatialGridState>(gridEntity);
            state.Version = math.max(1u, state.Version + 1u);
            _gridVersion = state.Version;
            _entityManager.SetComponentData(gridEntity, state);
        }

        private static int3 CalculateCellCounts(float3 min, float3 max, float cellSize)
        {
            var extent = math.abs(max - min);
            var counts = (int3)math.ceil(extent / math.max(0.001f, cellSize));
            return math.max(counts, new int3(1, 1, 1));
        }

        private Entity CreateSpatialAnchor(int cellId)
        {
            var entity = _entityManager.CreateEntity(typeof(SpatialGridResidency));
            _entityManager.SetComponentData(entity, new SpatialGridResidency
            {
                CellId = cellId,
                LastPosition = float3.zero,
                Version = _gridVersion
            });
            return entity;
        }
    }
}
