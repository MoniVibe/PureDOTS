using NUnit.Framework;
using PureDOTS.Runtime.Aggregates;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Village;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class AggregatePersonalitySystemsTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("AggregatePersonalitySystemsTests");
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        private Entity CreateAggregateEntity()
        {
            var aggregate = _entityManager.CreateEntity(typeof(AggregateEntity));
            _entityManager.SetComponentData(aggregate, new AggregateEntity
            {
                Category = AggregateCategory.Guild,
                MemberCount = 0
            });
            _entityManager.AddBuffer<AggregateMember>(aggregate);
            return aggregate;
        }

        [Test]
        public void AggregateAlignmentSystem_ComputesWeightedAverage()
        {
            var memberA = _entityManager.CreateEntity(typeof(VillagerAlignment));
            _entityManager.SetComponentData(memberA, new VillagerAlignment
            {
                MoralAxis = 60,
                OrderAxis = 40,
                PurityAxis = 20,
                AlignmentStrength = 0.9f
            });

            var memberB = _entityManager.CreateEntity(typeof(VillagerAlignment));
            _entityManager.SetComponentData(memberB, new VillagerAlignment
            {
                MoralAxis = -60,
                OrderAxis = -20,
                PurityAxis = 80,
                AlignmentStrength = 0.5f
            });

            var aggregate = CreateAggregateEntity();
            var buffer = _entityManager.GetBuffer<AggregateMember>(aggregate);
            buffer.Add(new AggregateMember { Member = memberA, Weight = 1f });
            buffer.Add(new AggregateMember { Member = memberB, Weight = 2f });

            UpdateSystem<AggregatePersonalityBootstrapSystem>();
            UpdateSystem<AggregateAlignmentComputationSystem>();

            var alignment = _entityManager.GetComponentData<VillagerAlignment>(aggregate);
            Assert.AreEqual(-20, alignment.MoralAxis);
            Assert.AreEqual(0, alignment.OrderAxis);
            Assert.AreEqual(60, alignment.PurityAxis);
            Assert.That(alignment.AlignmentStrength, Is.EqualTo(0.63f).Within(0.01f));
        }

        [Test]
        public void AggregateBehaviorSystem_ComputesAverageTraits()
        {
            var memberA = _entityManager.CreateEntity(typeof(VillagerBehavior));
            _entityManager.SetComponentData(memberA, new VillagerBehavior
            {
                BoldScore = 60,
                VengefulScore = -20,
                InitiativeModifier = 0.1f,
                ActiveGrudgeCount = 2,
                LastMajorActionTick = 5
            });

            var memberB = _entityManager.CreateEntity(typeof(VillagerBehavior));
            _entityManager.SetComponentData(memberB, new VillagerBehavior
            {
                BoldScore = 0,
                VengefulScore = 60,
                InitiativeModifier = -0.2f,
                ActiveGrudgeCount = 1,
                LastMajorActionTick = 10
            });

            var aggregate = CreateAggregateEntity();
            var buffer = _entityManager.GetBuffer<AggregateMember>(aggregate);
            buffer.Add(new AggregateMember { Member = memberA, Weight = 1f });
            buffer.Add(new AggregateMember { Member = memberB, Weight = 3f });

            UpdateSystem<AggregatePersonalityBootstrapSystem>();
            UpdateSystem<AggregateBehaviorComputationSystem>();

            var behavior = _entityManager.GetComponentData<VillagerBehavior>(aggregate);
            Assert.AreEqual(15, behavior.BoldScore);
            Assert.AreEqual(45, behavior.VengefulScore);
            Assert.That(behavior.InitiativeModifier, Is.EqualTo(-0.125f).Within(0.001f));
            Assert.AreEqual(1, behavior.ActiveGrudgeCount);
            Assert.AreEqual(10u, behavior.LastMajorActionTick);
        }

        [Test]
        public void AggregateInitiativeSystem_ComputesInitiative()
        {
            var aggregate = CreateAggregateEntity();
            UpdateSystem<AggregatePersonalityBootstrapSystem>();

            _entityManager.SetComponentData(aggregate, new AggregateEntity
            {
                Category = AggregateCategory.Guild,
                Morale = 0.8f,
                Cohesion = 0.6f,
                Stress = 0.1f,
                MemberCount = 12
            });

            _entityManager.SetComponentData(aggregate, new VillagerBehavior
            {
                BoldScore = 50,
                ActiveGrudgeCount = 3
            });

            _entityManager.SetComponentData(aggregate, new VillagerAlignment
            {
                MoralAxis = 10,
                OrderAxis = -50,
                PurityAxis = 30
            });

            UpdateSystem<AggregateInitiativeComputationSystem>();

            var initiative = _entityManager.GetComponentData<VillagerInitiativeState>(aggregate);
            Assert.That(initiative.CurrentInitiative, Is.GreaterThan(0.5f));
            Assert.That(initiative.NextActionTick, Is.GreaterThan(0u));
        }

        [Test]
        private void UpdateSystem<T>() where T : unmanaged, ISystem
        {
            var handle = _world.GetOrCreateSystem<T>();
            handle.Update(_world.Unmanaged);
        }
    }
}
