using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Individual
{
    /// <summary>
    /// Installed augmentations on an individual.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct InstalledAugmentation : IBufferElementData
    {
        public FixedString64Bytes SlotId;
        public FixedString64Bytes AugmentId;
        public float Quality; // 0-1
        public byte Tier; // 0-255
        public byte Rarity; // 0-255 (project-specific mapping)
        public FixedString64Bytes ManufacturerId;
        public uint StatusFlags;
    }

    /// <summary>
    /// Aggregated augmentation modifiers for presentation and decisioning.
    /// </summary>
    public struct AugmentationSummary : IComponentData
    {
        public float PhysiqueModifier;
        public float FinesseModifier;
        public float WillModifier;
        public float GeneralModifier;
        public float TotalUpkeepCost;
        public float RiskFactor;
        public byte AugmentCount;
        public byte BionicsCount;
    }
}
