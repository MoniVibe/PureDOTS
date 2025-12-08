using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Caching
{
    /// <summary>
    /// Burst-safe ring-buffer cache keyed by uint hashes. Capacity is fixed; oldest entries are evicted on overflow.
    /// </summary>
    [BurstCompile]
    public struct ResultCache<T> where T : unmanaged
    {
        private NativeArray<uint> _hashes;
        private NativeArray<T> _values;
        private int _capacity;
        private int _count;
        private int _head;
        private bool _created;

        public ResultCache(int capacity, Allocator allocator)
        {
            _capacity = math.max(1, capacity);
            _hashes = new NativeArray<uint>(_capacity, allocator, NativeArrayOptions.ClearMemory);
            _values = new NativeArray<T>(_capacity, allocator, NativeArrayOptions.ClearMemory);
            _count = 0;
            _head = 0;
            _created = true;
        }

        public bool IsCreated => _created && _hashes.IsCreated && _values.IsCreated;

        [BurstCompile]
        public void Dispose()
        {
            if (!IsCreated) return;
            if (_hashes.IsCreated) _hashes.Dispose();
            if (_values.IsCreated) _values.Dispose();
            _created = false;
            _count = 0;
            _head = 0;
        }

        /// <summary>
        /// Try to get a cached value for the hash.
        /// </summary>
        [BurstCompile]
        public bool TryGet(uint hash, out T value)
        {
            if (!_created)
            {
                value = default;
                return false;
            }

            // Linear probe is fine for small capacities (intended for hot-path caches with modest size).
            for (int i = 0; i < _count; i++)
            {
                if (_hashes[i] == hash)
                {
                    value = _values[i];
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Store a value for the hash, evicting oldest if full.
        /// </summary>
        [BurstCompile]
        public void Store(uint hash, in T value)
        {
            if (!_created) return;

            // Update existing
            for (int i = 0; i < _count; i++)
            {
                if (_hashes[i] == hash)
                {
                    _values[i] = value;
                    return;
                }
            }

            // Insert at head (ring)
            _hashes[_head] = hash;
            _values[_head] = value;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity) _count++;
        }

        /// <summary>
        /// Clear all entries.
        /// </summary>
        [BurstCompile]
        public void Clear()
        {
            if (!_created) return;
            _hashes.Clear();
            _values.Clear();
            _count = 0;
            _head = 0;
        }

        /// <summary>
        /// Combine two hashes into one (FNV-1a inspired).
        /// </summary>
        [BurstCompile]
        public static uint CombineHashes(uint a, uint b)
        {
            uint hash = 2166136261;
            hash = (hash ^ a) * 16777619;
            hash = (hash ^ b) * 16777619;
            return hash;
        }

        [BurstCompile]
        public static uint ComputeHash(int value)
        {
            int mix = value ^ unchecked((int)0x9E3779B9u);
            return math.hash(new int2(value, mix));
        }

        [BurstCompile]
        public static uint ComputeHash(uint value) => math.hash(new uint2(value, value ^ 0x85EBCA6Bu));

        [BurstCompile]
        public static uint ComputeHash(float value)
        {
            // math.hash(float) is ambiguous in newer math versions; explicitly hash a uint2
            uint bits = math.asuint(value);
            return math.hash(new uint2(bits, 0u));
        }

        [BurstCompile]
        public static uint ComputeHash(float2 value)
        {
            // math.hash(float2) is overloaded; pass explicit float2 to avoid float2x2 ambiguity
            var key = new float2(value.x, value.y);
            return math.hash(key);
        }

        [BurstCompile]
        public static uint ComputeHash(float3 value)
        {
            var key = new float3(value.x, value.y, value.z);
            return math.hash(key);
        }

        [BurstCompile]
        public static uint ComputeHash(bool value) => value ? 0xA341316Cu : 0xF27FCE1Fu;
    }
}
