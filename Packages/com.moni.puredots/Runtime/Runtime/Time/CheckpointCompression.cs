using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Burst-compiled compression for checkpoint data.
    /// Uses run-length encoding + XOR delta compression.
    /// </summary>
    [BurstCompile]
    public static class CheckpointCompression
    {
        /// <summary>
        /// Compress data using run-length encoding + XOR delta.
        /// </summary>
        [BurstCompile]
        public static unsafe NativeArray<byte> Compress(
            NativeArray<byte> data,
            NativeArray<byte> previousData,
            Allocator allocator)
        {
            if (!data.IsCreated || data.Length == 0)
            {
                return default;
            }

            var compressed = new NativeList<byte>(data.Length / 2, allocator);

            byte* dataPtr = (byte*)data.GetUnsafePtr();
            byte* prevPtr = previousData.IsCreated ? (byte*)previousData.GetUnsafePtr() : null;

            int i = 0;
            while (i < data.Length)
            {
                byte value = dataPtr[i];
                byte delta = prevPtr != null ? (byte)(value ^ prevPtr[i]) : value;

                // Run-length encoding for repeated bytes
                int runLength = 1;
                while (i + runLength < data.Length && runLength < 255)
                {
                    byte nextValue = dataPtr[i + runLength];
                    byte nextDelta = prevPtr != null ? (byte)(nextValue ^ prevPtr[i + runLength]) : nextValue;
                    if (nextDelta != delta)
                    {
                        break;
                    }
                    runLength++;
                }

                if (runLength > 3 || delta == 0)
                {
                    // Encode run: [0xFF, delta, length]
                    compressed.Add(0xFF);
                    compressed.Add(delta);
                    compressed.Add((byte)runLength);
                }
                else
                {
                    // Encode literal bytes
                    for (int j = 0; j < runLength; j++)
                    {
                        compressed.Add(delta);
                    }
                }

                i += runLength;
            }

            return compressed.AsArray();
        }

        /// <summary>
        /// Decompress data using run-length decoding + XOR delta.
        /// </summary>
        [BurstCompile]
        public static unsafe NativeArray<byte> Decompress(
            NativeArray<byte> compressed,
            NativeArray<byte> previousData,
            int outputLength,
            Allocator allocator)
        {
            if (!compressed.IsCreated || compressed.Length == 0)
            {
                return default;
            }

            var decompressed = new NativeArray<byte>(outputLength, allocator, NativeArrayOptions.UninitializedMemory);

            byte* compPtr = (byte*)compressed.GetUnsafePtr();
            byte* decompPtr = (byte*)decompressed.GetUnsafePtr();
            byte* prevPtr = previousData.IsCreated ? (byte*)previousData.GetUnsafePtr() : null;

            int compIndex = 0;
            int decompIndex = 0;

            while (compIndex < compressed.Length && decompIndex < outputLength)
            {
                if (compPtr[compIndex] == 0xFF && compIndex + 2 < compressed.Length)
                {
                    // Run-length encoded
                    byte delta = compPtr[compIndex + 1];
                    byte length = compPtr[compIndex + 2];
                    compIndex += 3;

                    for (int i = 0; i < length && decompIndex < outputLength; i++)
                    {
                        byte value = prevPtr != null ? (byte)(delta ^ prevPtr[decompIndex]) : delta;
                        decompPtr[decompIndex++] = value;
                    }
                }
                else
                {
                    // Literal byte
                    byte delta = compPtr[compIndex++];
                    byte value = prevPtr != null ? (byte)(delta ^ prevPtr[decompIndex]) : delta;
                    decompPtr[decompIndex++] = value;
                }
            }

            return decompressed;
        }

        /// <summary>
        /// Estimate compression ratio (for memory budgeting).
        /// </summary>
        [BurstCompile]
        public static float EstimateCompressionRatio(int dataSize, int compressedSize)
        {
            if (dataSize == 0)
            {
                return 1.0f;
            }
            return (float)compressedSize / dataSize;
        }
    }
}

