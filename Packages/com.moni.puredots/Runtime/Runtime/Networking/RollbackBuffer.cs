using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Generic ring buffer for rewind-rollback integration.
    /// Stores snapshots + deltas for multiplayer rollback.
    /// Each player's input advances speculative ticks; on server correction:
    /// - Load snapshot at confirmed tick
    /// - Re-apply queued inputs
    /// - Catch up to current tick
    /// </summary>
    [BurstCompile]
    public struct RollbackBuffer<T> where T : unmanaged
    {
        private NativeArray<T> _buffer;
        private int _capacity;
        private int _head;
        private int _count;

        public RollbackBuffer(int capacity, Allocator allocator)
        {
            _capacity = capacity;
            _buffer = new NativeArray<T>(capacity, allocator);
            _head = 0;
            _count = 0;
        }

        public void Push(T item)
        {
            if (_capacity == 0)
            {
                return;
            }

            int index = (_head + _count) % _capacity;
            _buffer[index] = item;

            if (_count < _capacity)
            {
                _count++;
            }
            else
            {
                _head = (_head + 1) % _capacity;
            }
        }

        public bool TryPeek(out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_head];
            return true;
        }

        public bool TryPop(out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_head];
            _head = (_head + 1) % _capacity;
            _count--;
            return true;
        }

        public bool TryGet(int index, out T item)
        {
            if (index < 0 || index >= _count)
            {
                item = default;
                return false;
            }

            int bufferIndex = (_head + index) % _capacity;
            item = _buffer[bufferIndex];
            return true;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }

        public void Dispose()
        {
            if (_buffer.IsCreated)
            {
                _buffer.Dispose();
            }
        }

        public int Count => _count;
        public int Capacity => _capacity;
        public bool IsCreated => _buffer.IsCreated;
    }
}

