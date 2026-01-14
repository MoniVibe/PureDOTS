using System;
using NUnit.Framework;
using PureDOTS.Runtime.Combat;
using Unity.Collections;
using Unity.Entities;

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
            using var specBlob = CreateProjectileSpecBlob((ref ProjectileSpec spec) =>
            {
                spec.Id = "test.projectile";
                spec.Kind = (byte)ProjectileKind.Ballistic;
                spec.Speed = -10f;
                spec.Lifetime = 5f;
                spec.TurnRateDeg = 0f;
                spec.SeekRadius = 0f;
                spec.AoERadius = 0f;
                spec.Pierce = 0f;
                spec.ChainRange = 0f;
                spec.HitFilter = 0xFFFFFFFF;
                spec.Damage = new DamageModel { BaseDamage = 10f };
            });
            
            ref var spec = ref specBlob.Value;
            Assert.GreaterOrEqual(spec.Speed, 0f, "Speed should be non-negative");
        }
        
        [Test]
        public void ProjectileSpec_NonNegativeLifetime()
        {
            using var specBlob = CreateProjectileSpecBlob((ref ProjectileSpec spec) =>
            {
                spec.Id = "test.projectile";
                spec.Kind = (byte)ProjectileKind.Ballistic;
                spec.Speed = 100f;
                spec.Lifetime = -1f;
                spec.TurnRateDeg = 0f;
                spec.SeekRadius = 0f;
                spec.AoERadius = 0f;
                spec.Pierce = 0f;
                spec.ChainRange = 0f;
                spec.HitFilter = 0xFFFFFFFF;
                spec.Damage = new DamageModel { BaseDamage = 10f };
            });
            
            ref var spec = ref specBlob.Value;
            Assert.GreaterOrEqual(spec.Lifetime, 0f, "Lifetime should be non-negative");
        }
        
        [Test]
        public void ProjectileSpec_NonNegativeRadius()
        {
            using var specBlob = CreateProjectileSpecBlob((ref ProjectileSpec spec) =>
            {
                spec.Id = "test.projectile";
                spec.Kind = (byte)ProjectileKind.Ballistic;
                spec.Speed = 100f;
                spec.Lifetime = 5f;
                spec.TurnRateDeg = 0f;
                spec.SeekRadius = 0f;
                spec.AoERadius = -5f;
                spec.Pierce = 0f;
                spec.ChainRange = 0f;
                spec.HitFilter = 0xFFFFFFFF;
                spec.Damage = new DamageModel { BaseDamage = 10f };
            });
            
            ref var spec = ref specBlob.Value;
            Assert.GreaterOrEqual(spec.AoERadius, 0f, "AoE radius should be non-negative");
        }
        
        [Test]
        public void ProjectileSpec_ValidCollisionFilter()
        {
            using var specBlob = CreateProjectileSpecBlob((ref ProjectileSpec spec) =>
            {
                spec.Id = "test.projectile";
                spec.Kind = (byte)ProjectileKind.Ballistic;
                spec.Speed = 100f;
                spec.Lifetime = 5f;
                spec.TurnRateDeg = 0f;
                spec.SeekRadius = 0f;
                spec.AoERadius = 0f;
                spec.Pierce = 0f;
                spec.ChainRange = 0f;
                spec.HitFilter = 0xFFFFFFFF;
                spec.Damage = new DamageModel { BaseDamage = 10f };
            });
            
            ref var spec = ref specBlob.Value;
            Assert.IsTrue(spec.HitFilter != 0 || spec.AoERadius > 0f, "HitFilter should be non-zero or AoE radius should be positive");
        }
        
        [Test]
        public void ProjectileSpec_BeamHasZeroSpeed()
        {
            using var specBlob = CreateProjectileSpecBlob((ref ProjectileSpec spec) =>
            {
                spec.Id = "test.beam";
                spec.Kind = (byte)ProjectileKind.Beam;
                spec.Speed = 100f;
                spec.Lifetime = 5f;
                spec.TurnRateDeg = 0f;
                spec.SeekRadius = 0f;
                spec.AoERadius = 0f;
                spec.Pierce = 0f;
                spec.ChainRange = 0f;
                spec.HitFilter = 0xFFFFFFFF;
                spec.Damage = new DamageModel { BaseDamage = 10f };
            });
            
            ref var spec = ref specBlob.Value;
            if ((ProjectileKind)spec.Kind == ProjectileKind.Beam)
            {
                Assert.AreEqual(0f, spec.Speed, 0.01f, "Beam projectiles should have speed 0");
            }
        }
        
        [Test]
        public void ProjectileSpec_HomingHasTurnRate()
        {
            using var specBlob = CreateProjectileSpecBlob((ref ProjectileSpec spec) =>
            {
                spec.Id = "test.homing";
                spec.Kind = (byte)ProjectileKind.Homing;
                spec.Speed = 100f;
                spec.Lifetime = 5f;
                spec.TurnRateDeg = 0f;
                spec.SeekRadius = 50f;
                spec.AoERadius = 0f;
                spec.Pierce = 0f;
                spec.ChainRange = 0f;
                spec.HitFilter = 0xFFFFFFFF;
                spec.Damage = new DamageModel { BaseDamage = 10f };
            });
            
            ref var spec = ref specBlob.Value;
            if ((ProjectileKind)spec.Kind == ProjectileKind.Homing)
            {
                Assert.Greater(spec.TurnRateDeg, 0f, "Homing projectiles should have positive turn rate");
            }
        }
        
        private delegate void RefSpecConfigurator(ref ProjectileSpec spec);

        private static BlobAssetReference<ProjectileSpec> CreateProjectileSpecBlob(RefSpecConfigurator configure)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var spec = ref builder.ConstructRoot<ProjectileSpec>();
            
            builder.Allocate(ref spec.OnHit, 0);

            configure(ref spec);
            ProjectileSpecSanitizer.Sanitize(ref spec);
            
            var blob = builder.CreateBlobAssetReference<ProjectileSpec>(Allocator.Temp);
            builder.Dispose();
            return blob;
        }
    }
}
