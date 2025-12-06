using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for constraint definitions.
    /// Defines constraint parameters for different entity types.
    /// </summary>
    public struct ConstraintDefinitionBlob
    {
        public BlobString ConstraintId;      // Constraint identifier
        public float DefaultCapacity;        // Default capacity
        public float DefaultRegenRate;        // Default regeneration rate
        public float DefaultCostRate;         // Default cost rate
        public float MinCapacity;            // Minimum capacity
        public float MaxCapacity;            // Maximum capacity
    }

    /// <summary>
    /// Catalog of constraint definitions.
    /// </summary>
    public struct ConstraintDefinitionCatalogBlob
    {
        public BlobArray<ConstraintDefinitionBlob> Definitions;
    }
}

