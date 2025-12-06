using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Ownership
{
    /// <summary>
    /// Asset type enum for categorizing economic assets.
    /// Burst-safe enum (no FixedString construction in static contexts).
    /// </summary>
    public enum AssetType : byte
    {
        Mine = 0,
        Facility = 1,
        Village = 2,
        City = 3,
        Colony = 4,
        System = 5,
        Sector = 6,
        Galaxy = 7,
        Farm = 8,
        Shipyard = 9,
        Factory = 10,
        Warehouse = 11,
        Custom0 = 240
    }

    /// <summary>
    /// Ownership rights bitmask flags.
    /// Defines what actions an owner can perform on an asset.
    /// </summary>
    [System.Flags]
    public enum OwnershipRights : byte
    {
        None = 0,
        Manage = 1 << 0,  // Can modify asset operations
        Trade = 1 << 1,   // Can sell/transfer ownership
        Tax = 1 << 2,     // Can collect taxes from asset
        Use = 1 << 3      // Can use asset for production/consumption
    }

    /// <summary>
    /// Ownership component for single-owner assets.
    /// For multiple owners, use OwnershipBuffer instead.
    /// </summary>
    public struct Ownership : IComponentData
    {
        /// <summary>
        /// Entity that owns this asset (individual, organization, or aggregate entity).
        /// </summary>
        public Entity Owner;

        /// <summary>
        /// Ownership share [0..1]. Fraction of asset owned by Owner.
        /// </summary>
        public float Share;

        /// <summary>
        /// Rights bitmask defining what Owner can do with this asset.
        /// </summary>
        public OwnershipRights Rights;
    }

    /// <summary>
    /// Buffer element for multiple owners per asset.
    /// Use this when an asset has shared ownership (joint ventures, co-ownership).
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct OwnershipBuffer : IBufferElementData
    {
        /// <summary>
        /// Entity that owns a share of this asset.
        /// </summary>
        public Entity Owner;

        /// <summary>
        /// Ownership share [0..1]. Fraction of asset owned by this Owner.
        /// Sum of all OwnershipBuffer.Share should be ≤ 1.0.
        /// </summary>
        public float Share;

        /// <summary>
        /// Rights bitmask for this specific owner.
        /// </summary>
        public OwnershipRights Rights;
    }

    /// <summary>
    /// Tag component identifying an entity as an economic asset.
    /// </summary>
    public struct AssetTag : IComponentData
    {
        /// <summary>
        /// Type of asset (Mine, Facility, Village, etc.).
        /// </summary>
        public AssetType Type;

        /// <summary>
        /// Unique asset identifier for tracking and queries.
        /// </summary>
        public ulong AssetId;
    }
}

