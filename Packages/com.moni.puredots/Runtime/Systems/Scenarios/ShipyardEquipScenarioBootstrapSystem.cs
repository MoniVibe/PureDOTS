using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Scenarios
{
    /// <summary>
    /// Seeds a pure-data shipyard equip scenario using weapon pools.
    /// Scenario ID: scenario.puredots.shipyard.equip
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ShipyardEquipScenarioBootstrapSystem : SystemBase
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.puredots.shipyard.equip");

        protected override void OnCreate()
        {
            RequireForUpdate<ScenarioInfo>();
        }

        protected override void OnUpdate()
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            if (!scenarioInfo.ScenarioId.Equals(TargetScenarioId))
            {
                Enabled = false;
                return;
            }

            SeedShipyardEquip();
            Enabled = false;
        }

        private void SeedShipyardEquip()
        {
            var shipyard = CreateShipyard(new float3(0f, 0f, 0f));
            var hull = CreateHull(new float3(8f, 0f, 0f));

            var poolConfig = new WeaponPoolConfig
            {
                SelectionMode = WeaponPoolSelectionMode.WeightedRandom,
                MinIntervalSec = 0f,
                LastSelectTime = -999f,
                MaxSelections = 0,
                SelectionsMade = 0,
                RoundRobinIndex = 0,
                AutoInstall = 0,
                RequireNoWeapon = 1,
                ReplaceExisting = 1,
                ConsumeBudget = 0,
                PrimaryBias = 1.4f,
                SecondaryBias = 0.7f,
                PointDefenseBias = 0.5f,
                SupportBias = 0.6f,
                ExperimentalBias = 0.2f
            };

            EntityManager.AddComponentData(hull, poolConfig);
            var pool = EntityManager.AddBuffer<WeaponPoolEntry>(hull);
            pool.Add(new WeaponPoolEntry
            {
                WeaponId = new FixedString64Bytes("weapon.basic.railgun"),
                AmmoId = new FixedString32Bytes("ammo.kinetic"),
                Role = WeaponPoolRole.Primary,
                InstallMode = WeaponInstallMode.Mount,
                AimMode = WeaponSpawnerAimMode.TargetEntity,
                Weight = 1.0f,
                ReplaceExisting = 1,
                ConsumeBudget = 0,
                InitialEnergy = 1000f,
                InitialHeat = 0f,
                MagazineCapacity = 12,
                MagazineCurrent = 12,
                AmmoPerShot = 1,
                ReloadSec = 1.4f,
                StockpileCapacity = 120,
                StockpileCurrent = 120
            });

            pool.Add(new WeaponPoolEntry
            {
                WeaponId = new FixedString64Bytes("weapon.basic.launcher"),
                AmmoId = new FixedString32Bytes("ammo.he"),
                Role = WeaponPoolRole.Secondary,
                InstallMode = WeaponInstallMode.Mount,
                AimMode = WeaponSpawnerAimMode.TargetEntity,
                Weight = 0.8f,
                ReplaceExisting = 1,
                ConsumeBudget = 0,
                InitialEnergy = 1000f,
                InitialHeat = 0f,
                MagazineCapacity = 8,
                MagazineCurrent = 8,
                AmmoPerShot = 1,
                ReloadSec = 1.8f,
                StockpileCapacity = 80,
                StockpileCurrent = 80
            });

            ShipyardSeedingHelpers.QueueEquip(EntityManager, shipyard, new ShipyardEquipRequest
            {
                InstallEntity = hull,
                WeaponId = default,
                AmmoId = default,
                UseTargetPool = 1,
                InstallMode = WeaponInstallMode.Mount,
                AimMode = WeaponSpawnerAimMode.TargetEntity,
                TriggerTick = 0,
                ShipyardRequireEnergy = 50f,
                ShipyardRequireMaterials = 10f,
                ShipyardRequireCrew = 1f,
                ConsumeShipyardBudget = 1,
                ReplaceExisting = 1
            });
        }

        private Entity CreateShipyard(float3 position)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.AddComponentData(entity, new PersistentId { Value = 1001u });
            EntityManager.AddComponentData(entity, new Shipyard
            {
                Range = 25f,
                InstallCooldownSec = 0f,
                LastInstallTime = -999f,
                IsActive = 1
            });

            EntityManager.AddComponentData(entity, new ShipyardBuildBudget
            {
                Energy = 1000f,
                Materials = 400f,
                Crew = 40f,
                LastSpendTime = 0f
            });

            ShipyardSeedingHelpers.EnsureEquipBuffer(EntityManager, entity);
            return entity;
        }

        private Entity CreateHull(float3 position)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.AddComponentData(entity, new PersistentId { Value = 2001u });

            EntityManager.AddComponentData(entity, new WeaponBuildBudget
            {
                Energy = 200f,
                Materials = 100f,
                Crew = 10f,
                LastSpendTime = 0f
            });

            return entity;
        }
    }
}
