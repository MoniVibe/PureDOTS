using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime
{
    /// <summary>
    /// Mass/velocity-derived impact values used by collision response systems.
    /// </summary>
    public struct CollisionImpactKinematics
    {
        public float RelativeSpeed;
        public float NormalSpeed;
        public float EffectiveMass;
        public float EstimatedImpulse;
        public float KineticEnergy;
    }

    /// <summary>
    /// Helper class for computing collision damage based on material properties.
    /// Centralizes damage calculation logic so it can be easily tuned or changed later.
    /// </summary>
    [BurstCompile]
    public static class CollisionDamage
    {
        /// <summary>
        /// Computes damage from a collision using material-aware calculation.
        /// </summary>
        /// <param name="impulseMagnitude">Collision impulse magnitude from physics event (N·s)</param>
        /// <param name="relativeSpeed">Relative velocity magnitude (m/s). Can be computed from relativeVelocity.</param>
        /// <param name="materialA">Material stats for the first entity</param>
        /// <param name="materialB">Material stats for the second entity</param>
        /// <param name="damagePerImpulse">Global damage multiplier (from ImpactDamage.DamagePerImpulse or PhysicsConfig)</param>
        /// <param name="useEnergyFormula">If true, uses energy-based formula (0.5 * m_eff * v²). If false, uses impulse-based formula.</param>
        /// <returns>Computed damage amount</returns>
        [BurstCompile]
        public static float ComputeDamage(
            float impulseMagnitude,
            float relativeSpeed,
            in MaterialStats materialA,
            in MaterialStats materialB,
            float damagePerImpulse,
            bool useEnergyFormula = false)
        {
            if (useEnergyFormula)
            {
                // Energy-based formula: damage ∝ 0.5 * m_eff * v²
                // Effective mass factor from density
                float effectiveMassFactor = 0.5f * (materialA.Density + materialB.Density);
                float energy = 0.5f * effectiveMassFactor * relativeSpeed * relativeSpeed;
                return energy * damagePerImpulse;
            }
            else
            {
                // Impulse-based formula: damage ∝ impulse * material_hardness
                // Material factor is average hardness
                float materialFactor = 0.5f * (materialA.Hardness + materialB.Hardness);
                return impulseMagnitude * materialFactor * damagePerImpulse;
            }
        }

        /// <summary>
        /// Simplified version that only uses impulse (no relative speed needed).
        /// Uses the impulse-based formula.
        /// </summary>
        [BurstCompile]
        public static float ComputeDamage(
            float impulseMagnitude,
            in MaterialStats materialA,
            in MaterialStats materialB,
            float damagePerImpulse)
        {
            float materialFactor = 0.5f * (materialA.Hardness + materialB.Hardness);
            return impulseMagnitude * materialFactor * damagePerImpulse;
        }

        /// <summary>
        /// Resolves scalar mass from inverse mass (physics representation) with authored fallback.
        /// </summary>
        [BurstCompile]
        public static float ResolveMass(float inverseMass, float fallbackMass = 1f)
        {
            if (inverseMass > 1e-5f)
            {
                return 1f / inverseMass;
            }

            return math.max(0.0001f, fallbackMass);
        }

        /// <summary>
        /// Computes reduced mass for two colliding bodies.
        /// </summary>
        [BurstCompile]
        public static float ComputeReducedMass(float sourceMass, float targetMass)
        {
            sourceMass = math.max(0.0001f, sourceMass);
            targetMass = math.max(0.0001f, targetMass);

            var sum = sourceMass + targetMass;
            if (sum <= 1e-5f)
            {
                return 0f;
            }

            return (sourceMass * targetMass) / sum;
        }

        /// <summary>
        /// Computes impact kinematics from mass, relative velocity, and contact normal.
        /// </summary>
        [BurstCompile]
        public static CollisionImpactKinematics ComputeImpactKinematics(
            float3 sourceVelocity,
            float sourceMass,
            float3 targetVelocity,
            float targetMass,
            float3 contactNormal)
        {
            var relativeVelocity = sourceVelocity - targetVelocity;
            var relativeSpeed = math.length(relativeVelocity);

            var normal = math.normalizesafe(
                contactNormal,
                math.normalizesafe(relativeVelocity, new float3(0f, 1f, 0f)));

            var normalSpeed = math.abs(math.dot(relativeVelocity, normal));
            var effectiveMass = ComputeReducedMass(sourceMass, targetMass);
            var estimatedImpulse = effectiveMass * normalSpeed;
            var kineticEnergy = 0.5f * effectiveMass * normalSpeed * normalSpeed;

            return new CollisionImpactKinematics
            {
                RelativeSpeed = relativeSpeed,
                NormalSpeed = normalSpeed,
                EffectiveMass = effectiveMass,
                EstimatedImpulse = estimatedImpulse,
                KineticEnergy = kineticEnergy
            };
        }

        /// <summary>
        /// Chooses the highest quality impulse estimate between physics-reported and mass/velocity-derived values.
        /// </summary>
        [BurstCompile]
        public static float ResolveEffectiveImpulse(float reportedImpulse, in CollisionImpactKinematics kinematics)
        {
            return math.max(math.max(0f, reportedImpulse), kinematics.EstimatedImpulse);
        }
    }
}
