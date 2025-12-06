using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Tag component marking a biome chunk as dirty (needs recalculation).
    /// </summary>
    public struct BiomeChunkDirtyTag : IComponentData
    {
    }

    /// <summary>
    /// Hash value for a biome chunk, used to detect changes.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct BiomeChunkHash : IBufferElementData
    {
        public uint Value;
    }

    /// <summary>
    /// Dirty flag for a biome chunk (1 = dirty, 0 = clean).
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct BiomeChunkDirtyFlag : IBufferElementData
    {
        public byte Value; // 1 = dirty, 0 = clean
    }

    /// <summary>
    /// Metadata describing biome chunk organization.
    /// </summary>
    public struct BiomeChunkMetadata : IComponentData
    {
        public int2 ChunkSize;        // Cells per chunk (default: 64×64)
        public int2 ChunkCounts;      // Number of chunks in each dimension
        public int TotalChunkCount;   // Total number of chunks
        public EnvironmentGridMetadata GridMetadata; // Reference to underlying grid

        public static BiomeChunkMetadata Create(EnvironmentGridMetadata gridMetadata, int2 chunkSize)
        {
            var chunkCounts = new int2(
                (gridMetadata.Resolution.x + chunkSize.x - 1) / chunkSize.x,
                (gridMetadata.Resolution.y + chunkSize.y - 1) / chunkSize.y
            );

            return new BiomeChunkMetadata
            {
                ChunkSize = chunkSize,
                ChunkCounts = chunkCounts,
                TotalChunkCount = chunkCounts.x * chunkCounts.y,
                GridMetadata = gridMetadata
            };
        }

        /// <summary>
        /// Gets the chunk index for a given cell coordinate.
        /// </summary>
        public readonly int GetChunkIndex(int2 cellCoord)
        {
            var chunkCoord = new int2(
                cellCoord.x / ChunkSize.x,
                cellCoord.y / ChunkSize.y
            );
            chunkCoord = math.clamp(chunkCoord, new int2(0, 0), ChunkCounts - 1);
            return chunkCoord.y * ChunkCounts.x + chunkCoord.x;
        }

        /// <summary>
        /// Gets the chunk coordinate for a given cell coordinate.
        /// </summary>
        public readonly int2 GetChunkCoord(int2 cellCoord)
        {
            return new int2(
                cellCoord.x / ChunkSize.x,
                cellCoord.y / ChunkSize.y
            );
        }

        /// <summary>
        /// Gets the cell range for a given chunk index.
        /// </summary>
        public readonly void GetChunkCellRange(int chunkIndex, out int2 minCell, out int2 maxCell)
        {
            var chunkCoord = new int2(
                chunkIndex % ChunkCounts.x,
                chunkIndex / ChunkCounts.x
            );

            minCell = new int2(
                chunkCoord.x * ChunkSize.x,
                chunkCoord.y * ChunkSize.y
            );

            maxCell = new int2(
                math.min(minCell.x + ChunkSize.x, GridMetadata.Resolution.x),
                math.min(minCell.y + ChunkSize.y, GridMetadata.Resolution.y)
            );
        }
    }
}

