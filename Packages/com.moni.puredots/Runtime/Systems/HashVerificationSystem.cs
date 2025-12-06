using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems
{
    /// <summary>
    /// System for verifying deterministic rewind by comparing component hashes.
    /// Compare component hashes before/after rewind → must match bit-for-bit.
    /// Any mismatch pinpoints non-deterministic system.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TimeSystemGroup))]
    public partial struct HashVerificationSystem : ISystem
    {
        private NativeHashMap<uint, ulong> _hashSnapshots;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _hashSnapshots = new NativeHashMap<uint, ulong>(1000, Allocator.Persistent);
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_hashSnapshots.IsCreated)
            {
                _hashSnapshots.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode == RewindMode.Record)
            {
                // Record hash snapshot
                ulong hash = ComputeWorldHash(ref state);
                _hashSnapshots[timeState.Tick] = hash;
            }
            else if (rewindState.Mode == RewindMode.Playback)
            {
                // Verify hash matches
                uint playbackTick = rewindState.PlaybackTick;
                if (_hashSnapshots.TryGetValue(playbackTick, out ulong expectedHash))
                {
                    ulong actualHash = ComputeWorldHash(ref state);
                    if (actualHash != expectedHash)
                    {
                        // Hash mismatch - non-deterministic behavior detected
                        // In practice, log this and identify which system caused it
                        UnityEngine.Debug.LogError($"Hash mismatch at tick {playbackTick}: expected {expectedHash}, got {actualHash}");
                    }
                }
            }
        }

        /// <summary>
        /// Compute hash of all component data in the world.
        /// </summary>
        [BurstCompile]
        private ulong ComputeWorldHash(ref SystemState state)
        {
            ulong hash = 0ul;

            // Hash all entities and their components
            var query = state.GetEntityQuery();
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);

            for (int chunkIdx = 0; chunkIdx < chunks.Length; chunkIdx++)
            {
                var chunk = chunks[chunkIdx];
                var archetype = chunk.Archetype;
                var componentTypes = archetype.GetComponentTypes();

                // Hash chunk archetype
                hash ^= (ulong)archetype.ArchetypeTypeIndex.Value;
                hash = math.rol(hash, 13);

                // Hash component data
                for (int typeIdx = 0; typeIdx < componentTypes.Length; typeIdx++)
                {
                    if (componentTypes[typeIdx].IsZeroSized)
                    {
                        continue;
                    }

                    unsafe
                    {
                        byte* componentPtr = (byte*)chunk.GetComponentDataPtrRO(typeIdx);
                        int componentSize = componentTypes[typeIdx].SizeInChunk;
                        int chunkDataSize = componentSize * chunk.Count;

                        // Hash component data
                        for (int i = 0; i < chunkDataSize; i++)
                        {
                            hash ^= (ulong)componentPtr[i];
                            hash = math.rol(hash, 7);
                        }
                    }
                }
            }

            chunks.Dispose();
            return hash;
        }

        /// <summary>
        /// Get hash for specific tick (for external verification).
        /// </summary>
        public bool TryGetHash(uint tick, out ulong hash)
        {
            return _hashSnapshots.TryGetValue(tick, out hash);
        }

        /// <summary>
        /// Clear hash snapshots (for memory management).
        /// </summary>
        public void ClearSnapshots()
        {
            _hashSnapshots.Clear();
        }
    }
}

