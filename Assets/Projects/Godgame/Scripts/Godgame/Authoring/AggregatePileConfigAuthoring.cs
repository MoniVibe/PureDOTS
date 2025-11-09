using Godgame.Resources;
using Unity.Entities;
using UnityEngine;

namespace Godgame.Authoring
{
    /// <summary>
    /// Provides tuning data for aggregate resource piles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AggregatePileConfigAuthoring : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField] private float defaultMaxCapacity = 2500f;
        [SerializeField] private float globalMaxCapacity = 5000f;
        [SerializeField] private int maxActivePiles = 200;

        [Header("Behaviour")]
        [SerializeField] private float mergeRadius = 2.5f;
        [SerializeField] private float mergeCheckSeconds = 5f;
        [SerializeField] private float splitThreshold = 2500f;
        [SerializeField] private float minSpawnAmount = 10f;
        [SerializeField] private float conservationEpsilon = 0.01f;

        private sealed class Baker : Baker<AggregatePileConfigAuthoring>
        {
            public override void Bake(AggregatePileConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AggregatePileConfig
                {
                    DefaultMaxCapacity = Mathf.Max(1f, authoring.defaultMaxCapacity),
                    GlobalMaxCapacity = Mathf.Max(authoring.defaultMaxCapacity, authoring.globalMaxCapacity),
                    MergeRadius = Mathf.Max(0.1f, authoring.mergeRadius),
                    SplitThreshold = Mathf.Max(1f, authoring.splitThreshold),
                    MergeCheckSeconds = Mathf.Max(0.1f, authoring.mergeCheckSeconds),
                    MinSpawnAmount = Mathf.Max(0.1f, authoring.minSpawnAmount),
                    ConservationEpsilon = Mathf.Max(0.0001f, authoring.conservationEpsilon),
                    MaxActivePiles = Mathf.Max(1, authoring.maxActivePiles)
                });

                AddComponent(entity, new AggregatePileRuntimeState
                {
                    NextMergeTime = 0f,
                    ActivePiles = 0
                });
            }
        }
    }
}
