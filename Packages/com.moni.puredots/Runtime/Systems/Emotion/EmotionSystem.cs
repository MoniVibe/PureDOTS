using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Emotion
{
    /// <summary>
    /// Emotion system handling emotion decay and updates.
    /// Processes interaction digests to update emotion state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct EmotionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Update emotions from interactions
            var emotionQuery = state.GetEntityQuery(
                typeof(EmotionState),
                typeof(InteractionDigest));

            if (emotionQuery.IsEmpty)
            {
                return;
            }

            var job = new UpdateEmotionsJob
            {
                CurrentTick = tickState.Tick,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.ScheduleParallel(emotionQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct UpdateEmotionsJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;

            public void Execute(
                ref EmotionState emotion,
                in DynamicBuffer<InteractionDigest> interactions)
            {
                // Process interactions and update emotions
                for (int i = 0; i < interactions.Length; i++)
                {
                    var interaction = interactions[i];
                    
                    // Apply weighted decay
                    var age = CurrentTick - interaction.InteractionTick;
                    var decayFactor = math.exp(-emotion.DecayRate * age);
                    var weight = interaction.Weight * decayFactor;

                    // Update emotions based on interaction type
                    switch (interaction.Type)
                    {
                        case InteractionType.Help:
                            emotion.Happiness += interaction.PositiveDelta * weight;
                            emotion.Trust += interaction.PositiveDelta * weight * 0.5f;
                            break;
                        case InteractionType.Harm:
                            emotion.Fear += interaction.NegativeDelta * weight;
                            emotion.Anger += interaction.NegativeDelta * weight;
                            emotion.Trust -= interaction.NegativeDelta * weight;
                            break;
                        case InteractionType.Social:
                            emotion.Happiness += interaction.PositiveDelta * weight * 0.5f;
                            break;
                    }
                }

                // Clamp emotions to valid range
                emotion.Happiness = math.clamp(emotion.Happiness, 0f, 1f);
                emotion.Fear = math.clamp(emotion.Fear, 0f, 1f);
                emotion.Anger = math.clamp(emotion.Anger, 0f, 1f);
                emotion.Trust = math.clamp(emotion.Trust, 0f, 1f);

                // Apply natural decay
                emotion.Happiness = math.max(0f, emotion.Happiness - emotion.DecayRate * DeltaTime);
                emotion.Fear = math.max(0f, emotion.Fear - emotion.DecayRate * DeltaTime);
                emotion.Anger = math.max(0f, emotion.Anger - emotion.DecayRate * DeltaTime);

                emotion.LastUpdateTick = CurrentTick;
            }
        }
    }
}

