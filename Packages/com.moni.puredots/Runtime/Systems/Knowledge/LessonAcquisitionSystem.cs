using PureDOTS.Runtime.Knowledge;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Knowledge
{
    /// <summary>
    /// Processes lesson acquisition requests, validates prerequisites, and adds LessonMastery entries.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct LessonAcquisitionSystem : ISystem
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

            new ProcessAcquisitionRequestsJob
            {
                LessonCatalog = lessonCatalog,
                CurrentTick = currentTick,
                Ecb = ecb
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessAcquisitionRequestsJob : IJobEntity
        {
            [ReadOnly]
            public LessonDefinitionBlob LessonCatalog;

            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref DynamicBuffer<LessonAcquisitionRequest> requests,
                ref DynamicBuffer<LessonMastery> lessonMastery,
                ref DynamicBuffer<LessonAcquiredEvent> acquiredEvents)
            {
                for (int i = requests.Length - 1; i >= 0; i--)
                {
                    var request = requests[i];
                    bool acquired = false;

                    // Find lesson definition
                    LessonEntry lessonEntry = default;
                    bool foundLesson = false;
                    for (int j = 0; j < LessonCatalog.Lessons.Length; j++)
                    {
                        if (LessonCatalog.Lessons[j].LessonId.Equals(request.LessonId))
                        {
                            lessonEntry = LessonCatalog.Lessons[j];
                            foundLesson = true;
                            break;
                        }
                    }

                    if (!foundLesson)
                    {
                        requests.RemoveAt(i);
                        continue; // Invalid lesson
                    }

                    // Check if already learned
                    bool alreadyLearned = false;
                    for (int j = 0; j < lessonMastery.Length; j++)
                    {
                        if (lessonMastery[j].LessonId.Equals(request.LessonId))
                        {
                            alreadyLearned = true;
                            break;
                        }
                    }

                    if (alreadyLearned)
                    {
                        requests.RemoveAt(i);
                        continue; // Already learned
                    }

                    // Validate prerequisites
                    if (!ValidateLessonPrerequisites(entity, lessonEntry))
                    {
                        requests.RemoveAt(i);
                        continue; // Prerequisites not met
                    }

                    // Add lesson mastery at Novice tier
                    lessonMastery.Add(new LessonMastery
                    {
                        LessonId = request.LessonId,
                        Tier = MasteryTier.Novice,
                        TierProgress = 0f,
                        TotalXp = 0f,
                        LastProgressTick = CurrentTick
                    });

                    // Emit event
                    acquiredEvents.Add(new LessonAcquiredEvent
                    {
                        LessonId = request.LessonId,
                        Entity = entity,
                        TeacherEntity = request.TeacherEntity,
                        Source = request.Source,
                        AcquiredTick = CurrentTick
                    });

                    acquired = true;
                    requests.RemoveAt(i);
                }
            }

            [BurstCompile]
            private bool ValidateLessonPrerequisites(Entity entity, LessonEntry lesson)
            {
                // Check enlightenment requirement
                if (lesson.RequiredEnlightenment > 0)
                {
                    if (SystemAPI.HasComponent<Enlightenment>(entity))
                    {
                        var enlightenment = SystemAPI.GetComponent<Enlightenment>(entity);
                        if (enlightenment.Level < lesson.RequiredEnlightenment)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                // Check prerequisites
                if (!SystemAPI.HasBuffer<LessonMastery>(entity))
                {
                    // No lessons learned yet, can only learn lessons with no prerequisites
                    return lesson.Prerequisites.Length == 0;
                }

                var lessonMastery = SystemAPI.GetBuffer<LessonMastery>(entity);

                for (int i = 0; i < lesson.Prerequisites.Length; i++)
                {
                    var prereq = lesson.Prerequisites[i];
                    bool met = false;

                    switch (prereq.Type)
                    {
                        case LessonPrerequisiteType.Lesson:
                            // Check if prerequisite lesson is mastered to required tier
                            for (int j = 0; j < lessonMastery.Length; j++)
                            {
                                if (lessonMastery[j].LessonId.Equals(prereq.TargetId))
                                {
                                    if (lessonMastery[j].Tier >= prereq.RequiredTier)
                                    {
                                        met = true;
                                        break;
                                    }
                                }
                            }
                            break;

                        case LessonPrerequisiteType.Spell:
                            // TODO: Check if spell is learned
                            met = true; // Placeholder
                            break;

                        case LessonPrerequisiteType.Skill:
                            // TODO: Check skill level
                            met = true; // Placeholder
                            break;

                        case LessonPrerequisiteType.Attribute:
                            // TODO: Check attribute level
                            met = true; // Placeholder
                            break;

                        case LessonPrerequisiteType.Enlightenment:
                            if (SystemAPI.HasComponent<Enlightenment>(entity))
                            {
                                var enlightenment = SystemAPI.GetComponent<Enlightenment>(entity);
                                if (enlightenment.Level >= prereq.RequiredLevel)
                                {
                                    met = true;
                                }
                            }
                            break;

                        case LessonPrerequisiteType.Culture:
                            // TODO: Check culture membership
                            met = true; // Placeholder
                            break;
                    }

                    if (!met)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}

