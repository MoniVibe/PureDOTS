using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime
{
    public struct ImpactKinematics
    {
        public float3 RelativeVelocity;
        public float RelativeSpeed;
        public float ClosingSpeed;
        public float ReducedMass;
        public float EstimatedImpulse;
    }

    /// <summary>
    /// Helper class for computing collision damage based on material properties.
    /// Centralizes damage calculation logic so it can be easily tuned or changed later.
    /// </summary>
    [BurstCompile]
    public static class CollisionDamage
    {
        [BurstCompile]
        public static void ComputeImpactKinematics(
            in float3 sourceVelocity,
            float sourceMass,
            in float3 targetVelocity,
            float targetMass,
            in float3 contactNormal,
            out ImpactKinematics kinematics)
        {
            var relativeVelocity = sourceVelocity - targetVelocity;
            var relativeSpeed = math.length(relativeVelocity);

            var normalLengthSq = math.lengthsq(contactNormal);
            var normal = normalLengthSq > 0.000001f ? contactNormal / math.sqrt(normalLengthSq) : new float3(0f, 1f, 0f);
            var closingSpeed = math.abs(math.dot(relativeVelocity, normal));

            var safeSourceMass = math.max(0.0001f, sourceMass);
            var safeTargetMass = math.max(0.0001f, targetMass);
            var reducedMass = (safeSourceMass * safeTargetMass) / (safeSourceMass + safeTargetMass);

            kinematics = new ImpactKinematics
            {
                RelativeVelocity = relativeVelocity,
                RelativeSpeed = relativeSpeed,
                ClosingSpeed = closingSpeed,
                ReducedMass = reducedMass,
                EstimatedImpulse = reducedMass * closingSpeed
            };
        }

        [BurstCompile]
        public static float ResolveEffectiveImpulse(float reportedImpulse, in ImpactKinematics kinematics)
        {
            return math.max(reportedImpulse, kinematics.EstimatedImpulse);
        }

        [BurstCompile]
        public static float ResolveMass(float inverseMass, float fallbackMass)
        {
            if (inverseMass > 0.000001f)
            {
                return 1f / inverseMass;
            }

            return math.max(0.0001f, fallbackMass);
        }

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
    }
}
