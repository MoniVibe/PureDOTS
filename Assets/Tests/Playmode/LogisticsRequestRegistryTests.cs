using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
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
            var reqA = CreateRequest(
                source: new float3(0f, 0f, 0f),
                destination: new float3(10f, 0f, 0f),
                requested: 100f,
                fulfilled: 25f,
                assigned: 40f,
                priority: LogisticsRequestPriority.High,
                flags: LogisticsRequestFlags.Urgent);

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

            Assert.AreEqual(LogisticsRequestPriority.Normal, entries[1].Priority);
            Assert.AreEqual(50f, entries[1].RemainingUnits, 0.001f);
        }

        private Entity CreateRequest(
            float3 source,
            float3 destination,
            float requested,
            float fulfilled,
            float assigned,
            LogisticsRequestPriority priority,
            LogisticsRequestFlags flags)
        {
            var entity = _entityManager.CreateEntity(
                typeof(LogisticsRequest),
                typeof(LogisticsRequestProgress));

            _entityManager.SetComponentData(entity, new LogisticsRequest
            {
                SourceEntity = Entity.Null,
                DestinationEntity = Entity.Null,
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
    }
}
