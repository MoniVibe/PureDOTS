using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Coordinator for multi-world time sync.
    /// Each ECS world keeps its own timeline buffer.
    /// RewindCoordinator syncs by global tick ratio.
    /// Godgame (planet): 1:1 rewind
    /// Space4X (galaxy): rewind every 1000 planet ticks → slow-motion galactic time.
    /// </summary>
    public struct WorldTimeline
    {
        public uint LocalTick;
        public uint GlobalTick;
        public float TickRatio; // GlobalTick / LocalTick ratio
        public ChunkDeltaStorage DeltaStorage;
    }

    /// <summary>
    /// Multi-world rewind coordinator.
    /// </summary>
    public struct RewindCoordinator : System.IDisposable
    {
        private NativeHashMap<int, WorldTimeline> _worldTimelines;
        private int _nextWorldId;

        public bool IsCreated => _worldTimelines.IsCreated;

        public RewindCoordinator(int initialCapacity, Allocator allocator)
        {
            _worldTimelines = new NativeHashMap<int, WorldTimeline>(initialCapacity, allocator);
            _nextWorldId = 0;
        }

        public void Dispose()
        {
            if (_worldTimelines.IsCreated)
            {
                foreach (var kvp in _worldTimelines)
                {
                    kvp.Value.DeltaStorage.Dispose();
                }
                _worldTimelines.Dispose();
            }
        }

        /// <summary>
        /// Register a world with the coordinator.
        /// </summary>
        public int RegisterWorld(float tickRatio, int deltaStorageCapacity, Allocator allocator)
        {
            int worldId = _nextWorldId++;
            var timeline = new WorldTimeline
            {
                LocalTick = 0u,
                GlobalTick = 0u,
                TickRatio = tickRatio,
                DeltaStorage = new ChunkDeltaStorage(deltaStorageCapacity, allocator)
            };

            _worldTimelines[worldId] = timeline;
            return worldId;
        }

        /// <summary>
        /// Update world timeline based on global tick.
        /// </summary>
        public void UpdateWorld(int worldId, uint globalTick)
        {
            if (!_worldTimelines.TryGetValue(worldId, out var timeline))
            {
                return;
            }

            timeline.GlobalTick = globalTick;
            timeline.LocalTick = (uint)(globalTick / timeline.TickRatio);
            _worldTimelines[worldId] = timeline;
        }

        /// <summary>
        /// Rewind all worlds to target global tick.
        /// </summary>
        [BurstCompile]
        public void RewindToGlobalTick(uint targetGlobalTick, Allocator allocator)
        {
            foreach (var kvp in _worldTimelines)
            {
                var timeline = kvp.Value;
                uint targetLocalTick = (uint)(targetGlobalTick / timeline.TickRatio);

                // Rewind this world's timeline
                // In practice, this would trigger rewind in the world's RewindCoordinatorSystem
                timeline.LocalTick = targetLocalTick;
                timeline.GlobalTick = targetGlobalTick;
                _worldTimelines[kvp.Key] = timeline;
            }
        }

        /// <summary>
        /// Get world timeline.
        /// </summary>
        public bool TryGetWorldTimeline(int worldId, out WorldTimeline timeline)
        {
            return _worldTimelines.TryGetValue(worldId, out timeline);
        }

        /// <summary>
        /// Get all registered world IDs.
        /// </summary>
        public NativeArray<int> GetWorldIds(Allocator allocator)
        {
            var ids = new NativeList<int>(_worldTimelines.Count, allocator);
            foreach (var kvp in _worldTimelines)
            {
                ids.Add(kvp.Key);
            }
            return ids.ToArray(allocator);
        }
    }

    /// <summary>
    /// Singleton component for multi-world rewind coordination.
    /// </summary>
    public struct MultiWorldRewindState : IComponentData
    {
        /// <summary>Global tick across all worlds.</summary>
        public uint GlobalTick;
        /// <summary>World ID for this world.</summary>
        public int WorldId;
        /// <summary>Tick ratio for this world (GlobalTick / LocalTick).</summary>
        public float TickRatio;
    }

    /// <summary>
    /// Helper constants for multi-world sync.
    /// </summary>
    public static class MultiWorldSyncConstants
    {
        /// <summary>Godgame (planet): 1:1 rewind ratio.</summary>
        public const float GodgameTickRatio = 1.0f;

        /// <summary>Space4X (galaxy): rewind every 1000 planet ticks.</summary>
        public const float Space4XTickRatio = 1000.0f;
    }
}

