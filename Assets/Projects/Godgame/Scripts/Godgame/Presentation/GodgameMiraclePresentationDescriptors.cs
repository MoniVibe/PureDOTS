using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Presentation
{
    internal static class GodgameMiraclePresentationDescriptors
    {
        private static readonly Hash128 DefaultMiracle = Compute("godgame.miracle.generic");
        private static readonly Hash128 RainCharge = Compute("godgame.miracle.rain.charge");
        private static readonly Hash128 RainActive = Compute("godgame.miracle.rain.active");
        private static readonly Hash128 RainCooldown = Compute("godgame.miracle.rain.cooldown");
        private static readonly Hash128 FireballCharge = Compute("godgame.miracle.fireball.charge");
        private static readonly Hash128 FireballActive = Compute("godgame.miracle.fireball.active");
        private static readonly Hash128 FireballCooldown = Compute("godgame.miracle.fireball.cooldown");
        private static readonly Hash128 HealActive = Compute("godgame.miracle.heal.active");
        private static readonly Hash128 HealCooldown = Compute("godgame.miracle.heal.cooldown");
        private static readonly Hash128 ShieldActive = Compute("godgame.miracle.shield.active");
        private static readonly Hash128 ShieldCooldown = Compute("godgame.miracle.shield.cooldown");

        private static readonly Hash128 RainProjectile = Compute("godgame.projectile.rain");
        private static readonly Hash128 FireballProjectile = Compute("godgame.projectile.fireball");
        private static readonly Hash128 DefaultProjectile = Compute("godgame.projectile.generic");

        public static Hash128 ResolveMiracle(MiracleType type, MiracleLifecycleState lifecycle, MiracleCastingMode castingMode)
        {
            switch (type)
            {
                case MiracleType.Rain:
                    return lifecycle switch
                    {
                        MiracleLifecycleState.Charging => RainCharge.IsValid ? RainCharge : DefaultMiracle,
                        MiracleLifecycleState.Active => RainActive.IsValid ? RainActive : DefaultMiracle,
                        MiracleLifecycleState.CoolingDown => RainCooldown.IsValid ? RainCooldown : DefaultMiracle,
                        _ => RainActive.IsValid ? RainActive : DefaultMiracle
                    };
                case MiracleType.Fireball:
                    if (castingMode == MiracleCastingMode.Token && lifecycle == MiracleLifecycleState.Charging)
                    {
                        return FireballCharge.IsValid ? FireballCharge : DefaultMiracle;
                    }

                    return lifecycle switch
                    {
                        MiracleLifecycleState.CoolingDown => FireballCooldown.IsValid ? FireballCooldown : DefaultMiracle,
                        MiracleLifecycleState.Active => FireballActive.IsValid ? FireballActive : DefaultMiracle,
                        _ => FireballCharge.IsValid ? FireballCharge : DefaultMiracle
                    };
                case MiracleType.Heal:
                    return lifecycle == MiracleLifecycleState.CoolingDown
                        ? (HealCooldown.IsValid ? HealCooldown : DefaultMiracle)
                        : (HealActive.IsValid ? HealActive : DefaultMiracle);
                case MiracleType.Shield:
                    return lifecycle == MiracleLifecycleState.CoolingDown
                        ? (ShieldCooldown.IsValid ? ShieldCooldown : DefaultMiracle)
                        : (ShieldActive.IsValid ? ShieldActive : DefaultMiracle);
                default:
                    return DefaultMiracle;
            }
        }

        public static Hash128 ResolveProjectile(MiracleType type)
        {
            return type switch
            {
                MiracleType.Fireball => FireballProjectile.IsValid ? FireballProjectile : DefaultProjectile,
                MiracleType.Rain => RainProjectile.IsValid ? RainProjectile : DefaultProjectile,
                _ => DefaultProjectile
            };
        }

        public static bool TryGetTint(MiracleType type, out float4 tint)
        {
            switch (type)
            {
                case MiracleType.Rain:
                    tint = new float4(0.45f, 0.72f, 1f, 0.95f);
                    return true;
                case MiracleType.Fireball:
                    tint = new float4(1f, 0.55f, 0.2f, 1f);
                    return true;
                case MiracleType.Heal:
                    tint = new float4(0.4f, 0.92f, 0.65f, 1f);
                    return true;
                case MiracleType.Shield:
                    tint = new float4(0.35f, 0.95f, 0.95f, 1f);
                    return true;
                default:
                    tint = float4.zero;
                    return false;
            }
        }

        private static Hash128 Compute(string key)
        {
            return PresentationKeyUtility.TryParseKey(key, out var hash, out _)
                ? hash
                : default;
        }
    }
}
