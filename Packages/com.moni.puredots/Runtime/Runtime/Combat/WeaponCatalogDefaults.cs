using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Minimal default weapon catalog for PureDOTS-only headless runs.
    /// Provides a basic railgun and a basic launcher.
    /// </summary>
    public static class WeaponCatalogDefaults
    {
        public static BlobAssetReference<WeaponCatalogBlob> CreateDefaultCatalog()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<WeaponCatalogBlob>();
            var array = builder.Allocate(ref root.Weapons, 2);

            array[0] = new WeaponSpec
            {
                Id = new FixedString64Bytes("weapon.basic.railgun"),
                Class = (byte)WeaponClass.MassDriver,
                FireRate = 0.8f,
                Burst = 1,
                SpreadDeg = 0.5f,
                EnergyCost = 6f,
                HeatCost = 0.08f,
                ProjectileId = new FixedString32Bytes("projectile.simple.ballistic")
            };

            array[1] = new WeaponSpec
            {
                Id = new FixedString64Bytes("weapon.basic.launcher"),
                Class = (byte)WeaponClass.Missile,
                FireRate = 0.25f,
                Burst = 1,
                SpreadDeg = 2f,
                EnergyCost = 8f,
                HeatCost = 0.05f,
                ProjectileId = new FixedString32Bytes("projectile.simple.homing")
            };

            return builder.CreateBlobAssetReference<WeaponCatalogBlob>(Allocator.Persistent);
        }
    }
}
