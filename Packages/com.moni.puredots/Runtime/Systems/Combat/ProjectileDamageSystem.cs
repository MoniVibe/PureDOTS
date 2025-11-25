using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Processes projectile impacts and creates damage events.
    /// Handles pierce mechanics and projectile destruction.
    /// Runs before DamageApplicationSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(DamageApplicationSystem))]
    public partial struct ProjectileDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
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
            var currentTick = timeState.Tick;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var entityLookup = state.GetEntityStorageInfoLookup();
            var healthLookup = state.GetComponentLookup<Health>(true);
            var damageableLookup = state.GetComponentLookup<Damageable>(true);
            var damageBufferLookup = state.GetBufferLookup<DamageEvent>();

            new ProcessProjectileImpactsJob
            {
                CurrentTick = currentTick,
                Ecb = ecb,
                EntityLookup = entityLookup,
                HealthLookup = healthLookup,
                DamageableLookup = damageableLookup,
                DamageBuffers = damageBufferLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessProjectileImpactsJob : IJobEntity
        {
            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public EntityStorageInfoLookup EntityLookup;
            [ReadOnly] public ComponentLookup<Health> HealthLookup;
            [ReadOnly] public ComponentLookup<Damageable> DamageableLookup;
            [NativeDisableParallelForRestriction] public BufferLookup<DamageEvent> DamageBuffers;

            void Execute(
                Entity projectileEntity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref ProjectileEntity projectile,
                ref DynamicBuffer<DamageEvent> damageEvents)
            {
                // Check if projectile has hit its target
                if (projectile.TargetEntity == Entity.Null || !EntityLookup.Exists(projectile.TargetEntity))
                {
                    // No target or target destroyed - destroy projectile
                    Ecb.DestroyEntity(entityInQueryIndex, projectileEntity);
                    return;
                }

                // Check if target is damageable
                if (!HealthLookup.HasComponent(projectile.TargetEntity) &&
                    !DamageableLookup.HasComponent(projectile.TargetEntity))
                {
                    // Target not damageable - destroy projectile
                    Ecb.DestroyEntity(entityInQueryIndex, projectileEntity);
                    return;
                }

                // Get projectile damage from WeaponSpec or default
                float damage = 10f; // Default damage
                DamageType damageType = DamageType.Physical;

                // TODO: Look up projectile damage from WeaponSpec blob if available
                // For now, use default damage

                // Create damage event
                var damageEvent = new DamageEvent
                {
                    SourceEntity = projectile.SourceEntity,
                    TargetEntity = projectile.TargetEntity,
                    RawDamage = damage,
                    Type = damageType,
                    Tick = CurrentTick,
                    Flags = DamageFlags.Pierce // Projectiles typically pierce armor
                };

                // Add damage event to target
                if (DamageBuffers.HasBuffer(projectile.TargetEntity))
                {
                    var targetDamageEvents = DamageBuffers[projectile.TargetEntity];
                    targetDamageEvents.Add(damageEvent);
                }
                else
                {
                    // Create buffer if it doesn't exist
                    Ecb.AddBuffer<DamageEvent>(entityInQueryIndex, projectile.TargetEntity);
                    // Note: We can't add to the buffer here since it's on another entity
                    // This will be handled by a separate system or the damage will be queued
                }

                // Handle pierce mechanics
                projectile.PierceCount--;
                if (projectile.PierceCount <= 0)
                {
                    // Projectile exhausted - destroy it
                    Ecb.DestroyEntity(entityInQueryIndex, projectileEntity);
                }
                else
                {
                    // Projectile continues - reset target for next impact
                    // Note: In a full implementation, we'd need to find the next target
                    // For now, destroy the projectile after first hit
                    Ecb.DestroyEntity(entityInQueryIndex, projectileEntity);
                }
            }
        }
    }
}

