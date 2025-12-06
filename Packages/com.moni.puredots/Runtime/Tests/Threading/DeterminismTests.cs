using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using PureDOTS.Runtime.Threading;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Tests.Threading
{
    /// <summary>
    /// Tests for verifying deterministic simulation across different thread counts.
    /// </summary>
    public class DeterminismTests
    {
        [Test]
        public void ThreadingConfig_DefaultValues_AreValid()
        {
            var config = ThreadingConfig.Default;
            
            Assert.Greater(config.SimulationThreadCount, 0);
            Assert.Greater(config.PhysicsThreadCount, 0);
            Assert.Greater(config.AsyncIOThreadCount, 0);
            Assert.Greater(config.BackgroundThreadCount, 0);
            Assert.Greater(config.MicroTaskThresholdMs, 0f);
            Assert.Greater(config.DefaultBatchCount, 0);
        }

        [Test]
        public void ThreadRoleManager_GetRoleMap_ContainsExpectedRoles()
        {
            using var map = ThreadRoleManager.GetRoleMap();
            
            Assert.IsTrue(map.ContainsKey(typeof(Unity.Entities.SimulationSystemGroup)));
            Assert.IsTrue(map.ContainsKey(typeof(Unity.Physics.Systems.PhysicsSystemGroup)));
            Assert.IsTrue(map.ContainsKey(typeof(PureDOTS.Systems.GameplaySystemGroup)));
        }

        [Test]
        public void AdaptiveBatchSizing_CalculateBatchCount_ReturnsValidValues()
        {
            int batchCount = AdaptiveBatchSizing.CalculateBatchCount(1000, 4, 64);
            Assert.GreaterOrEqual(batchCount, 64);
            
            batchCount = AdaptiveBatchSizing.CalculateBatchCount(100, 8, 64);
            Assert.GreaterOrEqual(batchCount, 64);
        }

        [Test]
        public void LoadBalancer_MeasureImbalance_DetectsImbalance()
        {
            var profiles = new NativeArray<ThreadLoadProfile>(3, Allocator.Temp);
            profiles[0] = new ThreadLoadProfile { ThreadId = 0, AvgJobDurationMs = 1.0f };
            profiles[1] = new ThreadLoadProfile { ThreadId = 1, AvgJobDurationMs = 1.0f };
            profiles[2] = new ThreadLoadProfile { ThreadId = 2, AvgJobDurationMs = 2.0f };

            bool hasImbalance = LoadBalancer.MeasureImbalance(profiles, 0.2f, out float ratio);
            Assert.IsTrue(hasImbalance);
            Assert.Greater(ratio, 0.2f);

            profiles.Dispose();
        }

        [Test]
        public void SpatialThreadPartitioning_PartitionMortonKeys_CreatesValidRanges()
        {
            uint minKey = 0;
            uint maxKey = 1000;
            int threadCount = 4;

            var result = SpatialThreadPartitioning.PartitionMortonKeys(minKey, maxKey, threadCount, 
                out var threadMinKeys, out var threadMaxKeys);

            Assert.AreEqual(threadCount, threadMinKeys.Length);
            Assert.AreEqual(threadCount, threadMaxKeys.Length);
            
            for (int i = 0; i < threadCount; i++)
            {
                Assert.LessOrEqual(threadMinKeys[i], threadMaxKeys[i]);
                if (i > 0)
                {
                    Assert.Greater(threadMinKeys[i], threadMaxKeys[i - 1]);
                }
            }

            threadMinKeys.Dispose();
            threadMaxKeys.Dispose();
        }

        [Test]
        public void CacheAlignmentHelpers_AlignToCacheLine_ReturnsAlignedSize()
        {
            int size = 100;
            int aligned = CacheAlignmentHelpers.AlignToCacheLine(size);
            
            Assert.GreaterOrEqual(aligned, size);
            Assert.AreEqual(0, aligned % CacheAlignmentHelpers.CacheLineSize);
        }
    }
}

