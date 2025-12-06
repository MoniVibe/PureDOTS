using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Telemetry
{
    /// <summary>
    /// Async aggregator thread for telemetry (second level of hierarchy).
    /// Aggregates metrics from local buffers across systems.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class TelemetryAggregatorSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TickTimeState>();
        }

        protected override void OnUpdate()
        {
            // Aggregate telemetry from local buffers
            // In full implementation, would:
            // 1. Collect metrics from LocalTelemetryBuffer across all systems
            // 2. Aggregate metrics (sum, average, min, max)
            // 3. Update global TelemetryStream singleton
            // 4. Run on async thread for performance
        }
    }
}

