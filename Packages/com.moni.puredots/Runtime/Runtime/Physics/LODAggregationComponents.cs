using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Mass proxy component for aggregate physics.
    /// Represents a group of identical items as a single physics entity.
    /// </summary>
    public struct MassProxy : IComponentData
    {
        /// <summary>
        /// Number of entities represented by this proxy.
        /// </summary>
        public int EntityCount;

        /// <summary>
        /// Scaling factor to distribute results back to individual entities.
        /// </summary>
        public float ScaleFactor;

        /// <summary>
        /// Material/resource type identifier for grouping.
        /// </summary>
        public FixedString64Bytes GroupId;
    }

    /// <summary>
    /// Tag component indicating entity should be aggregated into a mass proxy.
    /// </summary>
    public struct AggregateProxyTag : IComponentData
    {
    }
}

