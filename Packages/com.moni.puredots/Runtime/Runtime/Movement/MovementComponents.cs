using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Movement
{
    /// <summary>
    /// Reference to a movement model specification blob.
    /// Entities use this to access their movement capabilities.
    /// </summary>
    public struct MovementModelRef : IComponentData
    {
        public BlobAssetReference<MovementModelSpec> Blob;
    }

    /// <summary>
    /// Runtime movement state for an entity.
    /// Updated each tick by movement systems.
    /// </summary>
    public struct MovementState : IComponentData
    {
        public float3 Vel; // Current velocity vector
        public float3 Desired; // Command vector (goal + avoidance steering)
        public byte Mode; // MovementMode enum (Cruise, Hover, Boost, Drift, etc.)
    }

    /// <summary>
    /// Movement mode enumeration.
    /// </summary>
    public enum MovementMode : byte
    {
        Cruise = 0,
        Hover = 1,
        Boost = 2,
        Drift = 3,
        Brake = 4
    }

    /// <summary>
    /// Request to switch movement mode.
    /// Processed by MovementModeSystem with validation (energy, heat, cooldown).
    /// </summary>
    public struct MovementModeRequest : IBufferElementData
    {
        public byte Mode; // MovementMode enum
        public uint RequestTick; // Tick when request was made
    }
}

