using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Root blob asset structure for modifier catalog.
    /// Contains array of modifier definitions and flattened dependency chains.
    /// </summary>
    public struct ModifierCatalogBlob
    {
        /// <summary>
        /// Array of modifier definitions indexed by ModifierId.
        /// </summary>
        public BlobArray<ModifierSpec> Modifiers;

        /// <summary>
        /// Flattened dependency chains (topologically sorted).
        /// Each chain is a BlobArray<ushort> of modifier IDs in evaluation order.
        /// </summary>
        public BlobArray<BlobArray<ushort>> DependencyChains;
    }

    /// <summary>
    /// Individual modifier definition stored in blob asset.
    /// All lookups use numeric ModifierId (ushort index), no string comparisons.
    /// </summary>
    public struct ModifierSpec
    {
        /// <summary>
        /// Display name (debug/editor only, not used in hot path).
        /// </summary>
        public BlobString Name;

        /// <summary>
        /// Operation type: Add=0, Multiply=1, Override=2.
        /// </summary>
        public byte Operation;

        /// <summary>
        /// Base modifier value.
        /// </summary>
        public float BaseValue;

        /// <summary>
        /// Category: Economy=0, Military=1, Environment=2.
        /// Used for aggregation to avoid cascades.
        /// </summary>
        public byte Category;

        /// <summary>
        /// Index into DependencyChains array for flattened dependency evaluation.
        /// 0 = no dependencies.
        /// </summary>
        public ushort DependencyChainIndex;

        /// <summary>
        /// Duration scale factor (multiplier for duration calculations).
        /// </summary>
        public float DurationScale;
    }

}

