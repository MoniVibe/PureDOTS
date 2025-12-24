#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Cooperation;
using PureDOTS.Runtime.Intent;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Runtime.Relations;
using PureDOTS.Runtime.Social;
using PureDOTS.Systems;
using PureDOTS.Systems.AI;
using PureDOTS.Systems.Cooperation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    public class RelationAwareBehaviorTests
    {
        private World _world;
        private EntityManager EntityManager => _world.EntityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("RelationAwareBehaviorTests", WorldFlags.Game);
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            EnsureTimeState();
            EnsureRewindState();
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
        public void RelationAwareHaulingSystem_PositiveRelation_FollowsPartner()
        {
            var friendA = CreateHauler(new float3(0f, 0f, 0f));
            var friendB = CreateHauler(new float3(2f, 0f, 0f));

            AddRelation(friendA, friendB, 80);
            AddRelation(friendB, friendA, 80);

            var haulingSystem = _world.GetOrCreateSystem<RelationAwareHaulingSystem>();
            haulingSystem.Update(_world.Unmanaged);

            var intentA = EntityManager.GetComponentData<EntityIntent>(friendA);
            Assert.AreEqual(IntentMode.Follow, intentA.Mode);
            Assert.AreEqual(friendB, intentA.TargetEntity);
        }

        [Test]
        public void RelationAwareHaulingSystem_NegativeRelation_FleesEnemy()
        {
            var hauler = CreateHauler(new float3(0f, 0f, 0f));
            var rival = CreateHauler(new float3(1f, 0f, 0f));

            AddRelation(hauler, rival, -80);

            var haulingSystem = _world.GetOrCreateSystem<RelationAwareHaulingSystem>();
            haulingSystem.Update(_world.Unmanaged);

            var intentA = EntityManager.GetComponentData<EntityIntent>(hauler);
            Assert.AreEqual(IntentMode.MoveTo, intentA.Mode);
            Assert.AreEqual(Entity.Null, intentA.TargetEntity);
        }

        [Test]
        public void RelationAwareProductionSystem_EmitsPositiveInteraction()
        {
            var workerA = CreateWorker(new float3(0f, 0f, 0f));
            var workerB = CreateWorker(new float3(1f, 0f, 0f));

            AddRelation(workerA, workerB, 60);
            AddRelation(workerB, workerA, 60);

            var productionSystem = _world.GetOrCreateSystem<RelationAwareProductionSystem>();
            productionSystem.Update(_world.Unmanaged);

            var beginEcb = _world.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            beginEcb.Update();

            var requestsQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RecordInteractionRequest>());
            Assert.Greater(requestsQuery.CalculateEntityCount(), 0, "Expected cooperation interaction request to be emitted.");
        }

        [Test]
        public void RelationAvoidanceSystem_FleesHostileEntity()
        {
            var workerA = CreateWorker(new float3(0f, 0f, 0f));
            var workerB = CreateWorker(new float3(2f, 0f, 0f));

            AddRelation(workerA, workerB, -70);

            var avoidanceSystem = _world.GetOrCreateSystem<RelationAvoidanceSystem>();
            avoidanceSystem.Update(_world.Unmanaged);

            var intentA = EntityManager.GetComponentData<EntityIntent>(workerA);
            Assert.AreEqual(IntentMode.MoveTo, intentA.Mode);
        }

        [Test]
        public void RelationCohesionSystem_UsesMemberRelations()
        {
            var memberA = CreateWorker(new float3(0f, 0f, 0f));
            var memberB = CreateWorker(new float3(0f, 0f, 1f));

            AddRelation(memberA, memberB, 80);
            AddRelation(memberB, memberA, 80);

            var teamEntity = EntityManager.CreateEntity(typeof(ProductionTeam));
            EntityManager.SetComponentData(teamEntity, new ProductionTeam
            {
                Leader = memberA,
                MemberCount = 2,
                Cohesion = 0.1f,
                Status = ProductionTeamStatus.Forming
            });

            var members = EntityManager.AddBuffer<ProductionTeamMember>(teamEntity);
            members.Add(new ProductionTeamMember
            {
                MemberEntity = memberA,
                Role = ProductionRole.Builder,
                ContributionWeight = 1f,
                SkillFactor = 0.8f
            });
            members.Add(new ProductionTeamMember
            {
                MemberEntity = memberB,
                Role = ProductionRole.Craftsman,
                ContributionWeight = 1f,
                SkillFactor = 0.8f
            });

            var cohesionSystem = _world.GetOrCreateSystem<RelationCohesionSystem>();
            cohesionSystem.Update(_world.Unmanaged);

            var updatedTeam = EntityManager.GetComponentData<ProductionTeam>(teamEntity);
            Assert.Greater(updatedTeam.Cohesion, 0.1f, "Cohesion should improve when members share strong relations.");
        }

        private Entity CreateHauler(float3 position)
        {
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            EntityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Gather,
                Priority = InterruptPriority.Low,
                IsValid = 1
            });

            EntityManager.AddBuffer<EntityRelation>(entity);
            return entity;
        }

        private Entity CreateWorker(float3 position)
        {
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            EntityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Gather,
                Priority = InterruptPriority.Low,
                IsValid = 1
            });

            EntityManager.AddBuffer<EntityRelation>(entity);
            return entity;
        }

        private void AddRelation(Entity owner, Entity other, sbyte intensity)
        {
            var buffer = EntityManager.GetBuffer<EntityRelation>(owner);
            buffer.Add(new EntityRelation
            {
                OtherEntity = other,
                Type = intensity >= 0 ? RelationType.Friend : RelationType.Enemy,
                Intensity = intensity,
                InteractionCount = 5,
                FirstMetTick = 0,
                LastInteractionTick = 0,
                Trust = 50,
                Familiarity = 10,
                Respect = 40,
                Fear = 0
            });
        }

        private void EnsureTimeState()
        {
            if (!EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).IsEmpty)
            {
                return;
            }

            var timeEntity = EntityManager.CreateEntity(typeof(TimeState), typeof(TickTimeState));
            EntityManager.SetComponentData(timeEntity, new TimeState
            {
                Tick = 1,
                FixedDeltaTime = 1f / 60f,
                DeltaTime = 1f / 60f,
                DeltaSeconds = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                ElapsedTime = 1f / 60f,
                WorldSeconds = 1f / 60f,
                IsPaused = false
            });
            EntityManager.SetComponentData(timeEntity, new TickTimeState
            {
                Tick = 1,
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                TargetTick = 1,
                IsPaused = false,
                IsPlaying = true,
                WorldSeconds = 1f / 60f
            });
        }

        private void EnsureRewindState()
        {
            if (!EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).IsEmpty)
            {
                return;
            }

            var rewindEntity = EntityManager.CreateEntity(typeof(RewindState), typeof(RewindLegacyState));
            EntityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record
            });
            EntityManager.SetComponentData(rewindEntity, new RewindLegacyState
            {
                CurrentTick = 1,
                StartTick = 1,
                PlaybackTick = 1,
                PlaybackSpeed = 1f,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = ScrubDirection.None,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });
        }
    }
}
#endif

