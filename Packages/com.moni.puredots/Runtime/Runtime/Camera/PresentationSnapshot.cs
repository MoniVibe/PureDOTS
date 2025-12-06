using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
    /// Double-buffered presentation snapshot for temporal coherency.
    /// Stores two snapshots (A and B) that presentation systems interpolate between.
    /// Prevents half-updated reads from ECS chunks.
    /// </summary>
    public struct PresentationSnapshot : IComponentData
    {
        public float3 PositionA;
        public float3 PositionB;
        public quaternion RotationA;
        public quaternion RotationB;
        public uint TickA;
        public uint TickB;
        public bool IsBufferASwap; // Tracks which buffer is current
    }
}

