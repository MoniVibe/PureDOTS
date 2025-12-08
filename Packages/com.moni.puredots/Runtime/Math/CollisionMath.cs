using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Math
{
    /// <summary>
    /// Collision data structure for Q-law computation.
    /// </summary>
    public struct CollisionData
    {
        /// <summary>
        /// Mass of projectile/impactor (kg).
        /// </summary>
        public float MassProjectile;

        /// <summary>
        /// Mass of target (kg).
        /// </summary>
        public float MassTarget;

        /// <summary>
        /// Relative velocity vector (m/s).
        /// </summary>
        public float3 Velocity;
    }

    /// <summary>
    /// Q-law energy scaling constants and computation.
    /// Q = 0.5 * m_projectile * v^2 / m_target (J/kg).
    /// </summary>
    [BurstCompile]
    public static class CollisionMath
    {
        /// <summary>
        /// Q threshold for elastic bounce (J/kg).
        /// Q < 10³ → Elastic bounce.
        /// </summary>
        public const float Q_THRESHOLD_ELASTIC = 1e3f;

        /// <summary>
        /// Q threshold for crater/partial damage (J/kg).
        /// 10³ ≤ Q < 10⁶ → Crater/partial damage.
        /// </summary>
        public const float Q_THRESHOLD_CRATER = 1e6f;

        /// <summary>
        /// Q threshold for catastrophic disruption (J/kg).
        /// Q ≥ 10⁶ → Catastrophic disruption.
        /// </summary>
        public const float Q_THRESHOLD_CATASTROPHIC = 1e6f;

        /// <summary>
        /// Regime thresholds in meters.
        /// </summary>
        public const float REGIME_MICRO_MAX = 100f;
        public const float REGIME_MESO_MIN = 100f;
        public const float REGIME_MESO_MAX = 10000f;
        public const float REGIME_MACRO_MIN = 10000f;

        /// <summary>
        /// Radius ratio threshold for regime culling.
        /// If r₁/r₂ > 1e5, skip fine physics (use analytic trajectory).
        /// </summary>
        public const float RADIUS_RATIO_CULL_THRESHOLD = 1e5f;

    /// <summary>
    /// Computes Q value (impact energy per unit target mass) in J/kg.
    /// Q = 0.5 * m_projectile * v^2 / m_target
    /// 
    /// Q thresholds determine collision outcome:
    /// - Q < 10³ J/kg → Elastic bounce
    /// - 10³ ≤ Q < 10⁶ J/kg → Crater/partial damage
    /// - Q ≥ 10⁶ J/kg → Catastrophic disruption
    /// 
        /// Example:
        /// var q = CollisionMath.ComputeQ(1000f, 10000f, new float3(100f, 0f, 0f));
        /// </summary>
        [BurstCompile]
        public static float ComputeQ(in CollisionData c)
        {
            if (c.MassTarget <= 0f)
                return float.MaxValue; // Infinite Q for zero-mass target

            var velocitySquared = math.lengthsq(c.Velocity);
            return 0.5f * c.MassProjectile * velocitySquared / c.MassTarget;
        }

        /// <summary>
        /// Computes Q value from individual parameters.
        /// </summary>
        [BurstCompile]
        public static float ComputeQ(float massProjectile, float massTarget, in float3 velocity)
        {
            var collisionData = new CollisionData
            {
                MassProjectile = massProjectile,
                MassTarget = massTarget,
                Velocity = velocity
            };
            return ComputeQ(in collisionData);
        }

        /// <summary>
        /// Determines collision outcome based on Q value.
        /// </summary>
        [BurstCompile]
        public static CollisionOutcome GetCollisionOutcome(float q)
        {
            if (q < Q_THRESHOLD_ELASTIC)
                return CollisionOutcome.ElasticBounce;
            
            if (q < Q_THRESHOLD_CATASTROPHIC)
                return CollisionOutcome.CraterPartialDamage;
            
            return CollisionOutcome.CatastrophicDisruption;
        }

        /// <summary>
        /// Computes post-collision velocities using 6-DoF momentum conservation.
        /// vA' = vA - (2*mB/(mA+mB))*dot(vA-vB, n)*n
        /// vB' = vB + (2*mA/(mA+mB))*dot(vA-vB, n)*n
        /// 
        /// Used by MicroCollisionSystem for deterministic small-object collisions.
        /// 
        /// Example:
        /// CollisionMath.ComputeMomentumConservation(
        ///     velocityA, velocityB, massA, massB, collisionNormal,
        ///     out float3 vAOut, out float3 vBOut);
        /// </summary>
        [BurstCompile]
        public static void ComputeMomentumConservation(
            in float3 vA, in float3 vB,
            float mA, float mB,
            in float3 normal,
            ref float3 vAOut, ref float3 vBOut)
        {
            var totalMass = mA + mB;
            if (totalMass <= 0f)
            {
                vAOut = vA;
                vBOut = vB;
                return;
            }

            var relativeVelocity = vA - vB;
            var dotProduct = math.dot(relativeVelocity, normal);
            var impulseScalar = 2f * dotProduct / totalMass;

            vAOut = vA - (mB * impulseScalar) * normal;
            vBOut = vB + (mA * impulseScalar) * normal;
        }

        /// <summary>
        /// Clamps velocity to escape velocity or terminal velocity limits.
        /// Prevents numeric blow-ups in extreme collisions.
        /// </summary>
        [BurstCompile]
        public static void ClampVelocity(ref float3 velocity, float escapeVelocity, float terminalVelocity)
        {
            var speed = math.length(velocity);
            var maxSpeed = math.max(escapeVelocity, terminalVelocity);
            
            if (speed > maxSpeed)
            {
                velocity = math.normalize(velocity) * maxSpeed;
            }
        }

        /// <summary>
        /// Computes crater radius from Q value.
        /// craterRadius = k * pow(Q, 0.28f)
        /// </summary>
        [BurstCompile]
        public static float ComputeCraterRadius(float q, float k = 0.1f)
        {
            return k * math.pow(q, 0.28f);
        }

        /// <summary>
        /// Computes ejecta mass from Q value.
        /// ejectaMass = c * pow(Q, 0.75f)
        /// </summary>
        [BurstCompile]
        public static float ComputeEjectaMass(float q, float c = 0.01f)
        {
            return c * math.pow(q, 0.75f);
        }
    }

    /// <summary>
    /// Collision outcome enum based on Q value.
    /// </summary>
    public enum CollisionOutcome : byte
    {
        /// <summary>
        /// Elastic bounce (Q < 10³ J/kg).
        /// </summary>
        ElasticBounce = 0,

        /// <summary>
        /// Crater/partial damage (10³ ≤ Q < 10⁶ J/kg).
        /// </summary>
        CraterPartialDamage = 1,

        /// <summary>
        /// Catastrophic disruption (Q ≥ 10⁶ J/kg).
        /// </summary>
        CatastrophicDisruption = 2
    }
}

