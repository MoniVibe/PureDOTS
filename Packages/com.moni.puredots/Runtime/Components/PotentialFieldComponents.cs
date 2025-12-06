using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Scalar potential field value at a spatial grid cell.
    /// Represents attraction/repulsion strength.
    /// </summary>
    public struct PotentialFieldScalar : IComponentData
    {
        public float AttractionStrength;    // Attraction to goals (positive)
        public float RepulsionStrength;     // Repulsion from threats (negative)
        public float CombinedPotential;      // Combined field value
        public uint LastUpdateTick;          // When field was last updated
    }

    /// <summary>
    /// Vector potential field (gradient) at a spatial grid cell.
    /// Direction and magnitude of field influence.
    /// </summary>
    public struct PotentialFieldVector : IComponentData
    {
        public float3 Gradient;             // Gradient vector (direction of influence)
        public float Magnitude;              // Magnitude of gradient
        public uint LastUpdateTick;          // When gradient was last computed
    }

    /// <summary>
    /// Potential field emitter attached to agent limbs/ship modules.
    /// Each emitter contributes to the spatial grid fields.
    /// </summary>
    public struct PotentialFieldEmitter : IComponentData
    {
        public AgentGuid EmitterGuid;        // Unique identifier for emitter
        public float AttractionCoefficient;  // How much this emitter attracts (0-1)
        public float RepulsionCoefficient;   // How much this emitter repels (0-1)
        public float InfluenceRadius;        // Radius of influence
        public float3 EmitterPosition;       // Current position
        public PotentialFieldType Type;       // Type of field (Goal, Threat, Social, etc.)
    }

    /// <summary>
    /// Types of potential fields.
    /// </summary>
    public enum PotentialFieldType : byte
    {
        Goal = 0,        // Attraction to goals
        Threat = 1,      // Repulsion from threats
        Social = 2,      // Social bias (attraction/repulsion)
        Resource = 3     // Resource attraction
    }

    /// <summary>
    /// Field gradient sample for Mind ECS consumption.
    /// Contains sampled gradient data for decision-making.
    /// </summary>
    public struct FieldGradientSample : IBufferElementData
    {
        public float3 Position;              // Sample position
        public float3 Gradient;              // Sampled gradient vector
        public float Magnitude;              // Gradient magnitude
        public PotentialFieldType Type;      // Field type
        public uint SampleTick;              // When sample was taken
    }
}

