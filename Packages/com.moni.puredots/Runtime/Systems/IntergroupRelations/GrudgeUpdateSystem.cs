using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.IntergroupRelations
{
    /// <summary>
    /// Updates grudge entries based on negative experiences.
    /// Uses exponential weighting: grudge[culture] = 1 - exp(-Anger * negativeEvents)
    /// Applies decay based on PrejudiceProfile (dwarves never forget, others decay).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GrudgeUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            var currentTick = timeState.Tick;

            // Process entities with grudge buffers and prejudice profiles
            foreach (var (grudgeBuffer, prejudiceProfile, emotionState) in SystemAPI.Query<DynamicBuffer<GrudgeEntry>, RefRO<PrejudiceProfile>, RefRO<EmotionState>>())
            {
                var decayRate = prejudiceProfile.ValueRO.DecayRate;
                var forgivenessFactor = prejudiceProfile.ValueRO.ForgivenessFactor;
                var neverForget = prejudiceProfile.ValueRO.NeverForget;
                var anger = emotionState.ValueRO.Anger;

                // Update each grudge entry
                for (int i = 0; i < grudgeBuffer.Length; i++)
                {
                    var grudge = grudgeBuffer[i];

                    // Apply decay (unless NeverForget flag is set)
                    if (!neverForget && decayRate > 0f)
                    {
                        // Exponential decay with forgiveness
                        grudge.GrudgeValue *= (1f - decayRate * forgivenessFactor);
                        grudge.GrudgeValue = math.max(0f, grudge.GrudgeValue);
                    }

                    // Update exponential weighting based on anger and negative events
                    if (grudge.NegativeEventCount > 0)
                    {
                        var exponentialGrudge = 1f - math.exp(-anger * grudge.NegativeEventCount);
                        grudge.GrudgeValue = math.max(grudge.GrudgeValue, exponentialGrudge);
                    }

                    grudge.LastUpdateTick = currentTick;
                    grudgeBuffer[i] = grudge;
                }
            }

            // Process ExperienceEvent buffers to add new grudges
            foreach (var (experienceBuffer, grudgeBuffer, prejudiceProfile, emotionState) in SystemAPI.Query<DynamicBuffer<ExperienceEvent>, DynamicBuffer<GrudgeEntry>, RefRO<PrejudiceProfile>, RefRO<EmotionState>>())
            {
                var anger = emotionState.ValueRO.Anger;

                // Process negative experiences (Outcome < 0)
                for (int i = 0; i < experienceBuffer.Length; i++)
                {
                    var experience = experienceBuffer[i];
                    
                    if (experience.Outcome < 0f)
                    {
                        var cultureId = experience.CultureId;

                        // Find or create grudge entry
                        int grudgeIndex = -1;
                        for (int j = 0; j < grudgeBuffer.Length; j++)
                        {
                            if (grudgeBuffer[j].CultureId == cultureId)
                            {
                                grudgeIndex = j;
                                break;
                            }
                        }

                        if (grudgeIndex >= 0)
                        {
                            // Update existing grudge
                            var grudge = grudgeBuffer[grudgeIndex];
                            grudge.NegativeEventCount++;
                            grudge.GrudgeValue = math.max(grudge.GrudgeValue, 1f - math.exp(-anger * grudge.NegativeEventCount));
                            grudge.LastUpdateTick = currentTick;
                            grudgeBuffer[grudgeIndex] = grudge;
                        }
                        else
                        {
                            // Create new grudge entry
                            grudgeBuffer.Add(new GrudgeEntry
                            {
                                CultureId = cultureId,
                                GrudgeValue = 1f - math.exp(-anger * 1f),
                                NegativeEventCount = 1,
                                LastUpdateTick = currentTick
                            });
                        }
                    }
                }
            }
        }
    }
}

