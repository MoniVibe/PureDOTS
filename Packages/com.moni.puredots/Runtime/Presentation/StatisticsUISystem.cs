using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Presentation
{
    /// <summary>
    /// System for rendering statistics UI using UI Toolkit or IMGUI.
    /// Parses telemetry streams into charts, histograms, and correlation heatmaps.
    /// Pre-allocates UI elements and updates data bindings only.
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct StatisticsUISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // UI initialization happens in MonoBehaviour layer
            // This system coordinates data updates
        }

        public void OnUpdate(ref SystemState state)
        {
            // Poll telemetry stream from TelemetryStreamingSystem
            // Update UI data bindings
            // Rendering happens in MonoBehaviour/UI Toolkit layer
        }
    }

    /// <summary>
    /// Component marking entities that represent statistics UI panels.
    /// </summary>
    public struct StatisticsUITag : IComponentData
    {
    }

    /// <summary>
    /// Data component for statistics UI state.
    /// </summary>
    public struct StatisticsUIData : IComponentData
    {
        public FixedString128Bytes SelectedMetric1;
        public FixedString128Bytes SelectedMetric2;
        public bool ShowCorrelationHeatmap;
        public bool ShowHistogram;
        public bool ShowTimeSeries;
    }
}

