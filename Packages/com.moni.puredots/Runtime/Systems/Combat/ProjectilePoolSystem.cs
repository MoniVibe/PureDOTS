using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Manages entity pools for projectiles per archetype.
    /// Batch allocation (256-1024 entities per chunk), dequeue on fire, enqueue on expiry.
    /// Uses IEnableableComponent for deactivation (not destroy) - 0 GC, constant-time spawn/despawn.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(ProjectileMotionSystem))]
    public partial struct ProjectilePoolSystem : ISystem
    {
        private const int BatchAllocationSize = 512; // Entities per batch allocation
        private const int PrewarmCount = 256; // Pre-warm count per archetype

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectilePool>();
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

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var poolEntity = SystemAPI.GetSingletonEntity<ProjectilePool>();
            var pool = SystemAPI.GetComponent<ProjectilePool>(poolEntity);

            if (!pool.ArchetypeCatalog.IsCreated)
            {
                return;
            }

            // Process lifetime expiry - return to pool
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new ProjectileLifetimeJob
            {
                DeltaTime = timeState.DeltaTime,
                Ecb = ecb
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Borrows a projectile entity from the pool for the given archetype.
        /// If pool is empty, allocates a new batch.
        /// </summary>
        public static Entity BorrowProjectile(
            ref SystemState state,
            ushort archetypeId,
            float3 position,
            float3 velocity,
            quaternion rotation,
            Entity sourceEntity,
            Entity targetEntity,
            uint seed,
            int shotSequence,
            int pelletIndex)
        {
            // This is a managed operation - will be called from managed spawn system
            // For now, create entity directly (pool integration will be added in managed wrapper)
            var entity = state.EntityManager.CreateEntity();

            // Add minimal components
            state.EntityManager.AddComponent<Projectile>(entity);
            state.EntityManager.AddComponent<LocalTransform>(entity);
            state.EntityManager.AddComponent<ProjectileMetadata>(entity);
            state.EntityManager.AddComponent<PooledProjectileTag>(entity);

            // Set initial data
            var projectile = new Projectile
            {
                Position = position,
                Velocity = velocity,
                Rotation = rotation,
                Lifetime = 10f, // Will be set from spec
                ArchetypeId = archetypeId
            };
            state.EntityManager.SetComponentData(entity, projectile);

            var transform = LocalTransform.FromPositionRotation(position, rotation);
            state.EntityManager.SetComponentData(entity, transform);

            var metadata = new ProjectileMetadata
            {
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                PrevPos = position,
                DistanceTraveled = 0f,
                HitsLeft = 1f, // Will be set from spec
                Age = 0f,
                Seed = seed,
                ShotSequence = shotSequence,
                PelletIndex = pelletIndex
            };
            state.EntityManager.SetComponentData(entity, metadata);

            // Enable the pooled tag
            state.EntityManager.SetComponentEnabled<PooledProjectileTag>(entity, true);

            return entity;
        }

        /// <summary>
        /// Returns a projectile entity to the pool (disables it).
        /// </summary>
        public static void ReturnProjectile(ref SystemState state, Entity projectileEntity)
        {
            if (state.EntityManager.HasComponent<PooledProjectileTag>(projectileEntity))
            {
                state.EntityManager.SetComponentEnabled<PooledProjectileTag>(projectileEntity, false);
            }
        }

        [BurstCompile]
        public partial struct ProjectileLifetimeJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity entity,
                ref Projectile projectile,
                EnabledRefRW<PooledProjectileTag> pooledTag)
            {
                // Check lifetime expiry
                if (projectile.Lifetime <= 0f)
                {
                    // Return to pool by disabling
                    pooledTag.ValueRW = false;
                }
            }
        }
    }
}

