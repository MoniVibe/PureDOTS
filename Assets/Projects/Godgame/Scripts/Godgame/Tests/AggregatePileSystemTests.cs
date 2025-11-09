using Godgame.Resources;
using Godgame.Systems;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Tests.Resources
{
    public class AggregatePileSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private SystemHandle _pileSystemHandle;
        private Entity _commandEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("AggregatePileSystemTests");
            _entityManager = _world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            var timeEntity = _entityManager.CreateEntity(typeof(TimeState));
            _entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                Tick = 1,
                IsPaused = false
            });

            var configEntity = _entityManager.CreateEntity(typeof(AggregatePileConfig), typeof(AggregatePileRuntimeState));
            _entityManager.SetComponentData(configEntity, AggregatePileConfig.CreateDefault());
            _entityManager.SetComponentData(configEntity, new AggregatePileRuntimeState { NextMergeTime = 0f, ActivePiles = 0 });

            _commandEntity = _entityManager.CreateEntity(typeof(AggregatePileCommandState));
            _entityManager.AddBuffer<AggregatePileAddCommand>(_commandEntity);
            _entityManager.AddBuffer<AggregatePileTakeCommand>(_commandEntity);
            _entityManager.AddBuffer<AggregatePileCommandResult>(_commandEntity);

            _world.GetOrCreateSystemManaged<AggregatePileCommandBootstrapSystem>();
            _pileSystemHandle = _world.CreateSystem<AggregatePileSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void AddCommandSpawnsPileAndUpdatesScale()
        {
            var addBuffer = _entityManager.GetBuffer<AggregatePileAddCommand>(_commandEntity);
            addBuffer.Add(new AggregatePileAddCommand
            {
                Requester = Entity.Null,
                Position = float3.zero,
                ResourceTypeIndex = 1,
                Amount = 600f,
                MergeRadiusOverride = 0f,
                PreferredPile = Entity.Null,
                Flags = AggregatePileAddFlags.None
            });

            UpdateSystem(_pileSystemHandle);

            var pileQuery = _entityManager.CreateEntityQuery(typeof(AggregatePile), typeof(LocalTransform));
            Assert.AreEqual(1, pileQuery.CalculateEntityCount());

            var pileEntity = pileQuery.GetSingletonEntity();
            var pile = _entityManager.GetComponentData<AggregatePile>(pileEntity);
            Assert.AreEqual(600f, pile.Amount, 0.01f);

            var transform = _entityManager.GetComponentData<LocalTransform>(pileEntity);
            Assert.Greater(transform.Scale, 1f);

            var results = _entityManager.GetBuffer<AggregatePileCommandResult>(_commandEntity);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(AggregatePileCommandResultType.AddAccepted, results[0].Type);
            Assert.AreEqual(600f, results[0].Amount, 0.01f);
        }

        [Test]
        public void AddCommandMergesIntoExistingPile()
        {
            var existing = CreatePileEntity(resourceType: 2, amount: 1200f, position: new float3(1f, 0f, 0f));

            var addBuffer = _entityManager.GetBuffer<AggregatePileAddCommand>(_commandEntity);
            addBuffer.Add(new AggregatePileAddCommand
            {
                Requester = Entity.Null,
                Position = new float3(1.2f, 0f, 0f),
                ResourceTypeIndex = 2,
                Amount = 800f,
                MergeRadiusOverride = 3f,
                PreferredPile = existing,
                Flags = AggregatePileAddFlags.None
            });

            UpdateSystem(_pileSystemHandle);

            var pile = _entityManager.GetComponentData<AggregatePile>(existing);
            Assert.AreEqual(2000f, pile.Amount, 0.01f);

            var results = _entityManager.GetBuffer<AggregatePileCommandResult>(_commandEntity);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(800f, results[0].Amount, 0.01f);
        }

        [Test]
        public void TakeCommandRemovesAmountAndDestroysPileWhenEmpty()
        {
            var pileEntity = CreatePileEntity(resourceType: 0, amount: 150f, position: float3.zero);

            var takeBuffer = _entityManager.GetBuffer<AggregatePileTakeCommand>(_commandEntity);
            takeBuffer.Add(new AggregatePileTakeCommand
            {
                Requester = Entity.Null,
                Pile = pileEntity,
                Amount = 200f
            });

            UpdateSystem(_pileSystemHandle);

            Assert.IsFalse(_entityManager.Exists(pileEntity));

            var results = _entityManager.GetBuffer<AggregatePileCommandResult>(_commandEntity);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(AggregatePileCommandResultType.TakeCompleted, results[0].Type);
            Assert.AreEqual(150f, results[0].Amount, 0.01f);
        }

        [Test]
        public void MergePassCombinesNearbyPiles()
        {
            CreatePileEntity(1, 600f, new float3(0f, 0f, 0f));
            CreatePileEntity(1, 400f, new float3(1f, 0f, 0f));

            // Advance time so merge pass runs immediately.
            var time = _entityManager.CreateEntityQuery(typeof(TimeState)).GetSingleton<TimeState>();
            time.Tick = 10;
            _entityManager.SetComponentData(_entityManager.CreateEntityQuery(typeof(TimeState)).GetSingletonEntity(), time);

            UpdateSystem(_pileSystemHandle);

            var pileQuery = _entityManager.CreateEntityQuery(typeof(AggregatePile));
            Assert.AreEqual(1, pileQuery.CalculateEntityCount());
            var pile = pileQuery.GetSingleton<AggregatePile>();
            Assert.AreEqual(1000f, pile.Amount, 0.01f);
        }

        private Entity CreatePileEntity(ushort resourceType, float amount, float3 position)
        {
            var entity = _entityManager.CreateEntity(typeof(AggregatePile), typeof(LocalTransform), typeof(AggregatePileVisual));
            _entityManager.SetComponentData(entity, new AggregatePile
            {
                ResourceTypeIndex = resourceType,
                Amount = amount,
                MaxCapacity = 2500f,
                MergeRadius = 2.5f,
                LastMutationTime = 0f,
                State = AggregatePileState.Growing
            });
            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, AggregatePileVisualUtility.CalculateScale(amount)));
            _entityManager.SetComponentData(entity, new AggregatePileVisual
            {
                CurrentScale = AggregatePileVisualUtility.CalculateScale(amount),
                TargetScale = AggregatePileVisualUtility.CalculateScale(amount)
            });

            var runtime = _entityManager.CreateEntityQuery(typeof(AggregatePileRuntimeState)).GetSingleton<AggregatePileRuntimeState>();
            runtime.ActivePiles += 1;
            var runtimeEntity = _entityManager.CreateEntityQuery(typeof(AggregatePileRuntimeState)).GetSingletonEntity();
            _entityManager.SetComponentData(runtimeEntity, runtime);

            return entity;
        }

        private void UpdateSystem(SystemHandle handle)
        {
            _world.Unmanaged.ResolveSystemStateRef(handle).Update();
        }
    }
}
