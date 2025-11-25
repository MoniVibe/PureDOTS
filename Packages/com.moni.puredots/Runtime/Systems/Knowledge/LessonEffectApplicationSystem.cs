using PureDOTS.Runtime.Knowledge;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Knowledge
{
    /// <summary>
    /// Aggregates lesson effects into LessonEffectCache component.
    /// Runs after LessonProgressionSystem to update bonuses when mastery changes.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(LessonProgressionSystem))]
    public partial struct LessonEffectApplicationSystem : ISystem
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

            // Get lesson catalog
            if (!SystemAPI.TryGetSingleton<LessonCatalogRef>(out var lessonCatalogRef) ||
                !lessonCatalogRef.Blob.IsCreated)
            {
                return;
            }

            var lessonCatalog = lessonCatalogRef.Blob.Value;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new AggregateLessonEffectsJob
            {
                LessonCatalog = lessonCatalog,
                CurrentTick = currentTick,
                Ecb = ecb
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct AggregateLessonEffectsJob : IJobEntity
        {
            [ReadOnly]
            public LessonDefinitionBlob LessonCatalog;

            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in DynamicBuffer<LessonMastery> lessonMastery,
                ref LessonEffectCache effectCache)
            {
                // Reset cache
                effectCache = new LessonEffectCache
                {
                    HarvestYieldMultiplier = 1f,
                    HarvestTimeMultiplier = 1f,
                    HarvestQualityBonus = 0f,
                    CraftingQualityBonus = 0f,
                    CraftingSpeedMultiplier = 1f,
                    CraftingEfficiencyBonus = 0f,
                    CombatDamageBonus = 0f,
                    CombatAccuracyBonus = 0f,
                    CombatDefenseBonus = 0f,
                    GeneralSkillBonus = 0f,
                    UnlockedSpellFlags = 0,
                    LastUpdateTick = CurrentTick
                };

                // Aggregate effects from all mastered lessons
                for (int i = 0; i < lessonMastery.Length; i++)
                {
                    var mastery = lessonMastery[i];

                    // Find lesson definition
                    LessonEntry lessonEntry = default;
                    bool foundLesson = false;
                    for (int j = 0; j < LessonCatalog.Lessons.Length; j++)
                    {
                        if (LessonCatalog.Lessons[j].LessonId.Equals(mastery.LessonId))
                        {
                            lessonEntry = LessonCatalog.Lessons[j];
                            foundLesson = true;
                            break;
                        }
                    }

                    if (!foundLesson)
                    {
                        continue;
                    }

                    // Apply effects that are unlocked at current tier or below
                    for (int j = 0; j < lessonEntry.Effects.Length; j++)
                    {
                        var effect = lessonEntry.Effects[j];
                        if (mastery.Tier >= effect.RequiredTier)
                        {
                            ApplyEffect(effect, ref effectCache);
                        }
                    }
                }
            }

            [BurstCompile]
            private void ApplyEffect(LessonEffect effect, ref LessonEffectCache cache)
            {
                switch (effect.Type)
                {
                    case LessonEffectType.YieldMultiplier:
                        cache.HarvestYieldMultiplier *= (1f + effect.Value);
                        break;

                    case LessonEffectType.QualityBonus:
                        cache.HarvestQualityBonus += effect.Value;
                        cache.CraftingQualityBonus += effect.Value;
                        break;

                    case LessonEffectType.SpeedBonus:
                        cache.HarvestTimeMultiplier *= (1f - effect.Value); // Reduction
                        cache.CraftingSpeedMultiplier *= (1f - effect.Value);
                        break;

                    case LessonEffectType.UnlockSpell:
                        // TODO: Set bit flag for spell unlock
                        // For now, just track that a spell was unlocked
                        break;

                    case LessonEffectType.UnlockRecipe:
                        // TODO: Track recipe unlocks
                        break;

                    case LessonEffectType.StatBonus:
                        // TODO: Apply stat bonus based on TargetId
                        break;

                    case LessonEffectType.SkillBonus:
                        cache.GeneralSkillBonus += effect.Value;
                        break;

                    case LessonEffectType.ResistanceBonus:
                        // TODO: Apply resistance bonus
                        break;

                    case LessonEffectType.HarvestTimeReduction:
                        cache.HarvestTimeMultiplier *= (1f - effect.Value);
                        break;

                    case LessonEffectType.CraftingEfficiency:
                        cache.CraftingEfficiencyBonus += effect.Value;
                        break;
                }
            }
        }
    }
}

