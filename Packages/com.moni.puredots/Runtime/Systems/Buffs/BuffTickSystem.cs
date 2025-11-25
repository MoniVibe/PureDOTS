using PureDOTS.Runtime.Buffs;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Buffs
{
    /// <summary>
    /// Processes buff duration, expiry, and periodic effects.
    /// Runs after BuffApplicationSystem in GameplaySystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(BuffApplicationSystem))]
    public partial struct BuffTickSystem : ISystem
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
            var deltaTime = timeState.FixedDeltaTime * timeState.CurrentSpeedMultiplier;
            var currentTick = timeState.Tick;

            // Get buff catalog for periodic effects
            if (!SystemAPI.TryGetSingleton<BuffCatalogRef>(out var catalogRef) ||
                !catalogRef.Blob.IsCreated)
            {
                return;
            }

            ref var catalog = ref catalogRef.Blob.Value;

            // Process all entities with active buffs
            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProcessBuffTicksJob
            {
                Catalog = catalogRef.Blob,
                DeltaTime = deltaTime,
                CurrentTick = currentTick,
                Ecb = ecb
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessBuffTicksJob : IJobEntity
        {
            [ReadOnly]
            public BlobAssetReference<BuffDefinitionBlob> Catalog;

            public float DeltaTime;
            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref DynamicBuffer<ActiveBuff> activeBuffs)
            {
                ref var catalog = ref Catalog.Value;

                // Process buffs in reverse order so we can safely remove expired ones
                for (int i = activeBuffs.Length - 1; i >= 0; i--)
                {
                    var buff = activeBuffs[i];

                    // Find buff definition
                    int buffIndex = -1;
                    for (int j = 0; j < catalog.Buffs.Length; j++)
                    {
                        if (catalog.Buffs[j].BuffId.Equals(buff.BuffId))
                        {
                            buffIndex = j;
                            break;
                        }
                    }

                    if (buffIndex < 0)
                    {
                        // Buff definition not found, remove it
                        activeBuffs.RemoveAtSwapBack(i);
                        continue;
                    }

                    ref var buffDef = ref catalog.Buffs[buffIndex];

                    // Update duration (if not permanent)
                    if (buffDef.BaseDuration > 0f)
                    {
                        buff.RemainingDuration -= DeltaTime;
                        if (buff.RemainingDuration <= 0f)
                        {
                            // Buff expired
                            // Emit removed event (events buffer will be added by game-specific systems if needed)
                            // For now, we skip event emission in the job - games can add event buffers and process them separately
                            activeBuffs.RemoveAtSwapBack(i);
                            continue;
                        }
                    }

                    // Process periodic effects
                    if (buffDef.TickInterval > 0f && buffDef.PeriodicEffects.Length > 0)
                    {
                        buff.TimeSinceLastTick += DeltaTime;
                        if (buff.TimeSinceLastTick >= buffDef.TickInterval)
                        {
                            // Trigger periodic effects
                            int tickCount = (int)(buff.TimeSinceLastTick / buffDef.TickInterval);
                            buff.TimeSinceLastTick -= tickCount * buffDef.TickInterval;

                            // Apply periodic effects (scaled by stacks)
                            for (int p = 0; p < buffDef.PeriodicEffects.Length; p++)
                            {
                                var periodic = buffDef.PeriodicEffects[p];
                                float effectValue = periodic.Value * buff.CurrentStacks * tickCount;

                                // Apply periodic effect based on type
                                // Note: This is a simplified implementation - games may want to emit events instead
                                switch (periodic.Type)
                                {
                                    case PeriodicEffectType.Damage:
                                        // TODO: Apply damage to entity (emit damage event or modify health)
                                        // For now, this is a placeholder - games will implement damage application
                                        break;

                                    case PeriodicEffectType.Heal:
                                        // TODO: Apply healing to entity
                                        break;

                                    case PeriodicEffectType.Mana:
                                        // TODO: Restore mana
                                        break;

                                    case PeriodicEffectType.Stamina:
                                        // TODO: Restore stamina
                                        break;

                                    case PeriodicEffectType.ResourceGrant:
                                        // TODO: Grant resources
                                        break;
                                }
                            }
                        }
                    }

                    activeBuffs[i] = buff;
                }
            }
        }
    }
}

