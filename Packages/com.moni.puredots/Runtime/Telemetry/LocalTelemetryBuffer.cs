using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Per-system ring buffer for local telemetry (Burst-safe).
    /// First level of hierarchical telemetry pipeline.
    /// </summary>
    [BurstCompile]
    public struct LocalTelemetryBuffer
    {
        private NativeQueue<TelemetryMetric> _ringBuffer;
        private bool _isCreated;
        private int _capacity;
        private Allocator _allocator;

        /// <summary>
        /// Creates a new local telemetry buffer with the specified capacity.
        /// </summary>
        public LocalTelemetryBuffer(int capacity, Allocator allocator)
        {
            _ringBuffer = new NativeQueue<TelemetryMetric>(allocator);
            _isCreated = true;
            _capacity = capacity;
            _allocator = allocator;
        }

        /// <summary>
        /// Disposes the ring buffer.
        /// </summary>
        public void Dispose()
        {
            if (_isCreated && _ringBuffer.IsCreated)
            {
                _ringBuffer.Dispose();
                _isCreated = false;
            }
        }

        /// <summary>Whether the buffer was created.</summary>
        public bool IsCreated => _isCreated && _ringBuffer.IsCreated;

        /// <summary>Current count of pending metrics.</summary>
        public int Count => _isCreated && _ringBuffer.IsCreated ? _ringBuffer.Count : 0;

        /// <summary>Clears all pending metrics.</summary>
        public void Clear()
        {
            if (!_isCreated || !_ringBuffer.IsCreated) return;
            while (_ringBuffer.Count > 0) _ringBuffer.Dequeue();
        }

        /// <summary>
        /// Adds a telemetry metric to the buffer.
        /// </summary>
        public void Add(in TelemetryMetric metric)
        {
            if (!_isCreated)
                return;

            if (_ringBuffer.Count >= _capacity)
            {
                _ringBuffer.Dequeue(); // Remove oldest
            }
            _ringBuffer.Enqueue(metric);
        }

        /// <summary>
        /// Get a ParallelWriter for job producers. Caller must ensure the buffer is created.
        /// </summary>
        public NativeQueue<TelemetryMetric>.ParallelWriter AsParallelWriter()
        {
            return _ringBuffer.IsCreated ? _ringBuffer.AsParallelWriter() : default;
        }

        /// <summary>
        /// Dequeues all metrics for aggregation.
        /// </summary>
        public void DequeueAll(NativeList<TelemetryMetric> output)
        {
            if (!_isCreated)
                return;

            while (_ringBuffer.Count > 0)
            {
                output.Add(_ringBuffer.Dequeue());
            }
        }
    }
}

