using System;
using Unity.Entities;

namespace PureDOTS.Runtime.Individual
{
    [Flags]
    public enum AnatomyPartTags : int
    {
        None = 0,
        Internal = 1 << 0,
        Limb = 1 << 1,
        Sensory = 1 << 2,
        Vital = 1 << 3
    }

    [Flags]
    public enum ConditionFlags : int
    {
        None = 0,
        Missing = 1 << 0,
        Impaired = 1 << 1,
        OneEyeMissing = 1 << 2
    }

    public static class AnatomyPartIds
    {
        public const int Head = 1;
        public const int EyeLeft = 2;
        public const int EyeRight = 3;
        public const int Brain = 4;
    }

    /// <summary>
    /// Anatomy part definition. Parts are hierarchical via ParentIndex.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct AnatomyPart : IBufferElementData
    {
        public int PartId;
        public int ParentIndex;
        public float Coverage;
        public AnatomyPartTags Tags;
    }

    /// <summary>
    /// Condition attached to a part (or whole entity if TargetPartId=0).
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct Condition : IBufferElementData
    {
        public int TargetPartId;
        public float Severity;
        public int StageId;
        public ConditionFlags Flags;
    }

    /// <summary>
    /// Derived capacities consumed by systems (stable interface).
    /// </summary>
    public struct DerivedCapacities : IComponentData
    {
        public float Sight;
        public float Manipulation;
        public float Consciousness;
        public float ReactionTime;
        public float Boarding;
    }
}
