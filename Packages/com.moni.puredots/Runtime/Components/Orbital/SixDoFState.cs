using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components.Orbital
{
    /// <summary>
    /// Six-degree-of-freedom state for orbital objects.
    /// Stores position, orientation, linear velocity, and angular velocity.
    /// Used for hierarchical decoupling optimization - linear and angular states integrated separately.
    /// </summary>
    public struct SixDoFState : IComponentData
    {
        /// <summary>World position in meters.</summary>
        public float3 Position;

        /// <summary>World orientation quaternion.</summary>
        public quaternion Orientation;

        /// <summary>Linear velocity in m/s.</summary>
        public float3 LinearVelocity;

        /// <summary>Angular velocity as rotation vector (rad/s).</summary>
        public float3 AngularVelocity;
    }
}

