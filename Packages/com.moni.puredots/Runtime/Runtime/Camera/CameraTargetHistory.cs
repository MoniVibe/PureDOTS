using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
    /// Stores interpolation history for camera targets.
    /// Used by CameraInterpolationSystem to smooth camera movement between simulation ticks.
    /// </summary>
    public struct CameraTargetHistory : IComponentData
    {
        public float3 PrevPosition;
        public float3 NextPosition;
        public quaternion PrevRotation;
        public quaternion NextRotation;
        public float Alpha; // interpolation factor: 0 = prev, 1 = next
        public float3 Velocity; // for extrapolation
        public uint PrevTick;
        public uint NextTick;
    }
}

