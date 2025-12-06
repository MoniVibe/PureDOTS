using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Material category enumeration for precomputed material tables.
    /// </summary>
    public enum MaterialCategory : byte
    {
        Metal = 0,
        Alloy = 1,
        Organic = 2,
        Composite = 3
    }

    /// <summary>
    /// Material specification blob containing physical constants per material.
    /// Used for weapon performance, collision elasticity, and deformation calculations.
    /// </summary>
    public struct MaterialSpec
    {
        /// <summary>
        /// Material identifier.
        /// </summary>
        public FixedString64Bytes MaterialId;

        /// <summary>
        /// Material name.
        /// </summary>
        public FixedString64Bytes Name;

        /// <summary>
        /// Material category for quick lookup.
        /// </summary>
        public MaterialCategory Category;

        /// <summary>
        /// Density in kg/m³.
        /// </summary>
        public float Density;

        /// <summary>
        /// Young's modulus (elastic modulus) in Pa.
        /// </summary>
        public float YoungsModulus;

        /// <summary>
        /// Yield strength in Pa (stress at which material begins to deform plastically).
        /// </summary>
        public float YieldStrength;

        /// <summary>
        /// Flexibility factor (0-1, higher = more flexible).
        /// </summary>
        public float Flexibility;

        /// <summary>
        /// Heat capacity in J/(kg·K).
        /// </summary>
        public float HeatCapacity;
    }

    /// <summary>
    /// Material catalog blob containing all material specifications.
    /// Provides efficient lookup by MaterialId.
    /// </summary>
    public struct MaterialCatalogBlob
    {
        /// <summary>
        /// Array of material specifications.
        /// </summary>
        public BlobArray<MaterialSpec> Materials;
    }

    /// <summary>
    /// Singleton component holding the material catalog reference.
    /// </summary>
    public struct MaterialCatalog : IComponentData
    {
        public BlobAssetReference<MaterialCatalogBlob> Catalog;
    }

    /// <summary>
    /// Component storing material ID reference for an entity.
    /// Used to look up material properties from MaterialCatalog.
    /// </summary>
    public struct MaterialId : IComponentData
    {
        /// <summary>
        /// Material identifier for lookup in catalog.
        /// </summary>
        public FixedString64Bytes Value;
    }
}

