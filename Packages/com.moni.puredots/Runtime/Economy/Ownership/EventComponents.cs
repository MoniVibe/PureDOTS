using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Ownership
{
    /// <summary>
    /// Purchase event buffer element for asset acquisition.
    /// Created by investment systems, processed by PortfolioManagementSystem.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct PurchaseEvent : IBufferElementData
    {
        /// <summary>
        /// Entity purchasing the asset.
        /// </summary>
        public Entity Buyer;

        /// <summary>
        /// Asset entity being purchased.
        /// </summary>
        public Entity Asset;

        /// <summary>
        /// Ownership share being purchased [0..1].
        /// </summary>
        public float Share;

        /// <summary>
        /// Purchase price.
        /// </summary>
        public float Price;

        /// <summary>
        /// Tick when purchase was initiated.
        /// </summary>
        public uint Tick;
    }

    /// <summary>
    /// Sale event buffer element for asset disposal.
    /// Created by investment systems, processed by PortfolioManagementSystem.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct SaleEvent : IBufferElementData
    {
        /// <summary>
        /// Entity selling the asset.
        /// </summary>
        public Entity Seller;

        /// <summary>
        /// Asset entity being sold.
        /// </summary>
        public Entity Asset;

        /// <summary>
        /// Ownership share being sold [0..1].
        /// </summary>
        public float Share;

        /// <summary>
        /// Sale price.
        /// </summary>
        public float Price;

        /// <summary>
        /// Tick when sale was initiated.
        /// </summary>
        public uint Tick;
    }

    /// <summary>
    /// Investment command event for MindECS→BodyECS communication.
    /// Sent via AgentSyncBus for investment decisions.
    /// </summary>
    public struct InvestmentCommand : IComponentData
    {
        /// <summary>
        /// Entity making the investment decision.
        /// </summary>
        public Entity Investor;

        /// <summary>
        /// Target asset entity.
        /// </summary>
        public Entity TargetAsset;

        /// <summary>
        /// Desired ownership share [0..1].
        /// </summary>
        public float DesiredShare;

        /// <summary>
        /// Maximum price willing to pay.
        /// </summary>
        public float MaxPrice;

        /// <summary>
        /// Utility score that triggered this investment.
        /// </summary>
        public float UtilityScore;

        /// <summary>
        /// Tick when command was created.
        /// </summary>
        public uint Tick;
    }
}

