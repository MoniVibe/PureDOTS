using PureDOTS.Runtime.Buffs;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Processes projectile hit results and applies effect operations (Damage, AoE, Chain, Pierce, Status).
    /// Runs after ProjectileCollisionSystem in CombatSystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileCollisionSystem))]
    [UpdateBefore(typeof(DamageApplicationSystem))]
    public partial struct ProjectileEffectExecutionSystem : ISystem
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

            if (!SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog))
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            // Optional: get spatial grid for AoE/Chain queries
            var hasSpatialGrid = SystemAPI.TryGetSingleton<SpatialGridConfig>(out var spatialConfig) &&
                                 SystemAPI.TryGetSingleton<SpatialGridState>(out var spatialState);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Optional: get physics world for AoE overlap queries
            var hasPhysicsWorld = SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorld);

            var damageBufferLookup = state.GetBufferLookup<DamageEvent>();
            var buffRequestBufferLookup = state.GetBufferLookup<BuffApplicationRequest>();
            var transformLookup = state.GetComponentLookup<LocalTransform>(true);

            var job = new ProjectileEffectExecutionJob
            {
                ProjectileCatalog = projectileCatalog.Catalog,
                CurrentTick = currentTick,
                Ecb = ecb,
                HasSpatialGrid = hasSpatialGrid,
                SpatialConfig = hasSpatialGrid ? spatialConfig : default,
                HasPhysicsWorld = hasPhysicsWorld,
                PhysicsWorld = hasPhysicsWorld ? physicsWorld : default,
                DamageBuffers = damageBufferLookup,
                BuffRequestBuffers = buffRequestBufferLookup,
                TransformLookup = transformLookup
            };

            damageBufferLookup.Update(ref state);
            buffRequestBufferLookup.Update(ref state);
            transformLookup.Update(ref state);

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProjectileEffectExecutionJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public bool HasSpatialGrid;
            [ReadOnly] public SpatialGridConfig SpatialConfig;
            public bool HasPhysicsWorld;
            [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
            [NativeDisableParallelForRestriction] public BufferLookup<DamageEvent> DamageBuffers;
            [NativeDisableParallelForRestriction] public BufferLookup<BuffApplicationRequest> BuffRequestBuffers;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity projectileEntity,
                ref ProjectileEntity projectile,
                DynamicBuffer<ProjectileHitResult> hitResults)
            {
                if (hitResults.Length == 0)
                {
                    return;
                }

                // Find projectile spec
                if (!TryFindProjectileSpec(ProjectileCatalog, projectile.ProjectileId, out var spec))
                {
                    return;
                }

                // Process each hit
                for (int hitIndex = 0; hitIndex < hitResults.Length; hitIndex++)
                {
                    var hit = hitResults[hitIndex];

                    // Skip invalid hits
                    if (hit.HitEntity == Entity.Null)
                    {
                        continue;
                    }

                    // Process all effect operations
                    for (int opIndex = 0; opIndex < spec.OnHit.Length; opIndex++)
                    {
                        var effectOp = spec.OnHit[opIndex];
                        ProcessEffectOp(
                            chunkIndex,
                            projectileEntity,
                            ref projectile,
                            spec,
                            effectOp,
                            hit,
                            chunkIndex);
                    }

                    // Decrement pierce count
                    projectile.HitsLeft -= 1f;
                    if (projectile.HitsLeft <= 0f)
                    {
                        // Projectile exhausted - destroy it
                        Ecb.DestroyEntity(chunkIndex, projectileEntity);
                        return;
                    }
                }

                // Clear hit results after processing
                hitResults.Clear();
            }

            private void ProcessEffectOp(
                int chunkIndex,
                Entity projectileEntity,
                ref ProjectileEntity projectile,
                ProjectileSpec spec,
                EffectOp effectOp,
                ProjectileHitResult hit,
                int entityInQueryIndex)
            {
                switch (effectOp.Kind)
                {
                    case EffectOpKind.Damage:
                        ApplyDamage(chunkIndex, projectile, spec, effectOp, hit);
                        break;

                    case EffectOpKind.AoE:
                        ApplyAoE(chunkIndex, projectile, spec, effectOp, hit);
                        break;

                    case EffectOpKind.Chain:
                        ApplyChain(chunkIndex, projectile, spec, effectOp, hit);
                        break;

                    case EffectOpKind.Status:
                        ApplyStatus(chunkIndex, projectile, effectOp, hit);
                        break;

                    case EffectOpKind.Knockback:
                        ApplyKnockback(chunkIndex, projectile, effectOp, hit);
                        break;

                    case EffectOpKind.SpawnSub:
                        ApplySpawnSub(chunkIndex, projectile, spec, effectOp, hit);
                        break;
                }
            }

            private void ApplyDamage(
                int chunkIndex,
                ProjectileEntity projectile,
                ProjectileSpec spec,
                EffectOp effectOp,
                ProjectileHitResult hit)
            {
                // Calculate damage from spec and effect magnitude
                float damage = spec.Damage.BaseDamage * effectOp.Magnitude;

                // Add damage event to target's buffer
                if (DamageBuffers.HasBuffer(hit.HitEntity))
                {
                    var damageEvents = DamageBuffers[hit.HitEntity];
                    damageEvents.Add(new DamageEvent
                    {
                        SourceEntity = projectile.SourceEntity,
                        TargetEntity = hit.HitEntity,
                        RawDamage = damage,
                        Type = DamageType.Physical, // Could be extended based on effectOp
                        Tick = CurrentTick,
                        Flags = DamageFlags.Pierce // Projectiles typically pierce armor
                    });
                }
                else
                {
                    // Create buffer if it doesn't exist
                    Ecb.AddBuffer<DamageEvent>(chunkIndex, hit.HitEntity);
                }
            }

            private void ApplyAoE(
                int chunkIndex,
                ProjectileEntity projectile,
                ProjectileSpec spec,
                EffectOp effectOp,
                ProjectileHitResult hit)
            {
                float aoeRadius = effectOp.Aux > 0f ? effectOp.Aux : spec.AoERadius;
                if (aoeRadius <= 0f)
                {
                    return;
                }

                // Collect entities in AoE radius
                var aoeTargets = new NativeList<Entity>(32, Allocator.Temp);

                if (HasPhysicsWorld)
                {
                    // Use physics overlap sphere
                    var overlapInput = new OverlapSphereInput
                    {
                        Position = hit.HitPosition,
                        Radius = aoeRadius,
                        Filter = new CollisionFilter
                        {
                            BelongsTo = 0xFFFFFFFF,
                            CollidesWith = spec.HitFilter,
                            GroupIndex = 0
                        }
                    };

                    var collector = new AoeCollector(projectile.SourceEntity, hit.HitEntity);
                    PhysicsWorld.OverlapSphere(overlapInput, ref collector);
                    aoeTargets = collector.HitEntities;
                }
                else if (HasSpatialGrid)
                {
                    // Use spatial grid query
                    var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
                    // Note: We'd need access to ranges/entries buffers here
                    // For now, skip spatial grid AoE if physics is available
                }

                // Apply damage to all entities in AoE
                for (int i = 0; i < aoeTargets.Length; i++)
                {
                    var target = aoeTargets[i];
                    if (target == Entity.Null || target == hit.HitEntity)
                    {
                        continue; // Skip primary hit (already processed)
                    }

                    // Get target position for distance calculation
                    float3 targetPos = float3.zero;
                    if (TransformLookup.HasComponent(target))
                    {
                        targetPos = TransformLookup[target].Position;
                    }
                    else
                    {
                        continue; // Skip if no transform
                    }

                    // Calculate distance-based falloff
                    float distance = math.distance(hit.HitPosition, targetPos);
                    float falloff = math.saturate(1f - (distance / aoeRadius));
                    float aoeDamage = spec.Damage.BaseDamage * effectOp.Magnitude * falloff;

                    // Add damage event to target's buffer
                    if (DamageBuffers.HasBuffer(target))
                    {
                        var damageEvents = DamageBuffers[target];
                        damageEvents.Add(new DamageEvent
                        {
                            SourceEntity = projectile.SourceEntity,
                            TargetEntity = target,
                            RawDamage = aoeDamage,
                            Type = DamageType.Physical,
                            Tick = CurrentTick,
                            Flags = DamageFlags.Pierce
                        });
                    }
                    else
                    {
                        Ecb.AddBuffer<DamageEvent>(chunkIndex, target);
                    }
                }

                aoeTargets.Dispose();
            }

            private void ApplyChain(
                int chunkIndex,
                ProjectileEntity projectile,
                ProjectileSpec spec,
                EffectOp effectOp,
                ProjectileHitResult hit)
            {
                float chainRange = effectOp.Aux > 0f ? effectOp.Aux : spec.ChainRange;
                if (chainRange <= 0f)
                {
                    return;
                }

                int chainCount = (int)effectOp.Magnitude;
                if (chainCount <= 0)
                {
                    chainCount = 1;
                }

                // Find next target within chain range
                // Use spatial grid or physics query
                Entity currentTarget = hit.HitEntity;
                var chainedTargets = new NativeList<Entity>(chainCount, Allocator.Temp);
                chainedTargets.Add(currentTarget);

                for (int chainIndex = 0; chainIndex < chainCount; chainIndex++)
                {
                    Entity nextTarget = FindNextChainTarget(
                        currentTarget,
                        chainedTargets,
                        chainRange,
                        spec.HitFilter);

                    if (nextTarget == Entity.Null)
                    {
                        break; // No more valid targets
                    }

                    chainedTargets.Add(nextTarget);
                    currentTarget = nextTarget;

                    // Apply chain damage (reduced per hop)
                    float chainDamage = spec.Damage.BaseDamage * effectOp.Magnitude * math.pow(0.7f, chainIndex + 1);
                    
                    // Add damage event to target's buffer
                    if (DamageBuffers.HasBuffer(nextTarget))
                    {
                        var damageEvents = DamageBuffers[nextTarget];
                        damageEvents.Add(new DamageEvent
                        {
                            SourceEntity = projectile.SourceEntity,
                            TargetEntity = nextTarget,
                            RawDamage = chainDamage,
                            Type = DamageType.Physical,
                            Tick = CurrentTick,
                            Flags = DamageFlags.Pierce
                        });
                    }
                    else
                    {
                        Ecb.AddBuffer<DamageEvent>(chunkIndex, nextTarget);
                    }
                }

                chainedTargets.Dispose();
            }

            private void ApplyStatus(
                int chunkIndex,
                ProjectileEntity projectile,
                EffectOp effectOp,
                ProjectileHitResult hit)
            {
                if (effectOp.StatusId == 0)
                {
                    return; // Invalid status ID
                }

                // Create buff ID from status ID (would need mapping in practice)
                // For now, use a placeholder - in practice, StatusId would map to a BuffId
                var buffId = new FixedString64Bytes($"Status_{effectOp.StatusId}");

                // Add buff application request to target's buffer
                if (BuffRequestBuffers.HasBuffer(hit.HitEntity))
                {
                    var buffRequests = BuffRequestBuffers[hit.HitEntity];
                    buffRequests.Add(new BuffApplicationRequest
                    {
                        BuffId = buffId,
                        SourceEntity = projectile.SourceEntity,
                        DurationOverride = effectOp.Duration > 0f ? effectOp.Duration : 0f,
                        StacksToApply = 1
                    });
                }
                else
                {
                    Ecb.AddBuffer<BuffApplicationRequest>(chunkIndex, hit.HitEntity);
                }
            }

            private void ApplyKnockback(
                int chunkIndex,
                ProjectileEntity projectile,
                EffectOp effectOp,
                ProjectileHitResult hit)
            {
                // Apply knockback force
                // Would require Velocity component or similar
                // Placeholder for now
            }

            private void ApplySpawnSub(
                int chunkIndex,
                ProjectileEntity projectile,
                ProjectileSpec spec,
                EffectOp effectOp,
                ProjectileHitResult hit)
            {
                // Spawn sub-projectiles
                // Would create new ProjectileEntity with different spec
                // Placeholder for now
            }

            private Entity FindNextChainTarget(
                Entity currentTarget,
                NativeList<Entity> excludeList,
                float range,
                uint hitFilter)
            {
                // Find nearest valid target within range, excluding already chained targets
                if (!TransformLookup.HasComponent(currentTarget))
                {
                    return Entity.Null;
                }

                float3 currentPos = TransformLookup[currentTarget].Position;
                Entity nearestTarget = Entity.Null;
                float nearestDistSq = range * range;

                // Use physics overlap sphere to find candidates
                if (HasPhysicsWorld)
                {
                    var overlapInput = new OverlapSphereInput
                    {
                        Position = currentPos,
                        Radius = range,
                        Filter = new CollisionFilter
                        {
                            BelongsTo = 0xFFFFFFFF,
                            CollidesWith = hitFilter,
                            GroupIndex = 0
                        }
                    };

                    var collector = new ChainCollector(excludeList);
                    PhysicsWorld.OverlapSphere(overlapInput, ref collector);

                    // Find nearest from collected entities
                    for (int i = 0; i < collector.HitEntities.Length; i++)
                    {
                        var candidate = collector.HitEntities[i];
                        if (!TransformLookup.HasComponent(candidate))
                        {
                            continue;
                        }

                        float3 candidatePos = TransformLookup[candidate].Position;
                        float distSq = math.lengthsq(candidatePos - currentPos);
                        if (distSq < nearestDistSq)
                        {
                            nearestDistSq = distSq;
                            nearestTarget = candidate;
                        }
                    }

                    collector.HitEntities.Dispose();
                }

                return nearestTarget;
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

        // Collector for AoE overlap queries
        private struct AoeCollector : ICollector<OverlapColliderHit>
        {
            public Entity SourceEntity;
            public Entity PrimaryHitEntity;
            public NativeList<Entity> HitEntities;

            public AoeCollector(Entity source, Entity primaryHit)
            {
                SourceEntity = source;
                PrimaryHitEntity = primaryHit;
                HitEntities = new NativeList<Entity>(32, Allocator.Temp);
            }

            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction => 1f;

            public bool AddHit(OverlapColliderHit hit)
            {
                if (hit.Entity != SourceEntity && hit.Entity != PrimaryHitEntity)
                {
                    HitEntities.Add(hit.Entity);
                }
                return true;
            }
        }

        // Collector for chain queries
        private struct ChainCollector : ICollector<OverlapColliderHit>
        {
            public NativeList<Entity> HitEntities;
            public NativeList<Entity> ExcludeList;

            public ChainCollector(NativeList<Entity> excludeList)
            {
                HitEntities = new NativeList<Entity>(16, Allocator.Temp);
                ExcludeList = excludeList;
            }

            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction => 1f;

            public bool AddHit(OverlapColliderHit hit)
            {
                // Exclude already chained targets
                bool isExcluded = false;
                for (int i = 0; i < ExcludeList.Length; i++)
                {
                    if (ExcludeList[i] == hit.Entity)
                    {
                        isExcluded = true;
                        break;
                    }
                }

                if (!isExcluded)
                {
                    HitEntities.Add(hit.Entity);
                }
                return true;
            }
        }
    }
}

