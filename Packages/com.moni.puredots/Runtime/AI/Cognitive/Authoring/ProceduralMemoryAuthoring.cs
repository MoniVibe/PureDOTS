using PureDOTS.Runtime.AI.Cognitive;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring.Cognitive
{
    /// <summary>
    /// Authoring component for configuring procedural memory on agents.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralMemoryAuthoring : MonoBehaviour
    {
        [Header("Learning Configuration")]
        [Range(0.01f, 1.0f)]
        [Tooltip("Learning rate for reinforcement updates. Higher values adapt faster but may be less stable.")]
        public float learningRate = 0.1f;

        [Header("Memory Capacity")]
        [Range(1, 64)]
        [Tooltip("Maximum number of actions to remember per context.")]
        public int maxActionsPerContext = 8;
    }

    /// <summary>
    /// Baker for ProceduralMemoryAuthoring.
    /// </summary>
    public sealed class ProceduralMemoryBaker : Baker<ProceduralMemoryAuthoring>
    {
        public override void Bake(ProceduralMemoryAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent(entity, new ProceduralMemory
            {
                TriedActions = default,
                SuccessScores = default,
                ContextHash = 0,
                LearningRate = authoring.learningRate,
                LastUpdateTick = 0,
                SuccessChainCount = 0
            });

            // Add related components
            AddComponent(entity, new LimbicState
            {
                Curiosity = 0.5f,
                Fear = 0f,
                Frustration = 0f,
                RecentSuccessRate = 0.5f,
                RecentFailures = 0,
                LastEmotionUpdateTick = 0,
                StabilityThreshold = 0.1f,
                RecentActionWindow = 10
            });

            AddComponent(entity, new ContextHash
            {
                TerrainType = TerrainType.None,
                ObstacleTag = ObstacleTag.None,
                GoalType = GoalType.None,
                Hash = 0,
                LastComputedTick = 0
            });
        }
    }
}

