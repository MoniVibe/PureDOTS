using Unity.Entities;
using Unity.Collections;

namespace PureDOTS.Runtime.Resources
{
    /// <summary>
    /// A request for resources by an agent or group.
    /// Describes what is needed, who needs it, and priority.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct NeedRequest : IBufferElementData
    {
        /// <summary>
        /// Resource type identifier.
        /// </summary>
        public FixedString32Bytes ResourceTypeId;

        /// <summary>
        /// Amount needed.
        /// </summary>
        public float Amount;

        /// <summary>
        /// Entity that needs this resource.
        /// </summary>
        public Entity RequesterEntity;

        /// <summary>
        /// Priority (higher = more urgent).
        /// </summary>
        public float Priority;

        /// <summary>
        /// Tick when request was created.
        /// </summary>
        public uint CreatedTick;

        /// <summary>
        /// Optional target entity (e.g., storehouse to deliver to).
        /// </summary>
        public Entity TargetEntity;

        /// <summary>
        /// Request ID for tracking fulfillment.
        /// </summary>
        public uint RequestId;
    }
}



