using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Streaming configuration for cell size and hysteresis (to reduce thrash).
    /// </summary>
    public struct CellStreamingConfig : IComponentData
    {
        /// <summary>Cell size in world units (XZ plane).</summary>
        public float2 CellSize;
        /// <summary>Additional half-extents added to the activation window to reduce oscillation.</summary>
        public float2 Hysteresis;
        /// <summary>Estimated bytes per agent when serialized (for metrics only).</summary>
        public float EstimatedAgentBytes;
    }

    /// <summary>
    /// Active streaming window (usually camera or player focus area).
    /// </summary>
    public struct CellStreamingWindow : IComponentData
    {
        /// <summary>Center of the active window in world space.</summary>
        public float3 Center;
        /// <summary>Half-extents of the active window in world units (XZ plane).</summary>
        public float2 HalfExtents;
    }

    /// <summary>
    /// Telemetry for streaming state.
    /// </summary>
    public struct CellStreamingMetrics : IComponentData
    {
        public int ActiveCells;
        public int SerializedCells;
        public int ActiveAgents;
        public int SerializedAgents;
        public int ApproxBytes;
    }

    /// <summary>
    /// Optional target for the streaming window (e.g., camera or player anchor).
    /// If present, the window updater system copies these values into CellStreamingWindow.
    /// </summary>
    public struct CellStreamingWindowTarget : IComponentData
    {
        public float3 Position;
        public float2 HalfExtents;
    }
}
