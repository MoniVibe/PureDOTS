using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Burst-compiled jobs for delta compression of chunk data.
    /// Uses XOR encoding to store only changed bytes.
    /// </summary>
    [BurstCompile]
    public static class ChunkDeltaCompression
    {
        /// <summary>
        /// Compute delta between two chunk states using XOR.
        /// Only stores bytes that changed.
        /// </summary>
        [BurstCompile]
        public static unsafe ChunkDelta ComputeDelta(
            ArchetypeChunk currentChunk,
            NativeArray<byte> previousState,
            uint tick,
            Allocator allocator)
        {
            var archetype = currentChunk.Archetype;
            int archetypeId = archetype.ArchetypeTypeIndex.Value;
            int entityCount = currentChunk.Count;

            // Get all component types in this archetype
            var componentTypes = archetype.GetComponentTypes();
            var changedIndices = new NativeList<int>(componentTypes.Length, Allocator.Temp);
            var offsets = new NativeList<int>(componentTypes.Length, Allocator.Temp);
            var sizes = new NativeList<int>(componentTypes.Length, Allocator.Temp);

            // Calculate total size needed
            int totalSize = 0;
            for (int typeIndex = 0; typeIndex < componentTypes.Length; typeIndex++)
            {
                var componentType = componentTypes[typeIndex];
                if (componentType.IsZeroSized)
                {
                    continue;
                }

                int componentSize = componentType.SizeInChunk;
                int chunkDataSize = componentSize * entityCount;

                offsets.Add(totalSize);
                sizes.Add(chunkDataSize);
                changedIndices.Add(typeIndex);
                totalSize += chunkDataSize;
            }

            // Allocate delta buffer
            var deltaBytes = new NativeArray<byte>(totalSize, allocator, NativeArrayOptions.UninitializedMemory);

            // Compute XOR delta
            int deltaOffset = 0;
            for (int i = 0; i < changedIndices.Length; i++)
            {
                int typeIndex = changedIndices[i];
                int componentSize = componentTypes[typeIndex].SizeInChunk;
                int chunkDataSize = componentSize * entityCount;
                int offset = offsets[i];

                // Get current chunk data
                byte* currentPtr = (byte*)currentChunk.GetComponentDataPtrRO(typeIndex);
                byte* deltaPtr = (byte*)deltaBytes.GetUnsafePtr() + deltaOffset;

                if (previousState.IsCreated && previousState.Length > offset + chunkDataSize)
                {
                    // XOR with previous state
                    byte* prevPtr = (byte*)previousState.GetUnsafePtr() + offset;
                    for (int j = 0; j < chunkDataSize; j++)
                    {
                        deltaPtr[j] = (byte)(currentPtr[j] ^ prevPtr[j]);
                    }
                }
                else
                {
                    // No previous state - store current state as-is
                    UnsafeUtility.MemCpy(deltaPtr, currentPtr, chunkDataSize);
                }

                deltaOffset += chunkDataSize;
            }

            var delta = new ChunkDelta(archetypeId, entityCount, tick, allocator)
            {
                ChangedBytes = deltaBytes,
                ChangedComponentIndices = changedIndices.ToArray(allocator),
                ComponentOffsets = offsets.ToArray(allocator),
                ComponentSizes = sizes.ToArray(allocator)
            };

            changedIndices.Dispose();
            offsets.Dispose();
            sizes.Dispose();

            return delta;
        }

        /// <summary>
        /// Apply delta to restore chunk state.
        /// </summary>
        [BurstCompile]
        public static unsafe void ApplyDelta(
            ArchetypeChunk chunk,
            ChunkDelta delta,
            NativeArray<byte> targetState)
        {
            if (!delta.IsCreated || !targetState.IsCreated)
            {
                return;
            }

            var archetype = chunk.Archetype;
            int entityCount = chunk.Count;

            if (entityCount != delta.EntityCount)
            {
                // Entity count mismatch - skip this delta
                return;
            }

            // Apply XOR deltas to restore state
            int deltaOffset = 0;
            for (int i = 0; i < delta.ChangedComponentIndices.Length; i++)
            {
                int typeIndex = delta.ChangedComponentIndices[i];
                int componentSize = archetype.GetComponentType(typeIndex).SizeInChunk;
                int chunkDataSize = componentSize * entityCount;
                int targetOffset = delta.ComponentOffsets[i];

                byte* deltaPtr = (byte*)delta.ChangedBytes.GetUnsafePtr() + deltaOffset;
                byte* targetPtr = (byte*)targetState.GetUnsafePtr() + targetOffset;
                byte* currentPtr = (byte*)chunk.GetComponentDataPtrRW(typeIndex);

                // XOR delta with current state to restore previous state
                for (int j = 0; j < chunkDataSize; j++)
                {
                    targetPtr[j] = (byte)(currentPtr[j] ^ deltaPtr[j]);
                }

                deltaOffset += chunkDataSize;
            }
        }

        /// <summary>
        /// Store current chunk state as baseline (for full snapshot).
        /// </summary>
        [BurstCompile]
        public static unsafe NativeArray<byte> StoreBaseline(
            ArchetypeChunk chunk,
            Allocator allocator)
        {
            var archetype = chunk.Archetype;
            int entityCount = chunk.Count;
            var componentTypes = archetype.GetComponentTypes();

            // Calculate total size
            int totalSize = 0;
            for (int typeIndex = 0; typeIndex < componentTypes.Length; typeIndex++)
            {
                if (componentTypes[typeIndex].IsZeroSized)
                {
                    continue;
                }
                totalSize += componentTypes[typeIndex].SizeInChunk * entityCount;
            }

            var baseline = new NativeArray<byte>(totalSize, allocator, NativeArrayOptions.UninitializedMemory);

            // Copy all component data
            int offset = 0;
            for (int typeIndex = 0; typeIndex < componentTypes.Length; typeIndex++)
            {
                if (componentTypes[typeIndex].IsZeroSized)
                {
                    continue;
                }

                int componentSize = componentTypes[typeIndex].SizeInChunk;
                int chunkDataSize = componentSize * entityCount;

                byte* srcPtr = (byte*)chunk.GetComponentDataPtrRO(typeIndex);
                byte* dstPtr = (byte*)baseline.GetUnsafePtr() + offset;

                UnsafeUtility.MemCpy(dstPtr, srcPtr, chunkDataSize);
                offset += chunkDataSize;
            }

            return baseline;
        }

        /// <summary>
        /// Restore chunk from baseline state.
        /// </summary>
        [BurstCompile]
        public static unsafe void RestoreBaseline(
            ArchetypeChunk chunk,
            NativeArray<byte> baseline)
        {
            if (!baseline.IsCreated)
            {
                return;
            }

            var archetype = chunk.Archetype;
            int entityCount = chunk.Count;
            var componentTypes = archetype.GetComponentTypes();

            int offset = 0;
            for (int typeIndex = 0; typeIndex < componentTypes.Length; typeIndex++)
            {
                if (componentTypes[typeIndex].IsZeroSized)
                {
                    continue;
                }

                int componentSize = componentTypes[typeIndex].SizeInChunk;
                int chunkDataSize = componentSize * entityCount;

                byte* srcPtr = (byte*)baseline.GetUnsafePtr() + offset;
                byte* dstPtr = (byte*)chunk.GetComponentDataPtrRW(typeIndex);

                UnsafeUtility.MemCpy(dstPtr, srcPtr, chunkDataSize);
                offset += chunkDataSize;
            }
        }
    }
}

