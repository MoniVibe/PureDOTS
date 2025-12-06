using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Spatial task assignment system with fairness coefficients.
    /// Uses weighted utility: distance × urgency × morale × fairness_coefficient
    /// Prevents high-speed/elite entities from starving slower ones.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    public partial struct SpatialTaskAssignmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialGridState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // This system provides fairness-aware task scoring
            // Actual task assignment happens in VillagerJobRequestSystem
            // This system can be extended to pre-filter or re-score candidates
        }

        /// <summary>
        /// Calculate fairness-weighted utility score for task assignment.
        /// Formula: (distance_weight × distance) + (urgency_weight × urgency) + (morale_weight × morale) × fairness_coefficient
        /// </summary>
        [BurstCompile]
        public static float CalculateFairnessWeightedScore(
            float distance,
            float urgency,
            float morale,
            ref RaceSpecBlob raceSpec)
        {
            // Normalize inputs (distance should be inverted - closer is better)
            var normalizedDistance = 1f / (1f + distance); // Closer = higher score
            var normalizedUrgency = math.clamp(urgency / 100f, 0f, 1f); // 0-1
            var normalizedMorale = math.clamp(morale / 100f, 0f, 1f); // 0-1

            // Weighted components
            var distanceComponent = normalizedDistance * raceSpec.DistanceWeight;
            var urgencyComponent = normalizedUrgency * raceSpec.UrgencyWeight;
            var moraleComponent = normalizedMorale * raceSpec.MoraleWeight;

            // Combined utility before fairness
            var baseUtility = distanceComponent + urgencyComponent + moraleComponent;

            // Apply fairness coefficient (higher = more fair, prevents elite starvation)
            var fairnessAdjusted = baseUtility * raceSpec.BaseFairnessCoefficient;

            // Apply speed multiplier penalty (faster entities get slight penalty to prevent starvation)
            var speedPenalty = 1f - (raceSpec.SpeedMultiplier - 1f) * 0.1f; // 10% penalty per speed multiplier above 1.0
            speedPenalty = math.clamp(speedPenalty, 0.5f, 1f); // Cap penalty at 50%

            var finalScore = fairnessAdjusted * speedPenalty;

            return finalScore;
        }

        /// <summary>
        /// Get race spec for an entity, or return default if not found.
        /// </summary>
        [BurstCompile]
        public static bool TryGetRaceSpec(
            Entity entity,
            ComponentLookup<RaceSpecReference> raceSpecLookup,
            out RaceSpecBlob raceSpec)
        {
            if (raceSpecLookup.HasComponent(entity))
            {
                ref var catalog = ref raceSpecLookup[entity].Catalog.Value;
                var index = raceSpecLookup[entity].RaceIndex;
                
                if (index >= 0 && index < catalog.Races.Length)
                {
                    raceSpec = catalog.Races[index];
                    return true;
                }
            }

            // Default race spec (fair, balanced)
            raceSpec = new RaceSpecBlob
            {
                BaseFairnessCoefficient = 1f,
                SpeedMultiplier = 1f,
                EliteBonus = 0f,
                MoraleWeight = 0.3f,
                UrgencyWeight = 0.4f,
                DistanceWeight = 0.3f
            };
            return false;
        }
    }
}

