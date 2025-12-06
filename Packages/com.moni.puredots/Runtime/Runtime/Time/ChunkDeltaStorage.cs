using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Delta encoding for a single chunk's changed components.
    /// Stores only the bytes that changed between ticks.
    /// </summary>
    public struct ChunkDelta : System.IDisposable
    {
        /// <summary>Archetype identifier (hash of component types).</summary>
        public int ArchetypeId;
        /// <summary>Number of entities in this chunk.</summary>
        public int EntityCount;
        /// <summary>Changed component bytes (XOR diff from previous tick).</summary>
        public NativeArray<byte> ChangedBytes;
        /// <summary>Tick this delta was recorded at.</summary>
        public uint Tick;
        /// <summary>Component type indices that changed.</summary>
        public NativeArray<int> ChangedComponentIndices;
        /// <summary>Byte offsets for each changed component type.</summary>
        public NativeArray<int> ComponentOffsets;
        /// <summary>Byte sizes for each changed component type.</summary>
        public NativeArray<int> ComponentSizes;

        public bool IsCreated => ChangedBytes.IsCreated;

        public ChunkDelta(int archetypeId, int entityCount, uint tick, Allocator allocator)
        {
            ArchetypeId = archetypeId;
            EntityCount = entityCount;
            Tick = tick;
            ChangedBytes = default;
            ChangedComponentIndices = default;
            ComponentOffsets = default;
            ComponentSizes = default;
        }

        public void Dispose()
        {
            if (ChangedBytes.IsCreated)
            {
                ChangedBytes.Dispose();
            }
            if (ChangedComponentIndices.IsCreated)
            {
                ChangedComponentIndices.Dispose();
            }
            if (ComponentOffsets.IsCreated)
            {
                ComponentOffsets.Dispose();
            }
            if (ComponentSizes.IsCreated)
            {
                ComponentSizes.Dispose();
            }
        }
    }

    /// <summary>
    /// Storage for chunk deltas in a ring buffer.
    /// Stores delta-encoded state pages instead of full snapshots.
    /// </summary>
    public struct ChunkDeltaStorage : System.IDisposable
    {
        private NativeList<ChunkDelta> _deltas;
        private NativeHashMap<uint, int> _tickToDeltaIndex;
        private int _maxDeltas;
        private uint _oldestTick;
        private uint _newestTick;

        public bool IsCreated => _deltas.IsCreated;

        /// <summary>
        /// Create delta storage with capacity for specified number of ticks.
        /// </summary>
        public ChunkDeltaStorage(int maxDeltas, Allocator allocator)
        {
            _deltas = new NativeList<ChunkDelta>(maxDeltas, allocator);
            _tickToDeltaIndex = new NativeHashMap<uint, int>(maxDeltas * 2, allocator);
            _maxDeltas = maxDeltas;
            _oldestTick = 0u;
            _newestTick = 0u;
        }

        public void Dispose()
        {
            if (_deltas.IsCreated)
            {
                for (int i = 0; i < _deltas.Length; i++)
                {
                    _deltas[i].Dispose();
                }
                _deltas.Dispose();
            }
            if (_tickToDeltaIndex.IsCreated)
            {
                _tickToDeltaIndex.Dispose();
            }
        }

        /// <summary>
        /// Add a delta for a specific tick.
        /// </summary>
        public void AddDelta(uint tick, ChunkDelta delta)
        {
            // Prune old deltas if at capacity
            if (_deltas.Length >= _maxDeltas && _oldestTick < tick)
            {
                PruneOlderThan(tick - (uint)_maxDeltas);
            }

            int index = _deltas.Length;
            _deltas.Add(delta);
            _tickToDeltaIndex[tick] = index;

            if (_oldestTick == 0u || tick < _oldestTick)
            {
                _oldestTick = tick;
            }
            if (tick > _newestTick)
            {
                _newestTick = tick;
            }
        }

        /// <summary>
        /// Get delta for a specific tick, or null if not found.
        /// </summary>
        public bool TryGetDelta(uint tick, out ChunkDelta delta)
        {
            delta = default;
            if (!_tickToDeltaIndex.TryGetValue(tick, out int index))
            {
                return false;
            }

            if (index < 0 || index >= _deltas.Length)
            {
                return false;
            }

            delta = _deltas[index];
            return true;
        }

        /// <summary>
        /// Find nearest delta at or before target tick.
        /// </summary>
        public bool TryGetNearestDelta(uint targetTick, out ChunkDelta delta, out uint actualTick)
        {
            delta = default;
            actualTick = 0u;

            if (_deltas.Length == 0)
            {
                return false;
            }

            // Binary search for nearest tick <= targetTick
            uint bestTick = 0u;
            int bestIndex = -1;

            for (int i = 0; i < _deltas.Length; i++)
            {
                var candidate = _deltas[i];
                if (candidate.Tick <= targetTick && candidate.Tick > bestTick)
                {
                    bestTick = candidate.Tick;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                delta = _deltas[bestIndex];
                actualTick = bestTick;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Prune deltas older than minTick.
        /// </summary>
        public void PruneOlderThan(uint minTick)
        {
            if (_deltas.Length == 0)
            {
                return;
            }

            var newDeltas = new NativeList<ChunkDelta>(_maxDeltas, Allocator.Temp);
            var newTickMap = new NativeHashMap<uint, int>(_maxDeltas * 2, Allocator.Temp);

            for (int i = 0; i < _deltas.Length; i++)
            {
                var delta = _deltas[i];
                if (delta.Tick >= minTick)
                {
                    int newIndex = newDeltas.Length;
                    newDeltas.Add(delta);
                    newTickMap[delta.Tick] = newIndex;
                }
                else
                {
                    delta.Dispose();
                }
            }

            // Replace old storage
            for (int i = 0; i < _deltas.Length; i++)
            {
                if (_deltas[i].Tick < minTick)
                {
                    // Already disposed above
                    continue;
                }
            }

            _deltas.Clear();
            _tickToDeltaIndex.Clear();

            for (int i = 0; i < newDeltas.Length; i++)
            {
                _deltas.Add(newDeltas[i]);
            }

            foreach (var kvp in newTickMap)
            {
                _tickToDeltaIndex[kvp.Key] = kvp.Value;
            }

            if (newDeltas.Length > 0)
            {
                _oldestTick = minTick;
            }

            newDeltas.Dispose();
            newTickMap.Dispose();
        }

        /// <summary>
        /// Get the oldest tick stored.
        /// </summary>
        public uint GetOldestTick() => _oldestTick;

        /// <summary>
        /// Get the newest tick stored.
        /// </summary>
        public uint GetNewestTick() => _newestTick;

        /// <summary>
        /// Get number of stored deltas.
        /// </summary>
        public int GetDeltaCount() => _deltas.Length;
    }
}

