using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Lightweight space-filling curve helpers that expose Burst-friendly Morton (Z-order) encoding.
    /// Hilbert-based helpers were removed until their rotation tables can be represented without managed arrays.
    /// </summary>
    [BurstCompile]
    public static class SpaceFillingCurve
    {
        /// <summary>
        /// Encodes 3D integer coordinates into a 64-bit Morton (Z-order) key.
        /// </summary>
        [BurstCompile]
        public static ulong Morton3D(in int3 coords)
        {
            var x = (uint)math.clamp(coords.x, 0, 0x1FFFFF);
            var y = (uint)math.clamp(coords.y, 0, 0x1FFFFF);
            var z = (uint)math.clamp(coords.z, 0, 0x1FFFFF);

            x = Part1By2(x);
            y = Part1By2(y);
            z = Part1By2(z);

            return x | (y << 1) | ((ulong)z << 2);
        }

        /// <summary>
        /// Decodes a Morton key back into integer coordinates.
        /// </summary>
        [BurstCompile]
        public static void DecodeMorton(ulong mortonKey, out int3 result)
        {
            var x = Compact1By2((uint)(mortonKey & 0x249249249249249UL));
            var y = Compact1By2((uint)((mortonKey >> 1) & 0x249249249249249UL));
            var z = Compact1By2((uint)((mortonKey >> 2) & 0x249249249249249UL));

            result = new int3((int)x, (int)y, (int)z);
        }

        private static uint Part1By2(uint x)
        {
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;
            return x;
        }

        private static uint Compact1By2(uint x)
        {
            x &= 0x09249249;
            x = (x | (x >> 2)) & 0x030C30C3;
            x = (x | (x >> 4)) & 0x0300F00F;
            x = (x | (x >> 8)) & 0x030000FF;
            x = (x | (x >> 16)) & 0x000003FF;
            return x;
        }

        /// <summary>
        /// Convenience helper that quantizes a world position before Morton encoding.
        /// </summary>
        [BurstCompile]
        public static ulong Morton3DFromPosition(in float3 position, in SpatialGridConfig config)
        {
            SpatialHash.Quantize(position, config, out var coords);
            return Morton3D(in coords);
        }

        /// <summary>
        /// Convenience helper for Morton encoding of cell coordinates.
        /// </summary>
        [BurstCompile]
        public static ulong Morton3DFromCellCoords(in int3 cellCoords, in SpatialGridConfig config)
        {
            return Morton3D(in cellCoords);
        }
    }
}
