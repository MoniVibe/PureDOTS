using Unity.Entities;

namespace PureDOTS.Runtime.Villager
{
    /// <summary>
    /// Configuration singleton for villager behavior tuning parameters.
    /// Values are baked from VillagerBehaviorProfile ScriptableObject at authoring time.
    /// </summary>
    public struct VillagerBehaviorConfig : IComponentData
    {
        // Needs system parameters
        public float HungerIncreaseRate;
        public float EnergyDecreaseRate;
        public float HealthRegenRate;
        public float StarvationDamageRate;
        public float EnergyRecoveryMultiplier; // Multiplier when sleeping
        public float StarvationHungerThreshold; // Hunger level that triggers starvation
        public float RegenHungerThreshold; // Hunger level below which health regens
        public float StarvationMoraleDecreaseRate;
        
        // Satisfaction calculation weights (sum should be ~1.0)
        public float SatisfactionHungerWeight;
        public float SatisfactionEnergyWeight;
        public float SatisfactionHealthWeight;
        public float MoraleLerpRate;
        
        // Status system parameters
        public float WellbeingHungerWeight;
        public float WellbeingEnergyWeight;
        public float WellbeingHealthWeight;
        
        // Productivity calculation
        public float ProductivityBase;
        public float ProductivityEnergyWeight;
        public float ProductivityMoraleWeight;
        public float ProductivityMax;
        
        // Movement parameters
        public float ArrivalDistance;
        public float FleeSpeedMultiplier;
        public float LowEnergySpeedMultiplier;
        public float LowEnergyThreshold;
        public float VelocityThreshold; // Minimum velocity to consider moving
        public float RotationSpeed; // Rotation interpolation speed
        
        // AI thresholds
        public float HungerThreshold;
        public float EnergyThreshold;
        public float FleeHealthThreshold;
        public float EatingHungerThresholdMultiplier; // Multiplier of HungerThreshold for stopping eating
        public float EatingDuration; // Max seconds spent eating
        public float FleeDuration; // Max seconds spent fleeing
        public float RestEnergyThreshold; // Energy level that stops resting
        
        // Health thresholds
        public float AliveHealthThreshold; // Health level below which villager is considered dead
        
        /// <summary>
        /// Creates default configuration matching current hard-coded values.
        /// </summary>
        public static VillagerBehaviorConfig CreateDefaults()
        {
            return new VillagerBehaviorConfig
            {
                // Needs defaults
                HungerIncreaseRate = 5f,
                EnergyDecreaseRate = 3f,
                HealthRegenRate = 1f,
                StarvationDamageRate = 10f,
                EnergyRecoveryMultiplier = 2f,
                StarvationHungerThreshold = 90f,
                RegenHungerThreshold = 50f,
                StarvationMoraleDecreaseRate = 5f,
                SatisfactionHungerWeight = 0.5f,
                SatisfactionEnergyWeight = 0.3f,
                SatisfactionHealthWeight = 0.2f,
                MoraleLerpRate = 0.1f,
                
                // Status defaults
                WellbeingHungerWeight = 0.4f,
                WellbeingEnergyWeight = 0.4f,
                WellbeingHealthWeight = 0.2f,
                ProductivityBase = 0.25f,
                ProductivityEnergyWeight = 0.5f,
                ProductivityMoraleWeight = 0.25f,
                ProductivityMax = 1.5f,
                
                // Movement defaults
                ArrivalDistance = 0.75f,
                FleeSpeedMultiplier = 1.5f,
                LowEnergySpeedMultiplier = 0.5f,
                LowEnergyThreshold = 20f,
                VelocityThreshold = 0.0001f,
                RotationSpeed = 4f,
                
                // AI defaults
                HungerThreshold = 70f,
                EnergyThreshold = 20f,
                FleeHealthThreshold = 25f,
                EatingHungerThresholdMultiplier = 0.5f,
                EatingDuration = 3f,
                FleeDuration = 5f,
                RestEnergyThreshold = 80f,
                
                // Health defaults
                AliveHealthThreshold = 0.1f
            };
        }
    }
}

