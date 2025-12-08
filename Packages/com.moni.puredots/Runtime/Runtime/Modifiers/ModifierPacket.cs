using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// AoSoA (Array of Structures of Arrays) packet for SIMD-optimized modifier processing.
    /// Groups 8 modifiers into a packet for vectorized math operations.
    /// </summary>
    public unsafe struct ModifierPacket
    {
        /// <summary>
        /// Array of 8 modifier IDs.
        /// </summary>
        public fixed ushort Id[8];

        /// <summary>
        /// Array of 8 modifier values.
        /// </summary>
        public fixed float Value[8];

        /// <summary>
        /// Array of 8 modifier durations.
        /// </summary>
        public fixed short Duration[8];

        /// <summary>
        /// Number of valid modifiers in this packet (0-8).
        /// </summary>
        public byte Count;
    }
}

