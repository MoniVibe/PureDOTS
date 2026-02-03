using System;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Memory
{
    [Flags]
    public enum MemoryFlags : byte
    {
        None = 0,
        Sticky = 1 << 0,
        Shared = 1 << 1,
        Trauma = 1 << 2,
        Triumph = 1 << 3
    }

    /// <summary>
    /// Generic cognitive memory entry for entities.
    /// </summary>
    [InternalBufferCapacity(6)]
    public struct MemoryEntry : IBufferElementData
    {
        public FixedString64Bytes MemoryId;
        public float InitialMagnitude;
        public float CurrentMagnitude;
        public uint FormedTick;
        public uint DecayHalfLife;
        public Entity RelatedEntity;
        public MemoryFlags Flags;
    }

    /// <summary>
    /// Request to add or refresh a memory entry.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct MemoryAddRequest : IBufferElementData
    {
        public FixedString64Bytes MemoryId;
        public float Magnitude;
        public uint DecayHalfLife;
        public Entity RelatedEntity;
        public MemoryFlags Flags;
    }

    /// <summary>
    /// Configuration for generic memory decay and limits.
    /// </summary>
    public struct MemoryConfig : IComponentData
    {
        public float MinMagnitude;
        public int MaxMemories;

        public static MemoryConfig Default => new MemoryConfig
        {
            MinMagnitude = 0.01f,
            MaxMemories = 8
        };
    }
}
