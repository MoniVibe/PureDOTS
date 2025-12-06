using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Telemetry;

namespace PureDOTS.Runtime.Debug
{
    /// <summary>
    /// Overlay system for AI debugging: perception ranges, flowfield gradients, decision heatmaps.
    /// Debug-only, disabled in release builds.
    /// </summary>
    [BurstCompile]
    public struct AIDebugOverlay
    {
        public NativeList<float3> PerceptionRangeCenters;
        public NativeList<float> PerceptionRangeRadii;
        public NativeList<float3> FlowfieldGradients;
        public NativeList<float> DecisionHeatmapValues;

        public AIDebugOverlay(Allocator allocator)
        {
            PerceptionRangeCenters = new NativeList<float3>(64, allocator);
            PerceptionRangeRadii = new NativeList<float>(64, allocator);
            FlowfieldGradients = new NativeList<float3>(256, allocator);
            DecisionHeatmapValues = new NativeList<float>(256, allocator);
        }

        public void Dispose()
        {
            if (PerceptionRangeCenters.IsCreated)
                PerceptionRangeCenters.Dispose();
            if (PerceptionRangeRadii.IsCreated)
                PerceptionRangeRadii.Dispose();
            if (FlowfieldGradients.IsCreated)
                FlowfieldGradients.Dispose();
            if (DecisionHeatmapValues.IsCreated)
                DecisionHeatmapValues.Dispose();
        }
    }
}

