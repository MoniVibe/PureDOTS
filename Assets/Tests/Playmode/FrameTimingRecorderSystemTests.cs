using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Entities;

namespace PureDOTS.Tests
{
    public class FrameTimingRecorderSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private FrameTimingRecorderSystem _recorder;

        [SetUp]
        public void SetUp()
        {
            _world = new World("FrameTimingRecorderTests");
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
            _recorder = _world.CreateSystemManaged<FrameTimingRecorderSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void RecordGroupTiming_PublishesSample()
        {
            _recorder.RecordGroupTiming(FrameTimingGroup.Environment, 1.5f, 3, false);
            _recorder.Update();

            var streamEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<FrameTimingStream>()).GetSingletonEntity();
            var buffer = _entityManager.GetBuffer<FrameTimingSample>(streamEntity);
            Assert.AreEqual(1, buffer.Length);
            var sample = buffer[0];
            Assert.AreEqual(FrameTimingGroup.Environment, sample.Group);
            Assert.AreEqual(1.5f, sample.DurationMs, 0.001f);
            Assert.AreEqual(FrameTimingUtility.GetBudgetMs(FrameTimingGroup.Environment), sample.BudgetMs, 0.001f);
        }

        [Test]
        public void RecordGroupTiming_FlagsBudgetExceeded()
        {
            _recorder.RecordGroupTiming(FrameTimingGroup.Environment, 5f, 2, false);
            _recorder.Update();

            var streamEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<FrameTimingStream>()).GetSingletonEntity();
            var sample = _entityManager.GetBuffer<FrameTimingSample>(streamEntity)[0];
            Assert.IsTrue((sample.Flags & FrameTimingFlags.BudgetExceeded) != 0);
        }

        [Test]
        public void Update_WritesAllocationDiagnostics()
        {
            _recorder.Update();

            var streamEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<FrameTimingStream>()).GetSingletonEntity();
            var allocation = _entityManager.GetComponentData<AllocationDiagnostics>(streamEntity);

            Assert.GreaterOrEqual(allocation.TotalAllocatedBytes, 0);
            Assert.GreaterOrEqual(allocation.TotalReservedBytes, 0);
            Assert.GreaterOrEqual(allocation.GcCollectionsGeneration0, 0);
            Assert.GreaterOrEqual(allocation.GcCollectionsGeneration1, 0);
            Assert.GreaterOrEqual(allocation.GcCollectionsGeneration2, 0);
        }
    }
}
