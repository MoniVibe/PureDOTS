using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Physical properties of a planet used to compute baseline climate coefficients.
    /// Computed once at world creation and cached in a blob asset.
    /// </summary>
    public struct PlanetPhysicalProfileBlob
    {
        // Planetary constants
        public float Mass;              // kg
        public float Radius;            // meters
        public float DistanceToStar;    // meters (orbital distance)
        public float StarLuminosity;    // W (stellar output)
        public float RotationRate;      // rad/s (rotation speed)
        public float AxialTilt;         // radians (obliquity)
        public float CompositionOxygen; // 0-1 (atmospheric oxygen fraction)
        public float CompositionCO2;     // 0-1 (atmospheric CO2 fraction)
        public float CompositionNitrogen; // 0-1 (atmospheric nitrogen fraction)

        // Derived coefficients (computed once, cached)
        public float Gravity;           // m/s² (g = GM/R²)
        public float Irradiance;        // W/m² (I = L / (4πr²))
        public float AtmosphereDensity; // kg/m³ (derived from mass/composition)
        public float CoriolisStrength;   // Scaling factor for wind patterns

        /// <summary>
        /// Computes derived coefficients from planetary constants.
        /// </summary>
        public static PlanetPhysicalProfileBlob Compute(float mass, float radius, float distanceToStar,
            float starLuminosity, float rotationRate, float axialTilt,
            float compositionOxygen, float compositionCO2, float compositionNitrogen)
        {
            const float G = 6.67430e-11f; // Gravitational constant

            var gravity = (G * mass) / (radius * radius);
            var irradiance = starLuminosity / (4f * math.PI * distanceToStar * distanceToStar);
            
            // Simplified atmosphere density (proportional to gravity and composition)
            var atmosphereDensity = gravity * 1.2f * (compositionOxygen + compositionCO2 + compositionNitrogen);
            
            // Coriolis strength (proportional to rotation rate)
            var coriolisStrength = rotationRate * 0.1f;

            return new PlanetPhysicalProfileBlob
            {
                Mass = mass,
                Radius = radius,
                DistanceToStar = distanceToStar,
                StarLuminosity = starLuminosity,
                RotationRate = rotationRate,
                AxialTilt = axialTilt,
                CompositionOxygen = compositionOxygen,
                CompositionCO2 = compositionCO2,
                CompositionNitrogen = compositionNitrogen,
                Gravity = gravity,
                Irradiance = irradiance,
                AtmosphereDensity = atmosphereDensity,
                CoriolisStrength = coriolisStrength
            };
        }
    }

    /// <summary>
    /// Singleton component providing access to planet physical profile.
    /// </summary>
    public struct PlanetPhysicalProfile : IComponentData
    {
        public BlobAssetReference<PlanetPhysicalProfileBlob> Blob;

        public readonly bool IsCreated => Blob.IsCreated;

        public readonly float GetGravityMultiplier()
        {
            if (!IsCreated)
            {
                return 1f; // Default Earth-like
            }
            ref var profile = ref Blob.Value;
            return profile.Gravity / 9.81f; // Normalized to Earth gravity
        }

        public readonly float GetIrradianceMultiplier()
        {
            if (!IsCreated)
            {
                return 1f;
            }
            ref var profile = ref Blob.Value;
            // Normalize to Earth's solar constant (~1361 W/m²)
            return profile.Irradiance / 1361f;
        }
    }
}

