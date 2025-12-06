using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Spatial partition AoE system - uses existing SpatialGrid infrastructure.
    /// Explosion updates 3×3×3 cells around origin, entities sample cell's accumulated energy.
    /// Payoff: O(k) vs. O(n²) overlap checks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileCollisionSystem))]
    [UpdateBefore(typeof(ProjectileEffectExecutionSystem))]
    public partial struct ProjectileAoESystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileHitResult>();
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

            if (!SystemAPI.TryGetSingleton<SpatialGridConfig>(out var spatialConfig) ||
                !SystemAPI.TryGetSingleton<SpatialGridState>(out var spatialState))
            {
                return; // No spatial grid available
            }

            if (!SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog) ||
                !projectileCatalog.Catalog.IsCreated)
            {
                return;
            }

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var impactBufferLookup = state.GetBufferLookup<ImpactEvent>();
            impactBufferLookup.Update(ref state);

            var transformLookup = state.GetComponentLookup<LocalTransform>(true);
            transformLookup.Update(ref state);

            var job = new ProjectileAoEJob
            {
                SpatialConfig = spatialConfig,
                ProjectileCatalog = projectileCatalog.Catalog,
                Ecb = ecb,
                ImpactBuffers = impactBufferLookup,
                TransformLookup = transformLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProjectileAoEJob : IJobEntity
        {
            [ReadOnly] public SpatialGridConfig SpatialConfig;
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [NativeDisableParallelForRestriction] public BufferLookup<ImpactEvent> ImpactBuffers;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity projectileEntity,
                DynamicBuffer<ProjectileHitResult> hitResults)
            {
                if (hitResults.Length == 0)
                {
                    return;
                }

                // Process each hit for AoE
                for (int hitIdx = 0; hitIdx < hitResults.Length; hitIdx++)
                {
                    var hit = hitResults[hitIdx];
                    
                    // Find projectile spec (would need ProjectileMetadata to get archetype)
                    // For now, use default AoE radius
                    float aoeRadius = 5f; // TODO: Get from projectile spec

                    if (aoeRadius <= 0f)
                    {
                        continue; // No AoE
                    }

                    // Get cells in 3×3×3 grid around explosion origin
                    float3 explosionPos = hit.HitPosition;
                    SpatialHash.Quantize(explosionPos, SpatialConfig, out int3 centerCell);

                    // Query 3×3×3 cells around center
                    var aoeTargets = new NativeList<Entity>(64, Allocator.Temp);

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int3 cellCoords = centerCell + new int3(dx, dy, dz);
                                int cellId = SpatialHash.Flatten(cellCoords, SpatialConfig);

                                if (cellId < 0 || cellId >= SpatialConfig.CellCount)
                                {
                                    continue;
                                }

                                // Query entities in this cell (would need spatial grid lookup)
                                // For now, skip - requires spatial grid entity lookup system
                                // TODO: Integrate with SpatialGrid query API
                            }
                        }
                    }

                    // Apply AoE damage with distance falloff
                    float baseAoEDamage = 10f; // TODO: Get from projectile spec

                    for (int i = 0; i < aoeTargets.Length; i++)
                    {
                        var target = aoeTargets[i];
                        if (target == hit.HitEntity || !TransformLookup.HasComponent(target))
                        {
                            continue;
                        }

                        float3 targetPos = TransformLookup[target].Position;
                        float distance = math.distance(explosionPos, targetPos);
                        
                        if (distance > aoeRadius)
                        {
                            continue;
                        }

                        // Distance-based falloff
                        float falloff = math.saturate(1f - (distance / aoeRadius));
                        float aoeDamage = baseAoEDamage * falloff;

                        // Add impact event
                        if (ImpactBuffers.HasBuffer(target))
                        {
                            var impacts = ImpactBuffers[target];
                            impacts.Add(new ImpactEvent
                            {
                                Target = target,
                                Damage = aoeDamage,
                                HitPoint = explosionPos
                            });
                        }
                        else
                        {
                            Ecb.AddBuffer<ImpactEvent>(chunkIndex, target);
                        }
                    }

                    aoeTargets.Dispose();
                }
            }
        }
    }
}

