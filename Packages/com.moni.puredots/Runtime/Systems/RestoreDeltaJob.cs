using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Time;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Job to restore chunk state from delta.
    /// Reads chunk delta and applies reverse writes to component memory.
    /// Time complexity: O(k × changedChunks), k = Δ ticks.
    /// </summary>
    [BurstCompile]
    public struct RestoreDeltaJob : IJobEntityBatch
    {
        [ReadOnly] public NativeArray<ChunkDelta> Deltas;
        public uint TargetTick;
        public ArchetypeChunk Chunk;

        public void Execute(ArchetypeChunk chunk, int batchIndex)
        {
            // Find delta for this chunk at target tick
            int archetypeId = chunk.Archetype.ArchetypeTypeIndex.Value;
            
            for (int i = 0; i < Deltas.Length; i++)
            {
                var delta = Deltas[i];
                if (delta.Tick == TargetTick && delta.ArchetypeId == archetypeId)
                {
                    // Apply delta to restore state
                    var baseline = ChunkDeltaCompression.StoreBaseline(chunk, Allocator.Temp);
                    ChunkDeltaCompression.ApplyDelta(chunk, delta, baseline);
                    baseline.Dispose();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Helper for incremental rewind operations.
    /// </summary>
    [BurstCompile]
    public static class IncrementalRewindHelper
    {
        /// <summary>
        /// Perform incremental rewind from currentTick to targetTick.
        /// Algorithm:
        ///   targetTick = currentTick - Δ
        ///   while (currentTick > targetTick)
        ///       RestoreDelta(--currentTick)
        /// </summary>
        [BurstCompile]
        public static void RewindIncremental(
            ref SystemState state,
            uint currentTick,
            uint targetTick,
            NativeArray<ChunkDelta> deltas,
            Allocator allocator)
        {
            if (currentTick <= targetTick)
            {
                return;
            }

            uint tick = currentTick;
            while (tick > targetTick)
            {
                // Restore delta for this tick
                RestoreDeltaAtTick(ref state, tick, deltas, allocator);
                tick--;
            }
        }

        /// <summary>
        /// Restore delta at specific tick.
        /// </summary>
        [BurstCompile]
        private static void RestoreDeltaAtTick(
            ref SystemState state,
            uint tick,
            NativeArray<ChunkDelta> deltas,
            Allocator allocator)
        {
            // Find deltas for this tick
            var tickDeltas = new NativeList<ChunkDelta>(deltas.Length / 10, allocator);
            for (int i = 0; i < deltas.Length; i++)
            {
                if (deltas[i].Tick == tick)
                {
                    tickDeltas.Add(deltas[i]);
                }
            }

            if (tickDeltas.Length == 0)
            {
                tickDeltas.Dispose();
                return;
            }

            // Apply deltas - iterate through all chunks
            var query = state.GetEntityQuery();
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);

            var job = new RestoreDeltaJob
            {
                Deltas = tickDeltas.AsArray(),
                TargetTick = tick
            };

            for (int chunkIdx = 0; chunkIdx < chunks.Length; chunkIdx++)
            {
                job.Execute(chunks[chunkIdx]);
            }

            chunks.Dispose();
            tickDeltas.Dispose();
        }
    }
}

