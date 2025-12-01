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

            var currentTime = timeState.Time;
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

                // Generate shot sequence for deterministic seeding
                weaponMount.ShotSequence += 1;

                // Get persistent ID for deterministic seeding
                var shooterId = 0u;
                if (SystemAPI.HasComponent<PersistentId>(entity))
                {
                    shooterId = SystemAPI.GetComponent<PersistentId>(entity).Value;
                }

                // Create deterministic seed for this shot
                var worldSeed = 0x9E3779B9u; // Same seed as in your example
                var weaponSeed = math.hash(new uint2(
                    math.asuint(weaponSpec.Id.GetHashCode()),
                    (uint)weaponMount.ShotSequence));

                var shotSeed = MakeShotSeed(worldSeed, shooterId, weaponSeed, CurrentTick);

                // Calculate base direction
                var baseDirection = math.normalize(targetPosition - turretState.MuzzlePosition);

                // Generate spread pattern if weapon has spread
                if (weaponSpec.SpreadDeg > 0f)
                {
                    // Generate pellets in spread pattern
                    var pelletCount = weaponSpec.Burst; // Use burst count as pellet count for shotguns
                    if (pelletCount <= 1) pelletCount = 8; // Default to 8 pellets for spread weapons

                    // Generate spread directions with deterministic seeding
                    GenerateSpreadDirections(
                        shotSeed,
                        pelletCount,
                        weaponSpec.SpreadDeg,
                        baseDirection,
                        spawnRequests,
                        weaponSpec.ProjectileId,
                        turretState.MuzzlePosition,
                        entity,
                        weaponMount.TargetEntity,
                        weaponMount.ShotSequence);
                }
                else
                {
                    // Single projectile (no spread) - still use deterministic seed
                    spawnRequests.Add(new ProjectileSpawnRequest
                    {
                        ProjectileId = weaponSpec.ProjectileId,
                        SpawnPosition = turretState.MuzzlePosition,
                        SpawnDirection = baseDirection,
                        SourceEntity = entity,
                        TargetEntity = weaponMount.TargetEntity,
                        ShotSeed = shotSeed, // Pass deterministic seed
                        ShotSequence = weaponMount.ShotSequence,
                        PelletIndex = 0 // Single projectile is pellet 0
                    });
                }
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

        /// <summary>
        /// Creates a deterministic seed for shot randomness based on game state.
        /// Same inputs produce same randomness; different inputs produce different randomness.
        /// </summary>
        private static uint MakeShotSeed(uint worldSeed, uint shooterId, uint weaponSeed, uint tick)
        {
            return math.hash(new uint4(
                worldSeed ^ shooterId,
                weaponSeed,
                tick,
                0x9E3779B9u)); // golden ratio constant for mixing
        }

        /// <summary>
        /// Generates uniformly distributed points on a unit disk for spread patterns.
        /// </summary>
        private static float2 SampleUnitDisk(ref Random rng)
        {
            float2 u = rng.NextFloat2();
            float r = math.sqrt(u.x);
            float theta = 2f * math.PI * u.y;
            return r * new float2(math.cos(theta), math.sin(theta));
        }

        /// <summary>
        /// Generates spread directions for multiple projectiles using deterministic seeding.
        /// </summary>
        private static void GenerateSpreadDirections(
            uint shotSeed,
            int pelletCount,
            float spreadDeg,
            float3 baseDirection,
            DynamicBuffer<ProjectileSpawnRequest> spawnRequests,
            FixedString64Bytes projectileId,
            float3 muzzlePosition,
            Entity sourceEntity,
            Entity targetEntity,
            int shotSequence)
        {
            var rng = new Random(math.max(1u, shotSeed));

            // Create orthonormal basis for spread plane
            var up = math.up();
            var right = math.normalize(math.cross(up, baseDirection));
            if (math.all(math.isnan(right)) || math.lengthsq(right) < 1e-6f)
            {
                // Base direction is aligned with up, use forward as alternative
                right = math.normalize(math.cross(math.forward(), baseDirection));
            }
            var forward = math.normalize(math.cross(right, baseDirection));

            // Convert spread angle to radians and create spread scale
            var spreadRad = math.radians(spreadDeg);
            var spreadScale = math.tan(spreadRad * 0.5f); // Scale factor for unit disk

            for (int i = 0; i < pelletCount; i++)
            {
                // Sample uniform point on unit disk
                var diskPoint = SampleUnitDisk(ref rng);

                // Scale by spread angle and map to direction offsets
                var yawOffset = diskPoint.x * spreadScale;
                var pitchOffset = diskPoint.y * spreadScale;

                // Create rotation quaternion for spread
                var spreadRotation = quaternion.Euler(pitchOffset, yawOffset, 0f);

                // Apply spread to base direction
                var spreadDirection = math.mul(spreadRotation, baseDirection);
                spreadDirection = math.normalize(spreadDirection);

                // Queue projectile spawn with deterministic seed info
                spawnRequests.Add(new ProjectileSpawnRequest
                {
                    ProjectileId = projectileId,
                    SpawnPosition = muzzlePosition,
                    SpawnDirection = spreadDirection,
                    SourceEntity = sourceEntity,
                    TargetEntity = targetEntity,
                    ShotSeed = shotSeed,
                    ShotSequence = shotSequence,
                    PelletIndex = i
                });
            }
        }
    }
}

