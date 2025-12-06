using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Resource state component storing mass, density, and dimensions.
    /// Used for density-based volume calculations and resource pile simulation.
    /// </summary>
    public struct ResourceState : IComponentData
    {
        /// <summary>
        /// Total mass in kg.
        /// </summary>
        public float Mass;

        /// <summary>
        /// Density in kg/m³.
        /// </summary>
        public float Density;

        /// <summary>
        /// Bounding box dimensions or radius (x=width, y=height, z=depth or x=radius).
        /// </summary>
        public float3 Dimensions;

        /// <summary>
        /// Last update tick.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Resource catalog entry for static density lookup per resource type.
    /// </summary>
    public struct ResourceCatalogEntry
    {
        /// <summary>
        /// Resource type identifier.
        /// </summary>
        public FixedString64Bytes ResourceId;

        /// <summary>
        /// Static density in kg/m³ for this resource type.
        /// </summary>
        public float Density;
    }

    /// <summary>
    /// Resource catalog blob containing static density per resource type.
    /// </summary>
    public struct ResourceCatalogBlob
    {
        /// <summary>
        /// Array of resource catalog entries.
        /// </summary>
        public BlobArray<ResourceCatalogEntry> Resources;
    }

    /// <summary>
    /// Singleton component holding the resource catalog reference.
    /// </summary>
    public struct ResourceCatalog : IComponentData
    {
        public BlobAssetReference<ResourceCatalogBlob> Catalog;
    }
}

