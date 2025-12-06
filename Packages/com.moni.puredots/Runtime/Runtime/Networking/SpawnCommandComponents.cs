using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Spawn command for entity replication readiness.
    /// Integrates with existing Registry Infrastructure spawn system.
    /// In single-player, processes locally.
    /// In multiplayer, these same structs become spawn packets.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct SpawnCommand : IBufferElementData
    {
        /// <summary>
        /// Prefab identifier for spawning.
        /// In single-player, this maps to a prefab entity.
        /// In multiplayer, this is a network-persistent prefab ID.
        /// </summary>
        public ulong PrefabId;

        /// <summary>
        /// Spawn position in world space.
        /// </summary>
        public float3 Pos;

        /// <summary>
        /// Spawn rotation.
        /// </summary>
        public quaternion Rot;

        /// <summary>
        /// Optional player ID for ownership assignment.
        /// </summary>
        public ulong OwnerPlayerId;

        /// <summary>
        /// Optional tick when spawn should occur (for deterministic timing).
        /// </summary>
        public uint SpawnTick;
    }

    /// <summary>
    /// Tag component marking the singleton entity that owns the spawn command queue.
    /// </summary>
    public struct SpawnCommandQueueTag : IComponentData { }

    /// <summary>
    /// State tracking for spawn command processing.
    /// </summary>
    public struct SpawnCommandState : IComponentData
    {
        public int TotalSpawned;
        public int FailedSpawns;
        public uint LastProcessedTick;
    }
}

