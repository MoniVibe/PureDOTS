using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Replay
{
    /// <summary>
    /// Metadata for a replay file, storing tick range, hashes, and command info.
    /// </summary>
    public struct ReplayMetadata : IComponentData
    {
        /// <summary>
        /// Starting tick of the replay.
        /// </summary>
        public uint StartTick;

        /// <summary>
        /// Ending tick of the replay.
        /// </summary>
        public uint EndTick;

        /// <summary>
        /// Hash of the replay data for integrity checking.
        /// </summary>
        public ulong Hash;

        /// <summary>
        /// Number of commands in the replay.
        /// </summary>
        public int CommandCount;

        /// <summary>
        /// Replay file format version.
        /// </summary>
        public uint Version;

        /// <summary>
        /// Active tuning profiles at replay time (for fidelity).
        /// </summary>
        public FixedString64Bytes PhysicsProfile;
        public FixedString64Bytes AIProfile;
        public FixedString64Bytes EconomyProfile;

        public ReplayMetadata(uint startTick, uint endTick, ulong hash)
        {
            StartTick = startTick;
            EndTick = endTick;
            Hash = hash;
            CommandCount = 0;
            Version = 1;
            PhysicsProfile = default;
            AIProfile = default;
            EconomyProfile = default;
        }
    }
}

