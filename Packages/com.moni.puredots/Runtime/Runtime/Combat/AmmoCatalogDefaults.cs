using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Minimal default ammo catalog for PureDOTS-only headless runs.
    /// Provides standard, kinetic, explosive, and EMP ammo profiles.
    /// </summary>
    public static class AmmoCatalogDefaults
    {
        public static BlobAssetReference<AmmoCatalogBlob> CreateDefaultCatalog()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AmmoCatalogBlob>();
            var array = builder.Allocate(ref root.Ammunition, 5);

            BuildSpec(
                builder,
                ref array[0],
                "ammo.standard",
                damageMultiplier: 1f,
                speedMultiplier: 1f,
                lifetimeMultiplier: 1f,
                turnRateMultiplier: 1f,
                seekRadiusMultiplier: 1f,
                aoeRadiusMultiplier: 1f,
                chainRangeMultiplier: 1f,
                pierceBonus: 0f,
                knockbackMultiplier: 1f,
                damageTypeOverride: 255,
                damageFlags: DamageFlags.None,
                onHitCount: 0);

            BuildSpec(
                builder,
                ref array[1],
                "ammo.kinetic",
                damageMultiplier: 1.15f,
                speedMultiplier: 1.05f,
                lifetimeMultiplier: 1f,
                turnRateMultiplier: 1f,
                seekRadiusMultiplier: 1f,
                aoeRadiusMultiplier: 1f,
                chainRangeMultiplier: 1f,
                pierceBonus: 1f,
                knockbackMultiplier: 1.1f,
                damageTypeOverride: 255,
                damageFlags: DamageFlags.Pierce,
                onHitCount: 0);

            BuildSpec(
                builder,
                ref array[2],
                "ammo.he",
                damageMultiplier: 0.9f,
                speedMultiplier: 0.95f,
                lifetimeMultiplier: 1f,
                turnRateMultiplier: 1f,
                seekRadiusMultiplier: 1f,
                aoeRadiusMultiplier: 1.6f,
                chainRangeMultiplier: 1f,
                pierceBonus: 0f,
                knockbackMultiplier: 1.25f,
                damageTypeOverride: 255,
                damageFlags: DamageFlags.AoE,
                onHitCount: 1,
                out var heEffects);

            heEffects[0] = new EffectOp
            {
                Kind = EffectOpKind.AoE,
                Magnitude = 0.65f,
                Duration = 0f,
                Aux = 6f,
                StatusId = 0
            };

            BuildSpec(
                builder,
                ref array[3],
                "ammo.emp",
                damageMultiplier: 0.75f,
                speedMultiplier: 0.9f,
                lifetimeMultiplier: 1f,
                turnRateMultiplier: 1.2f,
                seekRadiusMultiplier: 1.1f,
                aoeRadiusMultiplier: 1f,
                chainRangeMultiplier: 1f,
                pierceBonus: 0f,
                knockbackMultiplier: 0.8f,
                damageTypeOverride: (byte)DamageType.Lightning,
                damageFlags: DamageFlags.IgnoreShield,
                onHitCount: 1,
                out var empEffects);

            empEffects[0] = new EffectOp
            {
                Kind = EffectOpKind.Status,
                Magnitude = 1f,
                Duration = 4f,
                Aux = 0f,
                StatusId = 2
            };

            BuildSpec(
                builder,
                ref array[4],
                "ammo.arc",
                damageMultiplier: 0.85f,
                speedMultiplier: 1f,
                lifetimeMultiplier: 1f,
                turnRateMultiplier: 1f,
                seekRadiusMultiplier: 1f,
                aoeRadiusMultiplier: 1f,
                chainRangeMultiplier: 1f,
                pierceBonus: 0f,
                knockbackMultiplier: 0.9f,
                damageTypeOverride: (byte)DamageType.Lightning,
                damageFlags: DamageFlags.Chain,
                onHitCount: 1,
                out var chainEffects);

            chainEffects[0] = new EffectOp
            {
                Kind = EffectOpKind.Chain,
                Magnitude = 1f,
                Duration = 0f,
                Aux = 8f,
                StatusId = 0
            };

            return builder.CreateBlobAssetReference<AmmoCatalogBlob>(Allocator.Persistent);
        }

        private static void BuildSpec(
            BlobBuilder builder,
            ref AmmoSpec spec,
            string id,
            float damageMultiplier,
            float speedMultiplier,
            float lifetimeMultiplier,
            float turnRateMultiplier,
            float seekRadiusMultiplier,
            float aoeRadiusMultiplier,
            float chainRangeMultiplier,
            float pierceBonus,
            float knockbackMultiplier,
            byte damageTypeOverride,
            DamageFlags damageFlags,
            int onHitCount)
        {
            BuildSpec(
                builder,
                ref spec,
                id,
                damageMultiplier,
                speedMultiplier,
                lifetimeMultiplier,
                turnRateMultiplier,
                seekRadiusMultiplier,
                aoeRadiusMultiplier,
                chainRangeMultiplier,
                pierceBonus,
                knockbackMultiplier,
                damageTypeOverride,
                damageFlags,
                onHitCount,
                out _);
        }

        private static void BuildSpec(
            BlobBuilder builder,
            ref AmmoSpec spec,
            string id,
            float damageMultiplier,
            float speedMultiplier,
            float lifetimeMultiplier,
            float turnRateMultiplier,
            float seekRadiusMultiplier,
            float aoeRadiusMultiplier,
            float chainRangeMultiplier,
            float pierceBonus,
            float knockbackMultiplier,
            byte damageTypeOverride,
            DamageFlags damageFlags,
            int onHitCount,
            out BlobBuilderArray<EffectOp> onHit)
        {
            spec = new AmmoSpec
            {
                Id = new FixedString32Bytes(id),
                DamageMultiplier = damageMultiplier,
                SpeedMultiplier = speedMultiplier,
                LifetimeMultiplier = lifetimeMultiplier,
                TurnRateMultiplier = turnRateMultiplier,
                SeekRadiusMultiplier = seekRadiusMultiplier,
                AoERadiusMultiplier = aoeRadiusMultiplier,
                ChainRangeMultiplier = chainRangeMultiplier,
                PierceBonus = pierceBonus,
                KnockbackMultiplier = knockbackMultiplier,
                DamageTypeOverride = damageTypeOverride,
                DamageFlags = damageFlags
            };

            onHit = builder.Allocate(ref spec.OnHitAdd, onHitCount);
            AmmoSpecSanitizer.Sanitize(ref spec);
        }
    }
}
