using UnityEngine;

namespace PureDOTS.Authoring.Culture
{
    /// <summary>
    /// ScriptableObject asset for authoring cultural doctrines.
    /// </summary>
    [CreateAssetMenu(menuName = "PureDOTS/Culture/Cultural Doctrine", fileName = "CulturalDoctrine")]
    public class CulturalDoctrineAsset : ScriptableObject
    {
        [Header("Archetype")]
        public string archetypeName = "Default";

        [Header("Behavior Modifiers")]
        [Tooltip("Soul harvest bias multiplier (converts enemy deaths to focus energy)")]
        [Range(0f, 2f)]
        public float soulHarvestBias = 0f;

        [Tooltip("Holy entity proximity morale bonus")]
        [Range(0f, 1f)]
        public float holyEntityMoraleBonus = 0f;

        [Tooltip("Random formation deviation multiplier")]
        [Range(0f, 2f)]
        public float deviationMultiplier = 1f;

        [Tooltip("Ignore morale decay vs grudge targets")]
        public bool ignoreMoraleDecayOnGrudge = false;

        [Tooltip("Attack weight modifier based on dead enemies nearby")]
        [Range(0f, 1f)]
        public float deadEnemyAttackWeightBonus = 0f;
    }
}

