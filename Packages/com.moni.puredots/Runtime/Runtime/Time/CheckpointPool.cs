using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Memory pool for checkpoint storage to reduce allocations.
    /// Reuses chunk memory blocks: Allocator.TempJob → Allocator.Persistent ring buffer.
    /// Copies only dirty component bytes.
    /// Target: 1M entities, 300MB active → ~30MB per 10s slice.
    /// </summary>
    public struct CheckpointPool : System.IDisposable
    {
        private NativeList<NativeArray<byte>> _pooledBuffers;
        private NativeList<int> _availableSizes;
        private int _maxPoolSize;
        private long _totalAllocatedBytes;
        private long _totalPooledBytes;

        public bool IsCreated => _pooledBuffers.IsCreated;

        public CheckpointPool(int maxPoolSize, Allocator allocator)
        {
            _pooledBuffers = new NativeList<NativeArray<byte>>(maxPoolSize, allocator);
            _availableSizes = new NativeList<int>(maxPoolSize, allocator);
            _maxPoolSize = maxPoolSize;
            _totalAllocatedBytes = 0L;
            _totalPooledBytes = 0L;
        }

        public void Dispose()
        {
            if (_pooledBuffers.IsCreated)
            {
                for (int i = 0; i < _pooledBuffers.Length; i++)
                {
                    if (_pooledBuffers[i].IsCreated)
                    {
                        _pooledBuffers[i].Dispose();
                    }
                }
                _pooledBuffers.Dispose();
            }
            if (_availableSizes.IsCreated)
            {
                _availableSizes.Dispose();
            }
        }

        /// <summary>
        /// Get a buffer from the pool, or allocate a new one if none available.
        /// </summary>
        public NativeArray<byte> GetBuffer(int size, Allocator allocator)
        {
            // Try to find a suitable buffer in the pool
            for (int i = 0; i < _pooledBuffers.Length; i++)
            {
                if (_availableSizes[i] >= size && _pooledBuffers[i].IsCreated)
                {
                    // Found a suitable buffer - mark as used
                    var buffer = _pooledBuffers[i];
                    _pooledBuffers.RemoveAtSwapBack(i);
                    _availableSizes.RemoveAtSwapBack(i);
                    _totalPooledBytes -= buffer.Length;
                    return buffer;
                }
            }

            // No suitable buffer found - allocate new one
            var newBuffer = new NativeArray<byte>(size, allocator, NativeArrayOptions.UninitializedMemory);
            _totalAllocatedBytes += size;
            return newBuffer;
        }

        /// <summary>
        /// Return a buffer to the pool for reuse.
        /// </summary>
        public void ReturnBuffer(NativeArray<byte> buffer)
        {
            if (!buffer.IsCreated)
            {
                return;
            }

            // Don't pool if we're at capacity
            if (_pooledBuffers.Length >= _maxPoolSize)
            {
                buffer.Dispose();
                return;
            }

            // Add to pool
            _pooledBuffers.Add(buffer);
            _availableSizes.Add(buffer.Length);
            _totalPooledBytes += buffer.Length;
        }

        /// <summary>
        /// Clear the pool, disposing all buffers.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _pooledBuffers.Length; i++)
            {
                if (_pooledBuffers[i].IsCreated)
                {
                    _pooledBuffers[i].Dispose();
                }
            }
            _pooledBuffers.Clear();
            _availableSizes.Clear();
            _totalPooledBytes = 0L;
        }

        /// <summary>
        /// Get pool statistics.
        /// </summary>
        public void GetStats(out int pooledCount, out long pooledBytes, out long allocatedBytes)
        {
            pooledCount = _pooledBuffers.Length;
            pooledBytes = _totalPooledBytes;
            allocatedBytes = _totalAllocatedBytes;
        }

        /// <summary>
        /// Copy only dirty bytes from source to destination.
        /// Uses XOR to detect changes.
        /// </summary>
        [BurstCompile]
        public static unsafe void CopyDirtyBytes(
            NativeArray<byte> source,
            NativeArray<byte> destination,
            NativeArray<byte> previous,
            out int dirtyByteCount)
        {
            dirtyByteCount = 0;

            if (!source.IsCreated || !destination.IsCreated || source.Length != destination.Length)
            {
                return;
            }

            byte* srcPtr = (byte*)source.GetUnsafePtr();
            byte* dstPtr = (byte*)destination.GetUnsafePtr();
            byte* prevPtr = previous.IsCreated ? (byte*)previous.GetUnsafePtr() : null;

            if (prevPtr != null && previous.Length == source.Length)
            {
                // Copy only changed bytes (XOR diff)
                for (int i = 0; i < source.Length; i++)
                {
                    byte delta = (byte)(srcPtr[i] ^ prevPtr[i]);
                    if (delta != 0)
                    {
                        dstPtr[i] = srcPtr[i];
                        dirtyByteCount++;
                    }
                    else
                    {
                        // Unchanged - keep destination as-is
                    }
                }
            }
            else
            {
                // No previous state - copy all
                UnsafeUtility.MemCpy(dstPtr, srcPtr, source.Length);
                dirtyByteCount = source.Length;
            }
        }

        /// <summary>
        /// Apply dirty bytes to restore state.
        /// </summary>
        [BurstCompile]
        public static unsafe void ApplyDirtyBytes(
            NativeArray<byte> dirtyBytes,
            NativeArray<byte> currentState,
            NativeArray<byte> targetState)
        {
            if (!dirtyBytes.IsCreated || !currentState.IsCreated || !targetState.IsCreated)
            {
                return;
            }

            if (currentState.Length != targetState.Length)
            {
                return;
            }

            byte* dirtyPtr = (byte*)dirtyBytes.GetUnsafePtr();
            byte* currentPtr = (byte*)currentState.GetUnsafePtr();
            byte* targetPtr = (byte*)targetState.GetUnsafePtr();

            // Apply XOR delta: target = current ^ dirty
            for (int i = 0; i < currentState.Length; i++)
            {
                targetPtr[i] = (byte)(currentPtr[i] ^ dirtyPtr[i]);
            }
        }
    }

    /// <summary>
    /// Ring buffer for checkpoint storage with memory pooling.
    /// </summary>
    public struct CheckpointRingBuffer : System.IDisposable
    {
        private CheckpointPool _pool;
        private NativeList<NativeArray<byte>> _checkpoints;
        private NativeList<uint> _checkpointTicks;
        private int _capacity;
        private int _writeIndex;

        public bool IsCreated => _checkpoints.IsCreated;

        public CheckpointRingBuffer(int capacity, int poolSize, Allocator allocator)
        {
            _pool = new CheckpointPool(poolSize, allocator);
            _checkpoints = new NativeList<NativeArray<byte>>(capacity, allocator);
            _checkpointTicks = new NativeList<uint>(capacity, allocator);
            _capacity = capacity;
            _writeIndex = 0;
        }

        public void Dispose()
        {
            if (_checkpoints.IsCreated)
            {
                for (int i = 0; i < _checkpoints.Length; i++)
                {
                    if (_checkpoints[i].IsCreated)
                    {
                        _checkpoints[i].Dispose();
                    }
                }
                _checkpoints.Dispose();
            }
            if (_checkpointTicks.IsCreated)
            {
                _checkpointTicks.Dispose();
            }
            _pool.Dispose();
        }

        /// <summary>
        /// Store checkpoint data, reusing memory from pool.
        /// </summary>
        public void StoreCheckpoint(uint tick, NativeArray<byte> data, Allocator allocator)
        {
            // Get buffer from pool
            var buffer = _pool.GetBuffer(data.Length, allocator);

            // Resize if needed
            if (buffer.Length != data.Length)
            {
                if (buffer.IsCreated)
                {
                    buffer.Dispose();
                }
                buffer = new NativeArray<byte>(data.Length, allocator, NativeArrayOptions.UninitializedMemory);
            }

            // Copy data
            NativeArray<byte>.Copy(data, buffer);

            // Store in ring buffer
            if (_writeIndex < _checkpoints.Length)
            {
                // Reuse existing slot
                if (_checkpoints[_writeIndex].IsCreated)
                {
                    _pool.ReturnBuffer(_checkpoints[_writeIndex]);
                }
                _checkpoints[_writeIndex] = buffer;
                _checkpointTicks[_writeIndex] = tick;
            }
            else
            {
                // Add new slot
                _checkpoints.Add(buffer);
                _checkpointTicks.Add(tick);
            }

            _writeIndex = (_writeIndex + 1) % _capacity;
        }

        /// <summary>
        /// Get checkpoint for specified tick.
        /// </summary>
        public bool TryGetCheckpoint(uint tick, out NativeArray<byte> data)
        {
            data = default;

            for (int i = 0; i < _checkpointTicks.Length; i++)
            {
                if (_checkpointTicks[i] == tick)
                {
                    data = _checkpoints[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Prune checkpoints older than minTick, returning buffers to pool.
        /// </summary>
        public void PruneOlderThan(uint minTick)
        {
            for (int i = _checkpointTicks.Length - 1; i >= 0; i--)
            {
                if (_checkpointTicks[i] < minTick)
                {
                    if (_checkpoints[i].IsCreated)
                    {
                        _pool.ReturnBuffer(_checkpoints[i]);
                    }
                    _checkpoints.RemoveAtSwapBack(i);
                    _checkpointTicks.RemoveAtSwapBack(i);
                }
            }
        }
    }
}

