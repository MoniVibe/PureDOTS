using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Serialization
{
    /// <summary>
    /// Partitions large worlds into sub-worlds by cell authority.
    /// Enables distributed simulation by assigning cell ownership.
    /// </summary>
    public struct WorldPartition : IComponentData
    {
        /// <summary>
        /// Cell coordinates for this partition.
        /// </summary>
        public int3 CellCoords;

        /// <summary>
        /// Authority identifier (0-255, identifies which process/machine owns this partition).
        /// </summary>
        public byte AuthorityId;

        /// <summary>
        /// Whether this partition is local (true) or remote (false).
        /// </summary>
        public bool IsLocal;

        public WorldPartition(int3 cellCoords, byte authorityId, bool isLocal)
        {
            CellCoords = cellCoords;
            AuthorityId = authorityId;
            IsLocal = isLocal;
        }
    }

    /// <summary>
    /// Manages world partitioning for distributed simulation.
    /// </summary>
    [BurstCompile]
    public static class WorldPartitionManager
    {
        /// <summary>
        /// Gets the authority ID for a given cell coordinate.
        /// </summary>
        [BurstCompile]
        public static byte GetAuthorityForCell(in int3 cellCoords, int partitionSize)
        {
            // Simple hash-based partitioning
            int hash = cellCoords.x + cellCoords.y * 256 + cellCoords.z * 65536;
            return (byte)(hash % partitionSize);
        }

        /// <summary>
        /// Checks if a cell belongs to the local authority.
        /// </summary>
        [BurstCompile]
        public static bool IsLocalCell(in int3 cellCoords, byte localAuthorityId, int partitionSize)
        {
            byte authority = GetAuthorityForCell(in cellCoords, partitionSize);
            return authority == localAuthorityId;
        }
    }
}

