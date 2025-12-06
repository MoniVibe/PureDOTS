using Unity.Entities;

namespace PureDOTS.Runtime.Recovery
{
    /// <summary>
    /// Configuration singleton for crash recovery system.
    /// </summary>
    public struct CrashRecoveryConfig : IComponentData
    {
        /// <summary>Interval in ticks between snapshots (default: 1000).</summary>
        public uint SnapshotIntervalTicks;

        /// <summary>Number of snapshots to keep in ring buffer (default: 10).</summary>
        public int RingBufferSize;

        /// <summary>Enable automatic snapshot saving.</summary>
        public bool AutoSaveEnabled;

        public static CrashRecoveryConfig Default => new CrashRecoveryConfig
        {
            SnapshotIntervalTicks = 1000,
            RingBufferSize = 10,
            AutoSaveEnabled = true
        };
    }
}

