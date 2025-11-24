using NUnit.Framework;
using PureDOTS.Config;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villager;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems.Villagers;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Editmode
{
    /// <summary>
    /// Tests to verify that TicketToOfferAdapterSystem converts tickets correctly and idempotently.
    /// </summary>
    public class TicketToOfferAdapter_Idempotent_Conversion_Editmode
    {
        private World _world;
        private EntityManager EntityManager => _world.EntityManager;
        
        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld", WorldFlags.Game);
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            EnsureTimeState();
            EnsureRewindState();
            EnsureJobCatalog();
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
        public void Adapter_ConvertsTicket_ToWorkOffer_Correctly()
        {
            // Arrange: Create a villager with a ticket
            var villager = EntityManager.CreateEntity();
            var resourceEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(resourceEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Assigned
            });
            
            EntityManager.AddComponentData(villager, new VillagerJobTicket
            {
                TicketId = 123,
                JobType = VillagerJob.JobType.Gatherer,
                ResourceEntity = resourceEntity,
                Priority = 75,
                AssignedTick = 100
            });
            
            // Act: Run the adapter system
            var adapterSystem = _world.GetOrCreateSystemManaged<TicketToOfferAdapterSystem>();
            adapterSystem.Update(_world.Unmanaged);
            
            // Assert: A WorkOffer should be created
            var query = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag));
            Assert.AreEqual(1, query.CalculateEntityCount(), "One WorkOffer should be created from ticket");
            
            var offerEntity = query.GetSingletonEntity();
            var offer = EntityManager.GetComponentData<WorkOffer>(offerEntity);
            
            Assert.AreEqual(resourceEntity, offer.Target, "Offer target should match ticket resource entity");
            Assert.AreEqual(1, offer.Slots, "Legacy tickets should have Slots=1");
            Assert.AreEqual(75, offer.Priority, "Offer priority should match ticket priority");
            Assert.AreEqual(0, offer.Taken, "New offer should have Taken=0");
        }
        
        [Test]
        public void Adapter_IsIdempotent_MultipleRuns()
        {
            // Arrange: Create a villager with a ticket
            var villager = EntityManager.CreateEntity();
            var resourceEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(resourceEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Assigned
            });
            
            EntityManager.AddComponentData(villager, new VillagerJobTicket
            {
                TicketId = 456,
                JobType = VillagerJob.JobType.Gatherer,
                ResourceEntity = resourceEntity,
                Priority = 50,
                AssignedTick = 200
            });
            
            var adapterSystem = _world.GetOrCreateSystemManaged<TicketToOfferAdapterSystem>();
            
            // Act: Run adapter multiple times
            adapterSystem.Update(_world.Unmanaged);
            var count1 = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag)).CalculateEntityCount();
            
            adapterSystem.Update(_world.Unmanaged);
            var count2 = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag)).CalculateEntityCount();
            
            adapterSystem.Update(_world.Unmanaged);
            var count3 = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag)).CalculateEntityCount();
            
            // Assert: Should always have exactly one offer (old ones cleaned up, new one created)
            Assert.AreEqual(1, count1, "First run should create one offer");
            Assert.AreEqual(1, count2, "Second run should still have one offer (idempotent)");
            Assert.AreEqual(1, count3, "Third run should still have one offer (idempotent)");
        }
        
        [Test]
        public void Adapter_SkipsTickets_WithNullResourceEntity()
        {
            // Arrange: Create a villager with a ticket that has null resource entity
            var villager = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Assigned
            });
            
            EntityManager.AddComponentData(villager, new VillagerJobTicket
            {
                TicketId = 789,
                JobType = VillagerJob.JobType.Gatherer,
                ResourceEntity = Entity.Null, // Invalid
                Priority = 50,
                AssignedTick = 300
            });
            
            // Act: Run the adapter system
            var adapterSystem = _world.GetOrCreateSystemManaged<TicketToOfferAdapterSystem>();
            adapterSystem.Update(_world.Unmanaged);
            
            // Assert: No offer should be created
            var query = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag));
            Assert.AreEqual(0, query.CalculateEntityCount(), "No offer should be created for invalid ticket");
        }
        
        [Test]
        public void Adapter_SkipsIdleVillagers()
        {
            // Arrange: Create a villager with idle job
            var villager = EntityManager.CreateEntity();
            var resourceEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(resourceEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.None, // Idle
                Phase = VillagerJob.JobPhase.Idle
            });
            
            EntityManager.AddComponentData(villager, new VillagerJobTicket
            {
                TicketId = 999,
                JobType = VillagerJob.JobType.Gatherer,
                ResourceEntity = resourceEntity,
                Priority = 50,
                AssignedTick = 400
            });
            
            // Act: Run the adapter system
            var adapterSystem = _world.GetOrCreateSystemManaged<TicketToOfferAdapterSystem>();
            adapterSystem.Update(_world.Unmanaged);
            
            // Assert: No offer should be created for idle villagers
            var query = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag));
            Assert.AreEqual(0, query.CalculateEntityCount(), "No offer should be created for idle villagers");
        }
        
        private void EnsureTimeState()
        {
            if (!EntityManager.HasComponent<TimeState>(EntityManager.CreateEntityQuery(typeof(TimeState)).GetSingletonEntity()))
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 0.016f,
                    IsPaused = false
                });
            }
        }
        
        private void EnsureRewindState()
        {
            if (!EntityManager.HasComponent<RewindState>(EntityManager.CreateEntityQuery(typeof(RewindState)).GetSingletonEntity()))
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new RewindState
                {
                    Mode = RewindMode.Record
                });
            }
        }
        
        private void EnsureJobCatalog()
        {
            // Create a minimal job catalog for testing
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<JobDefinitionCatalogBlob>();
            var jobs = blobBuilder.Allocate(ref root.Jobs, 1);
            jobs[0] = new JobDefinitionData
            {
                JobName = new FixedString64Bytes("Gatherer"),
                JobTypeIndex = (byte)VillagerJob.JobType.Gatherer,
                BaseDurationSeconds = 5f,
                BasePriority = 50
            };
            var blobRef = blobBuilder.CreateBlobAssetReference<JobDefinitionCatalogBlob>(Allocator.Persistent);
            blobBuilder.Dispose();
            
            var catalogEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(catalogEntity, new JobDefinitionCatalogComponent { Catalog = blobRef });
        }
    }
}

