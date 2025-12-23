using NUnit.Framework;
using PureDOTS.Config;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villager;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
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
        private World _world = null!;
        private SystemHandle _adapterSystemHandle;
        private BlobAssetReference<JobDefinitionCatalogBlob> _jobCatalog;
        private EntityManager EntityManager => _world.EntityManager;
        
        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld", WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = _world;
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            EnsureTimeState();
            EnsureRewindState();
            EnsureJobCatalog();
            _adapterSystemHandle = _world.GetOrCreateSystem<TicketToOfferAdapterSystem>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_jobCatalog.IsCreated)
            {
                _jobCatalog.Dispose();
            }
            
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }
        
        [Test]
        public void Adapter_ConvertsTicket_ToWorkOffer_Correctly()
        {
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
            
            RunAdapterSystem();
            
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
            
            RunAdapterSystem();
            var count1 = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag)).CalculateEntityCount();
            
            RunAdapterSystem();
            var count2 = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag)).CalculateEntityCount();
            
            RunAdapterSystem();
            var count3 = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag)).CalculateEntityCount();
            
            Assert.AreEqual(1, count1, "First run should create one offer");
            Assert.AreEqual(1, count2, "Second run should still have one offer (idempotent)");
            Assert.AreEqual(1, count3, "Third run should still have one offer (idempotent)");
        }
        
        [Test]
        public void Adapter_SkipsTickets_WithNullResourceEntity()
        {
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
                ResourceEntity = Entity.Null,
                Priority = 50,
                AssignedTick = 300
            });
            
            RunAdapterSystem();
            
            var query = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag));
            Assert.AreEqual(0, query.CalculateEntityCount(), "No offer should be created for invalid ticket");
        }
        
        [Test]
        public void Adapter_SkipsIdleVillagers()
        {
            var villager = EntityManager.CreateEntity();
            var resourceEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(resourceEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.None,
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
            
            RunAdapterSystem();
            
            var query = EntityManager.CreateEntityQuery(typeof(WorkOffer), typeof(TicketOriginatedOfferTag));
            Assert.AreEqual(0, query.CalculateEntityCount(), "No offer should be created for idle villagers");
        }
        
        private void RunAdapterSystem()
        {
            _adapterSystemHandle.Update(_world.Unmanaged);
        }
        
        private void EnsureTimeState()
        {
            var query = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 0.016f,
                    IsPaused = false
                });
                return;
            }

            var timeEntity = query.GetSingletonEntity();
            var timeState = EntityManager.GetComponentData<TimeState>(timeEntity);
            timeState.IsPaused = false;
            EntityManager.SetComponentData(timeEntity, timeState);
        }
        
        private void EnsureRewindState()
        {
            var query = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new RewindState
                {
                    Mode = RewindMode.Record
                });
                return;
            }

            var rewindEntity = query.GetSingletonEntity();
            var rewindState = EntityManager.GetComponentData<RewindState>(rewindEntity);
            rewindState.Mode = RewindMode.Record;
            EntityManager.SetComponentData(rewindEntity, rewindState);
        }
        
        private void EnsureJobCatalog()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<JobDefinitionCatalogComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                if (!_jobCatalog.IsCreated)
                {
                    _jobCatalog = EntityManager.GetComponentData<JobDefinitionCatalogComponent>(query.GetSingletonEntity()).Catalog;
                }
                return;
            }
            
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<JobDefinitionCatalogBlob>();
            var jobs = blobBuilder.Allocate(ref root.Jobs, 1);
            ref var job = ref jobs[0];
            job.JobName = new FixedString64Bytes("Gatherer");
            job.JobTypeIndex = (byte)VillagerJob.JobType.Gatherer;
            job.BaseDurationSeconds = 5f;
            job.MinDurationSeconds = 1f;
            job.MaxDurationSeconds = 10f;
            job.SkillMultiplier = 1f;
            job.NeedsMultiplier = 1f;
            job.BasePriority = 50;
            job.CooldownSeconds = 0f;
            
            blobBuilder.Allocate(ref job.ResourceCosts, 0);
            blobBuilder.Allocate(ref job.ResourceRewards, 0);
            
            _jobCatalog = blobBuilder.CreateBlobAssetReference<JobDefinitionCatalogBlob>(Allocator.Persistent);
            blobBuilder.Dispose();
            
            var catalogEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(catalogEntity, new JobDefinitionCatalogComponent { Catalog = _jobCatalog });
        }
    }
}
