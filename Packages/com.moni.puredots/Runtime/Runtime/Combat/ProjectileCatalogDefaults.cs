using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Minimal default projectile catalog for PureDOTS-only headless runs.
    /// Provides a simple ballistic round and a basic homing round.
    /// </summary>
    public static class ProjectileCatalogDefaults
    {
        public static BlobAssetReference<ProjectileCatalogBlob> CreateDefaultCatalog()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ProjectileCatalogBlob>();
            var array = builder.Allocate(ref root.Projectiles, 2);

            BuildSpec(
                builder,
                ref array[0],
                "projectile.simple.ballistic",
                ProjectileKind.Ballistic,
                speed: 45f,
                lifetime: 6f,
                turnRateDeg: 0f,
                seekRadius: 0f,
                aoeRadius: 0f,
                pierce: 0f,
                hitFilter: 0xFFFFFFFFu,
                baseDamage: 12f);

            BuildSpec(
                builder,
                ref array[1],
                "projectile.simple.homing",
                ProjectileKind.Homing,
                speed: 30f,
                lifetime: 8f,
                turnRateDeg: 90f,
                seekRadius: 120f,
                aoeRadius: 0f,
                pierce: 0f,
                hitFilter: 0xFFFFFFFFu,
                baseDamage: 18f);

            return builder.CreateBlobAssetReference<ProjectileCatalogBlob>(Allocator.Persistent);
        }

        private static void BuildSpec(
            BlobBuilder builder,
            ref ProjectileSpec spec,
            string id,
            ProjectileKind kind,
            float speed,
            float lifetime,
            float turnRateDeg,
            float seekRadius,
            float aoeRadius,
            float pierce,
            uint hitFilter,
            float baseDamage)
        {
            spec = new ProjectileSpec
            {
                Id = new FixedString64Bytes(id),
                Kind = (byte)kind,
                Speed = speed,
                Lifetime = lifetime,
                TurnRateDeg = turnRateDeg,
                SeekRadius = seekRadius,
                AoERadius = aoeRadius,
                Pierce = pierce,
                HitFilter = hitFilter,
                Damage = new DamageModel
                {
                    BaseDamage = baseDamage,
                    ShieldMultiplier = 1f,
                    ArmorMultiplier = 1f,
                    HullMultiplier = 1f
                }
            };

            var effects = builder.Allocate(ref spec.OnHit, 1);
            effects[0] = new EffectOp
            {
                Kind = EffectOpKind.Damage,
                Magnitude = 1f,
                Duration = 0f,
                Aux = 0f,
                StatusId = 0
            };

            ProjectileSpecSanitizer.Sanitize(ref spec);
        }
    }
}
