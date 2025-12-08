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
        public static void HalfToFloat(in half h, out float result)
        {
            result = (float)h;
        }

        /// <summary>
        /// Converts float to half.
        /// </summary>
        [BurstCompile]
        public static void FloatToHalf(float f, out half result)
        {
            result = (half)f;
        }

        /// <summary>
        /// Converts half2 to float2.
        /// </summary>
        [BurstCompile]
        public static void Half2ToFloat2(in half2 h, out float2 result)
        {
            result = new float2((float)h.x, (float)h.y);
        }

        /// <summary>
        /// Converts float2 to half2.
        /// </summary>
        [BurstCompile]
        public static void Float2ToHalf2(in float2 f, out half2 result)
        {
            result = new half2((half)f.x, (half)f.y);
        }
    }
}
