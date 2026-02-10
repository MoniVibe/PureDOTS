using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Physics;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Scenarios
{
    /// <summary>
    /// Seeds a pure-data weapon spawner scenario (data-only emitters, no mounts).
    /// Scenario IDs:
    /// - scenario.puredots.weapon_spawner.smoke
    /// - scenario.puredots.projectile_tracking.audit
    /// - scenario.puredots.projectile_lifecycle.audit
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class WeaponSpawnerScenarioBootstrapSystem : SystemBase
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.puredots.weapon_spawner.smoke");
        private static readonly FixedString64Bytes AuditScenarioId = new FixedString64Bytes("scenario.puredots.projectile_tracking.audit");
        private static readonly FixedString64Bytes LifecycleScenarioId = new FixedString64Bytes("scenario.puredots.projectile_lifecycle.audit");

        protected override void OnCreate()
        {
            RequireForUpdate<ScenarioInfo>();
        }

        protected override void OnUpdate()
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            if (!scenarioInfo.ScenarioId.Equals(TargetScenarioId) &&
                !scenarioInfo.ScenarioId.Equals(AuditScenarioId) &&
                !scenarioInfo.ScenarioId.Equals(LifecycleScenarioId))
            {
                Enabled = false;
                return;
            }

            if (scenarioInfo.ScenarioId.Equals(LifecycleScenarioId))
            {
                SeedProjectileLifecycleAudit();
            }
            else
            {
                SeedWeaponSpawner();
            }
            Enabled = false;
        }

        private void SeedWeaponSpawner()
        {
            var target = CreateTarget(
                new float3(0f, 0f, 35f),
                new FixedString64Bytes("target.spawner"),
                240f,
                1.5f);

            CreateSpawner(
                new float3(0f, 0f, 0f),
                new FixedString64Bytes("weapon.basic.launcher"),
                target,
                new FixedString32Bytes("ammo.arc"),
                301u);
        }

        private void SeedProjectileLifecycleAudit()
        {
            var target = CreateTarget(
                new float3(0f, 0f, 35f),
                new FixedString64Bytes("target.lifecycle"),
                240f,
                1.5f);

            CreateSpawner(
                new float3(0f, 0f, 0f),
                new FixedString64Bytes("weapon.basic.launcher"),
                target,
                new FixedString32Bytes("ammo.standard"),
                311u);

            CreateDirectionalSpawner(
                new float3(12f, 0f, -5f),
                new FixedString64Bytes("weapon.basic.railgun"),
                new float3(1f, 0f, 0f),
                new FixedString32Bytes("ammo.standard"),
                312u);
        }

        private Entity CreateTarget(float3 position, FixedString64Bytes id, float health, float radius)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.AddComponentData(entity, new PersistentId { Value = unchecked((uint)id.GetHashCode()) });

            EntityManager.AddComponentData(entity, new Health
            {
                Current = health,
                Max = health,
                RegenRate = 0f,
                LastDamageTick = 0
            });

            EntityManager.AddBuffer<DamageEvent>(entity);
            EntityManager.AddBuffer<DeathEvent>(entity);

            EntityManager.AddComponentData(entity, new VelocitySample
            {
                Velocity = float3.zero,
                LastPosition = position
            });

            EntityManager.AddComponentData(entity, new RequiresPhysics
            {
                Priority = 1,
                Flags = PhysicsInteractionFlags.Collidable
            });

            EntityManager.AddComponentData(entity, PhysicsColliderSpec.CreateSphere(radius, PhysicsInteractionFlags.Collidable));

            return entity;
        }

        private Entity CreateSpawner(float3 position, FixedString64Bytes weaponId, Entity target, FixedString32Bytes ammoId, uint persistentId)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.AddComponentData(entity, new PersistentId { Value = persistentId });

            EntityManager.AddComponentData(entity, new WeaponSpawner
            {
                WeaponId = weaponId,
                AmmoId = ammoId,
                TargetEntity = target,
                TargetPosition = float3.zero,
                FireDirection = float3.zero,
                AimMode = WeaponSpawnerAimMode.TargetEntity,
                LastFireTime = -999f,
                EnergyReserve = 1000f,
                HeatLevel = 0f,
                ShotSequence = 0,
                IsActive = 1
            });

            EntityManager.AddComponentData(entity, new AmmoStockpile
            {
                AmmoType = ammoId,
                Current = 160,
                Capacity = 160
            });

            EntityManager.AddComponentData(entity, new WeaponMagazine
            {
                AmmoType = ammoId,
                Current = 12,
                Capacity = 12,
                AmmoPerShot = 1,
                ReloadSec = 1.6f,
                LastReloadTime = -999f
            });

            return entity;
        }

        private Entity CreateDirectionalSpawner(float3 position, FixedString64Bytes weaponId, float3 direction, FixedString32Bytes ammoId, uint persistentId)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.AddComponentData(entity, new PersistentId { Value = persistentId });

            EntityManager.AddComponentData(entity, new WeaponSpawner
            {
                WeaponId = weaponId,
                AmmoId = ammoId,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                FireDirection = math.normalizesafe(direction, new float3(0f, 0f, 1f)),
                AimMode = WeaponSpawnerAimMode.FixedDirection,
                LastFireTime = -999f,
                EnergyReserve = 1000f,
                HeatLevel = 0f,
                ShotSequence = 0,
                IsActive = 1
            });

            EntityManager.AddComponentData(entity, new AmmoStockpile
            {
                AmmoType = ammoId,
                Current = 160,
                Capacity = 160
            });

            EntityManager.AddComponentData(entity, new WeaponMagazine
            {
                AmmoType = ammoId,
                Current = 12,
                Capacity = 12,
                AmmoPerShot = 1,
                ReloadSec = 1.6f,
                LastReloadTime = -999f
            });

            return entity;
        }
    }
}
