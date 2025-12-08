using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Central telemetry hub for queuing metrics from systems/jobs and draining into the global TelemetryStream.
    /// </summary>
    public static class TelemetryHub
    {
        private static NativeQueue<TelemetryMetric> _queue;
        private static bool _initialized;

        /// <summary>
        /// Initializes the hub with a persistent queue. Safe to call multiple times.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _queue = new NativeQueue<TelemetryMetric>(Allocator.Persistent);
            _initialized = true;
        }

        /// <summary>
        /// Disposes the underlying queue.
        /// </summary>
        public static void Dispose()
        {
            if (_initialized && _queue.IsCreated)
            {
                _queue.Dispose();
            }
            _initialized = false;
        }

        /// <summary>
        /// Enqueue a telemetry metric from main thread.
        /// </summary>
        public static void Enqueue(in TelemetryMetric metric)
        {
            if (!_initialized || !_queue.IsCreated)
            {
                return;
            }
            _queue.Enqueue(metric);
        }

        /// <summary>
        /// Get a ParallelWriter for use inside jobs (only valid if Initialize was called).
        /// </summary>
        public static NativeQueue<TelemetryMetric>.ParallelWriter AsParallelWriter()
        {
            return _queue.IsCreated ? _queue.AsParallelWriter() : default;
        }

        /// <summary>
        /// Drain all pending metrics into the provided list.
        /// </summary>
        public static void Drain(ref NativeList<TelemetryMetric> output)
        {
            if (!_initialized || !_queue.IsCreated)
            {
                return;
            }

            while (_queue.Count > 0)
            {
                output.Add(_queue.Dequeue());
            }
        }
    }
}
