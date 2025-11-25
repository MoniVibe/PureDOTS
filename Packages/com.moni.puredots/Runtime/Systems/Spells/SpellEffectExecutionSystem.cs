using PureDOTS.Runtime.Buffs;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Spells;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Spells
{
    /// <summary>
    /// Processes SpellCastEvent buffer and applies spell effects (damage, healing, buffs, etc.).
    /// Runs after combat systems to apply spell effects.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CombatSystemGroup))]
    public partial struct SpellEffectExecutionSystem : ISystem
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

            // Get spell catalog
            if (!SystemAPI.TryGetSingleton<SpellCatalogRef>(out var spellCatalogRef) ||
                !spellCatalogRef.Blob.IsCreated)
            {
                return;
            }

            var spellCatalog = spellCatalogRef.Blob.Value;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProcessSpellEffectsJob
            {
                SpellCatalog = spellCatalog,
                CurrentTick = currentTick,
                Ecb = ecb
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessSpellEffectsJob : IJobEntity
        {
            [ReadOnly]
            public SpellDefinitionBlob SpellCatalog;

            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity casterEntity,
                DynamicBuffer<SpellCastEvent> castEvents,
                ref DynamicBuffer<DamageEvent> damageEvents,
                ref DynamicBuffer<HealEvent> healEvents,
                ref DynamicBuffer<BuffApplicationRequest> buffRequests)
            {
                for (int i = 0; i < castEvents.Length; i++)
                {
                    var castEvent = castEvents[i];

                    if (castEvent.Result != SpellCastResult.Success)
                    {
                        continue; // Skip failed casts
                    }

                    // Find spell definition
                    SpellEntry spellEntry = default;
                    bool foundSpell = false;
                    for (int j = 0; j < SpellCatalog.Spells.Length; j++)
                    {
                        if (SpellCatalog.Spells[j].SpellId.Equals(castEvent.SpellId))
                        {
                            spellEntry = SpellCatalog.Spells[j];
                            foundSpell = true;
                            break;
                        }
                    }

                    if (!foundSpell)
                    {
                        continue;
                    }

                    // Apply each effect
                    for (int j = 0; j < spellEntry.Effects.Length; j++)
                    {
                        var effect = spellEntry.Effects[j];
                        float effectiveValue = effect.BaseValue * (1f + effect.ScalingFactor * castEvent.EffectiveStrength);

                        ApplySpellEffect(
                            casterEntity,
                            castEvent,
                            effect,
                            effectiveValue,
                            ref damageEvents,
                            ref healEvents,
                            ref buffRequests,
                            Ecb);
                    }
                }

                // Clear processed events
                castEvents.Clear();
            }

            [BurstCompile]
            private void ApplySpellEffect(
                Entity casterEntity,
                SpellCastEvent castEvent,
                SpellEffect effect,
                float effectiveValue,
                ref DynamicBuffer<DamageEvent> damageEvents,
                ref DynamicBuffer<HealEvent> healEvents,
                ref DynamicBuffer<BuffApplicationRequest> buffRequests,
                EntityCommandBuffer.ParallelWriter ecb)
            {
                Entity targetEntity = castEvent.TargetEntity;
                float3 targetPosition = castEvent.TargetPosition;

                switch (effect.Type)
                {
                    case SpellEffectType.Damage:
                        // Create damage event
                        if (targetEntity != Entity.Null && SystemAPI.Exists(targetEntity))
                        {
                            if (SystemAPI.HasBuffer<DamageEvent>(targetEntity))
                            {
                                var targetDamageEvents = SystemAPI.GetBuffer<DamageEvent>(targetEntity);
                                targetDamageEvents.Add(new DamageEvent
                                {
                                    SourceEntity = casterEntity,
                                    TargetEntity = targetEntity,
                                    RawDamage = effectiveValue,
                                    Type = DamageType.Fire, // TODO: Determine from spell school
                                    Tick = CurrentTick,
                                    Flags = DamageFlags.None
                                });
                            }
                            else
                            {
                                // Create buffer if needed
                                ecb.AddBuffer<DamageEvent>(targetEntity.Index, targetEntity);
                            }
                        }
                        break;

                    case SpellEffectType.Heal:
                        // Create heal event
                        if (targetEntity != Entity.Null && SystemAPI.Exists(targetEntity))
                        {
                            if (SystemAPI.HasBuffer<HealEvent>(targetEntity))
                            {
                                var targetHealEvents = SystemAPI.GetBuffer<HealEvent>(targetEntity);
                                targetHealEvents.Add(new HealEvent
                                {
                                    SourceEntity = casterEntity,
                                    TargetEntity = targetEntity,
                                    Amount = effectiveValue,
                                    Tick = CurrentTick
                                });
                            }
                            else
                            {
                                ecb.AddBuffer<HealEvent>(targetEntity.Index, targetEntity);
                            }
                        }
                        break;

                    case SpellEffectType.ApplyBuff:
                        // Create buff application request
                        if (targetEntity != Entity.Null && SystemAPI.Exists(targetEntity))
                        {
                            buffRequests.Add(new BuffApplicationRequest
                            {
                                BuffId = effect.BuffId,
                                SourceEntity = casterEntity,
                                DurationOverride = effect.Duration > 0f ? effect.Duration : 0f,
                                StacksToApply = 1
                            });
                        }
                        break;

                    case SpellEffectType.ApplyDebuff:
                        // Same as buff, but debuff category
                        if (targetEntity != Entity.Null && SystemAPI.Exists(targetEntity))
                        {
                            buffRequests.Add(new BuffApplicationRequest
                            {
                                BuffId = effect.BuffId,
                                SourceEntity = casterEntity,
                                DurationOverride = effect.Duration > 0f ? effect.Duration : 0f,
                                StacksToApply = 1
                            });
                        }
                        break;

                    case SpellEffectType.Dispel:
                        // TODO: Implement dispel logic
                        break;

                    case SpellEffectType.Shield:
                        // TODO: Apply shield to target
                        break;

                    case SpellEffectType.Summon:
                        // TODO: Spawn summoned entity
                        break;

                    case SpellEffectType.Teleport:
                        // TODO: Teleport target to position
                        break;

                    case SpellEffectType.ResourceGrant:
                        // TODO: Grant resources
                        break;

                    case SpellEffectType.StatModify:
                        // TODO: Modify stats temporarily
                        break;
                }
            }
        }
    }
}

