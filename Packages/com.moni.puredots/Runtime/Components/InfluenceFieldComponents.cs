using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Influence field data representing population averages.
    /// Agents react to these averages instead of individual neighbors.
    /// </summary>
    public struct InfluenceFieldData : IComponentData
    {
        public float AverageMorale;        // Population average morale
        public float AverageDensity;       // Population density in area
        public float AverageThreat;        // Average threat level
        public float3 InfluenceCenter;     // Center of influence field
        public float InfluenceRadius;      // Radius of influence
        public uint LastUpdateTick;        // When field was last updated
    }

    /// <summary>
    /// Influence field grid cell for spatial partitioning.
    /// </summary>
    public struct InfluenceFieldCell : IBufferElementData
    {
        public int CellId;                 // Spatial cell identifier
        public float AverageMorale;        // Average morale in this cell
        public float AverageDensity;       // Density in this cell
        public float AverageThreat;        // Average threat in this cell
        public int AgentCount;             // Number of agents in this cell
        public uint LastUpdateTick;        // When cell was last updated
    }

    /// <summary>
    /// Component marking an entity that contributes to influence fields.
    /// </summary>
    public struct InfluenceFieldContributor : IComponentData
    {
        public float ContributionWeight;    // How much this entity contributes (0-1)
        public float3 Position;            // Current position
        public float Morale;                // Current morale
        public float ThreatLevel;           // Threat level this entity represents
    }

    /// <summary>
    /// BlobAsset structure for influence field configuration.
    /// </summary>
    public struct InfluenceFieldConfigBlob
    {
        public float UpdateInterval;       // How often to update fields (seconds)
        public float CellSize;              // Size of influence field cells
        public float InfluenceRadius;       // Default influence radius
        public float MoraleDecayRate;       // How quickly morale influence decays
        public float DensityDecayRate;      // How quickly density influence decays
        public float ThreatDecayRate;        // How quickly threat influence decays
    }
}

