using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Sensor packet for asynchronous perception streaming.
    /// Queued results from expensive sensors (radar, smell diffusion).
    /// </summary>
    public struct SensorPacket : IBufferElementData
    {
        public float3 SourcePosition;
        public float Confidence;
        public SensorType SensorType;
        public uint DetectionTick;
        public Entity SourceEntity;
    }

    /// <summary>
    /// Sensor type enumeration.
    /// </summary>
    public enum SensorType : byte
    {
        Vision = 0,
        Hearing = 1,
        Smell = 2,
        Radar = 3
    }
}

