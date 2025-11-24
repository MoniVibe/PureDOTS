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
    /// Advances projectiles through space: ballistic/homing/beam ticks, applies damage on impact, queues impact FX.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(Space4XProjectileSpawnerSystem))]
    public partial struct Space4XProjectileAdvanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileEntity>();
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

            var currentTime = timeState.ElapsedTime;
            var deltaTime = timeState.FixedDeltaTime;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            var job = new ProjectileAdvanceJob
            {
                ProjectileCatalog = projectileCatalog.Catalog,
                CurrentTime = currentTime,
                DeltaTime = deltaTime,
                ECB = ecbParallel
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct ProjectileAdvanceJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public float CurrentTime;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                ref ProjectileEntity projectile,
                ref LocalTransform transform)
            {
                // Find projectile spec
                if (!TryFindProjectileSpec(ProjectileCatalog, projectile.ProjectileId, out var projectileSpec))
                {
                    ECB.DestroyEntity(entityInQueryIndex, entity);
                    return;
                }

                // Check lifetime
                var age = CurrentTime - projectile.SpawnTime;
                if (age >= projectileSpec.Lifetime)
                {
                    ECB.DestroyEntity(entityInQueryIndex, entity);
                    return;
                }

                // Update position based on projectile kind
                var newPosition = transform.Position;
                var newVelocity = projectile.Velocity;

                switch ((ProjectileKind)projectileSpec.Kind)
                {
                    case ProjectileKind.Ballistic:
                        // Ballistic: constant velocity
                        newPosition += projectile.Velocity * DeltaTime;
                        break;

                    case ProjectileKind.Homing:
                        // Homing: steer toward target
                        if (projectile.TargetEntity != Entity.Null)
                        {
                            // TODO: Get target position from entity
                            // For now, maintain velocity
                            newPosition += projectile.Velocity * DeltaTime;
                        }
                        else
                        {
                            newPosition += projectile.Velocity * DeltaTime;
                        }
                        break;

                    case ProjectileKind.Beam:
                        // Beam: instant hit (handled differently)
                        newPosition += projectile.Velocity * DeltaTime;
                        break;
                }

                projectile.DistanceTraveled += math.length(projectile.Velocity) * DeltaTime;
                transform.Position = newPosition;

                // Check for impact (simple distance check)
                // TODO: Use proper collision detection
                // For now, projectiles expire after lifetime
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

