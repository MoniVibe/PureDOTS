using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;

namespace PureDOTS.Tests.Runtime.Time
{
    /// <summary>
    /// Tests for deterministic rewind behavior.
    /// CI test: RunScenario --ticks 10000 --rewind 600 --verify-hash
    /// Compare component hashes before/after rewind → must match bit-for-bit.
    /// </summary>
    public class RewindDeterminismTests
    {
        private World _testWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("TestWorld");
            _entityManager = _testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testWorld != null && _testWorld.IsCreated)
            {
                _testWorld.Dispose();
            }
        }

        [Test]
        public void DeterministicRNG_ProducesSameResults()
        {
            uint entityId = 123u;
            uint tick = 456u;

            uint4 seed1 = DeterministicRNG.GenerateSeed(entityId, tick);
            uint4 seed2 = DeterministicRNG.GenerateSeed(entityId, tick);

            Assert.AreEqual(seed1.x, seed2.x);
            Assert.AreEqual(seed1.y, seed2.y);
            Assert.AreEqual(seed1.z, seed2.z);
            Assert.AreEqual(seed1.w, seed2.w);

            uint counter = 0u;
            uint4 result1 = DeterministicRNG.Philox(seed1, counter);
            uint4 result2 = DeterministicRNG.Philox(seed2, counter);

            Assert.AreEqual(result1.x, result2.x);
            Assert.AreEqual(result1.y, result2.y);
            Assert.AreEqual(result1.z, result2.z);
            Assert.AreEqual(result1.w, result2.w);
        }

        [Test]
        public void TemporalBuffer_StoresAndRetrievesValues()
        {
            var buffer = new TemporalBuffer<int>(10, Allocator.Temp);

            buffer.Add(100u, 42);
            buffer.Add(200u, 84);
            buffer.Add(300u, 126);

            Assert.IsTrue(buffer.TryGetExact(200u, out int value));
            Assert.AreEqual(84, value);

            Assert.IsTrue(buffer.TryGetNearest(250u, out int nearestValue, out uint actualTick));
            Assert.AreEqual(84, nearestValue);
            Assert.AreEqual(200u, actualTick);

            buffer.Dispose();
        }

        [Test]
        public void EventLog_AppendsAndTrimsCorrectly()
        {
            var eventLog = new EventLog(100, Allocator.Temp);

            var payload = new FixedBytes64();
            eventLog.Append(1, 100u, payload);
            eventLog.Append(2, 200u, payload);
            eventLog.Append(3, 300u, payload);

            Assert.AreEqual(3, eventLog.EventCount);

            eventLog.TrimToTick(200u);
            Assert.AreEqual(2, eventLog.EventCount);

            var events = eventLog.GetEventsUpToTick(200u, Allocator.Temp);
            Assert.AreEqual(2, events.Length);
            events.Dispose();

            eventLog.Dispose();
        }

        [Test]
        public void ChunkDeltaStorage_StoresAndRetrievesDeltas()
        {
            var storage = new ChunkDeltaStorage(100, Allocator.Temp);

            var delta1 = new ChunkDelta(1, 10, 100u, Allocator.Temp);
            var delta2 = new ChunkDelta(2, 20, 200u, Allocator.Temp);

            storage.AddDelta(100u, delta1);
            storage.AddDelta(200u, delta2);

            Assert.IsTrue(storage.TryGetDelta(100u, out ChunkDelta retrieved));
            Assert.AreEqual(1, retrieved.ArchetypeId);
            Assert.AreEqual(10, retrieved.EntityCount);

            Assert.IsTrue(storage.TryGetNearestDelta(150u, out ChunkDelta nearest, out uint actualTick));
            Assert.AreEqual(100u, actualTick);

            delta1.Dispose();
            delta2.Dispose();
            storage.Dispose();
        }

        [Test]
        public void MultiTierCheckpoint_ClassifiesTiersCorrectly()
        {
            float ticksPerSecond = 60f;
            var checkpoint = new MultiTierCheckpoint(ticksPerSecond, Allocator.Temp);

            uint currentTick = 10000u; // ~166 seconds @ 60 TPS

            // Tier 0: 0-10s
            CheckpointTier tier0 = checkpoint.GetTierForTick(9990u, currentTick);
            Assert.AreEqual(CheckpointTier.Tier0_RAM, tier0);

            // Tier 1: 10-120s
            CheckpointTier tier1 = checkpoint.GetTierForTick(9000u, currentTick);
            Assert.AreEqual(CheckpointTier.Tier1_CompressedRAM, tier1);

            // Tier 2: > 120s
            CheckpointTier tier2 = checkpoint.GetTierForTick(1000u, currentTick);
            Assert.AreEqual(CheckpointTier.Tier2_Disk, tier2);

            checkpoint.Dispose();
        }

        // Note: Full rewind determinism test would require:
        // 1. Create world with test entities
        // 2. Run simulation for N ticks
        // 3. Record hash at tick M
        // 4. Rewind to tick M
        // 5. Verify hash matches
        // This is more of an integration test that would be run via CLI:
        // RunScenario --ticks 10000 --rewind 600 --verify-hash
    }
}

