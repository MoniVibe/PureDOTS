using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Runtime
{
    /// <summary>
    /// Component for carrier entities that receive resources from mining vessels.
    /// </summary>
    public struct Carrier : IComponentData
    {
        public int CarrierId;
        public float TotalCapacity; // Total storage capacity
        public float CurrentLoad; // Current total load across all resource types
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Buffer element for carrier inventory - tracks resources stored in carrier.
    /// </summary>
    public struct CarrierInventoryItem : IBufferElementData
    {
        public ushort ResourceTypeIndex;
        public float Amount;
    }
}

