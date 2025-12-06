using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Temporal ring buffer for component history.
    /// Stores component values keyed by tick index.
    /// Binary-search nearest tick ≤ target for restore.
    /// </summary>
    public struct TemporalBuffer<T> : System.IDisposable where T : unmanaged
    {
        private NativeArray<T> _values;
        private NativeArray<uint> _ticks;
        private int _writeIndex;
        private int _count;
        private int _capacity;

        public bool IsCreated => _values.IsCreated && _ticks.IsCreated;
        public int Count => _count;
        public int Capacity => _capacity;

        public TemporalBuffer(int capacity, Allocator allocator)
        {
            _capacity = capacity;
            _values = new NativeArray<T>(capacity, allocator, NativeArrayOptions.UninitializedMemory);
            _ticks = new NativeArray<uint>(capacity, allocator, NativeArrayOptions.UninitializedMemory);
            _writeIndex = 0;
            _count = 0;
        }

        public void Dispose()
        {
            if (_values.IsCreated)
            {
                _values.Dispose();
            }
            if (_ticks.IsCreated)
            {
                _ticks.Dispose();
            }
        }

        /// <summary>
        /// Add a value at the specified tick.
        /// Overwrites oldest entry if at capacity.
        /// </summary>
        public void Add(uint tick, T value)
        {
            _values[_writeIndex] = value;
            _ticks[_writeIndex] = tick;
            _writeIndex = (_writeIndex + 1) % _capacity;

            if (_count < _capacity)
            {
                _count++;
            }
        }

        /// <summary>
        /// Find nearest value at or before target tick using binary search.
        /// </summary>
        [BurstCompile]
        public bool TryGetNearest(uint targetTick, out T value, out uint actualTick)
        {
            value = default;
            actualTick = 0u;

            if (_count == 0)
            {
                return false;
            }

            // Create sorted list for binary search
            var sortedIndices = new NativeList<int>(_count, Allocator.Temp);
            var sortedTicks = new NativeList<uint>(_count, Allocator.Temp);

            // Collect all valid entries
            for (int i = 0; i < _count; i++)
            {
                int idx = (_writeIndex - _count + i + _capacity) % _capacity;
                sortedIndices.Add(idx);
                sortedTicks.Add(_ticks[idx]);
            }

            // Binary search for nearest tick <= targetTick
            int bestIndex = -1;
            uint bestTick = 0u;

            for (int i = 0; i < sortedTicks.Length; i++)
            {
                uint tick = sortedTicks[i];
                if (tick <= targetTick && tick > bestTick)
                {
                    bestTick = tick;
                    bestIndex = sortedIndices[i];
                }
            }

            if (bestIndex >= 0)
            {
                value = _values[bestIndex];
                actualTick = bestTick;
                sortedIndices.Dispose();
                sortedTicks.Dispose();
                return true;
            }

            sortedIndices.Dispose();
            sortedTicks.Dispose();
            return false;
        }

        /// <summary>
        /// Get value at exact tick, or false if not found.
        /// </summary>
        [BurstCompile]
        public bool TryGetExact(uint tick, out T value)
        {
            value = default;

            for (int i = 0; i < _count; i++)
            {
                int idx = (_writeIndex - _count + i + _capacity) % _capacity;
                if (_ticks[idx] == tick)
                {
                    value = _values[idx];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Prune entries older than minTick.
        /// </summary>
        public void PruneOlderThan(uint minTick)
        {
            int validCount = 0;
            int newWriteIndex = 0;

            // Collect valid entries
            var validValues = new NativeList<T>(_count, Allocator.Temp);
            var validTicks = new NativeList<uint>(_count, Allocator.Temp);

            for (int i = 0; i < _count; i++)
            {
                int idx = (_writeIndex - _count + i + _capacity) % _capacity;
                if (_ticks[idx] >= minTick)
                {
                    validValues.Add(_values[idx]);
                    validTicks.Add(_ticks[idx]);
                    validCount++;
                }
            }

            // Rebuild buffer
            _count = validCount;
            _writeIndex = validCount % _capacity;

            for (int i = 0; i < validCount; i++)
            {
                _values[i] = validValues[i];
                _ticks[i] = validTicks[i];
            }

            validValues.Dispose();
            validTicks.Dispose();
        }

        /// <summary>
        /// Clear all entries.
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _writeIndex = 0;
        }
    }

    /// <summary>
    /// Configuration for temporal buffer capacity based on component change rate.
    /// </summary>
    public struct TemporalBufferConfig
    {
        /// <summary>Fast-changing components (Position, Velocity) → 180s.</summary>
        public const int FastChangingCapacity = 10800; // 180s @ 60 TPS

        /// <summary>Slow-changing components (Personality, Stats) → 1h or snapshot only.</summary>
        public const int SlowChangingCapacity = 216000; // 1h @ 60 TPS

        /// <summary>Medium-changing components → 10 minutes.</summary>
        public const int MediumChangingCapacity = 36000; // 10min @ 60 TPS

        /// <summary>
        /// Get capacity for component type based on change rate.
        /// </summary>
        public static int GetCapacity(TemporalChangeRate changeRate)
        {
            return changeRate switch
            {
                TemporalChangeRate.Fast => FastChangingCapacity,
                TemporalChangeRate.Medium => MediumChangingCapacity,
                TemporalChangeRate.Slow => SlowChangingCapacity,
                _ => MediumChangingCapacity
            };
        }
    }

    /// <summary>
    /// Classification of component change rate.
    /// </summary>
    public enum TemporalChangeRate : byte
    {
        /// <summary>Fast-changing: Position, Velocity (180s history).</summary>
        Fast = 0,
        /// <summary>Medium-changing: Health, Energy (10min history).</summary>
        Medium = 1,
        /// <summary>Slow-changing: Personality, Stats (1h or snapshot only).</summary>
        Slow = 2
    }
}

