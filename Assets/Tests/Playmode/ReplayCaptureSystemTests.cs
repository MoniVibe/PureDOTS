using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Tests
{
    public class ReplayCaptureSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private ReplayCaptureSystem _captureSystem;

        [SetUp]
        public void SetUp()
        {
            _world = new World("ReplayCaptureTests");
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
            _captureSystem = _world.CreateSystemManaged<ReplayCaptureSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void RecordEvent_PublishesReplayBuffer()
        {
            var label = new FixedString64Bytes("TestEvent");
            ReplayCaptureSystem.RecordEvent(_world, ReplayableEvent.EventType.Custom, 12u, label, 5f);
            _captureSystem.Update();

            var streamEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ReplayCaptureStream>()).GetSingletonEntity();
            var buffer = _entityManager.GetBuffer<ReplayCaptureEvent>(streamEntity);
            Assert.AreEqual(1, buffer.Length);

            var evt = buffer[0];
            Assert.AreEqual(12u, evt.Tick);
            Assert.AreEqual(ReplayableEvent.EventType.Custom, evt.Type);
            Assert.AreEqual(label.ToString(), evt.Label.ToString());
            Assert.AreEqual(5f, evt.Value);

            var stream = _entityManager.GetComponentData<ReplayCaptureStream>(streamEntity);
            Assert.AreEqual(1, stream.EventCount);
            Assert.AreEqual(label.ToString(), stream.LastEventLabel.ToString());
        }
    }
}
