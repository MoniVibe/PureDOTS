using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.LowLevel;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Spawns projectiles from WeaponSpawner components (data-only weapon emitters).
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(ProjectilePoolSpawnSystem))]
    public partial struct WeaponSpawnerSystem : ISystem
    {
        private static readonly FixedString32Bytes DefaultAmmoId = new FixedString32Bytes("ammo.standard");
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<PersistentId> _persistentLookup;
        private ComponentLookup<WeaponMagazine> _magazineLookup;
        private ComponentLookup<AmmoStockpile> _stockpileLookup;
        private EntityQuery _missingSpawnBufferQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<WeaponSpawner>();
            state.RequireForUpdate<WeaponCatalog>();
            state.RequireForUpdate<ProjectileCatalog>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _persistentLookup = state.GetComponentLookup<PersistentId>(true);
            _magazineLookup = state.GetComponentLookup<WeaponMagazine>(false);
            _stockpileLookup = state.GetComponentLookup<AmmoStockpile>(false);

            _missingSpawnBufferQuery = SystemAPI.QueryBuilder()
                .WithAll<WeaponSpawner>()
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
            _persistentLookup.Update(ref state);
            _magazineLookup.Update(ref state);
            _stockpileLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var currentTime = timeState.ElapsedTime;
            var currentTick = timeState.Tick;

            foreach (var (spawner, entity) in SystemAPI.Query<RefRW<WeaponSpawner>>().WithEntityAccess())
            {
                if (spawner.ValueRO.IsActive == 0)
                {
                    continue;
                }

                if (!entityManager.HasBuffer<ProjectileSpawnRequest>(entity))
                {
                    continue;
                }

                ref var weaponSpec = ref FindWeaponSpec(weaponCatalog.Catalog, spawner.ValueRO.WeaponId);
                if (UnsafeRef.IsNull(ref weaponSpec))
                {
                    continue;
                }

                ref var projectileSpec = ref FindProjectileSpec(projectileCatalog.Catalog, weaponSpec.ProjectileId);
                if (UnsafeRef.IsNull(ref projectileSpec))
                {
                    continue;
                }

                if (!TryGetMuzzle(entity, out var muzzlePos, out var muzzleForward))
                {
                    continue;
                }

                var fireDirection = ResolveAimDirection(spawner.ValueRO, muzzlePos, muzzleForward);
                if (math.lengthsq(fireDirection) < 0.0001f)
                {
                    continue;
                }

                var hasTarget = spawner.ValueRO.AimMode == WeaponSpawnerAimMode.TargetEntity &&
                                spawner.ValueRO.TargetEntity != Entity.Null;

                if (hasTarget && _transformLookup.HasComponent(spawner.ValueRO.TargetEntity))
                {
                    var targetPos = _transformLookup[spawner.ValueRO.TargetEntity].Position;
                    var distance = math.distance(muzzlePos, targetPos);
                    var maxRange = projectileSpec.Speed > 0f
                        ? projectileSpec.Speed * projectileSpec.Lifetime
                        : projectileSpec.Lifetime * 1000f;

                    if (distance > maxRange)
                    {
                        continue;
                    }
                }

                if (weaponSpec.EnergyCost > 0f && spawner.ValueRO.EnergyReserve < weaponSpec.EnergyCost)
                {
                    continue;
                }

                var fireInterval = weaponSpec.FireRate > 0f ? 1f / weaponSpec.FireRate : 0f;
                if (fireInterval > 0f && currentTime - spawner.ValueRO.LastFireTime < fireInterval)
                {
                    continue;
                }

                if ((ProjectileKind)projectileSpec.Kind == ProjectileKind.Beam)
                {
                    ApplyShotCosts(ref spawner.ValueRW, weaponSpec, currentTime);
                    continue;
                }

                var spawnBuffer = entityManager.GetBuffer<ProjectileSpawnRequest>(entity);
                var burst = math.max(1, weaponSpec.Burst);
                var spreadRad = math.radians(weaponSpec.SpreadDeg);

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

                var ammoPerShot = hasMagazine ? math.max(1, magazine.AmmoPerShot) : 0;
                if (hasMagazine && magazine.Current < ammoPerShot)
                {
                    if (!TryReloadMagazine(entity, ref magazine, ammoPerShot, currentTime))
                    {
                        _magazineLookup[entity] = magazine;
                        continue;
                    }
                }

                spawner.ValueRW.ShotSequence++;
                var shotSequence = spawner.ValueRO.ShotSequence;
                var seed = ComputeShotSeed(entity, currentTick, shotSequence);
                var rng = new Unity.Mathematics.Random(seed == 0u ? 1u : seed);

                var ammoId = spawner.ValueRO.AmmoId.Length > 0
                    ? spawner.ValueRO.AmmoId
                    : (hasMagazine && magazine.AmmoType.Length > 0 ? magazine.AmmoType : DefaultAmmoId);

                for (int i = 0; i < burst; i++)
                {
                    if (hasMagazine)
                    {
                        if (magazine.Current < ammoPerShot && !TryReloadMagazine(entity, ref magazine, ammoPerShot, currentTime))
                        {
                            break;
                        }

                        magazine.Current = math.max(0, magazine.Current - ammoPerShot);
                    }

                    var direction = ApplySpread(fireDirection, spreadRad, ref rng);
                    spawnBuffer.Add(new ProjectileSpawnRequest
                    {
                        ProjectileId = weaponSpec.ProjectileId,
                        AmmoId = ammoId,
                        SpawnPosition = muzzlePos,
                        SpawnDirection = direction,
                        SourceEntity = entity,
                        TargetEntity = spawner.ValueRO.TargetEntity,
                        ShotSeed = rng.NextUInt(),
                        ShotSequence = shotSequence,
                        PelletIndex = i
                    });
                }

                ApplyShotCosts(ref spawner.ValueRW, weaponSpec, currentTime);

                if (hasMagazine)
                {
                    _magazineLookup[entity] = magazine;
                }
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

        private float3 ResolveAimDirection(in WeaponSpawner spawner, float3 muzzlePos, float3 muzzleForward)
        {
            switch (spawner.AimMode)
            {
                case WeaponSpawnerAimMode.TargetEntity:
                    if (spawner.TargetEntity != Entity.Null && _transformLookup.HasComponent(spawner.TargetEntity))
                    {
                        var targetPos = _transformLookup[spawner.TargetEntity].Position;
                        return math.normalizesafe(targetPos - muzzlePos, muzzleForward);
                    }
                    break;

                case WeaponSpawnerAimMode.TargetPosition:
                    if (math.lengthsq(spawner.TargetPosition) > 1e-4f)
                    {
                        return math.normalizesafe(spawner.TargetPosition - muzzlePos, muzzleForward);
                    }
                    break;

                case WeaponSpawnerAimMode.FixedDirection:
                    if (math.lengthsq(spawner.FireDirection) > 1e-4f)
                    {
                        return math.normalizesafe(spawner.FireDirection, muzzleForward);
                    }
                    break;
            }

            return math.normalizesafe(muzzleForward, new float3(0f, 0f, 1f));
        }

        private static void ApplyShotCosts(ref WeaponSpawner spawner, in WeaponSpec spec, float currentTime)
        {
            spawner.LastFireTime = currentTime;
            spawner.EnergyReserve = math.max(0f, spawner.EnergyReserve - spec.EnergyCost);
            spawner.HeatLevel = math.saturate(spawner.HeatLevel + spec.HeatCost);
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
            FixedString64Bytes projectileId)
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
