using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Ammo specification data - modifies projectile behavior and hit effects.
    /// </summary>
    public struct AmmoSpec
    {
        public FixedString32Bytes Id;
        public float DamageMultiplier; // Multiplies base damage
        public float SpeedMultiplier; // Multiplies projectile speed
        public float LifetimeMultiplier; // Multiplies projectile lifetime
        public float TurnRateMultiplier; // Multiplies homing turn rate
        public float SeekRadiusMultiplier; // Multiplies homing seek radius
        public float AoERadiusMultiplier; // Multiplies AoE radius
        public float ChainRangeMultiplier; // Multiplies chain range
        public float PierceBonus; // Adds to projectile pierce count
        public float KnockbackMultiplier; // Multiplies knockback effects
        public byte DamageTypeOverride; // 255 = use projectile default
        public DamageFlags DamageFlags; // Extra flags to apply on damage
        public BlobArray<EffectOp> OnHitAdd; // Additional effects on hit
    }

    /// <summary>
    /// Blob catalog for ammo specifications.
    /// </summary>
    public struct AmmoCatalogBlob
    {
        public BlobArray<AmmoSpec> Ammunition;
    }

    /// <summary>
    /// Singleton component holding ammo catalog reference.
    /// </summary>
    public struct AmmoCatalog : IComponentData
    {
        public BlobAssetReference<AmmoCatalogBlob> Catalog;
    }

    public static class AmmoSpecSanitizer
    {
        public static void Sanitize(ref AmmoSpec spec)
        {
            if (spec.DamageMultiplier <= 0f)
            {
                spec.DamageMultiplier = 1f;
            }

            if (spec.SpeedMultiplier <= 0f)
            {
                spec.SpeedMultiplier = 1f;
            }

            if (spec.LifetimeMultiplier <= 0f)
            {
                spec.LifetimeMultiplier = 1f;
            }

            if (spec.TurnRateMultiplier <= 0f)
            {
                spec.TurnRateMultiplier = 1f;
            }

            if (spec.SeekRadiusMultiplier <= 0f)
            {
                spec.SeekRadiusMultiplier = 1f;
            }

            if (spec.AoERadiusMultiplier <= 0f)
            {
                spec.AoERadiusMultiplier = 1f;
            }

            if (spec.ChainRangeMultiplier <= 0f)
            {
                spec.ChainRangeMultiplier = 1f;
            }

            if (spec.KnockbackMultiplier <= 0f)
            {
                spec.KnockbackMultiplier = 1f;
            }
        }
    }
}
