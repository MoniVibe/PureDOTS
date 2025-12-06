using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Cache alignment helpers for 64-byte cache line boundaries.
    /// </summary>
    [BurstCompile]
    public static class CacheAlignmentHelpers
    {
        /// <summary>
        /// Cache line size in bytes (64 bytes for most modern CPUs).
        /// </summary>
        public const int CacheLineSize = 64;

        /// <summary>
        /// Aligns a size to cache line boundaries.
        /// </summary>
        [BurstCompile]
        public static int AlignToCacheLine(int size)
        {
            return (size + CacheLineSize - 1) & ~(CacheLineSize - 1);
        }

        /// <summary>
        /// Aligns a pointer to cache line boundaries.
        /// </summary>
        [BurstCompile]
        public static unsafe void* AlignToCacheLine(void* ptr)
        {
            ulong addr = (ulong)ptr;
            ulong aligned = (addr + CacheLineSize - 1) & ~(ulong)(CacheLineSize - 1);
            return (void*)aligned;
        }

        /// <summary>
        /// Checks if a pointer is cache-aligned.
        /// </summary>
        [BurstCompile]
        public static unsafe bool IsCacheAligned(void* ptr)
        {
            return ((ulong)ptr & (CacheLineSize - 1)) == 0;
        }

        /// <summary>
        /// Validates that a struct is cache-aligned.
        /// </summary>
        public static bool ValidateStructAlignment<T>() where T : struct
        {
            int size = Marshal.SizeOf<T>();
            int alignedSize = AlignToCacheLine(size);
            
            // Check if struct size is a multiple of cache line size
            bool isAligned = (size % CacheLineSize == 0) || (size == alignedSize);
            
            // Check if struct has StructLayout attribute with Pack = 64
            var layoutAttr = typeof(T).GetCustomAttributes(typeof(StructLayoutAttribute), false);
            if (layoutAttr.Length > 0)
            {
                var attr = (StructLayoutAttribute)layoutAttr[0];
                isAligned = isAligned && (attr.Pack == CacheLineSize);
            }

            return isAligned;
        }

        /// <summary>
        /// Gets recommended StructLayout attribute for cache alignment.
        /// </summary>
        public static string GetRecommendedLayoutAttribute()
        {
            return $"[StructLayout(LayoutKind.Sequential, Pack = {CacheLineSize})]";
        }
    }
}

