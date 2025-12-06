using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components.Economy
{
    /// <summary>
    /// Soil quality component for economy/ecology feedback (3-4 scalars per biome).
    /// Tracks soil degradation from over-harvesting.
    /// </summary>
    public struct SoilQuality : IComponentData
    {
        public float Fertility;      // 0-1, affects yield
        public float Moisture;       // 0-1, affects growth
        public float NutrientLevel; // 0-1, affects health
        public float Pollution;     // 0-1, affects all above
    }

    /// <summary>
    /// Population pressure component tracking over-population effects.
    /// </summary>
    public struct PopulationPressure : IComponentData
    {
        public float Density;        // Population per area
        public float FoodDemand;     // Food required per tick
        public float FoodAvailability; // Food available per tick
        public float PressureRatio;  // Demand / Availability
    }

    /// <summary>
    /// Resource yield component tracking harvest yields.
    /// </summary>
    public struct ResourceYield : IComponentData
    {
        public float BaseYield;      // Base yield per harvest
        public float CurrentYield;   // Current yield (affected by soil quality)
        public float HarvestRate;     // Harvests per tick
        public float RegenerationRate; // Regeneration per tick
    }
}

