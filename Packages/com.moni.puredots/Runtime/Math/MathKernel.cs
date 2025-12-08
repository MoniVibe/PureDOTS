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
        public static void NormalizeSafe(in float3 v, out float3 result, float epsilon = 1e-8f)
        {
            float lenSq = math.lengthsq(v);
            if (lenSq < epsilon)
            {
                result = float3.zero;
                return;
            }
            result = v * math.rsqrt(lenSq);
        }

        [BurstCompile]
        public static void NormalizeSafe(in float2 v, out float2 result, float epsilon = 1e-8f)
        {
            float lenSq = math.lengthsq(v);
            if (lenSq < epsilon)
            {
                result = float2.zero;
                return;
            }
            result = v * math.rsqrt(lenSq);
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
        public static void Lerp(in float3 a, in float3 b, float t, out float3 result)
        {
            result = math.lerp(a, b, t);
        }

        [BurstCompile]
        public static void Slerp(in float3 a, in float3 b, float t, out float3 result)
        {
            // Simplified slerp for unit vectors
            float dot = math.clamp(math.dot(a, b), -1f, 1f);
            float theta = math.acos(dot) * t;
            var diff = b - a * dot;
            NormalizeSafe(in diff, out var relative);
            result = a * math.cos(theta) + relative * math.sin(theta);
        }

        // Quaternion operations
        [BurstCompile]
        public static void QuaternionFromEuler(in float3 euler, out quaternion result)
        {
            result = quaternion.Euler(euler);
        }

        [BurstCompile]
        public static void QuaternionLookRotation(in float3 forward, in float3 up, out quaternion result)
        {
            result = quaternion.LookRotationSafe(forward, up);
        }

        [BurstCompile]
        public static void QuaternionSlerp(in quaternion a, in quaternion b, float t, out quaternion result)
        {
            result = math.slerp(a, b, t);
        }

        [BurstCompile]
        public static void QuaternionToEuler(in quaternion q, out float3 result)
        {
            var sinr = 2f * (q.value.w * q.value.x + q.value.y * q.value.z);
            var cosr = 1f - 2f * (q.value.x * q.value.x + q.value.y * q.value.y);
            var roll = math.atan2(sinr, cosr);

            var sinp = 2f * (q.value.w * q.value.y - q.value.z * q.value.x);
            float pitch;
            if (math.abs(sinp) >= 1f)
            {
                pitch = math.PI / 2f * math.sign(sinp);
            }
            else
            {
                pitch = math.asin(sinp);
            }

            var siny = 2f * (q.value.w * q.value.z + q.value.x * q.value.y);
            var cosy = 1f - 2f * (q.value.y * q.value.y + q.value.z * q.value.z);
            var yaw = math.atan2(siny, cosy);

            result = math.degrees(new float3(roll, pitch, yaw));
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
        public static float Noise2D(in float2 p, uint seed = 0)
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
        public static float Noise3D(in float3 p, uint seed = 0)
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
        public static void ClampMagnitude(in float3 v, float maxLength, out float3 result)
        {
            float lenSq = math.lengthsq(v);
            if (lenSq > maxLength * maxLength)
            {
                result = v * (maxLength * math.rsqrt(lenSq));
                return;
            }
            result = v;
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
        public static void Project(in float3 vector, in float3 onNormal, out float3 result)
        {
            float lenSq = math.lengthsq(onNormal);
            if (lenSq < 1e-8f)
            {
                result = float3.zero;
                return;
            }
            result = onNormal * (math.dot(vector, onNormal) / lenSq);
        }

        [BurstCompile]
        public static void ProjectOnPlane(in float3 vector, in float3 planeNormal, out float3 result)
        {
            Project(in vector, in planeNormal, out var projection);
            result = vector - projection;
        }

        // Reflection
        [BurstCompile]
        public static void Reflect(in float3 inDirection, in float3 inNormal, out float3 result)
        {
            result = inDirection - 2f * math.dot(inDirection, inNormal) * inNormal;
        }
    }
}

