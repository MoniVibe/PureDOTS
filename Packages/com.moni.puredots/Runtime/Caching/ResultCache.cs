using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Caching
{
    /// <summary>
    /// Burst-safe ring buffer for temporal caching of computation results.
    /// Stores results with input hashes to enable cache hits when inputs are unchanged.
    /// </summary>
    [BurstCompile]
    public struct ResultCache<T> where T : unmanaged
    {
        private NativeArray<T> _buffer;
        private NativeArray<uint> _hashes;
        private int _writeIndex;
        private int _capacity;
        private bool _isCreated;

        /// <summary>
        /// Creates a new result cache with the specified capacity.
        /// </summary>
        public ResultCache(int capacity, Allocator allocator)
        {
            _buffer = new NativeArray<T>(capacity, allocator);
            _hashes = new NativeArray<uint>(capacity, allocator);
            _writeIndex = 0;
            _capacity = capacity;
            _isCreated = true;
        }

        /// <summary>
        /// Disposes the cache buffers.
        /// </summary>
        public void Dispose()
        {
            if (_isCreated)
            {
                if (_buffer.IsCreated)
                    _buffer.Dispose();
                if (_hashes.IsCreated)
                    _hashes.Dispose();
                _isCreated = false;
            }
        }

        /// <summary>
        /// Tries to get a cached result for the given input hash.
        /// Returns true if cache hit, false if cache miss.
        /// </summary>
        public bool TryGet(uint inputHash, out T result)
        {
            if (!_isCreated || _capacity == 0)
            {
                result = default;
                return false;
            }

            // Search backwards from write index (most recent first)
            for (int i = 0; i < _capacity; i++)
            {
                int index = (_writeIndex - 1 - i + _capacity) % _capacity;
                if (_hashes[index] == inputHash)
                {
                    result = _buffer[index];
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Stores a result with the given input hash in the cache.
        /// </summary>
        public void Store(uint inputHash, T result)
        {
            if (!_isCreated || _capacity == 0)
                return;

            _buffer[_writeIndex] = result;
            _hashes[_writeIndex] = inputHash;
            _writeIndex = (_writeIndex + 1) % _capacity;
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public void Clear()
        {
            if (!_isCreated)
                return;

            for (int i = 0; i < _capacity; i++)
            {
                _hashes[i] = 0;
            }
        }

        /// <summary>
        /// Computes a hash from input values for cache key generation.
        /// </summary>
        [BurstCompile]
        public static uint ComputeHash(float value)
        {
            return math.asuint(value);
        }

        /// <summary>
        /// Computes a hash from multiple float values.
        /// </summary>
        [BurstCompile]
        public static uint ComputeHash(float2 value)
        {
            return math.asuint(value.x) ^ (math.asuint(value.y) << 1);
        }

        /// <summary>
        /// Computes a hash from multiple float values.
        /// </summary>
        [BurstCompile]
        public static uint ComputeHash(float3 value)
        {
            return math.asuint(value.x) ^ (math.asuint(value.y) << 1) ^ (math.asuint(value.z) << 2);
        }

        /// <summary>
        /// Computes a hash from multiple float values.
        /// </summary>
        [BurstCompile]
        public static uint ComputeHash(float4 value)
        {
            return math.asuint(value.x) ^ (math.asuint(value.y) << 1) ^ (math.asuint(value.z) << 2) ^ (math.asuint(value.w) << 3);
        }

        /// <summary>
        /// Computes a hash from an integer.
        /// </summary>
        [BurstCompile]
        public static uint ComputeHash(int value)
        {
            return (uint)value;
        }

        /// <summary>
        /// Computes a combined hash from multiple hashes.
        /// </summary>
        [BurstCompile]
        public static uint CombineHashes(uint hash1, uint hash2)
        {
            return hash1 ^ (hash2 << 1);
        }
    }
}

