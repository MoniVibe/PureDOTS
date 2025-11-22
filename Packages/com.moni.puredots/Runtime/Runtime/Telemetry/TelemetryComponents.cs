using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Units associated with telemetry metric values to assist downstream formatting.
    /// </summary>
    public enum TelemetryMetricUnit : byte
    {
        Count = 0,
        Ratio = 1,
        DurationMilliseconds = 2,
        Bytes = 3,
        None = 254,
        Custom = 255
    }

    /// <summary>
    /// Dynamic buffer element capturing a scalar telemetry reading.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct TelemetryMetric : IBufferElementData
    {
        public FixedString64Bytes Key;
        public float Value;
        public TelemetryMetricUnit Unit;
    }

    /// <summary>
    /// Singleton tagging the active telemetry stream with versioning for change detection.
    /// </summary>
    public struct TelemetryStream : IComponentData
    {
        public uint Version;
        public uint LastTick;
    }

    /// <summary>
    /// Helper extensions for appending metrics to a telemetry buffer without repeated boilerplate.
    /// </summary>
    public static class TelemetryBufferExtensions
    {
        public static void AddMetric(this DynamicBuffer<TelemetryMetric> buffer, in FixedString64Bytes key, float value, TelemetryMetricUnit unit = TelemetryMetricUnit.Count)
        {
            buffer.Add(new TelemetryMetric
            {
                Key = key,
                Value = value,
                Unit = unit
            });
        }
    }
}
