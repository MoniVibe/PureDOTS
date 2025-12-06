using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Type of field affected by terraforming events.
    /// </summary>
    public enum TerraformingFieldType : byte
    {
        Temperature = 0,
        Moisture = 1,
        Light = 2,
        Chemical = 3,
        Wind = 4
    }

    /// <summary>
    /// Shape of terraforming effect distribution.
    /// </summary>
    public enum TerraformingShape : byte
    {
        Gaussian = 0,  // Smooth falloff with radius
        Impulse = 1,   // Sharp delta injection
        Linear = 2     // Linear falloff
    }

    /// <summary>
    /// Terraforming event that applies deltas to environment fields.
    /// Enqueued by miracles, terraforming structures, or player actions.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct TerraformingEvent : IBufferElementData
    {
        public float3 Position;           // World position of effect center
        public float Radius;              // Effect radius in world units
        public float Intensity;           // Magnitude of the effect (signed delta)
        public TerraformingFieldType FieldType; // Which field to modify
        public TerraformingShape Shape;   // Distribution shape
        public uint Tick;                 // Tick when event was created
        public FixedString64Bytes SourceId; // Source identifier (miracle ID, structure ID, etc.)
    }

    /// <summary>
    /// Accumulated terraforming deltas per cell (temporary, applied then cleared).
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct TerraformingDelta : IBufferElementData
    {
        public float TemperatureDelta;
        public float MoistureDelta;
        public float LightDelta;
        public float ChemicalDelta;
    }
}

