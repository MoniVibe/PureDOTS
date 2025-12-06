using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Power zone component - groups entities into power grids for hierarchical aggregation.
    /// </summary>
    public struct PowerZone : IComponentData
    {
        /// <summary>Zone ID (derived from spatial cell or custom assignment).</summary>
        public int ZoneId;
        
        /// <summary>Parent zone ID (for hierarchical aggregation).</summary>
        public int ParentZoneId;
        
        /// <summary>Last tick when zone was updated.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Power production/consumption per zone (aggregated from entities).
    /// </summary>
    public struct PowerZoneState : IComponentData
    {
        /// <summary>Total power production (fixed-point).</summary>
        public long TotalProduction;
        
        /// <summary>Total power consumption (fixed-point).</summary>
        public long TotalConsumption;
        
        /// <summary>Net power (production - consumption).</summary>
        public long NetPower;
        
        /// <summary>Last tick when state was updated.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Power production/consumption for individual entities.
    /// </summary>
    public struct EntityPower : IComponentData
    {
        /// <summary>Power production rate (fixed-point).</summary>
        public long ProductionRate;
        
        /// <summary>Power consumption rate (fixed-point).</summary>
        public long ConsumptionRate;
        
        /// <summary>Zone ID this entity belongs to.</summary>
        public int ZoneId;
    }
}

