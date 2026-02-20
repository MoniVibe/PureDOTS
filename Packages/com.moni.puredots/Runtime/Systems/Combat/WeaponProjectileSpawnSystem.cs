using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Runtime.LowLevel;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Spawns projectile entities from WeaponMount data.
    /// Data-only launcher/railgun behavior with simple cooldown, energy, and heat gating.
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(ProjectilePoolSpawnSystem))]
    public partial struct WeaponProjectileSpawnSystem : ISystem
    {
        private static readonly FixedString32Bytes DefaultAmmoId = new FixedString32Bytes("ammo.standard");
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<TurretState> _turretLookup;
        private ComponentLookup<PersistentId> _persistentLookup;
        private ComponentLookup<WeaponMagazine> _magazineLookup;
        private ComponentLookup<AmmoStockpile> _stockpileLookup;
        private EntityQuery _missingSpawnBufferQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<WeaponMount>();
            state.RequireForUpdate<WeaponCatalog>();
            state.RequireForUpdate<ProjectileCatalog>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _turretLookup = state.GetComponentLookup<TurretState>(true);
            _persistentLookup = state.GetComponentLookup<PersistentId>(true);
            _magazineLookup = state.GetComponentLookup<WeaponMagazine>(false);
            _stockpileLookup = state.GetComponentLookup<AmmoStockpile>(false);

            _missingSpawnBufferQuery = SystemAPI.QueryBuilder()
                .WithAll<WeaponMount>()
                .WithNone<ProjectileSpawnRequest>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<WeaponCatalog>(out var weaponCatalog) ||
                !SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog) ||
                !weaponCatalog.Catalog.IsCreated || !projectileCatalog.Catalog.IsCreated)
            {
                return;
            }

            EnsureSpawnBuffers(ref state);

            _transformLookup.Update(ref state);
            _turretLookup.Update(ref state);
            _persistentLookup.Update(ref state);
            _magazineLookup.Update(ref state);
            _stockpileLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var currentTime = timeState.ElapsedTime;
            var currentTick = timeState.Tick;

            foreach (var (weaponMount, entity) in SystemAPI.Query<RefRW<WeaponMount>>().WithEntityAccess())
            {
                if (!entityManager.HasBuffer<ProjectileSpawnRequest>(entity))
                {
                    continue;
                }

                ref var weaponSpec = ref FindWeaponSpec(weaponCatalog.Catalog, weaponMount.ValueRO.WeaponId);
                if (UnsafeRef.IsNull(ref weaponSpec))
                {
                    weaponMount.ValueRW.IsFiring = false;
                    continue;
                }

                ref var projectileSpec = ref FindProjectileSpec(projectileCatalog.Catalog, weaponSpec.ProjectileId);
                if (UnsafeRef.IsNull(ref projectileSpec))
                {
                    weaponMount.ValueRW.IsFiring = false;
                    continue;
                }

                var behaviorArchetype = WeaponBehaviorProfiles.ResolveDefaultArchetype(
                    (WeaponClass)weaponSpec.Class,
                    (ProjectileKind)projectileSpec.Kind);
                var behaviorProfile = WeaponBehaviorProfiles.Resolve(behaviorArchetype);

                if (!TryGetMuzzle(entity, out var muzzlePos, out var muzzleForward))
                {
                    weaponMount.ValueRW.IsFiring = false;
                    continue;
                }

                var hasTarget = TryResolveTarget(weaponMount.ValueRO, out var targetPos);
                if (!hasTarget && !weaponMount.ValueRO.IsFiring)
                {
                    weaponMount.ValueRW.IsFiring = false;
                    continue;
                }

                var fireDirection = hasTarget
                    ? math.normalizesafe(targetPos - muzzlePos, muzzleForward)
                    : math.normalizesafe(muzzleForward, new float3(0f, 0f, 1f));

                if (math.lengthsq(fireDirection) < 0.0001f)
                {
                    weaponMount.ValueRW.IsFiring = false;
                    continue;
                }

                if (hasTarget)
                {
                    var distance = math.distance(muzzlePos, targetPos);
                    var maxRange = projectileSpec.Speed > 0f
                        ? projectileSpec.Speed * projectileSpec.Lifetime
                        : projectileSpec.Lifetime * 1000f;

                    if (distance > maxRange)
                    {
                        weaponMount.ValueRW.IsFiring = false;
                        continue;
                    }
                }

                if (weaponSpec.EnergyCost > 0f && weaponMount.ValueRO.EnergyReserve < weaponSpec.EnergyCost)
                {
                    weaponMount.ValueRW.IsFiring = false;
                    continue;
                }

                var hasMagazine = _magazineLookup.HasComponent(entity);
                WeaponMagazine magazine = default;
                if (hasMagazine)
                {
                    magazine = _magazineLookup[entity];
                    if (magazine.Capacity <= 0 || magazine.AmmoPerShot <= 0)
                    {
                        hasMagazine = false;
                    }
                }

                weaponMount.ValueRW.IsFiring = true;

                var fireInterval = weaponSpec.FireRate > 0f ? 1f / weaponSpec.FireRate : 0f;
                if (fireInterval > 0f && currentTime - weaponMount.ValueRO.LastFireTime < fireInterval)
                {
                    continue;
                }

                if ((ProjectileKind)projectileSpec.Kind == ProjectileKind.Beam)
                {
                    ApplyShotCosts(ref weaponMount.ValueRW, weaponSpec, behaviorProfile, currentTime);
                    continue;
                }

                var spawnBuffer = entityManager.GetBuffer<ProjectileSpawnRequest>(entity);
                var burst = math.max(1, weaponSpec.Burst);
                var spreadRad = math.radians(weaponSpec.SpreadDeg);

                var ammoPerShot = hasMagazine
                    ? WeaponBehaviorProfiles.ResolveAmmoPerShot(math.max(1, magazine.AmmoPerShot), behaviorProfile)
                    : 0;

                if (hasMagazine && ammoPerShot <= 0)
                {
                    hasMagazine = false;
                }

                var ammoId = hasMagazine && magazine.AmmoType.Length > 0
                    ? magazine.AmmoType
                    : DefaultAmmoId;

                var shotsFired = 0;
                var ammoSpent = 0;
                var outOfAmmo = false;

                if (hasMagazine && magazine.Current < ammoPerShot)
                {
                    if (!TryReloadMagazine(entity, ref magazine, ammoPerShot, currentTime))
                    {
                        weaponMount.ValueRW.IsFiring = false;
                        _magazineLookup[entity] = magazine;
                        outOfAmmo = true;
                        RecordAmmoMetrics(entityManager, ammoId, shotsFired, ammoSpent, outOfAmmo);
                        EmitOutOfAmmoInterrupt(entityManager, entity, ammoId, currentTick, outOfAmmo);
                        continue;
                    }
                }

                weaponMount.ValueRW.ShotSequence++;
                var shotSequence = weaponMount.ValueRO.ShotSequence;
                var seed = ComputeShotSeed(entity, currentTick, shotSequence);
                var rng = new Unity.Mathematics.Random(seed == 0u ? 1u : seed);

                for (int i = 0; i < burst; i++)
                {
                    if (hasMagazine)
                    {
                        if (magazine.Current < ammoPerShot && !TryReloadMagazine(entity, ref magazine, ammoPerShot, currentTime))
                        {
                            outOfAmmo = true;
                            break;
                        }

                        magazine.Current = math.max(0, magazine.Current - ammoPerShot);
                        ammoSpent += ammoPerShot;
                    }

                    var direction = ApplySpread(fireDirection, spreadRad, ref rng);
                    spawnBuffer.Add(new ProjectileSpawnRequest
                    {
                        ProjectileId = weaponSpec.ProjectileId,
                        AmmoId = ammoId,
                        SpawnPosition = muzzlePos,
                        SpawnDirection = direction,
                        SourceEntity = entity,
                        TargetEntity = weaponMount.ValueRO.TargetEntity,
                        ShotSeed = rng.NextUInt(),
                        ShotSequence = shotSequence,
                        PelletIndex = i
                    });
                    shotsFired++;
                }

                ApplyShotCosts(ref weaponMount.ValueRW, weaponSpec, behaviorProfile, currentTime);

                if (hasMagazine)
                {
                    _magazineLookup[entity] = magazine;
                }

                RecordAmmoMetrics(entityManager, ammoId, shotsFired, ammoSpent, outOfAmmo);
                EmitOutOfAmmoInterrupt(entityManager, entity, ammoId, currentTick, outOfAmmo);
            }
        }

        private void EnsureSpawnBuffers(ref SystemState state)
        {
            if (_missingSpawnBufferQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            using var entities = _missingSpawnBufferQuery.ToEntityArray(Allocator.Temp);
            var entityManager = state.EntityManager;
            for (int i = 0; i < entities.Length; i++)
            {
                if (!entityManager.HasBuffer<ProjectileSpawnRequest>(entities[i]))
                {
                    entityManager.AddBuffer<ProjectileSpawnRequest>(entities[i]);
                }
            }
        }

        private bool TryGetMuzzle(Entity entity, out float3 muzzlePos, out float3 muzzleForward)
        {
            if (_turretLookup.HasComponent(entity))
            {
                var turret = _turretLookup[entity];
                muzzlePos = turret.MuzzlePosition;
                muzzleForward = math.normalizesafe(turret.MuzzleForward, new float3(0f, 0f, 1f));
                return true;
            }

            if (_transformLookup.HasComponent(entity))
            {
                var transform = _transformLookup[entity];
                muzzlePos = transform.Position;
                muzzleForward = math.forward(transform.Rotation);
                return true;
            }

            muzzlePos = default;
            muzzleForward = default;
            return false;
        }

        private bool TryResolveTarget(in WeaponMount weaponMount, out float3 targetPos)
        {
            if (weaponMount.TargetEntity != Entity.Null && _transformLookup.HasComponent(weaponMount.TargetEntity))
            {
                targetPos = _transformLookup[weaponMount.TargetEntity].Position;
                return true;
            }

            if (math.lengthsq(weaponMount.TargetPosition) > 0.0001f)
            {
                targetPos = weaponMount.TargetPosition;
                return true;
            }

            targetPos = default;
            return false;
        }

        private static void ApplyShotCosts(
            ref WeaponMount weaponMount,
            in WeaponSpec spec,
            in WeaponBehaviorProfile behaviorProfile,
            float currentTime)
        {
            weaponMount.LastFireTime = currentTime;
            weaponMount.EnergyReserve = math.max(0f, weaponMount.EnergyReserve - spec.EnergyCost);
            var resolvedHeatCost = WeaponBehaviorProfiles.ResolveHeatCost(spec.HeatCost, behaviorProfile);
            weaponMount.HeatLevel = math.saturate(weaponMount.HeatLevel + resolvedHeatCost);
        }

        private static float3 ApplySpread(float3 direction, float spreadRad, ref Unity.Mathematics.Random rng)
        {
            if (spreadRad <= 0f)
            {
                return direction;
            }

            var yaw = rng.NextFloat(-spreadRad, spreadRad);
            var pitch = rng.NextFloat(-spreadRad, spreadRad);
            var rot = quaternion.Euler(pitch, yaw, 0f);
            return math.normalizesafe(math.mul(rot, direction), direction);
        }

        private static void RecordAmmoMetrics(
            EntityManager entityManager,
            FixedString32Bytes ammoId,
            int shotsFired,
            int ammoSpent,
            bool outOfAmmo)
        {
            if (shotsFired > 0)
            {
                ScenarioMetricsUtility.AddMetric(entityManager, "ammo.shots_total", shotsFired);
                if (ammoId.Length > 0)
                {
                    var shotsKey = new FixedString64Bytes("ammo.shots.");
                    shotsKey.Append(ammoId);
                    ScenarioMetricsUtility.AddMetric(entityManager, shotsKey, shotsFired);
                }
            }

            if (ammoSpent > 0)
            {
                ScenarioMetricsUtility.AddMetric(entityManager, "ammo.used_total", ammoSpent);
                if (ammoId.Length > 0)
                {
                    var usedKey = new FixedString64Bytes("ammo.used.");
                    usedKey.Append(ammoId);
                    ScenarioMetricsUtility.AddMetric(entityManager, usedKey, ammoSpent);
                }
            }

            if (outOfAmmo)
            {
                ScenarioMetricsUtility.AddMetric(entityManager, "ammo.out_of_ammo_total", 1.0);
                if (ammoId.Length > 0)
                {
                    var outKey = new FixedString64Bytes("ammo.out_of_ammo.");
                    outKey.Append(ammoId);
                    ScenarioMetricsUtility.AddMetric(entityManager, outKey, 1.0);
                }
            }
        }

        private static void EmitOutOfAmmoInterrupt(
            EntityManager entityManager,
            Entity entity,
            FixedString32Bytes ammoId,
            uint currentTick,
            bool outOfAmmo)
        {
            if (!outOfAmmo || !entityManager.HasBuffer<Interrupt>(entity))
            {
                return;
            }

            var interruptBuffer = entityManager.GetBuffer<Interrupt>(entity);
            for (int i = 0; i < interruptBuffer.Length; i++)
            {
                var existing = interruptBuffer[i];
                if (existing.Type == InterruptType.OutOfAmmo && existing.IsProcessed == 0)
                {
                    return;
                }
            }
            InterruptUtils.Emit(
                ref interruptBuffer,
                InterruptType.OutOfAmmo,
                InterruptPriority.High,
                entity,
                currentTick,
                payloadId: ammoId);
        }

        private bool TryReloadMagazine(Entity entity, ref WeaponMagazine magazine, int ammoPerShot, float currentTime)
        {
            if (magazine.ReloadSec <= 0f)
            {
                return false;
            }

            if (currentTime - magazine.LastReloadTime < magazine.ReloadSec)
            {
                return false;
            }

            var needed = math.max(0, magazine.Capacity - magazine.Current);
            if (needed == 0)
            {
                magazine.LastReloadTime = currentTime;
                return magazine.Current >= ammoPerShot;
            }

            if (_stockpileLookup.HasComponent(entity))
            {
                var stockpile = _stockpileLookup[entity];
                if (!stockpile.AmmoType.Equals(magazine.AmmoType) || stockpile.Current <= 0)
                {
                    return false;
                }

                var take = math.min(needed, stockpile.Current);
                stockpile.Current = math.max(0, stockpile.Current - take);
                magazine.Current = math.min(magazine.Capacity, magazine.Current + take);
                _stockpileLookup[entity] = stockpile;
            }
            else
            {
                magazine.Current = magazine.Capacity;
            }

            magazine.LastReloadTime = currentTime;
            return magazine.Current >= ammoPerShot;
        }

        private uint ComputeShotSeed(Entity entity, uint tick, int shotSequence)
        {
            var persistent = _persistentLookup.HasComponent(entity)
                ? _persistentLookup[entity].Value
                : (uint)entity.Index;

            var seed = 0x9E3779B9u;
            seed ^= persistent + 0x85ebca6bu;
            seed ^= tick * 0xC2B2AE35u;
            seed ^= (uint)shotSequence * 0x27d4eb2du;
            seed ^= (uint)entity.Version * 0x165667b1u;
            return seed == 0u ? 1u : seed;
        }

        private static ref WeaponSpec FindWeaponSpec(
            BlobAssetReference<WeaponCatalogBlob> catalog,
            FixedString64Bytes weaponId)
        {
            if (!catalog.IsCreated)
            {
                return ref UnsafeRef.Null<WeaponSpec>();
            }

            ref var weapons = ref catalog.Value.Weapons;
            for (int i = 0; i < weapons.Length; i++)
            {
                ref var spec = ref weapons[i];
                if (spec.Id.Equals(weaponId))
                {
                    return ref spec;
                }
            }

            return ref UnsafeRef.Null<WeaponSpec>();
        }

        private static ref ProjectileSpec FindProjectileSpec(
            BlobAssetReference<ProjectileCatalogBlob> catalog,
            FixedString32Bytes projectileId)
        {
            if (!catalog.IsCreated)
            {
                return ref UnsafeRef.Null<ProjectileSpec>();
            }

            ref var projectiles = ref catalog.Value.Projectiles;
            for (int i = 0; i < projectiles.Length; i++)
            {
                ref var spec = ref projectiles[i];
                if (spec.Id.Equals(projectileId))
                {
                    return ref spec;
                }
            }

            return ref UnsafeRef.Null<ProjectileSpec>();
        }
    }
}
