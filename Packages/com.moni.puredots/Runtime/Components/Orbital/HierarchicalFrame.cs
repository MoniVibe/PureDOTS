using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components.Orbital
{
    /// <summary>
    /// Reference frame for hierarchical orbital systems.
    /// Each hierarchy level (galactic → system → planet → object) maintains its own frame.
    /// </summary>
    public struct OrbitalFrame : IComponentData
    {
        /// <summary>Frame origin in parent frame coordinates.</summary>
        public float3 Origin;

        /// <summary>Frame orientation relative to parent.</summary>
        public quaternion Orientation;

        /// <summary>Frame scale factor (for coordinate system scaling).</summary>
        public float Scale;

        /// <summary>Previous orientation for delta detection.</summary>
        public quaternion PreviousOrientation;

        /// <summary>Threshold for recomputation (default 0.001 rad).</summary>
        public float DeltaThreshold;
    }

    /// <summary>
    /// Links an entity to its parent frame entity.
    /// </summary>
    public struct FrameParent : IComponentData
    {
        /// <summary>Parent frame entity.</summary>
        public Entity ParentFrameEntity;
    }

    /// <summary>
    /// Tag component marking frames that need recomputation.
    /// Set when parent frame's quaternion delta exceeds threshold.
    /// </summary>
    public struct FrameDirtyTag : IComponentData { }

    /// <summary>
    /// Cached world transform for a frame (computed from hierarchy).
    /// Updated only when FrameDirtyTag is present.
    /// </summary>
    public struct FrameWorldTransform : IComponentData
    {
        /// <summary>Cached world position.</summary>
        public float3 WorldPosition;

        /// <summary>Cached world orientation.</summary>
        public quaternion WorldOrientation;

        /// <summary>Last tick when transform was updated.</summary>
        public uint LastUpdateTick;
    }
}

