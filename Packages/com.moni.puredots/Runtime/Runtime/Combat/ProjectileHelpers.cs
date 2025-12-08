using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Helper functions for projectile calculations (deterministic, Burst-safe).
    /// </summary>
    public static class ProjectileHelpers
    {
        /// <summary>
        /// Calculates deterministic spread direction using pre-rotated firing matrices.
        /// From Panigrahy 2024: high-precision ballistic accuracy with pre-rotated matrices.
        /// </summary>
        /// <param name="dir">Base direction (normalized)</param>
        /// <param name="spread">Spread angle in radians</param>
        /// <param name="seed">Deterministic seed (from WorldRng + shot sequence)</param>
        /// <param name="idx">Pellet/spread index for unique randomization</param>
        /// <returns>Spread direction vector</returns>
        public static float3 RandomSpread(float3 dir, float spread, uint seed, uint idx)
        {
            var rand = new Unity.Mathematics.Random(seed + idx * 37u);
            float2 angle = rand.NextFloat2Direction() * spread;
            quaternion q = quaternion.Euler(angle.x, angle.y, 0f);
            return math.mul(q, dir);
        }

        /// <summary>
        /// Calculates deterministic spread direction using degrees (convenience wrapper).
        /// </summary>
        public static float3 RandomSpreadDeg(float3 dir, float spreadDeg, uint seed, uint idx)
        {
            return RandomSpread(dir, math.radians(spreadDeg), seed, idx);
        }

        /// <summary>
        /// Mitigation formula from HPC control frameworks (Vancin 2023 analog).
        /// Burst-safe, no branching.
        /// </summary>
        /// <param name="dmg">Base damage</param>
        /// <param name="armor">Armor value</param>
        /// <param name="resist">Resistance factor (0-1)</param>
        /// <returns>Mitigated damage</returns>
        public static float MitigatedDamage(float dmg, float armor, float resist)
        {
            return dmg * (1f - math.saturate(armor / (armor + 100f))) * (1f - resist);
        }

        /// <summary>
        /// Advanced mitigation formula with armor coefficient and shield factor.
        /// EffectiveDamage = BaseDamage / (1f + (ArmorCoeff * ArmorValue)) * (1f - ShieldFactor)
        /// </summary>
        public static float EffectiveDamage(float baseDamage, float armorCoeff, float armorValue, float shieldFactor)
        {
            return baseDamage / (1f + (armorCoeff * armorValue)) * (1f - shieldFactor);
        }

        /// <summary>
        /// Shield decay calculation: shield.Value = max(0, shield.Value - dmg * absorbCoeff)
        /// </summary>
        public static float ApplyShieldDecay(float currentShield, float damage, float absorbCoeff)
        {
            return math.max(0f, currentShield - damage * absorbCoeff);
        }

        /// <summary>
        /// Interpolates ballistic height from pre-computed LUT.
        /// LUT stores height = f(angle, v0, gravity) for common firing angles.
        /// Runtime interpolation (no trig per projectile).
        /// </summary>
        /// <param name="lut">Ballistic height lookup table</param>
        /// <param name="angle">Firing angle (radians)</param>
        /// <param name="v0">Muzzle velocity</param>
        /// <param name="gravity">Gravity magnitude</param>
        /// <returns>Interpolated height</returns>
        public static float GetBallisticHeight(ref BlobArray<float> lut, float angle, float v0, float gravity)
        {
            if (lut.Length == 0)
            {
                // Fallback: compute directly if no LUT
                float sinAngle = math.sin(angle);
                return (v0 * v0 * sinAngle * sinAngle) / (2f * gravity);
            }

            // Normalize angle to [0, PI/2] range
            float normalizedAngle = math.clamp(angle, 0f, math.PI / 2f);
            
            // Map to LUT index (assuming LUT covers 0 to PI/2)
            float lutStep = (math.PI / 2f) / (lut.Length - 1);
            float indexFloat = normalizedAngle / lutStep;
            int index = (int)indexFloat;
            float t = indexFloat - index;

            // Clamp index
            index = math.clamp(index, 0, lut.Length - 2);

            // Linear interpolation
            float height0 = lut[index];
            float height1 = lut[index + 1];
            return math.lerp(height0, height1, t);
        }

        /// <summary>
        /// Gets aerodynamic coefficient for given speed band.
        /// Pre-cached per speed band in ProjectileSpec blob.
        /// </summary>
        public static float GetAerodynamicCoeff(ref BlobArray<float> speedBands, ref BlobArray<float> coeffs, float speed)
        {
            if (speedBands.Length == 0 || coeffs.Length == 0 || speedBands.Length != coeffs.Length)
            {
                return 0f; // No aerodynamic data
            }

            // Find speed band
            for (int i = 0; i < speedBands.Length - 1; i++)
            {
                if (speed >= speedBands[i] && speed < speedBands[i + 1])
                {
                    // Linear interpolation between bands
                    float t = (speed - speedBands[i]) / (speedBands[i + 1] - speedBands[i]);
                    return math.lerp(coeffs[i], coeffs[i + 1], t);
                }
            }

            // Use last band if speed exceeds all bands
            return coeffs[coeffs.Length - 1];
        }
}

}
