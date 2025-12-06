using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Hull specification blob with precomputed inertia tensors for standard hulls.
    /// Used for composite or modifiable ships.
    /// </summary>
    public struct HullSpec
    {
        /// <summary>
        /// Hull identifier.
        /// </summary>
        public FixedString64Bytes HullId;

        /// <summary>
        /// Precomputed diagonalized inertia tensor (Ixx, Iyy, Izz) for base hull.
        /// </summary>
        public float3 BaseInertiaTensor;

        /// <summary>
        /// Base hull mass in kg.
        /// </summary>
        public float BaseMass;

        /// <summary>
        /// Center of mass offset from hull origin.
        /// </summary>
        public float3 CenterOfMassOffset;
    }

    /// <summary>
    /// Hull catalog blob containing standard hull specifications.
    /// </summary>
    public struct HullCatalogBlob
    {
        public BlobArray<HullSpec> Hulls;
    }

    /// <summary>
    /// Singleton component holding the hull catalog reference.
    /// </summary>
    public struct HullCatalog : IComponentData
    {
        public BlobAssetReference<HullCatalogBlob> Catalog;
    }

    /// <summary>
    /// Component storing hull reference for an entity.
    /// Used to look up precomputed inertia tensors.
    /// </summary>
    public struct HullReference : IComponentData
    {
        /// <summary>
        /// Hull identifier for lookup in catalog.
        /// </summary>
        public FixedString64Bytes HullId;
    }
}

