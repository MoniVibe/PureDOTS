using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Telemetry;

namespace PureDOTS.Runtime.Devtools
{
    /// <summary>
    /// Logs performance metrics: mean/max ms per system group, memory allocations, Burst job counts, entity counts per archetype.
    /// </summary>
    [BurstCompile]
    public struct BenchmarkMetrics
    {
        public NativeHashMap<FixedString64Bytes, float> MeanMsPerGroup;
        public NativeHashMap<FixedString64Bytes, float> MaxMsPerGroup;
        public NativeHashMap<FixedString64Bytes, long> MemoryAllocations;
        public NativeHashMap<FixedString64Bytes, int> BurstJobCounts;
        public NativeHashMap<FixedString64Bytes, int> EntityCountsPerArchetype;
        private Allocator _allocator;

        public BenchmarkMetrics(Allocator allocator)
        {
            MeanMsPerGroup = new NativeHashMap<FixedString64Bytes, float>(32, allocator);
            MaxMsPerGroup = new NativeHashMap<FixedString64Bytes, float>(32, allocator);
            MemoryAllocations = new NativeHashMap<FixedString64Bytes, long>(32, allocator);
            BurstJobCounts = new NativeHashMap<FixedString64Bytes, int>(32, allocator);
            EntityCountsPerArchetype = new NativeHashMap<FixedString64Bytes, int>(64, allocator);
            _allocator = allocator;
        }

        [BurstCompile]
        public void RecordSystemGroupTime(FixedString64Bytes groupName, float ms)
        {
            if (MeanMsPerGroup.TryGetValue(groupName, out float mean))
            {
                MeanMsPerGroup[groupName] = (mean + ms) * 0.5f; // Running average
            }
            else
            {
                MeanMsPerGroup[groupName] = ms;
            }

            if (MaxMsPerGroup.TryGetValue(groupName, out float max))
            {
                if (ms > max)
                {
                    MaxMsPerGroup[groupName] = ms;
                }
            }
            else
            {
                MaxMsPerGroup[groupName] = ms;
            }
        }

        public void Dispose()
        {
            if (MeanMsPerGroup.IsCreated)
                MeanMsPerGroup.Dispose();
            if (MaxMsPerGroup.IsCreated)
                MaxMsPerGroup.Dispose();
            if (MemoryAllocations.IsCreated)
                MemoryAllocations.Dispose();
            if (BurstJobCounts.IsCreated)
                BurstJobCounts.Dispose();
            if (EntityCountsPerArchetype.IsCreated)
                EntityCountsPerArchetype.Dispose();
        }
    }
}

