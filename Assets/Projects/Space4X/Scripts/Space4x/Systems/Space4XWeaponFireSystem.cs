using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Space4X.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Systems
{
    /// <summary>
    /// Handles weapon firing logic: rate/heat/energy budgets, lead calculation, projectile spawn requests.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct Space4XWeaponFireSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WeaponMount>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<WeaponCatalog>(out var weaponCatalog) ||
                !SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog))
            {
                return;
            }

            var currentTime = timeState.ElapsedTime;
            var deltaTime = timeState.FixedDeltaTime;

            var job = new WeaponFireJob
            {
                WeaponCatalog = weaponCatalog.Catalog,
                ProjectileCatalog = projectileCatalog.Catalog,
                CurrentTime = currentTime,
                DeltaTime = deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct WeaponFireJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<WeaponCatalogBlob> WeaponCatalog;
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public float CurrentTime;
            public float DeltaTime;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                ref WeaponMount weaponMount,
                in LocalTransform transform,
                in TurretState turretState,
                DynamicBuffer<ProjectileSpawnRequest> spawnRequests)
            {
                // Find weapon spec
                if (!TryFindWeaponSpec(WeaponCatalog, weaponMount.WeaponId, out var weaponSpec))
                {
                    return;
                }

                // Check if we have a valid target
                if (weaponMount.TargetEntity == Entity.Null)
                {
                    weaponMount.IsFiring = false;
                    return;
                }

                // Cooldown heat dissipation
                var heatDecayRate = 0.5f; // Heat decays at 50% per second
                weaponMount.HeatLevel = math.max(0f, weaponMount.HeatLevel - heatDecayRate * DeltaTime);

                // Check if weapon can fire (heat limit, energy, cooldown)
                var timeSinceLastFire = CurrentTime - weaponMount.LastFireTime;
                var fireInterval = 1f / weaponSpec.FireRate;
                var canFire = timeSinceLastFire >= fireInterval &&
                              weaponMount.HeatLevel < 0.95f &&
                              weaponMount.EnergyReserve >= weaponSpec.EnergyCost;

                if (!canFire)
                {
                    weaponMount.IsFiring = false;
                    return;
                }

                // Calculate lead position for moving targets
                var targetPosition = CalculateLeadPosition(
                    turretState.MuzzlePosition,
                    weaponMount.TargetPosition,
                    weaponSpec,
                    ProjectileCatalog);

                // Fire weapon
                weaponMount.LastFireTime = CurrentTime;
                weaponMount.HeatLevel = math.min(1f, weaponMount.HeatLevel + weaponSpec.HeatCost);
                weaponMount.EnergyReserve = math.max(0f, weaponMount.EnergyReserve - weaponSpec.EnergyCost);
                weaponMount.IsFiring = true;

                // Queue projectile spawn
                spawnRequests.Add(new ProjectileSpawnRequest
                {
                    ProjectileId = weaponSpec.ProjectileId,
                    SpawnPosition = turretState.MuzzlePosition,
                    SpawnDirection = math.normalize(targetPosition - turretState.MuzzlePosition),
                    SourceEntity = entity,
                    TargetEntity = weaponMount.TargetEntity
                });
            }

            private bool TryFindWeaponSpec(
                BlobAssetReference<WeaponCatalogBlob> catalog,
                FixedString64Bytes weaponId,
                out WeaponSpec spec)
            {
                spec = default;
                if (!catalog.IsCreated)
                {
                    return false;
                }

                var weapons = catalog.Value.Weapons;
                for (int i = 0; i < weapons.Length; i++)
                {
                    if (weapons[i].Id.Equals(weaponId))
                    {
                        spec = weapons[i];
                        return true;
                    }
                }

                return false;
            }

            private float3 CalculateLeadPosition(
                float3 muzzlePos,
                float3 targetPos,
                WeaponSpec weaponSpec,
                BlobAssetReference<ProjectileCatalogBlob> projectileCatalog)
            {
                // Simple lead calculation: assume target moves at constant velocity
                // For now, return target position (no lead)
                // TODO: Use target entity's velocity from VesselMovement or similar
                return targetPos;
            }
        }
    }
}

