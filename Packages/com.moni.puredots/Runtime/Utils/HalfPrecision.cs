using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Utils
{
    /// <summary>
    /// Utility for adaptive data precision using half type.
    /// Provides Burst-compatible conversion helpers for half precision.
    /// </summary>
    [BurstCompile]
    public static class HalfPrecision
    {
        /// <summary>
        /// Converts half to float.
        /// </summary>
        [BurstCompile]
        public static float HalfToFloat(half h) => (float)h;

        /// <summary>
        /// Converts float to half.
        /// </summary>
        [BurstCompile]
        public static half FloatToHalf(float f) => (half)f;

        /// <summary>
        /// Converts half2 to float2.
        /// </summary>
        [BurstCompile]
        public static float2 Half2ToFloat2(half2 h) => new float2((float)h.x, (float)h.y);

        /// <summary>
        /// Converts float2 to half2.
        /// </summary>
        [BurstCompile]
        public static half2 Float2ToHalf2(float2 f) => new half2((half)f.x, (half)f.y);
    }
}

