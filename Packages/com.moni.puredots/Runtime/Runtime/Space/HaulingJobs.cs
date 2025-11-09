using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Space
{
    public enum HaulingJobPriority : byte
    {
        Low,
        Normal,
        High
    }

    public struct HaulingJob : IComponentData
    {
        public HaulingJobPriority Priority;
        public Entity SourceEntity;
        public Entity DestinationEntity;
        public float RequestedAmount;
    }

    [InternalBufferCapacity(16)]
    public struct HaulingJobQueueEntry : IBufferElementData
    {
        public HaulingJobPriority Priority;
        public Entity SourceEntity;
        public Entity DestinationEntity;
        public float RequestedAmount;
    }

    public struct HaulerRole : IComponentData
    {
        public byte IsDedicatedFreighter;
    }
}
