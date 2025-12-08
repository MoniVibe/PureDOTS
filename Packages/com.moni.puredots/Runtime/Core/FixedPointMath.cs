using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Fixed-point math utilities for deterministic economy and power systems.
    /// Uses int64 with fixed decimal precision to avoid float drift.
    /// </summary>
    [BurstCompile]
    public static class FixedPointMath
    {
        /// <summary>
        /// Fixed-point scale: 1.0 = 10000 units (4 decimal places precision).
        /// </summary>
        public const long Scale = 10000L;
        public const long HalfScale = Scale / 2L;

        /// <summary>
        /// Converts float to fixed-point int64.
        /// </summary>
        [BurstCompile]
        public static long ToFixed(float value)
        {
            return (long)math.round(value * Scale);
        }

        /// <summary>
        /// Converts fixed-point int64 to float.
        /// </summary>
        [BurstCompile]
        public static float ToFloat(long fixedValue)
        {
            return fixedValue / (float)Scale;
        }

        /// <summary>
        /// Adds two fixed-point values.
        /// </summary>
        [BurstCompile]
        public static long Add(long a, long b)
        {
            return a + b;
        }

        /// <summary>
        /// Subtracts two fixed-point values.
        /// </summary>
        [BurstCompile]
        public static long Subtract(long a, long b)
        {
            return a - b;
        }

        /// <summary>
        /// Multiplies two fixed-point values.
        /// </summary>
        [BurstCompile]
        public static long Multiply(long a, long b)
        {
            return (a * b) / Scale;
        }

        /// <summary>
        /// Divides two fixed-point values.
        /// </summary>
        [BurstCompile]
        public static long Divide(long a, long b)
        {
            if (b == 0) return 0;
            return (a * Scale) / b;
        }

        /// <summary>
        /// Multiplies fixed-point value by float.
        /// </summary>
        [BurstCompile]
        public static long MultiplyByFloat(long fixedValue, float multiplier)
        {
            return (long)math.round(fixedValue * multiplier);
        }

        /// <summary>
        /// Clamps fixed-point value between min and max.
        /// </summary>
        [BurstCompile]
        public static long Clamp(long value, long min, long max)
        {
            return math.clamp(value, min, max);
        }

        /// <summary>
        /// Compares two fixed-point values.
        /// Returns: -1 if a < b, 0 if a == b, 1 if a > b
        /// </summary>
        [BurstCompile]
        public static int Compare(long a, long b)
        {
            if (a < b) return -1;
            if (a > b) return 1;
            return 0;
        }
    }

    /// <summary>
    /// Fixed-point value component for economy/power systems.
    /// </summary>
    public struct FixedPointValue : IComponentData
    {
        public long Value;

        public FixedPointValue(float floatValue)
        {
            Value = FixedPointMath.ToFixed(floatValue);
        }

        public float AsFloat => FixedPointMath.ToFloat(Value);
    }
}

