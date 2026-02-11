using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Physics;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Spatial;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Scenarios
{
    /// <summary>
    /// Seeds a pure-data firing range scenario with basic railgun + launcher weapons.
    /// Scenario ID: scenario.puredots.firing_range.smoke
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class FiringRangeScenarioBootstrapSystem : SystemBase
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.puredots.firing_range.smoke");

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

            SeedFiringRange();
            Enabled = false;
        }

        private void SeedFiringRange()
        {
            var targetBallistic = CreateTarget(
                new float3(0f, 0f, 30f),
                new FixedString64Bytes("target.ballistic"),
                250f,
                1.5f);

            var targetHoming = CreateTarget(
                new float3(12f, 0f, 40f),
                new FixedString64Bytes("target.homing"),
                250f,
                1.5f);

            CreateShooter(
                new float3(0f, 0f, 0f),
                new FixedString64Bytes("weapon.basic.railgun"),
                targetBallistic,
                new FixedString32Bytes("ammo.kinetic"),
                101u);

            CreateShooter(
                new float3(-8f, 0f, -4f),
                new FixedString64Bytes("weapon.basic.launcher"),
                targetHoming,
                new FixedString32Bytes("ammo.he"),
                102u);
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

        private Entity CreateShooter(float3 position, FixedString64Bytes weaponId, Entity target, FixedString32Bytes ammoId, uint persistentId)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            EntityManager.AddComponentData(entity, new PersistentId { Value = persistentId });

            EntityManager.AddComponentData(entity, new WeaponMount
            {
                WeaponId = weaponId,
                TurretId = default,
                TargetEntity = target,
                TargetPosition = float3.zero,
                LastFireTime = -999f,
                HeatLevel = 0f,
                EnergyReserve = 1000f,
                IsFiring = true,
                ShotSequence = 0
            });

            EntityManager.AddComponentData(entity, new AmmoStockpile
            {
                AmmoType = ammoId,
                Current = 200,
                Capacity = 200
            });

            EntityManager.AddComponentData(entity, new WeaponMagazine
            {
                AmmoType = ammoId,
                Current = 20,
                Capacity = 20,
                AmmoPerShot = 1,
                ReloadSec = 1.5f,
                LastReloadTime = -999f
            });

            EntityManager.AddComponentData(entity, new DeflectionProfile
            {
                ReactionSec = 0.15f,
                MinThreatScore = 0.2f,
                MaxThreatScore = 1.0f,
                DodgeBias = 0.4f,
                BlockBias = 0.2f,
                DeflectBias = 0.2f,
                RedirectBias = 0.1f,
                ControlBias = 0.1f,
                MaxActionsPerSecond = 6f,
                CooldownSec = 0.1f
            });

            EntityManager.AddComponentData(entity, new DeflectionBudget
            {
                Energy = 200f,
                Mana = 50f,
                Focus = 50f,
                Ammo = 20f,
                LastSpendTime = 0f
            });

            EntityManager.AddComponentData(entity, new FriendlyFireTolerance
            {
                Threshold = 5f,
                IncidentDecay = 15f
            });

            EntityManager.AddComponentData(entity, new EngagementEnvelope
            {
                MinRange = 5f,
                PreferredRange = 20f,
                MaxRange = 80f,
                HoldRange = 15f
            });

            EntityManager.AddComponentData(entity, new PositioningIntent
            {
                Mode = PositioningMode.Hold,
                Anchor = Entity.Null,
                Offset = float3.zero,
                Priority = 1f
            });

            EntityManager.AddComponentData(entity, new StrafeProfile
            {
                LateralSpeed = 2f,
                Jitter = 0.2f,
                ChangeIntervalSec = 1f
            });

            return entity;
        }
    }
}
