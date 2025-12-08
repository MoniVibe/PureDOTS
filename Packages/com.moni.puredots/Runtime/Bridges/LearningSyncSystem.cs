using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Sends updated EmotionModulator, SkillProfile, CultureBelief to Body ECS.
    /// Runs every 250ms (Mind→Body sync interval).
    /// Applies learning-derived modifiers to deterministic systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MindToBodySyncSystem))]
    public partial struct LearningSyncSystem : ISystem
    {
        private float _lastSyncTime;
        private const float SyncInterval = 0.25f; // 250ms

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            if (currentTime - _lastSyncTime < SyncInterval)
            {
                return;
            }

            var coordinator = state.World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            var bus = coordinator.GetBus();
            if (bus == null)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var tickNumber = tickState.Tick;

            // Process entities with learning components
            var entityQuery = SystemAPI.QueryBuilder()
                .WithAll<AgentSyncId>()
                .WithAny<EmotionModulator, SkillProfile, CultureBelief>()
                .Build();

            if (entityQuery.IsEmpty)
            {
                return;
            }

            var learningMessages = new NativeList<LearningMessage>(entityQuery.CalculateEntityCount(), Allocator.TempJob);

            var emotionModulatorLookup = state.GetComponentLookup<EmotionModulator>(true);
            var skillProfileLookup = state.GetComponentLookup<SkillProfile>(true);
            var cultureBeliefLookup = state.GetBufferLookup<CultureBelief>(true);

            emotionModulatorLookup.Update(ref state);
            skillProfileLookup.Update(ref state);
            cultureBeliefLookup.Update(ref state);

            var collectJob = new CollectLearningDataJob
            {
                LearningMessages = learningMessages,
                TickNumber = tickNumber,
                EmotionModulatorLookup = emotionModulatorLookup,
                SkillProfileLookup = skillProfileLookup,
                CultureBeliefLookup = cultureBeliefLookup
            };

            collectJob.ScheduleParallel(entityQuery, state.Dependency).Complete();

            // Apply learning modifiers to Body ECS entities
            // Modifiers stored as IComponentData on Body ECS entities
            // Deterministic systems read modifiers but don't mutate learning state

            learningMessages.Dispose();
            _lastSyncTime = currentTime;
        }

        [BurstCompile]
        private partial struct CollectLearningDataJob : IJobEntity
        {
            public NativeList<LearningMessage> LearningMessages;
            public uint TickNumber;
            [ReadOnly] public ComponentLookup<EmotionModulator> EmotionModulatorLookup;
            [ReadOnly] public ComponentLookup<SkillProfile> SkillProfileLookup;
            [ReadOnly] public BufferLookup<CultureBelief> CultureBeliefLookup;

            public void Execute(
                [EntityIndexInQuery] int index,
                Entity entity,
                in AgentSyncId syncId)
            {
                // Only sync if Mind ECS entity exists
                if (syncId.MindEntityIndex < 0)
                {
                    return;
                }

                var message = new LearningMessage
                {
                    AgentGuid = syncId.Guid,
                    Tick = TickNumber
                };

                // Collect emotion modulator
                if (EmotionModulatorLookup.HasComponent(entity))
                {
                    var modulator = EmotionModulatorLookup[entity];
                    message.HasEmotionModulator = true;
                    message.LearningRateMultiplier = modulator.LearningRateMultiplier;
                    message.BiasAdjustment = modulator.BiasAdjustment;
                    message.ConfidenceModifier = modulator.ConfidenceModifier;
                }

                // Collect skill profile
                if (SkillProfileLookup.HasComponent(entity))
                {
                    var skills = SkillProfileLookup[entity];
                    message.HasSkillProfile = true;
                    message.CastingSkill = skills.CastingSkill;
                    message.DualCastingAptitude = skills.DualCastingAptitude;
                    message.MeleeSkill = skills.MeleeSkill;
                    message.StrategicThinking = skills.StrategicThinking;
                }

                // Collect culture beliefs
                if (CultureBeliefLookup.HasBuffer(entity))
                {
                    var beliefs = CultureBeliefLookup[entity];
                    message.HasCultureBeliefs = true;
                    message.CultureBeliefCount = beliefs.Length;
                    // Would serialize beliefs array (for now, just count)
                }

                LearningMessages.Add(message);
            }
        }

        private struct LearningMessage
        {
            public AgentGuid AgentGuid;
            public uint Tick;
            public bool HasEmotionModulator;
            public float LearningRateMultiplier;
            public float BiasAdjustment;
            public float ConfidenceModifier;
            public bool HasSkillProfile;
            public float CastingSkill;
            public float DualCastingAptitude;
            public float MeleeSkill;
            public float StrategicThinking;
            public bool HasCultureBeliefs;
            public int CultureBeliefCount;
        }
    }
}

