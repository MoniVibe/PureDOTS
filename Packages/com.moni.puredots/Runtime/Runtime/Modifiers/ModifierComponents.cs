using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Per-entity active modifier instance stored in DynamicBuffer.
    /// Uses numeric ID indexing for Burst-safe lookups (no string comparisons).
    /// 
    /// USAGE:
    /// - Created automatically by ModifierEventApplicationSystem
    /// - Read via: SystemAPI.GetBuffer&lt;ModifierInstance&gt;(entity)
    /// - ModifierId is ushort index into ModifierCatalogBlob (NOT string!)
    /// - Duration: -1 = permanent, &gt;0 = ticks remaining
    /// 
    /// See: Docs/Guides/ModifierSystemAPI.md for API reference.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct ModifierInstance : IBufferElementData
    {
        /// <summary>
        /// Modifier ID from catalog (ushort index, not string).
        /// </summary>
        public ushort ModifierId;

        /// <summary>
        /// Modifier value (+% or absolute, depending on operation type).
        /// </summary>
        public float Value;

        /// <summary>
        /// Ticks remaining (-1 = permanent).
        /// </summary>
        public short Duration;
    }

    /// <summary>
    /// Singleton reference to modifier catalog blob asset.
    /// </summary>
    public struct ModifierCatalogRef : IComponentData
    {
        public BlobAssetReference<ModifierCatalogBlob> Blob;
    }

    /// <summary>
    /// Aggregated modifier sums per category (Economy, Military, Environment).
    /// Pre-computed to avoid cascading re-evaluations.
    /// </summary>
    public struct ModifierCategoryAccumulator : IComponentData
    {
        /// <summary>
        /// Economic category sums (income, upkeep, etc.).
        /// </summary>
        public float EconomicAdd;
        public float EconomicMul;

        /// <summary>
        /// Military category sums (morale, damage, etc.).
        /// </summary>
        public float MilitaryAdd;
        public float MilitaryMul;

        /// <summary>
        /// Environment category sums (temperature, fertility, etc.).
        /// </summary>
        public float EnvironmentAdd;
        public float EnvironmentMul;

        /// <summary>
        /// Tick when accumulator was last updated.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Component for hierarchical entities (villager → village → kingdom).
    /// Stores aggregated modifier totals for propagation to children.
    /// </summary>
    public struct ModifierAggregator : IComponentData
    {
        /// <summary>
        /// Aggregated modifier totals from all children.
        /// </summary>
        public ModifierCategoryAccumulator AggregatedTotals;

        /// <summary>
        /// Number of child entities contributing to this aggregate.
        /// </summary>
        public int ChildCount;

        /// <summary>
        /// Tick when aggregate was last recomputed.
        /// </summary>
        public uint LastRecomputeTick;
    }

    /// <summary>
    /// Tag component marking entities that need modifier recomputation.
    /// Added when modifiers are applied/expired to trigger hot path update.
    /// </summary>
    public struct ModifierDirtyTag : IComponentData
    {
    }
}

