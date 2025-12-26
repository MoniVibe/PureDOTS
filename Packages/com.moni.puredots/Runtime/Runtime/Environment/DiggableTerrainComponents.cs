using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Terrain chunk definition (authoritative base data).
    /// </summary>
    public struct TerrainChunk : IComponentData
    {
        public int3 ChunkCoord;
        public int3 VoxelsPerChunk;
        public BlobAssetReference<TerrainChunkBlob> BaseBlob;
    }

    public struct TerrainChunkBlob
    {
        public BlobArray<byte> SolidMask;
        public BlobArray<byte> MaterialId;
        public BlobArray<byte> Hardness;
        public BlobArray<byte> DepositId;
        public BlobArray<byte> OreGrade;
    }

    [InternalBufferCapacity(0)]
    public struct TerrainVoxelRuntime : IBufferElementData
    {
        public byte SolidMask;
        public byte MaterialId;
        public byte DepositId;
        public byte OreGrade;
    }

    public struct TerrainChunkDirty : IComponentData
    {
        public uint EditVersion;
        public uint LastEditTick;
    }

    [System.Flags]
    public enum TerrainModificationFlags : byte
    {
        None = 0,
        AffectsSurface = 1 << 0,
        AffectsVolume = 1 << 1,
        AffectsMaterial = 1 << 2
    }

    public enum TerrainModificationKind : byte
    {
        Dig = 0,
        Fill = 1,
        Carve = 2,
        PaintMaterial = 3
    }

    public enum TerrainModificationShape : byte
    {
        Brush = 0,
        Tunnel = 1,
        Ramp = 2
    }

    [InternalBufferCapacity(32)]
    public struct TerrainModificationRequest : IBufferElementData
    {
        public TerrainModificationKind Kind;
        public TerrainModificationShape Shape;
        public float3 Start;
        public float3 End;
        public float Radius;
        public float Depth;
        public byte MaterialId;
        public TerrainModificationFlags Flags;
        public uint RequestedTick;
        public Entity Actor;
    }

    public struct TerrainModificationQueue : IComponentData { }

    public struct TerrainModificationBudget : IComponentData
    {
        public int MaxEditsPerTick;
        public int MaxDirtyRegionsPerTick;

        public static TerrainModificationBudget Default => new()
        {
            MaxEditsPerTick = 8,
            MaxDirtyRegionsPerTick = 16
        };
    }

    [InternalBufferCapacity(16)]
    public struct TerrainDirtyRegion : IBufferElementData
    {
        public float3 WorldMin;
        public float3 WorldMax;
        public uint Version;
        public byte Flags;
    }

    [InternalBufferCapacity(0)]
    public struct TerrainSurfaceTileVersion : IBufferElementData
    {
        public int2 TileCoord;
        public uint Version;
    }

    [InternalBufferCapacity(0)]
    public struct TerrainUndergroundChunkVersion : IBufferElementData
    {
        public int3 ChunkCoord;
        public uint Version;
    }
}
