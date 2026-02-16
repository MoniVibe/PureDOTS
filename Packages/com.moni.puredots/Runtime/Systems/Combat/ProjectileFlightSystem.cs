using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.LowLevel;
using PureDOTS.Runtime.Movement;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Steering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Integrates projectile movement and simple homing guidance.
    /// Pure data only: updates LocalTransform + ProjectileEntity state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectilePoolSpawnSystem))]
    [UpdateBefore(typeof(ProjectileCollisionSystem))]
    public partial struct ProjectileFlightSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> _targetTransformLookup;
        private ComponentLookup<VelocitySample> _velocityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ProjectileActive>();
            _targetTransformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _velocityLookup = state.GetComponentLookup<VelocitySample>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog) ||
                !projectileCatalog.Catalog.IsCreated)
            {
                return;
            }

            var hasAmmoCatalog = SystemAPI.TryGetSingleton<AmmoCatalog>(out var ammoCatalog) &&
                                 ammoCatalog.Catalog.IsCreated;

            var poolingEnabled = SystemAPI.TryGetSingleton<ProjectilePoolConfig>(out var poolConfig) &&
                                 poolConfig.Capacity > 0 &&
                                 poolConfig.Prefab != Entity.Null;

            var hasTrackingHub = SystemAPI.TryGetSingletonEntity<ProjectileTrackingHub>(out var trackingHubEntity);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            _targetTransformLookup.Update(ref state);
            _velocityLookup.Update(ref state);

            var job = new ProjectileFlightJob
            {
                DeltaTime = timeState.DeltaTime,
                ProjectileCatalog = projectileCatalog.Catalog,
                HasAmmoCatalog = hasAmmoCatalog,
                AmmoCatalog = hasAmmoCatalog ? ammoCatalog.Catalog : default,
                TargetTransformLookup = _targetTransformLookup,
                VelocityLookup = _velocityLookup,
                PoolingEnabled = poolingEnabled,
                HasTrackingHub = hasTrackingHub,
                TrackingHub = trackingHubEntity,
                CurrentTick = timeState.Tick,
                CurrentTime = timeState.ElapsedTime,
                Ecb = ecb
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProjectileFlightJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public bool HasAmmoCatalog;
            [ReadOnly] public BlobAssetReference<AmmoCatalogBlob> AmmoCatalog;
            [ReadOnly] public ComponentLookup<LocalToWorld> TargetTransformLookup;
            [ReadOnly] public ComponentLookup<VelocitySample> VelocityLookup;
            public bool PoolingEnabled;
            public bool HasTrackingHub;
            public Entity TrackingHub;
            public uint CurrentTick;
            public float CurrentTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity entity,
                ref ProjectileEntity projectile,
                ref ProjectileTrackingState tracking,
                ref LocalTransform transform,
                EnabledRefRW<ProjectileActive> active,
                EnabledRefRW<ProjectileRecycleTag> recycleTag)
            {
                if (!active.ValueRO)
                {
                    return;
                }

                ref var spec = ref FindProjectileSpec(ProjectileCatalog, projectile.ProjectileId);
                if (UnsafeRef.IsNull(ref spec))
                {
                    return;
                }

                float speedMultiplier = 1f;
                float lifetimeMultiplier = 1f;
                float turnRateMultiplier = 1f;
                float seekRadiusMultiplier = 1f;

                if (HasAmmoCatalog && projectile.AmmoId.Length > 0)
                {
                    ref var ammoSpec = ref FindAmmoSpec(AmmoCatalog, projectile.AmmoId);
                    if (!UnsafeRef.IsNull(ref ammoSpec))
                    {
                        speedMultiplier = ammoSpec.SpeedMultiplier;
                        lifetimeMultiplier = ammoSpec.LifetimeMultiplier;
                        turnRateMultiplier = ammoSpec.TurnRateMultiplier;
                        seekRadiusMultiplier = ammoSpec.SeekRadiusMultiplier;
                    }
                }

                float dt = DeltaTime;
                if (dt <= 0f)
                {
                    return;
                }

                float3 currentPos = transform.Position;
                projectile.PrevPos = currentPos;

                projectile.Age += dt;
                float effectiveLifetime = spec.Lifetime > 0f ? spec.Lifetime * lifetimeMultiplier : 0f;
                if (effectiveLifetime > 0f && projectile.Age >= effectiveLifetime)
                {
                    if (HasTrackingHub && tracking.TrackingId != 0)
                    {
                        Ecb.AppendToBuffer(chunkIndex, TrackingHub, new ProjectileTrackingEvent
                        {
                            Kind = ProjectileTrackingEventKind.Expire,
                            TrackingId = tracking.TrackingId,
                            Projectile = entity,
                            Source = projectile.SourceEntity,
                            Target = projectile.TargetEntity,
                            ProjectileId = projectile.ProjectileId,
                            AmmoId = projectile.AmmoId,
                            Position = transform.Position,
                            Direction = math.normalizesafe(projectile.Velocity),
                            Tick = CurrentTick,
                            Time = CurrentTime,
                            Value = projectile.Age,
                            Mode = 0,
                            Result = 1
                        });
                        tracking.LastEventTick = CurrentTick;
                    }

                    RetireProjectile(chunkIndex, entity, ref projectile, ref active, ref recycleTag);
                    return;
                }

                float3 velocity = projectile.Velocity;
                float speed = math.length(velocity);

                if (speed <= 1e-4f)
                {
                    if (spec.Speed > 0f)
                    {
                        var forward = transform.Forward();
                        float effectiveSpeed = spec.Speed * speedMultiplier;
                        velocity = math.normalizesafe(forward, new float3(0f, 0f, 1f)) * effectiveSpeed;
                        speed = effectiveSpeed;
                    }
                }
                else if (spec.Speed > 0f)
                {
                    float effectiveSpeed = spec.Speed * speedMultiplier;
                    velocity = math.normalizesafe(velocity) * effectiveSpeed;
                    speed = effectiveSpeed;
                }

                if ((ProjectileKind)spec.Kind == ProjectileKind.Homing && speed > 1e-4f)
                {
                    float effectiveTurnRate = spec.TurnRateDeg * turnRateMultiplier;
                    float effectiveSeekRadius = spec.SeekRadius * seekRadiusMultiplier;
                    velocity = ApplyHoming(ref projectile, currentPos, velocity, speed, effectiveTurnRate, effectiveSeekRadius);
                }

                var delta = velocity * dt;
                transform.Position = currentPos + delta;

                if (math.lengthsq(velocity) > 1e-6f)
                {
                    OrientationHelpers.LookRotationSafe3D(math.normalize(velocity), OrientationHelpers.WorldUp, out var rotation);
                    transform.Rotation = rotation;
                }

                projectile.DistanceTraveled += math.length(delta);
                projectile.Velocity = velocity;
            }

            private float3 ApplyHoming(
                ref ProjectileEntity projectile,
                float3 currentPos,
                float3 currentVel,
                float speed,
                float turnRateDeg,
                float seekRadius)
            {
                if (projectile.TargetEntity == Entity.Null)
                {
                    return currentVel;
                }

                if (!TargetTransformLookup.HasComponent(projectile.TargetEntity))
                {
                    projectile.TargetEntity = Entity.Null;
                    return currentVel;
                }

                float3 targetPos = TargetTransformLookup[projectile.TargetEntity].Position;
                float3 targetVel = VelocityLookup.HasComponent(projectile.TargetEntity)
                    ? VelocityLookup[projectile.TargetEntity].Velocity
                    : float3.zero;

                float3 aimPos = targetPos;
                if (math.lengthsq(targetVel) > 1e-4f)
                {
                    if (SteeringPrimitives.LeadInterceptPoint(targetPos, targetVel, currentPos, speed, out var intercept, out _))
                    {
                        aimPos = intercept;
                    }
                }

                float3 toTarget = aimPos - currentPos;
                float distSq = math.lengthsq(toTarget);
                if (distSq <= 1e-6f)
                {
                    return currentVel;
                }

                float distance = math.sqrt(distSq);
                if (seekRadius > 0f && distance > seekRadius)
                {
                    projectile.TargetEntity = Entity.Null;
                    return currentVel;
                }

                float3 desiredDir = toTarget / distance;
                float3 currentDir = math.normalizesafe(currentVel, desiredDir);

                float dot = math.clamp(math.dot(currentDir, desiredDir), -1f, 1f);
                float angle = math.acos(dot);
                float maxTurn = math.radians(turnRateDeg) * DeltaTime;

                float t = angle <= 1e-5f ? 1f : math.saturate(maxTurn / angle);
                float3 newDir = math.normalizesafe(math.lerp(currentDir, desiredDir, t), desiredDir);

                return newDir * speed;
            }

            private void RetireProjectile(
                int chunkIndex,
                Entity projectileEntity,
                ref ProjectileEntity projectile,
                ref EnabledRefRW<ProjectileActive> active,
                ref EnabledRefRW<ProjectileRecycleTag> recycleTag)
            {
                if (PoolingEnabled)
                {
                    active.ValueRW = false;
                    recycleTag.ValueRW = true;
                    projectile.TargetEntity = Entity.Null;
                    projectile.Velocity = float3.zero;
                    projectile.HitsLeft = 0f;
                    return;
                }

                Ecb.DestroyEntity(chunkIndex, projectileEntity);
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
                    ref var projectileSpec = ref projectiles[i];
                    if (projectileSpec.Id.Equals(projectileId))
                    {
                        return ref projectileSpec;
                    }
                }

                return ref UnsafeRef.Null<ProjectileSpec>();
            }

            private static ref AmmoSpec FindAmmoSpec(
                BlobAssetReference<AmmoCatalogBlob> catalog,
                FixedString32Bytes ammoId)
            {
                if (!catalog.IsCreated)
                {
                    return ref UnsafeRef.Null<AmmoSpec>();
                }

                ref var ammos = ref catalog.Value.Ammunition;
                for (int i = 0; i < ammos.Length; i++)
                {
                    ref var ammoSpec = ref ammos[i];
                    if (ammoSpec.Id.Equals(ammoId))
                    {
                        return ref ammoSpec;
                    }
                }

                return ref UnsafeRef.Null<AmmoSpec>();
            }
        }
    }
}
