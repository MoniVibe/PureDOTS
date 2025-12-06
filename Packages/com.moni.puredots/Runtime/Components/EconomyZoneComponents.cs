using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Economy zone component - groups entities into economic regions for hierarchical aggregation.
    /// </summary>
    public struct EconomyZone : IComponentData
    {
        /// <summary>Zone ID (derived from spatial cell or custom assignment).</summary>
        public int ZoneId;
        
        /// <summary>Parent zone ID (for hierarchical aggregation).</summary>
        public int ParentZoneId;
        
        /// <summary>Last tick when zone was updated.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Economic state per zone (aggregated resource flows).
    /// </summary>
    public struct EconomyZoneState : IComponentData
    {
        /// <summary>Total resource production per type (fixed-point).</summary>
        public FixedList128Bytes<long> ProductionPerType;
        
        /// <summary>Total resource consumption per type (fixed-point).</summary>
        public FixedList128Bytes<long> ConsumptionPerType;
        
        /// <summary>Trade surplus/deficit per type (fixed-point).</summary>
        public FixedList128Bytes<long> TradeBalancePerType;
        
        /// <summary>Last tick when state was updated.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Economic production/consumption for individual entities.
    /// </summary>
    public struct EntityEconomy : IComponentData
    {
        /// <summary>Resource production rates per type (fixed-point).</summary>
        public FixedList32Bytes<long> ProductionRates;
        
        /// <summary>Resource consumption rates per type (fixed-point).</summary>
        public FixedList32Bytes<long> ConsumptionRates;
        
        /// <summary>Zone ID this entity belongs to.</summary>
        public int ZoneId;
    }
}

