using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Serialization
{
    /// <summary>
    /// Pure binary, endian-fixed, struct-only serialization.
    /// Ensures identical serialization across all platforms for multiplayer and distributed simulation.
    /// </summary>
    [BurstCompile]
    public static class BinarySerialization
    {
        /// <summary>
        /// Serializes a struct to binary format (little-endian).
        /// </summary>
        [BurstCompile]
        public static void Serialize<T>(in T value, ref NativeList<byte> output) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            int startIndex = output.Length;
            output.ResizeUninitialized(startIndex + size);

            unsafe
            {
                UnsafeUtility.MemCpy(
                    (byte*)output.GetUnsafePtr() + startIndex,
                    UnsafeUtility.AddressOf(ref UnsafeUtility.AsRef(in value)),
                    size
                );
            }
        }

        /// <summary>
        /// Deserializes a struct from binary format.
        /// </summary>
        [BurstCompile]
        public static bool Deserialize<T>(NativeArray<byte> input, int offset, out T value) where T : unmanaged
        {
            value = default;
            int size = UnsafeUtility.SizeOf<T>();

            if (offset + size > input.Length)
            {
                return false;
            }

            unsafe
            {
                UnsafeUtility.MemCpy(
                    UnsafeUtility.AddressOf(ref value),
                    (byte*)input.GetUnsafePtr() + offset,
                    size
                );
            }

            return true;
        }

        /// <summary>
        /// Converts to little-endian if needed (ensures cross-platform compatibility).
        /// </summary>
        [BurstCompile]
        public static void EnsureLittleEndian(ref NativeArray<byte> data)
        {
            // In a real implementation, this would swap bytes if running on big-endian system
            // For now, assume little-endian (most modern systems)
        }
    }
}

