using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Authoring.Economy.Ownership
{
    /// <summary>
    /// ScriptableObject catalog of all asset types.
    /// Maps AssetType enum → AssetSpec.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetSpecCatalog", menuName = "PureDOTS/Economy/Ownership/Asset Spec Catalog", order = 2)]
    public sealed class AssetSpecCatalogAuthoring : ScriptableObject
    {
        [SerializeField] private List<AssetSpecAuthoring> _assetSpecs = new List<AssetSpecAuthoring>();

        public List<AssetSpecAuthoring> AssetSpecs => _assetSpecs;

        /// <summary>
        /// Converts authoring catalog to AssetSpecCatalogBlob.
        /// This would be called by a bootstrap system to create the blob asset.
        /// </summary>
        public AssetSpecCatalogBlob ToBlob()
        {
            // This would be implemented by a bootstrap system that creates the blob asset
            // For now, this is a placeholder structure
            return default;
        }
    }
}

