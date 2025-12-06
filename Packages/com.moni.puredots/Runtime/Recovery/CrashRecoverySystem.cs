using System.IO;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Recovery;
using PureDOTS.Runtime.Time;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Systems.Recovery
{
    /// <summary>
    /// Crash recovery system that saves world state snapshots at regular intervals.
    /// Uses ring buffer to keep last N snapshots, deleting oldest when limit reached.
    /// 
    /// See: Docs/Guides/DemoLockSystemsGuide.md#crash-recovery
    /// API Reference: Docs/Guides/DemoLockSystemsAPI.md#crash-recovery-api
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct CrashRecoverySystem : ISystem
    {
        private uint _lastSnapshotTick;
        private int _snapshotCount;

        public void OnCreate(ref SystemState state)
        {
            // Create config singleton if it doesn't exist
            if (!SystemAPI.HasSingleton<CrashRecoveryConfig>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<CrashRecoveryConfig>(entity);
                state.EntityManager.SetComponentData(entity, CrashRecoveryConfig.Default);
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<CrashRecoveryConfig>() || !SystemAPI.HasSingleton<TimeState>())
            {
                return;
            }

            var config = SystemAPI.GetSingleton<CrashRecoveryConfig>();
            if (!config.AutoSaveEnabled)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var ticksSinceLastSnapshot = timeState.Tick - _lastSnapshotTick;

            if (ticksSinceLastSnapshot >= config.SnapshotIntervalTicks)
            {
                SaveSnapshot(ref state, timeState.Tick, config);
                _lastSnapshotTick = timeState.Tick;
                _snapshotCount++;

                // Clean up old snapshots if we exceed ring buffer size
                if (_snapshotCount > config.RingBufferSize)
                {
                    CleanupOldSnapshots(config.RingBufferSize);
                }
            }
        }

        private void SaveSnapshot(ref SystemState state, uint tick, CrashRecoveryConfig config)
        {
            try
            {
                var recoveryDir = Path.Combine(Application.persistentDataPath, "Recovery");
                if (!Directory.Exists(recoveryDir))
                {
                    Directory.CreateDirectory(recoveryDir);
                }

                var filePath = Path.Combine(recoveryDir, $"snapshot_{tick}.dat");
                var metadataPath = Path.Combine(recoveryDir, $"snapshot_{tick}.meta");

                // Serialize world state using TimeStreamWriter pattern
                using var buffer = new NativeList<byte>(Allocator.Temp);
                var writer = new TimeStreamWriter(ref buffer);

                // Write metadata
                writer.Write(tick);
                writer.Write(System.DateTime.UtcNow.Ticks);

                // Write entity count (simplified - full implementation would serialize all entities)
                var entityCount = state.EntityManager.GetAllEntities().Length;
                writer.Write(entityCount);

                // Write snapshot data
                File.WriteAllBytes(filePath, buffer.ToArray());

                // Write metadata JSON
                var metadata = new SnapshotMetadata
                {
                    Tick = tick,
                    Timestamp = System.DateTime.UtcNow.ToString("O"),
                    EntityCount = entityCount,
                    Hash = ComputeSnapshotHash(buffer.AsArray())
                };

                var metadataJson = JsonUtility.ToJson(metadata);
                File.WriteAllText(metadataPath, metadataJson);

                Debug.Log($"[CrashRecovery] Saved snapshot at tick {tick} ({_snapshotCount}/{config.RingBufferSize})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CrashRecovery] Failed to save snapshot: {ex}");
            }
        }

        private void CleanupOldSnapshots(int keepCount)
        {
            try
            {
                var recoveryDir = Path.Combine(Application.persistentDataPath, "Recovery");
                if (!Directory.Exists(recoveryDir))
                {
                    return;
                }

                var snapshotFiles = Directory.GetFiles(recoveryDir, "snapshot_*.dat");
                if (snapshotFiles.Length <= keepCount)
                {
                    return;
                }

                // Sort by tick (extract from filename)
                System.Array.Sort(snapshotFiles, (a, b) =>
                {
                    var tickA = ExtractTickFromFilename(a);
                    var tickB = ExtractTickFromFilename(b);
                    return tickA.CompareTo(tickB);
                });

                // Delete oldest snapshots
                var deleteCount = snapshotFiles.Length - keepCount;
                for (int i = 0; i < deleteCount; i++)
                {
                    File.Delete(snapshotFiles[i]);
                    var metaPath = snapshotFiles[i].Replace(".dat", ".meta");
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CrashRecovery] Failed to cleanup old snapshots: {ex}");
            }
        }

        private uint ExtractTickFromFilename(string filePath)
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

        private uint ComputeSnapshotHash(NativeArray<byte> data)
        {
            // Simple hash - in production would use proper hash algorithm
            uint hash = 2166136261u; // FNV-1a seed
            for (int i = 0; i < data.Length; i++)
            {
                hash ^= data[i];
                hash *= 16777619u;
            }
            return hash;
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

