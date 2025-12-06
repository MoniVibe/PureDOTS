using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Spatial;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring.Cognitive
{
    /// <summary>
    /// Authoring component for tagging objects with affordances.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AffordanceAuthoring : MonoBehaviour
    {
        [Header("Affordance Type")]
        [Tooltip("Type of affordance this object provides.")]
        public AffordanceType affordanceType = AffordanceType.None;

        [Header("Effort & Reward")]
        [Range(0.01f, 1.0f)]
        [Tooltip("Effort required to perform action (0.0 = easy, 1.0 = very difficult).")]
        public float effort = 0.5f;

        [Range(0.0f, 1.0f)]
        [Tooltip("Potential reward from performing action (0.0 = none, 1.0 = high reward).")]
        public float rewardPotential = 0.5f;
    }

    /// <summary>
    /// Baker for AffordanceAuthoring.
    /// </summary>
    public sealed class AffordanceBaker : Baker<AffordanceAuthoring>
    {
        public override void Bake(AffordanceAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent(entity, new Affordance
            {
                Type = authoring.affordanceType,
                Effort = authoring.effort,
                RewardPotential = authoring.rewardPotential,
                ObjectEntity = entity
            });

            // Ensure object is spatial indexed for affordance detection
            AddComponent<SpatialIndexedTag>(entity);
        }
    }
}

