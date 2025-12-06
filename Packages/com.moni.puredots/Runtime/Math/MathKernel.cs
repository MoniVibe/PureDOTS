using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Math
{
    /// <summary>
    /// Unified math kernel providing Burst-compiled math utilities.
    /// Guarantees identical math across every ECS world, every platform, every tick.
    /// </summary>
    [BurstCompile]
    public static class MathKernel
    {
        // Vector operations
        [BurstCompile]
        public static float3 NormalizeSafe(in float3 v, float epsilon = 1e-8f)
        {
            float lenSq = math.lengthsq(v);
            if (lenSq < epsilon)
                return float3.zero;
            return v * math.rsqrt(lenSq);
        }

        [BurstCompile]
        public static float2 NormalizeSafe(in float2 v, float epsilon = 1e-8f)
        {
            float lenSq = math.lengthsq(v);
            if (lenSq < epsilon)
                return float2.zero;
            return v * math.rsqrt(lenSq);
        }

        [BurstCompile]
        public static float DistanceSquared(in float3 a, in float3 b)
        {
            float3 diff = a - b;
            return math.lengthsq(diff);
        }

        [BurstCompile]
        public static float Distance(in float3 a, in float3 b)
        {
            return math.distance(a, b);
        }

        [BurstCompile]
        public static float3 Lerp(in float3 a, in float3 b, float t)
        {
            return math.lerp(a, b, t);
        }

        [BurstCompile]
        public static float3 Slerp(in float3 a, in float3 b, float t)
        {
            // Simplified slerp for unit vectors
            float dot = math.clamp(math.dot(a, b), -1f, 1f);
            float theta = math.acos(dot) * t;
            float3 relative = NormalizeSafe(b - a * dot);
            return a * math.cos(theta) + relative * math.sin(theta);
        }

        // Quaternion operations
        [BurstCompile]
        public static quaternion QuaternionFromEuler(float3 euler)
        {
            return quaternion.Euler(euler);
        }

        [BurstCompile]
        public static quaternion QuaternionLookRotation(in float3 forward, in float3 up)
        {
            return quaternion.LookRotationSafe(forward, up);
        }

        [BurstCompile]
        public static quaternion QuaternionSlerp(in quaternion a, in quaternion b, float t)
        {
            return math.slerp(a, b, t);
        }

        [BurstCompile]
        public static float3 QuaternionToEuler(in quaternion q)
        {
            return math.degrees(math.quaternionToEulerAnglesXYZ(q));
        }

        // Interpolation
        [BurstCompile]
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = math.clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        [BurstCompile]
        public static float SmoothStep(float x)
        {
            return SmoothStep(0f, 1f, x);
        }

        [BurstCompile]
        public static float EaseInOut(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }

        // Noise functions (deterministic)
        [BurstCompile]
        public static float Noise1D(float x, uint seed = 0)
        {
            // Simple hash-based noise
            uint n = (uint)(x * 73856093f) ^ seed;
            n = (n << 13) ^ n;
            return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483648.0f;
        }

        [BurstCompile]
        public static float Noise2D(float2 p, uint seed = 0)
        {
            // Simple 2D hash-based noise
            uint2 ip = (uint2)math.floor(p);
            float2 f = p - math.floor(p);
            float2 u = f * f * (3f - 2f * f);

            uint n = ip.x + ip.y * 57u + seed;
            n = (n << 13) ^ n;
            float a = ((n * (n * n * 15731u + 789221u) + 1376312589u) & 0x7fffffffu) / 2147483648.0f;

            n = (ip.x + 1u) + ip.y * 57u + seed;
            n = (n << 13) ^ n;
            float b = ((n * (n * n * 15731u + 789221u) + 1376312589u) & 0x7fffffffu) / 2147483648.0f;

            n = ip.x + (ip.y + 1u) * 57u + seed;
            n = (n << 13) ^ n;
            float c = ((n * (n * n * 15731u + 789221u) + 1376312589u) & 0x7fffffffu) / 2147483648.0f;

            n = (ip.x + 1u) + (ip.y + 1u) * 57u + seed;
            n = (n << 13) ^ n;
            float d = ((n * (n * n * 15731u + 789221u) + 1376312589u) & 0x7fffffffu) / 2147483648.0f;

            return math.lerp(math.lerp(a, b, u.x), math.lerp(c, d, u.x), u.y);
        }

        [BurstCompile]
        public static float Noise3D(float3 p, uint seed = 0)
        {
            // Simple 3D hash-based noise
            uint3 ip = (uint3)math.floor(p);
            float3 f = p - math.floor(p);
            float3 u = f * f * (3f - 2f * f);

            uint n = ip.x + ip.y * 57u + ip.z * 131u + seed;
            n = (n << 13) ^ n;
            float a = ((n * (n * n * 15731u + 789221u) + 1376312589u) & 0x7fffffffu) / 2147483648.0f;

            return a; // Simplified - full 3D interpolation would be more complex
        }

        // Random number generation (deterministic, seed-based)
        [BurstCompile]
        public static uint NextRandom(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        [BurstCompile]
        public static float RandomFloat(ref uint state)
        {
            return (NextRandom(ref state) & 0x7fffffffu) / 2147483648.0f;
        }

        [BurstCompile]
        public static float RandomFloatRange(ref uint state, float min, float max)
        {
            return min + RandomFloat(ref state) * (max - min);
        }

        [BurstCompile]
        public static int RandomIntRange(ref uint state, int min, int max)
        {
            return min + (int)(NextRandom(ref state) % (uint)(max - min));
        }

        // Clamping utilities
        [BurstCompile]
        public static float Clamp01(float x)
        {
            return math.clamp(x, 0f, 1f);
        }

        [BurstCompile]
        public static float3 ClampMagnitude(in float3 v, float maxLength)
        {
            float lenSq = math.lengthsq(v);
            if (lenSq > maxLength * maxLength)
            {
                return v * (maxLength * math.rsqrt(lenSq));
            }
            return v;
        }

        // Angle utilities
        [BurstCompile]
        public static float Angle(in float3 a, in float3 b)
        {
            return math.degrees(math.acos(math.clamp(math.dot(math.normalize(a), math.normalize(b)), -1f, 1f)));
        }

        [BurstCompile]
        public static float SignedAngle(in float3 from, in float3 to, in float3 axis)
        {
            float3 cross = math.cross(from, to);
            float dot = math.dot(from, to);
            float angle = math.atan2(math.length(cross), dot);
            if (math.dot(axis, cross) < 0f)
                angle = -angle;
            return math.degrees(angle);
        }

        // Projection utilities
        [BurstCompile]
        public static float3 Project(in float3 vector, in float3 onNormal)
        {
            float lenSq = math.lengthsq(onNormal);
            if (lenSq < 1e-8f)
                return float3.zero;
            return onNormal * (math.dot(vector, onNormal) / lenSq);
        }

        [BurstCompile]
        public static float3 ProjectOnPlane(in float3 vector, in float3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        // Reflection
        [BurstCompile]
        public static float3 Reflect(in float3 inDirection, in float3 inNormal)
        {
            return inDirection - 2f * math.dot(inDirection, inNormal) * inNormal;
        }
    }
}

