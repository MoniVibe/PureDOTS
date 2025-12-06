using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Temporal culling & prefetch for efficient timeline scrubbing.
    /// When scrubbing timeline:
    /// - Predict visible chunks (camera frustum/region)
    /// - Only decompress those chunks' deltas
    /// - Background job prefetches ±N ticks ahead
    /// Result: instant-scrub UX even at planetary scale.
    /// </summary>
    public struct TemporalCullingState : IComponentData
    {
        /// <summary>Current scrub position (tick).</summary>
        public uint ScrubTick;
        /// <summary>Prefetch window size (±N ticks).</summary>
        public uint PrefetchWindow;
        /// <summary>Whether culling is enabled.</summary>
        public bool CullingEnabled;
    }

    /// <summary>
    /// Camera frustum for temporal culling.
    /// </summary>
    public struct TemporalCullingFrustum : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float Fov;
        public float NearPlane;
        public float FarPlane;
        public float AspectRatio;
    }

    /// <summary>
    /// Region bounds for temporal culling (AABB).
    /// </summary>
    public struct TemporalCullingRegion : IComponentData
    {
        public float3 Min;
        public float3 Max;
    }

    /// <summary>
    /// Burst-compiled job for temporal culling.
    /// </summary>
    [BurstCompile]
    public struct TemporalCullingJob
    {
        [ReadOnly] public NativeArray<ChunkDelta> AllDeltas;
        [ReadOnly] public TemporalCullingFrustum Frustum;
        [ReadOnly] public TemporalCullingRegion Region;
        public uint ScrubTick;
        public uint PrefetchWindow;
        public NativeList<ChunkDelta> VisibleDeltas;

        public void Execute(ArchetypeChunk chunk)
        {
            // Determine if chunk is visible
            bool isVisible = IsChunkVisible(chunk);

            if (!isVisible)
            {
                return;
            }

            // Collect deltas for visible chunks in prefetch window
            uint minTick = ScrubTick > PrefetchWindow ? ScrubTick - PrefetchWindow : 0u;
            uint maxTick = ScrubTick + PrefetchWindow;

            for (int i = 0; i < AllDeltas.Length; i++)
            {
                var delta = AllDeltas[i];
                if (delta.Tick >= minTick && delta.Tick <= maxTick)
                {
                    int archetypeId = chunk.Archetype.ArchetypeTypeIndex.Value;
                    if (delta.ArchetypeId == archetypeId)
                    {
                        VisibleDeltas.Add(delta);
                    }
                }
            }
        }

        [BurstCompile]
        private bool IsChunkVisible(ArchetypeChunk chunk)
        {
            // Simplified visibility check - in practice, check chunk bounds against frustum/region
            // For now, assume all chunks are visible if no specific culling data
            return true;
        }
    }

    /// <summary>
    /// Prefetch job for temporal data.
    /// </summary>
    [BurstCompile]
    public struct TemporalPrefetchJob : IJob
    {
        [ReadOnly] public NativeArray<ChunkDelta> SourceDeltas;
        public uint CurrentTick;
        public uint PrefetchWindow;
        public NativeList<ChunkDelta> PrefetchedDeltas;

        public void Execute()
        {
            uint minTick = CurrentTick > PrefetchWindow ? CurrentTick - PrefetchWindow : 0u;
            uint maxTick = CurrentTick + PrefetchWindow;

            for (int i = 0; i < SourceDeltas.Length; i++)
            {
                var delta = SourceDeltas[i];
                if (delta.Tick >= minTick && delta.Tick <= maxTick)
                {
                    PrefetchedDeltas.Add(delta);
                }
            }
        }
    }

    /// <summary>
    /// Helper for temporal culling operations.
    /// </summary>
    [BurstCompile]
    public static class TemporalCullingHelper
    {
        /// <summary>
        /// Get visible chunks for current scrub position.
        /// </summary>
        [BurstCompile]
        public static NativeList<ChunkDelta> GetVisibleDeltas(
            ref SystemState state,
            uint scrubTick,
            uint prefetchWindow,
            NativeArray<ChunkDelta> allDeltas,
            Allocator allocator)
        {
            var visibleDeltas = new NativeList<ChunkDelta>(allDeltas.Length / 10, allocator);

            // Get culling frustum/region if available
            TemporalCullingFrustum frustum = default;
            TemporalCullingRegion region = default;
            bool hasFrustum = SystemAPI.TryGetSingleton<TemporalCullingFrustum>(out frustum);
            bool hasRegion = SystemAPI.TryGetSingleton<TemporalCullingRegion>(out region);

            if (!hasFrustum && !hasRegion)
            {
                // No culling - return all deltas in prefetch window
                uint minTick = scrubTick > prefetchWindow ? scrubTick - prefetchWindow : 0u;
                uint maxTick = scrubTick + prefetchWindow;

                for (int i = 0; i < allDeltas.Length; i++)
                {
                    var delta = allDeltas[i];
                    if (delta.Tick >= minTick && delta.Tick <= maxTick)
                    {
                        visibleDeltas.Add(delta);
                    }
                }

                return visibleDeltas;
            }

            // Apply culling
            var query = state.GetEntityQuery();
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);

            var cullingJob = new TemporalCullingJob
            {
                AllDeltas = allDeltas,
                Frustum = frustum,
                Region = region,
                ScrubTick = scrubTick,
                PrefetchWindow = prefetchWindow,
                VisibleDeltas = visibleDeltas
            };

            for (int i = 0; i < chunks.Length; i++)
            {
                cullingJob.Execute(chunks[i]);
            }

            chunks.Dispose();
            return visibleDeltas;
        }

        /// <summary>
        /// Prefetch deltas for ±N ticks around current position.
        /// </summary>
        [BurstCompile]
        public static NativeList<ChunkDelta> PrefetchDeltas(
            uint currentTick,
            uint prefetchWindow,
            NativeArray<ChunkDelta> sourceDeltas,
            Allocator allocator)
        {
            var prefetched = new NativeList<ChunkDelta>(sourceDeltas.Length / 5, allocator);

            var prefetchJob = new TemporalPrefetchJob
            {
                SourceDeltas = sourceDeltas,
                CurrentTick = currentTick,
                PrefetchWindow = prefetchWindow,
                PrefetchedDeltas = prefetched
            };

            prefetchJob.Execute();
            return prefetched;
        }
    }
}

