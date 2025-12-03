using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Restores world state from snapshots during global rewind operations.
    /// Works with per-component histories for refined playback.
    /// 
    /// CONCEPT: Snapshots are rare, coarse "checkpoints" that provide a baseline for rewinding.
    /// This system restores to the nearest checkpoint, then per-component histories handle
    /// fine-grained playback from the checkpoint to the target tick.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HistorySystemGroup))]
    [UpdateBefore(typeof(TimeHistoryPlaybackSystem))]
    public partial struct WorldSnapshotPlaybackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<WorldSnapshotState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Guard: Do not mutate history/snapshots in multiplayer modes
            if (SystemAPI.TryGetSingleton<TimeSystemFeatureFlags>(out var flags) &&
                flags.IsMultiplayerSession)
            {
                // For now, do not mutate history or snapshots in multiplayer modes.
                // When we implement MP, we can selectively allow modes like MP_SnapshotsOnly.
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            var snapshotState = SystemAPI.GetSingleton<WorldSnapshotState>();

            // Check for restore requests
            if (SystemAPI.TryGetSingletonRW<WorldSnapshotRestoreRequest>(out var requestHandle))
            {
                ref var request = ref requestHandle.ValueRW;
                if (request.IsPending)
                {
                    ProcessRestoreRequest(ref state, ref request, snapshotState);
                }
            }

            // During playback mode, restore from nearest snapshot if needed
            if (rewindState.Mode == RewindMode.Playback || rewindState.Mode == RewindMode.CatchUp)
            {
                uint targetTick = rewindState.Mode == RewindMode.Playback 
                    ? rewindState.PlaybackTick 
                    : timeState.Tick;

                // Only restore if we've jumped significantly (snapshot restoration is expensive)
                // Per-component histories handle fine-grained playback
            }
        }

        private void ProcessRestoreRequest(ref SystemState state, ref WorldSnapshotRestoreRequest request,
            in WorldSnapshotState snapshotState)
        {
            // Check feature flags - snapshots are single-player only
            if (SystemAPI.TryGetSingleton<TimeSystemFeatureFlags>(out var flags))
            {
                if (flags.SimulationMode != TimeSimulationMode.SinglePlayer)
                {
                    // Assert or log warning: snapshots not supported in multiplayer
                    SetRestoreResult(ref state, 0, 0, false, "World snapshots are single-player only");
                    request.IsPending = false;
                    return;
                }

                if (!flags.EnableWorldSnapshots)
                {
                    SetRestoreResult(ref state, 0, 0, false, "World snapshots are disabled");
                    request.IsPending = false;
                    return;
                }
            }

            var snapshotEntity = SystemAPI.GetSingletonEntity<WorldSnapshotState>();

            if (!state.EntityManager.HasBuffer<WorldSnapshotMeta>(snapshotEntity) ||
                !state.EntityManager.HasBuffer<WorldSnapshotData>(snapshotEntity))
            {
                SetRestoreResult(ref state, 0, 0, false, "No snapshot data available");
                request.IsPending = false;
                return;
            }

            var metaBuffer = state.EntityManager.GetBuffer<WorldSnapshotMeta>(snapshotEntity);
            var dataBuffer = state.EntityManager.GetBuffer<WorldSnapshotData>(snapshotEntity);

            // Find nearest snapshot at or before target tick
            int bestIndex = -1;
            uint bestTick = 0;

            for (int i = 0; i < metaBuffer.Length; i++)
            {
                var meta = metaBuffer[i];
                if (meta.IsValid && meta.Tick <= request.TargetTick && meta.Tick > bestTick)
                {
                    bestTick = meta.Tick;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                // Try to find any valid snapshot if exact match not found
                if (request.AllowInterpolation)
                {
                    for (int i = 0; i < metaBuffer.Length; i++)
                    {
                        var meta = metaBuffer[i];
                        if (meta.IsValid)
                        {
                            bestIndex = i;
                            break;
                        }
                    }
                }

                if (bestIndex < 0)
                {
                    SetRestoreResult(ref state, 0, 0, false, "No valid snapshot found");
                    request.IsPending = false;
                    return;
                }
            }

            var snapshotMeta = metaBuffer[bestIndex];
            int entitiesRestored = RestoreFromSnapshot(ref state, snapshotMeta, dataBuffer);

            SetRestoreResult(ref state, snapshotMeta.Tick, entitiesRestored, true, default);
            request.IsPending = false;
        }

        private int RestoreFromSnapshot(ref SystemState state, in WorldSnapshotMeta meta,
            DynamicBuffer<WorldSnapshotData> dataBuffer)
        {
            if (!meta.IsValid || meta.ByteLength == 0)
            {
                return 0;
            }

            // Extract snapshot data
            var snapshotData = new NativeArray<byte>(meta.ByteLength, Allocator.Temp);
            for (int i = 0; i < meta.ByteLength; i++)
            {
                snapshotData[i] = dataBuffer[meta.ByteOffset + i].Value;
            }

            int offset = 0;
            int entityCount = ReadInt(snapshotData, ref offset);
            int restoredCount = 0;

            // Build entity lookup
            var entityLookup = new NativeHashMap<int2, Entity>(entityCount, Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                .WithAny<RewindableTag, WorldSnapshotIncludeTag>()
                .WithEntityAccess())
            {
                var key = new int2(entity.Index, entity.Version);
                if (!entityLookup.ContainsKey(key))
                {
                    entityLookup.Add(key, entity);
                }
            }

            // Restore each entity
            for (int i = 0; i < entityCount; i++)
            {
                int entityIndex = ReadInt(snapshotData, ref offset);
                int entityVersion = ReadInt(snapshotData, ref offset);

                float3 position;
                position.x = ReadFloat(snapshotData, ref offset);
                position.y = ReadFloat(snapshotData, ref offset);
                position.z = ReadFloat(snapshotData, ref offset);

                quaternion rotation;
                rotation.value.x = ReadFloat(snapshotData, ref offset);
                rotation.value.y = ReadFloat(snapshotData, ref offset);
                rotation.value.z = ReadFloat(snapshotData, ref offset);
                rotation.value.w = ReadFloat(snapshotData, ref offset);

                float scale = ReadFloat(snapshotData, ref offset);

                var key = new int2(entityIndex, entityVersion);
                if (entityLookup.TryGetValue(key, out var entity) && 
                    state.EntityManager.Exists(entity) &&
                    state.EntityManager.HasComponent<LocalTransform>(entity))
                {
                    var transform = new LocalTransform
                    {
                        Position = position,
                        Rotation = rotation,
                        Scale = scale
                    };
                    state.EntityManager.SetComponentData(entity, transform);
                    restoredCount++;
                }
            }

            entityLookup.Dispose();
            snapshotData.Dispose();

            return restoredCount;
        }

        [BurstDiscard]
        private void SetRestoreResult(ref SystemState state, uint restoredTick, int entitiesRestored,
            bool success, FixedString64Bytes errorMessage)
        {
            Entity resultEntity;
            if (!SystemAPI.TryGetSingletonEntity<WorldSnapshotRestoreResult>(out resultEntity))
            {
                resultEntity = state.EntityManager.CreateEntity(typeof(WorldSnapshotRestoreResult));
            }

            state.EntityManager.SetComponentData(resultEntity, new WorldSnapshotRestoreResult
            {
                RestoredTick = restoredTick,
                EntitiesRestored = entitiesRestored,
                Success = success,
                ErrorMessage = errorMessage
            });
        }

        // Deserialization helpers
        private static int ReadInt(NativeArray<byte> buffer, ref int offset)
        {
            if (offset + 4 > buffer.Length)
            {
                return 0;
            }

            unsafe
            {
                byte* basePtr = (byte*)buffer.GetUnsafeReadOnlyPtr();
                byte* ptr = basePtr + offset;
                offset += 4;
                return *(int*)ptr;
            }
        }

        private static float ReadFloat(NativeArray<byte> buffer, ref int offset)
        {
            if (offset + 4 > buffer.Length)
            {
                return 0f;
            }

            unsafe
            {
                byte* basePtr = (byte*)buffer.GetUnsafeReadOnlyPtr();
                byte* ptr = basePtr + offset;
                offset += 4;
                return *(float*)ptr;
            }
        }
    }
}

