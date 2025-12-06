using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for genetic trait templates.
    /// Defines trait ranges and inheritance rules.
    /// </summary>
    public struct GeneTraitBlob
    {
        public BlobString TraitId;          // Trait identifier
        public GeneType Type;               // Trait type
        public float MinValue;              // Minimum trait value
        public float MaxValue;              // Maximum trait value
        public float DefaultValue;          // Default trait value
        public float MutationRate;          // Mutation rate (0-1)
        public float MutationMagnitude;     // Mutation magnitude (0-1)
    }

    /// <summary>
    /// Catalog of genetic trait templates.
    /// </summary>
    public struct GeneTraitCatalogBlob
    {
        public BlobArray<GeneTraitBlob> Traits;
    }
}

