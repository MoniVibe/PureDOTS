using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for race/species fairness coefficients.
    /// Stores fairness tuning parameters for spatial task allocation.
    /// </summary>
    public struct RaceSpecBlob
    {
        public BlobString RaceId; // Identifier for this race/species
        public float BaseFairnessCoefficient; // Base fairness (0-1, higher = more fair)
        public float SpeedMultiplier; // Movement speed multiplier
        public float EliteBonus; // Bonus for elite entities (0-1)
        public float MoraleWeight; // How much morale affects task assignment (0-1)
        public float UrgencyWeight; // How much urgency affects task assignment (0-1)
        public float DistanceWeight; // How much distance affects task assignment (0-1)
    }

    /// <summary>
    /// Catalog of race specifications stored as a BlobAsset.
    /// </summary>
    public struct RaceSpecCatalogBlob
    {
        public BlobArray<RaceSpecBlob> Races;
    }

    /// <summary>
    /// Component that references a race specification blob.
    /// </summary>
    public struct RaceSpecReference : IComponentData
    {
        public BlobAssetReference<RaceSpecCatalogBlob> Catalog;
        public int RaceIndex; // Index into catalog.Races array
    }
}

