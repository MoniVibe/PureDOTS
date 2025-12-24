using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
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
            var villager = CreateVillager(new float3(3f, 0f, -1f), isAvailable: true, health: 80f, morale: 70f, energy: 65f, combatCapable: true);
            CreateVillager(new float3(-5f, 0f, 2f), isAvailable: false, isReserved: true, health: 50f, morale: 40f, energy: 30f);

            UpdateRegistry();

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerRegistry>()).GetSingletonEntity();
            var registry = _entityManager.GetComponentData<VillagerRegistry>(registryEntity);
            var entries = _entityManager.GetBuffer<VillagerRegistryEntry>(registryEntity);

            Assert.AreEqual(2, registry.TotalVillagers);
            Assert.AreEqual(1, registry.AvailableVillagers);
            Assert.AreEqual(1, registry.IdleVillagers);
            Assert.AreEqual(1, registry.ReservedVillagers);
            Assert.AreEqual(1, registry.CombatReadyVillagers);
            Assert.AreEqual(2, entries.Length);

            var metadata = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(entries.Length, metadata.EntryCount);
            Assert.Greater(metadata.Version, 0u);

            var first = entries[0];
            Assert.AreEqual(villager, first.VillagerEntity);
            Assert.AreEqual((byte)VillagerJob.JobType.Gatherer, (byte)first.JobType);
            Assert.AreEqual((byte)VillagerJob.JobPhase.Idle, (byte)first.JobPhase);
            Assert.AreEqual(VillagerAvailabilityFlags.Available, first.AvailabilityFlags & VillagerAvailabilityFlags.Available);
            Assert.GreaterOrEqual(first.HealthPercent, 1);
            Assert.GreaterOrEqual(first.MoralePercent, 1);
            Assert.GreaterOrEqual(first.EnergyPercent, 1);
        }

        [Test]
        public void VillagerRegistrySystem_SkipsUpdateDuringPlayback()
        {
            CreateVillager(new float3(1f, 0f, 0f), isAvailable: true);

            UpdateRegistry();

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerRegistry>()).GetSingletonEntity();
            var metadataBefore = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);

            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            var rewindState = _entityManager.GetComponentData<RewindState>(rewindEntity);
            rewindState.Mode = RewindMode.Playback;
            _entityManager.SetComponentData(rewindEntity, rewindState);

            UpdateRegistry();

            var metadataAfter = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(metadataBefore.Version, metadataAfter.Version);
            Assert.AreEqual(metadataBefore.EntryCount, metadataAfter.EntryCount);
        }

        [Test]
        public void VillagerRegistrySystem_ComputesAverages()
        {
            CreateVillager(new float3(0f, 0f, 0f), isAvailable: true, health: 90f, morale: 80f, energy: 70f, combatCapable: true);
            CreateVillager(new float3(1f, 0f, 1f), isAvailable: false, isReserved: true, health: 50f, morale: 40f, energy: 30f);

            UpdateRegistry();

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerRegistry>()).GetSingletonEntity();
            var registry = _entityManager.GetComponentData<VillagerRegistry>(registryEntity);

            Assert.AreEqual(2, registry.TotalVillagers);
            Assert.AreEqual(1, registry.AvailableVillagers);
            Assert.AreEqual(1, registry.ReservedVillagers);
            Assert.AreEqual(1, registry.CombatReadyVillagers);
            Assert.That(registry.AverageHealthPercent, Is.InRange(69f, 71f));
            Assert.That(registry.AverageMoralePercent, Is.InRange(59f, 61f));
            Assert.That(registry.AverageEnergyPercent, Is.InRange(49f, 51f));
        }

        private Entity CreateVillager(float3 position, bool isAvailable, bool isReserved = false, float health = 75f, float maxHealth = 100f, float morale = 60f, float energy = 50f, bool combatCapable = false)
        {
            var entity = _entityManager.CreateEntity(
                typeof(VillagerId),
                typeof(VillagerJob),
                typeof(VillagerJobTicket),
                typeof(VillagerAvailability),
                typeof(VillagerDisciplineState),
                typeof(LocalTransform),
                typeof(VillagerNeeds),
                typeof(VillagerMood),
                typeof(VillagerAIState));

            if (combatCapable)
            {
                _entityManager.AddComponentData(entity, new VillagerCombatStats
                {
                    AttackDamage = 10f,
                    AttackSpeed = 1f,
                    CurrentTarget = Entity.Null
                });
            }

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
                IsReserved = (byte)(isReserved ? 1 : 0),
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

            var needs = new VillagerNeeds
            {
                Health = math.clamp(health, 0f, maxHealth),
                MaxHealth = maxHealth
            };
            needs.SetHunger(0f);
            needs.SetEnergy(math.clamp(energy, 0f, 100f));
            needs.SetMorale(math.clamp(morale, 0f, 100f));
            needs.SetTemperature(0f);
            _entityManager.SetComponentData(entity, needs);

            _entityManager.SetComponentData(entity, new VillagerMood
            {
                Mood = morale,
                TargetMood = morale,
                MoodChangeRate = 0f,
                Wellbeing = morale
            });

            _entityManager.SetComponentData(entity, new VillagerAIState
            {
                CurrentState = isAvailable ? VillagerAIState.State.Idle : VillagerAIState.State.Working,
                CurrentGoal = isAvailable ? VillagerAIState.Goal.Work : VillagerAIState.Goal.Rest,
                TargetEntity = Entity.Null,
                TargetPosition = position,
                StateTimer = 0f,
                StateStartTick = 0
            });

            return entity;
        }

        private void UpdateRegistry()
        {
            _villagerGroup.Update();
        }
    }
}
