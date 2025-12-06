using Unity.Entities;

namespace PureDOTS.Runtime.Diagnostics
{
    /// <summary>
    /// Configuration singleton for diagnostics system with safety toggles per category.
    /// </summary>
    public struct DiagnosticsConfig : IComponentData
    {
        /// <summary>Enable archetype validation checks.</summary>
        public bool EnableArchetypeValidation;

        /// <summary>Enable blob reference validation checks.</summary>
        public bool EnableBlobValidation;

        /// <summary>Enable registry entry validation checks.</summary>
        public bool EnableRegistryValidation;

        /// <summary>Enable component data bounds checking (NaN, infinity, out-of-range).</summary>
        public bool EnableComponentBoundsValidation;

        /// <summary>Maximum number of errors to report per category per tick.</summary>
        public int MaxErrorsPerCategory;

        /// <summary>Maximum total errors to report per tick.</summary>
        public int MaxTotalErrorsPerTick;

        public static DiagnosticsConfig Default => new DiagnosticsConfig
        {
            EnableArchetypeValidation = true,
            EnableBlobValidation = true,
            EnableRegistryValidation = true,
            EnableComponentBoundsValidation = true,
            MaxErrorsPerCategory = 10,
            MaxTotalErrorsPerTick = 50
        };
    }
}

