using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// AI decision telemetry stream for debugging and UI.
    /// Stores decision reasoning and explanations.
    /// </summary>
    public struct AIDecisionTelemetry : IBufferElementData
    {
        public AgentGuid AgentGuid;          // Agent that made decision
        public FixedString128Bytes Reason;  // Human-readable reason
        public DecisionReasonCode Code;      // Structured reason code
        public float Confidence;              // Decision confidence
        public uint DecisionTick;            // When decision was made
        public float3 DecisionContext;       // Context data (position, etc.)
    }

    /// <summary>
    /// Telemetry stream state tracking telemetry collection.
    /// </summary>
    public struct TelemetryStreamState : IComponentData
    {
        public int TelemetryCount;           // Number of telemetry entries
        public uint LastCollectionTick;      // When telemetry was last collected
        public bool IsEnabled;               // Whether telemetry collection is enabled
    }
}

