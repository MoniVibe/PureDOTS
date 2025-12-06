using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Collections;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Asynchronous IO thread for low-priority streaming/serialization operations.
    /// Uses ring buffers to avoid blocking simulation threads.
    /// </summary>
    public class AsyncIOThread : IDisposable
    {
        private Thread _ioThread;
        private readonly ConcurrentQueue<IOCommand> _commandQueue;
        private readonly ManualResetEventSlim _workAvailable;
        private volatile bool _running;

        public AsyncIOThread()
        {
            _commandQueue = new ConcurrentQueue<IOCommand>();
            _workAvailable = new ManualResetEventSlim(false);
            _running = false;
        }

        /// <summary>
        /// Starts the IO thread with reduced CPU priority.
        /// </summary>
        public void Start()
        {
            if (_running)
            {
                return;
            }

            _running = true;
            _ioThread = new Thread(IOThreadWorker)
            {
                Name = "PureDOTS AsyncIO",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal // Reduced priority
            };
            _ioThread.Start();
        }

        /// <summary>
        /// Stops the IO thread.
        /// </summary>
        public void Stop()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            _workAvailable.Set();

            _ioThread?.Join(1000);
            _ioThread = null;
        }

        /// <summary>
        /// Enqueues an IO command for asynchronous processing.
        /// </summary>
        public void EnqueueCommand(IOCommand command)
        {
            _commandQueue.Enqueue(command);
            _workAvailable.Set();
        }

        private void IOThreadWorker()
        {
            while (_running)
            {
                _workAvailable.Wait();

                while (_commandQueue.TryDequeue(out var command))
                {
                    try
                    {
                        command.Execute();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[AsyncIOThread] Error executing command: {ex}");
                    }
                }

                _workAvailable.Reset();
            }
        }

        public void Dispose()
        {
            Stop();
            _workAvailable?.Dispose();
        }

        /// <summary>
        /// Base class for IO commands.
        /// </summary>
        public abstract class IOCommand
        {
            public abstract void Execute();
        }

        /// <summary>
        /// Ring buffer for streaming operations.
        /// </summary>
        public class RingBuffer<T>
        {
            private readonly T[] _buffer;
            private readonly int _capacity;
            private int _head;
            private int _tail;
            private readonly object _lock = new object();

            public RingBuffer(int capacity)
            {
                _capacity = capacity;
                _buffer = new T[capacity];
                _head = 0;
                _tail = 0;
            }

            public bool TryEnqueue(T item)
            {
                lock (_lock)
                {
                    int nextTail = (_tail + 1) % _capacity;
                    if (nextTail == _head)
                    {
                        return false; // Buffer full
                    }

                    _buffer[_tail] = item;
                    _tail = nextTail;
                    return true;
                }
            }

            public bool TryDequeue(out T item)
            {
                lock (_lock)
                {
                    if (_head == _tail)
                    {
                        item = default;
                        return false; // Buffer empty
                    }

                    item = _buffer[_head];
                    _head = (_head + 1) % _capacity;
                    return true;
                }
            }

            public int Count
            {
                get
                {
                    lock (_lock)
                    {
                        return (_tail - _head + _capacity) % _capacity;
                    }
                }
            }
        }
    }
}

