using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Flags controlling which telemetry streams are exported.
    /// </summary>
    [System.Flags]
    public enum TelemetryExportFlags : byte
    {
        None = 0,
        IncludeTelemetryMetrics = 1 << 0,
        IncludeFrameTiming = 1 << 1,
        IncludeBehaviorTelemetry = 1 << 2,
        IncludeReplayEvents = 1 << 3,
        IncludeTelemetryEvents = 1 << 4
    }

    /// <summary>
    /// Configuration singleton consumed by <see cref="TelemetryExportSystem"/>.
    /// </summary>
    public struct TelemetryExportConfig : IComponentData
    {
        /// <summary>Absolute or project-relative path to the NDJSON export file.</summary>
        public FixedString512Bytes OutputPath;
        /// <summary>Identifier associated with the current run (auto-generated when empty).</summary>
        public FixedString128Bytes RunId;
        /// <summary>Active export flags.</summary>
        public TelemetryExportFlags Flags;
        /// <summary>Whether exporting is enabled.</summary>
        public byte Enabled;
        /// <summary>Version counter so systems can detect config changes.</summary>
        public uint Version;

        public static TelemetryExportConfig CreateDisabled()
        {
            return new TelemetryExportConfig
            {
                OutputPath = default,
                RunId = default,
                Flags = TelemetryExportFlags.IncludeTelemetryMetrics |
                        TelemetryExportFlags.IncludeFrameTiming |
                        TelemetryExportFlags.IncludeBehaviorTelemetry |
                        TelemetryExportFlags.IncludeReplayEvents |
                        TelemetryExportFlags.IncludeTelemetryEvents,
                Enabled = 0,
                Version = 1
            };
        }
    }
}
