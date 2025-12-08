using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Runtime.Economy.Ownership
{
    /// <summary>
    /// Market cell component for spatial market aggregation.
    /// Attached to spatial cells to track supply/demand per commodity.
    /// </summary>
    public struct MarketCell : IComponentData
    {
        /// <summary>
        /// Spatial cell ID for this market cell.
        /// </summary>
        public int CellId;

        /// <summary>
        /// Tick when market cell was last updated.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Market supply/demand buffer element per commodity type.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct MarketCommodityData : IBufferElementData
    {
        /// <summary>
        /// Resource type identifier.
        /// </summary>
        public FixedString64Bytes ResourceType;

        /// <summary>
        /// Current supply (total available quantity).
        /// </summary>
        public float Supply;

        /// <summary>
        /// Current demand (total desired quantity).
        /// </summary>
        public float Demand;

        /// <summary>
        /// Current market price.
        /// </summary>
        public float Price;

        /// <summary>
        /// Price adjustment rate constant (k in Price = Price + k * (Demand - Supply)).
        /// </summary>
        public float PriceAdjustmentRate;
    }
}

