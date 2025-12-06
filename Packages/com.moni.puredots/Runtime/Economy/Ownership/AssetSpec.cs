using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Ownership
{
    /// <summary>
    /// Asset specification blob defining static properties for asset categories.
    /// All mines, farms, shipyards, etc. reference their AssetSpec blob.
    /// </summary>
    public struct AssetSpecBlob
    {
        /// <summary>
        /// Initial capital cost to acquire/build this asset.
        /// </summary>
        public float CapitalCost;

        /// <summary>
        /// Upkeep cost per period (maintenance, operational expenses).
        /// </summary>
        public float Upkeep;

        /// <summary>
        /// Production output rate per period (units per second).
        /// </summary>
        public float OutputRate;

        /// <summary>
        /// Type of resource this asset produces (ResourceTypeId).
        /// </summary>
        public FixedString64Bytes OutputType;

        /// <summary>
        /// Workforce requirement (number of workers needed for full production).
        /// </summary>
        public float WorkforceNeed;

        /// <summary>
        /// Asset type identifier (Mine, Facility, Village, etc.).
        /// </summary>
        public AssetType Type;
    }

    /// <summary>
    /// Catalog blob containing all asset specifications.
    /// Maps AssetType enum to AssetSpecBlob.
    /// </summary>
    public struct AssetSpecCatalogBlob
    {
        /// <summary>
        /// Array of asset specs indexed by AssetType enum value.
        /// </summary>
        public BlobArray<AssetSpecBlob> Specs;
    }

    /// <summary>
    /// Singleton component holding the asset spec catalog reference.
    /// </summary>
    public struct AssetSpecCatalog : IComponentData
    {
        /// <summary>
        /// Blob asset reference to the catalog of all asset specs.
        /// </summary>
        public BlobAssetReference<AssetSpecCatalogBlob> Catalog;
    }
}

