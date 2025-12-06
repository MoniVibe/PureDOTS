using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Sensor type enumeration for different perception modalities.
    /// </summary>
    public enum SensorType : byte
    {
        Vision = 0,
        Smell = 1,
        Hearing = 2,
        Radar = 3,
        EM = 4
    }

    /// <summary>
    /// Sensor specification describing sensor capabilities and configuration.
    /// </summary>
    public struct SensorSpec : IComponentData
    {
        public SensorType Type;
        public float Range;
        public float FieldOfView; // For vision/radar (degrees), 0 = omnidirectional
        public float UpdateInterval; // Seconds between updates
        public float Sensitivity; // 0-1, affects confidence calculation
        public byte MaxResults; // Maximum number of readings to store
    }

    /// <summary>
    /// Sensor reading buffer element with type-specific data and confidence.
    /// Extends AISensorReading with sensor type and confidence.
    /// </summary>
    public struct SensorReadingBuffer : IBufferElementData
    {
        public Entity Target;
        public float3 Position;
        public float DistanceSq;
        public float Confidence; // 0-1, sensor-specific confidence calculation
        public SensorType SensorType;
        public uint TickNumber;
        public int CellId;
        public uint SpatialVersion;
    }

    /// <summary>
    /// Runtime state for sensor update cadence tracking.
    /// </summary>
    public struct SensorState : IComponentData
    {
        public float Elapsed;
        public uint LastSampleTick;
    }
}
