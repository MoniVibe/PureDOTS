using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Level of Detail component for simulation LOD.
    /// Far agents use statistical simulation, near agents use high-fidelity ECS ticks.
    /// </summary>
    public struct LODComponent : IComponentData
    {
        /// <summary>LOD level (0 = high-fidelity, higher = lower fidelity).</summary>
        public byte LODLevel;
        
        /// <summary>Distance to camera.</summary>
        public float DistanceToCamera;
        
        /// <summary>Update stride (update every N ticks).</summary>
        public uint UpdateStride;
        
        /// <summary>Last tick when LOD was updated.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// LOD configuration for different simulation domains.
    /// </summary>
    public struct LODConfig : IComponentData
    {
        /// <summary>Distance thresholds for each LOD level.</summary>
        public float4 DistanceThresholds; // LOD0, LOD1, LOD2, LOD3
        
        /// <summary>Update strides for each LOD level.</summary>
        public uint4 UpdateStrides; // LOD0, LOD1, LOD2, LOD3
    }
}

