using Unity.Entities;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Shared intent-drive pressures used to bias self-preservation, kinship, and loyalty behavior.
    /// </summary>
    public struct VillagerIntentDrive : IComponentData
    {
        public float SelfPreservationPressure;
        public float KinshipPressure;
        public float LoyaltyPressure;
        public Entity KinEntity;
        public Entity LoyaltyEntity;
        public uint LastUpdatedTick;
    }
}
