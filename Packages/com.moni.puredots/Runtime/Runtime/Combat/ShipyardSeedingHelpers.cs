using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Helper utilities for seeding shipyard equip requests.
    /// </summary>
    public static class ShipyardSeedingHelpers
    {
        public static void EnsureEquipBuffer(EntityManager entityManager, Entity shipyardEntity)
        {
            if (!entityManager.HasBuffer<ShipyardEquipRequest>(shipyardEntity))
            {
                entityManager.AddBuffer<ShipyardEquipRequest>(shipyardEntity);
            }
        }

        public static void QueueEquip(EntityManager entityManager, Entity shipyardEntity, in ShipyardEquipRequest request)
        {
            EnsureEquipBuffer(entityManager, shipyardEntity);
            var buffer = entityManager.GetBuffer<ShipyardEquipRequest>(shipyardEntity);
            buffer.Add(request);
        }

        public static ShipyardEquipRequest CreateEquipRequest(
            Entity installEntity,
            FixedString64Bytes weaponId,
            FixedString32Bytes ammoId,
            uint triggerTick = 0)
        {
            return new ShipyardEquipRequest
            {
                InstallEntity = installEntity,
                WeaponId = weaponId,
                AmmoId = ammoId,
                UseTargetPool = 0,
                InstallMode = WeaponInstallMode.Mount,
                AimMode = WeaponSpawnerAimMode.TargetEntity,
                TriggerTick = triggerTick,
                RequireEnergy = 0f,
                RequireMaterials = 0f,
                RequireCrew = 0f,
                ConsumeBudget = 0,
                ReplaceExisting = 1,
                InitialEnergy = 1000f,
                InitialHeat = 0f,
                MagazineCapacity = 0,
                MagazineCurrent = 0,
                AmmoPerShot = 1,
                ReloadSec = 1f,
                StockpileCapacity = 0,
                StockpileCurrent = 0
            };
        }
    }
}
