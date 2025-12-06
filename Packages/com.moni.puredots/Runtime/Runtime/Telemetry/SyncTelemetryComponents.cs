using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Extended telemetry components for multiplayer sync debugging.
    /// Logs tick number, input hash, world state CRC, and latency placeholders.
    /// These are cheap integers today; tomorrow they'll catch desyncs instantly.
    /// </summary>
    public struct SyncTelemetry : IComponentData
    {
        /// <summary>
        /// Current tick number (also available via TelemetryStream.LastTick).
        /// </summary>
        public uint Tick;

        /// <summary>
        /// CRC32 hash of input commands for this tick.
        /// Used to detect input divergence between clients.
        /// </summary>
        public uint InputHash;

        /// <summary>
        /// CRC32 hash of critical component buffers for world state validation.
        /// Used to detect simulation divergence between clients.
        /// </summary>
        public uint WorldStateCRC;

        /// <summary>
        /// Local latency placeholder (milliseconds).
        /// In multiplayer, this would be actual network latency.
        /// </summary>
        public int LocalLatencyMs;

        /// <summary>
        /// Remote latency placeholder (milliseconds).
        /// In multiplayer, this would be remote peer latency.
        /// </summary>
        public int RemoteLatencyMs;
    }

    /// <summary>
    /// Buffer element for historical sync telemetry samples.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct SyncTelemetrySample : IBufferElementData
    {
        public uint Tick;
        public uint InputHash;
        public uint WorldStateCRC;
        public int LocalLatencyMs;
        public int RemoteLatencyMs;
    }
}

