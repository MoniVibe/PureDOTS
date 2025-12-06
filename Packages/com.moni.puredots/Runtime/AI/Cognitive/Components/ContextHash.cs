using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.Cognitive
{
    /// <summary>
    /// Terrain type enum for context hashing.
    /// </summary>
    public enum TerrainType : byte
    {
        None = 0,
        Flat = 1,
        Hilly = 2,
        Mountainous = 3,
        Pit = 4,
        Water = 5,
        Forest = 6,
        Desert = 7,
        Custom0 = 240
    }

    /// <summary>
    /// Obstacle tag enum for context hashing.
    /// </summary>
    public enum ObstacleTag : byte
    {
        None = 0,
        Wall = 1,
        Box = 2,
        Rock = 3,
        Ladder = 4,
        Door = 5,
        Custom0 = 240
    }

    /// <summary>
    /// Goal type enum for context hashing.
    /// </summary>
    public enum GoalType : byte
    {
        None = 0,
        Escape = 1,
        Reach = 2,
        Gather = 3,
        Build = 4,
        Combat = 5,
        Custom0 = 240
    }

    /// <summary>
    /// Context hash component storing situation fingerprint for procedural learning.
    /// Used to group similar situations for shared learning.
    /// </summary>
    public struct ContextHash : IComponentData
    {
        /// <summary>
        /// Terrain type at current location.
        /// </summary>
        public TerrainType TerrainType;

        /// <summary>
        /// Primary obstacle tag in vicinity.
        /// </summary>
        public ObstacleTag ObstacleTag;

        /// <summary>
        /// Current goal type.
        /// </summary>
        public GoalType GoalType;

        /// <summary>
        /// Computed hash of terrain + obstacle + goal.
        /// Used for fast context matching.
        /// </summary>
        public byte Hash;

        /// <summary>
        /// Last tick when context was computed.
        /// </summary>
        public uint LastComputedTick;
    }

    /// <summary>
    /// Helper functions for context hashing.
    /// </summary>
    public static class ContextHashHelper
    {
        /// <summary>
        /// Compute deterministic hash from terrain, obstacle, and goal.
        /// </summary>
        public static byte ComputeHash(TerrainType terrain, ObstacleTag obstacle, GoalType goal)
        {
            // Simple deterministic hash: combine bytes with bit operations
            uint hash = (uint)terrain;
            hash = (hash << 4) ^ (uint)obstacle;
            hash = (hash << 4) ^ (uint)goal;
            hash = hash ^ (hash >> 8);
            hash = hash ^ (hash >> 16);
            return (byte)(hash & 0xFF);
        }

        /// <summary>
        /// Compute Hamming distance between two context hashes.
        /// Returns number of differing bits.
        /// </summary>
        public static int HammingDistance(byte hash1, byte hash2)
        {
            byte diff = (byte)(hash1 ^ hash2);
            int distance = 0;
            while (diff != 0)
            {
                distance += diff & 1;
                diff >>= 1;
            }
            return distance;
        }

        /// <summary>
        /// Check if two contexts are similar (Hamming distance <= threshold).
        /// </summary>
        public static bool AreSimilar(byte hash1, byte hash2, int threshold = 2)
        {
            return HammingDistance(hash1, hash2) <= threshold;
        }
    }
}

