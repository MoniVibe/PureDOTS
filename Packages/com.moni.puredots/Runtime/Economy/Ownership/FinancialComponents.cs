using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Ownership
{
    /// <summary>
    /// Financial ledger component tracking cash, income, and expenses.
    /// Attached to entities that participate in economic transactions.
    /// </summary>
    public struct Ledger : IComponentData
    {
        /// <summary>
        /// Current cash balance (liquid funds).
        /// </summary>
        public float Cash;

        /// <summary>
        /// Income per period (calculated from Portfolio assets).
        /// </summary>
        public float Income;

        /// <summary>
        /// Expenses per period (upkeep, taxes, etc.).
        /// </summary>
        public float Expenses;

        /// <summary>
        /// Tick when ledger was last updated.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Portfolio buffer tracking assets owned by an entity.
    /// Used to calculate income: Income = Σ(asset.OutputValue × OwnershipShare).
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct Portfolio : IBufferElementData
    {
        /// <summary>
        /// Entity reference to the owned asset.
        /// </summary>
        public Entity Asset;

        /// <summary>
        /// Ownership share [0..1]. Fraction of asset owned.
        /// </summary>
        public float OwnershipShare;

        /// <summary>
        /// Expected output value per period from this asset share.
        /// Cached for performance; recalculated when asset production changes.
        /// </summary>
        public float ExpectedOutputValue;
    }

    /// <summary>
    /// Financial state tracking component for dirty flag optimization.
    /// Marks entities needing recalculation when ownership/production changes.
    /// </summary>
    public struct FinancialState : IComponentData
    {
        /// <summary>
        /// Tick when financial state was last updated.
        /// </summary>
        public uint LastUpdateTick;

        /// <summary>
        /// Dirty flag: true if ledger/portfolio needs recalculation.
        /// </summary>
        public bool DirtyFlag;
    }
}

