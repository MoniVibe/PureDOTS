using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using PureDOTS.Systems;
using PureDOTS.Rendering;
using Space4X.Rendering;
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

                    // Create projectile entity with 3D-aware rotation
                    var projectileEntity = ECB.CreateEntity(entityInQueryIndex);
                    OrientationHelpers.LookRotationSafe3D(request.SpawnDirection, OrientationHelpers.WorldUp, out quaternion spawnRotation);
                    ECB.AddComponent(entityInQueryIndex, projectileEntity, LocalTransform.FromPositionRotation(
                        request.SpawnPosition,
                        spawnRotation));

                    // Add SpaceMovementTag for full 6DoF movement
                    ECB.AddComponent<SpaceMovementTag>(entityInQueryIndex, projectileEntity);

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

                    ECB.AddComponent<ProjectileTag>(entityInQueryIndex, projectileEntity);
                    ECB.AddComponent(entityInQueryIndex, projectileEntity, new ProjectileVisual
                    {
                        Width = 0f,
                        Length = 0f,
                        Color = float4.zero,
                        Style = 0
                    });

                    ECB.AddComponent(entityInQueryIndex, projectileEntity, new RenderKey
                    {
                        ArchetypeId = Space4XRenderKeys.Projectile,
                        LOD = 0
                    });
                    ECB.AddComponent(entityInQueryIndex, projectileEntity, new RenderFlags
                    {
                        Visible = 1,
                        ShadowCaster = 0,
                        HighlightMask = 0
                    });
                    ECB.AddComponent(entityInQueryIndex, projectileEntity, new RenderSemanticKey
                    {
                        Value = Space4XRenderKeys.Projectile
                    });
                    ECB.AddComponent(entityInQueryIndex, projectileEntity, new RenderVariantKey { Value = 0 });
                    ECB.AddComponent<RenderThemeOverride>(entityInQueryIndex, projectileEntity);
                    ECB.SetComponentEnabled<RenderThemeOverride>(projectileEntity, false);
                    ECB.AddComponent<MeshPresenter>(entityInQueryIndex, projectileEntity);
                    ECB.SetComponentEnabled<MeshPresenter>(projectileEntity, false);
                    ECB.AddComponent<SpritePresenter>(entityInQueryIndex, projectileEntity);
                    ECB.SetComponentEnabled<SpritePresenter>(projectileEntity, false);
                    ECB.AddComponent<DebugPresenter>(entityInQueryIndex, projectileEntity);
                    ECB.SetComponentEnabled<DebugPresenter>(projectileEntity, false);
                    ECB.AddComponent<TracerPresenter>(entityInQueryIndex, projectileEntity);
                    ECB.SetComponentEnabled<TracerPresenter>(projectileEntity, false);
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
