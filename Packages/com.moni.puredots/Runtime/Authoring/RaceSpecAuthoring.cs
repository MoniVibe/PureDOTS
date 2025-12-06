using System.Collections.Generic;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring
{
    /// <summary>
    /// Authoring component for race specifications.
    /// Creates RaceSpecBlob assets for fairness coefficients.
    /// </summary>
    public class RaceSpecAuthoring : MonoBehaviour
    {
        [Tooltip("Race identifier")]
        public string raceId = "Default";

        [Tooltip("Base fairness coefficient (0-1, higher = more fair)")]
        [Range(0f, 1f)]
        public float baseFairnessCoefficient = 1f;

        [Tooltip("Movement speed multiplier")]
        [Min(0.1f)]
        public float speedMultiplier = 1f;

        [Tooltip("Bonus for elite entities (0-1)")]
        [Range(0f, 1f)]
        public float eliteBonus = 0f;

        [Tooltip("How much morale affects task assignment (0-1)")]
        [Range(0f, 1f)]
        public float moraleWeight = 0.3f;

        [Tooltip("How much urgency affects task assignment (0-1)")]
        [Range(0f, 1f)]
        public float urgencyWeight = 0.4f;

        [Tooltip("How much distance affects task assignment (0-1)")]
        [Range(0f, 1f)]
        public float distanceWeight = 0.3f;
    }

    /// <summary>
    /// Baker for RaceSpecAuthoring that creates RaceSpecBlob assets.
    /// </summary>
    public class RaceSpecBaker : Baker<RaceSpecAuthoring>
    {
        public override void Bake(RaceSpecAuthoring authoring)
        {
            // Race specs are typically stored in a catalog singleton
            // For now, we'll create a component reference
            // Full catalog implementation would be in a separate catalog authoring
        }
    }

    /// <summary>
    /// ScriptableObject asset for race specification catalog.
    /// </summary>
    [CreateAssetMenu(menuName = "PureDOTS/Race Spec Catalog", fileName = "RaceSpecCatalog")]
    public class RaceSpecCatalogAsset : ScriptableObject
    {
        [Tooltip("List of race specifications")]
        public List<RaceSpecData> races = new List<RaceSpecData>();
    }

    /// <summary>
    /// Serializable race specification data.
    /// </summary>
    [System.Serializable]
    public class RaceSpecData
    {
        public string raceId = "Default";
        [Range(0f, 1f)] public float baseFairnessCoefficient = 1f;
        [Min(0.1f)] public float speedMultiplier = 1f;
        [Range(0f, 1f)] public float eliteBonus = 0f;
        [Range(0f, 1f)] public float moraleWeight = 0.3f;
        [Range(0f, 1f)] public float urgencyWeight = 0.4f;
        [Range(0f, 1f)] public float distanceWeight = 0.3f;
    }
}

