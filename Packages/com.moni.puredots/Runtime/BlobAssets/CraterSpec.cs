using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Crater specification blob asset for pre-generated crater parameters.
    /// Contains radius-to-energy curves and debris specifications.
    /// </summary>
    public struct CraterSpec
    {
        /// <summary>
        /// Crater radius coefficient k in radius = k * pow(Q, 0.28).
        /// </summary>
        public float RadiusCoefficient;

        /// <summary>
        /// Ejecta mass coefficient c in ejectaMass = c * pow(Q, 0.75).
        /// </summary>
        public float EjectaMassCoefficient;

        /// <summary>
        /// Minimum crater radius (m).
        /// </summary>
        public float MinRadius;

        /// <summary>
        /// Maximum crater radius (m).
        /// </summary>
        public float MaxRadius;

        /// <summary>
        /// Debris specification for particle spawning.
        /// </summary>
        public BlobArray<DebrisSpec> DebrisSpecs;
    }

    /// <summary>
    /// Debris specification for particle spawning from crater impacts.
    /// </summary>
    public struct DebrisSpec
    {
        /// <summary>
        /// Debris size range (min, max) in meters.
        /// </summary>
        public float2 SizeRange;

        /// <summary>
        /// Debris lifetime range (min, max) in seconds.
        /// </summary>
        public float2 LifetimeRange;

        /// <summary>
        /// Debris heat/temperature range (min, max) in Kelvin.
        /// </summary>
        public float2 HeatRange;

        /// <summary>
        /// Velocity multiplier for debris ejection.
        /// </summary>
        public float VelocityMultiplier;

        /// <summary>
        /// Mass fraction of total ejecta mass for this debris type.
        /// </summary>
        public float MassFraction;
    }

    /// <summary>
    /// Helper methods for crater specification.
    /// </summary>
    public static class CraterSpecHelpers
    {
        /// <summary>
        /// Computes crater radius from Q value using crater spec.
        /// </summary>
        public static float ComputeCraterRadius(ref CraterSpec spec, float q)
        {
            var radius = spec.RadiusCoefficient * math.pow(q, 0.28f);
            return math.clamp(radius, spec.MinRadius, spec.MaxRadius);
        }

        /// <summary>
        /// Computes ejecta mass from Q value using crater spec.
        /// </summary>
        public static float ComputeEjectaMass(ref CraterSpec spec, float q)
        {
            return spec.EjectaMassCoefficient * math.pow(q, 0.75f);
        }
    }
}

