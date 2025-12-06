using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.WorldBus
{
    /// <summary>
    /// Message for cross-world communication.
    /// Routes events between ECS worlds deterministically.
    /// </summary>
    public struct WorldMessage : IBufferElementData
    {
        /// <summary>
        /// Source world identifier (0-255).
        /// </summary>
        public byte SourceWorld;

        /// <summary>
        /// Target world identifier (0-255).
        /// </summary>
        public byte TargetWorld;

        /// <summary>
        /// Message payload (64 bytes fixed size for determinism).
        /// </summary>
        public FixedBytes64 Payload;

        /// <summary>
        /// Tick when message was sent.
        /// </summary>
        public uint Tick;

        /// <summary>
        /// Message type identifier.
        /// </summary>
        public byte MessageType;

        public WorldMessage(byte sourceWorld, byte targetWorld, FixedBytes64 payload, uint tick, byte messageType)
        {
            SourceWorld = sourceWorld;
            TargetWorld = targetWorld;
            Payload = payload;
            Tick = tick;
            MessageType = messageType;
        }
    }
}

