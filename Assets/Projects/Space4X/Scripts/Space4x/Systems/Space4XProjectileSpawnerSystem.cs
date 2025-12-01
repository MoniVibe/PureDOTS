using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Systems
{
    /// <summary>
    /// Spawns projectile entities from spawn requests queued by weapon fire system.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(Space4XWeaponFireSystem))]
    public partial struct Space4XProjectileSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileSpawnRequest>();
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

            if (!SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog))
            {
                return;
            }

            var currentTime = timeState.Time;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            var job = new ProjectileSpawnJob
            {
                ProjectileCatalog = projectileCatalog.Catalog,
                CurrentTime = currentTime,
                ECB = ecbParallel
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct ProjectileSpawnJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public float CurrentTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                DynamicBuffer<ProjectileSpawnRequest> spawnRequests)
            {
                for (int i = 0; i < spawnRequests.Length; i++)
                {
                    var request = spawnRequests[i];

                    // Find projectile spec
                    if (!TryFindProjectileSpec(ProjectileCatalog, request.ProjectileId, out var projectileSpec))
                    {
                        continue;
                    }

                    // Create projectile entity
                    var projectileEntity = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, projectileEntity, LocalTransform.FromPositionRotation(
                        request.SpawnPosition,
                        quaternion.LookRotationSafe(request.SpawnDirection, math.up())));

                    // Add projectile component
                    var velocity = request.SpawnDirection * projectileSpec.Speed;

                    ECB.AddComponent(entityInQueryIndex, projectileEntity, new ProjectileEntity
                    {
                        ProjectileId = request.ProjectileId,
                        SourceEntity = request.SourceEntity,
                        TargetEntity = request.TargetEntity,
                        Velocity = velocity,
                        PrevPos = request.SpawnPosition,
                        SpawnTime = CurrentTime,
                        DistanceTraveled = 0f,
                        HitsLeft = math.max(0f, projectileSpec.Pierce),
                        Age = 0f,
                        Seed = request.ShotSeed, // Use deterministic shot seed
                        ShotSequence = request.ShotSequence,
                        PelletIndex = request.PelletIndex
                    });

                    // Add hit results buffer for collision system
                    ECB.AddBuffer<ProjectileHitResult>(entityInQueryIndex, projectileEntity);
                }

                // Clear spawn requests
                spawnRequests.Clear();
            }

            private bool TryFindProjectileSpec(
                BlobAssetReference<ProjectileCatalogBlob> catalog,
                FixedString64Bytes projectileId,
                out ProjectileSpec spec)
            {
                spec = default;
                if (!catalog.IsCreated)
                {
                    return false;
                }

                var projectiles = catalog.Value.Projectiles;
                for (int i = 0; i < projectiles.Length; i++)
                {
                    if (projectiles[i].Id.Equals(projectileId))
                    {
                        spec = projectiles[i];
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

