using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.IntergroupRelations;

namespace PureDOTS.Systems.IntergroupRelations
{
    /// <summary>
    /// Updates culture belief vectors on leaders/captains based on observed traits.
    /// Belief updates: belief[culture] = lerp(belief, observedTrait, LearningRate)
    /// Used for predicting enemy tactics and adjusting diplomacy.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CultureMemoryGraphSystem))]
    public partial struct CultureBeliefUpdateSystem : ISystem
    {
        private const float DefaultLearningRate = 0.1f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            var currentTick = timeState.Tick;

            // Process entities with CultureBelief buffers (leaders/captains)
            var cultureProfileLookup = state.GetComponentLookup<CultureProfile>(true);
            cultureProfileLookup.Update(ref state);

            foreach (var (beliefBuffer, memoryProfile) in SystemAPI.Query<DynamicBuffer<CultureBelief>, RefRO<MemoryProfile>>())
            {
                var learningRate = memoryProfile.ValueRO.LearningRate;

                // Update each belief based on observed culture profiles
                for (int i = 0; i < beliefBuffer.Length; i++)
                {
                    var belief = beliefBuffer[i];
                    var cultureId = belief.CultureId;

                    // Find observed culture profile
                    var profileQuery = SystemAPI.QueryBuilder()
                        .WithAll<CultureProfile>()
                        .Build();

                    float observedTrait = 0.5f; // Default neutral
                    bool foundProfile = false;

                    foreach (var profile in profileQuery.ToComponentDataArray<CultureProfile>(Allocator.Temp))
                    {
                        if (profile.Id == cultureId)
                        {
                            // Use trustworthiness as observed trait (can be extended)
                            observedTrait = profile.Trustworthiness;
                            foundProfile = true;
                            break;
                        }
                    }

                    if (foundProfile)
                    {
                        // Update belief: lerp(belief, observedTrait, LearningRate)
                        var newBelief = math.lerp(belief.BeliefValue, observedTrait, learningRate);
                        var newConfidence = math.min(belief.Confidence + learningRate * 0.1f, 1f);

                        belief.BeliefValue = newBelief;
                        belief.Confidence = newConfidence;
                        belief.LastUpdateTick = currentTick;

                        beliefBuffer[i] = belief;
                    }
                }
            }
        }
    }
}

