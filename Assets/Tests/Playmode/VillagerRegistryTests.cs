using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests
{
    public class VillagerRegistryTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;
        private VillagerSystemGroup _villagerGroup;
        private int _villagerIdCounter;

        [SetUp]
        public void SetUp()
        {
            _world = new World("VillagerRegistryTestsWorld");
            _previousWorld = World.DefaultGameObjectInjectionWorld;
            World.DefaultGameObjectInjectionWorld = _world;
           _entityManager = _world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
            _villagerGroup = _world.GetOrCreateSystemManaged<VillagerSystemGroup>();
        }

        [TearDown]
        public void TearDown()
        {
            if (World.DefaultGameObjectInjectionWorld == _world)
            {
                World.DefaultGameObjectInjectionWorld = _previousWorld;
            }

            _world.Dispose();
        }

        [Test]
        public void VillagerRegistrySystem_PopulatesEntries()
        {
            var villager = CreateVillager(new float3(3f, 0f, -1f), isAvailable: true);
            CreateVillager(new float3(-5f, 0f, 2f), isAvailable: false);

            UpdateRegistry();

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerRegistry>()).GetSingletonEntity();
            var registry = _entityManager.GetComponentData<VillagerRegistry>(registryEntity);
            var entries = _entityManager.GetBuffer<VillagerRegistryEntry>(registryEntity);

            Assert.AreEqual(2, registry.TotalVillagers);
            Assert.AreEqual(1, registry.AvailableVillagers);
            Assert.AreEqual(2, entries.Length);

            var first = entries[0];
            Assert.AreEqual(villager, first.VillagerEntity);
            Assert.AreEqual((byte)VillagerJob.JobType.Gatherer, (byte)first.JobType);
            Assert.AreEqual((byte)VillagerJob.JobPhase.Idle, (byte)first.JobPhase);
            Assert.AreEqual(VillagerAvailabilityFlags.Available, first.AvailabilityFlags & VillagerAvailabilityFlags.Available);
        }

        private Entity CreateVillager(float3 position, bool isAvailable)
        {
            var entity = _entityManager.CreateEntity(
                typeof(VillagerId),
                typeof(VillagerJob),
                typeof(VillagerJobTicket),
                typeof(VillagerAvailability),
                typeof(VillagerDisciplineState),
                typeof(LocalTransform));

            _entityManager.SetComponentData(entity, new VillagerId
            {
                Value = ++_villagerIdCounter,
                FactionId = 0
            });

            _entityManager.SetComponentData(entity, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Idle,
                ActiveTicketId = 0,
                Productivity = 1f,
                LastStateChangeTick = 0
            });

            _entityManager.SetComponentData(entity, new VillagerJobTicket
            {
                TicketId = 0,
                JobType = VillagerJob.JobType.Gatherer,
                ResourceTypeIndex = ushort.MaxValue,
                ResourceEntity = Entity.Null,
                StorehouseEntity = Entity.Null,
                Priority = 0,
                Phase = (byte)VillagerJob.JobPhase.Idle,
                ReservedUnits = 0f,
                AssignedTick = 0,
                LastProgressTick = 0
            });

            _entityManager.SetComponentData(entity, new VillagerAvailability
            {
                IsAvailable = (byte)(isAvailable ? 1 : 0),
                IsReserved = 0,
                LastChangeTick = 0,
                BusyTime = 0f
            });

            _entityManager.SetComponentData(entity, new VillagerDisciplineState
            {
                Value = VillagerDisciplineType.Forester,
                Level = 1,
                Experience = 0f
            });

            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));

            return entity;
        }

        private void UpdateRegistry()
        {
            _villagerGroup.Update();
        }
    }
}
