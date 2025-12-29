using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace PureDOTS.Runtime.Physics
{
    public struct KinematicSweepResult
    {
        public float3 ResolvedDelta;
        public float3 HitPosition;
        public float3 HitNormal;
        public Entity HitEntity;
        public float HitFraction;
        public byte HasHit;
    }

    [BurstCompile]
    public static class KinematicSweepUtility
    {
        [BurstCompile]
        public static unsafe bool TryResolveSweep(
            in PhysicsWorldSingleton physicsWorld,
            in PhysicsCollider collider,
            Entity self,
            float3 startPosition,
            quaternion rotation,
            float3 desiredDelta,
            float skinDistance,
            bool allowSlide,
            bool ignoreTriggerHits,
            out KinematicSweepResult result)
        {
            result = default;
            result.ResolvedDelta = desiredDelta;

            var deltaSq = math.lengthsq(desiredDelta);
            if (deltaSq < 1e-8f)
            {
                return false;
            }

            var input = new ColliderCastInput
            {
                Collider = (Unity.Physics.Collider*)collider.Value.GetUnsafePtr(),
                Start = startPosition,
                End = startPosition + desiredDelta,
                Orientation = rotation
            };

            if (!physicsWorld.CastCollider(input, out var hit))
            {
                return false;
            }

            if (hit.Entity == self)
            {
                return false;
            }

            if (ignoreTriggerHits)
            {
                var response = hit.Material.CollisionResponse;
                if (response == CollisionResponsePolicy.RaiseTriggerEvents ||
                    response == CollisionResponsePolicy.None)
                {
                    return false;
                }
            }

            var travelDistance = math.sqrt(deltaSq);
            var travelDir = math.normalizesafe(desiredDelta);
            var hitDistance = travelDistance * hit.Fraction;
            var clampedDistance = math.max(0f, hitDistance - math.max(0f, skinDistance));
            var clampedDelta = travelDir * clampedDistance;
            var resolvedDelta = clampedDelta;

            if (allowSlide)
            {
                var remaining = desiredDelta - clampedDelta;
                var normal = hit.SurfaceNormal;
                var slide = remaining - math.dot(remaining, normal) * normal;
                resolvedDelta += slide;
            }

            result.ResolvedDelta = resolvedDelta;
            result.HitEntity = hit.Entity;
            result.HitNormal = hit.SurfaceNormal;
            result.HitFraction = hit.Fraction;
            result.HitPosition = startPosition + travelDir * hitDistance;
            result.HasHit = 1;

            return true;
        }
    }
}
