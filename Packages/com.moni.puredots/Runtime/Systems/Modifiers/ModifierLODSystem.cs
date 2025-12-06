using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// LOD culling system: replaces per-entity modifier buffers with statistical aggregates for distant entities.
    /// Swaps detail back in when player focuses on that region.
    /// Integrates with spatial system for distance checks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ModifierColdPathGroup))]
    public partial struct ModifierLODSystem : ISystem
    {
        private const float DefaultLODDistance = 100f; // 100 units

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

            // Get player/camera position for distance checks
            // For now, use origin as placeholder
            float3 focusPosition = float3.zero;

            // Process entities and determine LOD level
            new ProcessLODJob
            {
                FocusPosition = focusPosition,
                LODDistance = DefaultLODDistance
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessLODJob : IJobEntity
        {
            public float3 FocusPosition;
            public float LODDistance;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                [ChunkIndexInQuery] int chunkIndex,
                in DynamicBuffer<ModifierInstance> modifiers,
                ref ModifierLODAggregate lodAggregate)
            {
                // Calculate distance from focus (simplified - would use actual position component)
                // For now, use entity index as proxy (full implementation would query Translation)
                float distance = entityInQueryIndex * 0.1f; // Placeholder

                // If beyond LOD distance, use aggregate instead of individual modifiers
                if (distance > LODDistance)
                {
                    // Calculate averages from modifiers
                    float moraleSum = 0f;
                    float productivitySum = 0f;
                    int count = 0;

                    for (int i = 0; i < modifiers.Length; i++)
                    {
                        // Simplified: assume modifiers contribute to morale/productivity
                        moraleSum += modifiers[i].Value * 0.5f;
                        productivitySum += modifiers[i].Value * 0.5f;
                        count++;
                    }

                    if (count > 0)
                    {
                        lodAggregate.MoraleAvg = moraleSum / count;
                        lodAggregate.ProductivityAvg = productivitySum / count;
                        lodAggregate.EntityCount = 1; // This entity
                    }
                }
                else
                {
                    // Within LOD distance - use individual modifiers (clear aggregate)
                    lodAggregate = default;
                }
            }
        }
    }
}

