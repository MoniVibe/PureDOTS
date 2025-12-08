using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Ownership
{
    /// <summary>
    /// Resource stock buffer element tracking resource quantities on assets.
    /// Used for production/consumption chains and trade routes.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct ResourceStock : IBufferElementData
    {
        /// <summary>
        /// Resource type identifier (matches ResourceTypeId.Value).
        /// </summary>
        public FixedString64Bytes ResourceType;

        /// <summary>
        /// Current quantity of this resource type.
        /// </summary>
        public float Quantity;

        /// <summary>
        /// Maximum capacity for this resource type (0 = unlimited).
        /// </summary>
        public float MaxCapacity;
    }

    /// <summary>
    /// Trade route component defining resource flow between assets.
    /// Extends existing LogisticsRequest system for spatial pathfinding.
    /// </summary>
    public struct TradeRoute : IComponentData
    {
        /// <summary>
        /// Source asset entity (where resources originate).
        /// </summary>
        public Entity Source;

        /// <summary>
        /// Destination asset entity (where resources are delivered).
        /// </summary>
        public Entity Destination;

        /// <summary>
        /// Flow rate (units per second) for resource movement.
        /// </summary>
        public float FlowRate;

        /// <summary>
        /// Resource type being transported.
        /// </summary>
        public FixedString64Bytes ResourceType;

        /// <summary>
        /// Tick when trade route was created.
        /// </summary>
        public uint CreatedTick;

        /// <summary>
        /// Tick when trade route was last updated.
        /// </summary>
        public uint LastUpdateTick;
    }
}

