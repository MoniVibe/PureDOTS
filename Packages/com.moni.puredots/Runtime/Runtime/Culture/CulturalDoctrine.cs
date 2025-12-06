using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Culture
{
    /// <summary>
    /// Cultural doctrine archetype affecting formation behavior and morale.
    /// Stored as BlobAsset for zero-GC, read-only access.
    /// </summary>
    public struct CulturalDoctrineBlob
    {
        /// <summary>Archetype identifier (Corrupt Spiritualist, Zealot Paladin, etc.).</summary>
        public BlobString ArchetypeName;

        /// <summary>Soul harvest bias multiplier (converts enemy deaths to focus energy).</summary>
        public float SoulHarvestBias;

        /// <summary>Holy entity proximity morale bonus.</summary>
        public float HolyEntityMoraleBonus;

        /// <summary>Random formation deviation multiplier.</summary>
        public float DeviationMultiplier;

        /// <summary>Grudge condition flag (ignores morale decay vs grudge targets).</summary>
        public bool IgnoreMoraleDecayOnGrudge;

        /// <summary>Attack weight modifier based on dead enemies nearby.</summary>
        public float DeadEnemyAttackWeightBonus;
    }

    /// <summary>
    /// Reference to cultural doctrine blob asset.
    /// </summary>
    public struct CulturalDoctrineReference : IComponentData
    {
        public BlobAssetReference<CulturalDoctrineBlob> Doctrine;
    }
}

