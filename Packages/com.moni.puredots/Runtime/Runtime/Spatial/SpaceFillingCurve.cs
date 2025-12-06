using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Space-filling curve utilities for spatial indexing.
    /// Provides Morton (Z-order) and Hilbert curve encoding/decoding for cache-coherent spatial queries.
    /// All methods are Burst-compiled and deterministic.
    /// 
    /// <para>
    /// <b>Usage Example:</b>
    /// <code>
    /// // Encode cell coordinates to Morton key
    /// SpatialHash.Quantize(position, config, out var coords);
    /// var cellKey = SpaceFillingCurve.Morton3D(in coords);
    /// 
    /// // Decode Morton key back to coordinates
    /// var decodedCoords = SpaceFillingCurve.DecodeMorton(cellKey);
    /// </code>
    /// </para>
    /// 
    /// <para>
    /// <b>Performance:</b> Morton encoding is faster than Hilbert but provides slightly less locality.
    /// Use Morton for most cases, Hilbert for maximum cache coherence in dense queries.
    /// </para>
    /// 
    /// See also: <see cref="SpatialGridEntry.CellKey"/>, <see cref="SpatialQueryHelper.CollectEntitiesInRadiusSFC"/>
    /// </summary>
    [BurstCompile]
    public static class SpaceFillingCurve
    {
        /// <summary>
        /// Encodes 3D integer coordinates into a 64-bit Morton (Z-order) key.
        /// Each coordinate is interleaved bitwise: x[bit0] y[bit0] z[bit0] x[bit1] y[bit1] z[bit1] ...
        /// </summary>
        [BurstCompile]
        public static ulong Morton3D(in int3 coords)
        {
            // Clamp coordinates to valid range (21 bits per axis = 2^21 = 2,097,152 max)
            // This allows 63 bits total (21*3) + 1 sign bit = 64 bits
            var x = (uint)math.clamp(coords.x, 0, 0x1FFFFF);
            var y = (uint)math.clamp(coords.y, 0, 0x1FFFFF);
            var z = (uint)math.clamp(coords.z, 0, 0x1FFFFF);

            // Spread bits: interleave zeros between bits
            x = Part1By2(x);
            y = Part1By2(y);
            z = Part1By2(z);

            // Combine: x[bit0] y[bit0] z[bit0] x[bit1] y[bit1] z[bit1] ...
            return x | (y << 1) | ((ulong)z << 2);
        }

        /// <summary>
        /// Decodes a 64-bit Morton key back into 3D integer coordinates.
        /// </summary>
        [BurstCompile]
        public static int3 DecodeMorton(ulong mortonKey)
        {
            // Extract x, y, z by de-interleaving bits
            var x = Compact1By2((uint)(mortonKey & 0x249249249249249UL));
            var y = Compact1By2((uint)((mortonKey >> 1) & 0x249249249249249UL));
            var z = Compact1By2((uint)((mortonKey >> 2) & 0x249249249249249UL));

            return new int3((int)x, (int)y, (int)z);
        }

        /// <summary>
        /// Encodes 3D integer coordinates into a 64-bit Hilbert curve key.
        /// Hilbert curves provide better locality than Morton but are more expensive to compute.
        /// </summary>
        /// <param name="coords">3D integer coordinates</param>
        /// <param name="order">Hilbert curve order (1-10, determines resolution). Higher = more precision but slower.</param>
        [BurstCompile]
        public static ulong Hilbert3D(in int3 coords, int order = 7)
        {
            // Clamp order to valid range
            order = math.clamp(order, 1, 10);
            var maxCoord = (1u << order) - 1u;

            var x = (uint)math.clamp(coords.x, 0, (int)maxCoord);
            var y = (uint)math.clamp(coords.y, 0, (int)maxCoord);
            var z = (uint)math.clamp(coords.z, 0, (int)maxCoord);

            ulong h = 0;
            uint rotation = 0;

            for (int i = order - 1; i >= 0; i--)
            {
                var bit = (uint)(1u << i);
                var xBit = (x & bit) != 0 ? 1u : 0u;
                var yBit = (y & bit) != 0 ? 1u : 0u;
                var zBit = (z & bit) != 0 ? 1u : 0u;

                var octant = (xBit << 2) | (yBit << 1) | zBit;
                octant = RotateOctant(octant, rotation);
                h = (h << 3) | octant;
                rotation = UpdateRotation(rotation, octant);
            }

            return h;
        }

        /// <summary>
        /// Decodes a 64-bit Hilbert key back into 3D integer coordinates.
        /// </summary>
        /// <param name="hilbertKey">Hilbert curve key</param>
        /// <param name="order">Hilbert curve order (must match encoding order)</param>
        [BurstCompile]
        public static int3 DecodeHilbert(ulong hilbertKey, int order = 7)
        {
            order = math.clamp(order, 1, 10);
            uint x = 0, y = 0, z = 0;
            uint rotation = 0;

            for (int i = order - 1; i >= 0; i--)
            {
                var octant = (uint)((hilbertKey >> (i * 3)) & 7u);
                octant = RotateOctantInverse(octant, rotation);

                var bit = (uint)(1u << i);
                if ((octant & 4) != 0) x |= bit;
                if ((octant & 2) != 0) y |= bit;
                if ((octant & 1) != 0) z |= bit;

                rotation = UpdateRotation(rotation, octant);
            }

            return new int3((int)x, (int)y, (int)z);
        }

        /// <summary>
        /// Spreads bits by inserting two zeros between each bit (for Morton encoding).
        /// Example: 1011 -> 001000001001
        /// </summary>
        private static uint Part1By2(uint x)
        {
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;
            return x;
        }

        /// <summary>
        /// Compacts bits by removing zeros (for Morton decoding).
        /// Inverse of Part1By2.
        /// </summary>
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
        /// Rotates an octant based on the current rotation state (for Hilbert encoding).
        /// </summary>
        private static uint RotateOctant(uint octant, uint rotation)
        {
            // Hilbert curve rotation table
            var rotations = new uint[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, // Identity
                1, 0, 3, 2, 5, 4, 7, 6, // Swap x
                2, 3, 0, 1, 6, 7, 4, 5, // Swap y
                3, 2, 1, 0, 7, 6, 5, 4, // Swap x and y
                4, 5, 6, 7, 0, 1, 2, 3, // Swap z
                5, 4, 7, 6, 1, 0, 3, 2, // Swap x and z
                6, 7, 4, 5, 2, 3, 0, 1, // Swap y and z
                7, 6, 5, 4, 3, 2, 1, 0  // Swap all
            };

            if (rotation >= 8) return octant;
            return rotations[rotation * 8 + octant];
        }

        /// <summary>
        /// Inverse rotation for Hilbert decoding.
        /// </summary>
        private static uint RotateOctantInverse(uint octant, uint rotation)
        {
            // Inverse rotation table (same as forward for Hilbert curves)
            return RotateOctant(octant, rotation);
        }

        /// <summary>
        /// Updates rotation state based on current octant (for Hilbert encoding).
        /// </summary>
        private static uint UpdateRotation(uint currentRotation, uint octant)
        {
            // Hilbert curve rotation update table
            var nextRotations = new uint[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, // From rotation 0
                1, 0, 3, 2, 5, 4, 7, 6, // From rotation 1
                2, 3, 0, 1, 6, 7, 4, 5, // From rotation 2
                3, 2, 1, 0, 7, 6, 5, 4, // From rotation 3
                4, 5, 6, 7, 0, 1, 2, 3, // From rotation 4
                5, 4, 7, 6, 1, 0, 3, 2, // From rotation 5
                6, 7, 4, 5, 2, 3, 0, 1, // From rotation 6
                7, 6, 5, 4, 3, 2, 1, 0  // From rotation 7
            };

            if (currentRotation >= 8 || octant >= 8) return currentRotation;
            return nextRotations[currentRotation * 8 + octant];
        }

        /// <summary>
        /// Computes Morton key from world position and grid config.
        /// Convenience method that quantizes position first.
        /// </summary>
        [BurstCompile]
        public static ulong Morton3DFromPosition(in float3 position, in SpatialGridConfig config, HierarchicalGridLevel? level = null)
        {
            SpatialHash.Quantize(position, config, out var coords);
            return Morton3D(in coords);
        }

        /// <summary>
        /// Computes Morton key from cell coordinates and config.
        /// </summary>
        [BurstCompile]
        public static ulong Morton3DFromCellCoords(in int3 cellCoords, in SpatialGridConfig config)
        {
            return Morton3D(in cellCoords);
        }
    }
}

