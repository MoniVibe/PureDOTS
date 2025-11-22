using Unity.Entities;

namespace PureDOTS.Runtime.Economy
{
    public struct BatchPricingState : IComponentData
    {
        public float LastPriceMultiplier;
        public uint LastUpdateTick;
    }

    public struct BatchPricingConfig : IComponentData
    {
        public float MinMultiplier;
        public float MaxMultiplier;
        public float LowFillThreshold;
        public float HighFillThreshold;

        public static BatchPricingConfig CreateDefault()
        {
            return new BatchPricingConfig
            {
                MinMultiplier = 0.8f,
                MaxMultiplier = 1.4f,
                LowFillThreshold = 0.25f,
                HighFillThreshold = 0.9f
            };
        }
    }
}
