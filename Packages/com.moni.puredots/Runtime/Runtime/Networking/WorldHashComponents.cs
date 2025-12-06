using Unity.Entities;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// World hash component for frame hash validation.
    /// Computes CRC32 across critical component buffers each tick.
    /// Store locally; later compare between peers to detect divergence early.
    /// </summary>
    public struct WorldHash : IComponentData
    {
        /// <summary>
        /// CRC32 hash of critical world state for this tick.
        /// </summary>
        public uint CRC32;

        /// <summary>
        /// Tick when this hash was computed.
        /// </summary>
        public uint Tick;
    }
}

