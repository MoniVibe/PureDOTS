using NUnit.Framework;
using PureDOTS.Runtime.Combat;
using Unity.Collections;

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ProjectileSpec validation - ensures specs are sane and valid.
    /// </summary>
    public class ProjectileSpecSanityTests
    {
        [Test]
        public void ProjectileSpec_NonNegativeSpeeds()
        {
            var spec = new ProjectileSpec
            {
                Id = "test.projectile",
                Kind = (byte)ProjectileKind.Ballistic,
                Speed = -10f, // Invalid: negative speed
                Lifetime = 5f,
                TurnRateDeg = 0f,
                SeekRadius = 0f,
                AoERadius = 0f,
                Pierce = 0f,
                ChainRange = 0f,
                HitFilter = 0xFFFFFFFF,
                Damage = new DamageModel { BaseDamage = 10f }
            };

            Assert.GreaterOrEqual(spec.Speed, 0f, "Speed should be non-negative");
        }

        [Test]
        public void ProjectileSpec_NonNegativeLifetime()
        {
            var spec = new ProjectileSpec
            {
                Id = "test.projectile",
                Kind = (byte)ProjectileKind.Ballistic,
                Speed = 100f,
                Lifetime = -1f, // Invalid: negative lifetime
                TurnRateDeg = 0f,
                SeekRadius = 0f,
                AoERadius = 0f,
                Pierce = 0f,
                ChainRange = 0f,
                HitFilter = 0xFFFFFFFF,
                Damage = new DamageModel { BaseDamage = 10f }
            };

            Assert.GreaterOrEqual(spec.Lifetime, 0f, "Lifetime should be non-negative");
        }

        [Test]
        public void ProjectileSpec_NonNegativeRadius()
        {
            var spec = new ProjectileSpec
            {
                Id = "test.projectile",
                Kind = (byte)ProjectileKind.Ballistic,
                Speed = 100f,
                Lifetime = 5f,
                TurnRateDeg = 0f,
                SeekRadius = 0f,
                AoERadius = -5f, // Invalid: negative radius
                Pierce = 0f,
                ChainRange = 0f,
                HitFilter = 0xFFFFFFFF,
                Damage = new DamageModel { BaseDamage = 10f }
            };

            Assert.GreaterOrEqual(spec.AoERadius, 0f, "AoE radius should be non-negative");
        }

        [Test]
        public void ProjectileSpec_ValidCollisionFilter()
        {
            var spec = new ProjectileSpec
            {
                Id = "test.projectile",
                Kind = (byte)ProjectileKind.Ballistic,
                Speed = 100f,
                Lifetime = 5f,
                TurnRateDeg = 0f,
                SeekRadius = 0f,
                AoERadius = 0f,
                Pierce = 0f,
                ChainRange = 0f,
                HitFilter = 0xFFFFFFFF, // Valid: all layers
                Damage = new DamageModel { BaseDamage = 10f }
            };

            // Filter should be valid (non-zero for collision)
            Assert.IsTrue(spec.HitFilter != 0 || spec.AoERadius > 0f, "HitFilter should be non-zero or AoE radius should be positive");
        }

        [Test]
        public void ProjectileSpec_BeamHasZeroSpeed()
        {
            var spec = new ProjectileSpec
            {
                Id = "test.beam",
                Kind = (byte)ProjectileKind.Beam,
                Speed = 100f, // Beam should have 0 speed (instant hit)
                Lifetime = 5f,
                TurnRateDeg = 0f,
                SeekRadius = 0f,
                AoERadius = 0f,
                Pierce = 0f,
                ChainRange = 0f,
                HitFilter = 0xFFFFFFFF,
                Damage = new DamageModel { BaseDamage = 10f }
            };

            // Beam projectiles should have speed 0 (handled by BeamTickSystem)
            if ((ProjectileKind)spec.Kind == ProjectileKind.Beam)
            {
                Assert.AreEqual(0f, spec.Speed, 0.01f, "Beam projectiles should have speed 0");
            }
        }

        [Test]
        public void ProjectileSpec_HomingHasTurnRate()
        {
            var spec = new ProjectileSpec
            {
                Id = "test.homing",
                Kind = (byte)ProjectileKind.Homing,
                Speed = 100f,
                Lifetime = 5f,
                TurnRateDeg = 0f, // Invalid: homing should have turn rate
                SeekRadius = 50f,
                AoERadius = 0f,
                Pierce = 0f,
                ChainRange = 0f,
                HitFilter = 0xFFFFFFFF,
                Damage = new DamageModel { BaseDamage = 10f }
            };

            // Homing projectiles should have positive turn rate
            if ((ProjectileKind)spec.Kind == ProjectileKind.Homing)
            {
                Assert.Greater(spec.TurnRateDeg, 0f, "Homing projectiles should have positive turn rate");
            }
        }
    }
}

