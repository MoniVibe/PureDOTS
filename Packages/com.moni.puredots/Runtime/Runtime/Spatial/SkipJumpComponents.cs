using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Request an instantaneous skip jump to a destination.
    /// </summary>
    public struct SkipJumpRequest : IComponentData
    {
        public float3 Destination;
        public float MinRange;
        public float MaxRange;
        public byte ClampToRange;
    }

    /// <summary>
    /// Tracks recent skip jump timing for cooldown logic.
    /// </summary>
    public struct SkipJumpState : IComponentData
    {
        public uint LastJumpTick;
    }
}
