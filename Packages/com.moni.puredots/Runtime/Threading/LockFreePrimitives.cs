using System.Threading;
using Unity.Burst;
using Unity.Collections;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Lock-free synchronization primitives for Burst-compatible code.
    /// </summary>
    [BurstCompile]
    public static class LockFreePrimitives
    {
        /// <summary>
        /// Atomic flag using Interlocked operations.
        /// </summary>
        [BurstCompile]
        public struct AtomicFlag
        {
            private int _value;

            public AtomicFlag(bool initialValue)
            {
                _value = initialValue ? 1 : 0;
            }

            [BurstCompile]
            public bool Get()
            {
                return Interlocked.Read(ref _value) != 0;
            }

            [BurstCompile]
            public bool Set(bool value)
            {
                int oldValue = Interlocked.Exchange(ref _value, value ? 1 : 0);
                return oldValue != 0;
            }

            [BurstCompile]
            public bool CompareExchange(bool expected, bool newValue)
            {
                int expectedInt = expected ? 1 : 0;
                int newInt = newValue ? 1 : 0;
                int oldValue = Interlocked.CompareExchange(ref _value, newInt, expectedInt);
                return oldValue == expectedInt;
            }
        }

        /// <summary>
        /// Atomic counter using Interlocked operations.
        /// </summary>
        [BurstCompile]
        public struct AtomicCounter
        {
            private int _value;

            public AtomicCounter(int initialValue)
            {
                _value = initialValue;
            }

            [BurstCompile]
            public int Get()
            {
                return Interlocked.Read(ref _value);
            }

            [BurstCompile]
            public int Increment()
            {
                return Interlocked.Increment(ref _value);
            }

            [BurstCompile]
            public int Decrement()
            {
                return Interlocked.Decrement(ref _value);
            }

            [BurstCompile]
            public int Add(int delta)
            {
                return Interlocked.Add(ref _value, delta);
            }

            [BurstCompile]
            public int CompareExchange(int expected, int newValue)
            {
                return Interlocked.CompareExchange(ref _value, newValue, expected);
            }
        }

        /// <summary>
        /// SpinWait helper for small critical regions.
        /// </summary>
        [BurstCompile]
        public static void SpinWait(int iterations = 10)
        {
            for (int i = 0; i < iterations; i++)
            {
                // CPU pause instruction (hint to processor)
                System.Threading.Thread.SpinWait(1);
            }
        }

        /// <summary>
        /// Lock-free queue wrapper using NativeQueue.
        /// </summary>
        [BurstCompile]
        public struct LockFreeQueue<T> where T : unmanaged
        {
            private NativeQueue<T> _queue;

            public LockFreeQueue(Allocator allocator)
            {
                _queue = new NativeQueue<T>(allocator);
            }

            public void Dispose()
            {
                if (_queue.IsCreated)
                {
                    _queue.Dispose();
                }
            }

            public bool IsCreated => _queue.IsCreated;

            [BurstCompile]
            public void Enqueue(T item)
            {
                _queue.Enqueue(item);
            }

            [BurstCompile]
            public bool TryDequeue(out T item)
            {
                return _queue.TryDequeue(out item);
            }

            public int Count => _queue.Count;
        }

        /// <summary>
        /// Lock-free stream wrapper using NativeStream for high-volume append-only logs.
        /// </summary>
        [BurstCompile]
        public struct LockFreeStream
        {
            private NativeStream _stream;

            public LockFreeStream(int forEachCount, Allocator allocator)
            {
                _stream = new NativeStream(forEachCount, allocator);
            }

            public void Dispose()
            {
                if (_stream.IsCreated)
                {
                    _stream.Dispose();
                }
            }

            public bool IsCreated => _stream.IsCreated;

            [BurstCompile]
            public NativeStream.Writer AsWriter()
            {
                return _stream.AsWriter();
            }

            [BurstCompile]
            public NativeStream.Reader AsReader()
            {
                return _stream.AsReader();
            }
        }
    }
}

