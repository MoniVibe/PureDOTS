using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Math
{
    /// <summary>
    /// Perceptual calibration functions for collision visual/audio effects.
    /// Human perception of brightness and loudness is logarithmic.
    /// These curves make tiny crashes feel light and planetary ones overwhelming.
    /// </summary>
    [BurstCompile]
    public static class PerceptualScaling
    {
        /// <summary>
        /// Computes visual energy scaling from Q value.
        /// visualEnergy = pow(Q, 0.4f)
        /// </summary>
        [BurstCompile]
        public static float ComputeVisualEnergy(float q)
        {
            return math.pow(q, 0.4f);
        }

        /// <summary>
        /// Computes color shift scaling from Q value.
        /// colorShift = pow(Q, 0.2f)
        /// </summary>
        [BurstCompile]
        public static float ComputeColorShift(float q)
        {
            return math.pow(q, 0.2f);
        }

        /// <summary>
        /// Computes sound gain scaling from Q value.
        /// soundGain = pow(Q, 0.3f)
        /// </summary>
        [BurstCompile]
        public static float ComputeSoundGain(float q)
        {
            return math.pow(q, 0.3f);
        }

        /// <summary>
        /// Computes emissive brightness from melt percentage.
        /// Used for macro regime visual effects.
        /// </summary>
        [BurstCompile]
        public static float ComputeEmissiveBrightness(float meltPercentage)
        {
            // Brightness scales with melt percentage
            return math.pow(meltPercentage, 0.5f);
        }

        /// <summary>
        /// Computes color shift from atmosphere loss percentage.
        /// Used for macro regime visual effects.
        /// </summary>
        [BurstCompile]
        public static void ComputeAtmosphereColorShift(float atmosphereLossPercentage, out float3 result)
        {
            // Shift toward red/orange as atmosphere is lost
            var shift = atmosphereLossPercentage;
            result = new float3(shift * 0.5f, -shift * 0.3f, -shift * 0.2f);
        }

        /// <summary>
        /// Computes dust particle spawn rate from dust mass.
        /// Used for macro regime visual effects.
        /// </summary>
        [BurstCompile]
        public static float ComputeDustSpawnRate(float dustMass)
        {
            // Spawn rate scales logarithmically with dust mass
            return math.log10(math.max(1f, dustMass / 1000f));
        }
    }
}

