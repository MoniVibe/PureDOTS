using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Debug;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Drives AI debug overlay via TelemetryStream and DecisionEventBuffer.
    /// Draws perception ranges, flowfield gradients, decision heatmaps.
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [BurstCompile]
    [UpdateInGroup(typeof(PureDotsPresentationSystemGroup))]
    public partial struct AIDebugVisualizerSystem : ISystem
    {
        private AIDebugOverlay _overlay;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _overlay = new AIDebugOverlay(Unity.Collections.Allocator.Persistent);
            state.RequireForUpdate<TelemetryStream>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _overlay.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Collect perception ranges from AI systems
            // Collect flowfield gradients
            // Collect decision heatmaps from DecisionEventBuffer

            // In a real implementation, this would:
            // 1. Query entities with AISensorConfig
            // 2. Draw gizmos/LineRenderer for perception ranges
            // 3. Visualize flowfield gradients
            // 4. Draw heatmaps for decision utilities
        }
    }
#endif
}

