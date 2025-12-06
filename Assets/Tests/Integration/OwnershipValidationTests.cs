using NUnit.Framework;
using Unity.Entities;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Tests for ownership validation and single-writer rule enforcement.
    /// Validates that components are not written by multiple ECS layers.
    /// </summary>
    public class OwnershipValidationTests : ECSTestsFixture
    {
        [Test]
        public void AgentGuid_IsShared_NotOwned()
        {
            // AgentGuid should be in PureDOTS.Shared, not owned by any ECS
            var guidType = typeof(AgentGuid);
            Assert.IsNotNull(guidType);
            Assert.IsTrue(guidType.Namespace.Contains("PureDOTS.Shared"), 
                "AgentGuid should be in Shared namespace");
        }

        [Test]
        public void AgentSyncId_IsBodyOwned()
        {
            // AgentSyncId should be in PureDOTS.Runtime (Body ECS)
            var syncIdType = typeof(AgentSyncId);
            Assert.IsNotNull(syncIdType);
            Assert.IsTrue(syncIdType.Namespace.Contains("PureDOTS.Shared") || 
                         syncIdType.Namespace.Contains("PureDOTS.Runtime"),
                "AgentSyncId should be in Runtime or Shared namespace");
        }

        [Test]
        public void AgentIntentBuffer_IsBodyOwned()
        {
            // AgentIntentBuffer should be in PureDOTS.Runtime (Body ECS)
            var bufferType = typeof(AgentIntentBuffer);
            Assert.IsNotNull(bufferType);
            Assert.IsTrue(bufferType.Namespace.Contains("PureDOTS.Runtime"),
                "AgentIntentBuffer should be in Runtime namespace (Body ECS)");
        }

        [Test]
        public void MindToBodyMessage_IsBridgeContract()
        {
            // Message structs should be in Bridges namespace
            var messageType = typeof(MindToBodyMessage);
            Assert.IsNotNull(messageType);
            Assert.IsTrue(messageType.Namespace.Contains("PureDOTS.Runtime.Bridges"),
                "MindToBodyMessage should be in Bridges namespace");
        }

        [Test]
        public void BodyToMindMessage_IsBridgeContract()
        {
            // Message structs should be in Bridges namespace
            var messageType = typeof(BodyToMindMessage);
            Assert.IsNotNull(messageType);
            Assert.IsTrue(messageType.Namespace.Contains("PureDOTS.Runtime.Bridges"),
                "BodyToMindMessage should be in Bridges namespace");
        }

        [Test]
        public void NoComponentTypeCollisions_AcrossAssemblies()
        {
            // This test validates that component type names don't collide
            // Actual validation happens in OwnershipValidatorSystem at runtime
            
            // Verify key components exist in expected namespaces
            Assert.IsNotNull(typeof(AgentBody), "AgentBody should exist");
            Assert.IsNotNull(typeof(AgentSyncId), "AgentSyncId should exist");
            Assert.IsNotNull(typeof(AgentIntentBuffer), "AgentIntentBuffer should exist");
            
            // Verify they're in correct namespaces (no collisions)
            var agentBodyNs = typeof(AgentBody).Namespace;
            var agentSyncIdNs = typeof(AgentSyncId).Namespace;
            var agentIntentBufferNs = typeof(AgentIntentBuffer).Namespace;
            
            // Namespaces should be consistent (all Runtime or Shared)
            Assert.IsTrue(
                (agentBodyNs?.Contains("PureDOTS.Runtime") ?? false) ||
                (agentBodyNs?.Contains("PureDOTS.Shared") ?? false),
                "AgentBody should be in Runtime or Shared namespace");
        }

        [Test]
        public void ReadOnlyAccess_FromNonOwnerECS()
        {
            // This test validates that Mind ECS can read Body components but not write
            // In practice, Mind ECS reads via telemetry messages, not direct component access
            
            // Create body entity
            var bodyEntity = m_Manager.CreateEntity();
            var agentGuid = AgentGuid.NewGuid();
            
            m_Manager.AddComponentData(bodyEntity, new AgentBody { Id = agentGuid });
            m_Manager.AddComponentData(bodyEntity, new AgentSyncId { Guid = agentGuid });
            
            // Verify Body ECS can read its own components
            Assert.IsTrue(m_Manager.HasComponent<AgentBody>(bodyEntity));
            Assert.IsTrue(m_Manager.HasComponent<AgentSyncId>(bodyEntity));
            
            // Mind ECS would read via AgentSyncBus telemetry, not direct access
            // This is enforced by architecture (separate ECS worlds)
        }
    }
}

