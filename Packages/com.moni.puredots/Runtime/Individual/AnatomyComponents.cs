using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Individual
{
    /// <summary>
    /// Slot types for sentient anatomy (limb/organ/mount points, etc.).
    /// </summary>
    public enum AnatomySlotType : byte
    {
        Other = 0,
        Limb = 1,
        Organ = 2,
        Neural = 3,
        Sensory = 4,
        Utility = 5
    }

    /// <summary>
    /// Species identifier for sentient anatomy layouts.
    /// </summary>
    public struct SentientAnatomy : IComponentData
    {
        public FixedString64Bytes SpeciesId;
    }

    /// <summary>
    /// Per-species anatomy slots for augment compatibility and damage resolution.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct AnatomySlot : IBufferElementData
    {
        public FixedString64Bytes SlotId;
        public AnatomySlotType SlotType;
        public float HealthMultiplier;
        public FixedString64Bytes CompatibleFamilies;
    }
}
