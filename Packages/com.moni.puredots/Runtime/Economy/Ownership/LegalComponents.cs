using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Ownership
{
    /// <summary>
    /// Legal entity component for organizations, corporations, empires, etc.
    /// Represents a governing body that can own assets and collect taxes.
    /// </summary>
    public struct LegalEntity : IComponentData
    {
        /// <summary>
        /// Entity that founded/created this legal entity.
        /// </summary>
        public Entity Founder;

        /// <summary>
        /// Influence score [0..1] based on owned assets and power.
        /// Used to calculate tax rates and political weight.
        /// </summary>
        public float Influence;

        /// <summary>
        /// Tax rate [0..1] applied to assets under this entity's governance.
        /// Calculated from Influence and policies.
        /// </summary>
        public float TaxRate;

        /// <summary>
        /// Treasury balance (cash held by this legal entity).
        /// </summary>
        public float Treasury;

        /// <summary>
        /// Tick when legal entity was last updated.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Governing entity reference component.
    /// Attached to assets to indicate which LegalEntity governs them.
    /// Used for tax collection and legal jurisdiction.
    /// </summary>
    public struct GoverningEntity : IComponentData
    {
        /// <summary>
        /// Entity reference to the LegalEntity that governs this asset.
        /// </summary>
        public Entity LegalEntity;
    }
}

