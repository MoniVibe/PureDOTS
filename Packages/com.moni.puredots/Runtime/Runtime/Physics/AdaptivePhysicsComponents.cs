using Unity.Entities;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Mass tier enumeration for adaptive precision physics.
    /// Determines update frequency and integration model.
    /// </summary>
    public enum MassTier : byte
    {
        Light = 0,    // < 10⁴ kg: 60 Hz full rigid-body
        Medium = 1,  // 10⁴–10⁸ kg: 6 Hz simplified inertia
        Heavy = 2    // > 10⁸ kg: 0.6 Hz analytic orbit/drag
    }

    /// <summary>
    /// Component storing the mass tier for an entity.
    /// Updated by AdaptivePhysicsSystem based on mass thresholds.
    /// </summary>
    public struct MassTierComponent : IComponentData
    {
        public MassTier Tier;
    }

    /// <summary>
    /// Configuration for physics tier thresholds and tick rates.
    /// </summary>
    public struct PhysicsTierConfig : IComponentData
    {
        /// <summary>
        /// Mass threshold for Medium tier (kg). Default: 10⁴ kg.
        /// </summary>
        public float MediumTierThreshold;

        /// <summary>
        /// Mass threshold for Heavy tier (kg). Default: 10⁸ kg.
        /// </summary>
        public float HeavyTierThreshold;

        /// <summary>
        /// Tick rate multiplier for Light tier. Default: 1.0 (60 Hz).
        /// </summary>
        public float LightTierRate;

        /// <summary>
        /// Tick rate multiplier for Medium tier. Default: 0.1 (6 Hz).
        /// </summary>
        public float MediumTierRate;

        /// <summary>
        /// Tick rate multiplier for Heavy tier. Default: 0.01 (0.6 Hz).
        /// </summary>
        public float HeavyTierRate;

        public static PhysicsTierConfig Default => new PhysicsTierConfig
        {
            MediumTierThreshold = 10000f,  // 10⁴ kg
            HeavyTierThreshold = 100000000f, // 10⁸ kg
            LightTierRate = 1.0f,
            MediumTierRate = 0.1f,
            HeavyTierRate = 0.01f
        };
    }
}

