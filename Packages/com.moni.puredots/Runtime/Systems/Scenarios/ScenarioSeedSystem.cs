using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Focus;
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
    /// Centralized scenario seeding registry for PureDOTS headless scenarios.
    /// Keeps scenario JSONs minimal by driving spawn via ScenarioId.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ScenarioSeedSystem : SystemBase
    {
        private static readonly FixedString64Bytes FiringRangeScenarioId = new FixedString64Bytes("scenario.puredots.firing_range.smoke");
        private static readonly FixedString64Bytes FiringRangeMicroId = new FixedString64Bytes("puredots_firing_range_micro");
        private static readonly FixedString64Bytes ShipyardScenarioId = new FixedString64Bytes("scenario.puredots.shipyard.equip");
        private static readonly FixedString64Bytes ShipyardMicroId = new FixedString64Bytes("puredots_shipyard_equip_micro");
        private static readonly FixedString64Bytes WeaponSpawnerScenarioId = new FixedString64Bytes("scenario.puredots.weapon_spawner.smoke");
        private static readonly FixedString64Bytes TrackingScenarioId = new FixedString64Bytes("scenario.puredots.projectile_tracking.audit");
        private static readonly FixedString64Bytes TrackingMicroId = new FixedString64Bytes("puredots_projectile_tracking_audit_micro");
        private static readonly FixedString64Bytes LifecycleScenarioId = new FixedString64Bytes("scenario.puredots.projectile_lifecycle.audit");
        private static readonly FixedString64Bytes LifecycleMicroId = new FixedString64Bytes("puredots_projectile_lifecycle_micro");
        private static readonly FixedString64Bytes LastStandScenarioId = new FixedString64Bytes("scenario.puredots.last_stand.summoner");
        private static readonly FixedString64Bytes LastStandMicroId = new FixedString64Bytes("puredots_last_stand_summoner_micro");

        protected override void OnCreate()
        {
            RequireForUpdate<ScenarioInfo>();
        }

        protected override void OnUpdate()
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            var scenarioId = scenarioInfo.ScenarioId;

            if (scenarioId.Equals(FiringRangeScenarioId) || scenarioId.Equals(FiringRangeMicroId))
            {
                SeedFiringRange();
            }
            else if (scenarioId.Equals(ShipyardScenarioId) || scenarioId.Equals(ShipyardMicroId))
            {
                SeedShipyardEquip();
            }
            else if (scenarioId.Equals(WeaponSpawnerScenarioId) || scenarioId.Equals(TrackingScenarioId) || scenarioId.Equals(TrackingMicroId))
            {
                SeedWeaponSpawner();
            }
            else if (scenarioId.Equals(LifecycleScenarioId) || scenarioId.Equals(LifecycleMicroId))
            {
                SeedProjectileLifecycleAudit();
            }
            else if (scenarioId.Equals(LastStandScenarioId) || scenarioId.Equals(LastStandMicroId))
            {
                SeedLastStandSummoner();
            }

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

        private void SeedLastStandSummoner()
        {
            var heroPosition = new float3(0f, 0f, 0f);
            var summonerPosition = new float3(0f, 0f, 32f);

            var hero = CreateShooter(
                heroPosition,
                new FixedString64Bytes("weapon.basic.railgun"),
                Entity.Null,
                new FixedString32Bytes("ammo.kinetic"),
                4101u);

            var hero2Position = new float3(-3.5f, 0f, -2f);
            var hero2 = CreateShooter(
                hero2Position,
                new FixedString64Bytes("weapon.basic.railgun"),
                Entity.Null,
                new FixedString32Bytes("ammo.kinetic"),
                4102u);

            AttachCombatantBody(
                hero,
                heroPosition,
                health: 520f,
                radius: 1.1f,
                armorValue: 4.5f,
                armorType: ArmorType.Light,
                resistance: new Resistance
                {
                    Physical = 0.12f,
                    Fire = 0.05f,
                    Cold = 0.04f,
                    Lightning = 0.06f,
                    Poison = 0.08f
                });

            AttachCombatantBody(
                hero2,
                hero2Position,
                health: 500f,
                radius: 1.05f,
                armorValue: 4.2f,
                armorType: ArmorType.Light,
                resistance: new Resistance
                {
                    Physical = 0.1f,
                    Fire = 0.05f,
                    Cold = 0.04f,
                    Lightning = 0.05f,
                    Poison = 0.07f
                });

            var summoner = CreateShooter(
                summonerPosition,
                new FixedString64Bytes("weapon.basic.launcher"),
                hero,
                new FixedString32Bytes("ammo.arc"),
                4201u);

            AttachCombatantBody(
                summoner,
                summonerPosition,
                health: 360f,
                radius: 1.4f,
                armorValue: 3f,
                armorType: ArmorType.Magical,
                resistance: new Resistance
                {
                    Physical = 0.05f,
                    Fire = 0.1f,
                    Cold = 0.05f,
                    Lightning = 0.2f,
                    Poison = 0.15f
                });

            var heroMount = EntityManager.GetComponentData<WeaponMount>(hero);
            heroMount.TargetEntity = summoner;
            EntityManager.SetComponentData(hero, heroMount);

            var hero2Mount = EntityManager.GetComponentData<WeaponMount>(hero2);
            hero2Mount.TargetEntity = summoner;
            EntityManager.SetComponentData(hero2, hero2Mount);

            EntityManager.SetComponentData(hero, new EngagementEnvelope
            {
                MinRange = 0f,
                PreferredRange = 6f,
                MaxRange = 35f,
                HoldRange = 4f
            });

            EntityManager.SetComponentData(hero, new PositioningIntent
            {
                Mode = PositioningMode.Advance,
                Anchor = summoner,
                Offset = float3.zero,
                Priority = 2f
            });

            EntityManager.SetComponentData(hero2, new PositioningIntent
            {
                Mode = PositioningMode.Advance,
                Anchor = summoner,
                Offset = float3.zero,
                Priority = 1.8f
            });

            EntityManager.SetComponentData(hero, new StrafeProfile
            {
                LateralSpeed = 4.5f,
                Jitter = 0.35f,
                ChangeIntervalSec = 0.55f
            });

            EntityManager.SetComponentData(hero2, new StrafeProfile
            {
                LateralSpeed = 4.1f,
                Jitter = 0.4f,
                ChangeIntervalSec = 0.6f
            });

            EntityManager.SetComponentData(hero, new DeflectionProfile
            {
                ReactionSec = 0.08f,
                MinThreatScore = 0.15f,
                MaxThreatScore = 1.0f,
                DodgeBias = 0.45f,
                BlockBias = 0.25f,
                DeflectBias = 0.2f,
                RedirectBias = 0.05f,
                ControlBias = 0.05f,
                MaxActionsPerSecond = 9f,
                CooldownSec = 0.05f
            });

            EntityManager.SetComponentData(hero2, new DeflectionProfile
            {
                ReactionSec = 0.09f,
                MinThreatScore = 0.15f,
                MaxThreatScore = 1.0f,
                DodgeBias = 0.4f,
                BlockBias = 0.3f,
                DeflectBias = 0.2f,
                RedirectBias = 0.05f,
                ControlBias = 0.05f,
                MaxActionsPerSecond = 8f,
                CooldownSec = 0.06f
            });

            EntityManager.SetComponentData(hero, new DeflectionBudget
            {
                Energy = 320f,
                Mana = 10f,
                Focus = 140f,
                Ammo = 20f,
                LastSpendTime = 0f
            });

            EntityManager.SetComponentData(hero2, new DeflectionBudget
            {
                Energy = 300f,
                Mana = 10f,
                Focus = 130f,
                Ammo = 20f,
                LastSpendTime = 0f
            });

            AddFocus(hero, FocusArchetype.Finesse, 140f, 140f, 8f);
            AddFocus(hero2, FocusArchetype.Finesse, 130f, 130f, 7.5f);

            EntityManager.SetComponentData(summoner, new EngagementEnvelope
            {
                MinRange = 6f,
                PreferredRange = 28f,
                MaxRange = 80f,
                HoldRange = 22f
            });

            EntityManager.SetComponentData(summoner, new PositioningIntent
            {
                Mode = PositioningMode.Hold,
                Anchor = hero,
                Offset = float3.zero,
                Priority = 1.4f
            });

            EntityManager.SetComponentData(summoner, new StrafeProfile
            {
                LateralSpeed = 2.6f,
                Jitter = 0.25f,
                ChangeIntervalSec = 0.9f
            });

            EntityManager.SetComponentData(summoner, new DeflectionProfile
            {
                ReactionSec = 0.16f,
                MinThreatScore = 0.2f,
                MaxThreatScore = 1.0f,
                DodgeBias = 0.25f,
                BlockBias = 0.15f,
                DeflectBias = 0.2f,
                RedirectBias = 0.2f,
                ControlBias = 0.2f,
                MaxActionsPerSecond = 5f,
                CooldownSec = 0.12f
            });

            EntityManager.SetComponentData(summoner, new DeflectionBudget
            {
                Energy = 260f,
                Mana = 200f,
                Focus = 90f,
                Ammo = 40f,
                LastSpendTime = 0f
            });

            AddFocus(summoner, FocusArchetype.Arcane, 180f, 180f, 12f);

            var minionCount = 1200;
            var minionBaseRadius = 10f;
            var ringCount = 12;
            var perRing = math.max(1, minionCount / ringCount);
            var ringSpacing = 2.5f;
            for (var i = 0; i < minionCount; i++)
            {
                var ringIndex = i / perRing;
                if (ringIndex >= ringCount)
                {
                    ringIndex = ringCount - 1;
                }

                var ringSlot = i - ringIndex * perRing;
                var ringSlots = ringIndex == ringCount - 1 ? math.max(1, minionCount - ringIndex * perRing) : perRing;
                var angle = (math.PI * 2f) * (ringSlot / (float)ringSlots);
                var radius = minionBaseRadius + ringIndex * ringSpacing;
                var pos = heroPosition + new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);

                var useLauncher = (i % 2) == 0;
                var minionTarget = (i % 3) == 0 ? hero2 : hero;
                var minion = CreateShooter(
                    pos,
                    useLauncher ? new FixedString64Bytes("weapon.basic.launcher") : new FixedString64Bytes("weapon.basic.railgun"),
                    minionTarget,
                    useLauncher ? new FixedString32Bytes("ammo.standard") : new FixedString32Bytes("ammo.kinetic"),
                    (uint)(4300u + i));

                AttachCombatantBody(
                    minion,
                    pos,
                    health: 90f,
                    radius: 0.95f,
                    armorValue: 1.2f,
                    armorType: ArmorType.Light,
                    resistance: new Resistance
                    {
                        Physical = 0.03f,
                        Fire = 0f,
                        Cold = 0f,
                        Lightning = 0.02f,
                        Poison = 0.02f
                    });

                EntityManager.SetComponentData(minion, new EngagementEnvelope
                {
                    MinRange = 2f,
                    PreferredRange = 14f,
                    MaxRange = 40f,
                    HoldRange = 10f
                });

                EntityManager.SetComponentData(minion, new PositioningIntent
                {
                    Mode = PositioningMode.Advance,
                    Anchor = hero,
                    Offset = float3.zero,
                    Priority = 1f
                });

                EntityManager.SetComponentData(minion, new StrafeProfile
                {
                    LateralSpeed = 3.2f,
                    Jitter = 0.4f,
                    ChangeIntervalSec = 0.8f
                });
            }
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

        private void AttachCombatantBody(Entity entity, float3 position, float health, float radius, float armorValue, ArmorType armorType, in Resistance resistance)
        {
            EntityManager.AddComponentData(entity, new Health
            {
                Current = health,
                Max = health,
                RegenRate = 0f,
                LastDamageTick = 0
            });

            EntityManager.AddComponentData(entity, new ArmorValue
            {
                Value = armorValue,
                Type = armorType
            });

            EntityManager.AddComponentData(entity, resistance);

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
        }

        private void AddFocus(Entity entity, FocusArchetype archetype, float current, float max, float regen)
        {
            EntityManager.AddComponentData(entity, new EntityFocus
            {
                CurrentFocus = current,
                MaxFocus = max,
                BaseRegenRate = regen,
                TotalDrainRate = 0f,
                ExhaustionLevel = 0,
                IsInComa = false,
                PrimaryArchetype = archetype,
                LastUpdateTick = 0
            });
        }
    }
}
