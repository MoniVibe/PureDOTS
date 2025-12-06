using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Blob payload for a climate chunk containing compressed half-precision arrays.
    /// Each chunk represents a 64×64 cell region of the world.
    /// </summary>
    public struct ClimateChunkBlob
    {
        public BlobArray<half> Temperature; // 64×64×1 (or ×height if volumetric)
        public BlobArray<half> Moisture;
        public BlobArray<half> Oxygen;
    }

    /// <summary>
    /// Component tracking a climate chunk's state and location.
    /// </summary>
    public struct ClimateChunk : IComponentData
    {
        public int2 ChunkCoord; // Chunk coordinates in chunk space
        public BlobAssetReference<ClimateChunkBlob> Blob;
        public byte IsActive; // 0=serialized, 1=in-memory
        public uint LastAccessTick; // Last tick this chunk was accessed
    }

    /// <summary>
    /// Manager component tracking active chunk set and serialization state.
    /// </summary>
    public struct ClimateChunkManager : IComponentData
    {
        public int ChunkSize; // Cells per chunk (default 64)
        public int MaxActiveChunks; // Maximum chunks to keep in memory
        public uint SerializationTick; // Last tick chunks were serialized
    }

    /// <summary>
    /// Buffer storing chunk coordinates that should be active.
    /// Updated by spatial queries.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct ActiveChunkRequest : IBufferElementData
    {
        public int2 ChunkCoord;
        public uint RequestTick;
    }

    /// <summary>
    /// Temporal LOD configuration defining tick divisors per system.
    /// </summary>
    public struct TemporalLODConfig : IComponentData
    {
        public uint WindCloudDivisor;      // 1 (every tick)
        public uint TemperatureDivisor;     // 5 (every 5 ticks)
        public uint VegetationDivisor;      // 20 (every 20 ticks)
        public uint FireDivisor;            // 1 (but event-driven)
        public uint ClimateFeedbackDivisor; // 5 (every 5 ticks)
    }

    /// <summary>
    /// Double-buffered field data for deterministic reads and writes.
    /// </summary>
    public struct DoubleBufferedField : IComponentData
    {
        public byte ReadBufferIndex; // 0 or 1
        public BlobAssetReference<FieldBufferBlob> Buffer0;
        public BlobAssetReference<FieldBufferBlob> Buffer1;
    }

    /// <summary>
    /// Blob payload for double-buffered field data.
    /// </summary>
    public struct FieldBufferBlob
    {
        public BlobArray<float> Values;
    }

    /// <summary>
    /// Performance budget tracking frame time per subsystem.
    /// </summary>
    public struct EnvironmentPerformanceBudget : IComponentData
    {
        public float VegetationGrowthBudget; // ≤ 0.5 ms
        public float FireSpreadBudget;       // ≤ 1.0 ms
        public float ClimateGridBudget;      // ≤ 2.0 ms
        public float WindCloudBudget;        // ≤ 1.0 ms
        public float TotalBudget;            // ≤ 5.0 ms
    }
}

