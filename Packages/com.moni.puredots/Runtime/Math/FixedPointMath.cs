using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Math
{
    /// <summary>
    /// Optional fixed-point math utilities for cross-platform determinism.
    /// Use when floating-point precision differences across platforms are unacceptable.
    /// </summary>
    [BurstCompile]
    public static class FixedPointMath
    {
        // Fixed-point representation: 16.16 format (16 bits integer, 16 bits fractional)
        public const int FixedPointShift = 16;
        public const int FixedPointOne = 1 << FixedPointShift;
        public const int FixedPointHalf = FixedPointOne >> 1;

        [BurstCompile]
        public static int FloatToFixed(float value)
        {
            return (int)(value * FixedPointOne);
        }

        [BurstCompile]
        public static float FixedToFloat(int fixedValue)
        {
            return fixedValue / (float)FixedPointOne;
        }

        [BurstCompile]
        public static int FixedAdd(int a, int b)
        {
            return a + b;
        }

        [BurstCompile]
        public static int FixedSubtract(int a, int b)
        {
            return a - b;
        }

        [BurstCompile]
        public static int FixedMultiply(int a, int b)
        {
            return (int)(((long)a * b) >> FixedPointShift);
        }

        [BurstCompile]
        public static int FixedDivide(int a, int b)
        {
            return (int)(((long)a << FixedPointShift) / b);
        }

        [BurstCompile]
        public static int FixedLerp(int a, int b, int t)
        {
            // t is in fixed-point format (0 to FixedPointOne)
            int diff = b - a;
            return a + FixedMultiply(diff, t);
        }

        [BurstCompile]
        public static int FixedClamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        [BurstCompile]
        public static int FixedAbs(int value)
        {
            return value < 0 ? -value : value;
        }

        [BurstCompile]
        public static int FixedSqrt(int value)
        {
            // Simple integer square root approximation
            if (value <= 0) return 0;
            int result = 0;
            int bit = 1 << 30;
            while (bit > value)
                bit >>= 2;

            while (bit != 0)
            {
                if (value >= result + bit)
                {
                    value -= result + bit;
                    result = (result >> 1) + bit;
                }
                else
                {
                    result >>= 1;
                }
                bit >>= 2;
            }
            return result;
        }
    }
}

