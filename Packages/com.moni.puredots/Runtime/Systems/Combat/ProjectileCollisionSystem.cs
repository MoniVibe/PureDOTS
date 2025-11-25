using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Performs continuous collision detection for projectiles using raycast/spherecast from PrevPos→Pos.
    /// Runs in CombatSystemGroup after projectile advance systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(Space4X.Systems.Space4XProjectileAdvanceSystem))]
    public partial struct ProjectileCollisionSystem : ISystem
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
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorld))
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog))
            {
                return;
            }

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Get presentation request hub for impact FX
            var hasPresentationHub = SystemAPI.TryGetSingletonEntity<PresentationRequestHub>(out var hubEntity);
            var impactFxBufferLookup = state.GetBufferLookup<PlayEffectRequest>();

            var job = new ProjectileCollisionJob
            {
                PhysicsWorld = physicsWorld,
                ProjectileCatalog = projectileCatalog.Catalog,
                Ecb = ecb,
                HasPresentationHub = hasPresentationHub,
                HubEntity = hasPresentationHub ? hubEntity : Entity.Null,
                ImpactFxBuffers = impactFxBufferLookup
            };

            impactFxBufferLookup.Update(ref state);
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProjectileCollisionJob : IJobEntity
        {
            [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public bool HasPresentationHub;
            public Entity HubEntity;
            [NativeDisableParallelForRestriction] public BufferLookup<PlayEffectRequest> ImpactFxBuffers;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity entity,
                ref ProjectileEntity projectile,
                in LocalTransform transform,
                DynamicBuffer<ProjectileHitResult> hitResults)
            {
                hitResults.Clear();

                // Find projectile spec
                if (!TryFindProjectileSpec(ProjectileCatalog, projectile.ProjectileId, out var spec))
                {
                    return;
                }

                // Skip if projectile hasn't moved
                float3 currentPos = transform.Position;
                float3 prevPos = projectile.PrevPos;
                float3 delta = currentPos - prevPos;
                float deltaLength = math.length(delta);
                if (deltaLength < 1e-6f)
                {
                    return;
                }

                // Build collision filter from spec
                var filter = new CollisionFilter
                {
                    BelongsTo = 0xFFFFFFFF, // Projectiles belong to all layers
                    CollidesWith = spec.HitFilter,
                    GroupIndex = 0
                };

                bool hit = false;
                float3 hitPos = currentPos;
                float3 hitNormal = -math.normalize(delta);
                Entity hitEntity = Entity.Null;
                float timeOfImpact = 1f;

                // Choose collision method based on projectile radius
                if (spec.AoERadius > 0f)
                {
                    // Spherecast for projectiles with radius
                    var sphereGeometry = new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = spec.AoERadius
                    };

                    var collider = Unity.Physics.SphereCollider.Create(
                        sphereGeometry,
                        filter,
                        CollisionGeometry.Constants.UnityPhysicsConvexHullMassProperties);

                    var colliderCastInput = new ColliderCastInput
                    {
                        Collider = collider,
                        Start = prevPos,
                        End = currentPos,
                        Orientation = quaternion.identity
                    };

                    if (PhysicsWorld.CastCollider(colliderCastInput, out var castHit))
                    {
                        hit = true;
                        hitPos = castHit.Position;
                        hitNormal = castHit.SurfaceNormal;
                        hitEntity = castHit.Entity;
                        timeOfImpact = castHit.Fraction;
                    }

                    collider.Dispose();
                }
                else
                {
                    // Raycast for small/fast projectiles
                    var raycastInput = new RaycastInput
                    {
                        Start = prevPos,
                        End = currentPos,
                        Filter = filter
                    };

                    if (PhysicsWorld.CastRay(raycastInput, out var raycastHit))
                    {
                        hit = true;
                        hitPos = raycastHit.Position;
                        hitNormal = raycastHit.SurfaceNormal;
                        hitEntity = raycastHit.Entity;
                        timeOfImpact = raycastHit.Fraction;
                    }
                }

                if (hit)
                {
                    // Add hit result to buffer
                    hitResults.Add(new ProjectileHitResult
                    {
                        HitPosition = hitPos,
                        HitNormal = hitNormal,
                        HitEntity = hitEntity,
                        TimeOfImpact = timeOfImpact
                    });

                    // Update projectile position to hit point
                    projectile.PrevPos = transform.Position;
                    transform.Position = hitPos;

                    // Emit impact FX request
                    if (HasPresentationHub && ImpactFxBuffers.HasBuffer(HubEntity))
                    {
                        var fxRequests = ImpactFxBuffers[HubEntity];
                        // Use projectile ID hash as effect ID (would be mapped to actual FX in presentation bindings)
                        int effectId = (int)(projectile.ProjectileId.GetHashCode() & 0x7FFFFFFF);
                        fxRequests.Add(new PlayEffectRequest
                        {
                            EffectId = effectId,
                            Target = Entity.Null,
                            Position = hitPos,
                            Rotation = quaternion.LookRotationSafe(hitNormal, math.up()),
                            DurationSeconds = 1f,
                            LifetimePolicy = PresentationLifetimePolicy.Timed,
                            AttachRule = PresentationAttachRule.World
                        });
                    }
                }
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

