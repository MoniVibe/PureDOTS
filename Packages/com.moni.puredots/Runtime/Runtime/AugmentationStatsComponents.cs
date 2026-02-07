using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Aggregate augmentation modifiers applied to an entity.
    /// </summary>
    public struct AugmentationStats : IComponentData
    {
        public float PhysiqueModifier;
        public float FinesseModifier;
        public float WillModifier;
        public float GeneralModifier;
        public float TotalUpkeepCost;
        public float AggregatedRiskFactor;
    }
}
