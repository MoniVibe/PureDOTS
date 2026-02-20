using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    public enum WeaponBehaviorArchetype : byte
    {
        Default = 0,
        Energy = 1,
        Kinetic = 2,
        GuidedMissile = 3,
        GuidedHeavy = 4,
        DissipativePlasma = 5,
        DissipativeFlame = 6
    }

    public struct WeaponBehaviorProfile
    {
        public float HeatMultiplier;
        public float AmmoMultiplier;
        public float HitChanceMultiplier;
        public float DamageNearMultiplier;
        public float DamageFarMultiplier;
        public float DamageFalloffStart01;
        public float DamageFalloffEnd01;
        public float TurnRateMultiplier;
        public float SeekRadiusMultiplier;
        public float DeflectResistanceMultiplier;
    }

    public static class WeaponBehaviorProfiles
    {
        public static WeaponBehaviorArchetype ResolveDefaultArchetype(WeaponClass weaponClass, ProjectileKind projectileKind)
        {
            return (weaponClass, projectileKind) switch
            {
                (WeaponClass.Missile, ProjectileKind.Homing) => WeaponBehaviorArchetype.GuidedMissile,
                (WeaponClass.Torpedo, ProjectileKind.Homing) => WeaponBehaviorArchetype.GuidedHeavy,
                (WeaponClass.BeamCannon, _) => WeaponBehaviorArchetype.Energy,
                (WeaponClass.MassDriver, _) => WeaponBehaviorArchetype.Kinetic,
                (WeaponClass.PointDefense, _) => WeaponBehaviorArchetype.Kinetic,
                (_, ProjectileKind.Homing) => WeaponBehaviorArchetype.GuidedMissile,
                (_, ProjectileKind.Ballistic) => WeaponBehaviorArchetype.Kinetic,
                _ => WeaponBehaviorArchetype.Default
            };
        }

        public static WeaponBehaviorProfile Resolve(WeaponBehaviorArchetype archetype)
        {
            return archetype switch
            {
                WeaponBehaviorArchetype.Energy => new WeaponBehaviorProfile
                {
                    HeatMultiplier = 1.2f,
                    AmmoMultiplier = 0f,
                    HitChanceMultiplier = 1f,
                    DamageNearMultiplier = 1.02f,
                    DamageFarMultiplier = 0.94f,
                    DamageFalloffStart01 = 0.4f,
                    DamageFalloffEnd01 = 1f,
                    TurnRateMultiplier = 1f,
                    SeekRadiusMultiplier = 1f,
                    DeflectResistanceMultiplier = 1f
                },
                WeaponBehaviorArchetype.Kinetic => new WeaponBehaviorProfile
                {
                    HeatMultiplier = 0.82f,
                    AmmoMultiplier = 1f,
                    HitChanceMultiplier = 1f,
                    DamageNearMultiplier = 1f,
                    DamageFarMultiplier = 0.9f,
                    DamageFalloffStart01 = 0.5f,
                    DamageFalloffEnd01 = 1f,
                    TurnRateMultiplier = 1f,
                    SeekRadiusMultiplier = 1f,
                    DeflectResistanceMultiplier = 1.05f
                },
                WeaponBehaviorArchetype.GuidedMissile => new WeaponBehaviorProfile
                {
                    HeatMultiplier = 0.95f,
                    AmmoMultiplier = 1.8f,
                    HitChanceMultiplier = 0.82f,
                    DamageNearMultiplier = 1f,
                    DamageFarMultiplier = 1f,
                    DamageFalloffStart01 = 1f,
                    DamageFalloffEnd01 = 1f,
                    TurnRateMultiplier = 1.1f,
                    SeekRadiusMultiplier = 1.2f,
                    DeflectResistanceMultiplier = 0.72f
                },
                WeaponBehaviorArchetype.GuidedHeavy => new WeaponBehaviorProfile
                {
                    HeatMultiplier = 1.05f,
                    AmmoMultiplier = 2.3f,
                    HitChanceMultiplier = 0.74f,
                    DamageNearMultiplier = 1.08f,
                    DamageFarMultiplier = 0.96f,
                    DamageFalloffStart01 = 0.7f,
                    DamageFalloffEnd01 = 1f,
                    TurnRateMultiplier = 0.82f,
                    SeekRadiusMultiplier = 1.05f,
                    DeflectResistanceMultiplier = 0.66f
                },
                WeaponBehaviorArchetype.DissipativePlasma => new WeaponBehaviorProfile
                {
                    HeatMultiplier = 1.35f,
                    AmmoMultiplier = 0f,
                    HitChanceMultiplier = 1f,
                    DamageNearMultiplier = 1.08f,
                    DamageFarMultiplier = 0.42f,
                    DamageFalloffStart01 = 0.35f,
                    DamageFalloffEnd01 = 1f,
                    TurnRateMultiplier = 1f,
                    SeekRadiusMultiplier = 1f,
                    DeflectResistanceMultiplier = 1f
                },
                WeaponBehaviorArchetype.DissipativeFlame => new WeaponBehaviorProfile
                {
                    HeatMultiplier = 1.25f,
                    AmmoMultiplier = 0f,
                    HitChanceMultiplier = 1f,
                    DamageNearMultiplier = 1.15f,
                    DamageFarMultiplier = 0.25f,
                    DamageFalloffStart01 = 0.2f,
                    DamageFalloffEnd01 = 0.8f,
                    TurnRateMultiplier = 1f,
                    SeekRadiusMultiplier = 1f,
                    DeflectResistanceMultiplier = 1f
                },
                _ => new WeaponBehaviorProfile
                {
                    HeatMultiplier = 1f,
                    AmmoMultiplier = 1f,
                    HitChanceMultiplier = 1f,
                    DamageNearMultiplier = 1f,
                    DamageFarMultiplier = 1f,
                    DamageFalloffStart01 = 1f,
                    DamageFalloffEnd01 = 1f,
                    TurnRateMultiplier = 1f,
                    SeekRadiusMultiplier = 1f,
                    DeflectResistanceMultiplier = 1f
                }
            };
        }

        public static float ResolveHeatCost(float baseHeatCost, in WeaponBehaviorProfile profile)
        {
            return math.max(0f, baseHeatCost * math.max(0f, profile.HeatMultiplier));
        }

        public static int ResolveAmmoPerShot(int baseAmmoPerShot, in WeaponBehaviorProfile profile)
        {
            if (baseAmmoPerShot <= 0)
            {
                return 0;
            }

            if (profile.AmmoMultiplier <= 0f)
            {
                return 0;
            }

            return math.max(1, (int)math.ceil(baseAmmoPerShot * profile.AmmoMultiplier));
        }

        public static float ResolveHitChanceMultiplier(in WeaponBehaviorProfile profile)
        {
            return math.clamp(profile.HitChanceMultiplier, 0.05f, 1.5f);
        }

        public static float ResolveDistanceDamageMultiplier(
            float distanceTraveled,
            float speed,
            float lifetime,
            in WeaponBehaviorProfile profile)
        {
            if (distanceTraveled <= 0f)
            {
                return math.max(0f, profile.DamageNearMultiplier);
            }

            var maxDistance = speed > 0f && lifetime > 0f ? speed * lifetime : math.max(distanceTraveled, 1f);
            var normalized = math.saturate(distanceTraveled / math.max(1f, maxDistance));
            var start = math.clamp(profile.DamageFalloffStart01, 0f, 1f);
            var end = math.clamp(math.max(start + 0.0001f, profile.DamageFalloffEnd01), 0.0001f, 1f);
            var t = math.saturate((normalized - start) / math.max(0.0001f, end - start));
            return math.lerp(profile.DamageNearMultiplier, profile.DamageFarMultiplier, t);
        }

        public static float ResolveTurnRate(float baseTurnRateDeg, in WeaponBehaviorProfile profile)
        {
            return math.max(0f, baseTurnRateDeg * math.max(0f, profile.TurnRateMultiplier));
        }

        public static float ResolveSeekRadius(float baseSeekRadius, in WeaponBehaviorProfile profile)
        {
            return math.max(0f, baseSeekRadius * math.max(0f, profile.SeekRadiusMultiplier));
        }

        public static float ResolveDeflectResistance(float baseResistance, in WeaponBehaviorProfile profile)
        {
            return math.saturate(baseResistance * math.max(0f, profile.DeflectResistanceMultiplier));
        }
    }
}
