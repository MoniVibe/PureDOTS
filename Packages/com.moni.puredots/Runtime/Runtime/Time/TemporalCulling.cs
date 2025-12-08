using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
        public static void GetVisibleDeltas(
            ref SystemState state,
            uint scrubTick,
            uint prefetchWindow,
            in NativeArray<ChunkDelta> allDeltas,
            Allocator allocator,
            out NativeList<ChunkDelta> visibleDeltas)
        {
            visibleDeltas = new NativeList<ChunkDelta>(allDeltas.Length / 10, allocator);

            // Get culling frustum/region if available (keep SystemAPI out of static helpers)
            var frustumQuery = state.GetEntityQuery(ComponentType.ReadOnly<TemporalCullingFrustum>());
            var regionQuery = state.GetEntityQuery(ComponentType.ReadOnly<TemporalCullingRegion>());
            bool hasFrustum = frustumQuery.TryGetSingleton(out TemporalCullingFrustum frustum);
            bool hasRegion = regionQuery.TryGetSingleton(out TemporalCullingRegion region);
            _ = hasFrustum;
            _ = hasRegion;

            uint minTick = scrubTick > prefetchWindow ? scrubTick - prefetchWindow : 0u;
            uint maxTick = scrubTick + prefetchWindow;

            for (int i = 0; i < allDeltas.Length; i++)
            {
                var delta = allDeltas[i];
                if (delta.Tick < minTick || delta.Tick > maxTick)
                {
                    continue;
                }

                // TODO: once chunk bounds are tracked, incorporate frustum/region checks.
                // For now, temporal window is the primary filter.
                visibleDeltas.Add(delta);
            }

        }

        /// <summary>
        /// Prefetch deltas for ±N ticks around current position.
        /// </summary>
        [BurstCompile]
        public static void PrefetchDeltas(
            uint currentTick,
            uint prefetchWindow,
            in NativeArray<ChunkDelta> sourceDeltas,
            Allocator allocator,
            out NativeList<ChunkDelta> prefetched)
        {
            prefetched = new NativeList<ChunkDelta>(sourceDeltas.Length / 5, allocator);

            var prefetchJob = new TemporalPrefetchJob
            {
                SourceDeltas = sourceDeltas,
                CurrentTick = currentTick,
                PrefetchWindow = prefetchWindow,
                PrefetchedDeltas = prefetched
            };

            prefetchJob.Execute();
        }
    }
}

