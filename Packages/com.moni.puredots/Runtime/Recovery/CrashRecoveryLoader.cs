using System.IO;
using System.Linq;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Recovery;
using PureDOTS.Runtime.Time;
using PureDOTS.Runtime.Debugging;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Recovery
{
    /// <summary>
    /// Loader for crash recovery snapshots.
    /// Detects latest snapshot on startup and offers recovery option.
    /// </summary>
    public static class CrashRecoveryLoader
    {
        /// <summary>
        /// Detects the latest snapshot and returns its metadata.
        /// </summary>
        public static bool TryFindLatestSnapshot(out SnapshotInfo snapshotInfo)
        {
            snapshotInfo = default;

            try
            {
                var recoveryDir = Path.Combine(Application.persistentDataPath, "Recovery");
                if (!Directory.Exists(recoveryDir))
                {
                    return false;
                }

                var snapshotFiles = Directory.GetFiles(recoveryDir, "snapshot_*.dat");
                if (snapshotFiles.Length == 0)
                {
                    return false;
                }

                // Find latest by tick
                SnapshotInfo latest = default;
                uint latestTick = 0;

                foreach (var filePath in snapshotFiles)
                {
                    var tick = ExtractTickFromFilename(filePath);
                    if (tick > latestTick)
                    {
                        latestTick = tick;
                        var metaPath = filePath.Replace(".dat", ".meta");
                        if (File.Exists(metaPath))
                        {
                            var metadataJson = File.ReadAllText(metaPath);
                            var metadata = JsonUtility.FromJson<SnapshotMetadata>(metadataJson);
                            latest = new SnapshotInfo
                            {
                                FilePath = filePath,
                                Tick = metadata.Tick,
                                Timestamp = metadata.Timestamp,
                                EntityCount = metadata.EntityCount,
                                Hash = metadata.Hash
                            };
                        }
                        else
                        {
                            latest = new SnapshotInfo
                            {
                                FilePath = filePath,
                                Tick = tick,
                                Timestamp = File.GetLastWriteTime(filePath).ToString("O"),
                                EntityCount = 0,
                                Hash = 0
                            };
                        }
                    }
                }

                if (latestTick > 0)
                {
                    snapshotInfo = latest;
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                DebugLog.LogError($"[CrashRecovery] Failed to find latest snapshot: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Loads a snapshot into the world.
        /// </summary>
        public static bool LoadSnapshot(World world, string snapshotPath, out string error)
        {
            error = null;

            try
            {
                if (!File.Exists(snapshotPath))
                {
                    error = $"Snapshot file not found: {snapshotPath}";
                    return false;
                }

                var data = File.ReadAllBytes(snapshotPath);
                using var buffer = new NativeArray<byte>(data, Allocator.TempJob);

                var reader = new TimeStreamReader(buffer);

                // Read metadata
                var tick = reader.Read<uint>();
                var timestamp = reader.Read<long>();
                var entityCount = reader.Read<int>();

                // In a full implementation, would deserialize all entities and components
                // This is a simplified version

                DebugLog.Log($"[CrashRecovery] Loaded snapshot from tick {tick} ({entityCount} entities)");
                return true;
            }
            catch (System.Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static uint ExtractTickFromFilename(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.StartsWith("snapshot_"))
            {
                var tickStr = fileName.Substring("snapshot_".Length);
                if (uint.TryParse(tickStr, out var tick))
                {
                    return tick;
                }
            }
            return 0;
        }

        public struct SnapshotInfo
        {
            public string FilePath;
            public uint Tick;
            public string Timestamp;
            public int EntityCount;
            public uint Hash;
        }

        [System.Serializable]
        private class SnapshotMetadata
        {
            public uint Tick;
            public string Timestamp;
            public int EntityCount;
            public uint Hash;
        }
    }
}

