using PureDOTS.Runtime.Buffs;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Knowledge;
using PureDOTS.Runtime.Spells;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Spells
{
    /// <summary>
    /// Processes spell cast requests, validates prerequisites, manages cast state, and applies cooldowns.
    /// Integrates with lesson system for prerequisite checks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct SpellCastingSystem : ISystem
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
            var deltaTime = timeState.DeltaTime;

            // Get spell catalog
            if (!SystemAPI.TryGetSingleton<SpellCatalogRef>(out var spellCatalogRef) ||
                !spellCatalogRef.Blob.IsCreated)
            {
                return;
            }

            var spellCatalog = spellCatalogRef.Blob.Value;

            // Get lesson catalog for prerequisite checks
            var lessonCatalogRef = SystemAPI.GetSingleton<LessonCatalogRef>();
            var lessonCatalog = lessonCatalogRef.Blob.IsCreated ? lessonCatalogRef.Blob.Value : default;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Process cast requests
            new ProcessCastRequestsJob
            {
                SpellCatalog = spellCatalog,
                LessonCatalog = lessonCatalog,
                CurrentTick = currentTick,
                DeltaTime = deltaTime,
                Ecb = ecb
            }.ScheduleParallel();

            // Update active casts
            new UpdateActiveCastsJob
            {
                SpellCatalog = spellCatalog,
                CurrentTick = currentTick,
                DeltaTime = deltaTime,
                Ecb = ecb
            }.ScheduleParallel();

            // Update cooldowns
            new UpdateCooldownsJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessCastRequestsJob : IJobEntity
        {
            [ReadOnly]
            public SpellDefinitionBlob SpellCatalog;

            [ReadOnly]
            public LessonDefinitionBlob LessonCatalog;

            public uint CurrentTick;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref SpellCastRequest request,
                ref SpellCastState castState,
                ref SpellMana mana,
                in SpellCaster caster,
                in DynamicBuffer<LearnedSpell> learnedSpells,
                in DynamicBuffer<SpellCooldown> cooldowns,
                in DynamicBuffer<LessonMastery> lessonMastery,
                in DynamicBuffer<ExtendedSpellMastery> extendedMastery)
            {
                // Skip if already casting
                if (castState.Phase != SpellCastPhase.Idle)
                {
                    // Queue if requested
                    if ((request.Flags & SpellCastRequestFlags.QueueIfBusy) != 0)
                    {
                        // TODO: Implement queue system
                    }
                    else if ((request.Flags & SpellCastRequestFlags.ForceInterrupt) != 0)
                    {
                        // Interrupt current cast
                        castState.Phase = SpellCastPhase.Idle;
                        castState.ActiveSpellId = default;
                    }
                    else
                    {
                        return; // Busy, can't cast
                    }
                }

                // Find spell definition
                SpellEntry spellEntry = default;
                bool foundSpell = false;
                for (int i = 0; i < SpellCatalog.Spells.Length; i++)
                {
                    if (SpellCatalog.Spells[i].SpellId.Equals(request.SpellId))
                    {
                        spellEntry = SpellCatalog.Spells[i];
                        foundSpell = true;
                        break;
                    }
                }

                if (!foundSpell)
                {
                    return; // Invalid spell
                }

                // Check if spell is learned (either LearnedSpell or ExtendedSpellMastery)
                bool isLearned = false;
                byte masteryLevel = 0;
                float extendedMastery = 0f;
                
                // Check LearnedSpell first (legacy)
                for (int i = 0; i < learnedSpells.Length; i++)
                {
                    if (learnedSpells[i].SpellId.Equals(request.SpellId))
                    {
                        isLearned = true;
                        masteryLevel = learnedSpells[i].MasteryLevel;
                        break;
                    }
                }

                // Check ExtendedSpellMastery (new system)
                if (SystemAPI.HasBuffer<ExtendedSpellMastery>(entity))
                {
                    var extendedMasteryBuffer = SystemAPI.GetBuffer<ExtendedSpellMastery>(entity);
                    for (int i = 0; i < extendedMasteryBuffer.Length; i++)
                    {
                        if (extendedMasteryBuffer[i].SpellId.Equals(request.SpellId))
                        {
                            isLearned = true;
                            extendedMastery = extendedMasteryBuffer[i].MasteryProgress;
                            break;
                        }
                    }
                }

                if (!isLearned)
                {
                    return; // Spell not learned
                }

                // Check if can attempt cast (requires 20% mastery minimum)
                if (extendedMastery > 0f && !SpellMasteryUtility.CanAttemptCast(extendedMastery))
                {
                    return; // Cannot cast yet (below 20% mastery)
                }

                // Check prerequisites (lessons, enlightenment, etc.)
                if (!ValidatePrerequisites(entity, spellEntry, lessonMastery, lessonCatalog))
                {
                    return; // Prerequisites not met
                }

                // Check cooldown
                bool onCooldown = false;
                for (int i = 0; i < cooldowns.Length; i++)
                {
                    if (cooldowns[i].SpellId.Equals(request.SpellId) && cooldowns[i].RemainingTime > 0f)
                    {
                        onCooldown = true;
                        break;
                    }
                }

                if (onCooldown)
                {
                    return; // On cooldown
                }

                // Check mana cost (reduced by mastery)
                float effectiveCost = spellEntry.ManaCost * (1f - (masteryLevel / 255f * 0.3f)); // Up to 30% reduction
                effectiveCost *= mana.CostModifier;

                if (mana.Current < effectiveCost)
                {
                    return; // Not enough mana
                }

                // Consume mana
                mana.Current -= effectiveCost;
                mana.LastRegenTick = CurrentTick;

                // Start casting
                castState.ActiveSpellId = request.SpellId;
                castState.TargetEntity = request.TargetEntity;
                castState.TargetPosition = request.TargetPosition;
                castState.CastProgress = 0f;
                castState.ChargeLevel = 0f;
                castState.Phase = spellEntry.CastTime > 0f ? SpellCastPhase.Preparing : SpellCastPhase.Casting;
                castState.CastStartTick = CurrentTick;

                // Set flags based on cast type
                if (spellEntry.CastType == SpellCastType.Channeled)
                {
                    castState.Flags |= SpellCastFlags.Interruptible;
                    castState.Flags |= SpellCastFlags.Concentrating;
                }
                if (spellEntry.CastType == SpellCastType.Instant)
                {
                    castState.Flags |= SpellCastFlags.MovementLocked;
                }

                // Clear request
                request.SpellId = default;
            }

            [BurstCompile]
            private bool ValidatePrerequisites(
                Entity entity,
                SpellEntry spell,
                DynamicBuffer<LessonMastery> lessonMastery,
                LessonDefinitionBlob lessonCatalog)
            {
                // Check enlightenment requirement
                if (SystemAPI.HasComponent<Enlightenment>(entity))
                {
                    var enlightenment = SystemAPI.GetComponent<Enlightenment>(entity);
                    if (enlightenment.Level < spell.RequiredEnlightenment)
                    {
                        return false;
                    }
                }

                // Check spell prerequisites
                for (int i = 0; i < spell.Prerequisites.Length; i++)
                {
                    var prereq = spell.Prerequisites[i];
                    if (prereq.Type == PrerequisiteType.Lesson)
                    {
                        // Check if lesson is mastered to required tier
                        bool hasLesson = false;
                        for (int j = 0; j < lessonMastery.Length; j++)
                        {
                            if (lessonMastery[j].LessonId.Equals(prereq.TargetId))
                            {
                                if (lessonMastery[j].Tier >= (MasteryTier)prereq.RequiredLevel)
                                {
                                    hasLesson = true;
                                    break;
                                }
                            }
                        }
                        if (!hasLesson)
                        {
                            return false;
                        }
                    }
                    // TODO: Check other prerequisite types (spell, skill, attribute)
                }

                return true;
            }
        }

        [BurstCompile]
        public partial struct UpdateActiveCastsJob : IJobEntity
        {
            [ReadOnly]
            public SpellDefinitionBlob SpellCatalog;

            public uint CurrentTick;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref SpellCastState castState,
                in SpellCaster caster,
                in DynamicBuffer<LearnedSpell> learnedSpells,
                in DynamicBuffer<ExtendedSpellMastery> extendedMastery,
                ref DynamicBuffer<SpellCastEvent> castEvents)
            {
                if (castState.Phase == SpellCastPhase.Idle)
                {
                    return;
                }

                // Find spell definition
                SpellEntry spellEntry = default;
                bool foundSpell = false;
                for (int i = 0; i < SpellCatalog.Spells.Length; i++)
                {
                    if (SpellCatalog.Spells[i].SpellId.Equals(castState.ActiveSpellId))
                    {
                        spellEntry = SpellCatalog.Spells[i];
                        foundSpell = true;
                        break;
                    }
                }

                if (!foundSpell)
                {
                    castState.Phase = SpellCastPhase.Idle;
                    return;
                }

                // Get mastery level (prefer ExtendedSpellMastery, fallback to LearnedSpell)
                byte masteryLevel = 0;
                float extendedMastery = 0f;
                
                // Check ExtendedSpellMastery first
                for (int i = 0; i < extendedMastery.Length; i++)
                {
                    if (extendedMastery[i].SpellId.Equals(castState.ActiveSpellId))
                    {
                        extendedMastery = extendedMastery[i].MasteryProgress;
                        masteryLevel = (byte)math.clamp(extendedMastery * 255f, 0f, 255f); // Convert to legacy format
                        break;
                    }
                }
                
                // Fallback to LearnedSpell if no extended mastery
                if (extendedMastery == 0f)
                {
                    for (int i = 0; i < learnedSpells.Length; i++)
                    {
                        if (learnedSpells[i].SpellId.Equals(castState.ActiveSpellId))
                        {
                            masteryLevel = learnedSpells[i].MasteryLevel;
                            extendedMastery = masteryLevel / 255f; // Convert to 0-1 range
                            break;
                        }
                    }
                }

                // Update cast progress based on phase
                switch (castState.Phase)
                {
                    case SpellCastPhase.Preparing:
                        // Advance preparation
                        float castSpeed = caster.CastSpeedModifier * (1f + masteryLevel / 255f * 0.5f); // Mastery speeds up cast
                        castState.CastProgress += DeltaTime / spellEntry.CastTime * castSpeed;

                        if (castState.CastProgress >= 1f)
                        {
                            castState.Phase = SpellCastPhase.Casting;
                            castState.CastProgress = 1f;
                        }
                        break;

                    case SpellCastPhase.Casting:
                        // Spell activates - check success chance based on mastery
                        SpellCastResult castResult = SpellCastResult.Success;
                        
                        if (extendedMastery > 0f)
                        {
                            // Use extended mastery success chance
                            float successChance = SpellMasteryUtility.GetSuccessChance(extendedMastery);
                            
                            // Roll for success (deterministic RNG)
                            var rng = Unity.Mathematics.Random.CreateFromIndex((uint)(entity.Index + CurrentTick));
                            float roll = rng.NextFloat();
                            
                            if (roll > successChance)
                            {
                                castResult = SpellCastResult.Fizzled; // Failed due to low mastery
                            }
                        }
                        
                        castEvents.Add(new SpellCastEvent
                        {
                            SpellId = castState.ActiveSpellId,
                            CasterEntity = entity,
                            TargetEntity = castState.TargetEntity,
                            TargetPosition = castState.TargetPosition,
                            EffectiveStrength = extendedMastery > 0f ? extendedMastery : (masteryLevel / 255f),
                            CastTick = CurrentTick,
                            Result = castResult
                        });

                        // Increment cast count
                        // Note: This would need to be done via ECB or separate system

                        // Move to cooldown or channeling
                        if (spellEntry.CastType == SpellCastType.Channeled)
                        {
                            castState.Phase = SpellCastPhase.Channeling;
                        }
                        else
                        {
                            castState.Phase = SpellCastPhase.Cooldown;
                            castState.CastProgress = 0f;
                        }
                        break;

                    case SpellCastPhase.Channeling:
                        // Maintain channel (periodic effects handled by SpellEffectExecutionSystem)
                        // Channel can be interrupted
                        break;

                    case SpellCastPhase.Releasing:
                        // Release charged spell - check success chance
                        SpellCastResult releaseResult = SpellCastResult.Success;
                        
                        if (extendedMastery > 0f)
                        {
                            float successChance = SpellMasteryUtility.GetSuccessChance(extendedMastery);
                            var rng = Unity.Mathematics.Random.CreateFromIndex((uint)(entity.Index + CurrentTick));
                            float roll = rng.NextFloat();
                            
                            if (roll > successChance)
                            {
                                releaseResult = SpellCastResult.Fizzled;
                            }
                        }
                        
                        castEvents.Add(new SpellCastEvent
                        {
                            SpellId = castState.ActiveSpellId,
                            CasterEntity = entity,
                            TargetEntity = castState.TargetEntity,
                            TargetPosition = castState.TargetPosition,
                            EffectiveStrength = castState.ChargeLevel * (extendedMastery > 0f ? extendedMastery : (masteryLevel / 255f)),
                            CastTick = CurrentTick,
                            Result = releaseResult
                        });

                        castState.Phase = SpellCastPhase.Cooldown;
                        castState.CastProgress = 0f;
                        break;

                    case SpellCastPhase.Cooldown:
                        // Cooldown handled by UpdateCooldownsJob
                        break;
                }
            }
        }

        [BurstCompile]
        public partial struct UpdateCooldownsJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(ref DynamicBuffer<SpellCooldown> cooldowns)
            {
                for (int i = cooldowns.Length - 1; i >= 0; i--)
                {
                    var cooldown = cooldowns[i];
                    cooldown.RemainingTime -= DeltaTime;

                    if (cooldown.RemainingTime <= 0f)
                    {
                        cooldowns.RemoveAt(i);
                    }
                    else
                    {
                        cooldowns[i] = cooldown;
                    }
                }
            }
        }
    }
}

