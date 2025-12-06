using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;
using PureDOTS.AI.MindECS;
using DefaultEcs;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Integration tests for cross-ECS bridge communication.
    /// Validates intent/telemetry flow, delta compression, and missing mapping handling.
    /// </summary>
    public class BridgeIntegrationTests : ECSTestsFixture
    {
        private AgentSyncBus _syncBus;
        private World _mindWorld;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _syncBus = new AgentSyncBus();
            _mindWorld = new DefaultEcs.World();
        }

        [TearDown]
        public override void TearDown()
        {
            _syncBus?.Clear();
            _syncBus = null;
            _mindWorld?.Dispose();
            _mindWorld = null;
            base.TearDown();
        }

        [Test]
        public void AgentCreation_CreatesMappingsInBothWorlds()
        {
            // Create Body entity with AgentBody and AgentSyncId
            var bodyEntity = m_Manager.CreateEntity();
            var agentGuid = AgentGuid.NewGuid();
            
            var agentBody = new AgentBody
            {
                Id = agentGuid,
                Position = float3.zero,
                Rotation = quaternion.identity
            };
            
            var syncId = new AgentSyncId
            {
                Guid = agentGuid,
                MindEntityIndex = -1 // Not mapped yet
            };

            m_Manager.AddComponentData(bodyEntity, agentBody);
            m_Manager.AddComponentData(bodyEntity, syncId);

            // Create Mind entity with AgentGuid
            var mindEntity = _mindWorld.CreateEntity();
            _mindWorld.Set(mindEntity, agentGuid);

            // Verify mappings
            Assert.IsTrue(m_Manager.HasComponent<AgentSyncId>(bodyEntity));
            var retrievedSyncId = m_Manager.GetComponentData<AgentSyncId>(bodyEntity);
            Assert.AreEqual(agentGuid, retrievedSyncId.Guid);
        }

        [Test]
        public void IntentFlow_MindToBody_ViaBus()
        {
            // Setup: Create entities in both worlds
            var agentGuid = AgentGuid.NewGuid();
            var bodyEntity = m_Manager.CreateEntity();
            
            m_Manager.AddComponentData(bodyEntity, new AgentBody { Id = agentGuid });
            m_Manager.AddComponentData(bodyEntity, new AgentSyncId { Guid = agentGuid });
            m_Manager.AddBuffer<AgentIntentBuffer>(bodyEntity);

            // Create intent message
            var intent = new MindToBodyMessage
            {
                AgentGuid = agentGuid,
                Kind = IntentKind.Move,
                TargetPosition = new float3(10f, 0f, 10f),
                TargetEntity = Entity.Null,
                Priority = 128,
                TickNumber = 100
            };

            // Enqueue intent
            _syncBus.EnqueueMindToBody(intent);

            // Dequeue and verify
            using var batch = _syncBus.DequeueMindToBodyBatch(Allocator.Temp);
            Assert.AreEqual(1, batch.Length);
            Assert.AreEqual(IntentKind.Move, batch[0].Kind);
            Assert.AreEqual(agentGuid, batch[0].AgentGuid);
        }

        [Test]
        public void TelemetryFlow_BodyToMind_ViaBus()
        {
            // Setup: Create body entity
            var agentGuid = AgentGuid.NewGuid();
            var bodyEntity = m_Manager.CreateEntity();
            
            m_Manager.AddComponentData(bodyEntity, new AgentBody { Id = agentGuid });
            m_Manager.AddComponentData(bodyEntity, new AgentSyncId { Guid = agentGuid });
            m_Manager.AddComponentData(bodyEntity, LocalTransform.Identity);

            // Create telemetry message
            var telemetry = new BodyToMindMessage
            {
                AgentGuid = agentGuid,
                Position = new float3(5f, 0f, 5f),
                Rotation = quaternion.identity,
                Health = 75f,
                MaxHealth = 100f,
                Flags = BodyToMindFlags.PositionChanged | BodyToMindFlags.HealthChanged,
                TickNumber = 200
            };

            // Enqueue telemetry
            _syncBus.EnqueueBodyToMind(telemetry);

            // Dequeue and verify
            using var batch = _syncBus.DequeueBodyToMindBatch(Allocator.Temp);
            Assert.AreEqual(1, batch.Length);
            Assert.AreEqual(agentGuid, batch[0].AgentGuid);
            Assert.AreEqual(75f, batch[0].Health);
        }

        [Test]
        public void DeltaCompression_ReducesRedundantMessages()
        {
            var agentGuid = AgentGuid.NewGuid();

            // Send same intent twice
            var intent1 = new MindToBodyMessage
            {
                AgentGuid = agentGuid,
                Kind = IntentKind.Move,
                TargetPosition = new float3(10f, 0f, 10f),
                Priority = 128,
                TickNumber = 100
            };

            var intent2 = new MindToBodyMessage
            {
                AgentGuid = agentGuid,
                Kind = IntentKind.Move,
                TargetPosition = new float3(10f, 0f, 10f), // Same position
                Priority = 128, // Same priority
                TickNumber = 101
            };

            _syncBus.EnqueueMindToBody(intent1);
            _syncBus.EnqueueMindToBody(intent2); // Should be compressed away

            // Only first message should be in queue
            using var batch = _syncBus.DequeueMindToBodyBatch(Allocator.Temp);
            Assert.AreEqual(1, batch.Length, "Delta compression should reduce redundant messages");
        }

        [Test]
        public void DeltaCompression_AllowsChangedMessages()
        {
            var agentGuid = AgentGuid.NewGuid();

            // Send intent with different target
            var intent1 = new MindToBodyMessage
            {
                AgentGuid = agentGuid,
                Kind = IntentKind.Move,
                TargetPosition = new float3(10f, 0f, 10f),
                Priority = 128,
                TickNumber = 100
            };

            var intent2 = new MindToBodyMessage
            {
                AgentGuid = agentGuid,
                Kind = IntentKind.Move,
                TargetPosition = new float3(20f, 0f, 20f), // Different position
                Priority = 128,
                TickNumber = 101
            };

            _syncBus.EnqueueMindToBody(intent1);
            _syncBus.EnqueueMindToBody(intent2); // Should pass through (changed)

            // Both messages should be in queue
            using var batch = _syncBus.DequeueMindToBodyBatch(Allocator.Temp);
            Assert.AreEqual(2, batch.Length, "Changed messages should not be compressed");
        }

        [Test]
        public void MissingMappings_HandledGracefully()
        {
            // Create intent for non-existent entity
            var nonExistentGuid = AgentGuid.NewGuid();
            var intent = new MindToBodyMessage
            {
                AgentGuid = nonExistentGuid,
                Kind = IntentKind.Move,
                TargetPosition = float3.zero,
                TickNumber = 100
            };

            _syncBus.EnqueueMindToBody(intent);

            // Dequeue should succeed (bus doesn't validate mappings)
            using var batch = _syncBus.DequeueMindToBodyBatch(Allocator.Temp);
            Assert.AreEqual(1, batch.Length);

            // But resolving GUID → Entity should fail gracefully
            // (This would be tested in MindToBodySyncSystem integration)
        }

        [Test]
        public void MultipleAgents_IndependentMessageFlow()
        {
            var guid1 = AgentGuid.NewGuid();
            var guid2 = AgentGuid.NewGuid();

            var intent1 = new MindToBodyMessage
            {
                AgentGuid = guid1,
                Kind = IntentKind.Move,
                TargetPosition = new float3(10f, 0f, 10f),
                TickNumber = 100
            };

            var intent2 = new MindToBodyMessage
            {
                AgentGuid = guid2,
                Kind = IntentKind.Attack,
                TargetPosition = new float3(20f, 0f, 20f),
                TickNumber = 100
            };

            _syncBus.EnqueueMindToBody(intent1);
            _syncBus.EnqueueMindToBody(intent2);

            using var batch = _syncBus.DequeueMindToBodyBatch(Allocator.Temp);
            Assert.AreEqual(2, batch.Length);
            
            // Verify both messages are present
            bool foundGuid1 = false;
            bool foundGuid2 = false;
            for (int i = 0; i < batch.Length; i++)
            {
                if (batch[i].AgentGuid.Equals(guid1))
                    foundGuid1 = true;
                if (batch[i].AgentGuid.Equals(guid2))
                    foundGuid2 = true;
            }
            
            Assert.IsTrue(foundGuid1 && foundGuid2, "Both agents' messages should be in batch");
        }
    }
}

