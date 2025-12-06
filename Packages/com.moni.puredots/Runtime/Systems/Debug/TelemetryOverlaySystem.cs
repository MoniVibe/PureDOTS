using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Debug
{
    /// <summary>
    /// Telemetry overlay system displaying FPS vs. active-chunk count.
    /// Uses existing TelemetryStream infrastructure.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct TelemetryOverlaySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Visualization system - runs in presentation group
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Telemetry overlay logic would go here
            // Displays FPS, active chunk count, performance budgets
            // Uses TelemetryStream for data
            // For now, this is a placeholder structure
        }
    }
}

