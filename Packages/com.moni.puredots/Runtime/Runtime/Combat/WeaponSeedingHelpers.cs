using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Helper utilities for seeding weapon installs in scenarios or tests.
    /// </summary>
    public static class WeaponSeedingHelpers
    {
        public static void EnsureInstallBuffer(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasBuffer<WeaponInstallRequest>(entity))
            {
                entityManager.AddBuffer<WeaponInstallRequest>(entity);
            }
        }

        public static void QueueInstall(EntityManager entityManager, Entity entity, in WeaponInstallRequest request)
        {
            EnsureInstallBuffer(entityManager, entity);
            var buffer = entityManager.GetBuffer<WeaponInstallRequest>(entity);
            buffer.Add(request);
        }

        public static WeaponInstallRequest CreateMountRequest(
            FixedString64Bytes weaponId,
            FixedString32Bytes ammoId,
            Entity targetEntity,
            float3 targetPosition,
            uint triggerTick = 0)
        {
            return new WeaponInstallRequest
            {
                WeaponId = weaponId,
                AmmoId = ammoId,
                TargetEntity = targetEntity,
                TargetPosition = targetPosition,
                FireDirection = float3.zero,
                Mode = WeaponInstallMode.Mount,
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

        public static WeaponInstallRequest CreateSpawnerRequest(
            FixedString64Bytes weaponId,
            FixedString32Bytes ammoId,
            WeaponSpawnerAimMode aimMode,
            Entity targetEntity,
            float3 targetPosition,
            float3 fireDirection,
            uint triggerTick = 0)
        {
            return new WeaponInstallRequest
            {
                WeaponId = weaponId,
                AmmoId = ammoId,
                TargetEntity = targetEntity,
                TargetPosition = targetPosition,
                FireDirection = fireDirection,
                Mode = WeaponInstallMode.Spawner,
                AimMode = aimMode,
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
