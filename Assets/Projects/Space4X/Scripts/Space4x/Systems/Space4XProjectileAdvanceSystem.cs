using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
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

            var currentTime = timeState.Time;
            var deltaTime = timeState.FixedDeltaTime;

            // Optional: get spatial grid for homing target acquisition
            var hasSpatialGrid = SystemAPI.TryGetSingleton<SpatialGridConfig>(out var spatialConfig) &&
                                 SystemAPI.TryGetSingleton<SpatialGridState>(out var spatialState);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var transformLookup = state.GetComponentLookup<LocalTransform>(true);

            var job = new ProjectileAdvanceJob
            {
                ProjectileCatalog = projectileCatalog.Catalog,
                CurrentTime = currentTime,
                DeltaTime = deltaTime,
                Ecb = ecb,
                HasSpatialGrid = hasSpatialGrid,
                SpatialConfig = hasSpatialGrid ? spatialConfig : default,
                TransformLookup = transformLookup
            };

            transformLookup.Update(ref state);
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProjectileAdvanceJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public float CurrentTime;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public bool HasSpatialGrid;
            [ReadOnly] public SpatialGridConfig SpatialConfig;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity entity,
                ref ProjectileEntity projectile,
                ref LocalTransform transform)
            {
                // Find projectile spec
                if (!TryFindProjectileSpec(ProjectileCatalog, projectile.ProjectileId, out var projectileSpec))
                {
                    Ecb.DestroyEntity(chunkIndex, entity);
                    return;
                }

                // Update age
                projectile.Age = CurrentTime - projectile.SpawnTime;
                if (projectile.Age >= projectileSpec.Lifetime)
                {
                    Ecb.DestroyEntity(chunkIndex, entity);
                    return;
                }

                // Store previous position for continuous collision
                projectile.PrevPos = transform.Position;

                // Update position and velocity based on projectile kind
                float3 newPosition = transform.Position;
                float3 newVelocity = projectile.Velocity;

                switch ((ProjectileKind)projectileSpec.Kind)
                {
                    case ProjectileKind.Ballistic:
                        // Ballistic: constant velocity
                        newPosition += projectile.Velocity * DeltaTime;
                        break;

                    case ProjectileKind.Homing:
                        // Homing: steer toward target with turn rate limit
                        if (projectile.TargetEntity != Entity.Null && TransformLookup.HasComponent(projectile.TargetEntity))
                        {
                            float3 targetPos = TransformLookup[projectile.TargetEntity].Position;
                            float3 toTarget = targetPos - transform.Position;
                            float distanceToTarget = math.length(toTarget);

                            // Check if target is within seek radius
                            if (distanceToTarget <= projectileSpec.SeekRadius)
                            {
                                // Calculate desired direction
                                float3 desiredDirection = math.normalizesafe(toTarget, math.forward());
                                float3 currentDirection = math.normalizesafe(projectile.Velocity, math.forward());

                                // Calculate angle between current and desired direction
                                float angleRad = math.acos(math.clamp(math.dot(currentDirection, desiredDirection), -1f, 1f));
                                float angleDeg = math.degrees(angleRad);

                                // Clamp turn rate
                                float maxTurnDeg = projectileSpec.TurnRateDeg * DeltaTime;
                                float turnDeg = math.min(angleDeg, maxTurnDeg);

                                if (turnDeg > 0.1f) // Only turn if angle is significant
                                {
                                    // Calculate rotation axis
                                    float3 rotationAxis = math.cross(currentDirection, desiredDirection);
                                    if (math.lengthsq(rotationAxis) > 1e-6f)
                                    {
                                        rotationAxis = math.normalize(rotationAxis);
                                        quaternion rotation = quaternion.AxisAngle(rotationAxis, math.radians(turnDeg));
                                        newVelocity = math.mul(rotation, currentDirection) * math.length(projectile.Velocity);
                                    }
                                }
                                else
                                {
                                    // Already aligned, use desired direction
                                    newVelocity = desiredDirection * math.length(projectile.Velocity);
                                }
                            }
                            else
                            {
                                // Target out of range - try to acquire new target via spatial grid
                                // For now, maintain current velocity
                            }

                            // Ensure velocity is not NaN or zero
                            if (math.any(math.isnan(newVelocity)) || math.lengthsq(newVelocity) < 1e-6f)
                            {
                                newVelocity = math.forward() * projectileSpec.Speed;
                            }

                            newPosition += newVelocity * DeltaTime;
                        }
                        else
                        {
                            // No target or target invalid - maintain velocity
                            newPosition += projectile.Velocity * DeltaTime;
                        }
                        break;

                    case ProjectileKind.Beam:
                        // Beam: handled by BeamTickSystem, but still advance position for visuals
                        newPosition += projectile.Velocity * DeltaTime;
                        break;
                }

                // Update projectile state
                projectile.Velocity = newVelocity;
                projectile.DistanceTraveled += math.length(newVelocity) * DeltaTime;
                transform.Position = newPosition;
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

