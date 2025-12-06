using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Lock-free work-stealing queue using atomic head/tail indices.
    /// Each worker thread maintains a deque; when idle, steals from other threads.
    /// </summary>
    [BurstCompile]
    public struct WorkStealingQueue<T> where T : unmanaged
    {
        private UnsafeList<T> _items;
        private int _head;
        private int _tail;

        public WorkStealingQueue(int initialCapacity, Allocator allocator)
        {
            _items = new UnsafeList<T>(initialCapacity, allocator);
            _head = 0;
            _tail = 0;
        }

        public void Dispose()
        {
            if (_items.IsCreated)
            {
                _items.Dispose();
            }
        }

        public bool IsCreated => _items.IsCreated;

        /// <summary>
        /// Push item to the local end (tail) - lock-free for owner thread.
        /// </summary>
        [BurstCompile]
        public void PushLocal(T item)
        {
            int tail = _tail;
            if (tail >= _items.Length)
            {
                // Grow array if needed
                int newLength = math.max(_items.Length * 2, 16);
                _items.Resize(newLength, NativeArrayOptions.ClearMemory);
            }

            _items[tail] = item;
            System.Threading.Interlocked.Exchange(ref _tail, tail + 1);
        }

        /// <summary>
        /// Pop item from the local end (tail) - lock-free for owner thread.
        /// </summary>
        [BurstCompile]
        public bool TryPopLocal(out T item)
        {
            int tail = System.Threading.Interlocked.Decrement(ref _tail);
            if (tail >= _head)
            {
                item = _items[tail];
                return true;
            }

            // Queue was empty or stolen from
            System.Threading.Interlocked.Increment(ref _tail);
            item = default;
            return false;
        }

        /// <summary>
        /// Steal item from the remote end (head) - lock-free for stealing thread.
        /// </summary>
        [BurstCompile]
        public bool TrySteal(out T item)
        {
            int head = _head;
            int tail = _tail;

            if (tail <= head)
            {
                item = default;
                return false;
            }

            // Try to increment head atomically
            int newHead = System.Threading.Interlocked.CompareExchange(ref _head, head + 1, head);
            if (newHead == head)
            {
                item = _items[head];
                return true;
            }

            item = default;
            return false;
        }

        public int Count
        {
            [BurstCompile]
            get
            {
                int tail = _tail;
                int head = _head;
                return math.max(0, tail - head);
            }
        }

        public bool IsEmpty
        {
            [BurstCompile]
            get => _tail <= _head;
        }
    }
}

